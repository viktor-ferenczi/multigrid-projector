using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace MultigridProjector.Api
{
    // Copied next to MultigridProjectorModAgent
    public static class ModApiFunctions
    {
        public delegate int GetSubgridCount(long projectorId);

        public delegate List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId);

        public delegate IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex);

        public delegate IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex);

        public delegate int GetBlockState(long projectorId, int subgridIndex, Vector3I position);
        
        public delegate bool GetBlockStates(Dictionary<Vector3I, int> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask);

        public delegate bool GetBaseConnections(long projectorId, int subgridIndex, out Vector3I[] basePositions, out int[] gridIndices, out Vector3I[] topPositions);

        public delegate bool GetTopConnections(long projectorId, int subgridIndex, out Vector3I[] topPositions, out int[] gridIndices, out Vector3I[] basePositions);
    }
}