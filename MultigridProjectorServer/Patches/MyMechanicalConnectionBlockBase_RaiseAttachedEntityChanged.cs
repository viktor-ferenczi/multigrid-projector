using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;

namespace MultigridProjector.Patches
{
    [PatchShim]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public static class MyMechanicalConnectionBlockBase_RaiseAttachedEntityChanged
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyMechanicalConnectionBlockBase).GetMethod("RaiseAttachedEntityChanged", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Suffixes.Add(typeof (MyMechanicalConnectionBlockBase_RaiseAttachedEntityChanged).GetMethod(nameof(Suffix), BindingFlags.Static | BindingFlags.NonPublic));
        
        private static void Suffix(
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedParameter.Global
            MyMechanicalConnectionBlockBase __instance)
        {
            var baseBlock = __instance;

            try
            {
                if (!MultigridProjection.TryFindProjectionByBuiltGrid(baseBlock.CubeGrid, out var projection, out _)) return;

                projection.ForceUpdateProjection();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }
    }
}