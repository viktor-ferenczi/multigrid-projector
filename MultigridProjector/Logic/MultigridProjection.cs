using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MultigridProjector.Utilities;
using MultigridProjector.Extensions;
using Sandbox;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Multiplayer;
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
        public static readonly HashSet<long> ProjectorsWithBlueprintLoaded = new HashSet<long>();

        public readonly MyProjectorBase Projector;
        public readonly MyProjectorClipboard Clipboard;
        public readonly List<MyObjectBuilder_CubeGrid> GridBuilders;

        // Subgrids of the projection, associated built grids as they appear, block status information, statistics
        public readonly List<Subgrid> Subgrids = new List<Subgrid>();

        // Bidirectional mapping of corresponding base and top blocks by their grid index and min cube positions
        public readonly Dictionary<BlockMinLocation, BlockMinLocation> BlueprintConnections = new Dictionary<BlockMinLocation, BlockMinLocation>();

        // Preview base blocks by their grid index and min position
        public readonly Dictionary<BlockMinLocation, MyMechanicalConnectionBlockBase> PreviewBaseBlocks = new Dictionary<BlockMinLocation, MyMechanicalConnectionBlockBase>();

        // Preview top blocks by their grid index and min position
        public readonly Dictionary<BlockMinLocation, MyAttachableTopBlockBase> PreviewTopBlocks = new Dictionary<BlockMinLocation, MyAttachableTopBlockBase>();

        public int GridCount => GridBuilders.Count;
        public List<MyCubeGrid> PreviewGrids => Clipboard.PreviewGrids;
        public bool Initialized { get; private set; }

        private bool IsUpdateRequested => Initialized && Subgrids.Any(subgrid => subgrid.IsUpdateRequested);
        private bool IsBuildCompleted => Initialized && _stats.IsBuildCompleted;

        // Mechanical connection block locations in the preview grids by EntityId
        // public readonly Dictionary<long, BlockLocation> MechanicalConnectionBlockLocations = new Dictionary<long, BlockLocation>();

        // Latest aggregated statistics, suitable for built completeness decision and formatting as text
        private readonly ProjectionStats _stats = new ProjectionStats();

        // Background task to update the block states and collect welding completion statistics
        private MultigridUpdateWork _updateWork;

        // Keep projection flag saved for change detection
        private bool _keepProjection;

        // Show only buildable flag saved for change detection
        private bool _showOnlyBuildable;

        // Offset and rotation for change detection
        private Vector3I _projectionOffset;
        private Vector3I _projectionRotation;

        // Scan index, increased every time the preview grids are successfully scanned for block changes
        private long _scanIndex;
        public bool HasScanned => _scanIndex > 0;

        // Requests a remap operation on building the next functional (non-armor) block
        private bool _requestRemap;

        // Controls when the plugin and Mod API can access projector information already
        public bool IsValidForApi => Initialized && HasScanned;

        private static MultigridProjection Create(MyProjectorBase projector, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            using (Projections.Read())
            {
                if (Projections.ContainsKey(projector.EntityId))
                    return null;
            }

            var projection = new MultigridProjection(projector, gridBuilders);

            using (Projections.Write())
            {
                if (Projections.TryGetValue(projector.EntityId, out var existingProjection))
                {
                    projection.Destroy();
                    return existingProjection;
                }

                Projections[projector.EntityId] = projection;
                return projection;
            }
        }

        private MultigridProjection(MyProjectorBase projector, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            Projector = projector;
            Clipboard = projector.GetClipboard();
            GridBuilders = gridBuilders;

            _keepProjection = Projector.GetKeepProjection();
            _showOnlyBuildable = Projector.GetShowOnlyBuildable();

            _projectionOffset = Projector.ProjectionOffset;
            _projectionRotation = Projector.ProjectionRotation;

            if (Projector.Closed)
                return;

            MapBlueprintBlocks();
            MapPreviewBlocks();
            CreateSubgrids();
            MarkSupportedSubgrids();
            CreateUpdateWork();
            AutoAlignBlueprint();

            Projector.PropertiesChanged += OnPropertiesChanged;

            Initialized = true;

            ForceUpdateProjection();
        }

        public void Destroy()
        {
            using (Projections.Write())
                if (!Projections.Remove(Projector.EntityId))
                    return;

            if (!Initialized) return;
            Initialized = false;

            Projector.PropertiesChanged -= OnPropertiesChanged;

            _updateWork.OnUpdateWorkCompleted -= OnUpdateWorkCompletedWithErrorHandler;
            _updateWork.Dispose();
            _updateWork = null;

            _stats.Clear();

            foreach (var subgrid in Subgrids)
                subgrid.Dispose();

            Subgrids.Clear();
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
                    if (!topBuilderLocations.TryGetValue(baseBuilder.TopBlockId.Value, out var topLocation)) continue;

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

        private void MarkSupportedSubgrids()
        {
            Subgrids[0].Supported = true;

            foreach (var _ in Subgrids)
            {
                var modified = 0;

                foreach (var subgrid in Subgrids)
                {
                    if(!subgrid.Supported)
                        continue;

                    foreach (var baseConnection in subgrid.BaseConnections.Values)
                    {
                        var topSubgrid = Subgrids[baseConnection.TopLocation.GridIndex];
                        if(topSubgrid.Supported)
                            continue;

                        topSubgrid.Supported = true;
                        modified++;
                        break;
                    }

                    foreach (var topConnection in subgrid.TopConnections.Values)
                    {
                        var baseSubgrid = Subgrids[topConnection.BaseLocation.GridIndex];
                        if(baseSubgrid.Supported)
                            continue;

                        baseSubgrid.Supported = true;
                        modified++;
                        break;
                    }
                }

                if(modified == 0)
                    break;
            }
        }

        private void CreateUpdateWork()
        {
            _updateWork = new MultigridUpdateWork(this);
            _updateWork.OnUpdateWorkCompleted += OnUpdateWorkCompletedWithErrorHandler;
        }

        private void AutoAlignBlueprint()
        {
            if (!ProjectorsWithBlueprintLoaded.Contains(Projector.EntityId))
                return;

            ProjectorsWithBlueprintLoaded.Remove(Projector.EntityId);

            if(Projector.AlignToRepairProjector(PreviewGrids[0]))
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", $"Aligned repair projection: {Projector.CustomName}");
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

            if (!Projector.Enabled)
            {
                HidePreviewGrids();
                return;
            }

            if (!_updateWork.IsComplete)
                return;

            Projector.SetShouldUpdateProjection(false);
            Projector.SetForceUpdateProjection(false);

            _updateWork.Start();
        }

        private void HidePreviewGrids()
        {
            if (Sync.IsDedicated)
                return;

            foreach (var subgrid in Subgrids)
                subgrid.HidePreviewGrid(Projector);
        }

        [Everywhere]
        private void OnPropertiesChanged(MyTerminalBlock obj)
        {
            if (Projector.Closed || !Initialized)
                return;

            DetectKeepProjectionChange();
            DetectShowOnlyBuildableChange();
            DetectOffsetRotationChange();
        }

        private void DetectKeepProjectionChange()
        {
            var keepProjection = Projector.GetKeepProjection();
            if (_keepProjection == keepProjection) return;
            _keepProjection = keepProjection;

            // Remove projection if the build is complete and Keep Projection is unchecked
            if (!keepProjection && IsBuildCompleted)
                Projector.RequestRemoveProjection();
        }

        private void DetectShowOnlyBuildableChange()
        {
            var showOnlyBuildable = Projector.GetShowOnlyBuildable();
            if (_showOnlyBuildable == showOnlyBuildable) return;
            _showOnlyBuildable = showOnlyBuildable;

            UpdatePreviewBlockVisuals();
        }

        private void DetectOffsetRotationChange()
        {
            if (Projector.ProjectionOffset == _projectionOffset &&
                Projector.ProjectionRotation == _projectionRotation)
                return;

            _projectionOffset = Projector.ProjectionOffset;
            _projectionRotation = Projector.ProjectionRotation;

            RescanFullProjection();
        }

        [Everywhere]
        private void OnUpdateWorkCompletedWithErrorHandler()
        {
            try
            {
                OnUpdateWorkCompleted();
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
        }

        [Everywhere]
        private void OnUpdateWorkCompleted()
        {
            if (Projector.Closed || !Initialized)
                return;

            _scanIndex++;

            PluginLog.Debug($"{Projector.CustomName} [{Projector.EntityId}] scan #{_scanIndex}");

            Projector.SetLastUpdate(MySandboxGame.TotalGamePlayTimeInMilliseconds);

            // Clients must follow replicated grid changes from the server, therefore they need regular updates
            // FIXME: Optimize this case by listening on grid/block change events somehow
            if (!Sync.IsServer)
                ShouldUpdateProjection();

            Clipboard.HasPreviewBBox = false;

            UpdateMechanicalConnections();
            AggregateStatistics();
            UpdateProjectorStats();

            if (Sync.IsServer && _stats.BuiltOnlyArmorBlocks)
                _requestRemap = true;

            if (!Sync.IsDedicated)
            {
                UpdatePreviewBlockVisuals();
                Projector.UpdateSounds();
                Projector.SetEmissiveStateWorking();
            }

            if (Projector.GetShouldUpdateTexts())
            {
                Projector.SetShouldUpdateTexts(false);
                Projector.UpdateText();
                Projector.RaisePropertiesChanged();
            }

            if (!_keepProjection && IsBuildCompleted)
                Projector.RequestRemoveProjection();
        }

        private void AggregateStatistics()
        {
            _stats.Clear();
            foreach (var subgrid in Subgrids)
                _stats.Add(subgrid.Stats);
        }

        [Everywhere]
        public void UpdateProjectorStats()
        {
            Projector.SetTotalBlocks(_stats.TotalBlocks);
            Projector.SetRemainingBlocks(_stats.RemainingBlocks);
            Projector.SetRemainingArmorBlocks(_stats.RemainingArmorBlocks);
            Projector.SetBuildableBlocksCount(_stats.BuildableBlocks);
            Projector.GetRemainingBlocksPerType().Update(_stats.RemainingBlocksPerType);
            Projector.SetStatsDirty(true);
        }

        private void UpdatePreviewBlockVisuals()
        {
            if (Sync.IsDedicated)
                return;

            foreach (var subgrid in Subgrids)
                subgrid.UpdatePreviewBlockVisuals(Projector, _showOnlyBuildable);
        }

        // FIXME: Refactor, simplify
        public void UpdateGridTransformations()
        {
            // Align the preview grids to match any grids has already been built

            if (PreviewGrids == null || PreviewGrids.Count == 0 || Subgrids.Count != PreviewGrids.Count)
                return;

            var projectorMatrix = Projector.WorldMatrix;

            // Apply projections setting (offset, rotation)
            var fromQuaternion = MatrixD.CreateFromQuaternion(Projector.ProjectionRotationQuaternion);
            projectorMatrix = MatrixD.Multiply(fromQuaternion, projectorMatrix);
            projectorMatrix.Translation -= Vector3D.Transform(Projector.GetProjectionTranslationOffset(), Projector.WorldMatrix.GetOrientation());

            // Position the first subgrid preview relative to the projector
            var worldMatrix = projectorMatrix;
            var firstPreviewGrid = PreviewGrids[0];
            var mySlimBlock = firstPreviewGrid.CubeBlocks.First();
            var firstBlockWorldPosition = MyCubeGrid.GridIntegerToWorld(firstPreviewGrid.GridSize, mySlimBlock.Position, worldMatrix);
            var projectionOffset = worldMatrix.Translation - firstBlockWorldPosition;
            worldMatrix.Translation += projectionOffset;
            firstPreviewGrid.PositionComp.Scale = 1f;
            firstPreviewGrid.PositionComp.SetWorldMatrix(ref worldMatrix, skipTeleportCheck: true);

            // Further subgrids
            foreach (var (gridIndex, subgrid) in Subgrids.Enumerate())
            {
                if (gridIndex == 0)
                    continue;

                var gridBuilder = subgrid.GridBuilder;
                if (!gridBuilder.PositionAndOrientation.HasValue)
                    continue;

                var previewGrid = subgrid.PreviewGrid;
                previewGrid.PositionComp.Scale = 1f;

                // Align the preview to an already built top block
                var topConnection = subgrid.TopConnections.Values.FirstOrDefault(IsConnected);
                if (topConnection != null && !topConnection.Block.CubeGrid.Closed)
                {
                    topConnection.Preview.AlignGrid(topConnection.Block);
                    continue;
                }

                // Align the preview to an already built base block
                var baseConnection = subgrid.BaseConnections.Values.FirstOrDefault(IsConnected);
                if (baseConnection != null && !baseConnection.Block.CubeGrid.Closed)
                {
                    baseConnection.Preview.AlignGrid(baseConnection.Block);
                    continue;
                }

                // Align the preview by top block connecting to an already positioned preview with a lower index
                topConnection = subgrid.TopConnections.Values.FirstOrDefault(c => c.BaseLocation.GridIndex < gridIndex);
                if (topConnection != null)
                {
                    var baseSubgrid = Subgrids[topConnection.BaseLocation.GridIndex];
                    if (baseSubgrid.GridBuilder.PositionAndOrientation != null)
                    {
                        gridBuilder.PositionAndOrientation.Value.ToMatrixD(out var topMatrix);
                        baseSubgrid.GridBuilder.PositionAndOrientation.Value.ToMatrixD(out var baseMatrix);
                        var wm = topMatrix * MatrixD.Invert(baseMatrix) * baseSubgrid.PreviewGrid.PositionComp.WorldMatrixRef;
                        subgrid.PreviewGrid.PositionComp.SetWorldMatrix(ref wm, skipTeleportCheck: true);
                        continue;
                    }
                }

                // Align the preview by base block connecting to an already positioned preview with a lower index
                baseConnection = subgrid.BaseConnections.Values.FirstOrDefault(c => c.TopLocation.GridIndex < gridIndex);
                if (baseConnection != null)
                {
                    var topSubgrid = Subgrids[baseConnection.TopLocation.GridIndex];
                    if (topSubgrid.GridBuilder.PositionAndOrientation != null)
                    {
                        gridBuilder.PositionAndOrientation.Value.ToMatrixD(out var topMatrix);
                        topSubgrid.GridBuilder.PositionAndOrientation.Value.ToMatrixD(out var baseMatrix);
                        var wm = topMatrix * MatrixD.Invert(baseMatrix) * topSubgrid.PreviewGrid.PositionComp.WorldMatrixRef;
                        subgrid.PreviewGrid.PositionComp.SetWorldMatrix(ref wm, skipTeleportCheck: true);
                        continue;
                    }
                }

                // Reaching this point means that this subgrid is disconnected from the first subgrid, which should not happen 
            }
        }

        private void ShouldUpdateProjection()
        {
            Projector.SetShouldUpdateProjection(true);
            Projector.SetShouldUpdateTexts(true);
        }

        private void ForceUpdateProjection()
        {
            Projector.SetForceUpdateProjection(true);
            Projector.SetShouldUpdateTexts(true);
        }

        private void RescanFullProjection()
        {
            if (!Initialized || Projector.Closed || Subgrids.Count < 1)
                return;

            _stats.Clear();

            foreach (var subgrid in Subgrids)
            {
                subgrid.UnregisterBuiltGrid();
            }

            Subgrids[0].RegisterBuiltGrid(Projector.CubeGrid);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsConnected(BaseConnection baseConnection, TopConnection topConnection)
        {
            return baseConnection.HasBuilt &&
                   topConnection.HasBuilt &&
                   baseConnection.Block.TopBlock != null &&
                   baseConnection.Block.TopBlock.EntityId == topConnection.Block.EntityId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsConnected(BaseConnection baseConnection)
        {
            return IsConnected(baseConnection, GetCounterparty(baseConnection, out _));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsConnected(TopConnection topConnection)
        {
            return IsConnected(GetCounterparty(topConnection, out _), topConnection);
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

            UpdateSubgridConnectedness();
            UnregisterDisconnectedSubgrids();
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
            if (!baseConnection.HasBuilt || baseConnection.Block.TopBlock != null || !baseConnection.Block.IsFunctional) return;

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
            if (!topConnection.HasBuilt || topConnection.Block.Stator != null) return;

            // Create head of right size
            var baseConnection = GetCounterparty(topConnection, out var baseSubgrid);
            if (baseSubgrid.HasBuilt || !baseConnection.HasBuilt || !baseConnection.Block.IsFunctional) return;
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

            if (baseConnection.Block.TopBlock == null && topConnection.Block.Stator == null && baseConnection.RequestAttach)
            {
                baseConnection.RequestAttach = false;
                if (baseConnection.HasBuilt && topConnection.HasBuilt)
                    baseConnection.Block.CallAttach();
                return;
            }

            if (!baseSubgrid.HasBuilt)
                baseSubgrid.RegisterBuiltGrid(baseConnection.Block.CubeGrid);

            if (topSubgrid.HasBuilt) return;

            if (loneTopPart && (topConnection.Block.CubeGrid.GridSizeEnum != topSubgrid.GridSizeEnum || !baseConnection.Block.IsFunctional))
            {
                // Remove head if the grid size is wrong or if the base is not functional yet.aw
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
            if (topConnection.HasBuilt)
            {
                topConnection.Block.Detach(false);
                topConnection.Block.CubeGrid.Close();
            }

            topConnection.ClearBuiltBlock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAfterSimulation()
        {
            if (!Initialized || Projector.Closed)
                return;

            UpdateGridTransformations();

            if (!_updateWork.IsComplete) return;

            if (IsUpdateRequested)
                ForceUpdateProjection();

            var shouldUpdateProjection = Projector.GetShouldUpdateProjection() && MySandboxGame.TotalGamePlayTimeInMilliseconds - Projector.GetLastUpdate() >= 2000;
            if (shouldUpdateProjection)
            {
                foreach (var subgrid in Subgrids)
                    subgrid.RequestUpdate();
            }

            if (shouldUpdateProjection || Projector.GetForceUpdateProjection())
            {
                Projector.SetHiddenBlock(null);
                StartUpdateWork();
            }
        }

        [ServerOnly]
        public void BuildInternal(Vector3I previewCubeBlockPosition, long owner, long builder, bool requestInstant, long subgridIndex)
        {
            if (!Initialized || Projector.Closed || Subgrids.Count == 0)
                return;

            // Negative values are reserved, ignore them
            if (subgridIndex < 0)
                return;

            // Allow welding only on the first subgrid if the client does not have the MGP plugin installed or an MGP unaware mod sends in a request
            if (subgridIndex >= Subgrids.Count)
                subgridIndex = 0;

            // Find the subgrid to build on
            var subgrid = Subgrids[(int) subgridIndex];
            var previewGrid = subgrid.PreviewGrid;
            if (previewGrid == null)
                return;
            if (!subgrid.HasBuilt)
                return;

            // The subgrid must have a built grid registered already (they are registered as top/base blocks are built)
            MyCubeGrid builtGrid;
            using (subgrid.BuiltGridLock.Read())
            {
                builtGrid = subgrid.BuiltGrid;
            }

            if (builtGrid == null)
                return;

            // Sanity check: The latest known block states must allow for welding the block, ignore the build request if the block is unconfirmed
            if (!subgrid.HasBuildableBlockAtPosition(previewCubeBlockPosition))
                return;

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

            // Allow rebuilding the blueprint without EntityId collisions without power-cycling the projector,
            // relies on the detection of cutting down the built grids by the lack of functional blocks, see
            // where _requestRemap is set to true
            if (_requestRemap && previewFatBlock != null)
            {
                _requestRemap = false;
                PluginLog.Debug($"Remapping blueprint loaded into projector {Projector.CustomName} [{Projector.EntityId}] in preparation for building it again");
                MyEntities.RemapObjectBuilderCollection(GridBuilders);
            }

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

            // Optimization: Fast lookup of blueprint block builders at the expense of some additional memory consumption
            if (!subgrid.TryGetBlockBuilder(previewBlock.Position, out var blockBuilder))
                return;

            // Sanity check: The preview block must match the blueprint block builder both by definition and orientation
            if (!previewBlock.IsMatchingBuilder(blockBuilder))
                return;

            // Clone the block builder to prevent damaging the original blueprint
            blockBuilder = (MyObjectBuilder_CubeBlock) blockBuilder.Clone();

            // Make sure no EntityId collision will occur on re-welding a block on a previously disconnected (split)
            // part of a built subgrid which has not been destroyed (or garbage collected) yet
            if (blockBuilder.EntityId != 0 && MyEntityIdentifier.ExistsById(blockBuilder.EntityId))
            {
                blockBuilder.EntityId = MyEntityIdentifier.AllocateId();
            }

            // Empty inventory, ammo (including already loaded ammo), also clears battery charge (which is wrong, see below)
            blockBuilder.SetupForProjector();
            blockBuilder.ConstructionInventory = null;

            // Reset batteries to default charge
            // FIXME: This does not belong here, it should go into MyObjectBuilder_BatteryBlock.SetupForProjector. Maybe patch that instead!
            if (MyDefinitionManagerBase.Static != null && blockBuilder is MyObjectBuilder_BatteryBlock batteryBuilder)
            {
                var cubeBlockDefinition = (MyBatteryBlockDefinition) MyDefinitionManager.Static.GetCubeBlockDefinition(batteryBuilder);
                batteryBuilder.CurrentStoredPower = cubeBlockDefinition.InitialStoredPowerRatio * cubeBlockDefinition.MaxStoredPower;
            }

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

        [Everywhere]
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
            if (blockAlreadyBuilt?.HasSameDefinition(previewBlock) == true)
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

        [Everywhere]
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

        [ServerOnly]
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
            var definitionGroup = (MyCubeBlockDefinitionGroup) constructorInfo.Invoke(new object[] { });
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

        [ServerOnly]
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

        [Everywhere]
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
            for (var connectedSubgrids = 1; connectedSubgrids < Subgrids.Count;)
            {
                var connectedBefore = connectedSubgrids;

                foreach (var subgrid in Subgrids)
                {
                    if (!subgrid.IsConnectedToProjector || !subgrid.HasBuilt)
                        continue;

                    foreach (var baseConnection in subgrid.BaseConnections.Values)
                    {
                        var topConnection = GetCounterparty(baseConnection, out var topSubgrid);
                        if (topSubgrid.IsConnectedToProjector)
                            continue;

                        if (!IsConnected(baseConnection, topConnection))
                            continue;

                        topSubgrid.IsConnectedToProjector = true;
                        connectedSubgrids += 1;
                        break;
                    }

                    foreach (var topConnection in subgrid.TopConnections.Values)
                    {
                        var baseConnection = GetCounterparty(topConnection, out var baseSubgrid);
                        if (baseSubgrid.IsConnectedToProjector)
                            continue;

                        if (!IsConnected(baseConnection, topConnection))
                            continue;

                        baseSubgrid.IsConnectedToProjector = true;
                        connectedSubgrids += 1;
                        break;
                    }
                }

                if (connectedSubgrids == connectedBefore)
                    break;
            }
        }

        [Everywhere]
        public static void GetObjectBuilderOfProjector(MyProjectorBase projector, bool copy, MyObjectBuilder_CubeBlock blockBuilder)
        {
            if (!copy) return;

            var clipboard = projector.GetClipboard();
            if (clipboard?.CopiedGrids == null || clipboard.CopiedGrids.Count < 2)
                return;

            var gridBuilders = projector.GetOriginalGridBuilders();
            if (gridBuilders == null || gridBuilders.Count != clipboard.CopiedGrids.Count)
                return;

            // Fix the inconsistent remapping the original implementation has done, this is
            // needed to be able to load back the projection properly from a saved world
            var builderCubeBlock = (MyObjectBuilder_ProjectorBase) blockBuilder;
            builderCubeBlock.ProjectedGrids = gridBuilders.Clone();
            MyEntities.RemapObjectBuilderCollection(builderCubeBlock.ProjectedGrids);
        }

        [Everywhere]
        public static void ProjectorInit(MyProjectorBase projector, MyObjectBuilder_CubeBlock objectBuilder)
        {
            if (projector.CubeGrid == null || !projector.AllowWelding)
                return;

            // Projected projector?
            // Prevents ghost subgrids in case of blueprints with nested projections.
            // NOTE: projector.CubeGrid.IsPreview is still false, so don't depend on that!
            if (projector.Physics == null)
                return;

            if (!(objectBuilder is MyObjectBuilder_ProjectorBase projectorBuilder))
                return;

            var gridBuilders = projectorBuilder.ProjectedGrids;
            if (gridBuilders == null || gridBuilders.Count < 1)
                return;

            projector.SetOriginalGridBuilders(gridBuilders);
        }

        [ClientOnly]
        public static bool InitFromObjectBuilder(MyProjectorBase projector, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            if (gridBuilders == null)
                return true;

            // Projector update
            projector.ResourceSink.Update();
            projector.UpdateIsWorking();

            if (!projector.Enabled)
                return true;

            // Fall back to the original implementation to handle failure cases
            if (gridBuilders.Count < 1)
                return true;

            // Is the projector is dead?
            if (!projector.IsWorking)
                return false;

            // Clone and remap the blueprint before modifying it
            gridBuilders = gridBuilders.Clone();
            MyEntities.RemapObjectBuilderCollection(gridBuilders);

            projector.SetHiddenBlock(null);

            // Fixes the multiplayer preview position issue with console blocks (aka hologram table) and now projectors, which caused by damaged
            // first subgrid position. Something is clearing the first subgrid's position, but if we transform the whole blueprint to the origin,
            // then this is not a problem.
            // IMPORTANT: This issue does not appear in single player! Even testing it needs two players in multiplayer setup!
            gridBuilders.NormalizeBlueprintPositionAndOrientation();

            // Console block (aka hologram table)?
            if (!projector.AllowWelding || projector.AllowScaling)
            {
                gridBuilders.PrepareForConsoleProjection(projector.GetClipboard());
                projector.SetOriginalGridBuilders(gridBuilders);
                projector.SendNewBlueprint(gridBuilders);
                return false;
            }

            // Prevent re-initializing an existing multigrid projection
            if (TryFindProjectionByProjector(projector, out _))
                return false;

            // Ensure compatible grid size between the projector and the first subgrid to be built
            var compatibleGridSize = gridBuilders[0].GridSizeEnum == projector.CubeGrid.GridSizeEnum;
            if (!compatibleGridSize)
                return true;

            // Sign up for repair projection auto alignment
            ProjectorsWithBlueprintLoaded.Add(projector.EntityId);

            // Prepare the blueprint for being projected for welding
            gridBuilders.PrepareForProjection();

            // Load the blueprint
            projector.SetOriginalGridBuilders(gridBuilders);

            // Notify the server and all clients (including this one) to create the projection,
            // our data model will be created by SetNewBlueprint the same way at all locations
            projector.SendNewBlueprint(gridBuilders);
            return false;
        }

        [Everywhere]
        public void RemoveProjection(bool keepProjection)
        {
            if (!_keepProjection && _stats.IsBuildCompleted)
                keepProjection = false;

            Projector.SetHiddenBlock(null);
            Projector.SetStatsDirty(true);
            Projector.UpdateText();
            Projector.RaisePropertiesChanged();

            Destroy();

            if (!keepProjection)
            {
                Clipboard.Deactivate();
                Clipboard.Clear();
                Projector.SetOriginalGridBuilders(null);
            }

            Projector.UpdateSounds();

            if (Projector.Enabled)
                Projector.SetEmissiveStateWorking();
            else
                Projector.SetEmissiveStateDisabled();
        }

        [Everywhere]
        public static bool ProjectorUpdateAfterSimulation(MyProjectorBase projector)
        {
            // Create the MultigridProjection instance on demand
            if (!TryFindProjectionByProjector(projector, out var projection))
            {
                if (projector == null ||
                    projector.Closed ||
                    projector.CubeGrid.IsPreview ||
                    !projector.Enabled ||
                    !projector.IsFunctional ||
                    !projector.AllowWelding ||
                    projector.AllowScaling ||
                    projector.Clipboard.PreviewGrids == null ||
                    projector.Clipboard.PreviewGrids.Count == 0)
                    return true;

                var gridBuilders = projector.GetOriginalGridBuilders();
                if (gridBuilders == null || gridBuilders.Count != projector.Clipboard.PreviewGrids.Count)
                    return true;

                projection = Create(projector, gridBuilders);
                if (projection == null)
                    return true;
            }

            // Call the base class implementation
            //projector.UpdateAfterSimulation();
            // Could not call virtual base class method, so copied it here from MyEntity where it is defined:
            projector.GameLogic.UpdateAfterSimulation();

            // Call custom update logic
            projection.UpdateAfterSimulation();

            // Based on the original code

            var projectionTimer = projector.GetProjectionTimer();
            if (!projector.GetTierCanProject() && projectionTimer > 0)
            {
                --projectionTimer;
                projector.SetProjectionTimer(projectionTimer);
                if (projectionTimer == 0)
                    projector.MyProjector_IsWorkingChanged(projector);
            }

            projector.ResourceSink.Update();
            if (projector.GetRemoveRequested())
            {
                if (projector.IsProjecting())
                    projector.RemoveProjection(true);

                projector.SetRemoveRequested(false);
            }

            var clipboard = projector.GetClipboard();
            if (clipboard.IsActive)
            {
                // Client only
                clipboard.Update();
                if (projector.GetShouldResetBuildable())
                {
                    projector.SetShouldResetBuildable(false);
                    projection.ForceUpdateProjection();
                }
            }

            return false;
        }

        [ClientOnly]
        public static bool MyWelder_FindProjectedBlock(MyCasterComponent rayCaster, float distanceMultiplier, ref MyWelder.ProjectionRaycastData result)
        {
            var center = rayCaster.Caster.Center;

            var lookDirection = rayCaster.Caster.FrontPoint - rayCaster.Caster.Center;
            lookDirection.Normalize();

            var distance = MyEngineerToolBase.DEFAULT_REACH_DISTANCE * distanceMultiplier;
            var reachFarPoint = center + lookDirection * distance;
            var viewLine = new LineD(center, reachFarPoint);

            if (!MyCubeGrid.GetLineIntersection(ref viewLine, out var previewGridByWeldingLine, out _, out _, x => x.Projector != null))
                return true;

            if (previewGridByWeldingLine.Projector == null)
                return true;

            // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
            if (!TryFindProjectionByProjector(previewGridByWeldingLine.Projector, out var projection))
                return true;

            projection.FindProjectedBlock(center, reachFarPoint, ref result);
            return false;
        }

        [Everywhere]
        public void RaiseAttachedEntityChanged()
        {
            ForceUpdateProjection();
        }
    }
}