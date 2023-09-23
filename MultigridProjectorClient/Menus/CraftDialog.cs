using MultigridProjectorClient.Utilities;
using Sandbox.Game;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace MultigridProjectorClient.Menus
{
    internal static class CraftDialog
    {
        private static readonly StringBuilder MessageCaption = new StringBuilder("Multigrid Projector - Assemble Projection");

        public static MyGuiScreenMessageBox CreateDialog(
            string assemblerName,
            HashSet<MyGuiControlTable.Row> rows,
            Action assembleAll = null,
            Action assembleMissing = null,
            Action onClosing = null)
        {
            // I find it easier to hijack a message box for this then making the UI from scratch
            // TODO: Some day turn this into a proper GUI
            MyGuiScreenMessageBox messageBox = MyGuiSandbox.CreateMessageBox(
                MyMessageBoxStyleEnum.Info,
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageText: new StringBuilder($"Assembler selected in the production tab:\n{assemblerName}"),
                messageCaption: MessageCaption,
                size: new Vector2(0.6f, 0.7f),
                onClosing: onClosing);

            // Get the (private) multiline text control so that we can change the text position
            MyGuiControlMultilineText messageBoxText = (MyGuiControlMultilineText)Reflection.GetValue(messageBox, "m_messageBoxText");
            messageBoxText.TextAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP;
            messageBoxText.Size = new Vector2(0.5f, 0.2f);
            messageBoxText.Position = new Vector2(0f, -0.24f);

            // Make the background color less transparent, as the default is very faint and this is an important message
            messageBox.BackgroundColor = new Vector4(1f, 1f, 1f, 10.0f);

            // Change the yes button text
            MyGuiControlButton yesButton = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_yesButton");
            yesButton.Text = "Assemble Missing";

            if (assembleMissing != null)
            {
                yesButton.SetToolTip(new MyToolTips($"Send all the 'Missing' components to '{assemblerName}'"));
                yesButton.ButtonClicked += (_) => assembleMissing();
            }
            else
            {
                yesButton.SetToolTip(new MyToolTips($"Assemble all the 'Missing' components"));
                yesButton.Enabled = false;
            }

            // Change the no button text
            MyGuiControlButton noButton = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_noButton");
            noButton.Text = "Assemble All";

            if (assembleAll != null)
            {
                noButton.SetToolTip(new MyToolTips($"Send all the 'Blueprint' components to '{assemblerName}'"));
                noButton.ButtonClicked += (_) => assembleAll();
            }
            else
            {
                noButton.SetToolTip(new MyToolTips($"Assemble all the 'Blueprint' components"));
                noButton.Enabled = false;
            }

            // Create a table with all the components and their quantities
            MyGuiControlTable componentTable = new MyGuiControlTable()
            {
                Position = new Vector2(0f, -0.2f),
                Size = new Vector2(0.85f * 0.6f, 0.3f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                ColumnsCount = 4,
                VisibleRowsCount = 12,
                BorderHighlightEnabled = false,
            };

            float name = 0.4f;
            float number = 0.2f;
            componentTable.SetCustomColumnWidths(new[] { name, number, number, number });
            componentTable.SetColumnName(0, new StringBuilder("Component"));
            componentTable.SetColumnName(1, new StringBuilder("Missing"));
            componentTable.SetColumnName(2, new StringBuilder("Inventory"));
            componentTable.SetColumnName(3, new StringBuilder("Blueprint"));

            // Order rows by missing components (cell index 1)
            List<MyGuiControlTable.Row> orderedRows = rows.OrderByDescending(x => int.Parse(x.GetCell(3).Text.ToString().Replace(",", ""))).ToList();
            for (int i = 0; i < orderedRows.Count; i++)
            {
                componentTable.Insert(i, orderedRows[i]);
            }

            MyGuiControls controls = (MyGuiControls)Reflection.GetValue(typeof(MyGuiScreenBase), messageBox, "m_controls");
            controls.Add(componentTable);

            return messageBox;
        }
    }
}
