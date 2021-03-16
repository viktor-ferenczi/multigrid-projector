using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace MultigridProjector.Api
{
    public interface IMultigridProjectorApi
    {
        // Multigrid Projector version: 0.1.18
        string Version { get; }

        // Returns the number of subgrids in the active projection, returns zero if there is no projection
        int GetSubgridCount(long projectorId);

        // Returns the original grid builders the projection is based on, returns null if no blueprint is loaded
        List<MyObjectBuilder_CubeGrid> GetOriginalGridBuilders(long projectorId);

        // Returns the preview grid (aka hologram) for the given subgrid, it always exists if the projection is active, even if fully built
        IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex);

        // Returns the already built grid for the given subgrid if there is any, null if not built yet (the first subgrid is always built)
        IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex);

        // Returns the build state of a single projected block
        BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position);

        // Writes built state of the preview blocks into blockStates in a given subgrid and volume of cubes with the given state mask
        bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask);

        // Returns the base connections of the blueprint: base position => top subgrid and top part position (only those connected in the blueprint)
        Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex);

        // Returns the top connections of the blueprint: top position => base subgrid and base part position (only those connected in the blueprint)
        Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex);
    }
}