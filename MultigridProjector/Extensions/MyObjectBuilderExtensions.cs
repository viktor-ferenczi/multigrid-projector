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
                RemoveNestedRepairBlueprints(projectorBuilder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RemoveNestedRepairBlueprints(MyObjectBuilder_ProjectorBase projectorBuilder)
        {
            var firstGridBuilder = projectorBuilder.ProjectedGrids?.FirstOrDefault();
            if (firstGridBuilder == null)
                return;

            foreach (var nestedProjectorBuilder in firstGridBuilder.CubeBlocks.OfType<MyObjectBuilder_ProjectorBase>())
            {
                // Repair projector?
                if (nestedProjectorBuilder.SubtypeId != projectorBuilder.SubtypeId ||
                    nestedProjectorBuilder.Name != projectorBuilder.Name ||
                    nestedProjectorBuilder.CustomName != projectorBuilder.CustomName)
                    continue;

                // Clear out the nested blueprint
                nestedProjectorBuilder.ProjectedGrid = null;
                nestedProjectorBuilder.ProjectedGrids = null;
            }
        }
    }
}