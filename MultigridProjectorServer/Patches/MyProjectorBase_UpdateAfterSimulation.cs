using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using VRage.Game.Entity.EntityComponents.Interfaces;

namespace MultigridProjector.Patches
{
    [PatchShim]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_UpdateAfterSimulation
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("UpdateAfterSimulation", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)).Prefixes.Add(typeof (MyProjectorBase_UpdateAfterSimulation).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
        
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