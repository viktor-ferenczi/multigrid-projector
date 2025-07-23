using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
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
using MultigridProjector.Extensions;
using System.Linq;
using IMyEventControllerBlock = Sandbox.ModAPI.Ingame.IMyEventControllerBlock;
using SpaceEngineers.Game.EntityComponents.Blocks;
using VRage.Sync;
using Sandbox.Graphics.GUI;
using MultigridProjector.Logic;

namespace MultigridProjectorClient.Utilities
{
    internal static class UpdateEventController
    {
        // FIXME: Use `ToolbarFixer` instead to restore event controller settings from the preview
        public static void CopyEvents(MyEventControllerBlock sourceBlock, MyEventControllerBlock destinationBlock)
        {
            Events.InvokeOnGameThread(() =>
            {
                long eventId = ((Sandbox.ModAPI.IMyEventControllerBlock)sourceBlock).SelectedEvent.UniqueSelectionId;
                Delegate selectEvent = Reflection.GetMethod(typeof(MyEventControllerBlock), destinationBlock, "SelectEvent");
                selectEvent.DynamicInvoke(eventId);
            }, 30);

            Events.InvokeOnGameThread(() => { CopyEventControllerCondition(sourceBlock, destinationBlock); }, 60);

            Events.InvokeOnGameThread(() => { CopyEventControllerBlockSelection(sourceBlock, destinationBlock); }, 90);
        }

        private static void CopyEventControllerCondition(MyEventControllerBlock previewBlock, MyEventControllerBlock builtBlock)
        {
            ((IMyEventControllerBlock)builtBlock).Threshold = ((IMyEventControllerBlock)previewBlock).Threshold;
            ((IMyEventControllerBlock)builtBlock).IsLowerOrEqualCondition = ((IMyEventControllerBlock)previewBlock).IsLowerOrEqualCondition;
            ((IMyEventControllerBlock)builtBlock).IsAndModeEnabled = ((IMyEventControllerBlock)previewBlock).IsAndModeEnabled;

            if (previewBlock.Components.TryGet<MyEventAngleChanged>(out var sourceComp) &&
                builtBlock.Components.TryGet<MyEventAngleChanged>(out var destinationComp))
            {
                Sync<float, SyncDirection.BothWays> sourceAngle = (Sync<float, SyncDirection.BothWays>)Reflection.GetValue(sourceComp, "m_angle");
                Sync<float, SyncDirection.BothWays> destinationAngle = (Sync<float, SyncDirection.BothWays>)Reflection.GetValue(destinationComp, "m_angle");

                destinationAngle.Value = sourceAngle.Value;
            }
            else
            {
                PluginLog.Error($"Could not find angle for block: {previewBlock.DisplayName}");
            }
        }

        private static void CopyEventControllerBlockSelection(MyEventControllerBlock previewBlock, MyEventControllerBlock builtBlock)
        {
            // Sanity checks, only for debugging
            if (previewBlock.CubeGrid?.IsPreview != true)
                return;
            if (builtBlock.CubeGrid?.IsPreview != false)
                return;

            // FIXME: Awkward way to verify that the preview block corresponds to the built block.
            // It would be much cleaner to pass only the block location of the event controller to restore
            // the source blocks for from the corresponding projection (preview block).
            if (!MultigridProjection.TryFindProjectionByProjector(previewBlock.CubeGrid.Projector, out var sourceProjection) ||
                !MultigridProjection.TryFindProjectionByBuiltGrid(builtBlock.CubeGrid, out var projection, out _) ||
                projection.Projector?.EntityId != sourceProjection.Projector?.EntityId)
                return;

            var selectedBlockIds = projection.MapPreviewToBuiltTerminalBlockIds(previewBlock.GetSelectedBlockIds()).ToList();

            // No need to select blocks if there were none selected (an empty list is the default)
            if (!selectedBlockIds.Any())
                return;

            // Do exactly what the UI does, so the changes are synced to the server
            // SelectAvailableBlocks and SelectButton expect MyGuiControlListbox.Item
            var listItems = selectedBlockIds.Select(blockId => new MyGuiControlListbox.Item(userData: blockId)).ToList();
            builtBlock.SelectAvailableBlocks(listItems);
            builtBlock.SelectButton();
        }
    }

    internal static class UpdateBlock
    {
        // Allocate this only once
        private static readonly HashSet<string> ExcludedEventControllerPropertiesEventController = new HashSet<string>
        {
            // UI only field, it would fail if set programmatically
            "SearchBox",
        };

        private static readonly HashSet<string> ExcludedEventControllerPropertiesTurretController = new HashSet<string>
        {
            // These are filled by ReferenceFixer,
            // they contain block IDs which must be mapped
            "RotorAzimuth",
            "RotorElevation",
            "CameraList",
        };

        public static void CopyProperties(MyTerminalBlock sourceBlock, MyTerminalBlock destinationBlock)
        {
            // Special cases
            var powerDelay = -1;
            HashSet<string> exclude = null;
            switch (sourceBlock)
            {
                case MyEventControllerBlock _:
                    exclude = ExcludedEventControllerPropertiesEventController;
                    powerDelay = 120;
                    break;

                case MyTurretControlBlock _:
                    exclude = ExcludedEventControllerPropertiesTurretController;
                    powerDelay = 120;
                    break;
            }

            // Copy over terminal properties
            CopyTerminalProperties(sourceBlock, destinationBlock, exclude);

            // Copy over special properties if applicable
            switch (sourceBlock)
            {
                case MyProjectorBase sourceProjectorBase when destinationBlock is MyProjectorBase destinationProjectorBase:
                    CopyBlueprints(sourceProjectorBase, destinationProjectorBase);
                    break;

                case MyEventControllerBlock sourceEventControllerBlock when destinationBlock is MyEventControllerBlock destinationEventControllerBlock:
                    // Events in Event Controllers are not stored as properties, so copy those as well
                    UpdateEventController.CopyEvents(sourceEventControllerBlock, destinationEventControllerBlock);
                    break;

                case IMyProgrammableBlock sourceProgrammableBlock when destinationBlock is IMyProgrammableBlock destinationProgrammableBlock:
                    if (MySession.Static.IsSettingsExperimental())
                        CopyScripts(sourceProgrammableBlock, destinationProgrammableBlock);
                    break;
            }

            // Copying power must be done in the next frame as disabling a block will prevent properties being modified
            // so we need to wait for all the changes to process
            Events.InvokeOnGameThread(() => CopyPowerState(sourceBlock, destinationBlock), powerDelay);
        }

        private static void CopyPowerState(MyTerminalBlock sourceBlock, MyTerminalBlock destinationBlock)
        {
            if (destinationBlock.GetProperty("OnOff") == null)
                return;

            destinationBlock.SetValue("OnOff", sourceBlock.GetValue<bool>("OnOff"));
        }

        private static void CopyTerminalProperties(MyTerminalBlock sourceBlock, MyTerminalBlock destinationBlock, HashSet<string> exclude = null)
        {
            // Guard condition to prevent a rare crash,
            // see: https://discord.com/channels/1378756728107040829/1391879813006098463
            if (sourceBlock == null)
            {
                PluginLog.Warn("CopyTerminalProperties(): sourceBlock is null");
                return;
            }

            if (destinationBlock == null)
            {
                PluginLog.Warn("CopyTerminalProperties(): destinationBlock is null");
                return;
            }

            List<ITerminalProperty> properties = new List<ITerminalProperty>();
            sourceBlock.GetProperties(properties);

            foreach (ITerminalProperty property in properties)
            {
                // Disabling a block messes with setting properties and must be done last (in a separate function)
                if (property.Id == "OnOff")
                    continue;

                // Allow skipping properties for compatibility reasons
                if (exclude?.Contains(property.Id) == true)
                    continue;

                string propertyType = property.TypeName;

                // Silencing a rare crash,
                // see: https://discord.com/channels/1378756728107040829/1391879813006098463
                try
                {
                    switch (propertyType)
                    {
                        case "Boolean":
                            destinationBlock.SetValue(property.Id, sourceBlock.GetValue<bool>(property.Id));
                            break;
                        case "Color":
                            destinationBlock.SetValue(property.Id, sourceBlock.GetValue<Color>(property.Id));
                            break;
                        case "Single":
                            destinationBlock.SetValue(property.Id, sourceBlock.GetValue<float>(property.Id));
                            break;
                        case "Int64":
                            destinationBlock.SetValue(property.Id, sourceBlock.GetValue<long>(property.Id));
                            break;
                        case "StringBuilder":
                            destinationBlock.SetValue(property.Id, sourceBlock.GetValue<StringBuilder>(property.Id));
                            break;
                        default:
                            // This should not be triggered unless Keen makes a MAJOR UI overhaul
                            PluginLog.Error($"CopyTerminalProperties(): Unknown property type: {property.TypeName} in {property.Id}");
                            break;
                    }
                }
                catch (NullReferenceException e)
                {
                    PluginLog.Error(e, $"CopyTerminalProperties(): Silenced NullReferenceException: {property.TypeName} in {property.Id}");
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
            var projectedGrids = (List<MyObjectBuilder_CubeGrid>)Reflection.GetValue(sourceBlock, "m_savedProjections");
            var initFromObjectBuilder = Reflection.GetMethod(typeof(MyProjectorBase), destinationBlock, "InitFromObjectBuilder");

            if (projectedGrids == null)
                return;

            initFromObjectBuilder.DynamicInvoke(projectedGrids, null);
        }
    }
}