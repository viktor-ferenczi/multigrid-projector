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
            var action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("AlignProjectionAction");
            action.Enabled = (terminalBlock) => terminalBlock is IMyProjector;
            action.Action = (terminalBlock) => ProjectorAligner.Instance?.Assign(terminalBlock as IMyProjector);
            action.ValidForGroups = false;
            action.Icon = ActionIcons.MOVING_OBJECT_TOGGLE;
            action.Name = new StringBuilder("Start manual projection alignment");
            action.Writer = (b, s) => s.Append("Align");
            action.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.None, MyToolbarType.Character, MyToolbarType.Spectator };
            customActions.Add(action);
        }
    }
}