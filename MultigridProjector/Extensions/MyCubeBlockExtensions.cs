using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using VRageMath;

namespace MultigridProjector.Extensions
{
    public static class MyCubeBlockExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasTheSameDefinition(this MySlimBlock block, MySlimBlock other)
        {
            return block.BlockDefinition.Id == other.BlockDefinition.Id;
        }

        private static readonly MethodInfo RecreateTopInfo = AccessTools.DeclaredMethod(typeof(MyMechanicalConnectionBlockBase), "RecreateTop");
        public static void RecreateTop(this MyMechanicalConnectionBlockBase stator, long? builderId = null, bool smallToLarge = false, bool instantBuild = false)
        {
            RecreateTopInfo.Invoke(stator, new object[] {builderId, smallToLarge, instantBuild});
        }

        // Aligns the grid of the block to a corresponding block on another grid
        public static void AlignGrid(this MyCubeBlock block, MyCubeBlock referenceBlock)
        {
            // Misaligned preview grid position and orientation
            var wm = block.CubeGrid.WorldMatrix;

            // Center the preview block
            wm.Translation -= block.WorldMatrix.Translation;

            // Reorient to match the built block
            wm *= MatrixD.Invert(block.PositionComp.GetOrientation());
            wm *= referenceBlock.PositionComp.GetOrientation();

            // Translate to the built block's position
            wm.Translation += referenceBlock.WorldMatrix.Translation;

            // Move the preview grid
            block.CubeGrid.PositionComp.SetWorldMatrix(ref wm, skipTeleportCheck: true);
        }
    }
}