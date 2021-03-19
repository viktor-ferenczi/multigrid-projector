using System;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using VRage.Game;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("GetObjectBuilderCubeBlock")]
    [EnsureOriginal("6b6ba5b3")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_GetObjectBuilderCubeBlock
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            // ReSharper disable once InconsistentNaming
            MyObjectBuilder_CubeBlock __result,
            bool copy)
        {
            try
            {
                GetObjectBuilderCubeBlock(__instance, __result, copy);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        private static void GetObjectBuilderCubeBlock(MyProjectorBase projector, MyObjectBuilder_CubeBlock blockBuilder, bool copy)
        {
            if (!copy) return;

            var clipboard = projector.GetClipboard();
            if (clipboard?.CopiedGrids == null || clipboard.CopiedGrids.Count < 1)
                return;

            var gridBuilders = projector.GetOriginalGridBuilders();
            if (gridBuilders == null)
                return;

            // Fix the inconsistent remapping the original implementation has done, this is
            // needed to be able to load back the projection properly form a saved world
            var builderCubeBlock = (MyObjectBuilder_ProjectorBase) blockBuilder;
            builderCubeBlock.ProjectedGrids = gridBuilders.Clone();
            MyEntities.RemapObjectBuilderCollection(builderCubeBlock.ProjectedGrids);
        }
    }
}