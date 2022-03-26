using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace MultigridProjector.Logic
{
    // FIXME: Refactor this class
    public class MultigridProjection
    {
        private static readonly RwLockDictionary<long, MultigridProjection> Projections = new RwLockDictionary<long, MultigridProjection>();
        private static readonly HashSet<long> ProjectorsWithBlueprintLoaded = new HashSet<long>();

        internal readonly MyProjectorBase Projector;
        internal readonly List<MyObjectBuilder_CubeGrid> GridBuilders;
        private readonly MyProjectorClipboard clipboard;

        // Subgrids of the projection, associated built grids as they appear, block status information, statistics
        private readonly List<Subgrid> subgrids = new List<Subgrid>();
        private readonly RwLock subgridsLock = new RwLock();

        public Subgrid[] GetSupportedSubgrids()
        {
            using (subgridsLock.Read())
                return SupportedSubgrids.ToArray();
        }

        // Subgrids supported for welding from projection, skips grids connected via connector (ships, missiles)
        private IEnumerable<Subgrid> SupportedSubgrids => subgrids.Where(s => s.Supported);

        // Bidirectional mapping of corresponding base and top blocks by their grid index and min cube positions
        internal readonly Dictionary<BlockMinLocation, BlockMinLocation> BlueprintConnections = new Dictionary<BlockMinLocation, BlockMinLocation>();

        // Preview base blocks by their grid index and min position
        internal readonly Dictionary<BlockMinLocation, MyMechanicalConnectionBlockBase> PreviewBaseBlocks = new Dictionary<BlockMinLocation, MyMechanicalConnectionBlockBase>();

        // Preview top blocks by their grid index and min position
        internal readonly Dictionary<BlockMinLocation, MyAttachableTopBlockBase> PreviewTopBlocks = new Dictionary<BlockMinLocation, MyAttachableTopBlockBase>();

        // Locking GridBuilders only while remapping Entity IDs or depending on their consistency
        internal int GridCount => GridBuilders.Count;
        internal List<MyCubeGrid> PreviewGrids => clipboard.PreviewGrids;
        internal bool Initialized { get; private set; }

        private bool IsUpdateRequested => Initialized && subgrids.Any(subgrid => subgrid.IsUpdateRequested);
        private bool IsBuildCompleted => Initialized && stats.IsBuildCompleted;

        // Latest aggregated statistics, suitable for built completeness decision and formatting as text
        private readonly ProjectionStats stats = new ProjectionStats();

        // Background task to update the block states and collect welding completion statistics
        private MultigridUpdateWork updateWork;

        // Keep projection flag saved for change detection
        private bool latestKeepProjection;

        // Show only buildable flag saved for change detection
        private bool latestShowOnlyBuildable;

        // Offset and rotation for change detection
        private Vector3I latestProjectionOffset;
        private Vector3I latestProjectionRotation;

        // Scan sequence number, increased every time the preview grids are successfully scanned for block changes
        internal long ScanNumber;
        private bool HasScanned => ScanNumber > 0;

        // YAML generated from the current state, cleared when the scan number changes
        private string latestYaml;

        // Requests a remap operation on building the next functional (non-armor) block
        private bool requestRemap;

        // Controls when the plugin and Mod API can access projector information already
        internal bool IsValidForApi => Initialized && HasScanned;

        public static void EnsureNoProjections()
        {
            int projectionCount;
            using (Projections.Read())
                projectionCount = Projections.Count;

            if (projectionCount != 0)
                PluginLog.Warn($"{projectionCount} projections are active, but there should be none!");
        }

        private static MultigridProjection Create(MyProjectorBase projector, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            using (Projections.Write())
            {
                if (Projections.ContainsKey(projector.EntityId))
                    return null;
            }

            var projection = new MultigridProjection(projector, gridBuilders);

            using (Projections.Write())
                Projections[projector.EntityId] = projection;

            return projection;
        }

        private MultigridProjection(MyProjectorBase projector, List<MyObjectBuilder_CubeGrid> gridBuilders)
        {
            Projector = projector;
            GridBuilders = gridBuilders;
            clipboard = projector.GetClipboard();

            latestKeepProjection = Projector.GetKeepProjection();
            latestShowOnlyBuildable = Projector.GetShowOnlyBuildable();

            latestProjectionOffset = Projector.ProjectionOffset;
            latestProjectionRotation = Projector.ProjectionRotation;

            if (Projector.Closed)
                return;

            lock (GridBuilders)
            {
                MapBlueprintBlocks();
                MapPreviewBlocks();
                CreateSubgrids();
                MarkSupportedSubgrids();
            }

            CreateUpdateWork();

            Projector.PropertiesChanged += OnPropertiesChanged;

            Initialized = true;

            foreach (var subgrid in SupportedSubgrids)
                subgrid.RequestUpdate();

            ForceUpdateProjection();

            AutoAlignBlueprint();

            MultigridProjectorApiProvider.RegisterProgrammableBlockApi();
        }

        private void Destroy()
        {
            using (Projections.Write())
                Projections.Remove(Projector.EntityId);

            if (!Initialized)
                return;

            Initialized = false;

            Projector.PropertiesChanged -= OnPropertiesChanged;

            updateWork.OnUpdateWorkCompleted -= OnUpdateWorkCompletedWithErrorHandler;
            updateWork.Dispose();
            updateWork = null;

            stats.Clear();

            foreach (var subgrid in subgrids)
                subgrid.Dispose();

            using (subgridsLock.Write())
                subgrids.Clear();
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
                            PreviewBaseBlocks[new BlockMinLocation(gridIndex, slimBlock.Min)] = baseBlock;
                            break;
                        case MyAttachableTopBlockBase topBlock:
                            PreviewTopBlocks[new BlockMinLocation(gridIndex, slimBlock.Min)] = topBlock;
                            break;
                    }
                }
            }
        }

        private void CreateSubgrids()
        {
            subgrids.Add(new ProjectorSubgrid(this));

            for (var gridIndex = 1; gridIndex < GridCount; gridIndex++)
                subgrids.Add(new Subgrid(this, gridIndex));

            subgrids[0].RegisterBuiltGrid(Projector.CubeGrid);
        }

        private void MarkSupportedSubgrids()
        {
            subgrids[0].Supported = true;

            foreach (var _ in subgrids)
            {
                var modified = 0;

                foreach (var subgrid in subgrids)
                {
                    if (!subgrid.Supported)
                        continue;

                    foreach (var baseConnection in subgrid.BaseConnections.Values)
                    {
                        var topSubgrid = subgrids[baseConnection.TopLocation.GridIndex];
                        if (topSubgrid.Supported)
                            continue;

                        topSubgrid.Supported = true;
                        modified++;
                        break;
                    }

                    foreach (var topConnection in subgrid.TopConnections.Values)
                    {
                        var baseSubgrid = subgrids[topConnection.BaseLocation.GridIndex];
                        if (baseSubgrid.Supported)
                            continue;

                        baseSubgrid.Supported = true;
                        modified++;
                        break;
                    }
                }

                if (modified == 0)
                    break;
            }
        }

        private void CreateUpdateWork()
        {
            updateWork = new MultigridUpdateWork(this);
            updateWork.OnUpdateWorkCompleted += OnUpdateWorkCompletedWithErrorHandler;
        }

        private void AutoAlignBlueprint()
        {
            // This condition will be True only on the client which loaded the blueprint
            if (!ProjectorsWithBlueprintLoaded.Contains(Projector.EntityId))
                return;

            ProjectorsWithBlueprintLoaded.Remove(Projector.EntityId);

            if (Projector.AlignToRepairProjector(GridBuilders[0]))
                PluginLog.Debug($"Aligned repair projection loaded into {Projector.GetDebugName()}");
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
                return Projections.TryGetValue(projectorId, out projection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFindSubgrid(long projectorId, int subgridIndex, out MultigridProjection projection, out Subgrid subgrid)
        {
            using (Projections.Read())
            {
                if (!Projections.TryGetValue(projectorId, out projection))
                {
                    subgrid = null;
                    return false;
                }
            }

            if (!projection.Initialized)
            {
                subgrid = null;
                return false;
            }

            using (projection.subgridsLock.Read())
            {
                if (subgridIndex < 0 || subgridIndex >= projection.GridCount)
                {
                    subgrid = null;
                    return false;
                }

                subgrid = projection.subgrids[subgridIndex];
            }

            if (!subgrid.Supported)
            {
                subgrid = null;
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFindPreviewGrid(MyCubeGrid grid, out int gridIndex)
        {
            if (!Initialized)
            {
                gridIndex = 0;
                return false;
            }

            gridIndex = PreviewGrids.IndexOf(grid);

            return gridIndex >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFindProjectionByBuiltGrid(MyCubeGrid grid, out MultigridProjection projection, out Subgrid subgrid)
        {
            using (Projections.Read())
                projection = Projections.Values.FirstOrDefault(p => p.TryFindSubgridByBuiltGrid(grid, out _));

            if (projection == null)
            {
                subgrid = null;
                return false;
            }

            if (!projection.Initialized)
            {
                subgrid = null;
                return false;
            }

            using (projection.subgridsLock.Read())
                return projection.TryFindSubgridByBuiltGrid(grid, out subgrid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFindSubgridByBuiltGrid(MyCubeGrid grid, out Subgrid subgrid)
        {
            subgrid = SupportedSubgrids.FirstOrDefault(s => s.BuiltGrid == grid);
            return subgrid != null;
        }

        private void StartUpdateWork()
        {
            if (Projector.Closed)
                return;

            if (!Projector.Enabled)
            {
                HidePreviewGrids();
                return;
            }

            if (!updateWork.IsComplete)
                return;

            Projector.SetShouldUpdateProjection(false);
            Projector.SetForceUpdateProjection(false);

            updateWork.Start();
        }

        private void HidePreviewGrids()
        {
            if (Sync.IsDedicated)
                return;

            foreach (var subgrid in subgrids)
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
            if (latestKeepProjection == keepProjection) return;
            latestKeepProjection = keepProjection;

            // Remove projection if the build is complete and Keep Projection is unchecked
            if (!keepProjection && IsBuildCompleted)
                Projector.RequestRemoveProjection();
        }

        private void DetectShowOnlyBuildableChange()
        {
            var showOnlyBuildable = Projector.GetShowOnlyBuildable();
            if (latestShowOnlyBuildable == showOnlyBuildable) return;
            latestShowOnlyBuildable = showOnlyBuildable;

            UpdatePreviewBlockVisuals();
        }

        private void DetectOffsetRotationChange()
        {
            if (Projector.ProjectionOffset == latestProjectionOffset &&
                Projector.ProjectionRotation == latestProjectionRotation)
                return;

            latestProjectionOffset = Projector.ProjectionOffset;
            latestProjectionRotation = Projector.ProjectionRotation;

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

            ScanNumber++;
            latestYaml = null;

#if DEBUG
            PluginLog.Debug($"Scan #{ScanNumber} of {Projector.GetDebugName()}: {updateWork.SubgridsScanned} subgrids, {updateWork.BlocksScanned} blocks");
#endif

            Projector.SetLastUpdate(MySandboxGame.TotalGamePlayTimeInMilliseconds);

            // Clients must follow replicated grid changes from the server, therefore they need regular updates
            // FIXME: Optimize this case by listening on grid/block change events somehow
            if (!Sync.IsServer)
                ShouldUpdateProjection();

            clipboard.HasPreviewBBox = false;

            UpdateMechanicalConnections();
            AggregateStatistics();
            UpdateProjectorStats();

            if (Sync.IsServer && stats.BuiltOnlyArmorBlocks)
                requestRemap = true;

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

            if (!latestKeepProjection && IsBuildCompleted)
                Projector.RequestRemoveProjection();
        }

        public string GetYaml(bool requireScan = true)
        {
            using (subgridsLock.Read())
                return GetYamlUnsafe(requireScan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetYamlUnsafe(bool requireScan)
        {
            if (requireScan && !HasScanned)
                return "";

            var yaml = latestYaml;
            if (yaml != null)
                return yaml;

            var sb = new StringBuilder();
            sb.AppendLine($"ProjectorEntityId: {Projector.EntityId}");
            sb.AppendLine($"ProjectorName: {Projector.GetSafeName()}");
            sb.AppendLine($"ScanNumber: {ScanNumber}");
            sb.AppendLine($"SubgridCount: {subgrids.Count}");
            sb.AppendLine($"Subgrids:");
            foreach (var subgrid in subgrids)
            {
                if (!subgrid.Supported)
                    continue;

                sb.AppendLine($"  Index: {subgrid.Index}");
                sb.AppendLine($"  GridSize: {subgrid.PreviewGrid.GridSizeEnum}");
                sb.AppendLine($"  PreviewGridEntityId: {subgrid.PreviewGrid.EntityId}");
                using (subgrid.BuiltGridLock.Read())
                {
                    sb.AppendLine($"  HasBuilt: {subgrid.HasBuilt}");
                    sb.AppendLine($"  BuiltGridEntityId: {(subgrid.HasBuilt ? subgrid.BuiltGrid.EntityId : 0)}");
                }

                using (subgrid.BlocksLock.Read())
                {
                    sb.AppendLine($"  BlockCount: {subgrid.Blocks.Count}");
                    sb.AppendLine($"  Blocks:");
                    foreach (var (position, block) in subgrid.Blocks)
                    {
                        sb.AppendLine($"    - Block: {block.SlimBlock?.FatBlock?.EntityId ?? 0}");
                        sb.AppendLine($"      State: {block.State}");
                        sb.AppendLine($"      Position: [{position.FormatYaml()}]");
                        if (subgrid.BaseConnections.TryGetValue(position, out var baseConnection))
                        {
                            sb.AppendLine($"      TopSubgrid: {baseConnection.TopLocation.GridIndex}");
                            sb.AppendLine($"      TopPosition: {baseConnection.TopLocation.Position}");
                            sb.AppendLine($"      IsConnected: {IsConnected(baseConnection)}");
                        }
                        else if (subgrid.TopConnections.TryGetValue(position, out var topConnection))
                        {
                            sb.AppendLine($"      BaseSubgrid: {topConnection.BaseLocation.GridIndex}");
                            sb.AppendLine($"      BasePosition: {topConnection.BaseLocation.Position}");
                            sb.AppendLine($"      IsConnected: {IsConnected(topConnection)}");
                        }
                    }
                }
            }

            yaml = sb.ToString();
            latestYaml = yaml;

            return yaml;
        }

        private void AggregateStatistics()
        {
            stats.Clear();
            foreach (var subgrid in SupportedSubgrids)
                stats.Add(subgrid.Stats);
        }

        [Everywhere]
        public void UpdateProjectorStats()
        {
            Projector.SetTotalBlocks(stats.TotalBlocks);
            Projector.SetRemainingBlocks(stats.RemainingBlocks);
            Projector.SetRemainingArmorBlocks(stats.RemainingArmorBlocks);
            Projector.SetBuildableBlocksCount(stats.BuildableBlocks);
            Projector.GetRemainingBlocksPerType().Update(stats.RemainingBlocksPerType);
            Projector.SetStatsDirty(true);
        }

        private void UpdatePreviewBlockVisuals()
        {
            if (Sync.IsDedicated)
                return;

            foreach (var subgrid in SupportedSubgrids)
                subgrid.UpdatePreviewBlockVisuals(Projector, latestShowOnlyBuildable);
        }

        // FIXME: Refactor, simplify
        private void UpdateGridTransformations()
        {
            // Align the preview grids to match any grids have already been built, align the rest of grids relative to other preview grids

            if (!Initialized || PreviewGrids == null || PreviewGrids.Count == 0 || subgrids.Count != PreviewGrids.Count)
                return;

            // Mark all subgrids as not positioned
            foreach (var subgrid in SupportedSubgrids)
                subgrid.Positioned = false;

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
            subgrids[0].Positioned = true;

            // Further subgrids
            var gridsToPositionBefore = 0;
            var gridsToPosition = SupportedSubgrids.Count(s => !s.Positioned);
            for (var i = 1; i < subgrids.Count; i++)
            {
                if (gridsToPosition == 0 || gridsToPosition == gridsToPositionBefore)
                    break;

                gridsToPositionBefore = gridsToPosition;

                foreach (var subgrid in SupportedSubgrids)
                {
                    if (subgrid.Positioned)
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
                        subgrid.Positioned = true;
                        gridsToPosition--;
                        continue;
                    }

                    // Align the preview to an already built base block
                    var baseConnection = subgrid.BaseConnections.Values.FirstOrDefault(IsConnected);
                    if (baseConnection != null && !baseConnection.Block.CubeGrid.Closed)
                    {
                        baseConnection.Preview.AlignGrid(baseConnection.Block);
                        subgrid.Positioned = true;
                        gridsToPosition--;
                        continue;
                    }

                    // Align the preview by top block connecting to an already positioned base block preview
                    topConnection = subgrid.TopConnections.Values.FirstOrDefault(c => subgrids[c.BaseLocation.GridIndex].Positioned);
                    if (topConnection != null)
                    {
                        var baseSubgrid = subgrids[topConnection.BaseLocation.GridIndex];
                        if (baseSubgrid.GridBuilder.PositionAndOrientation != null)
                        {
                            gridBuilder.PositionAndOrientation.Value.ToMatrixD(out var topMatrix);
                            baseSubgrid.GridBuilder.PositionAndOrientation.Value.ToMatrixD(out var baseMatrix);
                            var wm = topMatrix * MatrixD.Invert(baseMatrix) * baseSubgrid.PreviewGrid.PositionComp.WorldMatrixRef;
                            subgrid.PreviewGrid.PositionComp.SetWorldMatrix(ref wm, skipTeleportCheck: true);
                            subgrid.Positioned = true;
                            gridsToPosition--;
                            continue;
                        }
                    }

                    // Align the preview by base block connecting to an already positioned top block preview
                    baseConnection = subgrid.BaseConnections.Values.FirstOrDefault(c => subgrids[c.TopLocation.GridIndex].Positioned);
                    if (baseConnection != null)
                    {
                        var topSubgrid = subgrids[baseConnection.TopLocation.GridIndex];
                        if (topSubgrid.GridBuilder.PositionAndOrientation != null)
                        {
                            gridBuilder.PositionAndOrientation.Value.ToMatrixD(out var topMatrix);
                            topSubgrid.GridBuilder.PositionAndOrientation.Value.ToMatrixD(out var baseMatrix);
                            var wm = topMatrix * MatrixD.Invert(baseMatrix) * topSubgrid.PreviewGrid.PositionComp.WorldMatrixRef;
                            subgrid.PreviewGrid.PositionComp.SetWorldMatrix(ref wm, skipTeleportCheck: true);
                            subgrid.Positioned = true;
                            gridsToPosition--;
                            // ReSharper disable once RedundantJumpStatement
                            continue;
                        }
                    }

                    // This subgrid cannot be positioned yet, let's try again in a later iteration
                    // PluginLog.Debug($"Subgrid #{subgrid.Index} could not be positioned at loop #{i}");
                }
            }

            if (gridsToPosition == 0)
                return;

            foreach (var subgrid in SupportedSubgrids.Where(s => !s.Positioned))
                PluginLog.Error($"Subgrid #{subgrid.Index} could not be positioned!");

            var yaml = GetYaml(false);
            PluginLog.Error($"Projection with the above problem:\r\n{yaml}");
            ((IMyProjector)Projector).Enabled = false;
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
            if (!Initialized || Projector.Closed || subgrids.Count < 1)
                return;

            foreach (var subgrid in SupportedSubgrids)
            {
                subgrid.UnregisterBuiltGrid();
            }

            subgrids[0].RegisterBuiltGrid(Projector.CubeGrid);

            ForceUpdateProjection();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TopConnection GetCounterparty(BaseConnection baseConnection, out Subgrid topSubgrid)
        {
            topSubgrid = subgrids[baseConnection.TopLocation.GridIndex];
            return topSubgrid.TopConnections[baseConnection.TopLocation.Position];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BaseConnection GetCounterparty(TopConnection topConnection, out Subgrid baseSubgrid)
        {
            baseSubgrid = subgrids[topConnection.BaseLocation.GridIndex];
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
            foreach (var subgrid in SupportedSubgrids)
            {
                if (subgrid.HasBuilt && !subgrid.IsConnectedToProjector)
                    subgrid.UnregisterBuiltGrid();
            }
        }

        private void UpdateMechanicalConnections()
        {
            if (subgrids.Count < 1)
                return;

            foreach (var subgrid in SupportedSubgrids)
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
            if (!Sync.IsServer || !baseConnection.HasBuilt || baseConnection.Block.TopBlock != null || !baseConnection.Block.IsFunctional)
                return;

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
            if (topConnection.HasBuilt || baseConnection.Block?.TopBlock == null)
                return;

            topConnection.Block = baseConnection.Block.TopBlock;
            topConnection.Found = topConnection.Block;

            ForceUpdateProjection();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTopConnection(Subgrid topSubgrid, TopConnection topConnection)
        {
            var baseConnection = GetCounterparty(topConnection, out var baseSubgrid);

            FindNewlyBuiltTop(topConnection);
            FindNewlyAddedBase(topConnection, baseConnection);
            BuildMissingBase(topConnection, topSubgrid);
            RegisterConnectedSubgrid(baseSubgrid, baseConnection, topConnection, topSubgrid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FindNewlyAddedBase(TopConnection topConnection, BaseConnection baseConnection)
        {
            if (baseConnection.HasBuilt || topConnection.Block?.Stator == null)
                return;

            baseConnection.Block = topConnection.Block.Stator;
            baseConnection.Found = baseConnection.Block;

            ForceUpdateProjection();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FindNewlyBuiltTop(TopConnection topConnection)
        {
            if (topConnection.HasBuilt || topConnection.Found == null)
                return;

            topConnection.Block = topConnection.Found;
        }

        private void BuildMissingBase(TopConnection topConnection, Subgrid topSubgrid)
        {
            if (!Sync.IsServer || !topConnection.HasBuilt || topConnection.Block.Stator != null)
                return;

            // Create base of right size
            var baseConnection = GetCounterparty(topConnection, out var baseSubgrid);
            if (baseSubgrid.HasBuilt)
                return;

            // Create base grid builder
            var baseGridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            baseGridBuilder.GridSizeEnum = baseConnection.Preview.CubeGrid.GridSizeEnum;
            baseGridBuilder.IsStatic = baseConnection.Preview.CubeGrid.IsStatic;
            baseGridBuilder.PositionAndOrientation = new MyPositionAndOrientation(baseConnection.Preview.CubeGrid.WorldMatrix);

            // Optimization: Fast lookup of blueprint block builders at the expense of some additional memory consumption
            if (!baseSubgrid.TryGetBlockBuilder(baseConnection.Preview.Position, out var baseBlockBuilder))
                return;

            // Sanity check: The preview block must match the blueprint block builder both by definition and orientation
            if (!baseConnection.Preview.SlimBlock.IsMatchingBuilder(baseBlockBuilder))
                return;

            // Clone the block builder to prevent damaging the original blueprint
            baseBlockBuilder = (MyObjectBuilder_CubeBlock)baseBlockBuilder.Clone();

            // Make sure no EntityId collision will occur on re-welding a block on a previously disconnected (split)
            // part of a built subgrid which has not been destroyed (or garbage collected) yet
            if (baseBlockBuilder.EntityId != 0 && MyEntityIdentifier.ExistsById(baseBlockBuilder.EntityId))
            {
                baseBlockBuilder.EntityId = MyEntityIdentifier.AllocateId();
            }

            // Empty inventory, ammo (including already loaded ammo), also clears battery charge (which is wrong, see below)
            baseBlockBuilder.SetupForProjector();
            baseBlockBuilder.ConstructionInventory = null;

            // Ownership is determined by the projector's grid, not by who is welding the block
            baseBlockBuilder.BuiltBy = Projector.OwnerId;

            // Add base block as the first block of the new grid
            baseGridBuilder.CubeBlocks.Add(baseBlockBuilder);

            // Create the actual base grid
            var baseGrid = MyAPIGateway.Entities.CreateFromObjectBuilder(baseGridBuilder) as MyCubeGrid;
            if (baseGrid == null)
                return;
            MyEntities.Add(baseGrid);

            // Attach the existing top part to the newly created base
            var baseBlock = baseGrid.CubeBlocks.First().FatBlock as MyMechanicalConnectionBlockBase;
            baseSubgrid.AddBlockToGroups(baseBlock);
            try
            {
                baseBlock.Attach(topConnection.Block);
            }
            catch (TargetInvocationException e)
            {
                /* FIXME:

Dirty hack to ignore the exception inside Attach.

Maybe this is the reason why there is no "Attach Head" on pistons?

System.NullReferenceException: Object reference not set to an instance of an object.
   at Sandbox.Game.Entities.Blocks.MyPistonBase.GetTopMatrixLocal()
   at Sandbox.Game.Entities.Blocks.MyPistonBase.FillFixedData()
   at Sandbox.Game.Entities.Blocks.MyPistonBase.CreateConstraint(MyAttachableTopBlockBase topBlock)
   at Sandbox.Game.Entities.Blocks.MyPistonBase.Attach(MyAttachableTopBlockBase topBlock, Boolean updateGroup)
   --- End of inner exception stack trace ---
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Object[] arguments, Signature sig, Boolean constructor)
   at System.Reflection.RuntimeMethodInfo.UnsafeInvokeInternal(Object obj, Object[] parameters, Object[] arguments)
   at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at System.Reflection.MethodBase.Invoke(Object obj, Object[] parameters)
   at MultigridProjector.Extensions.MyMechanicalConnectionBlockBaseExtensions.Attach(MyMechanicalConnectionBlockBase obj, MyAttachableTopBlockBase topBlock, Boolean updateGroup) in C:\Dev\multigrid-projector\MultigridProjector\Extensions\MyMechanicalConnectionBlockBaseExtensions.cs:line 19
   at MultigridProjector.Logic.MultigridProjection.BuildMissingBase(TopConnection topConnection, Subgrid topSubgrid) in C:\Dev\multigrid-projector\MultigridProjector\Logic\MultigridProjection.cs:line 925
                */
                if (!e.ToString().Contains("System.NullReferenceException"))
                    throw;

                PluginLog.Warn($"Ignored System.NullReferenceException in Attach called from BuildMissingBase: projector.DebugName = \"{Projector.DebugName}\", baseSubgrid.Index = {baseSubgrid.Index}, baseBlock.Position = {baseBlock?.Position}, baseBlock.DebugName = \"{baseBlock?.DebugName}\"");
            }

            // Sanity check
            if (baseBlock?.TopBlock == null)
            {
                baseGrid.Close();
                return;
            }

            // Register base subgrid
            baseConnection.Found = baseBlock;
            baseConnection.Block = baseBlock;
            RegisterConnectedSubgrid(baseSubgrid, baseConnection, topConnection, topSubgrid);

            // Need to try again every 2 seconds, because building the base part may fail due to objects in the way
            ShouldUpdateProjection();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterConnectedSubgrid(Subgrid baseSubgrid, BaseConnection baseConnection, TopConnection topConnection, Subgrid topSubgrid)
        {
            var bothBaseAndTopAreBuilt = baseConnection.HasBuilt && topConnection.HasBuilt;
            if (!bothBaseAndTopAreBuilt)
                return;

            if (baseConnection.Block.TopBlock == null && topConnection.Block.Stator == null && baseConnection.RequestAttach)
            {
                baseConnection.RequestAttach = false;
                if (baseConnection.HasBuilt && topConnection.HasBuilt)
                    baseConnection.Block.CallAttach();
                return;
            }

            if (!baseSubgrid.HasBuilt)
            {
                var loneBasePart = !baseConnection.IsWheel && baseConnection.Block.CubeGrid.CubeBlocks.Count == 1;
                if (loneBasePart)
                    ConfigureBaseToMatchTop(baseConnection);

                baseSubgrid.RegisterBuiltGrid(baseConnection.Block.CubeGrid);
                return;
            }

            // topSubgrid.HasBuilt must be true here
            if (topSubgrid.HasBuilt)
                return;

            var loneTopPart = !topConnection.IsWheel && topConnection.Block.CubeGrid.CubeBlocks.Count == 1;
            if (loneTopPart && (topConnection.Block.CubeGrid.GridSizeEnum != topSubgrid.GridSizeEnum || !baseConnection.Block.IsFunctional))
            {
                if (!Sync.IsServer)
                {
                    ShouldUpdateProjection();
                    return;
                }

                // Remove head if the grid size is wrong or if the base is not functional yet.
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

            if (loneTopPart)
            {
                topConnection.Block.AlignGrid(topConnection.Preview);
                ConfigureBaseToMatchTop(baseConnection);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ConfigureBaseToMatchTop(BaseConnection baseConnection)
        {
            switch (baseConnection.Block)
            {
                case MyPistonBase pistonBase:
                    pistonBase.SetCurrentPosByTopGridMatrix();
                    break;

                case MyMotorStator motorStator:
                    motorStator.SetAngleToPhysics();
                    motorStator.SetValueFloat("Displacement", ((MyMotorStator)baseConnection.Preview).DummyDisplacement);
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

            if (updateWork == null || !updateWork.IsComplete)
                return;

            if (IsUpdateRequested)
                ForceUpdateProjection();

            var shouldUpdateProjection = Projector.GetShouldUpdateProjection() && MySandboxGame.TotalGamePlayTimeInMilliseconds - Projector.GetLastUpdate() >= 2000;
            if (shouldUpdateProjection)
            {
                foreach (var subgrid in SupportedSubgrids)
                    subgrid.RequestUpdate();
            }

            if (shouldUpdateProjection || Projector.GetForceUpdateProjection())
            {
                Projector.SetHiddenBlock(null);
                StartUpdateWork();
            }
        }

        [ServerOnly]
        public void BuildInternal(Vector3I previewCubeBlockPosition, long owner, long builder, bool requestInstant, long builtBy)
        {
            if (!Initialized || Projector.Closed)
                return;

            // Allow welding only on the first subgrid if the client does not have the MGP plugin installed or an MGP unaware mod sends in a request
            var subgridIndex = builtBy >= 0 && builtBy < GridCount ? (int)builtBy : 0;

            // Find the subgrid to build on
            Subgrid subgrid;
            using (subgridsLock.Read())
            {
                if (subgridIndex >= subgrids.Count)
                    return;

                subgrid = subgrids[subgridIndex];
            }

            // Preview (projected) grid
            var previewGrid = subgrid.PreviewGrid;
            if (previewGrid == null)
                return;

            // The subgrid must have a built grid registered already (they are registered as top/base blocks are built)
            MyCubeGrid builtGrid;
            using (subgrid.BuiltGridLock.Read())
            {
                builtGrid = subgrid.BuiltGrid;
                if (builtGrid == null)
                    return;
            }

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
                myMultiplayerServerBase?.ValidationFailed(MyEventContext.Current.Sender.Value, false, stackTrace: false,
                    additionalInfo:
                    $"MultigridProjection.BuildInternal: previewCubeBlockPosition={previewCubeBlockPosition}; owner={owner}; builder={builder}; requestInstant={requestInstant}; builtBy={builtBy}; subgridIndex={subgridIndex}; previewBlock={previewBlock}; Projector.AllowWelding={Projector.AllowWelding}");
                return;
            }

            // Transform from the preview grid's coordinates to the grid being build (they don't match exactly for the projecting grid)
            // FIXME: Potential optimization opportunity on the non-projecting grids

            var previewFatBlock = previewBlock.FatBlock;

            // Allow rebuilding the blueprint without EntityId collisions without power-cycling the projector,
            // relies on the detection of cutting down the built grids by the lack of functional blocks, see
            // where requestRemap is set to true
            if (requestRemap && previewFatBlock != null)
            {
                requestRemap = false;
                PluginLog.Debug($"Remapping blueprint loaded into projector {Projector.CustomName} [{Projector.EntityId}] in preparation for building it again");
                lock (GridBuilders)
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
            blockBuilder = (MyObjectBuilder_CubeBlock)blockBuilder.Clone();

            // Make sure no EntityId collision will occur on re-welding a block on a previously disconnected (split)
            // part of a built subgrid which has not been destroyed (or garbage collected) yet
            if (blockBuilder.EntityId != 0 && MyEntityIdentifier.ExistsById(blockBuilder.EntityId))
            {
                blockBuilder.EntityId = MyEntityIdentifier.AllocateId();
            }

            // Empty inventory, ammo (including already loaded ammo), also clears battery charge (which is wrong, see below)
            blockBuilder.ConstructionInventory = null;

            // Reset batteries to default charge
            // FIXME: This does not belong here, it should go into MyObjectBuilder_BatteryBlock.SetupForProjector. Maybe patch that instead!
            if (MyDefinitionManagerBase.Static != null && blockBuilder is MyObjectBuilder_BatteryBlock batteryBuilder)
            {
                var cubeBlockDefinition = (MyBatteryBlockDefinition)MyDefinitionManager.Static.GetCubeBlockDefinition(batteryBuilder);
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
            builtGrid.BuildBlockRequestInternal(visuals, location, blockBuilder, builder, instantBuild, owner, MyEventContext.Current.IsLocallyInvoked ? steamId : MyEventContext.Current.Sender.Value, isProjection: true);
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

            // Subgrid
            Subgrid subgrid;
            using (subgridsLock.Read())
            {
                if (gridIndex < 0 || gridIndex >= subgrids.Count)
                    return BuildCheckResult.NotFound;

                subgrid = subgrids[gridIndex];
                if (!subgrid.Supported)
                    return BuildCheckResult.NotWeldable;
            }

            // Must have a built grid
            MyCubeGrid builtGrid;
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

            var gridPlacementSettings = new MyGridPlacementSettings { SnapMode = SnapMode.OneFreeAxis };
            if (MyCubeGrid.TestPlacementAreaCube(builtGrid, ref gridPlacementSettings, min, max, previewBlock.Orientation, previewBlock.BlockDefinition, ignoredEntity: builtGrid, isProjected: true))
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

        private const int MaxHandWeldableCubes = 32;

        private class FindProjectedBlockLocals
        {
            public readonly MyCube[] Cubes = new MyCube[MaxHandWeldableCubes];
            public readonly double[] Distances = new double[MaxHandWeldableCubes];
        }

        private readonly ThreadLocal<FindProjectedBlockLocals> findProjectedBlockLocals = new ThreadLocal<FindProjectedBlockLocals>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FindProjectedBlock(Vector3D center, Vector3D reachFarPoint, ref MyWelder.ProjectionRaycastData raycastData)
        {
            if (!Initialized || Projector.Closed)
                return;

            // Allocate the working arrays only once per thread to avoid allocations
            var locals = findProjectedBlockLocals.Value ?? (findProjectedBlockLocals.Value = new FindProjectedBlockLocals());
            var cubes = locals.Cubes;
            var distances = locals.Distances;

            // Get intersecting blocks from all preview grids
            var count = 0;
            using (subgridsLock.Read())
            {
                foreach (var subgrid in SupportedSubgrids)
                {
                    if (!subgrid.HasBuilt)
                        continue;

                    var hitCubes = subgrid.PreviewGrid.RayCastBlocksAllOrdered(center, reachFarPoint);
                    foreach (var cube in hitCubes)
                    {
                        cubes[count++] = cube;
                        if (count == MaxHandWeldableCubes)
                            break;
                    }

                    if (count == MaxHandWeldableCubes)
                        break;
                }
            }

            // Measure the squared distances of intersected cubes
            for (var i = 0; i < count; i++)
                distances[i] = -(cubes[i].CubeBlock.WorldPosition - center).LengthSquared();

            // Sort cubes in place by distance from farthest to closest,
            // the reverse order is due to negating the distance above
            Array.Sort(distances, cubes, 0, count);

            // Find the first one which can be built
            for (var i = 0; i < count; i++)
            {
                var slimBlock = cubes[i].CubeBlock;
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

            // Remove references to the cubes to allow for GC to clean them later
            for (var i = 0; i < count; i++)
                cubes[i] = null;
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
            var constructorInfo = Validation.EnsureInfo(AccessTools.Constructor(typeof(MyCubeBlockDefinitionGroup)));
            var definitionGroup = (MyCubeBlockDefinitionGroup)constructorInfo.Invoke(new object[] { });
            definitionGroup[MyCubeSize.Small] = topDefinition;
            definitionGroup[MyCubeSize.Large] = topDefinition;

            // Create the top part
            var instantBuild = Projector.GetInstantBuildingEnabled();
            var sizeConversion = baseConnection.Preview.CubeGrid.GridSizeEnum != topConnection.Preview.CubeGrid.GridSizeEnum;
            try
            {
                var topBlock = baseBlock.CreateTopPart(definitionGroup, sizeConversion, instantBuild);
                if (topBlock == null)
                    return false;

                // Attach to the base
                baseBlock.Attach(topBlock);
            }
            catch (TargetInvocationException e)
            {
                /* FIXME:

Dirty hack to ignore the exception inside Attach.

Maybe this is the reason why there is no "Attach Head" on pistons?

System.NullReferenceException: Object reference not set to an instance of an object.
   at Sandbox.Game.Entities.Blocks.MyPistonBase.CanPlaceTop(MyAttachableTopBlockBase topBlock, Int64 builtBy)
   at Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase.CreateTopPart_Patch0(MyMechanicalConnectionBlockBase this, MyAttachableTopBlockBase& topBlock, Int64 builtBy, MyCubeBlockDefinitionGroup topGroup, Boolean smallToLarge, Boolean instantBuild)
   --- End of inner exception stack trace ---
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Object[] arguments, Signature sig, Boolean constructor)
   at System.Reflection.RuntimeMethodInfo.UnsafeInvokeInternal(Object obj, Object[] parameters, Object[] arguments)
   at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at MultigridProjector.Extensions.MyMechanicalConnectionBlockBaseExtensions.CreateTopPart(MyMechanicalConnectionBlockBase baseBlock, MyCubeBlockDefinitionGroup definitionGroup, Boolean sizeConversion, Boolean instantBuild) in C:\Dev\multigrid-projector\MultigridProjector\Extensions\MyMechanicalConnectionBlockBaseExtensions.cs:line 28
   at MultigridProjector.Logic.MultigridProjection.CreateTopPartAndAttach(Subgrid subgrid, MyMechanicalConnectionBlockBase baseBlock) in C:\Dev\multigrid-projector\MultigridProjector\Logic\MultigridProjection.cs:line 1466
   at MultigridProjectorServer.Patches.MyMechanicalConnectionBlockBase_CreateTopPartAndAttach.Prefix(MyMechanicalConnectionBlockBase __instance, Int64 builtBy, Boolean smallToLarge, Boolean instantBuild) in C:\Dev\multigrid-projector\MultigridProjectorServer\Patches\MyMechanicalConnectionBlockBase_CreateTopPartAndAttach.cs:line 34
                */
                if (!e.ToString().Contains("System.NullReferenceException"))
                    throw;

                PluginLog.Warn($"Ignored System.NullReferenceException in CreateTopPart or Attach called from CreateTopPartAndAttach: projector.DebugName = \"{Projector.DebugName}\", subgrid.Index = {subgrid.Index}, baseBlock.Position = {baseBlock.Position}, baseBlock.DebugName = \"{baseBlock.DebugName}\"");
            }
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
            if (clipboard.IsActive || Projector.IsActivating)
                return;

            clipboard.ResetGridOrientation();

            var gridBuilders = clipboard.CopiedGrids;
            if (!EnsureBuildableUnderLimits(gridBuilders))
                return;

            Projector.SetIsActivating(true);
            clipboard.Activate(() =>
            {
                clipboard.PreviewGrids?.SetProjector(Projector);

                Projector.SetForceUpdateProjection(true);
                Projector.SetShouldUpdateTexts(true);
                Projector.SetShouldResetBuildable(true);

                clipboard.ActuallyTestPlacement();

                Projector.SetRotation(clipboard, Projector.GetProjectionRotation());
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
            foreach (var subgrid in SupportedSubgrids)
                subgrid.IsConnectedToProjector = false;

            // Flood fill along the connected mechanical connections
            subgrids[0].IsConnectedToProjector = true;
            var supportedSubgridCount = SupportedSubgrids.Count();
            for (var connectedSubgrids = 1; connectedSubgrids < supportedSubgridCount;)
            {
                var connectedBefore = connectedSubgrids;

                foreach (var subgrid in SupportedSubgrids)
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

                        // Extra validation needed on client side after grid splits
                        if (baseConnection.Block.CubeGrid.EntityId != subgrid.BuiltGrid.EntityId)
                        {
                            baseConnection.ClearBuiltBlock();
                            continue;
                        }

                        if (topConnection.Block.CubeGrid.EntityId != topSubgrid.BuiltGrid.EntityId)
                        {
                            topConnection.ClearBuiltBlock();
                            continue;
                        }

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
            var builderCubeBlock = (MyObjectBuilder_ProjectorBase)blockBuilder;
            lock (gridBuilders)
                builderCubeBlock.ProjectedGrids = gridBuilders.Clone();
            MyEntities.RemapObjectBuilderCollection(builderCubeBlock.ProjectedGrids);
        }

        [Everywhere]
        public static void ProjectorInit(MyProjectorBase projector, MyObjectBuilder_CubeBlock objectBuilder)
        {
            if (projector.CubeGrid == null || !projector.AllowWelding)
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
            if (!latestKeepProjection && stats.IsBuildCompleted)
                keepProjection = false;

            Projector.SetHiddenBlock(null);
            Projector.SetStatsDirty(true);
            Projector.UpdateText();
            Projector.RaisePropertiesChanged();

            Destroy();

            if (!keepProjection)
            {
                clipboard.Deactivate();
                clipboard.Clear();
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

                try
                {
                    projection = Create(projector, gridBuilders);
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "Failed to initialize multigrid projection - Removing blueprint to avoid spamming the log.");
                    ((IMyProjector)projector).SetProjectedGrid(null);
                    return false;
                }

                if (projection == null)
                    return true;
            }

            // Call the base class implementation
            //projector.UpdateAfterSimulation();
            // Could not call virtual base class method, so copied it here from MyEntity where it is defined:
            projector.GameLogic.UpdateAfterSimulation();

            // Call custom update logic
            try
            {
                projection.UpdateAfterSimulation();
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "UpdateAfterSimulation of multigrid projection failed - Removing blueprint to avoid spamming the log.");
                ((IMyProjector)projector).SetProjectedGrid(null);
            }

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