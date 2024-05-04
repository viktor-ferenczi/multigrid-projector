using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Blocks;
using VRage;
using VRage.Game;
using VRageMath;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatBlockName(MyObjectBuilder_TerminalBlock terminalBlockBuilder)
        {
            if (terminalBlockBuilder.CustomName != null)
                return terminalBlockBuilder.CustomName;

            MyCubeBlockDefinition blockDefinition;
            var defaultName =
                MyDefinitionManager.Static?.TryGetDefinition(terminalBlockBuilder.SubtypeId,
                    out blockDefinition) == true
                    ? blockDefinition.DisplayNameText
                    : terminalBlockBuilder.SubtypeId.ToString();
            return terminalBlockBuilder.NumberInGrid > 1
                ? $"{defaultName} {terminalBlockBuilder.NumberInGrid}"
                : defaultName;
        }

        public static bool AlignToRepairProjector(this MyObjectBuilder_CubeGrid gridBuilder, MyProjectorBase projector)
        {
            // Find all projectors
            var projectorBuilders = gridBuilder
                .CubeBlocks
                .OfType<MyObjectBuilder_Projector>()
                .ToList();
            if (projectorBuilders.Count == 0)
                return false;

            // Select the repair projector (must be unambiguous)
            // 1. The projector which is the very first block is expected to be a repair one
            // 2. Projector with the exact same name if the name is unique
            // 3. The first projector with "Repair" in its name (case-insensitive)
            // In case of ambiguity do not align.
            var projectorBuilder = gridBuilder.CubeBlocks.First() as MyObjectBuilder_Projector;
            if (projectorBuilder == null && projector != null)
            {
                var projectorName = projector.CustomName.ToString();
                var projectorBuildersWithSameName = projectorBuilders
                    .Where(b => FormatBlockName(b) == projectorName)
                    .ToList();

                if (projectorBuildersWithSameName.Count == 1)
                {
                    projectorBuilder = projectorBuildersWithSameName.First();
                }
            }
            if (projectorBuilder == null)
            {
                projectorBuilder = projectorBuilders
                    .FirstOrDefault(b => FormatBlockName(b).ToLower().Contains("repair"));
            }
            if (projectorBuilder == null)
            {
                // The projector to use is ambiguous, so don't align
                return false;
            }

            // The preview grid is relative to the first block's position and orientation,
            // therefore aligning the blueprint means promoting the projector to be the first block
            if (gridBuilder.CubeBlocks[0].EntityId != projectorBuilder.EntityId)
            {
                gridBuilder.CubeBlocks.Remove(projectorBuilder);
                gridBuilder.CubeBlocks.Insert(0, projectorBuilder);
            }

            // Signal to the caller that the blueprint is aligned to a repair projector,
            // so it can zero out the offset and set the rotation of the projection
            return true;
        }

        public static void CensorWorldPosition(this MyObjectBuilder_CubeGrid gridBuilder)
        {
            if (gridBuilder?.PositionAndOrientation == null)
                return;

            gridBuilder.PositionAndOrientation = MyPositionAndOrientation.Default;
        }

        public static void CensorWorldPosition(this IReadOnlyCollection<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            if (gridBuilders == null || gridBuilders.Count == 0)
                return;

            var maybeMainGridPO = gridBuilders.First().PositionAndOrientation;
            if (!maybeMainGridPO.HasValue)
                return;

            var mainGridPosition = (Vector3D) maybeMainGridPO.Value.Position;

            foreach (var gridBuilder in gridBuilders)
            {
                if (!gridBuilder.PositionAndOrientation.HasValue)
                    continue;

                var gridPO = gridBuilder.PositionAndOrientation.Value;
                gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(gridPO.Position - mainGridPosition, gridPO.Forward, gridPO.Up);
            }
        }
    }
}