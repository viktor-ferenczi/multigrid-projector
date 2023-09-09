using Entities.Blocks;
using MultigridProjector.Logic;
using MultigridProjectorClient.Utilities;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace MultigridProjectorClient.Extra
{
    internal static class BlockHighlight
    {
        // Implement highlighting for blocks that have not been built or partially built
        private static HashSet<MyProjectorBase> TargetProjectors = new HashSet<MyProjectorBase>();
        private static bool IsProjecting(MyProjectorBase block) => IsWorking(block) && block.ProjectedGrid != null;
        private static bool IsWorking(MyProjectorBase block) => block.CubeGrid?.Physics != null && block.IsWorking;
        private static bool Enabled => Config.CurrentConfig.BlockHighlight;

        public static void Initialize()
        {
            CreateTerminalControls();
            CreateToolbarControls();
        }

        private static void CreateTerminalControls()
        {
            MyTerminalControlCheckbox<MySpaceProjector> highlightBlocks = new MyTerminalControlCheckbox<MySpaceProjector>(
                "HighlightBlocks",
                MyStringId.GetOrCompute("Highlight Blocks"),
                MyStringId.GetOrCompute("Highlight blocks based on their status:\n" +
                    "Green - Can be built\n" +
                    "Yellow - Not fully welded\n" +
                    "Orange - Obstructed by entity\n" +
                    "Red - Obstructed by other block\n" +
                    "No Highlight - Built or unconnected"))
            {
                Getter = (projector) => TargetProjectors.Contains(projector),
                Setter = (projector, value) =>
                {
                    if (value)
                        EnableHighlightBlocks(projector);
                    else
                        DisableHighlightBlocks(projector);
                },

                Visible = (_) => Enabled,
                Enabled = IsProjecting,
                SupportsMultipleBlocks = false
            };

            AddControl.AddControlAfter("ShowOnlyBuildable", highlightBlocks);
        }

        private static void CreateToolbarControls()
        {
            List<IMyTerminalAction> customActions = new List<IMyTerminalAction>();

            {
                IMyTerminalAction action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BlockHighlightToggle");
                action.Enabled = (terminalBlock) => Enabled && terminalBlock is IMyProjector;
                action.Action = (terminalBlock) => ToggleHighlightBlocks(terminalBlock as IMyProjector);
                action.ValidForGroups = true;
                action.Icon = ActionIcons.TOGGLE;
                action.Name = new StringBuilder("Toggle block highlighting");
                action.Writer = (b, s) => s.Append("Highlight");
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
                customActions.Add(action);
            }

            {
                IMyTerminalAction action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BlockHighlightEnable");
                action.Enabled = (terminalBlock) => Enabled && terminalBlock is IMyProjector;
                action.Action = (terminalBlock) => EnableHighlightBlocks(terminalBlock as IMyProjector);
                action.ValidForGroups = true;
                action.Icon = ActionIcons.ON;
                action.Name = new StringBuilder("Enable block highlighting");
                action.Writer = (b, s) => s.Append("Highlight");
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
                customActions.Add(action);
            }

            {
                IMyTerminalAction action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BlockHighlightDisable");
                action.Enabled = (terminalBlock) => Enabled && terminalBlock is IMyProjector;
                action.Action = (terminalBlock) => DisableHighlightBlocks(terminalBlock as IMyProjector);
                action.ValidForGroups = true;
                action.Icon = ActionIcons.OFF;
                action.Name = new StringBuilder("Disable block highlighting");
                action.Writer = (b, s) => s.Append("Highlight");
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
                customActions.Add(action);
            }

            MyAPIGateway.TerminalControls.CustomActionGetter += (block, actions) =>
            {
                if (block is IMyProjector)
                    actions.AddRange(customActions);
            };
        }

        public static bool IsHighlightBlocksEnabled(IMyProjector projector)
        {
            if (projector is null)
                return false;
            
            return TargetProjectors.Contains((MyProjectorBase)projector);
        }

        public static void EnableHighlightBlocks(IMyProjector projector)
        {
            if (projector is null)
                return;
            
            TargetProjectors.Add((MyProjectorBase)projector);
        }

        public static void DisableHighlightBlocks(IMyProjector projector)
        {
            if (projector is null)
                return;
            
            TargetProjectors.Remove((MyProjectorBase)projector);
        }

        public static void ToggleHighlightBlocks(IMyProjector projector)
        {
            if (projector is null)
                return;
            
            if (IsHighlightBlocksEnabled(projector))
            {
                DisableHighlightBlocks(projector);
            }
            else
            {
                EnableHighlightBlocks(projector);
            }
        }

        public static void HighlightLoop()
        {
            // Clean stale projectors
            TargetProjectors.RemoveWhere(block => block.CubeGrid?.Physics == null);

            foreach (MyProjectorBase projector in TargetProjectors)
            {
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out MultigridProjection projection))
                    continue;

                foreach (Subgrid subgrid in projection.GetSupportedSubgrids())
                {
                    HashSet<MySlimBlock> blocks = subgrid.PreviewGrid.GetBlocks();

                    foreach (MySlimBlock block in blocks)
                    {
                        BuildCheckResult buildCheck = projector.CanBuild(block, true);

                        Color highlightColor;
                        if (buildCheck == BuildCheckResult.AlreadyBuilt)
                        {
                            Vector3I builtBlockPos = subgrid.BuiltGrid.WorldToGridInteger(subgrid.PreviewGrid.GridIntegerToWorld(block.Position));
                            MySlimBlock builtBlock = subgrid.BuiltGrid.GetCubeBlock(builtBlockPos);

                            if (builtBlock.Integrity < block.Integrity)
                                highlightColor = Color.Yellow;
                            else
                                continue;
                        }

                        else if (buildCheck == BuildCheckResult.OK)
                            highlightColor = Color.Green;

                        else if (buildCheck == BuildCheckResult.IntersectedWithSomethingElse)
                            highlightColor = Color.DarkOrange;

                        else if (buildCheck == BuildCheckResult.IntersectedWithGrid)
                            highlightColor = Color.Crimson;

                        else
                            continue;

                        WireFrame(block, highlightColor);
                    }
                }
            }
        }

        public static void WireFrame(MySlimBlock block, Color color, MyStringId? material = null)
        {
            // Modified version of MyCubeBuilder.DrawSemiTransparentBox

            MyCubeGrid grid = block.CubeGrid;
            float gridSize = grid.GridSize;

            Vector3I minPos = block.Min;
            Vector3I maxPos = block.Max;

            Vector3D minVector = minPos * gridSize - new Vector3(gridSize / 2f);
            Vector3D maxVector = maxPos * gridSize + new Vector3(gridSize / 2f);
            BoundingBoxD localbox = new BoundingBoxD(minVector, maxVector);

            MatrixD worldMatrix = grid.WorldMatrix;

            MySimpleObjectDraw.DrawTransparentBox(
                ref worldMatrix,
                ref localbox,
                ref color,
                MySimpleObjectRasterizer.Wireframe,
                1,
                0.01f,
                lineMaterial: material ?? MyStringId.GetOrCompute("ContainerBorder"),
                blendType: MyBillboard.BlendTypeEnum.AdditiveTop,
                intensity: 100);
        }
    }
}