using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using VRage.Utils;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("UpdateStats")]
    [EnsureOriginal("ee9fdb75")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_UpdateStats
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(MyProjectorBase __instance)
        {
            var projector = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return true;

                projection.UpdateProjectorStats();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}