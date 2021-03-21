using System;
using System.Collections.Generic;
using System.Linq;
using MultigridProjector.Api;
using MultigridProjector.Extensions;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

// Copied to MultigridProjectorModAgent
using ModApiFunctions1 = System.Tuple<
    // int GetSubgridCount(long projectorId)
    System.Func<long, int>,
        
    // List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId)
    System.Func<long, System.Collections.Generic.List<VRage.Game.MyObjectBuilder_CubeGrid>>,
        
    // IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
    System.Func<long, int, VRage.Game.ModAPI.IMyCubeGrid>,
        
    // IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
    System.Func<long, int, VRage.Game.ModAPI.IMyCubeGrid>
>;
using ModApiFunctions2 = System.Tuple<
    // BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
    System.Func<long, int, VRageMath.Vector3I, int>,
        
    // List<Tuple<Vector3I, BlockState>> GetBlockStates(long projectorId, int subgridIndex, BoundingBoxI box, int mask)
    System.Func<long, int, VRageMath.BoundingBoxI, int, System.Collections.Generic.List<System.Tuple<VRageMath.Vector3I, int>>>,
        
    // Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex)
    System.Func<long, int, System.Collections.Generic.Dictionary<VRageMath.Vector3I, System.Tuple<int, VRageMath.Vector3I>>>,
        
    // Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex)
    System.Func<long, int, System.Collections.Generic.Dictionary<VRageMath.Vector3I, System.Tuple<int, VRageMath.Vector3I>>>
>;

namespace MultigridProjector.Logic
{
    public class MultigridProjectorApiProvider: IMultigridProjectorApi
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
        public static object ModApi => _modApi ?? (_modApi = new object[] {
            Api.Version,
            // Cast is to verify that the tuple signature matches what the mods expect. Do NOT remove the cast! 
            // ReSharper disable once RedundantCast
            (ModApiFunctions1) Tuple.Create<
                Func<long, int>,
                Func<long, List<MyObjectBuilder_CubeGrid>>,
                Func<long, int, IMyCubeGrid>,
                Func<long, int, IMyCubeGrid>>(
                ModApiGetSubgridCount, 
                ModApiGetOriginalGridBuilders, 
                ModApiGetPreviewGrid, 
                ModApiGetBuiltGrid
            ),
            // Cast is to verify that the tuple signature matches what the mods expect. Do NOT remove the cast!
            // ReSharper disable once RedundantCast
            (ModApiFunctions2) Tuple.Create<    
                Func<long, int, Vector3I, int>,
                Func<long, int, BoundingBoxI, int, List<Tuple<Vector3I, int>>>,
                Func<long, int, Dictionary<Vector3I, Tuple<int, Vector3I>>>,
                Func<long, int, Dictionary<Vector3I, Tuple<int, Vector3I>>>
            >(
                ModApiGetBlockState, 
                ModApiGetBlockState, 
                ModApiGetBaseConnections, 
                ModApiGetTopConnections
            ),
        });

        private static int ModApiGetSubgridCount(long projectorId) => Api.GetSubgridCount(projectorId);
        private static List<MyObjectBuilder_CubeGrid> ModApiGetOriginalGridBuilders(long projectorId) => Api.GetOriginalGridBuilders(projectorId);
        private static IMyCubeGrid ModApiGetPreviewGrid(long projectorId, int subgridIndex) => Api.GetPreviewGrid(projectorId, subgridIndex);
        private static IMyCubeGrid ModApiGetBuiltGrid(long projectorId, int subgridIndex) => Api.GetBuiltGrid(projectorId, subgridIndex);
        private static int ModApiGetBlockState(long projectorId, int subgridIndex, Vector3I position) => (int) Api.GetBlockState(projectorId, subgridIndex, position);

        private static List<Tuple<Vector3I, int>> ModApiGetBlockState(long projectorId, int subgridIndex, BoundingBoxI box, int mask)
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