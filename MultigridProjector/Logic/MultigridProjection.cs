using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MultigridProjector.Utilities;
using MultigridProjector.Extensions;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Entities.Blocks;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using VRage.ModAPI;
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace MultigridProjector.Logic
{
    // FIXME: Refactor this class
    public class MultigridProjection
    {
        public static readonly RwLockDictionary<long, MultigridProjection> Projections = new RwLockDictionary<long, MultigridProjection>();

        public readonly MyProjectorBase Projector;
        public readonly MyProjectorClipboard Clipboard;
        public readonly List<MyObjectBuilder_CubeGrid> GridBuilders;

        public int GridCount => GridBuilders.Count;
        public List<MyCubeGrid> PreviewGrids => Clipboard.PreviewGrids;
        public bool IsClipboardActive => PreviewGrids?.Count == GridBuilders.Count;
        public bool Initialized { get; private set; }
        public bool IsBuildCompleted => Stats.IsBuildCompleted;

        // Blueprint block builders by min cube position
        public readonly Dictionary<BlockMinLocation, MyObjectBuilder_TerminalBlock> TerminalBlockBuilders = new Dictionary<BlockMinLocation, MyObjectBuilder_TerminalBlock>();

        // Bidirectional mapping of corresponding base and top blocks y their grid index and min cube positions
        public readonly Dictionary<BlockMinLocation, BlockMinLocation> BlueprintConnections = new Dictionary<BlockMinLocation, BlockMinLocation>();

        // Preview base blocks by their grid index and min position
        public readonly Dictionary<BlockMinLocation, MyMechanicalConnectionBlockBase> PreviewBaseBlocks = new Dictionary<BlockMinLocation, MyMechanicalConnectionBlockBase>();

        // Preview top blocks by their grid index and min position
        public readonly Dictionary<BlockMinLocation, MyAttachableTopBlockBase> PreviewTopBlocks = new Dictionary<BlockMinLocation, MyAttachableTopBlockBase>();

        // Mechanical connection block locations in the preview grids by EntityId
        // public readonly Dictionary<long, BlockLocation> MechanicalConnectionBlockLocations = new Dictionary<long, BlockLocation>();

        // Subgrids of the projection, associated built grids as they appear, block status information, statistics
        public readonly List<Subgrid> Subgrids = new List<Subgrid>();

        // Latest aggregated statistics, suitable for built completeness decision and formatting as text
        public readonly ProjectionStats Stats = new ProjectionStats();

        // Background task to update the block states and collect welding completion statistics
        public MultigridUpdateWork UpdateWork;

        // True if the preview block visuals have already been set once before (used for optimization only)
        private bool _previewBlockVisualsUpdated;

        // Show only buildable flag saved on updating cube visuals
        private bool _showOnlyBuildable;

        public static void Create(MyProjectorBase projector, List <MyObjectBuilder_CubeGrid> gridBuilders)
        {
            using (Projections.Write())
            {
                if (Projections.ContainsKey(projector.EntityId))
                    return;

                var projection = new MultigridProjection(projector, gridBuilders);
                Projections[projector.EntityId] = projection;
            }
        }

        public void Destroy()
        {
            using (Projections.Write())
                if (!Projections.Remove(Projector.EntityId))
                    return;

            Clipboard.Deactivate();
            Clipboard.Clear();

            Dispose();
        }

        private void Dispose()
        {
            if (!Initialized) return;
            Initialized = false;

            UpdateWork.OnUpdateWorkCompleted -= OnUpdateWorkCompletedWithErrorHandler;
            UpdateWork.Dispose();
            UpdateWork = null;

            Stats.Clear();

            DisconnectSubgridEventHandlers();

            foreach (var subgrid in Subgrids)
                subgrid.Dispose();

            Subgrids.Clear();
        }

        private MultigridProjection(MyProjectorBase projector, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            Projector = projector;
            Clipboard = projector.GetClipboard();
            GridBuilders = gridBuilders;
            _showOnlyBuildable = Projector.GetShowOnlyBuildable();
        }

        private void Initialize()
        {
            if (Projector.Closed)
                return;

            MapBlueprintBlocks();
            MapPreviewBlocks();
            CreateSubgrids();
            ConnectSubgridEventHandlers();
            CreateUpdateWork();

            Initialized = true;

            ForceUpdateProjection();
        }

        private void MapBlueprintBlocks()
        {
            var topBuilderLocations = new Dictionary<long, BlockMinLocation>();
            foreach (var (gridIndex, gridBuilder) in GridBuilders.Enumerate())
            {
                foreach (var blockBuilder in gridBuilder.CubeBlocks)
                {
                    switch (blockBuilder)
                    {
                        case MyObjectBuilder_AttachableTopBlockBase _:
                        case MyObjectBuilder_Wheel _:
                            topBuilderLocations[blockBuilder.EntityId] = new BlockMinLocation(gridIndex, blockBuilder.Min);
                            break;

                        case MyObjectBuilder_TerminalBlock terminalBlock:
                            TerminalBlockBuilders[new BlockMinLocation(gridIndex, blockBuilder.Min)] = terminalBlock;
                            break;
                    }
                }
            }

            foreach (var (gridIndex, gridBuilder) in GridBuilders.Enumerate())
            {
                foreach (var blockBuilder in gridBuilder.CubeBlocks)
                {
                    if (!(blockBuilder is MyObjectBuilder_MechanicalConnectionBlock baseBuilder)) continue;
                    if (baseBuilder.TopBlockId == null) continue;

                    var baseLocation = new BlockMinLocation(gridIndex, baseBuilder.Min);
                    if(!topBuilderLocations.TryGetValue(baseBuilder.TopBlockId.Value, out var topLocation)) continue;

                    BlueprintConnections[baseLocation] = topLocation;
                    BlueprintConnections[topLocation] = baseLocation;
                }
            }
        }

        private void MapPreviewBlocks()
        {
            foreach (var (gridIndex, previewGrid) in PreviewGrids.Enumerate())
            {
                foreach (var slimBlock in previewGrid.CubeBlocks)
                {
                    switch (slimBlock.FatBlock)
                    {
                        case MyMechanicalConnectionBlockBase baseBlock:
                            // MechanicalConnectionBlockLocations[baseBlock.EntityId] = new BlockLocation(gridIndex, slimBlock.Position);
                            PreviewBaseBlocks[new BlockMinLocation(gridIndex, slimBlock.Min)] = baseBlock;
                            break;
                        case MyAttachableTopBlockBase topBlock:
                            // MechanicalConnectionBlockLocations[topBlock.EntityId] = new BlockLocation(gridIndex, slimBlock.Position);
                            PreviewTopBlocks[new BlockMinLocation(gridIndex, slimBlock.Min)] = topBlock;
                            break;
                    }
                }
            }
        }

        private void CreateSubgrids()
        {
            Subgrids.Add(new ProjectorSubgrid(this));

            for (var gridIndex = 1; gridIndex < GridCount; gridIndex++)
                Subgrids.Add(new Subgrid(this, gridIndex));

            Subgrids[0].RegisterBuiltGrid(Projector.CubeGrid);
        }

        private void ConnectSubgridEventHandlers()
        {
            foreach (var subgrid in Subgrids)
            {
                subgrid.OnBaseAdded += OnBaseAdded;
                subgrid.OnBaseRemoved += OnBaseRemoved;
                subgrid.OnTopAdded += OnTopAdded;
                subgrid.OnTopRemoved += OnTopRemoved;
                subgrid.OnTerminalBlockAdded += OnTerminalBlockAdded;
                subgrid.OnTerminalBlockRemoved += OnTerminalBlockRemoved;
                subgrid.OnOtherBlockAdded += OnOtherBlockAdded;
                subgrid.OnOtherBlockRemoved += OnOtherBlockRemoved;
                subgrid.OnBuiltGridRegistered += OnBuiltGridRegistered;
                subgrid.OnBuiltGridUnregistered += OnBuiltGridUnregistered;
                subgrid.OnBuiltGridSplit += OnBuiltGridSplit;
                subgrid.OnBuiltGridClose += OnBuiltGridClose;
            }
        }

        private void DisconnectSubgridEventHandlers()
        {
            foreach (var subgrid in Subgrids)
            {
                subgrid.OnBaseAdded -= OnBaseAdded;
                subgrid.OnBaseRemoved -= OnBaseRemoved;
                subgrid.OnTopAdded -= OnTopAdded;
                subgrid.OnTopRemoved -= OnTopRemoved;
                subgrid.OnTerminalBlockAdded -= OnTerminalBlockAdded;
                subgrid.OnTerminalBlockRemoved -= OnTerminalBlockRemoved;
                subgrid.OnOtherBlockAdded -= OnOtherBlockAdded;
                subgrid.OnOtherBlockRemoved -= OnOtherBlockRemoved;
                subgrid.OnBuiltGridRegistered -= OnBuiltGridRegistered;
                subgrid.OnBuiltGridUnregistered -= OnBuiltGridUnregistered;
                subgrid.OnBuiltGridSplit -= OnBuiltGridSplit;
                subgrid.OnBuiltGridClose -= OnBuiltGridClose;
            }
        }

        private void CreateUpdateWork()
        {
            UpdateWork = new MultigridUpdateWork(this);
            UpdateWork.OnUpdateWorkCompleted += OnUpdateWorkCompletedWithErrorHandler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFindProjectionByProjector(MyProjectorBase projector, out MultigridProjection projection)
        {
            return TryFindProjectionByProjector(projector?.EntityId ?? -1, out projection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFindProjectionByProjector(long projectorId, out MultigridProjection projection)
        {
            using (Projections.Read())
            {
                return Projections.TryGetValue(projectorId, out projection);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFindSubgrid(long projectorId, int subgridIndex, out MultigridProjection projection, out Subgrid subgrid)
        {
            using (Projections.Read())
            {
                if (!Projections.TryGetValue(projectorId, out projection) || !projection.Initialized)
                {
                    subgrid = null;
                    return false;
                }

                if (subgridIndex < 0 || subgridIndex >= projection.GridCount)
                {
                    subgrid = null;
                    return false;
                }

                subgrid = projection.Subgrids[subgridIndex];
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFindProjectionByProjectorClipboard(MyProjectorClipboard projectorClipboard, out MultigridProjection projection)
        {
            using (Projections.Read())
            {
                projection = Projections.Values.FirstOrDefault(p => p.Clipboard == projectorClipboard);
            }
            return projection != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFindPreviewGrid(MyCubeGrid grid, out int gridIndex)
        {
            if (!Initialized)
            {
                gridIndex = 0;
                return false;
            }
            
            using (Projections.Read())
            {
                gridIndex = PreviewGrids.IndexOf(grid);
            }
            return gridIndex >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFindProjectionByBuiltGrid(MyCubeGrid grid, out MultigridProjection projection, out Subgrid subgrid)
        {
            using (Projections.Read())
            {
                projection = Projections.Values.FirstOrDefault(p => p.TryFindSubgridByBuiltGrid(grid, out _));
                if (projection == null || !projection.Initialized)
                {
                    subgrid = null;
                    return false;
                }

                return projection.TryFindSubgridByBuiltGrid(grid, out subgrid);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFindSubgridByBuiltGrid(MyCubeGrid grid, out Subgrid subgrid)
        {
            subgrid = Subgrids.FirstOrDefault(s => s.BuiltGrid == grid);
            return subgrid != null;
        }

        public void StartUpdateWork()
        {
            if (Projector.Closed)
                return;

            if (Projector.Enabled)
                UpdateWork.Start();
            else
                HidePreviewGrids();
        }

        // FIXME: Do we really need this?
        private void HidePreviewGrids()
        {
            foreach (var subgrid in Subgrids)
                subgrid.HidePreviewGrid(Projector);
        }

        private void OnUpdateWorkCompletedWithErrorHandler() {
            try
            {
                OnUpdateWorkCompleted();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        private void OnUpdateWorkCompleted()
        {
            if (Projector.Closed)
                return;

            Clipboard.HasPreviewBBox = false;
            
            UpdateSubgridConnectedness();
            UnregisterDisconnectedSubgrids();
            UpdateMechanicalConnections();
            
            AggregateStatistics();
            UpdatePreviewBlockVisuals(true);

            Projector.UpdateSounds();
            Projector.SetEmissiveStateWorking();

            if (Projector.GetShouldUpdateTexts())
            {
                Projector.SetShouldUpdateTexts(false);
                Projector.UpdateText();
                Projector.RaisePropertiesChanged();
            }

            DetectCompletedProjection();
        }

        private void DetectCompletedProjection()
        {
            var buildCompleted = Stats.IsBuildCompleted;
            var keepProjection = Projector.GetKeepProjection();

            if (!buildCompleted || keepProjection) return;

            Projector.RequestRemoveProjection();
        }

        private void AggregateStatistics()
        {
            Stats.Clear();
            foreach (var subgrid in Subgrids)
                Stats.Add(subgrid.Stats);
        }

        private void UpdatePreviewBlockVisuals(bool allowOptimization)
        {
            _showOnlyBuildable = Projector.GetShowOnlyBuildable();

            if (!_previewBlockVisualsUpdated)
                allowOptimization = false;

            foreach (var subgrid in Subgrids)
                subgrid.UpdatePreviewBlockVisuals(Projector, _showOnlyBuildable, allowOptimization);

            _previewBlockVisualsUpdated = true;
        }

        public void UpdateProjectorStats()
        {
            Projector.SetTotalBlocks(Stats.TotalBlocks);
            Projector.SetRemainingBlocks(Stats.RemainingBlocks);
            Projector.SetRemainingArmorBlocks(Stats.RemainingArmorBlocks);
            Projector.SetBuildableBlocksCount(Stats.BuildableBlocks);
            Projector.GetRemainingBlocksPerType().Update(Stats.RemainingBlocksPerType);
            Projector.SetStatsDirty(true);
        }

        // FIXME: Refactor, simplify
        public void UpdateGridTransformations()
        {
            if (!IsClipboardActive || Projector.Closed)
                return;

            if (!Initialized)
                Initialize();

            if(Subgrids.Any(s => s.UpdateRequested))
                ForceUpdateProjection();

            var projectorMatrix = Projector.WorldMatrix;

            // Apply projections setting (offset, rotation)
            var fromQuaternion = MatrixD.CreateFromQuaternion(Projector.ProjectionRotationQuaternion);
            projectorMatrix = MatrixD.Multiply(fromQuaternion, projectorMatrix);
            projectorMatrix.Translation -= Vector3D.Transform(Projector.GetProjectionTranslationOffset(), Projector.WorldMatrix.GetOrientation());

            // First subgrid
            var firstPreviewGrid = PreviewGrids[0];
            var inverseFirstGridMatrix = MatrixD.Invert(firstPreviewGrid.WorldMatrix);
            var worldMatrix = projectorMatrix;
            var mySlimBlock = firstPreviewGrid.CubeBlocks.First();
            var firstBlockWorldPosition = MyCubeGrid.GridIntegerToWorld(firstPreviewGrid.GridSize, mySlimBlock.Position, worldMatrix);
            var projectionOffset = worldMatrix.Translation - firstBlockWorldPosition;
            worldMatrix.Translation += projectionOffset;
            firstPreviewGrid.PositionComp.Scale = 1f;
            firstPreviewGrid.PositionComp.SetWorldMatrix(ref worldMatrix, skipTeleportCheck: true);

            // Further subgrids
            foreach (var (gridIndex, subgrid) in Subgrids.Enumerate())
            {
                if(gridIndex == 0) continue;

                var previewGrid = subgrid.PreviewGrid;
                previewGrid.PositionComp.Scale = 1f;

                // Align the preview to an already built top block
                var topConnection = subgrid.TopConnections.Values.FirstOrDefault(c => c.IsConnected);
                if (topConnection != null && !topConnection.Block.CubeGrid.Closed)
                {
                    topConnection.Preview.AlignGrid(topConnection.Block);
                    continue;
                }

                // Align the preview to an already built base block
                var baseConnection = subgrid.BaseConnections.Values.FirstOrDefault(c => c.IsConnected);
                if (baseConnection != null && !baseConnection.Block.CubeGrid.Closed)
                {
                    baseConnection.Preview.AlignGrid(baseConnection.Block);
                    continue;
                }

                // Snap the preview by top block connecting to an already positioned preview with a lower index
                topConnection = subgrid.TopConnections.Values.FirstOrDefault(c => c.BaseLocation.GridIndex < gridIndex);
                if (topConnection != null && subgrid.GridBuilder.PositionAndOrientation.HasValue)
                {
                    var baseSubgrid = Subgrids[topConnection.BaseLocation.GridIndex];
                    if (baseSubgrid.GridBuilder.PositionAndOrientation != null)
                    {
                        subgrid.GridBuilder.PositionAndOrientation.Value.ToMatrixD(out var topMatrix);
                        baseSubgrid.GridBuilder.PositionAndOrientation.Value.ToMatrixD(out var baseMatrix);
                        var wm = topMatrix * MatrixD.Invert(baseMatrix) * baseSubgrid.PreviewGrid.PositionComp.WorldMatrixRef;
                        subgrid.PreviewGrid.PositionComp.SetWorldMatrix(ref wm, skipTeleportCheck: true);
                        continue;
                    }
                }

                // Snap the preview by base block connecting to an already positioned preview with a lower index
                baseConnection = subgrid.BaseConnections.Values.FirstOrDefault(c => c.TopLocation.GridIndex < gridIndex);
                if (baseConnection != null && subgrid.GridBuilder.PositionAndOrientation.HasValue)
                {
                    var topSubgrid = Subgrids[baseConnection.TopLocation.GridIndex];
                    if (topSubgrid.GridBuilder.PositionAndOrientation != null)
                    {
                        subgrid.GridBuilder.PositionAndOrientation.Value.ToMatrixD(out var topMatrix);
                        topSubgrid.GridBuilder.PositionAndOrientation.Value.ToMatrixD(out var baseMatrix);
                        var wm = topMatrix * MatrixD.Invert(baseMatrix) * topSubgrid.PreviewGrid.PositionComp.WorldMatrixRef;
                        subgrid.PreviewGrid.PositionComp.SetWorldMatrix(ref wm, skipTeleportCheck: true);
                        continue;
                    }
                }

                // FIXME: This fallback case should not be used
                // Default positioning (incorrect, but follows the projecting grid at least)
                worldMatrix = previewGrid.WorldMatrix * inverseFirstGridMatrix * projectorMatrix;
                worldMatrix.Translation += projectionOffset;
                previewGrid.PositionComp.SetWorldMatrix(ref worldMatrix, skipTeleportCheck: true);
            }
        }

        private void OnBaseAdded(Subgrid subgrid, BaseConnection baseConnection)
        {
            baseConnection.RequestAttach = true;
            ForceUpdateProjection();
        }

        private void OnBaseRemoved(Subgrid subgrid, BaseConnection baseConnection)
        {
            ForceUpdateProjection();
        }

        private void OnTopAdded(Subgrid subgrid, TopConnection topConnection)
        {
            var baseConnection = GetCounterparty(topConnection, out _);
            baseConnection.RequestAttach = true;
            ForceUpdateProjection();
        }

        private void OnTopRemoved(Subgrid subgrid, TopConnection topConnection)
        {
            ForceUpdateProjection();
        }

        private void OnTerminalBlockAdded(Subgrid subgrid, MyTerminalBlock terminalBlock)
        {
            terminalBlock.CheckConnectionChanged += CheckConnectionChanged;
            subgrid.AddBlockToGroups(terminalBlock);
        }

        private void OnTerminalBlockRemoved(Subgrid subgrid, MyTerminalBlock terminalBlock)
        {
            terminalBlock.CheckConnectionChanged -= CheckConnectionChanged;
        }

        private void CheckConnectionChanged(MyCubeBlock fatBlock)
        {
            try
            {
                ShouldUpdateProjection();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        private void OnOtherBlockAdded(Subgrid subgrid, MySlimBlock block)
        {
            ShouldUpdateProjection();
        }

        private void OnOtherBlockRemoved(Subgrid subgrid, MySlimBlock block)
        {
            ShouldUpdateProjection();
        }

        private void OnBuiltGridRegistered(Subgrid subgrid)
        {
            ForceUpdateProjection();
        }

        private void OnBuiltGridUnregistered(Subgrid subgrid)
        {
            ForceUpdateProjection();
        }

        private void OnBuiltGridSplit(Subgrid subgrid)
        {
            DetectAndUnregisterAnyDisconnectedGrids();
            ForceUpdateProjection();
        }

        private void OnBuiltGridClose(Subgrid subgrid)
        {
            DetectAndUnregisterAnyDisconnectedGrids();
            ForceUpdateProjection();
        }

        public void ShouldUpdateProjection()
        {
            Projector.SetShouldUpdateProjection(true);
            Projector.SetShouldUpdateTexts(true);
        }

        public void ForceUpdateProjection()
        {
            Projector.SetForceUpdateProjection(true);
            Projector.SetShouldUpdateTexts(true);
        }

        public void RescanFullProjection()
        {
            if (!Initialized || Projector.Closed)
                return;

            if (Subgrids.Count < 1)
                return;

            Subgrids[0].UpdateRequested = true;

            foreach (var subgrid in Subgrids.Skip(1))
            {
                subgrid.UnregisterBuiltGrid();
                subgrid.UpdateRequested = true;
            }

            ForceUpdateProjection();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TopConnection GetCounterparty(BaseConnection baseConnection, out Subgrid topSubgrid)
        {
            topSubgrid = Subgrids[baseConnection.TopLocation.GridIndex];
            return topSubgrid.TopConnections[baseConnection.TopLocation.Position];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BaseConnection GetCounterparty(TopConnection topConnection, out Subgrid baseSubgrid)
        {
            baseSubgrid = Subgrids[topConnection.BaseLocation.GridIndex];
            return baseSubgrid.BaseConnections[topConnection.BaseLocation.Position];
        }

        private void UnregisterDisconnectedSubgrids()
        {
            foreach (var subgrid in Subgrids)
            {
                if (subgrid.HasBuilt && !subgrid.IsConnectedToProjector)
                    subgrid.UnregisterBuiltGrid();
            }
        }
        
        private void UpdateMechanicalConnections()
        {
            foreach (var subgrid in Subgrids)
                UpdateSubgridConnections(subgrid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateSubgridConnections(Subgrid subgrid)
        {
            foreach (var baseConnection in subgrid.BaseConnections.Values)
                UpdateBaseConnection(subgrid, baseConnection);

            foreach (var topConnection in subgrid.TopConnections.Values)
                UpdateTopConnection(subgrid, topConnection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateBaseConnection(Subgrid baseSubgrid, BaseConnection baseConnection)
        {
            var topConnection = GetCounterparty(baseConnection, out var topSubgrid);

            FindNewlyBuiltBase(baseConnection);
            FindNewlyAddedHead(baseConnection, topConnection);
            BuildMissingHead(baseConnection, baseSubgrid);
            RegisterConnectedSubgrid(baseSubgrid, baseConnection, topConnection, topSubgrid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BuildMissingHead(BaseConnection baseConnection, Subgrid baseSubgrid)
        {
            if (!baseConnection.HasBuilt) return;
            if (baseConnection.IsConnected) return;

            // Create head of right size
            GetCounterparty(baseConnection, out var topSubgrid);
            if (topSubgrid.HasBuilt) return;
            var smallToLarge = baseSubgrid.GridSizeEnum != topSubgrid.GridSizeEnum;
            baseConnection.Block.RecreateTop(baseConnection.Block.BuiltBy, smallToLarge);
            
            // Need to try again every 2 seconds, because building the top part may fail due to objects in the way 
            ShouldUpdateProjection();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FindNewlyBuiltBase(BaseConnection baseConnection)
        {
            if (baseConnection.HasBuilt || baseConnection.Found == null) return;

            baseConnection.Block = baseConnection.Found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FindNewlyAddedHead(BaseConnection baseConnection, TopConnection topConnection)
        {
            if (topConnection.HasBuilt || baseConnection.Block?.TopBlock == null) return;

            topConnection.Block = baseConnection.Block.TopBlock;
            topConnection.Found = topConnection.Block;

            ForceUpdateProjection();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTopConnection(Subgrid topSubgrid, TopConnection topConnection)
        {
            var baseConnection = GetCounterparty(topConnection, out var baseSubgrid);

            FindNewlyBuiltTop(topConnection);
            BuildMissingBase(topConnection, topSubgrid);
            RegisterConnectedSubgrid(baseSubgrid, baseConnection, topConnection, topSubgrid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FindNewlyBuiltTop(TopConnection topConnection)
        {
            if (topConnection.HasBuilt || topConnection.Found == null) return;

            topConnection.Block = topConnection.Found;
        }

        private void BuildMissingBase(TopConnection topConnection, Subgrid topSubgrid)
        {
            if (!topConnection.HasBuilt) return;
            if (topConnection.IsConnected) return;

            // Create head of right size
            GetCounterparty(topConnection, out var baseSubgrid);
            if (baseSubgrid.HasBuilt) return;
            var smallToLarge = baseSubgrid.GridSizeEnum != topSubgrid.GridSizeEnum;
            // FIXME: Implement extension method RecreateBase
            // topConnection.Block.RecreateBase(topConnection.Block.BuiltBy, smallToLarge);
            
            // Need to try again every 2 seconds, because building the base part may fail due to objects in the way 
            // ShouldUpdateProjection();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterConnectedSubgrid(Subgrid baseSubgrid, BaseConnection baseConnection, TopConnection topConnection, Subgrid topSubgrid)
        {
            var bothBaseAndTopAreBuilt = baseConnection.HasBuilt && topConnection.HasBuilt;
            if (!bothBaseAndTopAreBuilt) return;

            var loneTopPart = topConnection.Block.CubeGrid.CubeBlocks.Count == 1;

            var connected = baseConnection.IsConnected && baseConnection.Block.TopBlock.EntityId == topConnection.Block.EntityId;
            if (!connected && baseConnection.RequestAttach)
            {
                baseConnection.RequestAttach = false;
                if(baseConnection.HasBuilt && topConnection.HasBuilt)
                    baseConnection.Block.CallAttach();
                return;
            }

            if (!baseSubgrid.HasBuilt)
                baseSubgrid.RegisterBuiltGrid(baseConnection.Block.CubeGrid);

            if (topSubgrid.HasBuilt) return;

            if (loneTopPart && topConnection.Block.CubeGrid.GridSizeEnum != topSubgrid.GridSizeEnum)
            {
                // This is an ugly workaround to remove the newly built head of wrong size,
                // then building a new one of the proper size on the next simulation frame.
                // It is required, because the patched MyMechanicalConnectionBlockBase.CreateTopPart
                // wasn't used somehow when the new top part is created, therefore the smallToLarge
                // condition cannot be fixed there. It may change with new game releases, because
                // this was most likely due to inlining which this plugin cannot prevent.
                RemoveHead(topConnection);
                ForceUpdateProjection();
                return;
            }

            topSubgrid.RegisterBuiltGrid(topConnection.Block.CubeGrid);

            if (!loneTopPart)
                return;

            topConnection.Block.AlignGrid(topConnection.Preview);

            switch (baseConnection.Block)
            {
                case MyPistonBase pistonBase:
                    pistonBase.SetCurrentPosByTopGridMatrix();
                    break;
                case MyMotorStator motorStator:
                    motorStator.SetAngleToPhysics();
                    motorStator.SetValueFloat("Displacement", ((MyMotorStator) baseConnection.Preview).DummyDisplacement);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RemoveHead(TopConnection topConnection)
        {
            if(topConnection.HasBuilt)
            {
                topConnection.Block.Detach(false);
                topConnection.Block.CubeGrid.Close();
            }
            topConnection.ClearBuiltBlock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateAfterSimulation()
        {
            if (!Initialized || Projector.Closed)
                return;

            if (_showOnlyBuildable != Projector.GetShowOnlyBuildable())
                UpdatePreviewBlockVisuals(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BuildInternal(Vector3I previewCubeBlockPosition, long owner, long builder, bool requestInstant, long subgridIndex)
        {
            if (!Initialized || Projector.Closed || Subgrids.Count == 0)
                return;

            // Negative values are reserved, ignore them
            if (subgridIndex < 0)
                return;

            // Handle single grid projections, even if the client does not have the plugin installed
            if (Subgrids.Count == 1)
                subgridIndex = 0;

            // Find the subgrid to build on
            Subgrid subgrid;
            if (subgridIndex < Subgrids.Count)
            {
                subgrid = Subgrids[(int) subgridIndex];
            }
            else
            {
                // The request was sent by a client without the plugin installed or from a mod which is not aware of subgrids, but can traverse them.
                // They send an identityId here, which is very likely above the subgrid count.
                // Here we attempt to guess the subgrid based on the existence of a weldable block at the given position.
                // In some cases the result is ambiguous and the first one is picked, but this is still better than not being able to weld at all.
                // It works for at least the "Build and Repair mod". Do not remove this speculative code, because it fixes compatibility!
                // It does not work without a mod for hand and ship welders. So a supplemental mod will be needed to send a BuildInternal request.
                subgrid = Subgrids.Find(sg => sg.HasBuildableBlockAtPosition(previewCubeBlockPosition));
                if (subgrid == null)
                    return;
            }

            var previewGrid = subgrid.PreviewGrid;
            if (previewGrid == null)
                return;

            MyCubeGrid builtGrid;
            using (subgrid.BuiltGridLock.Read())
            {
                // The subgrid must have a built grid already
                // Starting top blocks are placed with their base, then registered as the built grid when connections are re-checked (updated).
                builtGrid = subgrid.BuiltGrid;
                if (builtGrid == null)
                    return;
            }

            // Can the player build this block?
            // LEGAL: DO NOT REMOVE THIS CHECK!
            var steamId = MySession.Static.Players.TryGetSteamId(owner);
            var previewBlock = previewGrid.GetCubeBlock(previewCubeBlockPosition);
            if (previewBlock == null || !Projector.AllowWelding || !MySession.Static.GetComponent<MySessionComponentDLC>().HasDefinitionDLC(previewBlock.BlockDefinition, steamId))
            {
                var myMultiplayerServerBase = MyMultiplayer.Static as MyMultiplayerServerBase;
                myMultiplayerServerBase?.ValidationFailed(MyEventContext.Current.Sender.Value, false);
                return;
            }

            // Transform from the preview grid's coordinates to the grid being build (they don't match exactly for the projecting grid)
            // FIXME: Potential optimization opportunity on the non-projecting grids

            var previewFatBlock = previewBlock.FatBlock;

            var previewMin = previewFatBlock?.Min ?? previewBlock.Position;
            var previewMax = previewFatBlock?.Max ?? previewBlock.Position;

            var builtMin = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(previewMin));
            var builtMax = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(previewMax));
            var builtPos = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(previewBlock.Position));

            var min = Vector3I.Min(builtMin, builtMax);
            var max = Vector3I.Max(builtMin, builtMax);

            subgrid.GetBlockOrientationQuaternion(previewBlock, out var previewBlockQuaternion);

            // Fully define where to place the block
            var location = new MyCubeGrid.MyBlockLocation(previewBlock.BlockDefinition.Id, min, max, builtPos, previewBlockQuaternion, 0L, owner);

            // Terminal blocks have their original object builders in a quick to lookup dictionary,
            // but armor blocks can just built from object builders created on demand from the preview
            MyObjectBuilder_CubeBlock blockBuilder;
            if (TerminalBlockBuilders.TryGetValue(new BlockMinLocation(subgrid.Index, previewBlock.Min), out var terminalBlockBuilder) && terminalBlockBuilder.GetId() == previewBlock.BlockDefinition.Id)
            {
                // Terminal blocks with custom settings are created directly from the original blueprint
                blockBuilder = (MyObjectBuilder_CubeBlock)terminalBlockBuilder.Clone();

                // Make sure no EntityId collision will occur on re-welding a terminal block on a previously disconnected (split)
                // part of a built subgrid which has not been destroyed (or garbage collected) yet
                if (MyEntityIdentifier.ExistsById(blockBuilder.EntityId))
                {
                    blockBuilder.EntityId = MyEntityIdentifier.AllocateId();
                }
            }
            else
            {
                // Non-terminal blocks with no custom settings (like armor blocks) are created from the preview blocks to save memory
                blockBuilder = previewBlock.GetObjectBuilder(true);
                location.EntityId = MyEntityIdentifier.AllocateId();
            }

            // Reset batteries to default charge
            if (MyDefinitionManagerBase.Static != null && blockBuilder is MyObjectBuilder_BatteryBlock batteryBuilder)
            {
                var cubeBlockDefinition = (MyBatteryBlockDefinition) MyDefinitionManager.Static.GetCubeBlockDefinition(batteryBuilder);
                batteryBuilder.CurrentStoredPower = cubeBlockDefinition.InitialStoredPowerRatio * cubeBlockDefinition.MaxStoredPower;
            }

            // Empty inventory, ammo (including already loaded ammo)
            blockBuilder.SetupForProjector();
            blockBuilder.ConstructionInventory = null;

            // Ownership is determined by the projector's grid, not by who is welding the block
            blockBuilder.BuiltBy = owner;

            // Instant build is active in creative mode, in survival blocks are built gradually
            var instantBuild = requestInstant && MySession.Static.CreativeToolsEnabled(MyEventContext.Current.Sender.Value);
            var component = MySession.Static.GetComponent<MySessionComponentGameInventory>();
            var skinId = component?.ValidateArmor(previewBlock.SkinSubtypeId, steamId) ?? MyStringHash.NullOrEmpty;
            var visuals = new MyCubeGrid.MyBlockVisuals(previewBlock.ColorMaskHSV.PackHSVToUint(), skinId);

            // Actually build the block on both the server and all clients
            builtGrid.BuildBlockRequestInternal(visuals, location, blockBuilder, builder, instantBuild, owner, MyEventContext.Current.IsLocallyInvoked ? steamId : MyEventContext.Current.Sender.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BuildCheckResult CanBuild(MySlimBlock projectedBlock, bool checkHavokIntersections, out bool fallback)
        {
            // Find the preview grid which as the projectedBlock
            var previewBlock = projectedBlock;
            var previewGrid = previewBlock.CubeGrid;
            if (!TryFindPreviewGrid(previewGrid, out var gridIndex))
            {
                // Unknown projection, fall back to the original code
                fallback = true;
                return BuildCheckResult.NotFound;
            }
            fallback = false;

            // The subgrid being built
            if (gridIndex < 0 || gridIndex >= Subgrids.Count)
                return BuildCheckResult.NotFound;

            MyCubeGrid builtGrid;
            var subgrid = Subgrids[gridIndex];
            using (subgrid.BuiltGridLock.Read())
            {
                builtGrid = subgrid.BuiltGrid;
                if (builtGrid == null)
                {
                    // This subgrid has not been built yet (no top block)
                    return BuildCheckResult.NotWeldable;
                }
            }

            // The following part is based on the original code

            var transformedMin = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(previewBlock.Min));
            var transformedMax = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(previewBlock.Max));
            var transformedPos = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(previewBlock.Position));

            var min = Vector3I.Min(transformedMin, transformedMax);
            var max = Vector3I.Max(transformedMin, transformedMax);

            var blockAlreadyBuilt = builtGrid.GetCubeBlock(transformedPos);
            if (blockAlreadyBuilt?.HasTheSameDefinition(previewBlock) == true)
                return BuildCheckResult.AlreadyBuilt;

            if (!builtGrid.CanAddCubes(min, max))
                return BuildCheckResult.IntersectedWithGrid;

            subgrid.GetBlockOrientationQuaternion(previewBlock, out var previewBlockQuaternion);

            var modelMountPoints = previewBlock.BlockDefinition.GetBuildProgressModelMountPoints(1f);
            if (!MyCubeGrid.CheckConnectivity(builtGrid, previewBlock.BlockDefinition, modelMountPoints, ref previewBlockQuaternion, ref transformedPos))
                return BuildCheckResult.NotConnected;

            if (!checkHavokIntersections)
                return BuildCheckResult.OK;

            var gridPlacementSettings = new MyGridPlacementSettings {SnapMode = SnapMode.OneFreeAxis};
            if (MyCubeGrid.TestPlacementAreaCube(builtGrid, ref gridPlacementSettings, min, max, previewBlock.Orientation, previewBlock.BlockDefinition, ignoredEntity: builtGrid))
                return BuildCheckResult.OK;

            return BuildCheckResult.IntersectedWithSomethingElse;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TestPlacementAreaCube(Subgrid subgrid, MyCubeGrid targetGrid, Vector3I min)
        {
            if (!PreviewBaseBlocks.TryGetValue(new BlockMinLocation(subgrid.Index, min), out var previewBlock))
                return false;

            if (!subgrid.BaseConnections.TryGetValue(previewBlock.Position, out var baseConnection))
                return false;

            GetCounterparty(baseConnection, out var topSubgrid);
            if (!topSubgrid.HasBuilt)
                return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FindProjectedBlock(Vector3D center, Vector3D reachFarPoint, ref MyWelder.ProjectionRaycastData raycastData)
        {
            if (!Initialized || Projector.Closed)
                return;

            // Get intersecting grids from all preview grids
            // FIXME: Optimization would be to intersect only with the ones with a bounding box/sphere inside reachFarPoint of the center
            var cubes = Subgrids
                .Where(subgrid => subgrid.HasBuilt)
                .SelectMany(subgrid => subgrid.PreviewGrid.RayCastBlocksAllOrdered(center, reachFarPoint)).ToList();

            // Sort cubes by farthest to closest
            var cubeDistances = cubes
                .Enumerate()
                .Select(p => (p.Index, (p.Value.CubeBlock.WorldPosition - center).LengthSquared()))
                .ToList();
            cubeDistances.SortNoAlloc((a, b) => b.Item2.CompareTo(a.Item2));

            // Find the first one which can be built
            foreach (var (cubeIndex, _) in cubeDistances)
            {
                var slimBlock = cubes[cubeIndex].CubeBlock;
                var buildCheckResult = Projector.CanBuild(slimBlock, true);

                // Bugfix for a bug most likely exists in the original projector/welding code:
                // Sometimes CanBuild fails with IntersectedWithSomethingElse, but there is no real intersection.
                // My best guess is that this is a floating point boundary / rounding issues somewhere in a collision check.
                // Here it is safe to let the build get through, it will fail anyway in BuildInternal if the block cannot be built.
                // It seems the check in BuildInternal is more reliable, at least with the patch this plugin provides.
                if (buildCheckResult == BuildCheckResult.IntersectedWithSomethingElse)
                    buildCheckResult = BuildCheckResult.OK;

                if (buildCheckResult != BuildCheckResult.OK) continue;

                // Buildable block
                raycastData = new MyWelder.ProjectionRaycastData()
                {
                    raycastResult = buildCheckResult,
                    hitCube = slimBlock,
                    cubeProjector = Projector,
                };
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CreateTopPartAndAttach(Subgrid subgrid, MyMechanicalConnectionBlockBase baseBlock)
        {
            var previewBase = subgrid.PreviewGrid.GetOverlappingBlock(baseBlock.SlimBlock);
            if (previewBase == null)
                return true;

            // Prevent building unwanted heads
            if (!subgrid.BaseConnections.TryGetValue(previewBase.Position, out var baseConnection))
                return false;

            // Prevent building heads already exist on the subgrid
            var topConnection = GetCounterparty(baseConnection, out var topSubgrid);
            if (topSubgrid.HasBuilt)
                return false;

            // Build head of the right model and size according to the top connection's preview block
            var topDefinition = topConnection.Preview.BlockDefinition;
            var constructorInfo = AccessTools.Constructor(typeof(MyCubeBlockDefinitionGroup));
            var definitionGroup = (MyCubeBlockDefinitionGroup)constructorInfo.Invoke(new object[]{});
            definitionGroup[MyCubeSize.Small] = topDefinition;
            definitionGroup[MyCubeSize.Large] = topDefinition;

            // Create the top part
            var instantBuild = Projector.GetInstantBuildingEnabled();
            var sizeConversion = baseConnection.Preview.CubeGrid.GridSizeEnum != topConnection.Preview.CubeGrid.GridSizeEnum;
            var topBlock = baseBlock.CreateTopPart(definitionGroup, sizeConversion, instantBuild);
            if (topBlock == null)
                return false;

            // Attach to the base
            baseBlock.Attach(topBlock);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MyWelder.ProjectionRaycastData[] FindProjectedBlocks(MyShipWelder welder, BoundingSphere detectorSphere, HashSet<MySlimBlock> weldedBlocks)
        {
            var boundingSphereD = new BoundingSphereD(Vector3D.Transform(detectorSphere.Center, welder.CubeGrid.WorldMatrix), detectorSphere.Radius);
            var entitiesInSphere = MyEntities.GetEntitiesInSphere(ref boundingSphereD);

            var raycastDataList = new List<MyWelder.ProjectionRaycastData>();
            try
            {
                foreach (var myEntity in entitiesInSphere)
                {
                    if (!(myEntity is MyCubeGrid myCubeGrid)) continue;

                    var projector = myCubeGrid.Projector;
                    if (projector == null) continue;

                    myCubeGrid.GetBlocksInsideSphere(ref boundingSphereD, weldedBlocks);
                    foreach (var previewSlimBlock in weldedBlocks)
                    {
                        var buildCheckResult = projector.CanBuild(previewSlimBlock, true);

                        // Fix for a bug most likely exists in the original projector/welding code:
                        // Sometimes CanBuild fails with IntersectedWithSomethingElse, but there is no real intersection.
                        // My best guess is that this is a floating point boundary / rounding issues somewhere in a collision check.
                        // Here it is safe to let the build get through, it will fail anyway in BuildInternal if the block cannot be built.
                        // It seems the check in BuildInternal is more reliable, at least with the patch this plugin provides.
                        if (buildCheckResult == BuildCheckResult.IntersectedWithSomethingElse)
                            buildCheckResult = BuildCheckResult.OK;

                        if (buildCheckResult != BuildCheckResult.OK) continue;

                        var cubeBlock = myCubeGrid.GetCubeBlock(previewSlimBlock.Position);
                        if (cubeBlock == null) continue;

                        raycastDataList.Add(new MyWelder.ProjectionRaycastData(BuildCheckResult.OK, cubeBlock, projector));
                    }

                    weldedBlocks.Clear();
                }
            }
            finally
            {
                // Maybe redundant, but clearing anyway to avoid "spooky action at a distance" crashes
                weldedBlocks.Clear();

                // MUST BE CLEARED after using MyEntities.GetEntitiesInSphere,
                // otherwise it can cause NullReferenceException in MyDrillBase.DrillEnvironmentSector!
                entitiesInSphere.Clear();
            }
            return raycastDataList.ToArray();
        }

        // Based on the original code, but without taking up PCU for the projection
        public void InitializeClipboard()
        {
            if (Clipboard.IsActive || Projector.IsActivating)
                return;

            Clipboard.ResetGridOrientation();

            var gridBuilders = Clipboard.CopiedGrids;
            if (!EnsureBuildableUnderLimits(gridBuilders))
                return;

            Projector.SetIsActivating(true);
            Clipboard.Activate(() =>
            {
                Clipboard.PreviewGrids?.SetProjector(Projector);

                Projector.SetForceUpdateProjection(true);
                Projector.SetShouldUpdateTexts(true);
                Projector.SetShouldResetBuildable(true);

                Clipboard.ActuallyTestPlacement();

                Projector.SetRotation(Clipboard, Projector.GetProjectionRotation());
                Projector.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
                Projector.SetIsActivating(false);
            });
        }

        private bool EnsureBuildableUnderLimits(List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            var identity = MySession.Static.Players.TryGetIdentity(Projector.BuiltBy);
            if (identity == null)
                return true;

            if (MultigridProjectorConfig.BlockLimit)
            {
                var blueprintBlockCount = gridBuilders.GetBlockCount();
                if (blueprintBlockCount > int.MaxValue)
                {
                    NotifyPlayer(MySession.LimitResult.MaxBlocksPerPlayer);
                    return false;
                }

                // Allow for building a repair projector for a ship consuming all the player's block limit
                // Please note that the limit is zero if disabled
                var maxBlocks = identity.BlockLimits.MaxBlocks;
                if (maxBlocks > 0 && blueprintBlockCount > 2 * maxBlocks)
                {
                    // Notify the player that the blueprint cannot be built because of exceeding block count limit
                    NotifyPlayer(MySession.LimitResult.MaxBlocksPerPlayer);
                    return false;
                }
            }

            if (MultigridProjectorConfig.PcuLimit)
            {
                // Allow for building a repair projector for a ship consuming all the player's PCU limit
                // Please note that the limit is zero if disabled
                gridBuilders.TryCalculatePcu(out var blueprintPcu, out _);
                var maxPcu = MyBlockLimits.GetMaxPCU(identity);
                if (maxPcu > 0 && blueprintPcu > 2 * maxPcu)
                {
                    // Notify the player that the blueprint cannot be built because of exceeding PCU limit
                    NotifyPlayer(MySession.LimitResult.PCU);
                    return false;
                }
            }

            return true;
        }

        private static void NotifyPlayer(MySession.LimitResult limitResult)
        {
            MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
            MyHud.Notifications.Add(MySession.GetNotificationForLimitResult(limitResult));
        }

        private void UpdateSubgridConnectedness()
        {
            // Clear connectedness
            foreach (var subgrid in Subgrids)
                subgrid.IsConnectedToProjector = false;
            
            // Flood fill along the connected mechanical connections
            Subgrids[0].IsConnectedToProjector = true;
            for (var connected = 0; connected < Subgrids.Count;)
            {
                var modified = 0;
                foreach (var subgrid in Subgrids)
                {
                    if(subgrid.IsConnectedToProjector || !subgrid.HasBuilt)
                        continue;

                    foreach (var baseConnection in subgrid.BaseConnections.Values)
                    {
                        if (Subgrids[baseConnection.TopLocation.GridIndex].IsConnectedToProjector && baseConnection.IsConnected)
                        {
                            subgrid.IsConnectedToProjector = true;
                            modified += 1;
                            connected += 1;
                            break;
                        }
                    }
                    
                    if(subgrid.IsConnectedToProjector)
                        continue;
                    
                    foreach (var topConnection in subgrid.TopConnections.Values)
                    {
                        if (Subgrids[topConnection.BaseLocation.GridIndex].IsConnectedToProjector && topConnection.IsConnected)
                        {
                            subgrid.IsConnectedToProjector = true;
                            modified += 1;
                            connected += 1;
                            break;
                        }
                    }
                }
                
                if(modified == 0)
                    break;
            }
        }

        public void DetectAndUnregisterAnyDisconnectedGrids()
        {
            UpdateSubgridConnectedness();
            UnregisterDisconnectedSubgrids();
        }
    }
}