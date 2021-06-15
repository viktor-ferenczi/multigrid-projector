using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRageMath;

namespace MultigridProjectorPrograms.RobotArm
{
    public class Subgrid
    {
        private static readonly BoundingBoxI MaxBox = new BoundingBoxI(Vector3I.MinValue, Vector3I.MaxValue);

        public readonly int Index;
        private readonly long projectorEntityId;
        private readonly MultigridProjectorProgrammableBlockAgent mgp;
        private readonly Dictionary<Vector3I, BlockState> BlockStates = new Dictionary<Vector3I, BlockState>();
        private readonly Dictionary<Vector3I, int> LayeredBlockPositions = new Dictionary<Vector3I, int>();
        private readonly List<int> LayerBlockCounts = new List<int>();
        private ulong latestStateHash;
        private MyCubeSize gridSize;
        public bool HasBuilt { get; private set; }
        public bool HasFinished { get; private set; }
        public int LayerIndex => LayerBlockCounts.Count;
        public int WeldedLayer { get; private set; }
        public bool IsValidLayer => WeldedLayer < LayerBlockCounts.Count;
        public int WeldedLayerBlockCount => LayerBlockCounts[WeldedLayer];
        public int BuildableBlockCount => LayeredBlockPositions.Count;

        public Subgrid(long projectorEntityId, MultigridProjectorProgrammableBlockAgent mgp, int index)
        {
            Index = index;
            this.projectorEntityId = projectorEntityId;
            this.mgp = mgp;

            var previewGrid = mgp.GetPreviewGrid(projectorEntityId, index);
            gridSize = previewGrid.GridSizeEnum;
        }

        public bool Update()
        {
            var stateHash = mgp.GetStateHash(projectorEntityId, Index);
            if (stateHash == latestStateHash)
            {
                if (BlockStates.Count > 0)
                    return false;

                if (mgp.IsSubgridComplete(projectorEntityId, Index))
                {
                    HasBuilt = true;
                    HasFinished = true;
                    return false;
                }
            }

            latestStateHash = stateHash;

            BlockStates.Clear();
            mgp.GetBlockStates(BlockStates, projectorEntityId, Index, MaxBox, (int) BlockState.Buildable | (int) BlockState.BeingBuilt);

            // Remove already built blocks (allocates memory, but it is hard to avoid here)
            var blocksToRemove = LayeredBlockPositions.Keys.Where(position => !BlockStates.ContainsKey(position)).ToList();
            foreach (var position in blocksToRemove)
            {
                LayerBlockCounts[LayeredBlockPositions[position]]--;
                LayeredBlockPositions.Remove(position);
            }

            // Store the new layer if any
            if (BlockStates.Count > 0)
            {
                HasBuilt = true;
                HasFinished = false;

                var blockCountBefore = LayeredBlockPositions.Count;

                foreach (var position in BlockStates.Keys)
                    if (!LayeredBlockPositions.ContainsKey(position))
                        LayeredBlockPositions[position] = LayerIndex;

                var layerBlockCount = LayeredBlockPositions.Count - blockCountBefore;
                if (layerBlockCount > 0)
                    LayerBlockCounts.Add(layerBlockCount);
            }
            else if (!HasFinished && mgp.IsSubgridComplete(projectorEntityId, Index))
            {
                HasFinished = true;
            }

            return true;
        }

        public int CountWeldableBlocks(out int lastLayerToWeld)
        {
            // Skip fully welded layers
            while (IsValidLayer && WeldedLayerBlockCount == 0)
                WeldedLayer++;

            // Find weldable layers
            lastLayerToWeld = WeldedLayer;
            var blockCount = LayerBlockCounts[lastLayerToWeld];
            var maxBlocksToWeld = gridSize == MyCubeSize.Large ? Cfg.MaxLargeBlocksToWeld : Cfg.MaxSmallBlocksToWeld;
            while (lastLayerToWeld + 1 < LayerBlockCounts.Count && blockCount + LayerBlockCounts[lastLayerToWeld + 1] <= maxBlocksToWeld)
                blockCount += LayerBlockCounts[lastLayerToWeld++];

            return blockCount;
        }

        public IEnumerable<Vector3I> IterWeldableBlockPositions()
        {
            int lastLayerToWeld;
            CountWeldableBlocks(out lastLayerToWeld);
            foreach (var position in LayeredBlockPositions.Where(p => p.Value <= lastLayerToWeld).Select(p => p.Key))
                yield return position;
        }

        public void Remove(Vector3I position)
        {
            BlockStates.Remove(position);
        }
    }
}