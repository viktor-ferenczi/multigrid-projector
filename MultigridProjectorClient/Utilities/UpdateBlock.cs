﻿using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRageMath;
using MultigridProjector.Utilities;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using Sandbox.Game.GameSystems;
using MultigridProjector.Extensions;
using System.Linq;
using System.Reflection;

namespace MultigridProjectorClient.Utilities
{
    internal static class UpdateToolbar
    {
        private static readonly string UnknownText = "UNKNOWN ACTION";
        private static readonly string PlaceholderText = "ACTION ENTITY NOT FOUND";

        private static MyToolbarItem CreateTerminalToolbarItem(MyObjectBuilder_ToolbarItemTerminalBlock builder)
        {
            // Use the entity ID of the built counterpart of the block tied to the builder
            if (!MyEntities.TryGetEntityById(builder.BlockEntityId, out MyTerminalBlock itemPreview))
                return null;

            MySlimBlock itemBuilt = Construction.GetBuiltBlock(itemPreview.SlimBlock);

            if (itemBuilt?.FatBlock == null)
                return null;

            MyObjectBuilder_ToolbarItemTerminalBlock data = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemTerminalBlock>();
            data.BlockEntityId = itemBuilt.FatBlock.EntityId;
            data._Action = builder._Action;
            data.Parameters = builder.Parameters;

            return MyToolbarItemFactory.CreateToolbarItem(data);
        }

        private static MyToolbarItem CreateGroupToolbarItem(MyObjectBuilder_ToolbarItemTerminalGroup builder, long blockEntityId)
        {
            MyObjectBuilder_ToolbarItemTerminalGroup data = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemTerminalGroup>();
            data.GroupName = builder.GroupName;
            data._Action = builder._Action;
            data.Parameters = builder.Parameters;

            // This is used internally to find which grid the block group is on.
            // It needs to be set to the block we want to assign the toolbar to.
            data.BlockEntityId = blockEntityId;

            return MyToolbarItemFactory.CreateToolbarItem(data);
        }

        private static MyToolbarItem CreateDummyToolbarItem(string text, long blockEntityId)
        {
            MyObjectBuilder_ToolbarItemTerminalGroup data = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemTerminalGroup>();
            data.GroupName = text;
            data._Action = "";
            data.Parameters = new List<MyObjectBuilder_ToolbarItemActionParameter>() { MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemActionParameter>() };

            // This is used internally to find which grid the block group is on.
            // It needs to be set to the block we want to assign the toolbar to.
            data.BlockEntityId = blockEntityId;

            return MyToolbarItemFactory.CreateToolbarItem(data);
        }

        private static MyBlockGroup CreateDummyGroup(string name, MyTerminalBlock dummyBlock)
        {
            MyBlockGroup dummyGroup = (MyBlockGroup)Activator.CreateInstance(typeof(MyBlockGroup), true);
            dummyGroup.Name = new StringBuilder(name);
            Reflection.SetValue(dummyGroup, "Blocks", new HashSet<MyTerminalBlock>() { dummyBlock });

            MyGridTerminalSystem terminalSystem = dummyBlock.CubeGrid.GridSystems.TerminalSystem;
            terminalSystem.AddUpdateGroup(dummyGroup, true);

            return dummyGroup;
        }

        private static void RemoveDummyGroup(MyBlockGroup dummyGroup)
        {
            MyCubeBlock dummyBlock = dummyGroup.GetTerminalBlocks().First();
            MyGridTerminalSystem terminalSystem = dummyBlock.CubeGrid.GridSystems.TerminalSystem;
            terminalSystem.RemoveGroup(dummyGroup, true);
        }

        private static void SetItemAtIndexWithDummyGroup(MyToolbar toolbar, int index, MyToolbarItem item, string name, MyTerminalBlock dummyBlock)
        {
            // Server side validation prevents toolbars from being made without a valid group
            // We can sidestep this by making a group and removing it once the item is added
            // TODO: Use an event rather then a fixed delay
            MyBlockGroup group = CreateDummyGroup(name, dummyBlock);
            Events.InvokeOnGameThread(() => toolbar.SetItemAtIndex(index, item), 20);   
            Events.InvokeOnGameThread(() => RemoveDummyGroup(group), 40);
        }

        private static MyToolbar GetToolbar(MyTerminalBlock block)
        {
            if (block is MyTimerBlock timerBlock)
                return timerBlock.Toolbar;

            if (block is MySensorBlock sensorBlock)
                return sensorBlock.Toolbar;

            if (block is MyButtonPanel buttonPanel)
                return buttonPanel.Toolbar;

            if (block is MyEventControllerBlock eventControllerBlock)
                return eventControllerBlock.Toolbar;

            if (block is MyFlightMovementBlock flightMovementBlock)
                return flightMovementBlock.Toolbar;

            if (block is MyAirVent airVent)
                return (MyToolbar)Reflection.GetValue(airVent, "m_actionToolbar");

            // Cockpits, remote controls and cryopods
            if (block is MyShipController shipController)
                return shipController.Toolbar;

            return null;
        }

        public static void CopyToolbars(MyTerminalBlock sourceBlock, MyTerminalBlock destinationBlock)
        {
            MyToolbar sourceToolbar = GetToolbar(sourceBlock);
            MyToolbar destinationToolbar = GetToolbar(destinationBlock);

            if (sourceToolbar == null || destinationToolbar == null)
                return;

            for (int i = 0; i < sourceToolbar.Items.Length; i++)
            {
                MyToolbarItem toolbarItem = sourceToolbar.GetItemAtIndex(i);

                if (toolbarItem == null)
                    continue;

                MyObjectBuilder_ToolbarItem builder = toolbarItem.GetObjectBuilder();
                if (builder is MyObjectBuilder_ToolbarItemTerminalBlock terminalBuilder)
                {
                    MyToolbarItem newToolbarItem = CreateTerminalToolbarItem(terminalBuilder);

                    // Make a placeholder if the entity the toolbar is attached to could not be found
                    if (newToolbarItem == null)
                    {
                        newToolbarItem = CreateDummyToolbarItem(PlaceholderText, destinationBlock.EntityId);
                        SetItemAtIndexWithDummyGroup(destinationToolbar, i, newToolbarItem, PlaceholderText, destinationBlock);
                        continue;
                    }

                    destinationToolbar.SetItemAtIndex(i, newToolbarItem);
                    continue;
                }

                else if (builder is MyObjectBuilder_ToolbarItemTerminalGroup groupBuilder)
                {
                    bool groupExists = false;
                    foreach (MyBlockGroup group in destinationBlock.CubeGrid.GetBlockGroups())
                    {
                        if (group.Name.ToString() == groupBuilder.GroupName)
                            groupExists = true;
                    }

                    MyToolbarItem newToolbarItem = CreateGroupToolbarItem(groupBuilder, destinationBlock.EntityId);

                    if (!groupExists)
                    {
                        SetItemAtIndexWithDummyGroup(destinationToolbar, i, newToolbarItem, groupBuilder.GroupName, destinationBlock);
                        continue;
                    }

                    destinationToolbar.SetItemAtIndex(i, newToolbarItem);
                    continue;
                }

                // If the toolbar item is of an unknown type make a dummy item as an error message
                else
                {
                    MyToolbarItem newToolbarItem = CreateDummyToolbarItem(UnknownText, destinationBlock.EntityId);
                    SetItemAtIndexWithDummyGroup(destinationToolbar, i, newToolbarItem, UnknownText, destinationBlock);

                    PluginLog.Error($"Cannot process toolbar item: {toolbarItem}");
                    continue;
                }
            }
        }
    }

    internal static class UpdateBlock
    {
        public static void CopyProperties(MyTerminalBlock sourceBlock, MyTerminalBlock destinationBlock)
        {
            CopyTerminalProperties(sourceBlock, destinationBlock);
            UpdateToolbar.CopyToolbars(sourceBlock, destinationBlock);

            if (sourceBlock is MyProjectorBase sourceProjectorBase &&
                destinationBlock is MyProjectorBase destinationProjectorBase)
            {
                CopyBlueprints(sourceProjectorBase, destinationProjectorBase);
            }

            else if (sourceBlock is IMyProgrammableBlock sourceProgrammableBlock &&
                destinationBlock is IMyProgrammableBlock destinationProgrammableBlock &&
                MySession.Static.IsSettingsExperimental())
            {
                CopyScripts(sourceProgrammableBlock, destinationProgrammableBlock);
            }

            // Copying power must be done in the next frame as disabling a block will prevent properties being modified
            // so we need to wait for all the changes to process
            Events.InvokeOnGameThread(() => CopyPowerState(sourceBlock, destinationBlock));
        }

        private static void CopyPowerState(MyTerminalBlock sourceBlock, MyTerminalBlock destinationBlock)
        {
            if (destinationBlock.GetProperty("OnOff") == null)
                return;
            
            destinationBlock.SetValue("OnOff", sourceBlock.GetValue<bool>("OnOff"));
        }

        private static void CopyTerminalProperties(MyTerminalBlock sourceBlock, MyTerminalBlock destinationBlock)
        {
            List<ITerminalProperty> properties = new List<ITerminalProperty>();
            sourceBlock.GetProperties(properties);

            foreach (ITerminalProperty property in properties)
            {
                // Disabling a block messes with setting properties and must be done last (in a seperate function)
                if (property.Id == "OnOff")
                    continue;

                string propertyType = property.TypeName;

                if (propertyType == "Boolean")
                    destinationBlock.SetValue(property.Id, sourceBlock.GetValue<bool>(property.Id));

                else if (propertyType == "Color")
                    destinationBlock.SetValue(property.Id, sourceBlock.GetValue<Color>(property.Id));

                else if (propertyType == "Single")
                    destinationBlock.SetValue(property.Id, sourceBlock.GetValue<float>(property.Id));

                else if (propertyType == "Int64")
                    destinationBlock.SetValue(property.Id, sourceBlock.GetValue<long>(property.Id));

                else if (propertyType == "StringBuilder")
                    destinationBlock.SetValue(property.Id, sourceBlock.GetValue<StringBuilder>(property.Id));

                else
                {
                    // This should not be triggered unless Keen makes a MAJOR UI overhaul
                    PluginLog.Error($"Unknown Property Type: {property.TypeName} in {property.Id}");
                }
            }

            // This is not in the property list for whatever reason
            destinationBlock.CustomData = sourceBlock.CustomData;
        }

        private static void CopyScripts(IMyProgrammableBlock sourceBlock, IMyProgrammableBlock destinationBlock)
        {
            destinationBlock.ProgramData = sourceBlock.ProgramData;
        }

        private static void CopyBlueprints(MyProjectorBase sourceBlock, MyProjectorBase destinationBlock)
        {
            List<MyObjectBuilder_CubeGrid> projectedGrids = (List<MyObjectBuilder_CubeGrid>)Reflection.GetValue(sourceBlock, "m_savedProjections");
            Delegate InitFromObjectBuilder = Reflection.GetMethod(typeof(MyProjectorBase), destinationBlock, "InitFromObjectBuilder");

            if (projectedGrids == null)
                return;

            InitFromObjectBuilder.DynamicInvoke(projectedGrids, null);
        }
    }
}