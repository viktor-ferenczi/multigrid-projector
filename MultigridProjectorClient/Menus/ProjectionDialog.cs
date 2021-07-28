using MultigridProjectorClient.Utilities;
using Sandbox.Graphics.GUI;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace MultigridProjectorClient.Menus
{
    internal static class ProjectionDialog
    {
        private static readonly StringBuilder MessageCaption = new StringBuilder("Multigrid Projector - Compatibility Mode");

        private static readonly StringBuilder MessageText = new StringBuilder(
            "This server does not have the Multigrid Projector plugin installed.\n" +
            "Blocks on projected subgrids will be placed normally and their properties copied over.\n" +
            "All of this is handled by the plugin and will take place behind the scenes.\n" +
            "\n" +
            "Limitations:\n" +
            "This is not a true multi-grid projection!\n" +
            "Consider it a build assist that automates work you would do if building without the plugin.\n" +
            "Do not expect to seamlessly weld complex multi-grid contraptions without any effort.\n" +
            "Simple subgrids on common workshop builds can be built without any issue.\n\n");

        private static readonly StringBuilder ShipWeldingText = new StringBuilder(
            "Ship Welders:\n" +
            "Block welders are handled by the server rather then the client.\n" +
            "All blocks that are valid ship welding targets will be placed from the client automatically.\n" +
            "This will only be done for welders on the craft you are currently piloting to prevent lag.\n\n");

        private static readonly StringBuilder ConnectSubgridsText = new StringBuilder(
            "Connect Subgrids:\n" +
            "Subparts of mechanical blocks will be copied over when welding.\n" +
            "If in survival, you will need to remove the existing subpart before a new one is created.\n" +
            "Due to expanded hitboxes not all parts can be placed close enough to automatically attach.\n" +
            "Smaller blocks should attach; you might need to manually move some of the larger ones.\n\n");

        private static readonly StringBuilder CompatibilityText = new StringBuilder(
            "Block Compatibility:\n" +
            "- All vanilla blocks up are supported.\n" +
            "- Modded blocks should have their properties configured correctly.\n" +
            "\n" +
            "If you encounter any bugs make an issue on the GitHub!\n");

        public static MyGuiScreenMessageBox CreateDialog()
        {
            // Create the message text based on the enabled options
            Vector2 size = new Vector2(0.65f, 0.53f);
            StringBuilder message = new StringBuilder();
            message.Append(MessageText);

            if (Config.CurrentConfig.ShipWelding)
            {
                message.Append(ShipWeldingText);
                size += new Vector2(0f, 0.1f);
            }

            if (Config.CurrentConfig.ConnectSubgrids)
            {
                message.Append(ConnectSubgridsText);
                size += new Vector2(0f, 0.15f);
            }

            message.Append(CompatibilityText);

            // I find it easier to hijack a message box for this then making the UI from scratch
            // TODO: Some day turn this into a proper GUI
            MyGuiScreenMessageBox messageBox = MyGuiSandbox.CreateMessageBox(
                MyMessageBoxStyleEnum.Info,
                messageText: message,
                messageCaption: MessageCaption,
                size: size);

            // Make the background color less transparent, as the default is very faint and this is an important message
            messageBox.BackgroundColor = new Vector4(1f, 1f, 1f, 10.0f);

            // Get the (private) multiline text control so that we can change the text alignment
            MyGuiControlMultilineText messageBoxText = (MyGuiControlMultilineText)Reflection.GetValue(messageBox, "m_messageBoxText");
            messageBoxText.TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;


            // Change the button text
            MyGuiControlButton button = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_yesButton");
            button.Text = "Acknowledge";

            return messageBox;
        }

        public static MyGuiScreenMessageBox CreateUnsupportedDialog()
        {
            MyGuiScreenMessageBox messageBox = MyGuiSandbox.CreateMessageBox(
                MyMessageBoxStyleEnum.Info,
                messageText: new StringBuilder("Client welding is disabled and this server does not have Multigrid Projector.\nOnly the largest grid will be weldable."),
                messageCaption: new StringBuilder("Multigrid Projector - Welding Unsupported"),
                size: new Vector2(0.65f, 0.25f));

            // Make the background color less transparent, as the default is very faint and this is an important message
            messageBox.BackgroundColor = new Vector4(1f, 1f, 1f, 10.0f);

            // Change the button text
            MyGuiControlButton button = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_yesButton");
            button.Text = "Acknowledge";

            return messageBox;
        }
    }
}