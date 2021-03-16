using System;
using System.Collections.Generic;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using VRage.Game;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("SetNewBlueprint")]
    [EnsureOriginal("65d6a2dc")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_SetNewBlueprint
    {
        // ReSharper disable once UnusedMember.Local
        public static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            var projector = __instance;

            try
            {
                if (!projector.Enabled || !projector.AllowWelding || projector.AllowScaling)
                    return true;

                projector.SetHiddenBlock(null);

                MultigridProjection.Create(projector, gridBuilders);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return true;
        }
    }
}