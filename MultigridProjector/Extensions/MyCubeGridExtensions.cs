using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using VRage.Game;
using VRageMath;

namespace MultigridProjector.Extensions
{
    public static class MyCubeGridExtensions
    {
        private static readonly MethodInfo RayCastBlocksAllOrderedInfo = AccessTools.DeclaredMethod(typeof(MyCubeGrid), "RayCastBlocksAllOrdered");
        public static List<MyCube> RayCastBlocksAllOrdered(this MyCubeGrid obj, Vector3D worldStart, Vector3D worldEnd)
        {
            return RayCastBlocksAllOrderedInfo.Invoke(obj, new object[] {worldStart, worldEnd}) as List<MyCube>;
        }

        private static readonly MethodInfo AddGroupInfo = AccessTools.DeclaredMethod(typeof(MyCubeGrid), "AddGroup", new []{typeof(MyBlockGroup)});
        public static void AddGroup(this MyCubeGrid obj, MyBlockGroup group)
        {
            AddGroupInfo.Invoke(obj, new object[] {@group});
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MySlimBlock GetOverlappingBlock(this MyCubeGrid grid, MySlimBlock block)
        {
            var cubeIndex = grid.WorldToGridInteger(block.WorldPosition);
            return grid.GetCubeBlock(cubeIndex);
        }

        public static MyCubeSize GetOppositeCubeSize(MyCubeSize cubeSize)
        {
            return cubeSize == MyCubeSize.Large ? MyCubeSize.Small : MyCubeSize.Large;
        }
    }
}