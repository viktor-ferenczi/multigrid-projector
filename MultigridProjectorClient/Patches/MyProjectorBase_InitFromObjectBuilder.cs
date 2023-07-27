using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using MultigridProjectorClient.Menus;
using MultigridProjectorClient.Utilities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using VRage;
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
        [ClientOnly]
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            List<MyObjectBuilder_CubeGrid> gridsObs)
        {
            try
            {
                if (MultigridProjection.InitFromObjectBuilder(__instance, gridsObs))
                    return true;

                if (!Comms.ServerPlugin && gridsObs.Count > 1 && Config.CurrentConfig.ShowDialogs)
                {
                    if (Config.CurrentConfig.ClientWelding)
                        MyGuiSandbox.AddScreen(ProjectionDialog.CreateDialog());

                    else
                        MyGuiSandbox.AddScreen(ProjectionDialog.CreateUnsupportedDialog());
                }

                return false;
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
        }
    }
}