using System;
using HarmonyLib;
using MultigridProjector.Utilities;
using MultigridProjectorClient.Utilities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("Build")]
    [EnsureOriginal("56be06c3")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_Build
    {
        [ClientOnly]
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            MySlimBlock cubeBlock,
            long owner,
            long builder,
            bool requestInstant,
            ref long builtBy)
        {
            var projector = __instance;

            try
            {
                return Construction.WeldBlock(projector, cubeBlock, owner, ref builtBy);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
        }
    }
}