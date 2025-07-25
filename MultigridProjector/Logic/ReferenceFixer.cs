using System;
using System.Collections.Generic;
using System.Linq;
using MultigridProjector.Api;
using MultigridProjector.Extensions;
using MultigridProjector.Utilities;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.EntityComponents.Blocks;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game;
using VRage.ObjectBuilder;
using IngameIMyFunctionalBlock = Sandbox.ModAPI.Ingame.IMyFunctionalBlock;

namespace MultigridProjector.Logic
{
    public class ReferenceFixer
    {
        // Terminal blocks by their ID in the blueprint, which must be consistent and stable
        private readonly Dictionary<long, ProjectedBlock> blocksById = new Dictionary<long, ProjectedBlock>();

        // Set of terminal block IDs referenced by each terminal block,
        // it is required to know which existing blocks to update when a new block is built
        // Key: The ID of the referenced block (which is on the toolbar slot, for example)
        // Value: Set of IDs of the blocks referencing the block identified by the keys (referrals)
        private readonly Dictionary<long, HashSet<long>> referenceMap = new Dictionary<long, HashSet<long>>();

        public ReferenceFixer(IEnumerable<Subgrid> subgrids)
        {
            foreach (var subgrid in subgrids)
            {
                foreach (var projectedBlock in subgrid.Blocks.Values)
                {
                    var blockBuilder = projectedBlock.Builder;
                    if (!(blockBuilder is MyObjectBuilder_TerminalBlock terminalBlockBuilder))
                        continue;

                    blocksById[blockBuilder.EntityId] = projectedBlock;

                    foreach (var referencedBlockId in IterReferencedBlockIds(terminalBlockBuilder))
                    {
                        if (referencedBlockId == 0)
                            continue;

                        if (!referenceMap.TryGetValue(referencedBlockId, out var referrals))
                        {
                            referrals = new HashSet<long>();
                            referenceMap.Add(referencedBlockId, referrals);
                        }

                        referrals.Add(blockBuilder.EntityId);
                    }
                }
            }
        }

        private IEnumerable<long> IterReferencedBlockIds(MyObjectBuilder_TerminalBlock terminalBlockBuilder)
        {
            switch (terminalBlockBuilder)
            {
                case MyObjectBuilder_RemoteControl builder:
                    foreach (var blockId in IterToolbarReferencedBlockIds(terminalBlockBuilder))
                        yield return blockId;

                    yield return builder.BindedCamera;
                    break;

                case MyObjectBuilder_EventControllerBlock builder:
                    foreach (var blockId in IterToolbarReferencedBlockIds(terminalBlockBuilder))
                        yield return blockId;

                    if (builder.SelectedBlocks == null)
                        break;
                    
                    foreach (var blockId in builder.SelectedBlocks)
                        yield return blockId;

                    break;

                case MyObjectBuilder_TurretControlBlock builder:
                    if (builder.ToolIds != null)
                    {
                        foreach (var blockId in builder.ToolIds)
                        {
                            yield return blockId;
                        }
                    }

                    yield return builder.AzimuthId;
                    yield return builder.ElevationId;
                    yield return builder.CameraId;
                    break;

                case MyObjectBuilder_OffensiveCombatBlock builder:
                    if (!builder.ComponentContainer.TryGet<MyObjectBuilder_OffensiveCombatCircleOrbit>(out var offensiveCombatComponentBuilder))
                        break;
                    
                    if (offensiveCombatComponentBuilder.SelectedWeapons == null)
                        break;
                    
                    foreach (var blockId in offensiveCombatComponentBuilder.SelectedWeapons)
                        yield return blockId;

                    break;

                // AI Recorder block
                case MyObjectBuilder_PathRecorderBlock builder:
                    if (!builder.ComponentContainer.TryGet<MyObjectBuilder_PathRecorderComponent>(out var pathRecorderComponentBuilder))
                        break;
                    
                    if (pathRecorderComponentBuilder.Waypoints == null)
                        break;
                    
                    foreach (var waypoint in pathRecorderComponentBuilder.Waypoints)
                    {
                        foreach (var waypointActionBuilder in waypoint.Actions)
                        {
                            if (waypointActionBuilder is MyObjectBuilder_ToolbarItemTerminalBlock waypointTerminalBlockActionBuilder)
                            {
                                yield return waypointTerminalBlockActionBuilder.BlockEntityId;
                            }
                        }
                    }

                    break;

                case MyObjectBuilder_ButtonPanel _:
                case MyObjectBuilder_DefensiveCombatBlock _:
                case MyObjectBuilder_SensorBlock _:
                case MyObjectBuilder_FlightMovementBlock _:
                case MyObjectBuilder_ShipController _:
                case MyObjectBuilder_TimerBlock _:
                    foreach (var blockId in IterToolbarReferencedBlockIds(terminalBlockBuilder))
                        yield return blockId;

                    break;
            }
        }

        private IEnumerable<long> IterToolbarReferencedBlockIds(MyObjectBuilder_TerminalBlock terminalBlockBuilder)
        {
            var toolbarBuilder = terminalBlockBuilder.GetToolbar();
            if (toolbarBuilder?.Slots == null)
                yield break;
            
            foreach (var slot in toolbarBuilder.Slots)
            {
                if (slot.Data is MyObjectBuilder_ToolbarItemTerminalBlock terminalBlockItem)
                    yield return terminalBlockItem.BlockEntityId;
            }
        }

        public bool TryMapPreviewToBuiltTerminalBlock<T>(long targetId, out T targetBlock) where T : MyTerminalBlock
        {
            targetBlock = null;
            if (blocksById.TryGetValue(targetId, out var projectedBlock))
            {
                if (projectedBlock.State != BlockState.BeingBuilt && projectedBlock.State != BlockState.FullyBuilt)
                    return false;

                targetBlock = projectedBlock.SlimBlock?.FatBlock as T;
            }

            return targetBlock != null && !targetBlock.Closed && targetBlock.InScene;
        }

        public void RestoreSafe(ProjectedBlock projectedBlock)
        {
            try
            {
                Restore(projectedBlock);
            }
            catch (Exception e)
            {
                PluginLog.Error(e, $"ReferenceFixer: RestoreSafe failed: projectedBlock.Builder.SubtypeName=\"{projectedBlock.Builder.SubtypeName}\"");
            }
        }

        public void Restore(ProjectedBlock projectedBlock)
        {
            RestoreOneWay(projectedBlock);

            // Find all blocks referencing the one which was restored above
            if (!referenceMap.TryGetValue(projectedBlock.Builder.EntityId, out var referencingBlockIds))
                return;

            // Restore each referencing block, so any slots referencing projectedBlock is corrected
            foreach (var referencingBlockId in referencingBlockIds)
            {
                RestoreOneWay(blocksById[referencingBlockId]);
            }
        }

        public void RestoreAllSafe()
        {
            try
            {
                RestoreAll();
            }
            catch (Exception e)
            {
                PluginLog.Error(e, $"ReferenceFixer: RestoreAll failed");
            }
        }

        public void RestoreAll()
        {
            foreach (var projectedBlock in blocksById.Values)
            {
                RestoreOneWay(projectedBlock);
            }
        }

        private void RestoreOneWay(ProjectedBlock projectedBlock)
        {
            if (projectedBlock.State != BlockState.BeingBuilt && projectedBlock.State != BlockState.FullyBuilt)
                return;

            if (!(projectedBlock.SlimBlock?.FatBlock is MyTerminalBlock terminalBlock) || terminalBlock.Closed || !terminalBlock.InScene)
                return;

            var modified = false;
            switch (projectedBlock.Builder)
            {
                case MyObjectBuilder_RemoteControl _:
                    modified = RestoreToolbar(projectedBlock);
                    modified = RestoreRemoteControl(projectedBlock) || modified;
                    break;

                case MyObjectBuilder_EventControllerBlock _:
                    modified = RestoreToolbar(projectedBlock);
                    modified = RestoreEventController(projectedBlock) || modified;
                    break;

                case MyObjectBuilder_TurretControlBlock _:
                    modified = RestoreTurretController(projectedBlock);
                    break;

                case MyObjectBuilder_OffensiveCombatBlock _:
                    modified = RestoreOffensiveCombat(projectedBlock);
                    break;

                // AI Recorder block
                case MyObjectBuilder_PathRecorderBlock _:
                    modified = RestorePathRecorder(projectedBlock);
                    break;

                case MyObjectBuilder_ButtonPanel _:
                    modified = RestoreToolbar(projectedBlock);
                    modified = RestoreButtonPanel(projectedBlock) || modified;
                    break;

                case MyObjectBuilder_DefensiveCombatBlock _:
                case MyObjectBuilder_SensorBlock _:
                case MyObjectBuilder_FlightMovementBlock _:
                case MyObjectBuilder_ShipController _:
                case MyObjectBuilder_TimerBlock _:
                    modified = RestoreToolbar(projectedBlock);
                    break;
            }

            // Optimization: Raise properties changed only if there has been any modification
            if (modified)
                terminalBlock.RaisePropertiesChanged();
        }

        private bool RestoreToolbar(ProjectedBlock projectedBlock)
        {
            var builder = ((MyObjectBuilder_TerminalBlock)projectedBlock.Builder).GetToolbar();
            var toolbar = ((MyTerminalBlock)projectedBlock.SlimBlock?.FatBlock)?.GetToolbar();
            if (builder == null || toolbar == null)
                return false;

            var modified = false;
            foreach (var slot in builder.Slots)
            {
                if (!(slot.Data is MyObjectBuilder_ToolbarItemTerminalBlock terminalBlockItemBuilder))
                    continue;

                var i = slot.Index;
                if (i < 0 || i >= toolbar.ItemCount)
                    continue;

                if (!TryMapPreviewToBuiltTerminalBlock<MyTerminalBlock>(terminalBlockItemBuilder.BlockEntityId, out var targetBlock))
                    continue;

                // Optimization: Do not change the toolbar item if it already has the right target ID 
                if (toolbar.GetItemAtIndex(i) is MyToolbarItem toolbarItem &&
                    toolbarItem.GetObjectBuilder() is MyObjectBuilder_ToolbarItemTerminalBlock toolbarItemBuilder &&
                    toolbarItemBuilder.BlockEntityId == targetBlock.EntityId)
                    continue;

                var itemBuilder = (MyObjectBuilder_ToolbarItemTerminalBlock)terminalBlockItemBuilder.Clone();
                itemBuilder.BlockEntityId = targetBlock.EntityId;
                toolbar.SetItemAtIndex(i, MyToolbarItemFactory.CreateToolbarItem(itemBuilder));

                modified = true;
            }

            // Toolbar items do not need change notifications to be sent, because ItemChanged
            // has already been invoked by SetItemAtIndex whenever required

            return modified;
        }

        private bool RestoreToolbarActions(MyToolbarItem[] actions, List<MyObjectBuilder_ToolbarItem> actionBuilders)
        {
            var modified = false;
            for (var i = 0; i < actions.Length; i++)
            {
                if (i == actionBuilders.Count)
                    break;

                var actionBuilder = actionBuilders[i];
                if (actionBuilder is MyObjectBuilder_ToolbarItemTerminalBlock terminalBlockItemBuilder)
                {
                    if (!TryMapPreviewToBuiltTerminalBlock<MyTerminalBlock>(terminalBlockItemBuilder.BlockEntityId, out var targetBlock))
                        continue;

                    // Optimization: Do not change the action if it already has the right target ID 
                    if (actions[i] != null &&
                        actions[i].GetObjectBuilder() is MyObjectBuilder_ToolbarItemTerminalBlock toolbarItemBuilder &&
                        toolbarItemBuilder.BlockEntityId == targetBlock.EntityId)
                        continue;

                    var itemBuilder = (MyObjectBuilder_ToolbarItemTerminalBlock)terminalBlockItemBuilder.Clone();
                    itemBuilder.BlockEntityId = targetBlock.EntityId;
                    actions[i] = MyToolbarItemFactory.CreateToolbarItem(itemBuilder);

                    modified = true;
                }
            }

            return modified;
        }

        private bool RestoreRemoteControl(ProjectedBlock projectedBlock)
        {
            var builder = (MyObjectBuilder_RemoteControl)projectedBlock.Builder;
            var block = (MyRemoteControl)projectedBlock.SlimBlock.FatBlock;

            if (!TryMapPreviewToBuiltTerminalBlock<MyRemoteControl>(builder.BindedCamera, out var cameraBlock))
                return false;

            var boundCameraSync = block.GetBoundCameraSync();

            // Optimization: Set the value only if it is different
            if (boundCameraSync.Value == cameraBlock.EntityId)
                return false;

            boundCameraSync.Value = cameraBlock.EntityId;
            return true;
        }

        private bool RestoreEventController(ProjectedBlock projectedBlock)
        {
            var builder = (MyObjectBuilder_EventControllerBlock)projectedBlock.Builder;
            var block = (MyEventControllerBlock)projectedBlock.SlimBlock.FatBlock;

            var ids = builder.SelectedBlocks
                .Select(id => TryMapPreviewToBuiltTerminalBlock<MyTerminalBlock>(id, out var selectedBlock) ? selectedBlock : null)
                .Where(tb => tb != null)
                .Select(tb => tb.EntityId)
                .ToHashSet();

            ids.ExceptWith(block.GetSelectedBlocks().Keys);

            var selectedBlockIds = block.GetSelectedBlockIds();
            if (selectedBlockIds != null)
                ids.ExceptWith(selectedBlockIds);

            if (ids.Count == 0)
                return false;

            if (selectedBlockIds == null)
            {
                selectedBlockIds = new MySerializableList<long>(ids.Count);
                block.SetSelectedBlockIds(selectedBlockIds);
            }

            selectedBlockIds.AddRange(ids);

            if (!Sync.IsServer)
            {
                // Do exactly what the UI does, so the changes are synced to the server
                // SelectAvailableBlocks and SelectButton expect MyGuiControlListbox.Item
                var listItems = selectedBlockIds.Select(blockId => new MyGuiControlListbox.Item(userData: blockId)).ToList();
                block.SetSelectedBlockIds(null);
                block.SelectAvailableBlocks(listItems);
                block.SelectButton();
            }

            return true;
        }

        private bool RestoreTurretController(ProjectedBlock projectedBlock)
        {
            var builder = (MyObjectBuilder_TurretControlBlock)projectedBlock.Builder;
            var block = (IMyTurretControlBlock)projectedBlock.SlimBlock.FatBlock;

            var modified = false;

            if (TryMapPreviewToBuiltTerminalBlock<MyMotorStator>(builder.AzimuthId, out var azimuthRotor) && block.AzimuthRotor != azimuthRotor)
            {
                try
                {
                    block.AzimuthRotor = azimuthRotor;
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "RestoreTurretController(): Error setting AzimuthRotor");
                }

                modified = true;
            }

            if (TryMapPreviewToBuiltTerminalBlock<MyMotorStator>(builder.ElevationId, out var elevationRotor) && block.ElevationRotor != elevationRotor)
            {
                try
                {
                    block.ElevationRotor = elevationRotor;
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "RestoreTurretController(): Error setting ElevationRotor");
                }

                modified = true;
            }

            if (TryMapPreviewToBuiltTerminalBlock<MyCameraBlock>(builder.CameraId, out var camera) && block.Camera != camera)
            {
                try
                {
                    block.Camera = camera;
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "RestoreTurretController(): Error setting Camera");
                }

                modified = true;
            }

            var tools = new List<IngameIMyFunctionalBlock>();
            block.GetTools(tools);

            var removeTools = new HashSet<IngameIMyFunctionalBlock>(tools);
            var addTools = new HashSet<IngameIMyFunctionalBlock>(builder.ToolIds.Count);
            foreach (var toolId in builder.ToolIds)
            {
                if (!TryMapPreviewToBuiltTerminalBlock<MyTerminalBlock>(toolId, out var targetBlock))
                    continue;

                if (!(targetBlock is IngameIMyFunctionalBlock targetFunctionalBlock))
                    continue;

                removeTools.Remove(targetFunctionalBlock);
                addTools.Add(targetFunctionalBlock);
            }

            if (removeTools.Count != 0)
            {
                block.RemoveTools(removeTools.ToList());
                modified = true;
            }

            if (addTools.Count != 0)
            {
                block.AddTools(addTools.ToList());
                modified = true;
            }

            return modified;
        }

        private bool RestoreOffensiveCombat(ProjectedBlock projectedBlock)
        {
            var builder = (MyObjectBuilder_OffensiveCombatBlock)projectedBlock.Builder;
            if (!builder.ComponentContainer.TryGet<MyObjectBuilder_OffensiveCombatCircleOrbit>(out var componentBuilder))
                return false;

            var block = (MyOffensiveCombatBlock)projectedBlock.SlimBlock.FatBlock;
            if (!block.Components.TryGet<MyOffensiveCombatCircleOrbit>(out var component))
                return false;

            var selectedWeapons = new List<long>(componentBuilder.SelectedWeapons.Count);
            component.GetSelectedWeapons(selectedWeapons);

            var removeIds = new HashSet<long>(selectedWeapons);
            var addIds = new HashSet<long>(componentBuilder.SelectedWeapons.Count);

            foreach (var blockId in componentBuilder.SelectedWeapons)
            {
                if (!TryMapPreviewToBuiltTerminalBlock<MyTerminalBlock>(blockId, out var targetBlock))
                    continue;

                var targetBlockId = targetBlock.EntityId;
                removeIds.Remove(targetBlockId);
                addIds.Add(targetBlockId);
            }

            // Optimization: Change the weapon selection only if it has changed
            if (removeIds.Count == 0 && addIds.Count == selectedWeapons.Count)
                return false;

            component.SetSelectedWeapons(addIds.ToList());
            return true;
        }

        private bool RestorePathRecorder(ProjectedBlock projectedBlock)
        {
            var builder = (MyObjectBuilder_PathRecorderBlock)projectedBlock.Builder;
            if (!builder.ComponentContainer.TryGet<MyObjectBuilder_PathRecorderComponent>(out var componentBuilder))
                return false;

            var block = (MyPathRecorderBlock)projectedBlock.SlimBlock.FatBlock;
            if (!block.Components.TryGet<MyPathRecorderComponent>(out var component))
                return false;

            var modified = false;
            var waypointCount = Math.Min(componentBuilder.Waypoints.Count, component.Waypoints.Count);
            for (int waypointIndex = 0; waypointIndex < waypointCount; waypointIndex++)
            {
                modified = RestoreToolbarActions(component.Waypoints[waypointIndex].Actions, componentBuilder.Waypoints[waypointIndex].Actions) || modified;
            }

            return modified;
        }

        private bool RestoreButtonPanel(ProjectedBlock projectedBlock)
        {
            var builder = (MyObjectBuilder_ButtonPanel)projectedBlock.Builder;
            var block = (MyButtonPanel)projectedBlock.SlimBlock.FatBlock;

            var modified = false;
            foreach (var pos in builder.CustomButtonNames.Dictionary.Keys)
            {
                var customName = builder.CustomButtonNames[pos];
                if (customName != null)
                {
                    block.SetCustomButtonName(customName, pos);
                    modified = true;
                }
            }

            return modified;
        }
    }
}