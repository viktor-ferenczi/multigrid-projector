using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using VRage.Game;

namespace MultigridProjectorServer.Patches
{
    [PatchShim]
    [EnsureOriginalTorch(typeof(MyProjectorBase), "Init", null, "4f7ff8c3")]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyProjectorBase_Init
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof(MyProjectorBase).GetMethod("Init", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)).Suffixes.Add(typeof(MyProjectorBase_Init).GetMethod(nameof(Suffix), BindingFlags.Static | BindingFlags.NonPublic));

        [ServerOnly]
        // ReSharper disable once UnusedMember.Local
        private static void Suffix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            MyObjectBuilder_CubeBlock objectBuilder,
            MyCubeGrid cubeGrid)
        {
            var projector = __instance;

            try
            {
                MultigridProjection.ProjectorInit(projector, objectBuilder);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }
    }
}