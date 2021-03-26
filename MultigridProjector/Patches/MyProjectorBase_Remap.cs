using System;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("Remap")]
    [EnsureOriginal("bce65541")]
    // ReSharper disable once InconsistentNaming
    public class MyProjectorBase_Remap
    {
        // ReSharper disable once InconsistentNaming
        private static bool Prefix(MyProjectorBase __instance)
        {
            var projector = __instance;

            try
            {
                Remap(projector);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            // Never run the original handler, because that breaks subgrid connections with inconsistent remapping of Entity IDs
            return false;
        }

        private static void Remap(MyProjectorBase projector)
        {
            if (!Sandbox.Game.Multiplayer.Sync.IsServer)
                return;

            var gridBuilders = projector.GetOriginalGridBuilders();
            if (gridBuilders == null || gridBuilders.Count <= 0)
                return;

            // Consistent remapping of all grids to keep sub-grid relations intact
            MyEntities.RemapObjectBuilderCollection(gridBuilders);

            // Call patched SetNewBlueprint
            var methodInfo = AccessTools.DeclaredMethod(typeof(MyProjectorBase), "SetNewBlueprint");
            methodInfo.Invoke(projector, new object[] {gridBuilders});
        }
    }
}