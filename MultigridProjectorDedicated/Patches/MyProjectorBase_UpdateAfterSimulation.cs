using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using VRage.Game.Entity.EntityComponents.Interfaces;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("UpdateAfterSimulation")]
    [EnsureOriginal("df3f0506")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_UpdateAfterSimulation
    {
        [ServerOnly]
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance)
        {
            var projector = __instance;

            try
            {
                return MultigridProjection.ProjectorUpdateAfterSimulation(projector);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
        }
    }
}