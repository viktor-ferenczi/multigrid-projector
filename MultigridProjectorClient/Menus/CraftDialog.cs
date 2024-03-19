using MultigridProjectorClient.Utilities;
using Sandbox;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game;
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
            List<string> bomLines,
            Action<MyDefinitionId, int> assembleFunc = null,
            Action onClosing = null)
        {
            // I find it easier to hijack a message box for this then making the UI from scratch
            // TODO: Some day turn this into a proper GUI
            MyGuiScreenMessageBox messageBox = MyGuiSandbox.CreateMessageBox(
                MyMessageBoxStyleEnum.Info,
                buttonType: MyMessageBoxButtonsType.YES_NO_CANCEL,
                messageText: new StringBuilder($"Assembler selected in the production tab:\n{assemblerName}"),
                messageCaption: MessageCaption,
                size: new Vector2(0.6f, 0.7f),
                onClosing: onClosing
            );

            // Get the (private) multiline text control so that we can change the text position
            MyGuiControlMultilineText messageBoxText = (MyGuiControlMultilineText)Reflection.GetValue(messageBox, "m_messageBoxText");
            messageBoxText.TextAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP;
            messageBoxText.Size = new Vector2(0.5f, 0.2f);
            messageBoxText.Position = new Vector2(0f, -0.24f);

            // Make the background color less transparent, as the default is very faint and this is an important message
            messageBox.BackgroundColor = new Vector4(1f, 1f, 1f, 10.0f);

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

            // Change the yes button text
            MyGuiControlButton yesButton = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_yesButton");
            yesButton.Text = "Assemble Missing";

            int totalMissing = 0;
            foreach (Row row in componentTable.Rows)
            {
                totalMissing += GetCellData<int>(row, 1);
            }

            if (assembleFunc != null)
            {
                yesButton.SetToolTip(new MyToolTips($"Send the {totalMissing:N0} 'Missing' components to '{assemblerName}'"));
                yesButton.ButtonClicked += (_) => Assemble(assembleFunc, componentTable, 1);
            }
            else
            {
                yesButton.SetToolTip(new MyToolTips($"Assemble the {totalMissing:N0} 'Missing' components"));
                yesButton.Enabled = false;
            }

            // Change the no button text
            MyGuiControlButton noButton = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_noButton");
            noButton.Text = "Assemble Selected";

            noButton.SetToolTip(new MyToolTips($"Assemble any selected 'Missing' components"));
            noButton.ButtonClicked += (_) => Assemble(assembleFunc, componentTable.SelectedRow, 1);
            noButton.Enabled = false;
            
            // Change to Cancel button text
            // FIXME: Prevent button from closing dialog
            MyGuiControlButton cancelButton = (MyGuiControlButton)Reflection.GetValue(messageBox, "m_cancelButton");
            cancelButton.Text = "Copy BoM";
            cancelButton.SetToolTip(new MyToolTips("Copy an Isy-compatible Bill of Materials to clipboard, for use with Special containers."));
            cancelButton.ButtonClicked += (_) => CopyBom(bomLines);

            componentTable.ItemSelected += (table, eventArgs) =>
            {
                if (componentTable.SelectedRow == null)
                {
                    noButton.Enabled = false;
                    noButton.SetToolTip(new MyToolTips($"Assemble any selected 'Missing' components"));
                    return;
                }

                string selectedName = GetCellText(componentTable.SelectedRow, 0);
                int selectedAmount = GetCellData<int>(componentTable.SelectedRow, 1);

                if (selectedAmount == 0)
                {
                    noButton.Enabled = false;
                    noButton.SetToolTip(new MyToolTips($"No {selectedName}{(selectedAmount != 1 ? "s" : "")} to assemble"));

                    return;
                }

                if (assembleFunc == null)
                {
                    noButton.SetToolTip(new MyToolTips($"Assemble {selectedAmount:N0} {selectedName}{(selectedAmount != 1 ? "s" : "")}"));
                    noButton.Enabled = false;

                    return;
                }

                noButton.SetToolTip(new MyToolTips($"Send {selectedAmount:N0} {selectedName}{(selectedAmount != 1 ? "s" : "")} to '{assemblerName}'"));
                noButton.Enabled = true;
            };

            return messageBox;

        }

        private static void Assemble(Action<MyDefinitionId, int> assembleFunc, MyGuiControlTable table, int column)
        {
            foreach (Row row in table.Rows)
            {
                Assemble(assembleFunc, row, column);
            }
        }

        private static void Assemble(Action<MyDefinitionId, int> assembleFunc, Row row, int column)
        {
            MyDefinitionId id = GetCellData<MyDefinitionId>(row, 0);
            int amount = GetCellData<int>(row, column);

            assembleFunc(id, amount);
        }
        
        private static void CopyBom(List<string> bomLines)
        {
            MyClipboardHelper.SetClipboard(string.Join("\n", bomLines));
            MyAPIGateway.Utilities.ShowNotification("Copied BoM to Clipboard", 6000);
        }

        private static string GetCellText(Row row, int column)
        {
            return row.GetCell(column).Text.ToString();
        }

        private static T GetCellData<T>(Row row, int column)
        {
            return (T)row.GetCell(column).UserData;
        }

        private static void SortRows(ref List<Row> rows, int column, bool inverse = false)
        {
            rows.Sort((row1, row2) =>
            {
                string cell1 = GetCellText(row1, column);
                string cell2 = GetCellText(row2, column);

                object cellData1 = GetCellData<object>(row1, column);
                object cellData2 = GetCellData<object>(row2, column);

                // Numerical Order
                if (cellData1 is int value1 && cellData2 is int value2)
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
            Row selectedRow = table.SelectedRow;
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

            if (selectedRow != null)
            {
                table.SelectedRow = selectedRow;
            }
        }
    }
}
