using System;
using Entities.Blocks;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjectorClient.Utilities;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Utils;

// ReSharper disable SuggestVarOrType_Elsewhere
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
                "CraftProjection",
                MyStringId.GetOrCompute("Assemble Projection"),
                MyStringId.GetOrCompute("View and assemble the components needed to build the projection"),
                MakeDialog)
            {
                Visible = (_) => Enabled,
                Enabled = (projector) => IsProjecting(projector) && projector.GetRemainingBlocksPerType().Count > 0,
                SupportsMultipleBlocks = false
            };

            AddControl.AddControlAfter("Blueprint", assembleMissing);
        }

        private static void MakeDialog(MySpaceProjector projector)
        {
            MyAssembler assembler = GetProductionAssembler();

            HashSet<MyGuiControlTable.Row> rows = new HashSet<MyGuiControlTable.Row>();

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

                int index = name.IndexOf("\n", StringComparison.Ordinal);
                if (index != -1)
                {
                    name = name.Substring(0, index);
                }

                // Discard the "." after "Comp."
                int index2 = name.IndexOf("Comp.", StringComparison.Ordinal);
                if (index2 != -1)
                {
                    name = name.Substring(0, index2 + 4);
                }

                int inventoryAmount = inventoryComponents.GetValueOrDefault(id);
                int requiredAmount = requiredComponents.GetValueOrDefault(id);

                MyGuiControlTable.Row row = new MyGuiControlTable.Row();
                row.AddCell(new MyGuiControlTable.Cell(name));
                row.AddCell(new MyGuiControlTable.Cell(requiredAmount.ToString("N0"), toolTip: $"You need to manufacture {requiredAmount:N0} {name}{(requiredAmount != 1 ? "s" : "")}"));
                row.AddCell(new MyGuiControlTable.Cell(inventoryAmount.ToString("N0"), toolTip: $"You and the current grid have {inventoryAmount:N0} {name}{(inventoryAmount != 1 ? "s" : "")}"));
                row.AddCell(new MyGuiControlTable.Cell(blueprintAmount.ToString("N0"), toolTip: $"Completing this blueprint requires {blueprintAmount:N0} {name}{(blueprintAmount != 1 ? "s" : "")}"));

                rows.Add(row);
            }

            MyGuiScreenMessageBox dialog;
            if (assembler != null)
            {
                dialog = Menus.CraftDialog.CreateDialog(
                    assembler.DisplayNameText,
                    rows,
                    () => SendToAssembler(assembler, blueprintComponents),
                    () => SendToAssembler(assembler, requiredComponents),
                    SwitchToProductionTab);
            }
            else
            {
                dialog = Menus.CraftDialog.CreateDialog("[None]", rows);
            }

            MyGuiSandbox.AddScreen(dialog);
        }

        private static MyAssembler GetProductionAssembler()
        {
            MyAssembler assembler = Traverse.Create<MyGuiScreenTerminal>()
                .Field("m_instance")
                .Field("m_controllerProduction")
                .Field("m_selectedAssembler")
                .GetValue<MyAssembler>();

            return assembler;
        }

        private static Dictionary<MyDefinitionId, int> GetBlueprintComponents(MyProjectorBase projector)
        {
            Dictionary<MyDefinitionId, int> components = new Dictionary<MyDefinitionId, int>();

            if (!MultigridProjection.TryFindProjectionByProjector(projector, out MultigridProjection projection))
                return components;

            Subgrid[] subgrids = projection.GetSupportedSubgrids();
            foreach (Subgrid subgrid in subgrids)
            {
                HashSet<MySlimBlock> previewBlocks = subgrid.PreviewGrid.CubeBlocks;

                foreach (MySlimBlock previewBlock in previewBlocks)
                {
                    Dictionary<MyDefinitionId, int> requiredComponents = null;
                    Dictionary<MyDefinitionId, int> previewBlockComponents = GetBlockComponents(previewBlock);

                    // Consider the progress of the built counterpart
                    MySlimBlock builtBlock = Construction.GetBuiltBlock(previewBlock);
                    if (builtBlock != null && builtBlock.Integrity < previewBlock.Integrity)
                    {
                        Dictionary<MyDefinitionId, int> builtBlockComponents = GetBlockComponents(builtBlock);
                        requiredComponents = GetRequiredComponents(previewBlockComponents, builtBlockComponents);
                    }

                    AddComponents(ref components, requiredComponents ?? previewBlockComponents);
                }
            }

            return components;
        }

        // FIXME: Return value is never used
        private static Dictionary<MyDefinitionId, int> AddComponents(ref Dictionary<MyDefinitionId, int> dict1, Dictionary<MyDefinitionId, int> dict2)
        {
            foreach (KeyValuePair<MyDefinitionId, int> kvp in dict2)
            {
                if (dict1.ContainsKey(kvp.Key))
                {
                    dict1[kvp.Key] += kvp.Value;
                }
                else
                {
                    dict1.Add(kvp.Key, kvp.Value);
                }
            }

            return dict1;
        }

        private static Dictionary<MyDefinitionId, int> GetBlockComponents(MySlimBlock slimBlock)
        {
            Dictionary<MyDefinitionId, int> components = new Dictionary<MyDefinitionId, int>();

            MyCubeBlockDefinition blockDefinition = slimBlock.BlockDefinition;
            MyCubeBlockDefinition.Component[] blockComponents = blockDefinition.Components;

            for (int i = 0; i < blockComponents.Length; i++)
            {
                MyCubeBlockDefinition.Component component = blockComponents[i];
                MyComponentStack.GroupInfo groupInfo = slimBlock.ComponentStack.GetGroupInfo(i);

                AddComponents(ref components, new Dictionary<MyDefinitionId, int>
                {
                    { component.Definition.Id, groupInfo.MountedCount }
                });
            }

            return components;
        }

        private static Dictionary<MyDefinitionId, int> GetInventoryComponents(MyCubeGrid startGrid, GridLinkTypeEnum linkType = GridLinkTypeEnum.Mechanical)
        {
            Dictionary<MyDefinitionId, int> components = new Dictionary<MyDefinitionId, int>();

            HashSet<VRage.Game.ModAPI.IMyCubeGrid> grids = new HashSet<VRage.Game.ModAPI.IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(startGrid, linkType, grids);

            foreach (MyCubeGrid grid in grids.Cast<MyCubeGrid>())
            {
                foreach (MyCubeBlock block in grid.GetFatBlocks())
                {
                    if (!block.HasInventory)
                        continue;

                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        List<MyPhysicalInventoryItem> items = block.GetInventory(i).GetItems();

                        foreach (MyPhysicalInventoryItem item in items)
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

            MyCharacter character = MySession.Static.LocalCharacter;
            for (int i = 0; i < character.InventoryCount; i++)
            {
                List<MyPhysicalInventoryItem> items = character.GetInventory(i).GetItems();

                foreach (MyPhysicalInventoryItem item in items)
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

                int inventoryAmount = inventoryComponents.GetValueOrDefault(id);
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

                if (amount == 0)
                    continue;

                MyBlueprintDefinitionBase blueprint = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(id);

                if (blueprint == null)
                    continue;

                assembler.AddQueueItemRequest(blueprint, amount);
            }
        }

        public static void SwitchToProductionTab()
        {
            MyGuiControlTabControl terminalTabs = Traverse.Create<MyGuiScreenTerminal>()
                .Field("m_instance")
                .Field("m_terminalTabs")
                .GetValue<MyGuiControlTabControl>();

            terminalTabs.SelectedPage = (int)MyTerminalPageEnum.Production;
        }
    }
}