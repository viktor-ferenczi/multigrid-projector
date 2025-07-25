using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjectorClient.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("previewGrid_OnBlockAdded")]
    [EnsureOriginal("6238b43c")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_OnBlockAdded
    {
        [ServerOnly]
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance)
        {
            var projector = __instance;
            
            // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
            if (!MultigridProjection.TryFindProjectionByProjector(projector, out _))
                return true;

            // Disable the original handler, since the MultigridProjection instance already handles it
            return false;
        }
    }
}