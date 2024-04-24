using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.Game;
using VRageMath;
using MultigridProjectorClient.Utilities;
using Sandbox.ModAPI.Interfaces;
using Entities.Blocks;
using Sandbox.Game.Gui;
using VRage.Utils;
using MultigridProjector.Utilities;

// ReSharper disable SuggestVarOrType_Elsewhere
namespace MultigridProjectorClient.Extra
{
    static class RepairProjection
    {
        private static bool Enabled => Config.CurrentConfig.RepairProjection;
        private static bool IsWorkingButNotProjecting(MyProjectorBase block) => IsWorking(block) && block.ProjectedGrid == null;
        private static bool IsWorking(MyProjectorBase block) => block.CubeGrid?.Physics != null && block.IsWorking;

        public static IEnumerable<CustomControl> IterControls()
        {
            var control = new MyTerminalControlButton<MySpaceProjector>(
                "RepairProjection",
                MyStringId.GetOrCompute("Load Repair Projection"),
                MyStringId.GetOrCompute("Loads the projector's own grid as a repair projection."),
                LoadMechanicalGroup)
            {
                Visible = (projector) => Enabled && IsWorking(projector),
                Enabled = IsWorkingButNotProjecting,
                SupportsMultipleBlocks = false
            };

            yield return new CustomControl(ControlPlacement.After, "Remove", control);
        }

        private static void LoadMechanicalGroup(MyProjectorBase projector)
        {
            List<IMyCubeGrid> grids = CollectGrids(projector);
            List<MyObjectBuilder_CubeGrid> gridBuilders = grids.Select(grid => grid.GetObjectBuilder()).Cast<MyObjectBuilder_CubeGrid>().ToList();

            Delegate initFromObjectBuilder = Reflection.GetMethod(typeof(MyProjectorBase), projector, "InitFromObjectBuilder");
            initFromObjectBuilder.DynamicInvoke(gridBuilders, null);

            projector.SetValue("KeepProjection", true);
            AlignToRepairProjector(projector, gridBuilders[0]);
        }

        private static List<IMyCubeGrid> CollectGrids(MyProjectorBase projector)
        {
            var grids = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(projector.CubeGrid, GridLinkTypeEnum.Mechanical, grids);

            grids.Remove(projector.CubeGrid);
            grids.Insert(0, projector.CubeGrid);

            return grids;
        }

        private static void AlignToRepairProjector(IMyProjector projector, MyObjectBuilder_CubeGrid gridBuilder)
        {
            // Find the projector itself in the self repair projection
            var projectorBuilder = gridBuilder
                .CubeBlocks
                .OfType<MyObjectBuilder_Projector>()
                .FirstOrDefault(b => b.Name == projector.Name);

            if (projectorBuilder == null) return;

            projector.Orientation.GetQuaternion(out var gridToProjectorQuaternion);
            var projectorToGridQuaternion = Quaternion.Inverse(gridToProjectorQuaternion);

            OrientationAlgebra.ProjectionRotationFromForwardAndUp(
                Base6Directions.GetDirection(projectorToGridQuaternion.Forward),
                Base6Directions.GetDirection(projectorToGridQuaternion.Up),
                out var projectionRotation);

            var blocks = new List<IMySlimBlock>();
            projector.CubeGrid.GetBlocks(blocks, block => blocks.Count == 0);
            if (blocks.Count == 0)
                return;

            var projectorPosition = projector.Position - blocks[0].Position;
            var projectionOffset = new Vector3I(Vector3.Round(projectorToGridQuaternion * projectorPosition));
            projectionOffset = Vector3I.Clamp(projectionOffset, new Vector3I(-50), new Vector3I(50));

            projector.ProjectionOffset = projectionOffset;
            projector.ProjectionRotation = projectionRotation;
            projector.UpdateOffsetAndRotation();
        }
    }
}
