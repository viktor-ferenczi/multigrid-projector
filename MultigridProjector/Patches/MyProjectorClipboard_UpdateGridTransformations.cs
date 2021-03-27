using System;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
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
            MyProjectorClipboard __instance,
            // ReSharper disable once InconsistentNaming
            MyProjectorBase ___m_projector)
        {
            var clipboard = __instance;
            var projector = ___m_projector;

            try
            {
                if (projector == null || projector.Closed || !projector.Enabled || !projector.IsFunctional || !clipboard.IsActive)
                    return true;

                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                {
                    // The projector must have a blueprint loaded
                    var gridBuilders = projector.GetOriginalGridBuilders();
                    if (gridBuilders == null || gridBuilders.Count == 0 || clipboard.CopiedGrids?.Count != gridBuilders.Count)
                        return true;
                    
                    projection = MultigridProjection.Create(projector, gridBuilders);
                    if (projection == null)
                        return true;
                }
                
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