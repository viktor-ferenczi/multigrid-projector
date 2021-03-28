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
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace MultigridProjector.Logic
{
    public class Subgrid : IDisposable
    {
        public delegate void SubgridEvent(Subgrid subgrid);
        public delegate void SubgridBlockEvent(Subgrid subgrid, MySlimBlock block);
        public delegate void SubgridTerminalBlockEvent(Subgrid subgrid, MyTerminalBlock block);
        public delegate void SubgridBaseEvent(Subgrid subgrid, BaseConnection baseConnection);
        public delegate void SubgridTopEvent(Subgrid subgrid, TopConnection baseConnection);

        public event SubgridBaseEvent OnBaseAdded;
        public event SubgridBaseEvent OnBaseRemoved;
        public event SubgridTopEvent OnTopAdded;
        public event SubgridTopEvent OnTopRemoved;
        public event SubgridTerminalBlockEvent OnTerminalBlockAdded;
        public event SubgridTerminalBlockEvent OnTerminalBlockRemoved;
        public event SubgridBlockEvent OnOtherBlockAdded;
        public event SubgridBlockEvent OnOtherBlockRemoved;
        public event SubgridEvent OnBuiltGridRegistered;
        public event SubgridEvent OnBuiltGridUnregistered;
        public event SubgridEvent OnBuiltGridSplit;
        public event SubgridEvent OnBuiltGridClose;

        // Mechanical base blocks on this subgrid by cube position
        public readonly Dictionary<Vector3I, BaseConnection> BaseConnections = new Dictionary<Vector3I, BaseConnection>();

        // Mechanical top blocks on this subgrid by cube position
        public readonly Dictionary<Vector3I, TopConnection> TopConnections = new Dictionary<Vector3I, TopConnection>();

        // Welding state of each block as collected by the background worker
        public readonly Dictionary<Vector3I, BlockState> BlockStates = new Dictionary<Vector3I, BlockState>();

        // Block state matching the current visual
        public readonly Dictionary<Vector3I, BlockState> VisualBlockStates = new Dictionary<Vector3I, BlockState>();

        public readonly int Index;

        // Grid builder
        public readonly MyObjectBuilder_CubeGrid GridBuilder;

        // Preview grid from the clipboard
        public readonly MyCubeGrid PreviewGrid;

        // Welding state statistics collected by the background worker
        public readonly ProjectionStats Stats = new ProjectionStats();
        
        public volatile bool UpdateRequested = true;

        public Subgrid(MultigridProjection projection, int index)
        {
            Index = index;
            GridBuilder = projection.GridBuilders[index];
            PreviewGrid = projection.PreviewGrids[index];
            
            PrepareMechanicalConnections(projection);
            ClearBlockStates();
        }

        // Grid if already built
        public MyCubeGrid BuiltGrid { get; protected set; }
        public readonly RwLock BuiltGridLock = new RwLock();

        public bool HasBuilt => BuiltGrid != null;
        public MyCubeSize GridSizeEnum => PreviewGrid.GridSizeEnum;

        public bool IsConnectedToProjector = false;

        public void Dispose()
        {
            BaseConnections.Clear();
            TopConnections.Clear();
            UnregisterBuiltGrid(true);
        }

        private void PrepareMechanicalConnections(MultigridProjection projection)
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

        private void ClearBlockStates()
        {
            foreach (var slimBlock in PreviewGrid.CubeBlocks)
                BlockStates[slimBlock.Position] = BlockState.NotBuildable;
        }

        public void RegisterBuiltGrid(MyCubeGrid grid)
        {
            using (BuiltGridLock.Write())
            {
                BuiltGrid = grid;
                UpdateRequested = true;

                ConnectGridEvents();
            }

            OnBuiltGridRegistered?.Invoke(this);
        }

        public void UnregisterBuiltGrid(bool dispose = false)
        {
            if (!HasBuilt)
                return;

            using (BuiltGridLock.Write())
            {
                DisconnectGridEvents();

                BuiltGrid = null;
                UpdateRequested = false;

                // Projector shutdown optimization
                if (dispose)
                    return;
                
                foreach (var baseConnection in BaseConnections.Values)
                    baseConnection.ClearBuiltBlock();

                foreach (var topConnection in TopConnections.Values)
                    topConnection.ClearBuiltBlock();
            }

            Stats.Clear();

            foreach (var previewSlimBlock in PreviewGrid.CubeBlocks)
                BlockStates[previewSlimBlock.Position] = BlockState.NotBuildable;
            
            OnBuiltGridUnregistered?.Invoke(this);
        }

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

        private void OnBlockIntegrityChanged(MySlimBlock slimBlock)
        {
            if (!HasBuilt) return;

            if (!TryGetPreviewByBuiltBlock(slimBlock, out var previewSlimBlock))
                return;

            if (!BlockStates.TryGetValue(previewSlimBlock.Position, out var blockState))
                return;

            switch (blockState)
            {
                case BlockState.BeingBuilt:
                    if (slimBlock.Integrity < previewSlimBlock.Integrity)
                        break;
                    
                    UpdateRequested = true;
                    BlockStates[previewSlimBlock.Position] = BlockState.BeingBuilt;
                    break;
                
                case BlockState.FullyBuilt:
                    if (slimBlock.Integrity >= previewSlimBlock.Integrity)
                        break;
                    
                    UpdateRequested = true;
                    BlockStates[previewSlimBlock.Position] = BlockState.FullyBuilt;
                    break;
            }
        }
        
        private void OnBlockAdded(MySlimBlock slimBlock)
        {
            if (!HasBuilt) return;

            if (!TryGetPreviewByBuiltBlock(slimBlock, out var previewSlimBlock))
                return;

            // FIXME: Optimize by limiting the update only to the volume around the block added
            UpdateRequested = true;

            switch (slimBlock.FatBlock)
            {
                case MyMechanicalConnectionBlockBase baseBlock:
                    if (!BaseConnections.TryGetValue(previewSlimBlock.Position, out var baseConnection)) break;
                    baseConnection.Block = baseBlock;
                    OnBaseAdded?.Invoke(this, baseConnection);
                    OnTerminalBlockAdded?.Invoke(this, baseBlock);
                    break;

                case MyAttachableTopBlockBase topBlock:
                    if (!TopConnections.TryGetValue(previewSlimBlock.Position, out var topConnection)) break;
                    topConnection.Block = topBlock;
                    OnTopAdded?.Invoke(this, topConnection);
                    break;

                case MyTerminalBlock terminalBlock:
                    OnTerminalBlockAdded?.Invoke(this, terminalBlock);
                    break;

                default:
                    OnOtherBlockAdded?.Invoke(this, slimBlock);
                    break;
            }
        }

        private void OnBlockRemoved(MySlimBlock slimBlock)
        {
            if (!HasBuilt) return;

            if (!TryGetPreviewByBuiltBlock(slimBlock, out var previewSlimBlock))
                return;

            // FIXME: Optimize by limiting the update only to the volume around the block removed
            UpdateRequested = true;

            switch (slimBlock.FatBlock)
            {
                case MyMechanicalConnectionBlockBase baseBlock:
                    if (!BaseConnections.TryGetValue(previewSlimBlock.Position, out var baseConnection)) break;
                    baseConnection.Block = null;
                    OnBaseRemoved?.Invoke(this, baseConnection);
                    OnTerminalBlockRemoved?.Invoke(this, baseBlock);
                    break;

                case MyAttachableTopBlockBase _:
                    if (!TopConnections.TryGetValue(previewSlimBlock.Position, out var topConnection)) break;
                    topConnection.Block = null;
                    OnTopRemoved?.Invoke(this, topConnection);
                    break;

                case MyTerminalBlock terminalBlock:
                    // It may not be a terminal block built from the projection, but that's okay for our use case
                    OnTerminalBlockRemoved?.Invoke(this, terminalBlock);
                    break;

                default:
                    OnOtherBlockRemoved?.Invoke(this, slimBlock);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetPreviewByBuiltBlock(MySlimBlock builtSlimBlock, out MySlimBlock previewSlimBlock)
        {
            previewSlimBlock = PreviewGrid.GetOverlappingBlock(builtSlimBlock);
            if (previewSlimBlock == null) return false;
            return builtSlimBlock.BlockDefinition.Id == previewSlimBlock.BlockDefinition.Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBuiltBlockByPreview(MySlimBlock previewSlimBlock, out MySlimBlock builtSlimBlock)
        {
            using (BuiltGridLock.Read())
            {
                builtSlimBlock = BuiltGrid.GetOverlappingBlock(previewSlimBlock);
            }
            if (builtSlimBlock == null) return false;
            return builtSlimBlock.BlockDefinition.Id == previewSlimBlock.BlockDefinition.Id;
        }

        private void OnGridSplit(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            bool gridKept;
            using (BuiltGridLock.Read())
            {
                gridKept = grid1 == BuiltGrid || grid2 == BuiltGrid;
                UpdateRequested = true;
            }

            if (gridKept)
                OnBuiltGridSplit?.Invoke(this);
            else
                UnregisterBuiltGrid();
        }

        private void OnGridClosing(MyEntity obj)
        {
            if (!(obj is MyCubeGrid))
                return;

            UnregisterBuiltGrid();
            OnBuiltGridClose?.Invoke(this);
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

        public void HidePreviewGrid(MyProjectorBase projector)
        {
            if (PreviewGrid == null)
                return;

            foreach (var slimBlock in PreviewGrid.CubeBlocks)
                projector.HideCube(slimBlock);
        }

        public void UpdatePreviewBlockVisuals(MyProjectorBase projector, bool showOnlyBuildable, bool allowOptimization)
        {
            if (PreviewGrid == null)
                return;

            foreach (var slimBlock in PreviewGrid.CubeBlocks)
            {
                var state = BlockStates[slimBlock.Position];

                // Optimization
                if (allowOptimization && VisualBlockStates.TryGetValue(slimBlock.Position, out var visual) && visual == state) continue;

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

                VisualBlockStates[slimBlock.Position] = state;
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

        public void AddBlockToGroups(MyTerminalBlock terminalBlock)
        {
            if (PreviewGrid == null)
                return;

            var position = PreviewGrid.WorldToGridInteger(terminalBlock.SlimBlock.WorldPosition);
            foreach (var blockGroup in GridBuilder.BlockGroups.Where(blockGroup => blockGroup.Blocks.Contains(position)))
            {
                var newBlockGroup = MyBlockGroupExtensions.NewBlockGroup(blockGroup.Name);
                newBlockGroup.GetBlocks().Add(terminalBlock);
                terminalBlock.CubeGrid.AddGroup(newBlockGroup);
            }
        }

        public bool HasBuildableBlockAtPosition(Vector3I position)
        {
            using (BuiltGridLock.Read())
            {
                if (!HasBuilt)
                    return false;

                if (!BlockStates.TryGetValue(position, out var blockState))
                    return false;

                switch (blockState)
                {
                    case BlockState.Buildable:
                    case BlockState.BeingBuilt:
                        return true;
                }

                return false;
            }
        }
    }
}