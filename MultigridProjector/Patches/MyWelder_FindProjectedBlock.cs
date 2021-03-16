using System;
using System.Text;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyWelder))]
    [HarmonyPatch("FindProjectedBlock")]
    [EnsureOriginal("4ece7678")]
    // ReSharper disable once InconsistentNaming
    public static class MyWelder_FindProjectedBlock
    {
        public static bool Prefix(
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedParameter.Global
            MyWelder __instance,
            // ReSharper disable once InconsistentNaming
            MyCasterComponent rayCaster,
            float distanceMultiplier,
            // ReSharper disable once InconsistentNaming
            out MyWelder.ProjectionRaycastData __result)
        {
            __result = new MyWelder.ProjectionRaycastData {raycastResult = BuildCheckResult.NotFound};

            try
            {
                var center = rayCaster.Caster.Center;

                var lookDirection = rayCaster.Caster.FrontPoint - rayCaster.Caster.Center;
                lookDirection.Normalize();

                var distance = MyEngineerToolBase.DEFAULT_REACH_DISTANCE * distanceMultiplier;
                var reachFarPoint = center + lookDirection * distance;
                var viewLine = new LineD(center, reachFarPoint);

                if (!MyCubeGrid.GetLineIntersection(ref viewLine, out var previewGridByWeldingLine, out _, out _, x => x.Projector != null))
                    return false;

                if (previewGridByWeldingLine.Projector == null)
                    return false;

                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(previewGridByWeldingLine.Projector, out var projection))
                    return true;

                projection.FindProjectedBlock(center, reachFarPoint, ref __result);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}