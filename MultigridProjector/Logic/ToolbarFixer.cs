using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using System.Collections.Generic;
using System.Linq;
using MultigridProjector.Extensions;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Game;
using VRage.ObjectBuilder;

namespace MultigridProjector.Logic
{
    public class ToolbarFixer
    {
        private readonly struct AssignedSlot
        {
            // Location of the block with the toolbar inside the blueprint
            public readonly FastBlockLocation ToolbarLocation;

            // Index of the slot where the block has an action assigned
            public readonly int SlotIndex;

            public AssignedSlot(FastBlockLocation toolbarLocation, int slotIndex)
            {
                ToolbarLocation = toolbarLocation;
                SlotIndex = slotIndex;
            }
        }

        private class SlotConfig
        {
            // Index of the toolbar slot
            public readonly int SlotIndex;

            // Object builder from the blueprint to construct the toolbar item
            public readonly MyObjectBuilder_ToolbarItem ItemBuilder;

            // Location of the terminal block referenced by the toolbar slot (if any)
            // inside the subgrid in blueprint block coordinates
            public FastBlockLocation? BlockLocation;

            public SlotConfig(int slotIndex, MyObjectBuilder_ToolbarItem itemBuilder)
            {
                SlotIndex = slotIndex;
                ItemBuilder = itemBuilder;
                BlockLocation = null;
            }
        }

        private class ToolbarConfig
        {
            public readonly FastBlockLocation ToolbarLocation;
            public readonly IReadOnlyDictionary<int, SlotConfig> SlotConfigs;

            public ToolbarConfig(FastBlockLocation toolbarLocation, IEnumerable<SlotConfig> iterSlotConfigs)
            {
                ToolbarLocation = toolbarLocation;
                SlotConfigs = iterSlotConfigs.ToDictionary(slotConfig => slotConfig.SlotIndex);
            }
        }
        
        // IMPORTANT: All block locations in the maps below are in the blueprint. NEVER use them to index built blocks!

        // Toolbars in the blueprint by the location of blocks having the toolbars
        private readonly Dictionary<FastBlockLocation, ToolbarConfig> toolbarConfigByToolbarLocation = new Dictionary<FastBlockLocation, ToolbarConfig>(32);

        // Mapping from terminal block locations to toolbar slots they have assigned actions defined
        private readonly Dictionary<FastBlockLocation, List<AssignedSlot>> assignedSlotsByBlockLocation = new Dictionary<FastBlockLocation, List<AssignedSlot>>(32);

        // Mapping of event controller locations to the set of locations of their assigned jobs and its reverse mapping 
        private readonly Dictionary<FastBlockLocation, HashSet<FastBlockLocation>> selectedBlockLocationsByEventControllerLocation = new Dictionary<FastBlockLocation, HashSet<FastBlockLocation>>(32);
        private readonly Dictionary<FastBlockLocation, HashSet<FastBlockLocation>> eventControllerLocationsBySelectedBlockLocation = new Dictionary<FastBlockLocation, HashSet<FastBlockLocation>>(32);
        
        public ToolbarFixer(IEnumerable<Subgrid> supportedSubgrids)
        {
            // Collect all blocks may be relevant by ID and all the toolbars
            var terminalBlockLocationsByEntityId = new Dictionary<long, FastBlockLocation>(1024);
            var eventControllerBuilders = new Dictionary<FastBlockLocation, MyObjectBuilder_EventControllerBlock>(32);
            foreach (var subgrid in supportedSubgrids)
            {
                foreach (var (position, projectedBlock) in subgrid.Blocks)
                {
                    if (!(projectedBlock.Builder is MyObjectBuilder_TerminalBlock terminalBlockBuilder))
                        continue;

                    // All terminal blocks
                    var location = new FastBlockLocation(subgrid.Index, position);
                    terminalBlockLocationsByEntityId[terminalBlockBuilder.EntityId] = location;

                    // Blocks with a toolbar
                    var toolbarBuilder = terminalBlockBuilder.GetToolbar();
                    if (toolbarBuilder != null)
                    {
                        var iterSlotConfigs = toolbarBuilder
                            .Slots
                            .Select(slot => new SlotConfig(slot.Index, slot.Data));

                        toolbarConfigByToolbarLocation[location] = new ToolbarConfig(location, iterSlotConfigs);
                    }

                    // Event controllers
                    if (projectedBlock.Builder is MyObjectBuilder_EventControllerBlock eventControllerBuilder)
                    {
                        eventControllerBuilders[location] = eventControllerBuilder;
                    }
                }
            }

            // Map the blocks to toolbar slots (reverse mapping)
            foreach (var toolbarConfig in toolbarConfigByToolbarLocation.Values)
            {
                foreach (var slotConfig in toolbarConfig.SlotConfigs.Values)
                {
                    if (slotConfig.ItemBuilder is MyObjectBuilder_ToolbarItemTerminalBlock itemBuilder &&
                        terminalBlockLocationsByEntityId.TryGetValue(itemBuilder.BlockEntityId, out var blockLocation))
                    {
                        slotConfig.BlockLocation = blockLocation;

                        if (!assignedSlotsByBlockLocation.TryGetValue(blockLocation, out var assignedSlots))
                            assignedSlotsByBlockLocation[blockLocation] = assignedSlots = new List<AssignedSlot>();

                        assignedSlots.Add(new AssignedSlot(toolbarConfig.ToolbarLocation, slotConfig.SlotIndex));
                    }
                }
            }

            // Map the blocks and event controllers (two-way mapping)
            foreach (var (eventControllerLocation, eventControllerBuilder) in eventControllerBuilders)
            {
                var selectedBlocksCount = eventControllerBuilder.SelectedBlocks.Count;
                if (selectedBlocksCount == 0)
                    continue;

                var selectedBlockLocations = new HashSet<FastBlockLocation>(selectedBlocksCount);
                foreach (var blockEntityId in eventControllerBuilder.SelectedBlocks)
                {
                    if (terminalBlockLocationsByEntityId.TryGetValue(blockEntityId, out var selectedBlockLocation))
                    {
                        selectedBlockLocations.Add(selectedBlockLocation);
                        var eventControllerLocations =
                            eventControllerLocationsBySelectedBlockLocation.TryGetValue(selectedBlockLocation, out var v)
                                ? v
                                : eventControllerLocationsBySelectedBlockLocation[selectedBlockLocation] = new HashSet<FastBlockLocation>(4);
                        eventControllerLocations.Add(eventControllerLocation);
                    }
                }
                selectedBlockLocationsByEventControllerLocation.Add(eventControllerLocation, selectedBlockLocations);
            }
        }

        public void ConfigureToolbar(MultigridProjection projection, Subgrid toolbarSubgrid, MyTerminalBlock toolbarBlock)
        {
            var toolbar = toolbarBlock?.GetToolbar();
            if (toolbar == null)
                return;

            var toolbarLocation = new FastBlockLocation(toolbarSubgrid.Index, toolbarSubgrid.BuiltToPreviewBlockPosition(toolbarBlock.Position));
            if (!toolbarConfigByToolbarLocation.TryGetValue(toolbarLocation, out var toolbarConfig))
                return;

            foreach (var slotConfig in toolbarConfig.SlotConfigs.Values)
            {
                if (!(slotConfig.ItemBuilder?.Clone() is MyObjectBuilder_ToolbarItem itemBuilder))
                    continue;

                switch (itemBuilder)
                {
                    case MyObjectBuilder_ToolbarItemTerminalBlock terminalBlockItemBuilder:
                        if (slotConfig.BlockLocation.HasValue &&
                            projection.TryGetProjectedBlock(slotConfig.BlockLocation.Value, out _, out var projectedBlock) &&
                            projectedBlock.SlimBlock?.FatBlock is MyTerminalBlock terminalBlock)
                        {
                            terminalBlockItemBuilder.BlockEntityId = terminalBlock.EntityId;
                        }
                        break;

                    case MyObjectBuilder_ToolbarItemTerminalGroup terminalGroupItemBuilder:
                        terminalGroupItemBuilder.BlockEntityId = toolbarBlock.EntityId;
                        break;
                }

                var toolbarItem = MyToolbarItemFactory.CreateToolbarItem(itemBuilder);
                toolbar.SetItemAtIndex(slotConfig.SlotIndex, toolbarItem);
            }

            toolbarBlock.RaisePropertiesChanged();
        }

        public void AssignBlockToToolbars(MultigridProjection projection, Subgrid blockSubgrid, MyTerminalBlock terminalBlock)
        {
            var blockLocation = new FastBlockLocation(blockSubgrid.Index, blockSubgrid.BuiltToPreviewBlockPosition(terminalBlock.Position));
            if (!assignedSlotsByBlockLocation.TryGetValue(blockLocation, out var assignedSlots))
                return;

            foreach (var assignedSlot in assignedSlots)
            {
                var toolbarConfig = toolbarConfigByToolbarLocation[assignedSlot.ToolbarLocation];
                var slotConfig = toolbarConfig.SlotConfigs[assignedSlot.SlotIndex];

                if (!projection.TryGetProjectedBlock(toolbarConfig.ToolbarLocation, out _, out var projectedBlock) ||
                    !(projectedBlock.SlimBlock?.FatBlock is MyTerminalBlock toolbarBlock))
                    continue;

                var toolbar = toolbarBlock.GetToolbar();
                if (toolbar == null)
                    continue;

                if (!(slotConfig.ItemBuilder?.Clone() is MyObjectBuilder_ToolbarItemTerminalBlock itemBuilder))
                    continue;

                itemBuilder.BlockEntityId = terminalBlock.EntityId;

                var toolbarItem = MyToolbarItemFactory.CreateToolbarItem(itemBuilder);
                toolbar.SetItemAtIndex(slotConfig.SlotIndex, toolbarItem);
                toolbarBlock.RaisePropertiesChanged();
            }
        }

        public void ConfigureEventController(MultigridProjection projection, Subgrid subgrid, MyTerminalBlock terminalBlock)
        {
            if (!(terminalBlock is MyEventControllerBlock eventControllerBlock))
                return;

            var eventControllerLocation = new FastBlockLocation(subgrid.Index, subgrid.BuiltToPreviewBlockPosition(eventControllerBlock.Position));
            if (!selectedBlockLocationsByEventControllerLocation.TryGetValue(eventControllerLocation, out var selectedBlockLocations))
                return;

            var blockIdItemList = new MySerializableList<MyGuiControlListbox.Item>(selectedBlockLocations.Count);
            foreach (var selectedBlockLocation in selectedBlockLocations)
            {
                if (!projection.TryGetProjectedBlock(selectedBlockLocation, out _, out var projectedBlock))
                    continue;

                var selectedBlockEntityId = projectedBlock.SlimBlock?.FatBlock.EntityId;
                if (selectedBlockEntityId.HasValue)
                    blockIdItemList.Add(new MyGuiControlListbox.Item(userData: selectedBlockEntityId.Value));
            }
            
            eventControllerBlock.SelectAvailableBlocks(blockIdItemList);
            eventControllerBlock.SelectButton();
        }

        public void AssignBlockToEventControllers(MultigridProjection projection, Subgrid subgrid, MyTerminalBlock terminalBlock)
        {
            var selectedBlockEntityId = terminalBlock.EntityId;

            var selectedBlockPreviewPosition = subgrid.BuiltToPreviewBlockPosition(terminalBlock.Position);
            var selectedBlockPreviewLocation = new FastBlockLocation(subgrid.Index, selectedBlockPreviewPosition);
            if (!eventControllerLocationsBySelectedBlockLocation.TryGetValue(selectedBlockPreviewLocation, out var eventControllerLocations))
                return;

            foreach (var eventControllerLocation in eventControllerLocations)
            {
                if (!projection.TryGetProjectedBlock(eventControllerLocation, out _, out var projectedBlock))
                    continue;

                if (!(projectedBlock?.SlimBlock?.FatBlock is MyEventControllerBlock eventControllerBlock))
                    continue;
                
                var blockIdItemList = new MySerializableList<MyGuiControlListbox.Item>
                {
                    new MyGuiControlListbox.Item(userData: selectedBlockEntityId)
                };
                eventControllerBlock.SelectAvailableBlocks(blockIdItemList);
                eventControllerBlock.SelectButton();
            }
        }

        public void FixToolbarsAndEventControllers(MultigridProjection projection)
        {
            foreach (var toolbarConfig in toolbarConfigByToolbarLocation.Values)
            {
                if (projection.TryGetProjectedBlock(toolbarConfig.ToolbarLocation, out var subgrid, out var projectedBlock) &&
                    projectedBlock.SlimBlock?.FatBlock is MyTerminalBlock terminalBlock)
                {
                    ConfigureToolbar(projection, subgrid, terminalBlock);
                }
            }

            foreach (var eventControllerLocation in selectedBlockLocationsByEventControllerLocation.Keys)
            {
                if (projection.TryGetProjectedBlock(eventControllerLocation, out var subgrid, out var projectedBlock) &&
                    projectedBlock.SlimBlock?.FatBlock is MyEventControllerBlock eventControllerBlock)
                {
                    ConfigureEventController(projection, subgrid, eventControllerBlock);
                }
            }
        }
    }
}