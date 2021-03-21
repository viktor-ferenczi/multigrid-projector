using System;
using System.Collections.Generic;
using System.Linq;
using MultigridProjector.Api;
using MultigridProjector.Extensions;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace MultigridProjector.Logic
{
    // Copied to MultigridProjectorModAgent
    public static class ModApiFunctions
    {
        public delegate int GetSubgridCount(long projectorId);

        public delegate List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId);

        public delegate IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex);

        public delegate IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex);

        public delegate int GetBlockState(long projectorId, int subgridIndex, Vector3I position);

        public delegate List<Tuple<Vector3I, int>> GetBlockStates(long projectorId, int subgridIndex, BoundingBoxI box, int mask);

        public delegate Dictionary<Vector3I, Tuple<int, Vector3I>> GetBaseConnections(long projectorId, int subgridIndex);

        public delegate Dictionary<Vector3I, Tuple<int, Vector3I>> GetTopConnections(long projectorId, int subgridIndex);
    }

    public class MultigridProjectorApiProvider : IMultigridProjectorApi
    {
        #region PluginApi

        private static MultigridProjectorApiProvider _api;
        public static IMultigridProjectorApi Api => _api ?? (_api = new MultigridProjectorApiProvider());

        public string Version => "0.1.21";

        public int GetSubgridCount(long projectorId)
        {
            if (!MultigridProjection.TryFindProjectionByProjector(projectorId, out var projection))
                return 0;

            return projection.GridCount;
        }

        public List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId)
        {
            if (!MultigridProjection.TryFindProjectionByProjector(projectorId, out var projection))
                return null;

            return projection.Projector.GetOriginalGridBuilders();
        }

        public IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out _, out var subgrid))
                return null;

            return subgrid.PreviewGrid;
        }

        public IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out _, out var subgrid))
                return null;

            return subgrid.BuiltGrid;
        }

        public BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out _, out var subgrid))
                return BlockState.Unknown;

            if (!subgrid.BlockStates.TryGetValue(position, out var blockState))
                return BlockState.Unknown;

            return blockState;
        }

        public bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out _, out var subgrid))
                return false;

            foreach (var (position, blockState) in IterBlockStates(subgrid, box, mask))
                blockStates[position] = blockState;

            return true;
        }

        private static IEnumerable<(Vector3I, BlockState)> IterBlockStates(Subgrid subgrid, BoundingBoxI box, int mask)
        {
            // Optimization
            var full = box.Min == Vector3I.MinValue && box.Max == Vector3I.MaxValue;

            foreach (var (position, blockState) in subgrid.BlockStates)
            {
                if (((int) blockState & mask) == 0)
                    continue;

                if (full || box.Contains(position) == ContainmentType.Contains)
                    yield return (position, blockState);
            }
        }

        public Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out _, out var subgrid))
                return null;

            return subgrid.BaseConnections
                .ToDictionary(pair => pair.Key, pair => pair.Value.TopLocation);
        }

        public Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out _, out var subgrid))
                return null;

            return subgrid.TopConnections
                .ToDictionary(pair => pair.Key, pair => pair.Value.BaseLocation);
        }

        #endregion

        #region ModApi

        private const long WorkshopId = 2415983416;
        public const long ModApiRequestId = WorkshopId * 1000 + 0;
        public const long ModApiResponseId = WorkshopId * 1000 + 1;

        private static object _modApi;

        public static object ModApi => _modApi ?? (_modApi = new object[]
        {
            Api.Version,
            (ModApiFunctions.GetSubgridCount) Api.GetSubgridCount,
            (ModApiFunctions.GetOriginalGridBuilders) Api.GetOriginalGridBuilders,
            (ModApiFunctions.GetPreviewGrid) Api.GetPreviewGrid,
            (ModApiFunctions.GetBuiltGrid) Api.GetBuiltGrid,
            (ModApiFunctions.GetBlockState) ModApiGetBlockState,
            (ModApiFunctions.GetBlockStates) ModApiGetBlockStates,
            (ModApiFunctions.GetBaseConnections) ModApiGetBaseConnections,
            (ModApiFunctions.GetTopConnections) ModApiGetTopConnections,
        });

        private static int ModApiGetBlockState(long projectorId, int subgridIndex, Vector3I position) => (int) Api.GetBlockState(projectorId, subgridIndex, position);

        private static List<Tuple<Vector3I, int>> ModApiGetBlockStates(long projectorId, int subgridIndex, BoundingBoxI box, int mask)
        {
            if (!MultigridProjection.TryFindSubgrid(projectorId, subgridIndex, out _, out var subgrid))
                return null;

            return IterBlockStates(subgrid, box, mask)
                .Select(pair => Tuple.Create(pair.Item1, (int) pair.Item2))
                .ToList();
        }

        private static Dictionary<Vector3I, Tuple<int, Vector3I>> ModApiGetBaseConnections(long projectorId, int subgridIndex)
        {
            var baseConnections = new Dictionary<Vector3I, Tuple<int, Vector3I>>();
            foreach (var pair in Api.GetBaseConnections(projectorId, subgridIndex))
                baseConnections[pair.Key] = Tuple.Create(pair.Value.GridIndex, pair.Value.Position);

            return baseConnections;
        }

        private static Dictionary<Vector3I, Tuple<int, Vector3I>> ModApiGetTopConnections(long projectorId, int subgridIndex)
        {
            var topConnections = new Dictionary<Vector3I, Tuple<int, Vector3I>>();
            foreach (var pair in Api.GetTopConnections(projectorId, subgridIndex))
                topConnections[pair.Key] = Tuple.Create(pair.Value.GridIndex, pair.Value.Position);

            return topConnections;
        }

        #endregion
    }
}