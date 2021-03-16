using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Cube;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorClipboard))]
    [HarmonyPatch("UpdateGridTransformations")]
    [EnsureOriginal("6a6d82b9")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorClipboard_UpdateGridTransformations
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorClipboard __instance)
        {
            var clipboard = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjectorClipboard(clipboard, out var projection))
                    return true;

                projection.UpdateGridTransformations();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}