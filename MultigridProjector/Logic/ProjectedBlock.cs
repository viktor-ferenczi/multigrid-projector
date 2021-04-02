using System.Runtime.CompilerServices;
using MultigridProjector.Api;
using MultigridProjector.Extensions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
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

        // Built block
        public MySlimBlock SlimBlock { get; private set; }

        // Preview block on the projected grid
        public readonly MySlimBlock Preview;

        // Preview block visual state
        private VisualState _visual = VisualState.None;

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
        public void DetectBlock(MyProjectorBase projector, MyCubeGrid builtGrid)
        {
            if (builtGrid == null)
            {
                if (!HasBuiltGrid)
                    return;

                SlimBlock = null;
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
                State = projector.CanBuild(Preview) ? BlockState.Buildable : BlockState.NotBuildable;
                return;
            }

            // Mismatching block?
            if (builtSlimBlock.BlockDefinition.Id != Preview.BlockDefinition.Id)
            {
                SlimBlock = null;
                State = BlockState.Mismatch;
                return;
            }

            // Register block, detect welding and grinding
            SlimBlock = builtSlimBlock;
            State = SlimBlock.Integrity >= Preview.Integrity ? BlockState.FullyBuilt : BlockState.BeingBuilt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateVisual(MyProjectorBase projector, bool showOnlyBuildable)
        {
            var visual = GetVisualState(showOnlyBuildable);
            if (visual == _visual)
                return;

            _visual = visual;

            switch (visual)
            {
                case VisualState.Hidden:
                    Hide(projector);
                    break;

                case VisualState.Buildable:
                    ShowBuildable(projector);
                    break;

                case VisualState.NotBuildable:
                    ShowNotBuildable(projector);
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
                    return showOnlyBuildable ? VisualState.Hidden : VisualState.NotBuildable;

                case BlockState.Buildable:
                    return VisualState.Buildable;

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
        private void ShowBuildable(MyProjectorBase projector)
        {
            projector.SetTransparency(Preview, -0.5f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShowNotBuildable(MyProjectorBase projector)
        {
            projector.SetTransparency(Preview, 0.5f);
        }
    }
}