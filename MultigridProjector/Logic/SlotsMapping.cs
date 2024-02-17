using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Screens.Helpers;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game.ModAPI;

namespace MultigridProjector.Logic
{
    public class SlotsMapping
    {
        private static readonly Guid Guid = new Guid("eb5b85ae-317e-41bf-ac38-7830b13f80d2");

        private readonly Dictionary<BlockMinLocation, Dictionary<int, BlockMinLocation>> blocksByControllerSlot = new Dictionary<BlockMinLocation, Dictionary<int, BlockMinLocation>>();

        public SlotsMapping(List<Subgrid> subgrids)
        {
            // Collect the "controller" functional blocks, these are the ones having the toolbar slots
            var controllers = new Dictionary<BlockMinLocation, ProjectedBlock>();
            foreach (var subgrid in subgrids)
            {
                if (!subgrid.Supported)
                {
                    continue;
                }

                foreach (var (position, projectedBlock) in subgrid.Blocks)
                {
                    if (projectedBlock.Preview.FatBlock is MyFunctionalBlock fb && GetToolbar(fb) != null)
                    {
                        var controllerLocation = new BlockMinLocation(subgrid.Index, position);
                        controllers[controllerLocation] = projectedBlock;
                        blocksByControllerSlot[controllerLocation] = new Dictionary<int, BlockMinLocation>();
                    }
                }
            }

            // Terminal block locations by name
            var blocksByName = new Dictionary<string, List<BlockMinLocation>>();
            foreach (var subgrid in subgrids)
            {
                if (!subgrid.Supported)
                {
                    continue;
                }

                foreach (var (position, projectedBlock) in subgrid.Blocks)
                {
                    if (projectedBlock.Preview.FatBlock is MyTerminalBlock tb)
                    {
                        var name = tb.CustomName.ToString();
                        (blocksByName.TryGetValue(name, out var blockList) ? blockList : blocksByName[name] = new List<BlockMinLocation>()).Add(new BlockMinLocation(subgrid.Index, position));
                    }
                }
            }

            // Find the right block for each of the saved controller toolbar slots
            foreach (var (controllerLocation, controllerProjectedBlock) in controllers)
            {
                var controllerBlock = (MyFunctionalBlock) controllerProjectedBlock.Preview.FatBlock;
                if (!controllerBlock.Storage.TryGetValue(Guid, out var slotInfoText))
                {
                    continue;
                }

                var slotInfo = slotInfoText.Split('\n')
                    .Select(line => line.Split(':'))
                    .Where(parts => parts.Length == 2)
                    .ToDictionary(
                        parts => int.TryParse(parts[0], out var v) ? v : -1,
                        parts => parts[1]);

                var slotIndex = 0;
                var toolbar = GetToolbar(controllerBlock);
                foreach (var toolbarItem in toolbar.Items)
                {
                    if (toolbarItem is MyToolbarItemTerminalBlock)
                    {
                        var name = slotInfo[slotIndex];
                        if (blocksByName.TryGetValue(name, out var blocksWithThisName))
                        {
                            var hits = blocksWithThisName.Where(location => location.GridIndex == controllerLocation.GridIndex).ToArray();
                            if (hits.Length == 0)
                            {
                                hits = blocksWithThisName.Where(location => location.GridIndex != controllerLocation.GridIndex).ToArray();
                            }
                            if (hits.Length != 0)
                            {
                                var blockLocation = hits[0];
                                blocksByControllerSlot[controllerLocation][slotIndex] = blockLocation;
                            }
                        }
                    }

                    slotIndex++;
                }
            }
        }

        // FIXME: Likely not required, remove if not needed for sure
        private bool TryGetBlockForSlot(BlockMinLocation controllerLocation, int slotIndex, out BlockMinLocation blockLocation)
        {
            if (blocksByControllerSlot.TryGetValue(controllerLocation, out var blockLocations))
            {
                return blockLocations.TryGetValue(slotIndex, out blockLocation);
            }

            blockLocation = default;
            return false;
        }

        public void FixToolbarSlots(List<Subgrid> subgrids)
        {
            foreach (var (controllerLocation, slotBlockLocations) in blocksByControllerSlot)
            {
                var controllerProjectedBlock = subgrids[controllerLocation.GridIndex].Blocks[controllerLocation.MinPosition];
                if (!(controllerProjectedBlock.SlimBlock.FatBlock is MyFunctionalBlock controllerBlock))
                    continue;

                var toolbar = GetToolbar(controllerBlock);
                var slotIndex = 0;
                foreach (var item in toolbar.Items)
                {
                    if (item is MyToolbarItemTerminalBlock terminalItem)
                    {
                        if (!MyEntityIdentifier.ExistsById(terminalItem.BlockEntityId))
                        {
                            if (slotBlockLocations.TryGetValue(slotIndex, out var blockLocation))
                            {
                                var blockProjectedBlock = subgrids[blockLocation.GridIndex].Blocks[blockLocation.MinPosition];
                                if (blockProjectedBlock.SlimBlock.FatBlock is MyTerminalBlock terminalBlock)
                                {
                                    // FIXME: BlockEntityId has no public setter, use another API or reflection (test!)
                                    terminalItem.BlockEntityId = terminalBlock.EntityId;
                                }
                            }
                        }
                    }

                    slotIndex++;
                }
            }
        }

        public static void RememberSlots(List<IMyCubeGrid> grids)
        {
            foreach (var grid in grids)
            {
                foreach (var blockWithSlots in grid.GetFatBlocks<MyFunctionalBlock>())
                {
                    var toolbar = GetToolbar(blockWithSlots);
                    if (toolbar is null)
                    {
                        continue;
                    }

                    var slotIndex = 0;
                    var slotInfo = new List<string>();
                    foreach (var anyItem in toolbar.Items)
                    {
                        if (anyItem is MyToolbarItemTerminalBlock blockItem &&
                            MyEntityIdentifier.TryGetEntity(blockItem.BlockEntityId, out var entity) &&
                            entity is MyTerminalBlock terminalBlock)
                        {
                            slotInfo.Add($"{slotIndex}:{terminalBlock.CustomName}");
                        }

                        slotIndex++;
                    }

                    if (slotInfo.Count == 0)
                    {
                        continue;
                    }

                    if (blockWithSlots.Storage == null)
                    {
                        blockWithSlots.Storage = new MyModStorageComponent();
                    }

                    var slotInfoText = string.Join("\n", slotInfo);
                    blockWithSlots.Storage.SetValue(Guid, slotInfoText);
                }
            }
        }

        private static MyToolbar GetToolbar(MyFunctionalBlock block)
        {
            switch (block)
            {
                case MySensorBlock b:
                    return b.Toolbar;
                case MyButtonPanel b:
                    return b.Toolbar;
                case MyEventControllerBlock b:
                    return b.Toolbar;
                case MyFlightMovementBlock b:
                    return b.Toolbar;
                case MyTimerBlock b:
                    return b.Toolbar;
            }

            return null;
        }
    }
}