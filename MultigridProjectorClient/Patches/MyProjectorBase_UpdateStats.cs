using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjectorClient.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("UpdateStats")]
    [EnsureOriginal("15addd6e")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_UpdateStats
    {
        [ClientOnly]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        private static bool Prefix(MyProjectorBase __instance)
        {
            var projector = __instance;

            // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
            if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                return true;
            
#if DEBUG
            projection.UpdateProjectorStats();
#else
            try
            {
                projection.UpdateProjectorStats();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
#endif

            return false;
        }
    }
}