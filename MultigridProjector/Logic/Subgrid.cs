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

        // Welding state statistics collected by the background worker
        public readonly ProjectionStats Stats = new ProjectionStats();

        // Indicates whether the built grid is connected to the projector
        public bool IsConnectedToProjector = false;

        // Mechanical base blocks on this subgrid by cube position
        public readonly Dictionary<Vector3I, BaseConnection> BaseConnections = new Dictionary<Vector3I, BaseConnection>();

        // Mechanical top blocks on this subgrid by cube position
        public readonly Dictionary<Vector3I, TopConnection> TopConnections = new Dictionary<Vector3I, TopConnection>();

        // Optimization: Fast lookup of blueprint block builders by their Min position at the expense of some additional memory consumption
        private readonly Dictionary<Vector3I, MyObjectBuilder_CubeBlock> _blockBuilders;

        // Welding state of each block as collected by the background worker
        private readonly Dictionary<Vector3I, BlockState> _blockStates;

        // Block state matching the current visual
        private readonly Dictionary<Vector3I, BlockState> _visualBlockStates;

        // Requests rescanning the preview blocks inside a specified bounding box
        private BoundingBoxI _updateBox = Constants.MaxBoundingBoxI;
        public bool IsUpdateRequested => _updateBox.IsValid;

        // Requests updating the preview block visuals inside a specified bounding box
        private BoundingBoxI _updateVisualsBox = Constants.InvalidBoundingBoxI;
        public bool IsUpdateVisualsRequested => _updateVisualsBox.IsValid;

        #region Initialization and disposal

        public Subgrid(MultigridProjection projection, int index)
        {
            Index = index;

            GridBuilder = projection.GridBuilders[index];
            PreviewGrid = projection.PreviewGrids[index];

            _blockBuilders = GridBuilder.CubeBlocks.ToDictionary(bb => new Vector3I(bb.Min.X, bb.Min.Y, bb.Min.Z));
            _blockStates = PreviewGrid.CubeBlocks.ToDictionary(slimBlock => slimBlock.Position, _ => BlockState.Unknown);
            _visualBlockStates = PreviewGrid.CubeBlocks.ToDictionary(slimBlock => slimBlock.Position, _ => BlockState.Unknown);

            CreateMechanicalConnections(projection);
        }

        public void Dispose()
        {
            _blockStates.Clear();
            _blockBuilders.Clear();

            BaseConnections.Clear();
            TopConnections.Clear();

            UnregisterBuiltGrid();
        }

        private void CreateMechanicalConnections(MultigridProjection projection)
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
            if (!projection.BlueprintConnections.TryGetValue(baseMinLocation, out var topMinLocation)) return;
            var topBlock = projection.PreviewTopBlocks[topMinLocation];
            BaseConnections[slimBlock.Position] = new BaseConnection(baseBlock, new BlockLocation(topMinLocation.GridIndex, topBlock.Position));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PrepareTop(MultigridProjection projection, MySlimBlock slimBlock, MyAttachableTopBlockBase topBlock)
        {
            var topMinLocation = new BlockMinLocation(Index, topBlock.Min);
            if (!projection.BlueprintConnections.TryGetValue(topMinLocation, out var baseMinLocation)) return;
            var baseBlock = projection.PreviewBaseBlocks[baseMinLocation];
            TopConnections[slimBlock.Position] = new TopConnection(topBlock, new BlockLocation(baseMinLocation.GridIndex, baseBlock.Position));
        }

        #endregion

        #region Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBlockBuilder(Vector3I previewBlockMinPosition, out MyObjectBuilder_CubeBlock blockBuilder)
        {
            return _blockBuilders.TryGetValue(previewBlockMinPosition, out blockBuilder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBlockState(Vector3I position, out BlockState blockState)
        {
            return _blockStates.TryGetValue(position, out blockState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBuildableBlockAtPosition(Vector3I position)
        {
            using (BuiltGridLock.Read())
            {
                return HasBuilt && _blockStates.TryGetValue(position, out var blockState) && blockState == BlockState.Buildable;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RequestUpdate()
        {
            _updateBox = Constants.MaxBoundingBoxI;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RequestUpdate(BoundingBoxI box)
        {
            _updateBox.Include(box);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RequestVisualsUpdate()
        {
            _updateVisualsBox = Constants.MaxBoundingBoxI;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RequestVisualsUpdate(BoundingBoxI box)
        {
            _updateVisualsBox.Include(box);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<(Vector3I, BlockState)> IterBlockStates(BoundingBoxI box, int mask)
        {
            foreach (var (position, blockState) in _blockStates)
            {
                if (((int) blockState & mask) == 0)
                    continue;

                if (box.Contains(position) == ContainmentType.Contains)
                    yield return (position, blockState);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetMatchingBuiltBlock(MySlimBlock builtSlimBlock, out MySlimBlock previewSlimBlock)
        {
            previewSlimBlock = PreviewGrid.GetOverlappingBlock(builtSlimBlock);
            return previewSlimBlock != null && builtSlimBlock.BlockDefinition.Id == previewSlimBlock.BlockDefinition.Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetBuiltBlockByPreview(MySlimBlock previewSlimBlock, out MySlimBlock builtSlimBlock)
        {
            using (BuiltGridLock.Read())
            {
                builtSlimBlock = BuiltGrid.GetOverlappingBlock(previewSlimBlock);
            }

            return builtSlimBlock != null && builtSlimBlock.BlockDefinition.Id == previewSlimBlock.BlockDefinition.Id;
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
                throw new Exception($"Duplicate registration of built grid: {grid.DisplayName} [{grid.EntityId}]; existing: {BuiltGrid.DisplayName} [{BuiltGrid.EntityId}]");

            using (BuiltGridLock.Write())
            {
                BuiltGrid = grid;

                RequestUpdate();
                _updateBox = Constants.MaxBoundingBoxI;

                ConnectGridEvents();
            }
        }

        public void UnregisterBuiltGrid()
        {
            if (!HasBuilt)
                return;

            using (BuiltGridLock.Write())
            {
                DisconnectGridEvents();

                BuiltGrid = null;

                RequestUpdate();

                foreach (var baseConnection in BaseConnections.Values)
                    baseConnection.ClearBuiltBlock();

                foreach (var topConnection in TopConnections.Values)
                    topConnection.ClearBuiltBlock();
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
            if (!HasBuilt) return;

            if (!TryGetMatchingBuiltBlock(slimBlock, out var previewSlimBlock))
                return;

            if (!_blockStates.TryGetValue(previewSlimBlock.Position, out var blockState))
                return;

            switch (blockState)
            {
                case BlockState.BeingBuilt:
                    if (slimBlock.Integrity >= previewSlimBlock.Integrity)
                    {
                        RequestUpdate();
                        _blockStates[previewSlimBlock.Position] = BlockState.FullyBuilt;
                    }

                    break;

                case BlockState.FullyBuilt:
                    if (slimBlock.Integrity < previewSlimBlock.Integrity)
                    {
                        RequestUpdate();
                        _blockStates[previewSlimBlock.Position] = BlockState.BeingBuilt;
                    }

                    break;
            }
        }

        private void OnBlockAdded(MySlimBlock slimBlock)
        {
            if (!HasBuilt) return;

            var previewSlimBlock = PreviewGrid.GetOverlappingBlock(slimBlock);

            // FIXME: Optimize by limiting the update only to the volume around the block added
            RequestUpdate();

            if (slimBlock.FatBlock is MyTerminalBlock terminalBlock)
            {
                AddBlockToGroups(terminalBlock);

                // FIXME: Figure whether we need it!
                // terminalBlock.CheckConnectionChanged += CheckConnectionChanged;
                // CheckConnectionChanged just invoked ShouldUpdateProjection();
            }

            if (previewSlimBlock != null)
            {
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
        }

        [ServerOnly]
        private void OnBlockRemoved(MySlimBlock slimBlock)
        {
            if (!HasBuilt) return;

            var previewSlimBlock = PreviewGrid.GetOverlappingBlock(slimBlock);

            // FIXME: Optimize by limiting the update only to the volume around the block removed
            RequestUpdate();

            if (slimBlock.FatBlock is MyTerminalBlock terminalBlock)
            {
                RemoveBlockFromGroups(terminalBlock);

                // FIXME: Figure whether we need it!
                // terminalBlock.CheckConnectionChanged -= CheckConnectionChanged;
                // CheckConnectionChanged just invoked ShouldUpdateProjection();
            }

            if (previewSlimBlock != null)
            {
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
        }

        private void OnGridSplit(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            bool gridKept;
            using (BuiltGridLock.Read())
            {
                gridKept = grid1 == BuiltGrid || grid2 == BuiltGrid;
                RequestUpdate();
            }

            if (!gridKept)
                UnregisterBuiltGrid();
        }

        private void OnGridClosing(MyEntity obj)
        {
            if (!(obj is MyCubeGrid))
                return;

            UnregisterBuiltGrid();
        }

        #endregion
        
        #region Preview Block Visuals

        public void HidePreviewGrid(MyProjectorBase projector)
        {
            if (Sync.IsDedicated)
                return;
            
            if (PreviewGrid == null)
                return;

            foreach (var slimBlock in PreviewGrid.CubeBlocks)
                projector.HideCube(slimBlock);
        }

        public void ResetNotBuildableVisuals()
        {
            if (Sync.IsDedicated)
                return;
            
            foreach (var slimBlock in PreviewGrid.CubeBlocks)
            {
                if (_visualBlockStates[slimBlock.Position] == BlockState.NotBuildable)
                    _visualBlockStates[slimBlock.Position] = BlockState.Unknown;
            }
        }

        public void UpdatePreviewBlockVisualsAsNeeded(MyProjectorBase projector, bool showOnlyBuildable)
        {
            if (Sync.IsDedicated)
                return;
            
            if (PreviewGrid == null)
                return;

            if (!IsUpdateVisualsRequested)
                return;

            var box = _updateVisualsBox;
            _updateVisualsBox = Constants.InvalidBoundingBoxI;

            UpdatePreviewBlockVisuals(projector, showOnlyBuildable, box);
        }

        private void UpdatePreviewBlockVisuals(MyProjectorBase projector, bool showOnlyBuildable, BoundingBoxI box)
        {
            foreach (var slimBlock in PreviewGrid.CubeBlocks)
            {
                if (box.Contains(slimBlock.Position) != ContainmentType.Contains)
                    continue;

                var state = _blockStates[slimBlock.Position];

                // Optimization
                if (_visualBlockStates.TryGetValue(slimBlock.Position, out var visual) && visual == state) continue;

                switch (state)
                {
                    case BlockState.Unknown:
                        break;

                    case BlockState.NotBuildable:
                        if (showOnlyBuildable)
                            HideCube(projector, slimBlock);
                        else
                            ShowCube(projector, slimBlock, false);
                        break;

                    case BlockState.Buildable:
                    case BlockState.Mismatch:
                        ShowCube(projector, slimBlock, true);
                        break;

                    case BlockState.BeingBuilt:
                    case BlockState.FullyBuilt:
                        HideCube(projector, slimBlock);
                        break;
                }

                _visualBlockStates[slimBlock.Position] = state;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShowCube(MyProjectorBase projector, MySlimBlock cubeBlock, bool canBuild)
        {
            projector.SetTransparency(cubeBlock, canBuild ? -0.5f : 0.5f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HideCube(MyProjectorBase projector, MySlimBlock cubeBlock)
        {
            projector.SetTransparency(cubeBlock, 1f);
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

        #region Background Work

        public void UpdateBlockStatesBackgroundWork(MyProjectorBase projector)
        {
            if (!IsUpdateRequested)
                return;

            Stats.Clear();
            Stats.TotalBlocks += PreviewGrid.CubeBlocks.Count;

            if (HasBuilt)
                UpdatePreviewBackgroundWork(projector);
            else
                ResetPreviewBackgroundWork();
        }

        private void ResetPreviewBackgroundWork()
        {
            foreach (var previewBlock in PreviewGrid.CubeBlocks)
            {
                _blockStates[previewBlock.Position] = BlockState.NotBuildable;
                Stats.RegisterRemainingBlock(previewBlock);
            }

            RequestUpdate(Constants.InvalidBoundingBoxI);
            RequestVisualsUpdate(Constants.MaxBoundingBoxI);
        }

        private void UpdatePreviewBackgroundWork(MyProjectorBase projector)
        {
            var box = _updateBox;
            if (!box.IsValid) return;

            if (box != Constants.MaxBoundingBoxI)
                throw new Exception("Any limited bounding box would result in partial (broken) statistics, currently. Needs to update statistics update code to support this optimization!");
            
            RequestVisualsUpdate(box);
            _updateBox = Constants.InvalidBoundingBoxI;

            foreach (var previewBlock in PreviewGrid.CubeBlocks)
            {
                if (box.Contains(previewBlock.Position) != ContainmentType.Contains)
                    continue;

                if (TryGetBuiltBlockByPreview(previewBlock, out var builtSlimBlock))
                {
                    // Partially or fully built
                    var fullyBuilt = builtSlimBlock.Integrity >= previewBlock.Integrity;
                    _blockStates[previewBlock.Position] = fullyBuilt ? BlockState.FullyBuilt : BlockState.BeingBuilt;

                    // What has not built to the level required by the blueprint is considered as remaining
                    if (!fullyBuilt)
                        Stats.RegisterRemainingBlock(previewBlock);

                    continue;
                }

                // This block hasn't been built yet
                Stats.RegisterRemainingBlock(previewBlock);

                if (builtSlimBlock != null)
                {
                    // A different block was built there
                    _blockStates[previewBlock.Position] = BlockState.Mismatch;
                    continue;
                }

                if (projector.CanBuild(previewBlock))
                {
                    // Block is buildable
                    _blockStates[previewBlock.Position] = BlockState.Buildable;
                    Stats.BuildableBlocks++;
                    continue;
                }

                _blockStates[previewBlock.Position] = BlockState.NotBuildable;
            }
        }

        public void FindBuiltBaseConnectionsBackgroundWork()
        {
            foreach (var (position, baseConnection) in BaseConnections)
            {
                switch (_blockStates[position])
                {
                    case BlockState.BeingBuilt:
                    case BlockState.FullyBuilt:
                        if (baseConnection.Found != null) break;
                        if (TryGetBuiltBlockByPreview(baseConnection.Preview.SlimBlock, out var builtBlock))
                            baseConnection.Found = (MyMechanicalConnectionBlockBase) builtBlock.FatBlock;
                        else
                            baseConnection.Found = null;
                        break;
                    default:
                        baseConnection.Found = null;
                        break;
                }
            }
        }

        public void FindBuiltTopConnectionsBackgroundWork()
        {
            foreach (var (position, topConnection) in TopConnections)
            {
                switch (_blockStates[position])
                {
                    case BlockState.BeingBuilt:
                    case BlockState.FullyBuilt:
                        if (topConnection.Found != null) break;
                        if (TryGetBuiltBlockByPreview(topConnection.Preview.SlimBlock, out var builtBlock))
                            topConnection.Found = (MyAttachableTopBlockBase) builtBlock.FatBlock;
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