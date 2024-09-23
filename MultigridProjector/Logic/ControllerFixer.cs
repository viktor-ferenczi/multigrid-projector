using Sandbox.Game.Entities.Cube;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRageMath;
using SpaceEngineers.Game.Entities.Blocks;

namespace MultigridProjector.Logic
{
    public class ControllerFixer
    {
        private readonly struct Location
        {
            public readonly int GridIndex;
            public readonly Vector3I Position;

            public Location(int gridIndex, Vector3I position)
            {
                // Subgrid index
                GridIndex = gridIndex;

                // Block position inside the subgrid in blueprint block coordinates
                Position = position;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return ((GridIndex * 397 ^ Position.X) * 397 ^ Position.Y) * 397 ^ Position.Z;
            }
        }

        private Dictionary<Location, HashSet<Location>> mappings = new Dictionary<Location, HashSet<Location>>();

        public ControllerFixer(IEnumerable<Subgrid> supportedSubgrids)
        {
            // Store locations of all terminal blocks
            var blockLocationsByEntityId = new Dictionary<long, Location>(1024);
            foreach (var subgrid in supportedSubgrids)
            {
                foreach (var (position, projectedBlock) in subgrid.Blocks)
                {
                    if (!(projectedBlock.Builder is MyObjectBuilder_TerminalBlock terminalBlockBuilder))
                        continue;

                    var location = new Location(subgrid.Index, position);
                    blockLocationsByEntityId[terminalBlockBuilder.EntityId] = location;
                }
            }

            // Create mappings between controllers and selected blocks
            foreach (var subgrid in supportedSubgrids)
            {
                foreach (var (position, projectedBlock) in subgrid.Blocks)
                {
                    if (!(projectedBlock.Builder is MyObjectBuilder_EventControllerBlock eventControllerBuilder))
                        continue;

                    HashSet<Location> selectedBlocks = new HashSet<Location>();
                    foreach (long blockId in eventControllerBuilder.SelectedBlocks)
                    {
                        if (blockLocationsByEntityId.TryGetValue(blockId, out Location blockLocation))
                        {
                            selectedBlocks.Add(blockLocation);
                        }
                    }
                    var location = new Location(subgrid.Index, position);
                    mappings.Add(location, selectedBlocks);
                }
            }
        }

        public (HashSet<long>, HashSet<long>) GetSelectedBlockIds(MultigridProjection projection, Subgrid controllerSubgrid, MyEventControllerBlock builtController)
        {
            var controllerLocation = new Location(controllerSubgrid.Index, controllerSubgrid.BuiltToPreviewBlockPosition(builtController.Position));
            if (!mappings.TryGetValue(controllerLocation, out var selectedBlockLocations))
                return (null, null);

            HashSet<long> foundEntityIds = new HashSet<long>();
            HashSet<long> missingEntityIds = new HashSet<long>();

            foreach (var location in selectedBlockLocations)
            {
                if (!projection.TryGetSupportedSubgrid(location.GridIndex, out var blockSubgrid))
                    continue;

                var blockPosition = blockSubgrid.PreviewToBuiltBlockPosition(location.Position);

                if (blockSubgrid.BuiltGrid?.GetCubeBlock(blockPosition)?.FatBlock is MyTerminalBlock builtTerminalBlock)
                {
                    foundEntityIds.Add(builtTerminalBlock.EntityId);
                }
                else
                {
                    if (blockSubgrid.PreviewGrid.GetCubeBlock(location.Position)?.FatBlock is MyTerminalBlock sourceTerminalBlock)
                    {
                        missingEntityIds.Add(sourceTerminalBlock.EntityId);
                    }
                    else
                    {
                        // This should never happen unless the blueprint is malformed
                    }
                }
            }

            return (foundEntityIds, missingEntityIds);
        }
    }
}