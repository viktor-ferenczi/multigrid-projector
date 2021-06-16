using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI.Ingame;

namespace MultigridProjectorPrograms.RobotArm
{
    public struct BlockLocation
    {
        public readonly int GridIndex;
        public readonly Vector3I Position;

        public BlockLocation(int gridIndex, Vector3I position)
        {
            GridIndex = gridIndex;
            Position = position;
        }

        public override int GetHashCode()
        {
            return (((((GridIndex * 397) ^ Position.X) * 397) ^ Position.Y) * 397) ^ Position.Z;
        }
    }

    public enum BlockState
    {
        // Block state is still unknown, not determined by the background worker yet
        Unknown = 0,

        // The block is not buildable due to lack of connectivity or colliding objects
        NotBuildable = 1,

        // The block has not built yet and ready to be built (side connections are good and no colliding objects)
        Buildable = 2,

        // The block is being built, but not to the level required by the blueprint (needs more welding)
        BeingBuilt = 4,

        // The block has been built to the level required by the blueprint or more
        FullyBuilt = 8,

        // There is mismatching block in the place of the projected block with a different definition than required by the blueprint
        Mismatch = 128
    }

    public class MultigridProjectorProgrammableBlockAgent
    {
        private const string CompatibleMajorVersion = "0.";

        private readonly Delegate[] api;

        public bool Available { get; }
        public string Version { get; }

        // Returns the number of subgrids in the active projection, returns zero if there is no projection
        public int GetSubgridCount(long projectorId)
        {
            if (!Available)
                return 0;

            var fn = (Func<long, int>) api[1];
            return fn(projectorId);
        }

        // Returns the preview grid (aka hologram) for the given subgrid, it always exists if the projection is active, even if fully built
        public IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            var fn = (Func<long, int, IMyCubeGrid>) api[2];
            return fn(projectorId, subgridIndex);
        }

        // Returns the already built grid for the given subgrid if there is any, null if not built yet (the first subgrid is always built)
        public IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            var fn = (Func<long, int, IMyCubeGrid>) api[3];
            return fn(projectorId, subgridIndex);
        }

        // Returns the build state of a single projected block
        public BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
        {
            if (!Available)
                return BlockState.Unknown;

            var fn = (Func<long, int, Vector3I, int>) api[4];
            return (BlockState) fn(projectorId, subgridIndex, position);
        }

        // Writes the build state of the preview blocks into blockStates in a given subgrid and volume of cubes with the given state mask
        public bool GetBlockStates(Dictionary<Vector3I, BlockState> blockStates, long projectorId, int subgridIndex, BoundingBoxI box, int mask)
        {
            if (!Available)
                return false;

            var blockIntStates = new Dictionary<Vector3I, int>();
            var fn = (Func<Dictionary<Vector3I, int>, long, int, BoundingBoxI, int, bool>) api[5];
            if (!fn(blockIntStates, projectorId, subgridIndex, box, mask))
                return false;

            foreach (var pair in blockIntStates)
                blockStates[pair.Key] = (BlockState) pair.Value;

            return true;
        }

        // Returns the base connections of the blueprint: base position => top subgrid and top part position (only those connected in the blueprint)
        public Dictionary<Vector3I, BlockLocation> GetBaseConnections(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            var basePositions = new List<Vector3I>();
            var gridIndices = new List<int>();
            var topPositions = new List<Vector3I>();
            var fn = (Func<long, int, List<Vector3I>, List<int>, List<Vector3I>, bool>) api[6];
            if (!fn(projectorId, subgridIndex, basePositions, gridIndices, topPositions))
                return null;

            var baseConnections = new Dictionary<Vector3I, BlockLocation>();
            for (var i = 0; i < basePositions.Count; i++)
                baseConnections[basePositions[i]] = new BlockLocation(gridIndices[i], topPositions[i]);

            return baseConnections;
        }

        // Returns the top connections of the blueprint: top position => base subgrid and base part position (only those connected in the blueprint)
        public Dictionary<Vector3I, BlockLocation> GetTopConnections(long projectorId, int subgridIndex)
        {
            if (!Available)
                return null;

            var topPositions = new List<Vector3I>();
            var gridIndices = new List<int>();
            var basePositions = new List<Vector3I>();
            var fn = (Func<long, int, List<Vector3I>, List<int>, List<Vector3I>, bool>) api[7];
            if (!fn(projectorId, subgridIndex, topPositions, gridIndices, basePositions))
                return null;

            var topConnections = new Dictionary<Vector3I, BlockLocation>();
            for (var i = 0; i < topPositions.Count; i++)
                topConnections[topPositions[i]] = new BlockLocation(gridIndices[i], basePositions[i]);

            return topConnections;
        }

        // Returns the grid scan sequence number, incremented each time the preview grids/blocks change in any way in any of the subgrids.
        // Reset to zero on loading a blueprint or clearing (or turning OFF) the projector.
        public long GetScanNumber(long projectorId)
        {
            if (!Available)
                return 0;

            var fn = (Func<long, long>) api[8];
            return fn(projectorId);
        }

        // Returns YAML representation of all information available via API functions.
        // Returns an empty string if the grid scan sequence number is zero (see above).
        // The format may change in incompatible ways only on major version increases.
        // New fields may be introduced without notice with any MGP release as the API changes.
        public string GetYaml(long projectorId)
        {
            if (!Available)
                return "";

            var fn = (Func<long, string>) api[9];
            return fn(projectorId);
        }

        // Returns the hash of all block states of a subgrid, updated when the scan number increases.
        // Changes only if there is any block state change. Can be used to monitor for state changes efficiently.
        // Reset to zero on loading a blueprint or clearing (or turning OFF) the projector.
        public ulong GetStateHash(long projectorId, int subgridIndex)
        {
            if (!Available)
                return 0;

            var fn = (Func<long, int, ulong>) api[10];
            return fn(projectorId, subgridIndex);
        }

        // Returns true if the subgrid is fully built (completed)
        public bool IsSubgridComplete(long projectorId, int subgridIndex)
        {
            if (!Available)
                return false;

            var fn = (Func<long, int, bool>) api[11];
            return fn(projectorId, subgridIndex);
        }

        public MultigridProjectorProgrammableBlockAgent(IMyProgrammableBlock programmableBlock)
        {
            api = programmableBlock.GetProperty("MgpApi")?.As<Delegate[]>().GetValue(programmableBlock);
            if (api == null || api.Length < 12)
                return;

            var getVersion = api[0] as Func<string>;
            if (getVersion == null)
                return;

            Version = getVersion();
            if (Version == null || !Version.StartsWith(CompatibleMajorVersion))
                return;

            Available = true;
        }
    }
}