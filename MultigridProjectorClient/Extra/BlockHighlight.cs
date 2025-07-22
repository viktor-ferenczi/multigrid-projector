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
using MultigridProjector.Api;
using MultigridProjector.Extensions;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace MultigridProjectorClient.Extra
{
    static class BlockHighlight
    {
        // Implement highlighting for blocks that have not been built or partially built
        private static readonly HashSet<MyProjectorBase> TargetProjectors = new HashSet<MyProjectorBase>();
        private static bool IsProjecting(MyProjectorBase block) => IsWorking(block) && block.ProjectedGrid != null;
        private static bool IsWorking(MyProjectorBase block) => block.CubeGrid?.Physics != null && block.IsWorking;
        private static bool Enabled => Config.CurrentConfig.BlockHighlight;

        public static IEnumerable<CustomControl> IterControls()
        {
            var control = new MyTerminalControlCheckbox<MySpaceProjector>(
                "BlockHighlight",
                MyStringId.GetOrCompute("Highlight Blocks"),
                MyStringId.GetOrCompute("Highlight blocks based on their status:\n" +
                                        "Green - Can be built\n" +
                                        "Yellow - Not fully welded\n" +
                                        "Cyan - Obstructed by entity\n" +
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

                Visible = (projector) => Enabled && !projector.AllowScaling,
                Enabled = IsProjecting,
                SupportsMultipleBlocks = false
            };

            yield return new CustomControl(ControlPlacement.After, "ShowOnlyBuildable", control);
        }

        public static IEnumerable<IMyTerminalAction> IterActions()
        {
            {
                var action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BlockHighlightToggle");
                action.Enabled = (terminalBlock) => Enabled && terminalBlock is IMyProjector;
                action.Action = (terminalBlock) => ToggleHighlightBlocks(terminalBlock as IMyProjector);
                action.ValidForGroups = true;
                action.Icon = ActionIcons.TOGGLE;
                action.Name = new StringBuilder("Toggle block highlighting");
                action.Writer = (b, s) => s.Append("Highlight");
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
                yield return action;
            }

            {
                var action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BlockHighlightEnable");
                action.Enabled = (terminalBlock) => Enabled && terminalBlock is IMyProjector;
                action.Action = (terminalBlock) => EnableHighlightBlocks(terminalBlock as IMyProjector);
                action.ValidForGroups = true;
                action.Icon = ActionIcons.ON;
                action.Name = new StringBuilder("Enable block highlighting");
                action.Writer = (b, s) => s.Append("Highlight");
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
                yield return action;
            }

            {
                var action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BlockHighlightDisable");
                action.Enabled = (terminalBlock) => Enabled && terminalBlock is IMyProjector;
                action.Action = (terminalBlock) => DisableHighlightBlocks(terminalBlock as IMyProjector);
                action.ValidForGroups = true;
                action.Icon = ActionIcons.OFF;
                action.Name = new StringBuilder("Disable block highlighting");
                action.Writer = (b, s) => s.Append("Highlight");
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
                yield return action;
            }
        }

        private static bool IsHighlightBlocksEnabled(IMyProjector projector)
        {
            if (projector is null)
                return false;

            return TargetProjectors.Contains((MyProjectorBase) projector);
        }

        private static void EnableHighlightBlocks(IMyProjector projector)
        {
            if (projector is null)
                return;

            TargetProjectors.Add((MyProjectorBase) projector);

            EnableCheckHavokIntersections(projector, true);
            MultigridProjection.QuickUpdate = true;
        }

        private static void DisableHighlightBlocks(IMyProjector projector)
        {
            if (projector is null)
                return;

            TargetProjectors.Remove((MyProjectorBase) projector);

            EnableCheckHavokIntersections(projector, false);
            MultigridProjection.QuickUpdate = false;
        }

        private static void ToggleHighlightBlocks(IMyProjector projector)
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

        private static void EnableCheckHavokIntersections(IMyProjector projector, bool checkHavokIntersections)
        {
            if (!MultigridProjection.TryFindProjectionByProjector((MyProjectorBase) projector, out var projection))
                return;

            projection.CheckHavokIntersections = checkHavokIntersections;
            projection.ShouldUpdateProjection();
        }

        public static void HighlightLoop()
        {
            if (!(MySession.Static?.CameraController?.Entity is IMyEntity camera))
                return;

            var cameraPosition = camera.WorldMatrix.Translation;

            const double maxCameraDistanceSquared = 500 * 500; // m^2

            // Clean stale projectors
            TargetProjectors.RemoveWhere(block => block.CubeGrid?.Physics == null);

            foreach (MyProjectorBase projector in TargetProjectors)
            {
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out MultigridProjection projection))
                    continue;

                if (!projector.IsProjecting())
                    continue;

                if ((projector.WorldMatrix.Translation - cameraPosition).LengthSquared() > maxCameraDistanceSquared)
                    continue;

                foreach (Subgrid subgrid in projection.GetSupportedSubgrids())
                {
                    using (subgrid.BlocksLock.Read())
                    {
                        foreach (var projectedBlock in subgrid.Blocks.Values)
                        {
                            var color = Color.Black;
                            switch (projectedBlock.BuildCheckResult)
                            {
                                case BuildCheckResult.OK:
                                    color = Color.Green;
                                    break;
                                case BuildCheckResult.NotConnected:
                                    continue;
                                case BuildCheckResult.IntersectedWithGrid:
                                    color = Color.Crimson;
                                    break;
                                case BuildCheckResult.IntersectedWithSomethingElse:
                                    color = Color.Cyan;
                                    break;

                                case BuildCheckResult.AlreadyBuilt:
                                    if (projectedBlock.State == BlockState.BeingBuilt)
                                    {
                                        color = Color.Yellow;
                                        break;
                                    }
                                    continue;
                                case BuildCheckResult.NotFound:
                                    continue;
                                case BuildCheckResult.NotWeldable:
                                    continue;
                            }
                            WireFrame(projectedBlock.Preview, color);
                        }
                    }
                }
            }
        }

        private static void WireFrame(MySlimBlock block, Color color, MyStringId? material = null)
        {
            // Modified version of MyCubeBuilder.DrawSemiTransparentBox

            MyCubeGrid grid = block.CubeGrid;
            float gridSize = grid.GridSize;

            Vector3I minPos = block.Min;
            Vector3I maxPos = block.Max;

            Vector3D minVector = minPos * gridSize - new Vector3(gridSize / 2f);
            Vector3D maxVector = maxPos * gridSize + new Vector3(gridSize / 2f);
            BoundingBoxD localBox = new BoundingBoxD(minVector, maxVector);

            MatrixD worldMatrix = grid.WorldMatrix;

            MySimpleObjectDraw.DrawTransparentBox(
                ref worldMatrix,
                ref localBox,
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