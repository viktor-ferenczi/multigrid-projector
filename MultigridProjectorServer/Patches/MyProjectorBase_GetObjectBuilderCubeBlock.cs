using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using VRage.Game;

namespace MultigridProjectorServer.Patches
{
    [PatchShim]
    [EnsureOriginalTorch(typeof(MyProjectorBase), "GetObjectBuilderCubeBlock", null, "66247c3b")]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_GetObjectBuilderCubeBlock
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof(MyProjectorBase).GetMethod("GetObjectBuilderCubeBlock", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)).Suffixes
            .Add(typeof(MyProjectorBase_GetObjectBuilderCubeBlock).GetMethod(nameof(Suffix), BindingFlags.Static | BindingFlags.NonPublic));

        [ServerOnly]
        // ReSharper disable once UnusedMember.Local
        private static void Suffix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            bool copy,
            // ReSharper disable once InconsistentNaming
            MyObjectBuilder_CubeBlock __result)
        {
            try
            {
                MultigridProjection.GetObjectBuilderOfProjector(__instance, copy, __result);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }
    }
}