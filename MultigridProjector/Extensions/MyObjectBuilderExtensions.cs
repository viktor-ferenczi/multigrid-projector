using System.Linq;
using System.Runtime.CompilerServices;
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

            // We need to keep the EntityId value to map mechanical connections.
            // Initial remapping and BuildInternal both avoid EntityID collisions.

            if(blockBuilder is MyObjectBuilder_ProjectorBase projectorBuilder)
                RemoveBlueprintFromSelfRepairProjector(projectorBuilder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RemoveBlueprintFromSelfRepairProjector(MyObjectBuilder_ProjectorBase projectorBuilder)
        {
            var firstGridBuilder = projectorBuilder.ProjectedGrids?.FirstOrDefault();
            if (firstGridBuilder == null)
                return;

            foreach (var projectedProjectorBuilder in firstGridBuilder.CubeBlocks.OfType<MyObjectBuilder_ProjectorBase>())
            {
                // Projection of the repair projector itself?
                if (projectedProjectorBuilder.SubtypeId != projectorBuilder.SubtypeId ||
                    projectedProjectorBuilder.CustomName != projectorBuilder.CustomName)
                    continue;

                // Clear out the nested self-repair projection
                projectorBuilder.ProjectedGrid = null;
                projectorBuilder.ProjectedGrids = null;
                break;
            }
        }
    }
}