using System;
using System.Reflection;
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
    [EnsureOriginal("73db0d9e")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_Remap
    {
        private static readonly MethodInfo SetNewBlueprintInfo = Validation.EnsureInfo(AccessTools.DeclaredMethod(typeof(MyProjectorBase), "SetNewBlueprint"));

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

                // See #92 on `MyBlueprintIdTracker`: The change to preview block IDs are not registered.
                projector.RemapObjectBuilders();
                
                // Call patched SetNewBlueprint
                var gridBuilders = projector.GetOriginalGridBuilders();
                if (gridBuilders != null && gridBuilders.Count > 0)
                {
                    SetNewBlueprintInfo.Invoke(projector, new object[] {gridBuilders});
                }
                else
                {
                    PluginLog.Warn($"Remap is called on an empty projector: \"{projector.CustomName}\" [{projector.EntityId}] on grid \"{projector.CubeGrid?.DisplayName ?? projector.CubeGrid?.Name}\" [{projector.CubeGrid?.EntityId}]");
                }
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