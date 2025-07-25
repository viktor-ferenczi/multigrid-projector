using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyMechanicalConnectionBlockBase))]
    [HarmonyPatch("CreateTopPartAndAttach")]
    [EnsureOriginal("7af10869")]
    // ReSharper disable once InconsistentNaming
    public static class MyMechanicalConnectionBlockBase_CreateTopPartAndAttach
    {
        [ServerOnly]
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyMechanicalConnectionBlockBase __instance)
        {
            var baseBlock = __instance;
            if (!MultigridProjection.TryFindProjectionByBuiltGrid(baseBlock.CubeGrid, out var projection, out var subgrid))
                return true;
            
#if DEBUG
            return projection.CreateTopPartAndAttach(subgrid, baseBlock);
#else
            try
            {
                return projection.CreateTopPartAndAttach(subgrid, baseBlock);
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