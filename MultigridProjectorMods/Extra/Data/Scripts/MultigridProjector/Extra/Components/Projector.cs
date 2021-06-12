using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;

// ReSharper disable once CheckNamespace
namespace MultigridProjector.Extra
{
    // ReSharper disable once UnusedType.Global
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), false)]
    public class Projector : MyGameLogicComponent
    {
        private static volatile bool initialized;

        private static bool IsWorkingButNotProjecting(IMyTerminalBlock block) => IsValid(block) && block.IsWorking && (block as IMyProjector)?.IsProjecting == false;
        private static bool IsProjecting(IMyTerminalBlock block) => IsValid(block) && block.IsWorking && (block as IMyProjector)?.IsProjecting == true;
        private static bool IsWorking(IMyTerminalBlock block) => IsValid(block) && block.IsWorking;
        private static bool IsValid(IMyTerminalBlock block) => block.CubeGrid?.Physics != null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            if (initialized)
                return;

            initialized = true;

            if (Comms.Role == Role.DedicatedServer)
                return;

            CreateManualAlignmentButton();
            CreateLoadRepairProjectionButton();
        }

        private void CreateManualAlignmentButton()
        {
            var checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyProjector>("ManualAlignment");
            checkbox.Visible = (_) => false;
            checkbox.Enabled = IsProjecting;
            checkbox.Getter = Aligner.Getter;
            checkbox.Setter = Aligner.Setter;
            checkbox.Title = MyStringId.GetOrCompute("Manual Alignment");
            checkbox.Tooltip = MyStringId.GetOrCompute("Allows the player to manually align the projection using keys familiar from block placement");
            checkbox.SupportsMultipleBlocks = false;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(checkbox);

            var action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("ToggleManualAlignment");
            action.Enabled = (_) => true;
            action.Action = Aligner.Toggle;
            action.ValidForGroups = false;
            action.Icon = ActionIcons.MOVING_OBJECT_TOGGLE;
            action.Name = new StringBuilder("Toggle Manual Alignment");
            action.Writer = (b, s) => s.Append(Aligner.Getter(b) ? "Aligning" : "Align");
            action.InvalidToolbarTypes = new List<MyToolbarType> {MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator};
            MyAPIGateway.TerminalControls.AddAction<IMyProjector>(action);
        }

        private static void CreateLoadRepairProjectionButton()
        {
            var button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("LoadRepairProjection");
            button.Visible = IsWorking;
            button.Enabled = IsWorkingButNotProjecting;
            button.Action = Repair.LoadMechanicalGroup;
            button.Title = MyStringId.GetOrCompute("Load Repair Projection");
            button.Tooltip = MyStringId.GetOrCompute("Loads the projector's own grid as a repair projection.");
            button.SupportsMultipleBlocks = false;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(button);
        }
    }
}