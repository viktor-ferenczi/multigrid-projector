using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using VRageMath;

namespace MultigridProjector.Api
{
    /* If MGP is not available, then you can instantiate this shim for each
     * projector to provide projected block information similar to MGP for
     * single grid use. It allows your mod to work without MGP without having
     * to duplicate much code. Please note that this class is IDisposable and
     * supposed to be disposed once the projector is closed or the information
     * is not required anymore. It is a difference from the mod agent which
     * needs only a single instance per mod to handle all projectors. The
     * projectorId passed to the shim methods are ignored and the only valie
     * subgridIndex is zero. The shim provides reasonable emulation of MGP's
     * grid scan behavior and provides a change sequence number instead of the
     * grid state hash. It will allow your optimizations to work reasonably.
     */

    // ReSharper disable once UnusedType.Global
    public class MultigridProjectorModShim : IMultigridProjectorApi, IDisposable
    {
        private readonly IMyProjector projector;
        private readonly double scanCooldownSeconds;

        // Restarting the projector is detected by the replacement of the projected grid
        private long previewGridEntityId;

        // Grid builder (blueprint) cache (null or a single item list)
        private List<MyObjectBuilder_CubeGrid> gridBuilders;

        // Emulated grid scans, so code relying on scan numbers will still work
        private int scanNumber;
        private bool requestScan;
        private double lastScanTime;

        // Counting grid and block change events to emulate block state hash
        private ulong changeNumber;

        // Stores the last known projector setting for change detection
        private Vector3I projectionOffset;
        private Vector3I projectionRotation;

        public string Version => "0.7.9";

        public int GetSubgridCount(long projectorId) => projector.IsProjecting ? 1 : 0;

        public List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId)
        {
            if (!projector.IsProjecting)
                return null;

            if (gridBuilders == null)
            {
                var gridBuilder = (MyObjectBuilder_CubeGrid) projector.ProjectedGrid.GetObjectBuilder();
                gridBuilders = new List<MyObjectBuilder_CubeGrid> { gridBuilder };
            }

            return gridBuilders;
        }

        public IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
        {
            if (!projector.IsProjecting || subgridIndex != 0)
                return null;

            return projector.ProjectedGrid;
        }

        public IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
        {
            if (!projector.IsProjecting || subgridIndex != 0)
                return null;

            return projector.CubeGrid;
        }

        public BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
        {
            if (!projector.IsProjecting || subgridIndex != 0)
                return BlockState.Unknown;

            var previewGrid = projector.ProjectedGrid;
            var previewBlock = previewGrid.GetCubeBlock(position);

            var builtGrid = projector.CubeGrid;
            var builtBlockPosition = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(position));
            var builtBlock = builtGrid.GetCubeBlock(builtBlockPosition);

            if (builtBlock == null)
                return projector.CanBuild(previewBlock, true) == BuildCheckResult.OK ? BlockState.Buildable : BlockState.NotBuildable;

            if (builtBlock.BlockDefinition.Id != previewBlock.BlockDefinition.Id)
                return BlockState.Mismatch;

            return builtBlock.Integrity < previewBlock.Integrity ? BlockState.BeingBuilt : BlockState.FullyBuilt;
        }

        public bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask)
        {
            if (!projector.IsProjecting || subgridIndex != 0)
                return false;

            var previewGrid = projector.ProjectedGrid;
            var previewBlocks = new List<IMySlimBlock>();

            if (box.Min == Vector3I.MinValue && box.Max == Vector3I.MaxValue)
            {
                previewGrid.GetBlocks(previewBlocks);
            }
            else
            {
                previewGrid.GetBlocks(previewBlocks, block => box.Contains(block.Position) == ContainmentType.Contains);
            }

            var builtGrid = projector.CubeGrid;
            foreach (var previewBlock in previewBlocks)
            {
                var previewBlockPosition = previewBlock.Position;
                var builtBlockPosition = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(previewBlockPosition));
                var builtBlock = builtGrid.GetCubeBlock(builtBlockPosition);

                if (builtBlock == null)
                {
                    blockStates[previewBlockPosition] = projector.CanBuild(previewBlock, true) == BuildCheckResult.OK ? BlockState.Buildable : BlockState.NotBuildable;
                    continue;
                }

                if (builtBlock.BlockDefinition.Id != previewBlock.BlockDefinition.Id)
                {
                    blockStates[previewBlockPosition] = BlockState.Mismatch;
                    continue;
                }

                blockStates[previewBlockPosition] = builtBlock.Integrity < previewBlock.Integrity ? BlockState.BeingBuilt : BlockState.FullyBuilt;
            }

            return true;
        }

        public Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex)
        {
            if (!projector.IsProjecting || subgridIndex != 0)
                return null;

            return new Dictionary<Vector3I, BlockLocation>();
        }

        public Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex)
        {
            if (!projector.IsProjecting || subgridIndex != 0)
                return null;

            return new Dictionary<Vector3I, BlockLocation>();
        }

        public long GetScanNumber(long projectorId)
        {
            if (!projector.IsProjecting)
                return 0;

            DetectProjectorRestart();
            DetectProjectionConfigChange();

            var now = MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds;
            if (requestScan && now >= lastScanTime + scanCooldownSeconds)
            {
                scanNumber++;
                requestScan = false;
                lastScanTime = now;
            }

            return scanNumber;
        }

        public string GetYaml(long projectorId) => "Not implemented in the shim, since it is used only for debugging";

        // Emulated by counting grid changes (not a real hash), it is designed to unblock optimizations relying on hash change
        public ulong GetStateHash(long projectorId, int subgridIndex)
        {
            if (!projector.IsProjecting)
                return 0;

            if (subgridIndex != 0)
                return 0;

            DetectProjectorRestart();
            DetectProjectionConfigChange();

            return changeNumber;
        }

        public bool IsSubgridComplete(long projectorId, int subgridIndex)
        {
            if (!projector.IsProjecting || subgridIndex != 0)
                return false;

            return projector.TotalBlocks != 0 && projector.RemainingBlocks == 0;
        }

        public MultigridProjectorModShim(IMyProjector projector, double scanCooldownSeconds = 2.0)
        {
            this.projector = projector;
            this.scanCooldownSeconds = scanCooldownSeconds;

            var builtGrid = projector.CubeGrid;
            builtGrid.OnBlockIntegrityChanged += OnBlockIntegrityChanged;
            builtGrid.OnBlockAdded += OnBlockAdded;
            builtGrid.OnBlockRemoved += OnBlockRemoved;
            builtGrid.OnGridSplit += OnGridSplitOrMerge;
            builtGrid.OnGridMerge += OnGridSplitOrMerge;
        }

        public void Dispose()
        {
            var builtGrid = projector.CubeGrid;
            builtGrid.OnBlockIntegrityChanged -= OnBlockIntegrityChanged;
            builtGrid.OnBlockAdded -= OnBlockAdded;
            builtGrid.OnBlockRemoved -= OnBlockRemoved;
            builtGrid.OnGridSplit -= OnGridSplitOrMerge;
            builtGrid.OnGridMerge -= OnGridSplitOrMerge;
        }

        private void OnBlockIntegrityChanged(IMySlimBlock builtBlock)
        {
            var previewGrid = projector.CubeGrid;
            var previewBlock = previewGrid.GetCubeBlock(previewGrid.WorldToGridInteger(builtBlock.CubeGrid.GridIntegerToWorld(builtBlock.Position)));
            if (previewBlock == null)
                return;

            if (builtBlock.BlockDefinition.Id != previewBlock.BlockDefinition.Id)
                return;

            var hasJustFullyBuilt = builtBlock.Integrity >= previewBlock.Integrity;
            var hasJustDamaged = builtBlock.Integrity < previewBlock.Integrity && (builtBlock.HasDeformation || builtBlock.DamageRatio > 1e-3);
            if (hasJustFullyBuilt || hasJustDamaged)
            {
                RegisterChange();
            }
        }

        private void OnBlockAdded(IMySlimBlock slimBlock)
        {
            if (!projector.IsProjecting)
                return;

            RegisterChange();
        }

        private void OnBlockRemoved(IMySlimBlock slimBlock)
        {
            if (!projector.IsProjecting)
                return;

            RegisterChange();
        }

        private void OnGridSplitOrMerge(IMyCubeGrid grid1, IMyCubeGrid grid2)
        {
            if (!projector.IsProjecting)
                return;

            RegisterChange();
        }

        private void DetectProjectionConfigChange()
        {
            if (!projector.IsProjecting)
                return;

            // ReSharper disable once EqualExpressionComparison
            if (projectionOffset == projector.ProjectionOffset && projectionRotation == projector.ProjectionRotation)
                return;

            projectionOffset = projector.ProjectionOffset;
            projectionRotation = projector.ProjectionRotation;

            RegisterChange();
        }

        private void RegisterChange()
        {
            changeNumber++;
            requestScan = true;
        }

        private void DetectProjectorRestart()
        {
            if (previewGridEntityId == projector.ProjectedGrid.EntityId)
                return;

            Reset();
        }

        private void Reset()
        {
            previewGridEntityId = projector.ProjectedGrid.EntityId;
            gridBuilders = null;

            scanNumber = 0;
            requestScan = true;
            lastScanTime = double.NegativeInfinity;

            changeNumber = 0;

            projectionOffset = projector.ProjectionOffset;
            projectionRotation = projector.ProjectionRotation;
        }
    }
}