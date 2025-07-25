using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;

namespace MultigridProjectorClient.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyWelder))]
    [HarmonyPatch("FindProjectedBlock")]
    [EnsureOriginal("4ece7678")]
    // ReSharper disable once InconsistentNaming
    public static class MyWelder_FindProjectedBlock
    {
        [ClientOnly]
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyCasterComponent rayCaster,
            float distanceMultiplier,
            // ReSharper disable once InconsistentNaming
            out MyWelder.ProjectionRaycastData __result)
        {
            __result = new MyWelder.ProjectionRaycastData {raycastResult = BuildCheckResult.NotFound};

#if DEBUG
            return MultigridProjection.MyWelder_FindProjectedBlock(rayCaster, distanceMultiplier, ref __result);
#else
            try
            {
                return MultigridProjection.MyWelder_FindProjectedBlock(rayCaster, distanceMultiplier, ref __result); 
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