using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjectorClient.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyMechanicalConnectionBlockBase))]
    [HarmonyPatch("RaiseAttachedEntityChanged")]
    [EnsureOriginal("f0a1b3d3")]
    // ReSharper disable once InconsistentNaming
    public static class MyMechanicalConnectionBlockBase_RaiseAttachedEntityChanged
    {
        [ClientOnly]
        private static void Postfix(
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedParameter.Global
            MyMechanicalConnectionBlockBase __instance)
        {
            var baseBlock = __instance;
            if (!MultigridProjection.TryFindProjectionByBuiltGrid(baseBlock.CubeGrid, out var projection, out _)) 
                return;

#if DEBUG
            projection.RaiseAttachedEntityChanged();
#else            
            try
            {
                projection.RaiseAttachedEntityChanged();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
#endif
        }
    }
}