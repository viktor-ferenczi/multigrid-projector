using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using VRage.Utils;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyMechanicalConnectionBlockBase))]
    [HarmonyPatch("RaiseAttachedEntityChanged")]
    [EnsureOriginal("f0a1b3d3")]
    // ReSharper disable once InconsistentNaming
    public static class MyMechanicalConnectionBlockBase_RaiseAttachedEntityChanged
    {
        public static void Postfix(
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedParameter.Global
            MyMechanicalConnectionBlockBase __instance)
        {
            var baseBlock = __instance;

            try
            {
                if (!MultigridProjection.TryFindProjectionByBuiltGrid(baseBlock.CubeGrid, out var projection, out var subgrid)) return;

                subgrid.UpdateRequested = true;
                projection.ForceUpdateProjection();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }
    }
}