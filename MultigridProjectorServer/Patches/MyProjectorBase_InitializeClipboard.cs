using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;

namespace MultigridProjectorServer.Patches
{
    [PatchShim]
    [EnsureOriginalTorch(typeof(MyProjectorBase), "InitializeClipboard", null, "2f55ee2e")]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_InitializeClipboard
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("InitializeClipboard", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(typeof (MyProjectorBase_InitializeClipboard).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));

        [ServerOnly]
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance)
        {
            var projector = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return true;

                projection.InitializeClipboard();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}