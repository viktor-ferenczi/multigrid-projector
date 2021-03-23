using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using VRageMath;

// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable once CheckNamespace
namespace MultigridProjector.Api
{
    // ReSharper disable once UnusedType.Global
    public class MultigridProjectorModAgent : IMultigridProjectorApi
    {
        private static readonly string CompatibleMajorVersion = "0.";

        private const long WorkshopId = 2415983416;
        private const long ModApiRequestId = WorkshopId * 1000 + 0;
        private const long ModApiResponseId = WorkshopId * 1000 + 1;

        private object[] _api;

        // ReSharper disable once MemberCanBePrivate.Global
        public bool Available { get; private set; }
        public string Version { get; private set; }

        public int GetSubgridCount(long projectorId)
        {
            if (!Available)
                return 0;

            var fn = (Func<long, int>) _api[1];
            return fn(projectorId);
        }

        public List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId)
        {
            if (!Available)
                return null;

            var fn = (Func<long, List<MyObjectBuilder_CubeGrid>>) _api[2];
            return fn(projectorId);
        }

        public IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            var fn = (Func<long, int, IMyCubeGrid>) _api[3];
            return fn(projectorId, subgridIndex);
        }

        public IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            var fn = (Func<long, int, IMyCubeGrid>) _api[4];
            return fn(projectorId, subgridIndex);
        }

        public BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
        {
            if (!Available)
                return BlockState.Unknown;

            var fn = (Func<long, int, Vector3I, int>) _api[5];
            return (BlockState) fn(projectorId, subgridIndex, position);
        }

        public bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask)
        {
            if (!Available)
                return false;

            var blockIntStates = new Dictionary<Vector3I, int>();
            var fn = (Func<Dictionary<Vector3I, int>, long, int, BoundingBoxI, int, bool>) _api[6];
            if (!fn(blockIntStates, projectorId, subgridIndex, box, mask))
                return false;

            foreach (var pair in blockIntStates)
                blockStates[pair.Key] = (BlockState) pair.Value;

            return true;
        }

        public Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            var basePositions = new List<Vector3I>();
            var gridIndices = new List<int>();
            var topPositions = new List<Vector3I>();
            var fn = (Func<long, int, List<Vector3I>, List<int>, List<Vector3I>, bool>) _api[7];
            if (!fn(projectorId, subgridIndex, basePositions, gridIndices, topPositions))
                return null;

            var baseConnections = new Dictionary<Vector3I, BlockLocation>();
            for (var i = 0; i < basePositions.Count; i++)
                baseConnections[basePositions[i]] = new BlockLocation(gridIndices[i], topPositions[i]);

            return baseConnections;
        }

        public Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;
            
            var topPositions = new List<Vector3I>();
            var gridIndices = new List<int>();
            var basePositions = new List<Vector3I>();
            var fn = (Func<long, int, List<Vector3I>, List<int>, List<Vector3I>, bool>) _api[8];
            if (!fn(projectorId, subgridIndex, topPositions, gridIndices, basePositions))
                return null;

            var topConnections = new Dictionary<Vector3I, BlockLocation>();
            for (var i = 0; i < topPositions.Count; i++)
                topConnections[topPositions[i]] = new BlockLocation(gridIndices[i], basePositions[i]);

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