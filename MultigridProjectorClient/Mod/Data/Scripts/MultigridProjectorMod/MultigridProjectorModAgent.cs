using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using Sandbox.ModAPI;

// Copied from MultigridProjectorApiProvider.cs
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

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Api
{
    public class MultigridProjectorModAgent : IMultigridProjectorApi
    {
        private static readonly string CompatibleMajorVersion = "0.";
        
        private const long WorkshopId = 2415983416;
        private const long ModApiRequestId = WorkshopId * 1000 + 0;
        private const long ModApiResponseId = WorkshopId * 1000 + 1;
        
        private ModApiFunctions1 _api1;
        private ModApiFunctions2 _api2;

        public bool Available { get; private set; }
        public string Version { get; private set; }

        public int GetSubgridCount(long projectorId)
        {
            return _api1?.Item1(projectorId) ?? 0;
        }

        public List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId)
        {
            return _api1?.Item2(projectorId);
        }

        public IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
        {
            return _api1?.Item3(projectorId, subgridIndex);
        }

        public IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
        {
            return _api1?.Item4(projectorId, subgridIndex);
        }

        public BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
        {
            if (!Available)
                return BlockState.Unknown;
            
            return (BlockState) _api2.Item1(projectorId, subgridIndex, position);
        }

        public bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask)
        {
            if (!Available)
                return false;

            foreach (var pair in _api2.Item2(projectorId, subgridIndex, box, mask))
            {
                var position = pair.Item1;
                var status = pair.Item2;
                blockStates[position] = (BlockState) status;
            }

            return true;
        }

        public Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            var baseConnections = new Dictionary<Vector3I, BlockLocation>();
            foreach (var pair in _api2.Item3(projectorId, subgridIndex))
            {
                var baseBlockPosition = pair.Key;
                var topSubgridIndex = pair.Value.Item1;
                var topBlockPosition = pair.Value.Item2;
                baseConnections[baseBlockPosition] = new BlockLocation(topSubgridIndex, topBlockPosition);
            }
            return baseConnections;
        }

        public Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            var topConnections = new Dictionary<Vector3I, BlockLocation>();
            foreach (var pair in _api2.Item3(projectorId, subgridIndex))
            {
                var topBlockPosition = pair.Key;
                var baseSubgridIndex = pair.Value.Item1;
                var baseBlockPosition = pair.Value.Item2;
                topConnections[topBlockPosition] = new BlockLocation(baseSubgridIndex, baseBlockPosition);
            }
            return topConnections;
        }

        public MultigridProjectorModAgent()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(ModApiResponseId, HandleModMessage);
            MyAPIGateway.Utilities.SendModMessage(ModApiRequestId, null);
        }

        private void HandleModMessage(object obj)
        {
            // The last 4 tuple items are reserved for future use
            var api = obj as object[];
            if (api == null || api.Length < 3)
                return;
            
            Version = api[0] as string;
            if (Version == null || !Version.StartsWith(CompatibleMajorVersion))
                return;

            _api1 = api[1] as ModApiFunctions1;
            _api2 = api[2] as ModApiFunctions2;

            if (_api1 == null || _api2 == null)
                return;
            
            Available = true;
        }
    }
}