using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Entities.Blocks;
using HarmonyLib;
using MultigridProjector.Tools;
using MultigridProjectorClient.Extra;
using MultigridProjectorClient.Utilities;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using VRage.Utils;

namespace MultigridProjectorClient.Patches
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(MySpaceProjector))]
    [HarmonyPatch("CreateTerminalControls")]
    public static class MySpaceProjector_CreateTerminalControls
    {
        // ReSharper disable once UnusedMember.Local
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            il.RecordOriginalCode();

            RemoveControl(il, "MarkMissingBlocks");
            RemoveControl(il, "MarkUnfinishedBlocks");

            var i = il.FindLastIndex(ci => ci.opcode == OpCodes.Ret);
            Debug.Assert(i >= 0);
            il.Insert(i++, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MySpaceProjector_CreateTerminalControls), nameof(CreateControls))));
            il.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MySpaceProjector_CreateTerminalControls), nameof(CreateActions))));

            il.RecordPatchedCode();
            return il.AsEnumerable();
        }

        private static void RemoveControl(List<CodeInstruction> il, string controlId)
        {
            var i = il.FindIndex(ci => ci.opcode == OpCodes.Ldstr && ci.operand is string s && s == controlId);
            Debug.Assert(i >= 0);

            var j = il.FindIndex(i + 1, ci => ci.opcode == OpCodes.Call && ci.operand is MethodInfo mi && mi.Name == "AddControl");
            il.RemoveRange(i, j + 1 - i);
        }

        private static void CreateControls()
        {
            var controls = BlockHighlight.IterControls()
                .Concat(ApplyPaint.IterControls())
                .Concat(RepairProjection.IterControls())
                .Concat(ProjectorAligner.IterControls())
                .Concat(CraftProjection.IterControls())
                .Concat(ToolbarFix.IterControls())
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
                var terminalControl = (MyTerminalControl<MySpaceProjector>)customControl.Control;

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
                MyTerminalControlFactory.AddAction((MyTerminalAction<MySpaceProjector>)action);
            }
        }
    }
}