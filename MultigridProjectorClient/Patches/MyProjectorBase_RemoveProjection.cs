using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjectorClient.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("RemoveProjection")]
    [EnsureOriginal("c0739d3a")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_RemoveProjection
    {
        [ClientOnly]
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(MyProjectorBase __instance, bool keepProjection)
        {
            var projector = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return true;

                projection.RemoveProjection(keepProjection);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
            
            return false;
        }
    }
}