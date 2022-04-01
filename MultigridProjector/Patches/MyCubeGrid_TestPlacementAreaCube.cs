using System;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using VRageMath;

namespace MultigridProjector.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyCubeGrid))]
    [HarmonyPatch("TestPlacementAreaCube",
        new[] {
            typeof(MyCubeGrid),
            typeof(MyGridPlacementSettings),
            typeof(Vector3I),
            typeof(Vector3I),
            typeof(MyBlockOrientation),
            typeof(MyCubeBlockDefinition),
            typeof(MyCubeGrid),
            typeof(ulong),
            typeof(MyEntity),
            typeof(bool),
            typeof(bool),
            typeof(bool),
        },
        new []
        {
            ArgumentType.Normal,
            ArgumentType.Ref,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Out,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Normal,
        })]
    [EnsureOriginal("e93c5c27")]
    // ReSharper disable once InconsistentNaming
    public static class MyCubeGrid_TestPlacementAreaCube
    {
        [Everywhere]
        // ReSharper disable once UnusedMember.Global
        private static void Postfix(
            MyCubeGrid targetGrid,
            Vector3I min,
            bool isProjected,
            // ReSharper disable once InconsistentNaming
            ref bool __result)
        {
            if (__result)
                return;

            if (!isProjected)
                return;

            try
            {
                if (!MultigridProjection.TryFindProjectionByBuiltGrid(targetGrid, out var projection, out var subgrid))
                    return;

                __result = __result || projection.TestPlacementAreaCube(subgrid, targetGrid, min);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }
    }
}