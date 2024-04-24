using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using VRageMath;

namespace MultigridProjector.Extensions
{
    public static class MyCubeGridExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetSafeName(this MyCubeGrid grid)
        {
            return grid?.DisplayNameText ?? grid?.DisplayName ?? grid?.Name ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDebugName(this MyCubeGrid grid)
        {
            return $"{grid.GetSafeName()} [{grid.EntityId}]";
        }

        private static readonly MethodInfo RayCastBlocksAllOrderedInfo = Validation.EnsureInfo(AccessTools.DeclaredMethod(typeof(MyCubeGrid), "RayCastBlocksAllOrdered"));
        public static List<MyCube> RayCastBlocksAllOrdered(this MyCubeGrid obj, Vector3D worldStart, Vector3D worldEnd)
        {
            return RayCastBlocksAllOrderedInfo.Invoke(obj, new object[] {worldStart, worldEnd}) as List<MyCube>;
        }

        private static readonly FieldInfo BlockGroupsInfo = Validation.EnsureInfo(AccessTools.Field(typeof(MyCubeGrid), "BlockGroups"));
        public static List<MyBlockGroup> GetBlockGroups(this MyCubeGrid grid)
        {
            return (List<MyBlockGroup>)BlockGroupsInfo.GetValue(grid);
        }

        private static readonly MethodInfo AddGroupInfo = Validation.EnsureInfo(AccessTools.DeclaredMethod(typeof(MyCubeGrid), "AddGroup", new []{typeof(MyBlockGroup), typeof(bool)}));
        public static void AddGroup(this MyCubeGrid obj, MyBlockGroup group, bool unionSameNameGroups = true)
        {
            AddGroupInfo.Invoke(obj, new object[] {group, unionSameNameGroups});
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MySlimBlock GetOverlappingBlock(this MyCubeGrid grid, MySlimBlock block)
        {
            var cubeIndex = grid.WorldToGridInteger(block.WorldPosition);
            return grid.GetCubeBlock(cubeIndex);
        }
    }
}