using System.Collections.Generic;
using System.Linq;
using Entities.Blocks;
using MultigridProjector.Logic;
using MultigridProjectorClient.Utilities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Gui;
using VRage.Utils;

namespace MultigridProjectorClient.Extra
{
    public static class ApplyPaint
    {
        private static bool IsProjecting(MyProjectorBase block) => IsWorking(block) && block.ProjectedGrid != null;
        private static bool IsWorking(MyProjectorBase block) => block.CubeGrid?.Physics != null && block.IsWorking;

        public static IEnumerable<CustomControl> IterControls()
        {
            var control = new MyTerminalControlButton<MySpaceProjector>(
                "ApplyPaint",
                MyStringId.GetOrCompute("Apply Paint"),
                MyStringId.GetOrCompute("Applies paint from the projection to the built blocks."),
                ApplyPaintFromProjection)
            {
                Visible = (projector) => !projector.AllowScaling && IsWorking(projector),
                Enabled = IsProjecting,
                SupportsMultipleBlocks = false
            };

            yield return new CustomControl(ControlPlacement.Before, "Blueprint", control);
        }

        private static void ApplyPaintFromProjection(MyProjectorBase projector)
        {
            if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                return;

            foreach (var subgrid in projection.GetSupportedSubgrids())
            {
                if (!subgrid.HasBuilt)
                    continue;

                var builtGrid = subgrid.BuiltGrid;
                foreach (var (position, projectedBlock) in subgrid.Blocks)
                {
                    var builtBlock = projectedBlock.SlimBlock;
                    if (builtBlock == null)
                        continue;

                    var previewBlock = projectedBlock.Preview;
                    if (builtBlock.ColorMaskHSV == previewBlock.ColorMaskHSV &&
                        builtBlock.SkinSubtypeId == previewBlock.SkinSubtypeId)
                        continue;

                    builtGrid.SkinBlocks(position, position, previewBlock.ColorMaskHSV, previewBlock.SkinSubtypeId, false);
                }
            }
        }
    }
}