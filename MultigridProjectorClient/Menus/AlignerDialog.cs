using MultigridProjectorClient.Utilities;
using Sandbox.Game;
using Sandbox.Graphics.GUI;
using System;
using System.Text;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace MultigridProjectorClient.Menus
{
    internal static class AlignerDialog
    {
        private static readonly string KeySplitter = " | ";
        private static readonly StringBuilder MessageCaption = new StringBuilder("Multigrid Projector - Projection Alignment");
        
        private static readonly StringBuilder HeadingText = new StringBuilder(
            "\n\n\n\n\n\n" +
            "Translation Controls:\n" +
            "\n\n\n\n\n\n\n" +
            "Rotation Controls:\n" +
            "\n\n\n\n\n\n");
        
        private static readonly StringBuilder MessageText = new StringBuilder(
            "This will capture all input until stopped (press ESC to cancel).\n" +
            $"Hold {GetControlType(MyControlsSpace.SPRINT)} to suspend alignment.\n" +
            "While suspended you are free to reposition yourself and interact with the world.\n" +
            "All translations and rotations are relative to your charcter's head orientation.\n" +
            "These controls are based on the controls for block alignment, and respect your current binds.\n" +
            "\n\n" +
            "Forwards:\n" +
            "Backwards:\n" +
            "Left:\n" +
            "Right:\n" +
            "Upwards:\n" +
            "Downwards:\n" +
            "\n\n" +
            "Roll Left:\n" +
            "Roll Right:\n" +
            "Pitch Upwards:\n" +
            "Pitch Downwards:\n" +
            "Yaw Left:\n" +
            "Yaw Right:\n");

        private static readonly StringBuilder KeybindText = new StringBuilder(
            "\n\n\n\n\n\n\n" +
            $"{GetControlType(MyControlsSpace.FORWARD)}\n" +
            $"{GetControlType(MyControlsSpace.BACKWARD)}\n" +
            $"{GetControlType(MyControlsSpace.STRAFE_LEFT, MyControlsSpace.ROTATION_LEFT)}\n" +
            $"{GetControlType(MyControlsSpace.STRAFE_RIGHT, MyControlsSpace.ROTATION_RIGHT)}\n" +
            $"{GetControlType(MyControlsSpace.JUMP, MyControlsSpace.ROTATION_UP)}\n" +
            $"{GetControlType(MyControlsSpace.CROUCH, MyControlsSpace.ROTATION_DOWN)}\n" +
            "\n\n" +
            $"{GetControlType(MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE)}\n" +
            $"{GetControlType(MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE)}\n" +
            $"{GetControlType(MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE)}\n" +
            $"{GetControlType(MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE)}\n" +
            $"{GetControlType(MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE)}\n" +
            $"{GetControlType(MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE)}\n");

        private static string GetControlType(params MyStringId[] controlEnums)
        {
            string output = "";
            foreach (MyStringId controlEnum in controlEnums)
            {
                IMyControl control = MyInput.Static.GetGameControl(controlEnum);

                string key = control.GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);
                string key2 = control.GetControlButtonName(MyGuiInputDeviceEnum.KeyboardSecond);
                string mouse = control.GetControlButtonName(MyGuiInputDeviceEnum.Mouse);

                if (key != "")
                    output += key + KeySplitter;
                if (key2 != "")
                    output += key2 + KeySplitter;
                if (mouse != "")
                    output += mouse + KeySplitter;
            }

            if (output.EndsWith(KeySplitter))
                output = output.Substring(0, output.LastIndexOf(KeySplitter));

            return output;
        }

        public static MyGuiScreenMessageBox CreateDialog(Action onClosing)
        {
            // I find it easier to hijack a message box for this then making the UI from scratch
            // TODO: Some day turn this into a proper GUI
            MyGuiScreenMessageBox messageBox = MyGuiSandbox.CreateMessageBox(
                MyMessageBoxStyleEnum.Info,
                messageText: MessageText,
                messageCaption: MessageCaption,
                size: new Vector2(0.6f, 0.7f));

            messageBox.DataUnloading += (_) => onClosing();

            // Make the background color less transparent, as the default is very faint and this is an important message
            messageBox.BackgroundColor = new Vector4(1f, 1f, 1f, 10.0f);

            // Get the (private) multiline text control so that we can change the text alignment
            MyGuiControlMultilineText messageBoxText = (MyGuiControlMultilineText)Reflection.GetValue(messageBox, "m_messageBoxText");
            messageBoxText.TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            // Make another multiline text control but with the text aligned to the opposite side
            MyGuiControlMultilineText keybindText = new MyGuiControlMultilineText(
                new Vector2(0.215f, -0.00175f),
                messageBoxText.Size,
                Vector4.One,
                contents: KeybindText,
                font: messageBoxText.Font,
                textScale: 0.8f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);

            // Make a final multiline text control but with the text aligned to the center
            MyGuiControlMultilineText centerText = new MyGuiControlMultilineText(
                new Vector2(0f, -0.00175f),
                messageBoxText.Size,
                Vector4.One,
                contents: HeadingText,
                font: messageBoxText.Font,
                textScale: 0.8f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);

            MyGuiControls controls = (MyGuiControls)Reflection.GetValue(typeof(MyGuiScreenBase), messageBox, "m_controls");
            controls.Add(keybindText);
            controls.Add(centerText);

            // Change the button text
            MyGuiControlButton button = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_yesButton");
            button.Text = "Acknowledge";

            return messageBox;
        }

    }
}