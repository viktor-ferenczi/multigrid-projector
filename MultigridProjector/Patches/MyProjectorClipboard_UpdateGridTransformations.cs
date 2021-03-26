using System;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Cube;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorClipboard))]
    [HarmonyPatch("UpdateGridTransformations")]
    [EnsureOriginal("6a6d82b9")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorClipboard_UpdateGridTransformations
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorClipboard __instance)
        {
            var clipboard = __instance;

            try
            {
                // Ensure an active clipboard with preview grids
                var previewGrids = clipboard.PreviewGrids;
                if(previewGrids == null || previewGrids.Count == 0)
                    return true;
                        
                // Projector is linked to the preview grids
                var projector = previewGrids[0].Projector;
                if (projector == null || !projector.AllowWelding || projector.AllowScaling)
                    return true;

                // The projector must have a blueprint loaded
                var gridBuilders = projector.GetOriginalGridBuilders();
                if (gridBuilders == null || gridBuilders.Count == 0)
                    return true;

                // Create custom data model for the multigrid projection if not exist
                var projection = MultigridProjection.Create(projector, gridBuilders);
                if (projection == null)
                    return true;
                
                // Align the preview grids to match any grids has already been built
                projection.UpdateGridTransformations();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}