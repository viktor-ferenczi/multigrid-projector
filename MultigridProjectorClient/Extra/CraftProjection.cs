using Entities.Blocks;
using HarmonyLib;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjectorClient.Utilities;
using Sandbox.Common.ObjectBuilders;
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
    static class CraftProjection
    {
        private static bool Enabled => Config.CurrentConfig.CraftProjection;
        private static bool IsProjecting(MyProjectorBase block) => IsWorking(block) && block.ProjectedGrid != null;
        private static bool IsWorking(MyProjectorBase block) => block.CubeGrid?.Physics != null && block.IsWorking;

        public static IEnumerable<CustomControl> IterControls()
        {
            var control = new MyTerminalControlButton<MySpaceProjector>(
                "CraftProjection",
                MyStringId.GetOrCompute("Assemble Projection"),
                MyStringId.GetOrCompute("View and assemble the components needed to build the projection"),
                MakeDialog)
            {
                Visible = (_) => Enabled,
                Enabled = (projector) => IsProjecting(projector) && projector.GetRemainingBlocksPerType().Count > 0,
                SupportsMultipleBlocks = false
            };

            yield return new CustomControl(ControlPlacement.After, "Blueprint", control);
        }

        private static void MakeDialog(MySpaceProjector projector)
        {
            MyAssembler assembler = GetProductionAssembler();

            HashSet<MyGuiControlTable.Row> rows = new HashSet<MyGuiControlTable.Row>();
            List<string> bomLines = new List<string>();

            Dictionary<MyDefinitionId, int> blueprintComponents = GetBlueprintComponents(projector);
            Dictionary<MyDefinitionId, int> inventoryComponents = GetInventoryComponents(projector.CubeGrid);

            Dictionary<MyDefinitionId, int> requiredComponents = new Dictionary<MyDefinitionId, int>(blueprintComponents);
            SubtractComponents(ref requiredComponents, inventoryComponents);
            ClampComponents(ref requiredComponents);

            const string idPrefix = "MyObjectBuilder_";

            foreach (KeyValuePair<MyDefinitionId, int> component in blueprintComponents)
            {
                MyDefinitionId id = component.Key;
                int blueprintAmount = component.Value;

                MyDefinitionManager.Static.TryGetComponentDefinition(id, out MyComponentDefinition compDef);
                string name = compDef.DisplayNameText ?? id.SubtypeName ?? "Unknown Component";

                int inventoryAmount = inventoryComponents.GetValueOrDefault(id);
                int requiredAmount = requiredComponents.GetValueOrDefault(id);

                MyGuiControlTable.Row row = new MyGuiControlTable.Row();
                row.AddCell(new MyGuiControlTable.Cell(name, userData: id));

                string requiredToolTip = $"You {(requiredAmount > 0 ? $"need to manufacture {requiredAmount:N0}" : "don't need to manufacture any")} {name}{(requiredAmount != 1 ? "s" : "")}";
                string inventoryToolTip = $"You and the current grid {(inventoryAmount > 0 ? $"have {inventoryAmount:N0}" : "don't have any")} {name}{(inventoryAmount != 1 ? "s" : "")}";
                string blueprintToolTip = $"Completing this blueprint requires {blueprintAmount:N0} {name}{(blueprintAmount != 1 ? "s" : "")}";

                row.AddCell(new MyGuiControlTable.Cell(requiredAmount.ToString("N0"), toolTip: requiredToolTip, userData: requiredAmount));
                row.AddCell(new MyGuiControlTable.Cell(inventoryAmount.ToString("N0"), toolTip: inventoryToolTip, userData: inventoryAmount));
                row.AddCell(new MyGuiControlTable.Cell(blueprintAmount.ToString("N0"), toolTip: blueprintToolTip, userData: blueprintAmount));

                rows.Add(row);

                var idStr = id.ToString();
                if (idStr.StartsWith(idPrefix))
                {
                    idStr = idStr.Substring(idPrefix.Length);
                }
                bomLines.Add($"{idStr}={blueprintAmount}");
            }

            MyGuiScreenMessageBox dialog;
            if (assembler != null)
            {
                dialog = Menus.CraftDialog.CreateDialog(
                    assembler.DisplayNameText,
                    rows,
                    bomLines,
                    (comp, amount) => SendToAssembler(assembler, new Dictionary<MyDefinitionId, int>() { { comp, amount } }),
                    SwitchToProductionTab);
            }
            else
            {
                dialog = Menus.CraftDialog.CreateDialog("[None]", rows, bomLines);
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

            if (projector.AllowScaling) // Handle Console Blocks (projector table)
            {
                foreach (MyCubeGrid subgrid in projector.ProjectedGrid.GetConnectedGrids(GridLinkTypeEnum.Logical))
                {
                    foreach (MySlimBlock previewBlock in subgrid.CubeBlocks)
                    {
                        Dictionary<MyDefinitionId, int> previewBlockComponents = GetBlockComponents(previewBlock);
                        AddComponents(ref components, previewBlockComponents);
                    }
                }

            }
            else
            {
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out MultigridProjection projection))
                    return components;

                Subgrid[] subgrids = projection.GetSupportedSubgrids();
                foreach (Subgrid subgrid in subgrids)
                {
                    HashSet<MySlimBlock> previewBlocks = subgrid.PreviewGrid.CubeBlocks;

                    foreach (MySlimBlock previewBlock in previewBlocks)
                    {
                        Dictionary<MyDefinitionId, int> previewBlockComponents = GetBlockComponents(previewBlock);

                        // Consider the progress of the built counterpart
                        MySlimBlock builtBlock = Construction.GetBuiltBlock(previewBlock);
                        if (builtBlock != null && builtBlock.Integrity < previewBlock.Integrity)
                        {
                            Dictionary<MyDefinitionId, int> builtBlockComponents = GetBlockComponents(builtBlock);
                            SubtractComponents(ref previewBlockComponents, builtBlockComponents);
                            ClampComponents(ref previewBlockComponents);
                        }

                        AddComponents(ref components, previewBlockComponents);
                    }
                }
            }

            return components;
        }

        private static void AddComponents(ref Dictionary<MyDefinitionId, int> dict1, Dictionary<MyDefinitionId, int> dict2)
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
        }

        private static void SubtractComponents(ref Dictionary<MyDefinitionId, int> dict1, Dictionary<MyDefinitionId, int> dict2)
        {
            foreach (KeyValuePair<MyDefinitionId, int> kvp in dict2)
            {
                if (dict1.ContainsKey(kvp.Key))
                {
                    dict1[kvp.Key] -= kvp.Value;
                }
                else
                {
                    dict1.Add(kvp.Key, -kvp.Value);
                }
            }
        }

        public static void ClampComponents(ref Dictionary<MyDefinitionId, int> dict)
        {
            List<MyDefinitionId> keys = new List<MyDefinitionId>(dict.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                MyDefinitionId key = keys[i];
                if (dict[key] < 0)
                {
                    dict[key] = 0;
                }
            }
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
                            int count = (int) item.Amount;

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
                    int count = (int) item.Amount;

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

            terminalTabs.SelectedPage = (int) MyTerminalPageEnum.Production;
        }
    }
}