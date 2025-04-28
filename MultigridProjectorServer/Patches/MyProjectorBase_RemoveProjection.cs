using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;

namespace MultigridProjectorServer.Patches
{
    [PatchShim]
    [EnsureOriginalTorch(typeof(MyProjectorBase), "RemoveProjection", null, "b3568c13")]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_RemoveProjection
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("RemoveProjection", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(typeof (MyProjectorBase_RemoveProjection).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
        
        [ServerOnly]
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(MyProjectorBase __instance, bool keepProjection)
        {
            var projector = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return true;

                projection.RemoveProjection(keepProjection);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
            
            return false;
        }
    }
}