using Entities.Blocks;
using MultigridProjector.Extensions;
using MultigridProjector.Utilities;
using MultigridProjectorClient.Utilities;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Utils;

namespace MultigridProjectorClient.Extra
{
    internal static class CraftProjection
    {
        private static bool Enabled => Config.CurrentConfig.CraftProjection;
        private static bool IsProjecting(MyProjectorBase block) => IsWorking(block) && block.ProjectedGrid != null;
        private static bool IsWorking(MyProjectorBase block) => block.CubeGrid?.Physics != null && block.IsWorking;

        public static void Initialize()
        {
            CreateTerminalControls();
        }

        private static void CreateTerminalControls()
        {
            MyTerminalControlButton<MySpaceProjector> assembleMissing = new MyTerminalControlButton<MySpaceProjector>(
                "CraftProjectionMissing",
                MyStringId.GetOrCompute("Assemble Projection"),
                MyStringId.GetOrCompute("Send the required components to the current assembler"),
                MakeDialog)
            {
                Visible = (_) => Enabled,
                Enabled = (projector) => IsProjecting(projector) && GetProductionAssembler() != null && projector.GetRemainingBlocksPerType().Count > 0,
                SupportsMultipleBlocks = false
            };

            AddControl.AddControlAfter("Blueprint", assembleMissing);
        }

        private static void MakeDialog(MySpaceProjector projector)
        {
            MyAssembler assembler = GetProductionAssembler();

            StringBuilder compList = new StringBuilder("\n\n\n");
            StringBuilder compCost = new StringBuilder("\n\n\n");

            Dictionary<MyDefinitionId, int> blueprintComponents = GetBlueprintComponents(projector);
            Dictionary<MyDefinitionId, int> inventoryComponents = GetInventoryComponents(projector.CubeGrid);
            Dictionary<MyDefinitionId, int> requiredComponents = GetRequiredComponents(blueprintComponents, inventoryComponents);

            foreach (KeyValuePair<MyDefinitionId, int> component in blueprintComponents)
            {
                MyDefinitionId id = component.Key;
                int blueprintAmount = component.Value;

                // The display name for components always comes with a full description tacked onto the end
                // We discard everything after the first newline
                // If no newline is found the whole name is used (eg modded blocks)
                string name = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(id).DisplayNameText;

                int index = name.IndexOf("\n");
                if (index != -1)
                {
                    name = name.Substring(0, index);
                }

                compList.Append(name + "\n");

                int inventoryAmount = 0;
                if (inventoryComponents.ContainsKey(id))
                    inventoryAmount = inventoryComponents[id];

                compCost.Append(inventoryAmount + " / " + blueprintAmount + "\n");
            }

            StringBuilder heading = new StringBuilder(
                "Assembler selected in the production tab:\n" +
                $"{assembler.DisplayNameText}\n" +
                "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n");
            
            // Lazy fix to make the UI expand to the intended size
            compList.Append(' ', 125);

            // Make sure the component list alwayc contains the same amount of newlines
            // The UI won't scale correctly otherwise
            int padding = 23 - compList.Split('\n').Count;
            if (padding > 0)
            {
                compList.Append('\n', padding);
                compCost.Append('\n', padding);
            }
            else
            {
                // If a large enough number of new components are added this may trigger
                // In the future a scrolling textbox may be used to mitigate this
                PluginLog.Error("Component list overflow!");
            }

            MyGuiSandbox.AddScreen(
                    Menus.CraftDialog.CreateDialog(
                        heading,
                        compList,
                        compCost,
                        () => SendToAssembler(assembler, blueprintComponents),
                        () => SendToAssembler(assembler, requiredComponents)));
        }

        private static MyAssembler GetProductionAssembler()
        {
            try
            {
                MyGuiScreenTerminal terminal = (MyGuiScreenTerminal)Reflection.GetValue(typeof(MyGuiScreenTerminal), "m_instance");
                object production = Reflection.GetValue(typeof(MyGuiScreenTerminal), terminal, "m_controllerProduction");
                MyAssembler assembler = (MyAssembler)Reflection.GetValue(production, "m_selectedAssembler");

                return assembler;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex);
            }

            return null;
        }

        private static Dictionary<MyDefinitionId, int> GetBlueprintComponents(MyProjectorBase projector)
        {
            Dictionary<MyDefinitionId, int> components = new Dictionary<MyDefinitionId, int>();
            Dictionary<MyCubeBlockDefinition, int> remainingBlocks = projector.GetRemainingBlocksPerType();

            foreach (KeyValuePair<MyCubeBlockDefinition, int> block in remainingBlocks)
            {
                MyCubeBlockDefinition.Component[] blockComponents = block.Key.Components;

                foreach (MyCubeBlockDefinition.Component component in blockComponents)
                {
                    if (components.ContainsKey(component.Definition.Id))
                    {
                        components[component.Definition.Id] += component.Count;
                    }
                    else
                    {
                        components.Add(component.Definition.Id, component.Count);
                    }
                }
            }

            return components;
        }

        private static Dictionary<MyDefinitionId, int> GetInventoryComponents(MyCubeGrid startGrid)
        {
            Dictionary<MyDefinitionId, int> components = new Dictionary<MyDefinitionId, int>();

            HashSet<VRage.Game.ModAPI.IMyCubeGrid> grids = new HashSet<VRage.Game.ModAPI.IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(startGrid, GridLinkTypeEnum.Mechanical, grids);

            foreach (MyCubeGrid grid in grids.Cast<MyCubeGrid>())
            {
                foreach(MyCubeBlock block in grid.GetFatBlocks())
                {
                    if (!block.HasInventory)
                        continue;

                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        List<MyPhysicalInventoryItem> items = block.GetInventory(i).GetItems();

                        foreach(MyPhysicalInventoryItem item in items)
                        {
                            MyDefinitionId id = item.GetDefinitionId();
                            int count = (int)item.Amount;

                            if (components.ContainsKey(id))
                            {
                                components[id] += count;
                            }
                            else
                            {
                                components.Add(id, count);
                            }
                        }
                    }
                }
            }

            return components;
        }

        private static Dictionary<MyDefinitionId, int> GetRequiredComponents(
            Dictionary<MyDefinitionId, int> blueprintComponents,
            Dictionary<MyDefinitionId, int> inventoryComponents)
        {
            Dictionary<MyDefinitionId, int> requiredComponents = new Dictionary<MyDefinitionId, int>();

            foreach (KeyValuePair<MyDefinitionId, int> component in blueprintComponents)
            {
                MyDefinitionId id = component.Key;
                int blueprintAmount = component.Value;

                int inventoryAmount = 0;
                if (inventoryComponents.ContainsKey(id))
                    inventoryAmount = inventoryComponents[id];

                int requiredAmount = blueprintAmount - inventoryAmount;

                if (requiredAmount < 0)
                    requiredAmount = 0;

                requiredComponents.Add(id, requiredAmount);
            }

            return requiredComponents;
        }

        private static void SendToAssembler(MyAssembler assembler, Dictionary<MyDefinitionId, int> components)
        {
            foreach (KeyValuePair<MyDefinitionId, int> component in components)
            {
                MyDefinitionId id = component.Key;
                int amount = component.Value;

                MyBlueprintDefinitionBase blueprint = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(id);

                if (blueprint == null)
                    continue;

                assembler.AddQueueItemRequest(blueprint, amount);
            }
        }
    }
}
