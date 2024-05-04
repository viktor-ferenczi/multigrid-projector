using Sandbox.Game.Entities.Blocks;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using MultigridProjectorClient.Utilities;
using Sandbox.ModAPI.Interfaces;
using Entities.Blocks;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using Sandbox.Game.Gui;
using VRage.Utils;

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

            MultigridProjection.InitFromObjectBuilder(projector, gridBuilders);

            // KeepProjection is enabled by InitFromObjectBuilder if it is detected as a
            // repair blueprint. But just in case we make it explicit here, so even in the
            // case of an ambiguous repair projector it will still work as expected.
            projector.SetValue("KeepProjection", true);
        }
    }
}