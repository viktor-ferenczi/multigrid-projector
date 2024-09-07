using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using System.Collections.Generic;
using System.Linq;
using MultigridProjector.Extensions;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;

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

        // Toolbars in the blueprint
        private readonly Dictionary<FastBlockLocation, ToolbarConfig> toolbarConfigByToolbarLocation = new Dictionary<FastBlockLocation, ToolbarConfig>();

        // Mapping from terminal block positions to toolbar slots they have assigned actions defined
        private readonly Dictionary<FastBlockLocation, List<AssignedSlot>> assignedSlotsByBlockLocation = new Dictionary<FastBlockLocation, List<AssignedSlot>>();

        public ToolbarFixer(IEnumerable<Subgrid> supportedSubgrids)
        {
            // Collect all blocks may be relevant by ID and all the toolbars
            var blockLocationsByEntityId = new Dictionary<long, FastBlockLocation>(1024);
            foreach (var subgrid in supportedSubgrids)
            {
                foreach (var (position, projectedBlock) in subgrid.Blocks)
                {
                    if (!(projectedBlock.Builder is MyObjectBuilder_TerminalBlock terminalBlockBuilder))
                        continue;

                    var location = new FastBlockLocation(subgrid.Index, position);
                    blockLocationsByEntityId[terminalBlockBuilder.EntityId] = location;

                    var toolbarBuilder = terminalBlockBuilder.GetToolbar();
                    if (toolbarBuilder == null)
                        continue;

                    var iterSlotConfigs = toolbarBuilder
                        .Slots
                        .Select(slot => new SlotConfig(slot.Index, slot.Data));

                    toolbarConfigByToolbarLocation[location] = new ToolbarConfig(location, iterSlotConfigs);
                }
            }

            // Map the blocks to slots
            foreach (var toolbarConfig in toolbarConfigByToolbarLocation.Values)
            {
                foreach (var slotConfig in toolbarConfig.SlotConfigs.Values)
                {
                    if (slotConfig.ItemBuilder is MyObjectBuilder_ToolbarItemTerminalBlock itemBuilder &&
                        blockLocationsByEntityId.TryGetValue(itemBuilder.BlockEntityId, out var blockLocation))
                    {
                        slotConfig.BlockLocation = blockLocation;

                        if (!assignedSlotsByBlockLocation.TryGetValue(blockLocation, out var assignedSlots))
                            assignedSlotsByBlockLocation[blockLocation] = assignedSlots = new List<AssignedSlot>();

                        assignedSlots.Add(new AssignedSlot(toolbarConfig.ToolbarLocation, slotConfig.SlotIndex));
                    }
                }
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
                if (slotConfig.ItemBuilder == null)
                    continue;

                var itemBuilder = slotConfig.ItemBuilder.Clone() as MyObjectBuilder_ToolbarItem;
                if (itemBuilder == null)
                    continue;

                switch (itemBuilder)
                {
                    case MyObjectBuilder_ToolbarItemTerminalBlock terminalBlockItemBuilder:
                        if (!slotConfig.BlockLocation.HasValue)
                            continue;

                        var blockLocation = slotConfig.BlockLocation.Value;
                        if (!projection.TryGetSupportedSubgrid(blockLocation.GridIndex, out var blockSubgrid) || !blockSubgrid.HasBuilt)
                            continue;

                        var blockPosition = blockSubgrid.PreviewToBuiltBlockPosition(blockLocation.Position);
                        if (!(blockSubgrid.BuiltGrid?.GetCubeBlock(blockPosition)?.FatBlock is MyTerminalBlock terminalBlock))
                            continue;

                        terminalBlockItemBuilder.BlockEntityId = terminalBlock.EntityId;
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

                var toolbarLocation = toolbarConfig.ToolbarLocation;
                if (!projection.TryGetSupportedSubgrid(toolbarLocation.GridIndex, out var toolbarSubgrid) || !toolbarSubgrid.HasBuilt)
                    continue;

                var toolbarPosition = toolbarSubgrid.PreviewToBuiltBlockPosition(toolbarLocation.Position);
                if (!(toolbarSubgrid.BuiltGrid?.GetCubeBlock(toolbarPosition)?.FatBlock is MyTerminalBlock toolbarBlock))
                    continue;

                var toolbar = toolbarBlock.GetToolbar();
                if (toolbar == null)
                    continue;

                if (slotConfig.ItemBuilder == null)
                    continue;

                var itemBuilder = slotConfig.ItemBuilder.Clone() as MyObjectBuilder_ToolbarItemTerminalBlock;
                if (itemBuilder == null)
                    continue;

                itemBuilder.BlockEntityId = terminalBlock.EntityId;

                var toolbarItem = MyToolbarItemFactory.CreateToolbarItem(itemBuilder);
                toolbar.SetItemAtIndex(slotConfig.SlotIndex, toolbarItem);
                toolbarBlock.RaisePropertiesChanged();
            }
        }

        public void FixToolbars(MultigridProjection projection)
        {
            foreach (var toolbarConfig in toolbarConfigByToolbarLocation.Values)
            {
                var toolbarLocation = toolbarConfig.ToolbarLocation;
                if (!projection.TryGetSupportedSubgrid(toolbarLocation.GridIndex, out var toolbarSubgrid))
                    continue;

                var toolbarPosition = toolbarSubgrid.PreviewToBuiltBlockPosition(toolbarLocation.Position);
                if (!(toolbarSubgrid.BuiltGrid?.GetCubeBlock(toolbarPosition)?.FatBlock is MyTerminalBlock toolbarBlock))
                    continue;

                ConfigureToolbar(projection, toolbarSubgrid, toolbarBlock);
            }
        }
    }
}