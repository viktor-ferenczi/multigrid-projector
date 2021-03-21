using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using Sandbox.ModAPI;

// Copied from MultigridProjectorApiProvider
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

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Api
{
    public class MultigridProjectorModAgent : IMultigridProjectorApi
    {
        private static readonly string CompatibleMajorVersion = "0.";

        private const long WorkshopId = 2415983416;
        private const long ModApiRequestId = WorkshopId * 1000 + 0;
        private const long ModApiResponseId = WorkshopId * 1000 + 1;

        private object[] _api;

        public bool Available { get; private set; }
        public string Version { get; private set; }

        public int GetSubgridCount(long projectorId)
        {
            if (!Available)
                return 0;

            return ((ModApiFunctions.GetSubgridCount) _api[1])(projectorId);
        }

        public List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId)
        {
            if (!Available)
                return null;

            return ((ModApiFunctions.GetOriginalGridBuilders) _api[2])(projectorId);
        }

        public IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            return ((ModApiFunctions.GetPreviewGrid) _api[3])(projectorId, subgridIndex);
        }

        public IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            return ((ModApiFunctions.GetBuiltGrid) _api[4])(projectorId, subgridIndex);
        }

        public BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
        {
            if (!Available)
                return BlockState.Unknown;

            return (BlockState) ((ModApiFunctions.GetBlockState) _api[5])(projectorId, subgridIndex, position);
        }

        public bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask)
        {
            if (!Available)
                return false;

            foreach (var pair in ((ModApiFunctions.GetBlockStates) _api[6])(projectorId, subgridIndex, box, mask))
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
            foreach (var pair in ((ModApiFunctions.GetBaseConnections) _api[7])(projectorId, subgridIndex))
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
            foreach (var pair in ((ModApiFunctions.GetTopConnections) _api[8])(projectorId, subgridIndex))
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
            _api = obj as object[];
            if (_api == null || _api.Length < 9)
                return;

            Version = _api[0] as string;
            if (Version == null || !Version.StartsWith(CompatibleMajorVersion))
                return;

            Available = true;
        }
    }
}