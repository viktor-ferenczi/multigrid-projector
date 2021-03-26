using System;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using VRage.Game;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("Init")]
    [EnsureOriginal("71397e45")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_Init
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            MyObjectBuilder_CubeBlock objectBuilder,
            MyCubeGrid cubeGrid)
        {
            var projector = __instance;

            try
            {
                Init(projector, objectBuilder);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        private static void Init(MyProjectorBase projector, MyObjectBuilder_CubeBlock objectBuilder)
        {
            if (projector.CubeGrid == null || !projector.AllowWelding)
                return;

            if (!(objectBuilder is MyObjectBuilder_ProjectorBase projectorBuilder))
                return;

            var gridBuilders = projectorBuilder.ProjectedGrids;
            if (gridBuilders == null || gridBuilders.Count < 1)
                return;

            projector.SetOriginalGridBuilders(gridBuilders);
        }
    }
}