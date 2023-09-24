using Sandbox.Graphics.GUI;
using VRageMath;
using MultigridProjectorClient.Utilities;
using VRage.Utils;
using System.Text;
using System;

namespace MultigridProjectorClient.Menus
{
    internal static class ConfigMenu
    {
        private static ConfigObject ConfigObject => Config.CurrentConfig;

        private static readonly StringBuilder MessageCaption = new StringBuilder("Multigrid Projector - Config");

        private static readonly StringBuilder MessageText = new StringBuilder(
            "Here you may change settings for Multigrid Projector.\n" +
            "Hover over a toggle to see a short description of that setting.\n" +
            "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n");

        public static MyGuiScreenMessageBox CreateDialog(bool allowEdit = true)
        {
            // I find it easier to hijack a message box for this then making the UI from scratch
            // TODO: Some day turn this into a proper GUI
            MyGuiScreenMessageBox messageBox = MyGuiSandbox.CreateMessageBox(
                MyMessageBoxStyleEnum.Info,
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageText: MessageText,
                messageCaption: MessageCaption,
                size: new Vector2(0.55f, 0.8f),
                onClosing: () => Config.SaveConfig());

            // Make the background color less transparent, as the default is very faint and this is an important message
            messageBox.BackgroundColor = new Vector4(1f, 1f, 1f, 10.0f);

            // Get the (private) multiline text control so that we can change the text alignment
            MyGuiControlMultilineText messageBoxText = (MyGuiControlMultilineText)Reflection.GetValue(messageBox, "m_messageBoxText");
            messageBoxText.TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            // Change the text of the "yes" button
            MyGuiControlButton yesButton = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_yesButton");
            yesButton.Text = "Confirm Changes";

            // Turn the exit button into a reset settings button
            MyGuiControlButton noButton = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_noButton");
            noButton.Text = "Reset to Default";
            noButton.Enabled = allowEdit;

            // Remove all the functions attached to the onClick event and add our own
            Delegate eventDelegate = (Delegate)Reflection.GetValue(noButton, "ButtonClicked");
            foreach (Delegate handler in eventDelegate.GetInvocationList())
                noButton.ButtonClicked -= (Action<MyGuiControlButton>)handler;

            // Create toggles
            MyGuiControls controls = (MyGuiControls)Reflection.GetValue(typeof(MyGuiScreenBase), messageBox, "m_controls");
            Vector2 basePos = new Vector2(-messageBox.Size.Value.X/3, -0.18f);
            float togglePadding = 0.04f;

            Vector2 corePos = new Vector2(0f, basePos.Y);
            MyGuiControlLabel core = new MyGuiControlLabel(corePos, new Vector2(0.25f, 0.03f), "Core:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, textScale: 1f);
            CreateOption("Show Warning Dialogs", "Prevent all of Multigrid Projector's warning dialogs from appearing.\nMake sure you've read them before disabling this!", corePos + new Vector2(basePos.X, togglePadding), controls, () => ConfigObject.ShowDialogs, (b) => ConfigObject.ShowDialogs = b, allowEdit);
            controls.Add(core);

            // If client welding is disabled we need to disable some other options
            void ClientWeldingSetter(bool newOption)
            {
                ConfigObject.ClientWelding = newOption;

                foreach (MyGuiControlBase control in controls)
                {
                    if (!(control is MyGuiControlCheckbox checkbox))
                        continue;

                    if (checkbox.Name == "Ship Welding" ||
                        checkbox.Name == "Connect Subgrids")
                        checkbox.Enabled = newOption;
                }
            }

            Vector2 compatibilityPos = new Vector2(0f, corePos.Y + togglePadding * 2.5f);
            MyGuiControlLabel compatibility = new MyGuiControlLabel(compatibilityPos, new Vector2(0.25f, 0.03f), "Compatibility Mode:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, textScale: 1f);
            CreateOption("Client Welding", "Place blocks and copy over their properties if they previously could not be welded without the server plugin.", compatibilityPos + new Vector2(basePos.X, togglePadding), controls, () => ConfigObject.ClientWelding, ClientWeldingSetter, allowEdit);
            CreateOption("Ship Welding", "Extend the features of client welding to the welders on the grids and subgrids of the craft you are currently piloting.", compatibilityPos + new Vector2(basePos.X, togglePadding*2), controls, () => ConfigObject.ShipWelding, (b) => ConfigObject.ShipWelding = b, allowEdit && ConfigObject.ClientWelding);
            CreateOption("Connect Subgrids", "Attempt to connect subgrids by removing incorrect heads and placing new ones.", compatibilityPos + new Vector2(basePos.X, togglePadding*3), controls, () => ConfigObject.ConnectSubgrids, (b) => ConfigObject.ConnectSubgrids = b, allowEdit && ConfigObject.ClientWelding);
            controls.Add(compatibility);

            Vector2 extraPos = new Vector2(0f, compatibilityPos.Y + togglePadding * 4.5f);
            MyGuiControlLabel extra = new MyGuiControlLabel(extraPos, new Vector2(0.25f, 0.03f), "Extra Features:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, textScale: 1f);
            CreateOption("Repair Projection", "Load a copy of a ship into projector so that it can be rebuilt if any accidents happen.", extraPos + new Vector2(basePos.X, togglePadding), controls, () => ConfigObject.RepairProjection, (b) => ConfigObject.RepairProjection = b, allowEdit);
            CreateOption("Align Projection", "Enable intuitive alignment of projections using the same keys you would use when aligning blocks normally.", extraPos + new Vector2(basePos.X, togglePadding*2), controls, () => ConfigObject.ProjectorAligner, (b) => ConfigObject.ProjectorAligner = b, allowEdit);
            CreateOption("Highlight Blocks", "Highlight projected blocks based on their status and completion.", extraPos + new Vector2(basePos.X, togglePadding*3), controls, () => ConfigObject.BlockHighlight, (b) => ConfigObject.BlockHighlight = b, allowEdit);
            CreateOption("Assemble Projections", "View a projection's component cost and queue it for assembly.", extraPos + new Vector2(basePos.X, togglePadding*4), controls, () => ConfigObject.CraftProjection, (b) => ConfigObject.CraftProjection = b, allowEdit);
            controls.Add(extra);

            // Register our reset settings function
            noButton.ButtonClicked += (_) => ResetConfig(controls);

            return messageBox;
        }

        private static void CreateOption(string name, string description, Vector2 position, MyGuiControls controls, Func<bool> getter, Action<bool> setter, bool allowEdit)
        {
            Vector2 labelSize = new Vector2(0.25f, 0.03f);

            MyGuiControlLabel label = new MyGuiControlLabel(position, labelSize, name);
            MyGuiControlCheckbox toggle = new MyGuiControlCheckbox(new Vector2(position.X*-1, position.Y), toolTip: description)
            {
                Enabled = allowEdit,
                Size = new Vector2(labelSize.Y, labelSize.Y),
                IsChecked = getter(),
                IsCheckedChanged = (x) => setter(x.IsChecked),
                Name = name,
            };

            controls.Add(label);
            controls.Add(toggle);
        }

        private static void ResetConfig(MyGuiControls controls)
        {
            Config.ResetConfig();

            foreach (MyGuiControlBase control in controls)
            {
                if (!(control is MyGuiControlCheckbox checkbox))
                    continue;

                if (checkbox.Name == "Show Warning Dialogs")
                    checkbox.IsChecked = ConfigObject.ShowDialogs;

                if (checkbox.Name == "Client Welding")
                    checkbox.IsChecked = ConfigObject.ClientWelding;

                if (checkbox.Name == "Ship Welding")
                    checkbox.IsChecked = ConfigObject.ShipWelding;

                if (checkbox.Name == "Connect Subgrids")
                    checkbox.IsChecked = ConfigObject.ConnectSubgrids;

                if (checkbox.Name == "Repair Projection")
                    checkbox.IsChecked = ConfigObject.RepairProjection;

                if (checkbox.Name == "Align Projection")
                    checkbox.IsChecked = ConfigObject.ProjectorAligner;

                if (checkbox.Name == "Highlight Blocks")
                    checkbox.IsChecked = ConfigObject.BlockHighlight;

                if (checkbox.Name == "Assemble Projections")
                    checkbox.IsChecked = ConfigObject.CraftProjection;
            }
        }
    }
}