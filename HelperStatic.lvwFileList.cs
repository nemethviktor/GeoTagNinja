using System;
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
    #region lvwFileList

    /// <summary>
    ///     Updates the data sitting in the main listview if there is anything outstanding in
    ///     dt_fileDataToWriteStage3ReadyToWrite for the file
    /// </summary>
    internal static void LwvUpdateRowFromDTWriteStage3ReadyToWrite()
    {
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        ListView.ColumnHeaderCollection lvchs = FrmMainAppInstance.ListViewColumnHeaders;
        ListViewItem lvi;

        string tmpCoordinates;

        foreach (DataRow dr_ThisDataRow in FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Rows)
        {
            try
            {
                lvi = FrmMainAppInstance.lvw_FileList.FindItemWithText(text: dr_ThisDataRow[columnIndex: 0]
                                                                           .ToString());
                // theoretically we'd want to update the columns for each tag but for example when removing all data
                // this becomes tricky bcs we're also firing a "-gps*=" tag.
                if (lvchs[key: "clh_" + dr_ThisDataRow[columnIndex: 1]] != null)
                {
                    lvi.ForeColor = Color.Red;
                    lvi.SubItems[index: lvchs[key: "clh_" + dr_ThisDataRow[columnIndex: 1]]
                                     .Index]
                        .Text = dr_ThisDataRow[columnIndex: 2]
                        .ToString();
                    //break;
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
            catch
            {
                // nothing. 
            }

            Application.DoEvents();
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
                FrmMainApp.DtFileDataCopyPool.Rows.Clear();
                List<string> listOfTagsToCopy = new()
                {
                    "Coordinates", "GPSLatitude", "GPSLatitudeRef", "GPSLongitude", "GPSLongitudeRef", "GPSSpeed", "GPSSpeedRef", "GPSAltitude", "GPSAltitudeRef", "Country", "CountryCode", "State", "City", "Sub_location", "DestCoordinates", "GPSDestLatitude", "GPSDestLatitudeRef", "GPSDestLongitude", "GPSDestLongitudeRef", "GPSImgDirection", "GPSImgDirectionRef"
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
    internal static void LwvPasteGeoData()
    {
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        // check there's antying in copy-pool
        if (FrmMainApp.DtFileDataCopyPool.Rows.Count > 0)
        {
            foreach (ListViewItem lvi in FrmMainAppInstance.lvw_FileList.SelectedItems)
            {
                if (File.Exists(path: Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text)))
                {
                    // paste all from copy-pool
                    foreach (DataRow dr in FrmMainApp.DtFileDataCopyPool.Rows)
                    {
                        string strToWrite;
                        if (dr[columnIndex: 1]
                                .ToString() ==
                            "-")
                        {
                            strToWrite = "";
                        }
                        else
                        {
                            strToWrite = dr[columnIndex: 1]
                                .ToString();
                        }

                        GenericUpdateAddToDataTable(
                            dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                            filePath: lvi.Text,
                            settingId: dr[columnIndex: 0]
                                .ToString(),
                            settingValue: strToWrite
                        );
                    }
                }
            }

            // push to lvw
            LwvUpdateRowFromDTWriteStage3ReadyToWrite();
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
        HTMLAddMarker = "";
        hs_MapMarkers.Clear();

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
                        hs_MapMarkers.Add(item: (strLat, strLng));
                    }
                }
                else if (s_ResetMapToZero)
                {
                    FrmMainAppInstance.tbx_lat.Text = "0";
                    FrmMainAppInstance.tbx_lng.Text = "0";
                    hs_MapMarkers.Add(item: ("0", "0"));
                }
                // leave as-is (most likely the last photo)
            }

            // don't do anything
            // don't try and create an preview img unless it's the last file
            if (FrmMainAppInstance.lvw_FileList.FocusedItem != null)
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
                            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorFileGoneMissing" + fileNameWithPath), caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a ListViewItem is a drive (e.g. C:\) or not
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
            catch (Exception ex)
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
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        FrmMainAppInstance.pbx_imagePreview.Image = null;
        // via https://stackoverflow.com/a/8701748/3968494
        Image img;

        FileInfo fi = new(fileName: fileNameWithPath);
        if (fi.Extension == ".jpg")
        {
            using (Bitmap bmpTemp = new(filename: fileNameWithPath))
            {
                img = new Bitmap(original: bmpTemp);
                FrmMainAppInstance.pbx_imagePreview.Image = img;
            }
        }
        else
        {
            string generatedFileName = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: FrmMainAppInstance.lvw_FileList.SelectedItems[index: 0]
                                                                                                     .Text +
                                                                                                 ".jpg");
            // don't run the thing again if file has already been generated
            if (!File.Exists(path: generatedFileName))
            {
                await ExifGetImagePreviews(fileName: fileNameWithPath);
            }

            //sometimes the file doesn't get created. (ie exiftool may fail to extract a preview.)
            if (File.Exists(path: generatedFileName))
            {
                using (Bitmap bmpTemp = new(filename: generatedFileName))
                {
                    try
                    {
                        img = new Bitmap(original: bmpTemp);
                        FrmMainAppInstance.pbx_imagePreview.Image = img;
                    }
                    catch
                    {
                        // nothing.
                    }
                }
            }
        }
    }

    #endregion
}