using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Torch.Managers.PatchManager;

namespace MultigridProjectorServer.Patches
{
    [PatchShim]
    [EnsureOriginalTorch(typeof(MyProjectorBase), "Build", null, "56be06c3")]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_Build
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("Build", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)).Prefixes.Add(typeof (MyProjectorBase_Build).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));

        // Called by the ShipWelder to build blocks and any server side mods
        [ServerOnly]
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            MySlimBlock cubeBlock,
            long owner,
            long builder,
            bool requestInstant,
            ref long builtBy)
        {
            var projector = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return true;

                if (!projection.TryFindPreviewGrid(cubeBlock.CubeGrid, out var gridIndex))
                    return false;

                // Deliver the subgrid index via the builtBy field, the owner will be used instead in BuildInternal
                builtBy = gridIndex;
                return true;
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
        }
    }
}