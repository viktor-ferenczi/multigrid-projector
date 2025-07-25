using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using VRage.Game;

namespace MultigridProjectorClient.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("Init")]
    [EnsureOriginal("4f7ff8c3")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_Init
    {
        [ClientOnly]
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            MyObjectBuilder_CubeBlock objectBuilder)
        {
            var projector = __instance;

#if DEBUG
            MultigridProjection.ProjectorInit(projector, objectBuilder);
#else
            try
            {
                MultigridProjection.ProjectorInit(projector, objectBuilder);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
#endif
        }
    }
}