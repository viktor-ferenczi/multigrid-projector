using System.Collections.Generic;
using System.Text;
using Entities.Blocks;
using MultigridProjector.Extensions;
using MultigridProjector.Logic;
using MultigridProjectorClient.Utilities;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage.Utils;

namespace MultigridProjectorClient.Extra
{
    public static class ToolbarFix
    {
        public static IEnumerable<CustomControl> IterControls()
        {
            var fixAllToolbars = new MyTerminalControlButton<MySpaceProjector>(
                "FixToolbarsFromProjection",
                MyStringId.GetOrCompute("Fix All Toolbars"),
                MyStringId.GetOrCompute("Fixes all toolbars from repair projection."),
                FixToolbars)
            {
                Visible = projector => projector.Enabled && !projector.AllowScaling && projector.IsWorking,
                Enabled = projector => projector.IsProjecting(),
                SupportsMultipleBlocks = false
            };

            yield return new CustomControl(ControlPlacement.Before, "Blueprint", fixAllToolbars);
        }

        private static void FixToolbars(MySpaceProjector projector)
        {
            if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                return;

            MyGuiSandbox.AddScreen(
                MyGuiSandbox.CreateMessageBox(buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: new StringBuilder("Are you sure to merge ALL slots of\r\nALL toolbars from the projection?"),
                    messageCaption: new StringBuilder("Confirmation"),
                    callback: result =>
                    {
                        if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                            projection.FixToolbars();
                    }));
        }
    }
}