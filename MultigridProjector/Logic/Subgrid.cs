using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MultigridProjector.Api;
using MultigridProjector.Utilities;
using MultigridProjector.Extensions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace MultigridProjector.Logic
{
    public class Subgrid : IDisposable
    {
        // Index of the subgrid in the blueprint, also indexes the preview grid list
        public readonly int Index;

        // Grid builder
        public readonly MyObjectBuilder_CubeGrid GridBuilder;

        // Preview grid from the clipboard
        public readonly MyCubeGrid PreviewGrid;

        // Grid if already built
        public MyCubeGrid BuiltGrid { get; private set; }
        public readonly RwLock BuiltGridLock = new RwLock();
        public bool HasBuilt => BuiltGrid != null;
        public MyCubeSize GridSizeEnum => PreviewGrid.GridSizeEnum;

        // Initial statistics of the subgrid with none of the blocks welded
        public readonly ProjectionStats InitialStats = new ProjectionStats();

        // Latest welding state statistics collected by the background worker
        public ProjectionStats Stats { get; private set; } = new ProjectionStats();

        // Welding state statistics being collected by the background worker, then swapped with Stats
        private ProjectionStats stats = new ProjectionStats();

        // Indicates whether the built grid is connected to the projector
        public bool IsConnectedToProjector;

        // Mechanical base blocks on this subgrid by cube position
        public readonly Dictionary<Vector3I, BaseConnection> BaseConnections = new Dictionary<Vector3I, BaseConnection>();

        // Mechanical top blocks on this subgrid by cube position
        public readonly Dictionary<Vector3I, TopConnection> TopConnections = new Dictionary<Vector3I, TopConnection>();

        // Requests rescanning the preview blocks
        public bool IsUpdateRequested;

        // Indicates whether the preview grid is supported for welding, e.g. connected to the first preview grid
        public bool Supported;

        // Projected and built block states, built block changes are detected by the background worker, visuals are updated by the main thread
        public Dictionary<Vector3I, ProjectedBlock> Blocks;

        // Indicates that the preview grid has been positioned correctly during an update
        public bool Positioned;

        // Block state hash
        public ulong StateHash { get; private set; }

        // Indicates whether an unsupported preview grid has already been hidden
        private bool hidden;

        #region Initialization and disposal

        public Subgrid(MultigridProjection projection, int index)
        {
            Index = index;

            GridBuilder = projection.GridBuilders[index];
            PreviewGrid = projection.PreviewGrids[index];

            DisableFunctionalBlocks();
            CreateBlockModels();
            CollectInitialStats();
            FindMechanicalConnections(projection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DisableFunctionalBlocks()
        {
            // Disable all functional blocks in the preview to avoid side effects, prevents ghost subgrids from projectors
            foreach (var functionalBlock in PreviewGrid.GetFatBlocks<MyFunctionalBlock>())
                functionalBlock.Enabled = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateBlockModels()
        {
            var blockBuilders = GridBuilder
                .CubeBlocks
                .ToDictionary(bb => (Vector3I) bb.Min);

            Blocks = PreviewGrid
                .CubeBlocks
                .Where(previewBlock => blockBuilders.ContainsKey(previewBlock.Min))
                .ToDictionary(
                    previewBlock => previewBlock.Position,
                    previewBlock => new ProjectedBlock(previewBlock, blockBuilders[previewBlock.Min]));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CollectInitialStats()
        {
            foreach (var slimBlock in PreviewGrid.CubeBlocks)
                InitialStats.RegisterBlock(slimBlock, BlockState.NotBuildable);

            Stats.Add(InitialStats);
        }

        public void Dispose()
        {
            BaseConnections.Clear();
            TopConnections.Clear();

            UnregisterBuiltGrid();

            Blocks.Clear();
        }

        private void FindMechanicalConnections(MultigridProjection projection)
        {
            foreach (var slimBlock in PreviewGrid.CubeBlocks)
                switch (slimBlock.FatBlock)
                {
                    case MyMechanicalConnectionBlockBase baseBlock:
                        PrepareBase(projection, slimBlock, baseBlock);
                        break;
                    case MyAttachableTopBlockBase topBlock:
                        PrepareTop(projection, slimBlock, topBlock);
                        break;
                }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PrepareBase(MultigridProjection projection, MySlimBlock slimBlock, MyMechanicalConnectionBlockBase baseBlock)
        {
            var baseMinLocation = new BlockMinLocation(Index, baseBlock.Min);
            if (!projection.BlueprintConnections.TryGetValue(baseMinLocation, out var topMinLocation))
                // It happens if the connection is detached
                return;
            if (!projection.PreviewTopBlocks.TryGetValue(topMinLocation, out var topBlock))
                // It happens if the other part was removed due to removal of unknown modded blocks on blueprint load
                return;
            if (!projection.PreviewBaseBlocks.ContainsKey(baseMinLocation))
                // Make sure the base part also presents in the preview
                return;
            BaseConnections[slimBlock.Position] = new BaseConnection(baseBlock, new BlockLocation(topMinLocation.GridIndex, topBlock.Position));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PrepareTop(MultigridProjection projection, MySlimBlock slimBlock, MyAttachableTopBlockBase topBlock)
        {
            var topMinLocation = new BlockMinLocation(Index, topBlock.Min);
            if (!projection.BlueprintConnections.TryGetValue(topMinLocation, out var baseMinLocation))
                // It happens if the connection is detached
                return;
            if (!projection.PreviewBaseBlocks.TryGetValue(baseMinLocation, out var baseBlock))
                // It happens if the other part was removed due to removal of unknown modded blocks on blueprint load
                return;
            if (!projection.PreviewTopBlocks.ContainsKey(topMinLocation))
                // Make sure the top part also presents in the preview
                return;
            TopConnections[slimBlock.Position] = new TopConnection(topBlock, new BlockLocation(baseMinLocation.GridIndex, baseBlock.Position));
        }

        #endregion

        #region Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetProjectedBlock(Vector3I previewPosition, out ProjectedBlock projectedBlock)
        {
            return Blocks.TryGetValue(previewPosition, out projectedBlock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBlockBuilder(Vector3I previewPosition, out MyObjectBuilder_CubeBlock blockBuilder)
        {
            if (!TryGetProjectedBlock(previewPosition, out var projectedBlock))
            {
                blockBuilder = null;
                return false;
            }

            blockBuilder = projectedBlock.Builder;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBlockState(Vector3I previewPosition, out BlockState blockState)
        {
            if (!TryGetProjectedBlock(previewPosition, out var projectedBlock))
            {
                blockState = BlockState.Unknown;
                return false;
            }

            blockState = projectedBlock.State;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBuildableBlockAtPosition(Vector3I position)
        {
            using (BuiltGridLock.Read())
            {
                return HasBuilt && TryGetBlockState(position, out var blockState) && blockState == BlockState.Buildable;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RequestUpdate()
        {
            IsUpdateRequested = Supported;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(Vector3I, BlockState)> IterBlockStates(BoundingBoxI box, int mask)
        {
            foreach (var (position, projectedBlock) in Blocks)
            {
                var blockState = projectedBlock.State;
                if (((int) blockState & mask) == 0)
                    continue;

                if (box.Contains(position) == ContainmentType.Contains)
                    yield return (position, blockState);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetBlockOrientationQuaternion(MySlimBlock previewBlock, out Quaternion orientationQuaternion)
        {
            // Orientation of the preview grid relative to the built grid
            var wm = PreviewGrid.WorldMatrix.GetOrientation();
            using (BuiltGridLock.Read())
            {
                if (BuiltGrid == null)
                {
                    orientationQuaternion = Quaternion.Identity;
                    return;
                }

                wm *= MatrixD.Invert(BuiltGrid.WorldMatrix.GetOrientation());
            }

            orientationQuaternion = wm.ToPositionAndOrientation().Orientation;

            // Apply the block's own orientation on the grid
            previewBlock.Orientation.GetQuaternion(out var previewBlockOrientation);
            orientationQuaternion *= previewBlockOrientation;
        }
        
        #endregion

        #region Built Grid Registration

        public void RegisterBuiltGrid(MyCubeGrid grid)
        {
            if (HasBuilt)
                throw new Exception($"Duplicate registration of built grid: {grid.GetDebugName()}; existing: {BuiltGrid.GetDebugName()}");

            using (BuiltGridLock.Write())
            {
                BuiltGrid = grid;
                StateHash = 0;

                foreach (var fatBlock in BuiltGrid.GetFatBlocks<MyTerminalBlock>())
                    fatBlock.CheckConnectionChanged += OnCheckConnectionChanged;

                ConnectGridEvents();
                RequestUpdate();
            }
        }

        public void UnregisterBuiltGrid()
        {
            if (!HasBuilt)
                return;

            using (BuiltGridLock.Write())
            {
                DisconnectGridEvents();

                foreach (var terminalBlock in BuiltGrid.GetFatBlocks<MyTerminalBlock>())
                    terminalBlock.CheckConnectionChanged -= OnCheckConnectionChanged;

                BuiltGrid = null;
                StateHash = 0;

                Stats.Clear();
                Stats.Add(InitialStats);

                foreach (var projectedBlock in Blocks.Values)
                    projectedBlock.Clear();

                foreach (var baseConnection in BaseConnections.Values)
                    baseConnection.ClearBuiltBlock();

                foreach (var topConnection in TopConnections.Values)
                    topConnection.ClearBuiltBlock();

                RequestUpdate();
            }
        }

        #endregion

        #region Grid Events

        private void ConnectGridEvents()
        {
            BuiltGrid.OnBlockIntegrityChanged += OnBlockIntegrityChangedWithErrorHandler;
            BuiltGrid.OnBlockAdded += OnBlockAddedWithErrorHandler;
            BuiltGrid.OnBlockRemoved += OnBlockRemovedWithErrorHandler;
            BuiltGrid.OnGridSplit += OnGridSplitWithErrorHandler;
            BuiltGrid.OnClosing += OnGridClosingWithErrorHandler;
        }

        private void DisconnectGridEvents()
        {
            BuiltGrid.OnBlockIntegrityChanged -= OnBlockIntegrityChangedWithErrorHandler;
            BuiltGrid.OnBlockAdded -= OnBlockAddedWithErrorHandler;
            BuiltGrid.OnBlockRemoved -= OnBlockRemovedWithErrorHandler;
            BuiltGrid.OnGridSplit -= OnGridSplitWithErrorHandler;
            BuiltGrid.OnClosing -= OnGridClosingWithErrorHandler;
        }

        [ServerOnly]
        private void OnBlockIntegrityChangedWithErrorHandler(MySlimBlock obj)
        {
            try
            {
                OnBlockIntegrityChanged(obj);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        [Everywhere]
        private void OnBlockAddedWithErrorHandler(MySlimBlock obj)
        {
            try
            {
                OnBlockAdded(obj);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        [Everywhere]
        private void OnBlockRemovedWithErrorHandler(MySlimBlock obj)
        {
            try
            {
                OnBlockRemoved(obj);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        [Everywhere]
        private void OnGridSplitWithErrorHandler(MyCubeGrid arg1, MyCubeGrid arg2)
        {
            try
            {
                OnGridSplit(arg1, arg2);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        [Everywhere]
        private void OnGridClosingWithErrorHandler(MyEntity obj)
        {
            try
            {
                OnGridClosing(obj);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        [ServerOnly]
        private void OnBlockIntegrityChanged(MySlimBlock slimBlock)
        {
            if (!HasBuilt)
                return;

            var previewSlimBlock = PreviewGrid.GetOverlappingBlock(slimBlock);
            if (previewSlimBlock == null)
                return;

            if (!Blocks.TryGetValue(previewSlimBlock.Position, out var projectedBlock))
                return;

            switch (projectedBlock.State)
            {
                case BlockState.BeingBuilt:
                    if (slimBlock.Integrity >= previewSlimBlock.Integrity)
                        RequestUpdate();
                    break;
                case BlockState.FullyBuilt:
                    if (slimBlock.Integrity < previewSlimBlock.Integrity)
                        RequestUpdate();
                    break;
            }
        }

        [Everywhere]
        private void OnBlockAdded(MySlimBlock slimBlock)
        {
            if (!HasBuilt) return;

            // Must refresh the preview, even if the block added does not overlap any preview blocks.
            // Failing to do so may cause the first block not to be weldable due to the lack of refresh.
            // FIXME: Optimize by limiting the update only to the volume around the block added
            RequestUpdate();

            var previewSlimBlock = PreviewGrid.GetOverlappingBlock(slimBlock);
            if (previewSlimBlock == null)
                return;

            if (slimBlock.FatBlock is MyTerminalBlock terminalBlock)
            {
                AddBlockToGroups(terminalBlock);
                terminalBlock.CheckConnectionChanged += OnCheckConnectionChanged;
            }

            switch (slimBlock.FatBlock)
            {
                case MyMechanicalConnectionBlockBase baseBlock:
                    if (!BaseConnections.TryGetValue(previewSlimBlock.Position, out var baseConnection)) break;
                    baseConnection.Block = baseBlock;
                    baseConnection.RequestAttach = true;
                    break;

                case MyAttachableTopBlockBase topBlock:
                    if (!TopConnections.TryGetValue(previewSlimBlock.Position, out var topConnection)) break;
                    topConnection.Block = topBlock;
                    topConnection.RequestAttach = true;
                    break;
            }
        }

        [Everywhere]
        private void OnBlockRemoved(MySlimBlock slimBlock)
        {
            if (!HasBuilt) return;

            // Must refresh the preview, even if the block removed does not overlap any preview blocks.
            // Failing to do so may cause blocks still showing up weldable due to the lack of refresh.
            // FIXME: Optimize by limiting the update only to the volume around the block removed
            RequestUpdate();

            var previewSlimBlock = PreviewGrid.GetOverlappingBlock(slimBlock);
            if (previewSlimBlock == null)
                return;

            if (slimBlock.FatBlock is MyTerminalBlock terminalBlock)
            {
                RemoveBlockFromGroups(terminalBlock);
                terminalBlock.CheckConnectionChanged -= OnCheckConnectionChanged;
            }

            switch (slimBlock.FatBlock)
            {
                case MyMechanicalConnectionBlockBase _:
                    if (!BaseConnections.TryGetValue(previewSlimBlock.Position, out var baseConnection)) break;
                    baseConnection.Block = null;
                    break;

                case MyAttachableTopBlockBase _:
                    if (!TopConnections.TryGetValue(previewSlimBlock.Position, out var topConnection)) break;
                    topConnection.Block = null;
                    break;
            }
        }

        [Everywhere]
        private void OnGridSplit(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            var builtGrid = BuiltGrid;

            UnregisterBuiltGrid();

            if (IsConnectedToProjector)
                RegisterBuiltGrid(builtGrid);
        }

        [Everywhere]
        private void OnGridClosing(MyEntity obj)
        {
            if (!(obj is MyCubeGrid))
                return;

            UnregisterBuiltGrid();
        }

        private void OnCheckConnectionChanged(MyCubeBlock obj)
        {
            RequestUpdate();
        }

        #endregion

        #region Preview Block Visuals

        public void UpdatePreviewBlockVisuals(MyProjectorBase projector, bool showOnlyBuildable)
        {
            if (Sync.IsDedicated)
                return;

            if (PreviewGrid == null)
                return;

            if (!Supported)
            {
                HideUnsupportedPreviewGrid(projector);
                Blocks.Clear();
                return;
            }

            foreach (var projectedBlock in Blocks.Values)
                projectedBlock.UpdateVisual(projector, showOnlyBuildable);
        }

        private void HideUnsupportedPreviewGrid(MyProjectorBase projector)
        {
            if (hidden)
                return;

            HidePreviewGrid(projector);
            hidden = true;
        }

        public void HidePreviewGrid(MyProjectorBase projector)
        {
            if (Sync.IsDedicated)
                return;

            if (PreviewGrid == null)
                return;

            foreach (var slimBlock in PreviewGrid.CubeBlocks)
                projector.HideCube(slimBlock);
        }

        #endregion

        #region Named Groups

        [ServerOnly]
        private void AddBlockToGroups(MyTerminalBlock terminalBlock)
        {
            if (PreviewGrid == null || terminalBlock.CubeGrid.Closed)
                return;

            // Add block to named groups
            var position = PreviewGrid.WorldToGridInteger(terminalBlock.SlimBlock.WorldPosition);
            foreach (var blockGroup in GridBuilder.BlockGroups.Where(blockGroup => blockGroup.Blocks.Contains(position)))
            {
                var newBlockGroup = MyBlockGroupExtensions.NewBlockGroup(blockGroup.Name);
                newBlockGroup.GetTerminalBlocks().Add(terminalBlock);
                terminalBlock.CubeGrid.AddGroup(newBlockGroup);
            }
        }

        [ServerOnly]
        private void RemoveBlockFromGroups(MyTerminalBlock terminalBlock)
        {
            if (PreviewGrid == null || terminalBlock.CubeGrid.Closed)
                return;

            // Remove block from named groups
            var blockGroups = terminalBlock.CubeGrid.GetBlockGroups();
            foreach (var blockGroup in blockGroups)
            {
                var blocks = blockGroup.GetTerminalBlocks();
                if (!blocks.Contains(terminalBlock))
                    continue;

                blocks.Remove(terminalBlock);
            }

            // Remove any named groups which remained empty
            for (;;)
            {
                var index = blockGroups.FindIndex(g => g.GetTerminalBlocks().Count == 0);
                if (index < 0)
                    break;

                blockGroups.RemoveAt(index);
            }
        }

        #endregion

        #region Update Work

        public int UpdateBlockStatesBackgroundWork(MyProjectorBase projector)
        {
            if (!IsUpdateRequested)
                return 0;

            IsUpdateRequested = false;

            stats.Clear();

            var stateHash = unchecked (0xdeadbeafdeadbeaful * (ulong) (1 + Index));
            foreach (var projectedBlock in Blocks.Values)
            {
                projectedBlock.DetectBlock(projector, BuiltGrid);
                stats.RegisterBlock(projectedBlock.Preview, projectedBlock.State);
                stateHash = unchecked ((stateHash << 11) - stateHash) ^ (ulong)projectedBlock.State;
            }

            using (BuiltGridLock.Read())
            {
                var oldStats = Stats;
                Stats = stats;
                stats = oldStats;
            }

            StateHash = stateHash;

            return Blocks.Count;
        }

        public void FindBuiltBaseConnectionsBackgroundWork()
        {
            if (!Supported)
                return;

            foreach (var (position, baseConnection) in BaseConnections)
            {
                if (!Blocks.TryGetValue(position, out var projectedBlock))
                    continue;

                switch (projectedBlock.State)
                {
                    case BlockState.BeingBuilt:
                    case BlockState.FullyBuilt:
                        if (baseConnection.Found != null)
                            break;
                        baseConnection.Found = (MyMechanicalConnectionBlockBase) projectedBlock.SlimBlock.FatBlock;
                        break;
                    default:
                        baseConnection.Found = null;
                        break;
                }
            }
        }

        public void FindBuiltTopConnectionsBackgroundWork()
        {
            if (!Supported)
                return;

            foreach (var (position, topConnection) in TopConnections)
            {
                if (!Blocks.TryGetValue(position, out var projectedBlock))
                    continue;

                switch (projectedBlock.State)
                {
                    case BlockState.BeingBuilt:
                    case BlockState.FullyBuilt:
                        topConnection.Found = (MyAttachableTopBlockBase) projectedBlock.SlimBlock.FatBlock;
                        break;

                    default:
                        topConnection.Found = null;
                        break;
                }
            }
        }

        #endregion
    }
}