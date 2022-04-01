using System;
using System.Collections.Generic;
using System.Linq;
using MultigridProjector.Api;
using MultigridProjector.Extensions;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace MultigridProjector.Logic
{
    public class MultigridProjectorApiProvider : IMultigridProjectorApi
    {
        #region PluginApi

        private static MultigridProjectorApiProvider api;
        public static IMultigridProjectorApi Api => api ?? (api = new MultigridProjectorApiProvider());

        public string Version => "0.4.13";

        public int GetSubgridCount(long projectorId)
        {
            if (!MultigridProjection.TryFindProjectionByProjector(projectorId, out var projection) || !projection.IsValidForApi)
                return 0;

            return projection.GridCount;
        }

        public List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId)
        {
            if (!MultigridProjection.TryFindProjectionByProjector(projectorId, out var projection) || !projection.IsValidForApi)
                return null;

            return projection.Projector.GetOriginalGridBuilders();
        }

        public IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out var projection, out var subgrid) || !projection.IsValidForApi)
                return null;

            return subgrid.PreviewGrid;
        }

        public IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out var projection, out var subgrid) || !projection.IsValidForApi)
                return null;

            return subgrid.BuiltGrid;
        }

        public BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out var projection, out var subgrid) || !projection.IsValidForApi)
                return BlockState.Unknown;

            if (!subgrid.TryGetBlockState(position, out var blockState))
                return BlockState.Unknown;

            return blockState;
        }

        public bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out var projection, out var subgrid) || !projection.IsValidForApi)
                return false;

            foreach (var (position, blockState) in subgrid.IterBlockStates(box, mask))
                blockStates[position] = blockState;

            return true;
        }

        public Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out var projection, out var subgrid) || !projection.IsValidForApi)
                return null;

            return subgrid.BaseConnections
                .ToDictionary(pair => pair.Key, pair => pair.Value.TopLocation);
        }

        public Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out var projection, out var subgrid) || !projection.IsValidForApi)
                return null;

            return subgrid.TopConnections
                .ToDictionary(pair => pair.Key, pair => pair.Value.BaseLocation);
        }

        public long GetScanNumber(long projectorId)
        {
            if (!MultigridProjection.TryFindProjectionByProjector(projectorId, out var projection) || !projection.IsValidForApi)
                return 0;

            return projection.ScanNumber;
        }

        public string GetYaml(long projectorId)
        {
            if (!MultigridProjection.TryFindProjectionByProjector(projectorId, out var projection) || !projection.IsValidForApi)
                return "";

            return projection.GetYaml();
        }

        public ulong GetStateHash(long projectorId, int subgridIndex)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out var projection, out var subgrid) || !projection.IsValidForApi)
                return 0;

            return subgrid.StateHash;
        }

        public bool IsSubgridComplete(long projectorId, int subgridIndex)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out var projection, out var subgrid) || !projection.IsValidForApi)
                return false;

            return subgrid.Stats.IsBuildCompleted;
        }

        #endregion

        #region ModApi

        private const long WorkshopId = 2415983416;
        public const long ModApiRequestId = WorkshopId * 1000 + 0;
        public const long ModApiResponseId = WorkshopId * 1000 + 1;

        private static object modApi;
        public static object ModApi => modApi ?? (modApi = new object[]
        {
            Api.Version,
            (Func<long, int>) Api.GetSubgridCount,
            (Func<long, List<MyObjectBuilder_CubeGrid>>) Api.GetOriginalGridBuilders,
            (Func<long, int, IMyCubeGrid>) Api.GetPreviewGrid,
            (Func<long, int, IMyCubeGrid>) Api.GetBuiltGrid,
            (Func<long, int, Vector3I, int>) ModApiGetBlockState,
            (Func<Dictionary<Vector3I, int>, long, int, BoundingBoxI, int, bool>) ModApiGetBlockStates,
            (Func<long, int, List<Vector3I>, List<int>, List<Vector3I>, bool>) ModApiGetBaseConnections,
            (Func<long, int, List<Vector3I>, List<int>, List<Vector3I>, bool>) ModApiGetTopConnections,
            (Func<long, long>) Api.GetScanNumber,
            (Func<long, string>) Api.GetYaml,
            (Func<long, int, ulong>) Api.GetStateHash,
            (Func<long, int, bool>) Api.IsSubgridComplete,
        });

        private static int ModApiGetBlockState(long projectorId, int subgridIndex, Vector3I position) => (int) Api.GetBlockState(projectorId, subgridIndex, position);

        private static bool ModApiGetBlockStates(Dictionary<Vector3I, int> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out var projection, out var subgrid) || !projection.IsValidForApi)
                return false;

            foreach (var (position, blockState) in subgrid.IterBlockStates(box, mask))
                blockStates[position] = (int)blockState;
            
            return true;
        }

        private static bool ModApiGetBaseConnections(long projectorId, int subgridIndex, List<Vector3I> basePositions, List<int> gridIndices, List<Vector3I> topPositions)
        {
            var baseConnections = Api.GetBaseConnections(projectorId, subgridIndex);
            if (baseConnections == null)
                return false;

            basePositions.AddRange(baseConnections.Keys);
            gridIndices.AddRange(baseConnections.Values.Select(blockLocation => blockLocation.GridIndex));
            topPositions.AddRange(baseConnections.Values.Select(blockLocation => blockLocation.Position));
            return true;
        }

        private static bool ModApiGetTopConnections(long projectorId, int subgridIndex, List<Vector3I> topPositions, List<int> gridIndices, List<Vector3I> basePositions)
        {
            var topConnections = Api.GetTopConnections(projectorId, subgridIndex);
            if (topConnections == null)
                return false;

            topPositions.AddRange(topConnections.Keys);
            gridIndices.AddRange(topConnections.Values.Select(blockLocation => blockLocation.GridIndex));
            basePositions.AddRange(topConnections.Values.Select(blockLocation => blockLocation.Position));
            return true;
        }

        #endregion

        #region ProgrammableBlock API

        private static Delegate[] pbApi;
        private static Delegate[] PbApi => pbApi ?? (pbApi = new Delegate[]
        {
            new Func<string>(() => Api.Version),
            new Func<long, int>(Api.GetSubgridCount),
            new Func<long, int, IMyCubeGrid>(Api.GetPreviewGrid),
            new Func<long, int, IMyCubeGrid>(Api.GetBuiltGrid),
            new Func<long, int, Vector3I, int>(ModApiGetBlockState),
            new Func<Dictionary<Vector3I, int>, long, int, BoundingBoxI, int, bool>(ModApiGetBlockStates),
            new Func<long, int, List<Vector3I>, List<int>, List<Vector3I>, bool>(ModApiGetBaseConnections),
            new Func<long, int, List<Vector3I>, List<int>, List<Vector3I>, bool>(ModApiGetTopConnections),
            new Func<long, long>(Api.GetScanNumber),
            new Func<long, string>(Api.GetYaml),
            new Func<long, int, ulong>(Api.GetStateHash),
            new Func<long, int, bool>(Api.IsSubgridComplete),
        });

        public static void RegisterProgrammableBlockApi()
        {
            if (!Sync.IsServer)
                return;

            MyAPIGateway.TerminalControls.GetControls<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(out var controls);
            if (controls.Any(control => control.Id == "MgpApi"))
                return;

            var property = MyAPIGateway.TerminalControls.CreateProperty<Delegate[], IMyTerminalBlock>("MgpApi");
            property.Visible = _ => false;
            property.Getter = _ => PbApi;
            MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(property);
        }

        #endregion
    }
}