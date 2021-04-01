using System;
using System.Reflection;
using HarmonyLib;
using MultigridProjector.Extensions;
using Sandbox.Game.Multiplayer;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;

namespace MultigridProjector.Patches
{
    [PatchShim]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_Remap
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("Remap", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(typeof (MyProjectorBase_Remap).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
        
        [ServerOnly]
        // ReSharper disable once InconsistentNaming
        private static bool Prefix(MyProjectorBase __instance)
        {
            var projector = __instance;

            try
            {
                // Unnecessary:
                // if (!Sync.IsServer)
                //     return false;
                
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