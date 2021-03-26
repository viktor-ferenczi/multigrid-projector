using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("OnOffsetsChanged")]
    [EnsureOriginal("6c57eeaf")]
    // ReSharper disable once InconsistentNaming
    public class MyProjectorBase_OnOffsetsChanged
    {
        // ReSharper disable once InconsistentNaming
        private static void Postfix(MyProjectorBase __instance)
        {
            var projector = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return;

                projection.RescanFullProjection();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }
    }
}