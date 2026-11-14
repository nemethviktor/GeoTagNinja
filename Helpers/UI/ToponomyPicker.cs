using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.View.Forms;
using GeoTagNinja.Model;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     The WinForms dialog that lets the user choose between several GeoNames results for a coordinate. Moved out of
///     ReadExifData so that reading code has no UI in it; reached through <see cref="UiServices.PickToponomyRow" />.
/// </summary>
internal static class ToponomyPicker
{
    /// <summary>
    ///     Scrolls the file list to the file in question, shows its preview, and asks the user which row to use.
    /// </summary>
    /// <returns>Zero-based index of the chosen row.</returns>
    internal static int Pick(DataTable dtSQLToponomyData,
                             string fileNameWithoutPath)
    {
                FrmMainApp frmMainAppInstance = FrmMainApp.Instance;
                // scroll to the file in question and show the image of it...makes life a lot easier
                if (!string.IsNullOrEmpty(value: fileNameWithoutPath))
                {
                    string fileNameWithPath =
                        Path.Combine(path1: HelperVariables.FolderName, path2: fileNameWithoutPath);
                    ListViewItem lvi =
                        frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath);
                    frmMainAppInstance.lvw_FileList.FocusedItem = lvi;
                    DirectoryElement directoryElement = lvi.Tag as DirectoryElement;
                    frmMainAppInstance.lvw_FileList.EnsureVisible(index: lvi.Index);
                    _ = ReadGetImagePreviews.GenericCreateImagePreview(
                        directoryElement: directoryElement,
                        initiator: ReadGetImagePreviews.Initiator.FrmMainAppAPIDataSelection
                    );
                    Application.DoEvents();
                }

        return ShowPicker(dtIn: dtSQLToponomyData, dtSQLToponomyData: dtSQLToponomyData);
    }

            static int ShowPicker(DataTable dtIn, DataTable dtSQLToponomyData)
            {
                Form FrmPickDataFromAPIBox = new()
                {
                    Text = HelperControlAndMessageBoxHandling.ReturnControlText(
                        controlName: "FrmPickDataFromAPIBox",
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.Form),
                    MinimizeBox = false,
                    MaximizeBox = false,
                    ShowIcon = false,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.CenterScreen
                };

                FlowLayoutPanel panel = new();

                ListView lvwDataChoices = new()
                {
                    Size = new Size(width: 800, height: 200),
                    View = System.Windows.Forms.View.Details,
                    MultiSelect = false,
                    FullRowSelect = true
                };

                _ = lvwDataChoices.Columns.Add(text: "Index");

                foreach (DataColumn dc in dtIn.Columns)
                {
                    _ = lvwDataChoices.Columns.Add(text: dc.ColumnName, width: -2);
                }

                lvwDataChoices.MouseDoubleClick += (sender,
                                                    args) =>
                {
                    ListViewHitTestInfo info = lvwDataChoices.HitTest(x: args.X, y: args.Y);
                    ListViewItem item = info.Item;

                    if (item != null)
                    {
                        FrmPickDataFromAPIBox.Close();
                    }
                };

                foreach (DataRow drItem in dtSQLToponomyData.Rows)
                {
                    // make it not-zero based.
                    ListViewItem lvi = new(text: (dtSQLToponomyData.Rows.IndexOf(row: drItem) +
                                                  1)
                       .ToString());
                    foreach (DataColumn dc in dtIn.Columns)
                    {
                        string dataToAdd = $"{drItem[column: dc]}";

                        _ = lvi.SubItems.Add(text: dataToAdd);
                    }

                    _ = lvwDataChoices.Items.Add(value: lvi);
                }

                lvwDataChoices.Items[index: 0]
                              .Selected = true;
                lvwDataChoices.Select();

                lvwDataChoices.KeyUp += (sender,
                                         args) =>
                {
                    if (args.KeyCode == Keys.Enter)
                    {
                        if (lvwDataChoices.SelectedItems.Count == 1)
                        {
                            FrmPickDataFromAPIBox.Close();
                        }
                    }
                };

                panel.Controls.Add(value: lvwDataChoices);
                panel.SetFlowBreak(control: lvwDataChoices, value: true);

                Button btn_Generic_OK = new()
                {
                    Text = HelperControlAndMessageBoxHandling.ReturnControlText(
                        controlName: "Generic_OK",
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.Button)
                };
                btn_Generic_OK.Click += (sender,
                                         e) =>
                {
                    FrmPickDataFromAPIBox.Close();
                };
                btn_Generic_OK.Location = new Point(x: 10, y: lvwDataChoices.Bottom + 15);
                btn_Generic_OK.AutoSize = true;
                panel.Controls.Add(value: btn_Generic_OK);

                panel.Padding = new Padding(all: 5);
                panel.AutoSize = true;

                FrmPickDataFromAPIBox.Controls.Add(value: panel);
                FrmPickDataFromAPIBox.MinimumSize = new Size(width: lvwDataChoices.Width + 40,
                    height: btn_Generic_OK.Bottom + 20);

                _ = FrmPickDataFromAPIBox.ShowDialog();

                try
                {
                    return lvwDataChoices.SelectedItems[index: 0]
                                         .Index;
                }
                catch
                {
                    return 0;
                }
            }
}
