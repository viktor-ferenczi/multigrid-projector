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
    [HarmonyPatch("UpdateProjection")]
    [EnsureOriginal("576ba4a7")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_UpdateProjection
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        public static bool Prefix(MyProjectorBase __instance)
        {
            var projector = __instance;

            try
            {
                return UpdateProjection(projector);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }

        private static bool UpdateProjection(MyProjectorBase projector)
        {
            // Console block (aka hologram table)?
            if (!projector.AllowWelding || projector.AllowScaling)
                return true;

            // In case of projectors never fall back to the original implementation, because that would start the original update work

            if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                return false;

            if (!projection.UpdateWork.IsComplete)
                return false;

            if (!projection.Initialized)
                return false;

            projector.SetHiddenBlock(null);
            projection.StartUpdateWork();
            return false;
        }
    }
}