using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
    /// <summary>
    ///     Updates the data sitting in the main listview if there is anything outstanding in
    ///     dt_fileDataToWriteStage3ReadyToWrite for the file
    /// </summary>
    /// <param name="lvi"></param>
    internal static async Task LwvUpdateRowFromDTWriteStage3ReadyToWrite(ListViewItem lvi)
    {
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        string tmpCoordinates;
        if (FrmMainAppInstance != null)
        {
            ListView lvw = FrmMainAppInstance.lvw_FileList;
            ListView.ColumnHeaderCollection lvchs = FrmMainAppInstance.ListViewColumnHeaders;

            int d = lvi.Index;
            string fileNameWithoutPath = lvi.Text;

            DataView dataViewRelevantRows = new(table: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite);
            dataViewRelevantRows.RowFilter = "fileNameWithoutPath = '" +
                                             fileNameWithoutPath +
                                             "'";
            DataTable dataTableRelevant = dataViewRelevantRows.ToTable();
            if (dataTableRelevant.Rows.Count > 0)
            {
                try
                {
                    lvw.BeginUpdate();
                    FrmMainApp.HandlerUpdateItemColour(lvw: lvw, itemText: fileNameWithoutPath, color: Color.Red);

                    foreach (DataRow drTagData in dataTableRelevant.Rows)
                    {
                        // theoretically we'd want to update the columns for each tag but for example when removing all data
                        // this becomes tricky bcs we're also firing a "-gps*=" tag.
                        string settingId = "clh_" +
                                           drTagData[columnIndex: 1];
                        string settingVal = drTagData[columnIndex: 2]
                            .ToString();
                        if (lvchs[key: settingId] != null)
                        {
                            try
                            {
                                lvi.SubItems[index: lvchs[key: "clh_" + drTagData[columnIndex: 1]]
                                                 .Index]
                                    .Text = settingVal;
                                //break;
                            }
                            catch
                            {
                                // nothing - basically this could happen if user navigates out of the folder
                            }
                        }

                        tmpCoordinates = lvi.SubItems[index: lvchs[key: "clh_GPSLatitude"]
                                                          .Index]
                                             .Text +
                                         ";" +
                                         lvi.SubItems[index: lvchs[key: "clh_GPSLongitude"]
                                                          .Index]
                                             .Text;

                        lvi.SubItems[index: lvchs[key: "clh_Coordinates"]
                                         .Index]
                            .Text = tmpCoordinates != ";"
                            ? tmpCoordinates
                            : "";
                    }
                }
                catch
                {
                    // nothing. 
                }
            }

            lvw.EndUpdate();
            if (d % 10 == 0)
            {
                Application.DoEvents();
            }
        }
    }

    /// <summary>
    ///     This drives the logic for "copying" (as in copy-paste) the geodata from one file to others.
    /// </summary>
    internal static void LwvCopyGeoData()
    {
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (FrmMainAppInstance.lvw_FileList.SelectedItems.Count == 1)
        {
            ListViewItem lvi = FrmMainAppInstance.lvw_FileList.SelectedItems[index: 0];
            if (File.Exists(path: Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text)))
            {
                FrmMainApp.FileDateCopySourceFileNameWithPath = Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text);
                FrmMainApp.DtFileDataCopyPool.Rows.Clear();
                List<string> listOfTagsToCopy = new()
                {
                    "Coordinates",
                    "GPSLatitude",
                    "GPSLatitudeRef",
                    "GPSLongitude",
                    "GPSLongitudeRef",
                    "GPSSpeed",
                    "GPSSpeedRef",
                    "GPSAltitude",
                    "GPSAltitudeRef",
                    "Country",
                    "CountryCode",
                    "State",
                    "City",
                    "Sub_location",
                    "DestCoordinates",
                    "GPSDestLatitude",
                    "GPSDestLatitudeRef",
                    "GPSDestLongitude",
                    "GPSDestLongitudeRef",
                    "GPSImgDirection",
                    "GPSImgDirectionRef",
                    "TakenDate",
                    "CreateDate",
                    "OffsetTime"
                };

                foreach (ColumnHeader clh in FrmMainAppInstance.lvw_FileList.Columns)
                {
                    if (listOfTagsToCopy.IndexOf(item: clh.Name.Substring(startIndex: 4)) >= 0)
                    {
                        DataRow dr_FileDataRow = FrmMainApp.DtFileDataCopyPool.NewRow();
                        dr_FileDataRow[columnName: "settingId"] = clh.Name.Substring(startIndex: 4);
                        dr_FileDataRow[columnName: "settingValue"] = lvi.SubItems[index: clh.Index]
                            .Text;
                        FrmMainApp.DtFileDataCopyPool.Rows.Add(row: dr_FileDataRow);
                    }
                }
            }
        }
        else
        {
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningTooManyFilesSelected"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    ///     This drives the logic for "pasting" (as in copy-paste) the geodata from one file to others.
    ///     See further comments inside
    /// </summary>
    internal static async void LwvPasteGeoData()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        // check there's anything in copy-pool
        if (FrmMainApp.DtFileDataCopyPool.Rows.Count > 0 && frmMainAppInstance != null)
        {
            FrmPasteWhat frmPasteWhat = new(initiator: frmMainAppInstance.Name);
            frmPasteWhat.ShowDialog();
        }
        else
        {
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNothingToPaste"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    ///     Extracted method for navigating to a set of coordinates on the map.
    /// </summary>
    /// <returns>Nothing in reality</returns>
    internal static async Task LvwItemClickNavigate()
    {
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        HtmlAddMarker = "";
        HsMapMarkers.Clear();

        foreach (ListViewItem lvw_FileListItem in FrmMainAppInstance.lvw_FileList.SelectedItems)
        {
            // make sure file still exists. just in case someone deleted it elsewhere
            string fileNameWithPath = Path.Combine(path1: FrmMainApp.FolderName, path2: lvw_FileListItem.Text);
            if (File.Exists(path: fileNameWithPath) && lvw_FileListItem.SubItems.Count > 1)
            {
                string firstSelectedItem = lvw_FileListItem.SubItems[index: FrmMainAppInstance.lvw_FileList.Columns[key: "clh_Coordinates"]
                                                                         .Index]
                    .Text;
                if (firstSelectedItem != "-" && firstSelectedItem != "")
                {
                    string strLat;
                    string strLng;
                    try
                    {
                        strLat = lvw_FileListItem.SubItems[index: FrmMainAppInstance.lvw_FileList.Columns[key: "clh_Coordinates"]
                                                               .Index]
                            .Text.Split(';')[0]
                            .Replace(oldChar: ',', newChar: '.');
                        strLng = lvw_FileListItem.SubItems[index: FrmMainAppInstance.lvw_FileList.Columns[key: "clh_Coordinates"]
                                                               .Index]
                            .Text.Split(';')[1]
                            .Replace(oldChar: ',', newChar: '.');
                    }
                    catch
                    {
                        strLat = "fail";
                        strLng = "fail";
                    }

                    double parsedLat;
                    double parsedLng;
                    if (double.TryParse(s: strLat, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strLng, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
                    {
                        FrmMainAppInstance.tbx_lat.Text = strLat;
                        FrmMainAppInstance.tbx_lng.Text = strLng;
                        HsMapMarkers.Add(item: (strLat, strLng));
                    }
                }
                else if (SResetMapToZero)
                {
                    FrmMainAppInstance.tbx_lat.Text = "0";
                    FrmMainAppInstance.tbx_lng.Text = "0";
                    HsMapMarkers.Add(item: ("0", "0"));
                }
                // leave as-is (most likely the last photo)
            }

            // don't try and create an preview img unless it's the last file
            if (FrmMainAppInstance.lvw_FileList.FocusedItem != null && lvw_FileListItem.Text != null)
            {
                if (FrmMainAppInstance.lvw_FileList.FocusedItem.Text == lvw_FileListItem.Text ||
                    FrmMainAppInstance.lvw_FileList.SelectedItems[index: 0]
                        .Text ==
                    lvw_FileListItem.Text)
                {
                    if (File.Exists(path: fileNameWithPath))
                    {
                        await LvwItemCreatePreview(fileNameWithPath: fileNameWithPath);
                    }
                    else if (Directory.Exists(path: fileNameWithPath))
                    {
                        // nothing.
                    }
                    else // check it's a drive? --> if so, don't do anything, otherwise warn the item is gone
                    {
                        bool isDrive = LvwItemIsDrive(lvwFileListItem: lvw_FileListItem);

                        if (isDrive)
                        {
                            // nothing
                        }
                        else
                        {
                            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorFileGoneMissing") + fileNameWithPath, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Checks if a ListViewItem is a drive (e.g. C:\) or not
    /// </summary>
    /// <param name="lvwFileListItem">The ListViewItem to check</param>
    /// <returns></returns>
    internal static bool LvwItemIsDrive(ListViewItem lvwFileListItem)
    {
        DriveInfo[] allDrives = DriveInfo.GetDrives();
        bool isDrive = false;
        foreach (DriveInfo d in allDrives)
        {
            try
            {
                if (lvwFileListItem.Text.Contains(value: "(" + d.Name.Replace(oldValue: "\\", newValue: "") + ")"))
                {
                    isDrive = true;
                    break;
                }
            }
            catch
            {
                //MessageBox.Show(ex.Message);
            }
        }

        return isDrive;
    }

    /// <summary>
    ///     Triggers the "create preview" process for the file it's sent to check
    /// </summary>
    /// <param name="fileNameWithPath">Filename w/ path to check</param>
    /// <returns></returns>
    internal static async Task LvwItemCreatePreview(string fileNameWithPath)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (frmMainAppInstance != null)
        {
            frmMainAppInstance.pbx_imagePreview.Image = null;
            // via https://stackoverflow.com/a/8701748/3968494
            Image img = null;

            FileInfo fi = new(fileName: fileNameWithPath);
            try
            {
                using Bitmap bmpTemp = new(filename: fileNameWithPath);
                img = new Bitmap(original: bmpTemp);
                frmMainAppInstance.pbx_imagePreview.Image = img;
            }
            catch
            {
                // nothing.
            }

            if (img == null)
            {
                string generatedFileName = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: frmMainAppInstance.lvw_FileList.SelectedItems[index: 0]
                                                                                                         .Text +
                                                                                                     ".jpg");
                // don't run the thing again if file has already been generated
                if (!File.Exists(path: generatedFileName))
                {
                    await ExifGetImagePreviews(fileNameWithoutPath: fileNameWithPath);
                }

                //sometimes the file doesn't get created. (ie exiftool may fail to extract a preview.)
                string ip_ErrorMsg = HelperStatic.DataReadSQLiteObjectText(
                    languageName: frmMainAppInstance.AppLanguage,
                    objectType: "PictureBox",
                    objectName: "pbx_imagePreview",
                    actionType: "CouldNotRetrieve"
                    );
                if (File.Exists(path: generatedFileName))
                {
                    try
                    {
                        using Bitmap bmpTemp = new(filename: generatedFileName);
                        img = new Bitmap(original: bmpTemp);
                        frmMainAppInstance.pbx_imagePreview.Image = img;
                    }
                    catch
                    {
                        frmMainAppInstance.pbx_imagePreview.SetErrorMessage(ip_ErrorMsg);
                    }
                } else
                {
                    frmMainAppInstance.pbx_imagePreview.SetErrorMessage(ip_ErrorMsg);
                }
            }
        }
    }

    /// <summary>
    ///     This updates the lbl_ParseProgress with count of items with geodata.
    /// </summary>
    internal static void LvwCountItemsWithGeoData()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        int fileCount = 0;
        int filesWithGeoData = 0;
        string fileNameWithoutPath;

        if (frmMainAppInstance != null)
        {
            foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.Items)
            {
                fileNameWithoutPath = lvi.Text;
                if (File.Exists(path: Path.Combine(path1: FrmMainApp.FolderName, path2: fileNameWithoutPath)))
                {
                    fileCount++;
                }

                if (lvi.SubItems.Count > 1)
                {
                    if (lvi.SubItems[index: 1]
                            .Text.Replace(oldValue: "-", newValue: "") !=
                        "")
                    {
                        filesWithGeoData++;
                    }
                }
            }

            FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Ready. Files: Total: " + fileCount + " Geodata: " + filesWithGeoData);
        }
    }
}