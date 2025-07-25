using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjectorClient.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("InitializeClipboard")]
    [EnsureOriginal("dcf7ea34")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_InitializeClipboard
    {
        [ClientOnly]
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance)
        {
            var projector = __instance;
            
            // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
            if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                return true;

#if DEBUG
            projection.InitializeClipboard();
#else
            try
            {
                projection.InitializeClipboard();
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