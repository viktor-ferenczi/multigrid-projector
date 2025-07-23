using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Blocks;
using VRage;
using VRage.Game;
using VRage.Game.ObjectBuilders.ComponentSystem;
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
                case MyObjectBuilder_AirVent b:
                    // FIXME: Not handled, MyAirVent does not have a properly serialized toolbar
                    break;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatBlockName(MyObjectBuilder_TerminalBlock terminalBlockBuilder)
        {
            if (terminalBlockBuilder.CustomName != null)
                return terminalBlockBuilder.CustomName;

            string defaultName;
            MyCubeBlockDefinition blockDefinition = null;
            if (MyDefinitionManager.Static != null &&
                MyDefinitionManager.Static.TryGetDefinition(terminalBlockBuilder.SubtypeId, out blockDefinition))
            {
                defaultName = blockDefinition.DisplayNameText;
            }
            else
            {
                defaultName = terminalBlockBuilder.SubtypeId.ToString();
            }

            return terminalBlockBuilder.NumberInGrid > 1
                ? $"{defaultName} {terminalBlockBuilder.NumberInGrid}"
                : defaultName;
        }

        public static bool AlignToRepairProjector(this MyObjectBuilder_CubeGrid gridBuilder, MyProjectorBase projector)
        {
            var projectorBuilder = FindMatchingProjectorInBlueprint(gridBuilder, projector);
            if (projectorBuilder == null)
                return false;

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

        private static MyObjectBuilder_Projector FindMatchingProjectorInBlueprint(MyObjectBuilder_CubeGrid gridBuilder, MyProjectorBase projector)
        {
            // Find all projectors in the blueprint
            var projectorBuilders = gridBuilder
                .CubeBlocks
                .OfType<MyObjectBuilder_Projector>()
                .ToList();
            if (projectorBuilders.Count == 0)
                return null;

            // Select the repair projector (must be unambiguous)
            MyObjectBuilder_Projector projectorBuilder;
            if (projector != null)
            {
                var existingProjectorBuilder = (MyObjectBuilder_TerminalBlock)projector.GetObjectBuilderCubeBlock();
                var existingProjectorName = FormatBlockName(existingProjectorBuilder);
                var projectorBuildersWithSameName = projectorBuilders
                    .Where(b => FormatBlockName(b) == existingProjectorName)
                    .ToList();

                // 1. Projector in the blueprint with the exact same name, position and orientation as the existing projector
                // (Load Repair Projection was used or original repair projector)
                projectorBuilder = projectorBuildersWithSameName
                    .FirstOrDefault(b =>
                        b.Min == existingProjectorBuilder.Min &&
                        b.BlockOrientation == existingProjectorBuilder.BlockOrientation);
                if (projectorBuilder != null)
                    return projectorBuilder;

                // 2. Projector in the blueprint with the exact same name as the existing projector,
                // but only if the name is unique in the blueprint (repair projector rebuilt by hand)
                if (projectorBuildersWithSameName.Count == 1)
                {
                    return projectorBuildersWithSameName.First();
                }
            }

            // 3. The projector in the blueprint which is the very first block (standardized blueprints which always load with default alignment)
            projectorBuilder = gridBuilder.CubeBlocks.FirstOrDefault() as MyObjectBuilder_Projector;
            if (projectorBuilder != null)
            {
                return projectorBuilder;
            }

            // 4. The first projector in the blueprint with "Repair" in its name, case-insensitive (best guess for any random blueprint)
            projectorBuilder = projectorBuilders
                .FirstOrDefault(b => FormatBlockName(b).ToLower().Contains("repair"));

            return projectorBuilder;
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

            var mainGridPosition = (Vector3D)maybeMainGridPO.Value.Position;

            foreach (var gridBuilder in gridBuilders)
            {
                if (!gridBuilder.PositionAndOrientation.HasValue)
                    continue;

                var gridPO = gridBuilder.PositionAndOrientation.Value;
                gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(gridPO.Position - mainGridPosition, gridPO.Forward, gridPO.Up);
            }
        }

        public static bool TryGet<T>(this MyObjectBuilder_ComponentContainer componentContainer, out T component) where T : MyObjectBuilder_ComponentBase
        {
            component = componentContainer.Components
                .Select(c => c.Component as T)
                .FirstOrDefault(c => c != null);
            return component != null;
        }
    }
}