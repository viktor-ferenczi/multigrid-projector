using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace MultigridProjector.Api
{
    public interface IMultigridProjectorApi
    {
        // Multigrid Projector version: 0.4.13
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

        // Writes the build state of the preview blocks into blockStates in a given subgrid and volume of cubes with the given state mask
        bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask);

        // Returns the base connections of the blueprint: base position => top subgrid and top part position (only those connected in the blueprint)
        Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex);

        // Returns the top connections of the blueprint: top position => base subgrid and base part position (only those connected in the blueprint)
        Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex);

        // Returns the grid scan sequence number, incremented each time the preview grids/blocks change in any way in any of the subgrids.
        // Reset to zero on loading a blueprint or clearing (or turning OFF) the projector.
        long GetScanNumber(long projectorId);

        // Returns YAML representation of all information available via API functions.
        // Returns an empty string if the grid scan sequence number is zero (see above).
        // The format may change in incompatible ways only on major version increases.
        // New fields may be introduced without notice with any MGP release as the API changes.
        string GetYaml(long projectorId);

        // Returns the hash of all block states of a subgrid, updated when the scan number increases.
        // Changes only if there is any block state change. Can be used to monitor for state changes efficiently.
        // Reset to zero on loading a blueprint or clearing (or turning OFF) the projector.
        ulong GetStateHash(long projectorId, int subgridIndex);

        // Returns true if the subgrid is fully built (completed)
        bool IsSubgridComplete(long projectorId, int subgridIndex);
    }
}