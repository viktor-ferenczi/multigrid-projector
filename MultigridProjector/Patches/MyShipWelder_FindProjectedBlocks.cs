using System;
using System.Collections.Generic;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using SpaceEngineers.Game.Entities.Blocks;
using VRageMath;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyShipWelder))]
    [HarmonyPatch("FindProjectedBlocks")]
    [EnsureOriginal("30c15aa0")]
    // ReSharper disable once InconsistentNaming
    public static class MyShipWelder_FindProjectedBlocks
    {
        public static bool Prefix(
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedParameter.Global
            MyShipWelder __instance,
            // ReSharper disable once InconsistentNaming
            out MyWelder.ProjectionRaycastData[] __result,
            // ReSharper disable once InconsistentNaming
            BoundingSphere ___m_detectorSphere,
            // ReSharper disable once InconsistentNaming
            HashSet<MySlimBlock> ___m_projectedBlock)
        {
            var welder = __instance;
            var detectorSphere = ___m_detectorSphere;
            var weldedBlocks = ___m_projectedBlock;

            try
            {
                __result = MultigridProjection.FindProjectedBlocks(welder, detectorSphere, weldedBlocks);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                __result = new MyWelder.ProjectionRaycastData[]{};
            }

            return false;
        }
    }
}