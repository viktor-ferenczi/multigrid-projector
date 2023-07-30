using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;

namespace MultigridProjectorClient.Extra
{
    // ReSharper disable once UnusedType.Global
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ProjectorSessionComponent : MySessionComponentBase
    {
        private readonly List<IMyTerminalAction> customActions = new List<IMyTerminalAction>();

        private bool initialized;

        public override void UpdateBeforeSimulation()
        {
            if (initialized)
                return;

            initialized = true;

            CreateCustomControls();

            MyAPIGateway.TerminalControls.CustomActionGetter += AddActionsToBlocks;
            MyAPIGateway.Utilities.InvokeOnGameThread(() => { SetUpdateOrder(MyUpdateOrder.NoUpdate); });
        }

        private void AddActionsToBlocks(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if (block is IMyProjector)
            {
                actions.AddRange(customActions);
            }
        }

        private void CreateCustomControls()
        {
            {
                var action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("AlignProjectionAction");
                action.Enabled = (terminalBlock) => terminalBlock is IMyProjector;
                action.Action = (terminalBlock) => ProjectorAligner.Instance?.Assign(terminalBlock as IMyProjector);
                action.ValidForGroups = true;
                action.Icon = ActionIcons.MOVING_OBJECT_TOGGLE;
                action.Name = new StringBuilder("Start manual projection alignment");
                action.Writer = (b, s) => s.Append("Align");
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
                customActions.Add(action);
            }

            {
                var action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("ToggleHighlightBlocks");
                action.Enabled = (terminalBlock) => terminalBlock is IMyProjector;
                action.Action = (terminalBlock) => BlockHighlight.ToggleHighlightBlocks(terminalBlock as IMyProjector);
                action.ValidForGroups = true;
                action.Icon = ActionIcons.TOGGLE;
                action.Name = new StringBuilder("Toggle block highlighting");
                action.Writer = (b, s) => s.Append("Highlight");
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
                customActions.Add(action);
            }

            {
                var action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("EnableHighlightBlocks");
                action.Enabled = (terminalBlock) => terminalBlock is IMyProjector;
                action.Action = (terminalBlock) => BlockHighlight.EnableHighlightBlocks(terminalBlock as IMyProjector);
                action.ValidForGroups = true;
                action.Icon = ActionIcons.ON;
                action.Name = new StringBuilder("Enable block highlighting");
                action.Writer = (b, s) => s.Append("Highlight");
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
                customActions.Add(action);
            }

            {
                var action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("DisableHighlightBlocks");
                action.Enabled = (terminalBlock) => terminalBlock is IMyProjector;
                action.Action = (terminalBlock) => BlockHighlight.DisableHighlightBlocks(terminalBlock as IMyProjector);
                action.ValidForGroups = true;
                action.Icon = ActionIcons.OFF;
                action.Name = new StringBuilder("Disable block highlighting");
                action.Writer = (b, s) => s.Append("Highlight");
                action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
                customActions.Add(action);
            }
        }
    }
}