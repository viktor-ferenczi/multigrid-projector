using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;

namespace MultigridProjectorClient.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("CanBuild", typeof(MySlimBlock), typeof(bool))]
    [EnsureOriginal("52ad3019")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_CanBuild
    {
        [ClientOnly]
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            MySlimBlock projectedBlock,
            bool checkHavokIntersections,
            // ReSharper disable once InconsistentNaming
            out BuildCheckResult __result)
        {
            __result = BuildCheckResult.NotWeldable;
            
            // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
            var projector = __instance;
            if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                return true;

#if DEBUG
            __result = projection.CanBuild(projectedBlock, checkHavokIntersections, out var fallback);
            return fallback;
#else
            try
            {
                __result = projection.CanBuild(projectedBlock, checkHavokIntersections, out var fallback);
                return fallback;
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
#endif
        }
    }
}