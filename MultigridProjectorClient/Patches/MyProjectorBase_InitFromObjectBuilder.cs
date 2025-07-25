using System;
using System.Collections.Generic;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using MultigridProjectorClient.Menus;
using MultigridProjectorClient.Utilities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Graphics.GUI;
using VRage.Game;

namespace MultigridProjectorClient.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("InitFromObjectBuilder")]
    [EnsureOriginal("3b1e7d1f")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_InitFromObjectBuilder
    {
        [ClientOnly]
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            List<MyObjectBuilder_CubeGrid> gridsObs)
        {
#if DEBUG
            return InitFromObjectBuilder_Implementation(__instance, gridsObs);
#else
            try
            {
                return InitFromObjectBuilder_Implementation(__instance, gridsObs);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
#endif
        }

        private static bool InitFromObjectBuilder_Implementation(MyProjectorBase projector, List<MyObjectBuilder_CubeGrid> gridsObs)
        {
            if (MultigridProjection.InitFromObjectBuilder(projector, gridsObs))
                return true;

            if (!projector.AllowScaling && 
                !Comms.ServerHasPlugin && 
                gridsObs.Count > 1 && 
                Config.CurrentConfig.ShowDialogs)
            {
                if (Config.CurrentConfig.ClientWelding)
                    MyGuiSandbox.AddScreen(ProjectionDialog.CreateDialog());
                else
                    MyGuiSandbox.AddScreen(ProjectionDialog.CreateUnsupportedDialog());
            }

            return false;
        }
    }
}