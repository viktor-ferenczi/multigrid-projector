using System;
using System.Reflection;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;

namespace MultigridProjector.Patches
{
    [PatchShim]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_OnOffsetsChanged
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("OnOffsetsChanged", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Suffixes.Add(typeof (MyProjectorBase_OnOffsetsChanged).GetMethod(nameof(Suffix), BindingFlags.Static | BindingFlags.NonPublic));
        
        // ReSharper disable once InconsistentNaming
        private static void Suffix(MyProjectorBase __instance)
        {
            var projector = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return;

                projection.OnOffsetsChanged();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }
    }
}