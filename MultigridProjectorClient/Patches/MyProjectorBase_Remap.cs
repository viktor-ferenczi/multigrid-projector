using System;
using HarmonyLib;
using MultigridProjector.Extensions;
using Sandbox.Game.Multiplayer;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("Remap")]
    [EnsureOriginal("bce65541")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_Remap
    {
        [ClientOnly]
        // ReSharper disable once InconsistentNaming
        private static bool Prefix(MyProjectorBase __instance)
        {
            var projector = __instance;

            try
            {
                if (!Sync.IsServer)
                    return false;
                
                projector.RemapObjectBuilders();
                
                // Call patched SetNewBlueprint
                var methodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "SetNewBlueprint");
                methodInfo.Invoke(projector, new object[] {projector.GetOriginalGridBuilders()});
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            // Never run the original handler, because that breaks subgrid connections with inconsistent remapping of Entity IDs
            return false;
        }
    }
}