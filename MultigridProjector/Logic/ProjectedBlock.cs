using System.Runtime.CompilerServices;
using MultigridProjector.Api;
using MultigridProjector.Extensions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;

namespace MultigridProjector.Logic
{
    public class ProjectedBlock
    {
        // Original block builder (do not modify it, always clone first)
        public readonly MyObjectBuilder_CubeBlock Builder;

        // Welding state
        public BlockState State { get; private set; } = BlockState.Unknown;
        public BuildCheckResult BuildCheckResult { get; private set; } = BuildCheckResult.NotFound;

        // Built block
        public MySlimBlock SlimBlock { get; private set; }

        // Preview block on the projected grid
        public readonly MySlimBlock Preview;

        // Preview block visual state
        private VisualState latestVisual = VisualState.None;

        // Position of the block on the built grid (not valid for the preview grid)
        private Vector3I BuiltPosition { get; set; } = Vector3I.MinValue;
        private bool HasBuiltGrid => BuiltPosition.X != int.MinValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProjectedBlock(MySlimBlock preview, MyObjectBuilder_CubeBlock builder)
        {
            Preview = preview;
            Builder = builder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            SlimBlock = null;
            BuildCheckResult = BuildCheckResult.NotFound;
            State = BlockState.Unknown;
            BuiltPosition = Vector3I.MinValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DetectBlock(MyProjectorBase projector, MyCubeGrid builtGrid, bool checkHavokIntersections)
        {
            if (builtGrid == null)
            {
                SlimBlock = null;
                BuildCheckResult = BuildCheckResult.NotConnected;
                State = BlockState.NotBuildable;
                BuiltPosition = Vector3I.MinValue;
                return;
            }

            // Calculate the position on the built grid only once
            if (!HasBuiltGrid)
                BuiltPosition = builtGrid.WorldToGridInteger(Preview.WorldPosition);

            // Try to find the block on the built grid
            var builtSlimBlock = builtGrid.GetCubeBlock(BuiltPosition);

            // Missing block?
            if (builtSlimBlock == null)
            {
                SlimBlock = null;
                BuildCheckResult = projector.CanBuild(Preview, checkHavokIntersections);
                State = BuildCheckResult == BuildCheckResult.OK ? BlockState.Buildable : BlockState.NotBuildable;
                return;
            }

            // Mismatching block?
            if (builtSlimBlock.BlockDefinition.Id != Preview.BlockDefinition.Id)
            {
                SlimBlock = null;
                BuildCheckResult = BuildCheckResult.IntersectedWithGrid;
                State = BlockState.Mismatch;
                return;
            }

            // Register block, detect welding and grinding
            SlimBlock = builtSlimBlock;
            BuildCheckResult = BuildCheckResult.AlreadyBuilt;
            State = SlimBlock.Integrity >= Preview.Integrity ? BlockState.FullyBuilt : BlockState.BeingBuilt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateVisual(MyProjectorBase projector, bool showOnlyBuildable)
        {
            var visual = GetVisualState(showOnlyBuildable);
            if (visual == latestVisual)
                return;

            latestVisual = visual;

            switch (visual)
            {
                case VisualState.Hidden:
                    Hide(projector);
                    break;

                case VisualState.Hologram:
                    ShowHologram(projector);
                    break;

                case VisualState.Transparent:
                    ShowTransparent(projector);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private VisualState GetVisualState(bool showOnlyBuildable)
        {
            switch (State)
            {
                case BlockState.NotBuildable:
                case BlockState.Mismatch:
                    return showOnlyBuildable ? VisualState.Hidden : VisualState.Hologram;

                case BlockState.Buildable:
                    return VisualState.Transparent;

                case BlockState.BeingBuilt:
                case BlockState.FullyBuilt:
                    return VisualState.Hidden;
            }

            return VisualState.Hidden;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Hide(MyProjectorBase projector)
        {
            projector.SetTransparency(Preview, 1f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShowHologram(MyProjectorBase projector)
        {
            projector.SetTransparency(Preview, 0.5f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShowTransparent(MyProjectorBase projector)
        {
            projector.SetTransparency(Preview, 0.25f);
        }
    }
}