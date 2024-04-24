using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.Game;
using VRageMath;
using MultigridProjectorClient.Utilities;
using Sandbox.ModAPI.Interfaces;
using Entities.Blocks;
using MultigridProjector.Extensions;
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

            yield return new CustomControl(ControlPlacement.Before, "Blueprint", control);
        }

        private static void LoadMechanicalGroup(MyProjectorBase projector)
        {
            var focusedGrids = projector.GetFocusedGridsInMechanicalGroup();
            if (focusedGrids == null)
                return;

            var gridBuilders = focusedGrids
                .Select(grid => grid.GetObjectBuilder())
                .Cast<MyObjectBuilder_CubeGrid>()
                .ToList();

            var initFromObjectBuilder = Reflection.GetMethod(typeof(MyProjectorBase), projector, "InitFromObjectBuilder");
            initFromObjectBuilder.DynamicInvoke(gridBuilders, null);

            projector.SetValue("KeepProjection", true);
            AlignToRepairProjector(projector, gridBuilders[0]);
        }

        public static void AlignToRepairProjector(IMyProjector projector, MyObjectBuilder_CubeGrid gridBuilder)
        {
            var projectorBase = (MyProjectorBase) projector;
            var defaultName = projectorBase.BlockDefinition.DisplayNameText;

            // Find the projector itself in the self repair projection
            var projectorBuilders = gridBuilder
                .CubeBlocks
                .OfType<MyObjectBuilder_Projector>()
                .ToList();
            if (projectorBuilders.Count == 0)
                return;
            var projectorBuilder = projectorBuilders.Count == 1
                ? projectorBuilders.First()
                : projectorBuilders.FirstOrDefault(b => b.EntityId == projector.EntityId) ??
                  projectorBuilders.FirstOrDefault(b => FormatBlockName(defaultName, b) == projector.CustomName);
            if (projectorBuilder == null)
                return;

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

        private static string FormatBlockName(string defaultName, MyObjectBuilder_TerminalBlock builder)
        {
            return builder.NumberInGrid > 1 ? $"{defaultName} {builder.NumberInGrid}" : defaultName;
        }
    }
}