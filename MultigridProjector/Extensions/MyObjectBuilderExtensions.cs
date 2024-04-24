using System.Linq;
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

            // We need to keep the EntityId value to map mechanical connections.
            // Initial remapping and BuildInternal both avoid EntityID collisions.

            // Remove nested blueprints from projectors, it still allows for repair projections and missile welders
            if (blockBuilder is MyObjectBuilder_ProjectorBase projectorBuilder)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MyObjectBuilder_Toolbar GetToolbar(this MyObjectBuilder_TerminalBlock block)
        {
            switch (block)
            {
                case MyObjectBuilder_SensorBlock b:
                    return b.Toolbar;
                case MyObjectBuilder_ButtonPanel b:
                    return b.Toolbar;
                case MyObjectBuilder_EventControllerBlock b:
                    return b.Toolbar;
                case MyObjectBuilder_ShipController b:
                    return b.Toolbar;
                case MyObjectBuilder_TimerBlock b:
                    return b.Toolbar;
            }

            return null;
        }
    }
}