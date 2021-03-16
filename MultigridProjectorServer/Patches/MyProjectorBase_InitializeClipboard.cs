using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;

namespace MultigridProjectorServer
{
    [PatchShim]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_InitializeClipboard
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("InitializeClipboard", BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(typeof (MyProjectorBase_InitializeClipboard).GetMethod(nameof(Prefix), BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic));

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