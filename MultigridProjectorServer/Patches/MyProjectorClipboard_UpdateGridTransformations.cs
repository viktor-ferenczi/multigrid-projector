using System;
using System.Reflection;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Torch.Managers.PatchManager;

namespace MultigridProjectorServer.Patches
{
    [PatchShim]
    [EnsureOriginalTorch(typeof(MyProjectorClipboard), "UpdateGridTransformations", null, "6a6d82b9")]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorClipboard_UpdateGridTransformations
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof(MyProjectorClipboard).GetMethod("UpdateGridTransformations", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes
            .Add(typeof(MyProjectorClipboard_UpdateGridTransformations).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));

        private static readonly FieldInfo ProjectorFieldInfo = AccessTools.Field(typeof(MyProjectorClipboard), "m_projector");

        [ServerOnly]
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorClipboard __instance)
        {
            var clipboard = __instance;

            try
            {
                var projector = (MyProjectorBase) ProjectorFieldInfo.GetValue(clipboard);
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out _))
                    return true;

                // Alignment is needed on server side as well, so it was moved into UpdateAfterSimulation
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}