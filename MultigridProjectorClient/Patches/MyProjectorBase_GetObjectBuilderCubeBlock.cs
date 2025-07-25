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
    [HarmonyPatch("GetObjectBuilderCubeBlock")]
    [EnsureOriginal("497919c6")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_GetObjectBuilderCubeBlock
    {
        [ClientOnly]
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            bool copy,
            // ReSharper disable once InconsistentNaming
            MyObjectBuilder_CubeBlock __result)
        {
#if DEBUG
            MultigridProjection.GetObjectBuilderOfProjector(__instance, copy, __result);
#else
            try
            {
                MultigridProjection.GetObjectBuilderOfProjector(__instance, copy, __result);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
#endif
        }
    }
}