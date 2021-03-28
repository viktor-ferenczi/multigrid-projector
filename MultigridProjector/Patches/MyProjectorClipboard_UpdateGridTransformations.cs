using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
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
        // Client only!
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorClipboard __instance,
            // ReSharper disable once InconsistentNaming
            MyProjectorBase ___m_projector)
        {
            var projector = ___m_projector;

            try
            {
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return true;

                // Alignment is needed on server side as well, so it was moved into UpdateAfterSimulation
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}