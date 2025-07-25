using System;
using HarmonyLib;
using MultigridProjector.Utilities;
using MultigridProjectorClient.Utilities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;

namespace MultigridProjectorClient.Patches
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
            ref long builtBy)
        {
            var projector = __instance;

#if DEBUG
            return Construction.WeldBlock(projector, cubeBlock, owner, ref builtBy);
#else    
            try
            {
                return Construction.WeldBlock(projector, cubeBlock, owner, ref builtBy);
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