using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRageMath;

namespace MultigridProjector.Api
{
    public class Program
    {
        #region These are normally provided by the game. Do NOT copy them!

        public IMyProgrammableBlock Me;
        public IMyGridTerminalSystem GridTerminalSystem;

        public void Echo(string message)
        {
        }

        #endregion

        #region Example script to access MGP from a programmable block. Copy this region as a template!

        private MultigridProjectorProgrammableBlockAgent mgp;
        private IMyProjector projector;

        public Program()
        {
            try
            {
                mgp = new MultigridProjectorProgrammableBlockAgent(Me);
                projector = GridTerminalSystem.GetBlockWithName("Projector") as IMyProjector;
            }
            catch (Exception e)
            {
                Echo(e.ToString());
            }
        }

        public void Main()
        {
            try
            {
                if (!mgp.Available)
                    return;

                if (projector == null)
                    return;

                Echo(mgp.GetYaml(projector.EntityId));
            }
            catch (Exception e)
            {
                Echo(e.ToString());
            }
        }

        public struct BlockLocation
        {
            public readonly int GridIndex;
            public readonly Vector3I Position;

            public BlockLocation(int gridIndex, Vector3I position)
            {
                GridIndex = gridIndex;
                Position = position;
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

            public int GetSubgridCount(long projectorId)
            {
                if (!Available)
                    return 0;

                var fn = (Func<long, int>) api[1];
                return fn(projectorId);
            }

            public IMyCubeGrid GetPreviewGrid(long projectorId, int subgridIndex)
            {
                if (!Available)
                    return null;

                var fn = (Func<long, int, IMyCubeGrid>) api[2];
                return fn(projectorId, subgridIndex);
            }

            public IMyCubeGrid GetBuiltGrid(long projectorId, int subgridIndex)
            {
                if (!Available)
                    return null;

                var fn = (Func<long, int, IMyCubeGrid>) api[3];
                return fn(projectorId, subgridIndex);
            }

            public BlockState GetBlockState(long projectorId, int subgridIndex, Vector3I position)
            {
                if (!Available)
                    return BlockState.Unknown;

                var fn = (Func<long, int, Vector3I, int>) api[4];
                return (BlockState) fn(projectorId, subgridIndex, position);
            }

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

            public long GetScanNumber(long projectorId)
            {
                if (!Available)
                    return 0;

                var fn = (Func<long, long>) api[8];
                return fn(projectorId);
            }

            public string GetYaml(long projectorId)
            {
                if (!Available)
                    return "";

                var fn = (Func<long, string>) api[9];
                return fn(projectorId);
            }

            public ulong GetStateHash(long projectorId, int subgridIndex)
            {
                if (!Available)
                    return 0;

                var fn = (Func<long, int, ulong>) api[10];
                return fn(projectorId, subgridIndex);
            }

            public MultigridProjectorProgrammableBlockAgent(IMyProgrammableBlock programmableBlock)
            {
                api = programmableBlock.GetProperty("MgpApi")?.As<Delegate[]>().GetValue(programmableBlock);
                if (api == null || api.Length < 11)
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

        #endregion
    }
}