using Sandbox.Game.Gui;

namespace MultigridProjectorClient.Utilities
{
    public enum ControlPlacement
    {
        Before,
        After,
    }

    public class CustomControl
    {
        public readonly ControlPlacement Placement;
        public readonly string ReferenceId;
        public readonly ITerminalControl Control;

        public CustomControl(ControlPlacement placement, string referenceId, ITerminalControl control)
        {
            Placement = placement;
            ReferenceId = referenceId;
            Control = control;
        }
    }
}