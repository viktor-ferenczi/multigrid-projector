using System.Runtime.CompilerServices;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;

namespace MultigridProjector.Extensions
{
    public static class MyObjectBuilderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrepareForProjection(this MyObjectBuilder_CubeGrid gridBuilder)
        {
            gridBuilder.IsStatic = false;
            gridBuilder.DestructibleBlocks = false;

            foreach (var blockBuilder in gridBuilder.CubeBlocks)
                blockBuilder.PrepareForProjection();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrepareForProjection(this MyObjectBuilder_CubeBlock blockBuilder)
        {
            blockBuilder.Owner = 0L;
            blockBuilder.ShareMode = MyOwnershipShareModeEnum.None;

            // We need to keep the EntityId to map mechanical connections
            // blockBuilder.EntityId = 0L;

            // We do not turn off functional blocks, so they can start working right after being welded
            // if (blockBuilder is MyObjectBuilder_FunctionalBlock functionalBlockBuilder)
            //     functionalBlockBuilder.Enabled = false;

            // FIXME: Remove nested projections above a certain depth (like 2). Useful for repair projectors, prevents DoS attack on servers.
            // FIXME: Consider disabling auto-lock on landing legs, would be useful for printer walls. Make it configurable.
        }
    }
}