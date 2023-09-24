using MultigridProjectorClient.Utilities;
using Sandbox;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using static Sandbox.Graphics.GUI.MyGuiControlTable;

namespace MultigridProjectorClient.Menus
{
    internal static class CraftDialog
    {
        private static readonly StringBuilder MessageCaption = new StringBuilder("Multigrid Projector - Assemble Projection");

        public static MyGuiScreenMessageBox CreateDialog(
            string assemblerName,
            HashSet<Row> rows,
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
                yesButton.SetToolTip(new MyToolTips("Assemble all the 'Missing' components"));
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
                noButton.SetToolTip(new MyToolTips("Assemble all the 'Blueprint' components"));
                noButton.Enabled = false;
            }

            // Create a table with all the components and their quantities
            MyGuiControlTable componentTable = new MyGuiControlTable
            {
                Position = new Vector2(0f, -0.2f),
                Size = new Vector2(0.85f * 0.6f, 0.3f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                ColumnsCount = 4,
                VisibleRowsCount = 12,
                BorderHighlightEnabled = false,
            };

            const float name = 0.4f;
            const float number = 0.2f;
            componentTable.SetCustomColumnWidths(new[] { name, number, number, number });
            componentTable.SetColumnName(0, new StringBuilder("Component"));
            componentTable.SetColumnName(1, new StringBuilder("Missing"));
            componentTable.SetColumnName(2, new StringBuilder("Inventory"));
            componentTable.SetColumnName(3, new StringBuilder("Blueprint"));

            componentTable.ColumnClicked += (table, column) =>
            {
                // If the column is > 0 it contains a number which we sort by descending order
                // Otherwise we sort by ascending (alphabetical) order
                bool invert = column > 0;

                // Invert sorting if the column is already sorted
                List<Row> rowsCopy = new List<Row>(table.Rows);
                SortRows(ref rowsCopy, column, invert);

                if (table.Rows.SequenceEqual(rowsCopy))
                {
                    SortByColumn(table, column, !invert);
                }
                else
                {
                    SortByColumn(table, column, invert);
                }

                // The click sound is not played so we play it ourself
                MyGuiSoundManager.PlaySound(VRage.Audio.GuiSounds.MouseClick);
            };

            foreach (Row row in rows)
            {
                componentTable.Add(row);
            }

            SortByColumn(componentTable, 3, true);

            MyGuiControls controls = (MyGuiControls)Reflection.GetValue(typeof(MyGuiScreenBase), messageBox, "m_controls");
            controls.Add(componentTable);

            return messageBox;
        }
        private static string GetCellText(Row row, int column)
        {
            return row.GetCell(column).Text.ToString();
        }

        private static void SortRows(ref List<Row> rows, int column, bool inverse = false)
        {
            rows.Sort((row1, row2) =>
            {
                string cell1 = GetCellText(row1, column);
                string cell2 = GetCellText(row2, column);

                // Numerical Order
                if (int.TryParse(cell1.Replace(",", ""), out int value1)
                    && int.TryParse(cell2.Replace(",", ""), out int value2))
                {
                    // Sort by name if values match
                    if (value1 == value2)
                    {
                        string name1 = GetCellText(row1, 0);
                        string name2 = GetCellText(row2, 0);

                        if (inverse)
                        {
                            return name2.CompareTo(name1);
                        }

                        return name1.CompareTo(name2);
                    }

                    return value1.CompareTo(value2);
                }

                // Alphabetical Order
                return cell1.CompareTo(cell2);
            });

            if (inverse)
            {
                rows.Reverse();
            }
        }

        private static void SortByColumn(MyGuiControlTable table, int column, bool inverse = false)
        {
            List<Row> rows = new List<Row>(table.Rows);

            foreach (Row row in rows)
            {
                table.Remove(row);
            }

            SortRows(ref rows, column, inverse);

            for (int i = 0; i < rows.Count; i++)
            {
                table.Insert(i, rows[i]);
            }
        }
    }
}
