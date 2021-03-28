using System;
using System.Collections.Generic;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using VRage.Game;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("InitFromObjectBuilder")]
    [EnsureOriginal("8e865331")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_InitFromObjectBuilder
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            List<MyObjectBuilder_CubeGrid> gridsObs)
        {
            try
            {
                return MultigridProjection.ProjectorLoadBlueprint(__instance, gridsObs);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
        }
    }
}