using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Entities.Blocks;
using HarmonyLib;
using MultigridProjectorClient.Extra;
using MultigridProjectorClient.Utilities;
using Sandbox.Game.Gui;

namespace MultigridProjectorClient.Patches
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(MySpaceProjector))]
    public static class MySpaceProjector_CreateTerminalControls
    {
        // ReSharper disable once UnusedMember.Local
        [HarmonyPostfix]
        [HarmonyPatch("CreateTerminalControls")]
        private static void Postfix()
        {
            CreateControls();
            CreateActions();
        }

        private static void CreateControls()
        {
            var controls = BlockHighlight.IterControls()
                .Concat(RepairProjection.IterControls())
                .Concat(ProjectorAligner.IterControls())
                .Concat(CraftProjection.IterControls())
                .ToList();

            var existingControls = new List<ITerminalControl>();
            MyTerminalControlFactory.GetControls(typeof(MySpaceProjector), existingControls);

            var existingControlIds = new HashSet<string>(existingControls.Select(c => c.Id));
            var controlIds = new HashSet<string>(controls.Select(c => c.Control.Id));

            if (controlIds.IsSubsetOf(existingControlIds))
            {
                return;
            }

            foreach (var customControl in controls)
            {
                var terminalControl = (MyTerminalControl<MySpaceProjector>) customControl.Control;

                var referenceId = customControl.ReferenceId;
                var i = existingControls.FindIndex(control => control.Id == referenceId);

                switch (customControl.Placement)
                {
                    case ControlPlacement.Before:
                        MyTerminalControlFactory.AddControl(i > 0 ? i : 0, terminalControl);
                        break;

                    case ControlPlacement.After:
                        if (i >= 0)
                        {
                            MyTerminalControlFactory.AddControl(i, terminalControl);
                        }
                        else
                        {
                            MyTerminalControlFactory.AddControl(terminalControl);
                        }
                        break;
                }
            }
        }

        private static void CreateActions()
        {
            var actions = BlockHighlight.IterActions()
                .Concat(ProjectorAligner.IterActions())
                .ToList();

            var existingActions = new List<ITerminalAction>();
            MyTerminalControlFactory.GetActions(typeof(MySpaceProjector), existingActions);

            var existingActionIds = new HashSet<string>(existingActions.Select(a => a.Id));
            var actionIds = new HashSet<string>(actions.Select(a => a.Id));

            if (actionIds.IsSubsetOf(existingActionIds))
            {
                return;
            }

            foreach (var action in actions)
            {
                MyTerminalControlFactory.AddAction((MyTerminalAction<MySpaceProjector>) action);
            }
        }
    }
}