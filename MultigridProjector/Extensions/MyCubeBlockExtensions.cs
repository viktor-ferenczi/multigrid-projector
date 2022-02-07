using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRageMath;

namespace MultigridProjector.Extensions
{
    public static class MyCubeBlockExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetSafeName(this MyCubeBlock block)
        {
            return block?.DisplayNameText ?? block?.DisplayName ?? block?.Name ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDebugName(this MyCubeBlock block)
        {
            return $"{block.GetSafeName()} [{block.EntityId}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasSameDefinition(this MySlimBlock block, MySlimBlock other)
        {
            return block.BlockDefinition.Id == other.BlockDefinition.Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatchingBuilder(this MySlimBlock previewBlock, MyObjectBuilder_CubeBlock blockBuilder)
        {
            return previewBlock.BlockDefinition.Id == blockBuilder.GetId() && 
                   previewBlock.Orientation.Forward == blockBuilder.BlockOrientation.Forward && 
                   previewBlock.Orientation.Up == blockBuilder.BlockOrientation.Up;
        }
        
        private static readonly MethodInfo RecreateTopInfo = Validation.EnsureInfo(AccessTools.DeclaredMethod(typeof(MyMechanicalConnectionBlockBase), "RecreateTop"));
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