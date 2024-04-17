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
    public static class MySpaceProjectorPatch
    {
        // ReSharper disable once UnusedMember.Local
        [HarmonyPrefix]
        [HarmonyPatch("CreateTerminalControls")]
        private static bool CreateTerminalControlsPrefix()
        {
            return !MyTerminalControlFactory.AreControlsCreated<MySpaceProjector>();
        }

        // ReSharper disable once UnusedMember.Local
        [HarmonyPostfix]
        [HarmonyPatch("CreateTerminalControls")]
        private static void CreateTerminalControlsPostfix()
        {
            var iterControls = BlockHighlight.IterControls()
                .Concat(ProjectorAligner.IterControls())
                .Concat(RepairProjection.IterControls())
                .Concat(CraftProjection.IterControls());

            var controls = new List<ITerminalControl>();
            MyTerminalControlFactory.GetControls(typeof(MySpaceProjector), controls);

            foreach (var customControl in iterControls)
            {
                var terminalControl = (MyTerminalControl<MySpaceProjector>) customControl.Control;

                var referenceId = customControl.ReferenceId;
                var i = controls.FindIndex(control => control.Id == referenceId);

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
    }
}