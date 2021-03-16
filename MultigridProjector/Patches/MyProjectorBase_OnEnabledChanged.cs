using System;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using VRage.Utils;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("OnEnabledChanged")]
    [EnsureOriginal("d081b99f")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_OnEnabledChanged
    {

        // ReSharper disable once UnusedMember.Local
        private static void Postfix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            // ReSharper disable once UnusedParameter.Local
            MyTerminalBlock myTerminalBlock)
        {
            var projector = __instance;

            try
            {
                OnEnabledChanged(projector);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        private static void OnEnabledChanged(MyProjectorBase projector)
        {
            if (!projector.AllowWelding || projector.AllowScaling)
                return;

            if (MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
            {
                if (projector.Enabled)
                    return;

                projection.Destroy();
                return;
            }

            if (!projector.Enabled) return;

            var gridBuilders = projector.GetOriginalGridBuilders();
            MyProjectorBase_InitFromObjectBuilder.InitFromObjectBuilder(projector, gridBuilders);
        }
    }
}