using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using SpaceEngineers.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using VRageMath;

namespace MultigridProjector.Patches
{
    [PatchShim]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyShipWelder_FindProjectedBlocks
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyShipWelder).GetMethod("FindProjectedBlocks", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(typeof (MyShipWelder_FindProjectedBlocks).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));

        private static readonly FieldInfo DetectorSphereFieldInfo = AccessTools.Field(typeof(MyShipWelder), "_m_detectorSphere");
        private static readonly FieldInfo ProjectedBlockFieldInfo = AccessTools.Field(typeof(MyShipWelder), "_m_projectedBlock");
        
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedParameter.Global
            MyShipWelder __instance,
            // ReSharper disable once InconsistentNaming
            ref MyWelder.ProjectionRaycastData[] __result)
        {
            var welder = __instance;
            var detectorSphere = (BoundingSphere)DetectorSphereFieldInfo.GetValue(welder);
            var weldedBlocks = (HashSet<MySlimBlock>)ProjectedBlockFieldInfo.GetValue(welder);

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