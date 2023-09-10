using MultigridProjectorClient.Utilities;
using Sandbox.Graphics.GUI;
using System;
using System.Text;
using VRage;
using VRage.Scripting;
using VRage.Utils;
using VRageMath;

namespace MultigridProjectorClient.Menus
{
    internal static class CraftDialog
    {
        private static readonly StringBuilder MessageCaption = new StringBuilder("Multigrid Projector - Assemble Projection");

        public static MyGuiScreenMessageBox CreateDialog(StringBuilder heading, StringBuilder messageTextLeft, StringBuilder messageTextRight, Action assembleAll, Action assembleMissing)
        {
            // I find it easier to hijack a message box for this then making the UI from scratch
            // TODO: Some day turn this into a proper GUI
            MyGuiScreenMessageBox messageBox = MyGuiSandbox.CreateMessageBox(
                MyMessageBoxStyleEnum.Info,
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageText: messageTextLeft,
                messageCaption: MessageCaption,
                size: new Vector2(0.6f, 0.7f));

            // Make the background color less transparent, as the default is very faint and this is an important message
            messageBox.BackgroundColor = new Vector4(1f, 1f, 1f, 10.0f);

            // Get the (private) multiline text control so that we can change the text alignment
            MyGuiControlMultilineText messageBoxText = (MyGuiControlMultilineText)Reflection.GetValue(messageBox, "m_messageBoxText");
            messageBoxText.TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            // Make another multiline text control but with the text aligned to the opposite side
            MyGuiControlMultilineText keybindText = new MyGuiControlMultilineText(
                new Vector2(0.215f, -0.012f),
                messageBoxText.Size,
                Vector4.One,
                contents: messageTextRight,
                font: messageBoxText.Font,
                textScale: 0.8f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);

            // Make a final multiline text control but with the text aligned to the center
            MyGuiControlMultilineText centerText = new MyGuiControlMultilineText(
                new Vector2(0f, -0.00175f),
                messageBoxText.Size,
                Vector4.One,
                contents: heading,
                font: messageBoxText.Font,
                textScale: 0.8f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);

            MyGuiControls controls = (MyGuiControls)Reflection.GetValue(typeof(MyGuiScreenBase), messageBox, "m_controls");
            controls.Add(keybindText);
            controls.Add(centerText);

            // Change the yes button text
            MyGuiControlButton yesButton = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_yesButton");
            yesButton.Text = "Assemble All";
            yesButton.SetToolTip(new MyToolTips("Queue all the components for assembly"));
            yesButton.ButtonClicked += (_) => assembleAll();

            // Change the no button text
            MyGuiControlButton noButton = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_noButton");
            noButton.Text = "Assemble Missing";
            noButton.SetToolTip(new MyToolTips("Only queue missing components for assembly"));
            noButton.ButtonClicked += (_) => assembleMissing();

            return messageBox;
        }
    }
}
