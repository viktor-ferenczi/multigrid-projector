using Sandbox.Game.Entities.Cube;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using SpaceEngineers.Game.Entities.Blocks;

namespace MultigridProjector.Logic
{
    public class ControllerFixer
    {
        private readonly Dictionary<FastBlockLocation, HashSet<FastBlockLocation>> mappings = new Dictionary<FastBlockLocation, HashSet<FastBlockLocation>>();

        public ControllerFixer(List<Subgrid> supportedSubgrids)
        {
            // Store locations of all terminal blocks
            var blockLocationsByEntityId = new Dictionary<long, FastBlockLocation>(1024);
            foreach (var subgrid in supportedSubgrids)
            {
                foreach (var (position, projectedBlock) in subgrid.Blocks)
                {
                    if (!(projectedBlock.Builder is MyObjectBuilder_TerminalBlock terminalBlockBuilder))
                        continue;

                    var location = new FastBlockLocation(subgrid.Index, position);
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

                    HashSet<FastBlockLocation> selectedBlocks = new HashSet<FastBlockLocation>();
                    foreach (long blockId in eventControllerBuilder.SelectedBlocks)
                    {
                        if (blockLocationsByEntityId.TryGetValue(blockId, out FastBlockLocation blockLocation))
                        {
                            selectedBlocks.Add(blockLocation);
                        }
                    }
                    var location = new FastBlockLocation(subgrid.Index, position);
                    mappings.Add(location, selectedBlocks);
                }
            }
        }

        public (HashSet<long>, HashSet<long>) GetSelectedBlockIds(MultigridProjection projection, Subgrid controllerSubgrid, MyEventControllerBlock builtController)
        {
            var controllerLocation = new FastBlockLocation(controllerSubgrid.Index, controllerSubgrid.BuiltToPreviewBlockPosition(builtController.Position));
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