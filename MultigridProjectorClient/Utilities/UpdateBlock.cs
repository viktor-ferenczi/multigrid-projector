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
using IMyEventControllerBlock = Sandbox.ModAPI.Ingame.IMyEventControllerBlock;
using SpaceEngineers.Game.EntityComponents.Blocks;
using VRage.Sync;
using MultigridProjector.Logic;

namespace MultigridProjectorClient.Utilities
{
    internal static class UpdateBlock
    {
        // Allocate this only once
        private static readonly HashSet<string> ExcludedEventControllerProperties = new HashSet<string>
        {
            "SearchBox"
        };

        public static void CopyProperties(MyTerminalBlock sourceBlock, MyTerminalBlock destinationBlock, bool shouldBlockBuiltOnServer = false)
        {
            if (!MultigridProjection.TryFindProjectionByBuiltGrid(destinationBlock.CubeGrid, out var projection, out var subgrid))
                return;
            
            // Blocks build on server side, we just fix the toolbars and envent controllers,
            // since they may point from the main grid to subgrids
            if (shouldBlockBuiltOnServer)
            {
                RestoreToolbars(projection, subgrid, destinationBlock);

                if (sourceBlock is MyEventControllerBlock sourceEventControllerBlock1 &&
                    destinationBlock is MyEventControllerBlock destinationEventControllerBlock1)
                {
                    CopyEvents(sourceEventControllerBlock1, destinationEventControllerBlock1);
                }

                return;
            }

            // Special case event controllers
            if (sourceBlock is MyEventControllerBlock sourceEventControllerBlock &&
                destinationBlock is MyEventControllerBlock destinationEventControllerBlock)
            {
                // Copy over terminal properties
                CopyTerminalProperties(sourceBlock, destinationBlock, ExcludedEventControllerProperties);
                RestoreToolbars(projection, subgrid, destinationBlock);

                // Events in Event Controllers are not stored as properties, so copy those as well
                CopyEvents(sourceEventControllerBlock, destinationEventControllerBlock);

                // Copying power must be done in the next frame as disabling a block will prevent properties being modified
                // so we need to wait for all the changes to process
                Events.InvokeOnGameThread(() => CopyPowerState(sourceBlock, destinationBlock));

                return;
            }

            // Copy over terminal properties
            CopyTerminalProperties(sourceBlock, destinationBlock);
            RestoreToolbars(projection, subgrid, destinationBlock);

            // Copy over special properties if applicable
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
        
        private static void RestoreToolbars(MultigridProjection projection, Subgrid subgrid, MyTerminalBlock terminalBlock)
        {
            projection.ToolbarFixer.ConfigureToolbar(projection, subgrid, terminalBlock);
            projection.ToolbarFixer.AssignBlockToToolbars(projection, subgrid, terminalBlock);
        }

        // FIXME: Use ToolbarFixer instead to restore event controller settings from the preview, make sure
        // the event controller code in ToolbarFixer does proper syncing and works both on server and client  
        private static void CopyEvents(MyEventControllerBlock sourceBlock, MyEventControllerBlock destinationBlock)
        {
            Events.InvokeOnGameThread(() =>
            {
                long eventId = ((Sandbox.ModAPI.IMyEventControllerBlock) sourceBlock).SelectedEvent.UniqueSelectionId;
                Delegate selectEvent = Reflection.GetMethod(typeof(MyEventControllerBlock), destinationBlock, "SelectEvent");
                selectEvent.DynamicInvoke(eventId);
            }, 30);

            Events.InvokeOnGameThread(() => { CopyEventControllerCondition(sourceBlock, destinationBlock); }, 60);

            Events.InvokeOnGameThread(() => { RestoreEventControllerBlockSelection(destinationBlock); }, 90);
        }

        private static void CopyEventControllerCondition(MyEventControllerBlock previewBlock, MyEventControllerBlock builtBlock)
        {
            ((IMyEventControllerBlock) builtBlock).Threshold = ((IMyEventControllerBlock) previewBlock).Threshold;
            ((IMyEventControllerBlock) builtBlock).IsLowerOrEqualCondition = ((IMyEventControllerBlock) previewBlock).IsLowerOrEqualCondition;
            ((IMyEventControllerBlock) builtBlock).IsAndModeEnabled = ((IMyEventControllerBlock) previewBlock).IsAndModeEnabled;

            if (previewBlock.Components.TryGet<MyEventAngleChanged>(out var sourceComp) &&
                builtBlock.Components.TryGet<MyEventAngleChanged>(out var destinationComp))
            {
                Sync<float, SyncDirection.BothWays> sourceAngle = (Sync<float, SyncDirection.BothWays>) Reflection.GetValue(sourceComp, "m_angle");
                Sync<float, SyncDirection.BothWays> destinationAngle = (Sync<float, SyncDirection.BothWays>) Reflection.GetValue(destinationComp, "m_angle");

                destinationAngle.Value = sourceAngle.Value;
            }
            else
            {
                PluginLog.Error($"Could not find angle for block: {previewBlock.DisplayName}");
            }
        }

        private static void RestoreEventControllerBlockSelection(MyEventControllerBlock builtBlock)
        {
            if (!MultigridProjection.TryFindProjectionByBuiltGrid(builtBlock.CubeGrid, out var projection, out var subgrid))
                return;

            projection.ToolbarFixer.ConfigureToolbar(projection, subgrid, builtBlock);
        }

        private static void CopyPowerState(MyTerminalBlock sourceBlock, MyTerminalBlock destinationBlock)
        {
            if (destinationBlock.GetProperty("OnOff") == null)
                return;

            destinationBlock.SetValue("OnOff", sourceBlock.GetValue<bool>("OnOff"));
        }

        private static void CopyTerminalProperties(MyTerminalBlock sourceBlock, MyTerminalBlock destinationBlock, HashSet<string> exclude = null)
        {
            List<ITerminalProperty> properties = new List<ITerminalProperty>();
            sourceBlock.GetProperties(properties);

            foreach (ITerminalProperty property in properties)
            {
                // Disabling a block messes with setting properties and must be done last (in a separate function)
                if (property.Id == "OnOff")
                    continue;

                // Allow skipping properties for compatibility reasons
                if (!(exclude is null) && exclude.Contains(property.Id))
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
            List<MyObjectBuilder_CubeGrid> projectedGrids = (List<MyObjectBuilder_CubeGrid>) Reflection.GetValue(sourceBlock, "m_savedProjections");
            Delegate initFromObjectBuilder = Reflection.GetMethod(typeof(MyProjectorBase), destinationBlock, "InitFromObjectBuilder");

            if (projectedGrids == null)
                return;

            initFromObjectBuilder.DynamicInvoke(projectedGrids, null);
        }
    }
}