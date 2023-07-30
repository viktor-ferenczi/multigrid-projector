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
        private static bool IsWorkingButNotProjecting(IMyTerminalBlock block) => IsValid(block) && block.IsWorking && (block as IMyProjector)?.IsProjecting == false;
        private static bool IsProjecting(IMyTerminalBlock block) => IsWorking(block) && (block as IMyProjector)?.IsProjecting == true;
        private static bool IsWorking(IMyTerminalBlock block) => IsValid(block) && block.IsWorking;
        private static bool IsValid(IMyTerminalBlock block) => block.CubeGrid?.Physics != null;

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
            var action = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("AlignProjection");
            action.Enabled = (terminalBlock) => IsProjecting(terminalBlock as IMyProjector);
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