using System;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("RemoveProjection")]
    [EnsureOriginal("f6fc8084")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_RemoveProjection
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(MyProjectorBase __instance, bool keepProjection)
        {
            var projector = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return true;

                var buildCompleted = projection.Stats.IsBuildCompleted;
                if (buildCompleted && !projection.Projector.GetKeepProjection())
                    keepProjection = false;

                projector.SetHiddenBlock(null);
                projector.SetStatsDirty(true);
                projector.UpdateText();
                projector.RaisePropertiesChanged();

                if (!keepProjection)
                {
                    projection.Destroy();
                    projector.SetOriginalGridBuilders(null);
                }

                projector.UpdateSounds();

                if (projector.Enabled)
                    projector.SetEmissiveStateWorking();
                else
                    projector.SetEmissiveStateDisabled();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
            return false;
        }
    }
}