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
    [HarmonyPatch("CreateTopPartAndAttach")]
    [EnsureOriginal("e1d2892d")]
    // ReSharper disable once InconsistentNaming
    public static class MyMechanicalConnectionBlockBase_CreateTopPartAndAttach
    {
        // private static readonly ConstructorInfo MyCubeBlockDefinitionGroupConstructor = AccessTools.Constructor(typeof(MyCubeBlockDefinitionGroup));

        // [Server]
        public static bool Prefix(
            MyMechanicalConnectionBlockBase __instance,
            long builtBy,
            bool smallToLarge,
            bool instantBuild)
        {
            var baseBlock = __instance;

            try
            {
                var baseGrid = baseBlock.CubeGrid;
                if (!MultigridProjection.TryFindProjectionByBuiltGrid(baseGrid, out var projection, out var subgrid))
                    return true;

                return projection.CreateTopPartAndAttach(subgrid, baseBlock);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
        }
    }
}