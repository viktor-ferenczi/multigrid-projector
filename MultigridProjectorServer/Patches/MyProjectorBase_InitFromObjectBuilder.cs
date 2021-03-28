using System;
using System.Collections.Generic;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using VRage.Game;

namespace MultigridProjector.Patches
{
    [PatchShim]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_InitFromObjectBuilder
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("InitFromObjectBuilder", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(typeof (MyProjectorBase_InitFromObjectBuilder).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
        
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