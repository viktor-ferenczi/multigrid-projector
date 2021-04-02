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
    public static class MyMechanicalConnectionBlockBase_CreateTopPartAndAttach
    {
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyMechanicalConnectionBlockBase).GetMethod("CreateTopPartAndAttach", BindingFlags.DeclaredOnly | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(typeof (MyMechanicalConnectionBlockBase_CreateTopPartAndAttach).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
        
        [ServerOnly]
        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyMechanicalConnectionBlockBase __instance,
            long builtBy,
            bool smallToLarge,
            bool instantBuild)
        {
            var baseBlock = __instance;

            try
            {
                var baseGrid = baseBlock.CubeGrid;
                if (!MultigridProjection.TryFindProjectionByBuiltGrid(baseGrid, out var projection, out var subgrid))
                    return true;

                return projection.CreateTopPartAndAttach(subgrid, baseBlock);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
                return false;
            }
        }
    }
}