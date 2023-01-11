using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using GeoTagNinja.View.ListView;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Forms.Application;
using Image = System.Drawing.Image;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
    private const int exifOrientationID = 0x112; //274

    /// <summary>
    ///     Updates the data sitting in the main listview if there is anything outstanding in
    ///     dt_fileDataToWriteStage3ReadyToWrite for the file
    /// </summary>
    /// <param name="lvi"></param>
    internal static Task LwvUpdateRowFromDTWriteStage3ReadyToWrite(ListViewItem lvi)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        string tmpCoordinates;
        if (frmMainAppInstance != null)
        {
            ListView lvw = frmMainAppInstance.lvw_FileList;
            ListView.ColumnHeaderCollection lvchs = frmMainAppInstance.ListViewColumnHeaders;

            int d = lvi.Index;
            string fileNameWithoutPath = lvi.Text;

            DataRow[] drRelevantRows = FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Select(filterExpression: "fileNameWithoutPath = '" + fileNameWithoutPath + "'");
            if (drRelevantRows.Length > 0)
            {
                try
                {
                    lvw.BeginUpdate();
                    frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: fileNameWithoutPath, color: Color.Red);

                    foreach (DataRow drTagData in drRelevantRows)
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

        return Task.CompletedTask;
    }

    /// <summary>
    ///     This drives the logic for "copying" (as in copy-paste) the geodata from one file to others.
    /// </summary>
    internal static void LwvCopyGeoData()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (frmMainAppInstance.lvw_FileList.SelectedItems.Count == 1)
        {
            ListViewItem lvi = frmMainAppInstance.lvw_FileList.SelectedItems[index: 0];
            if (File.Exists(path: Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text)))
            {
                FrmMainApp.FileDateCopySourceFileNameWithPath = Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text);
                FrmMainApp.DtFileDataCopyPool.Rows.Clear();
                List<string> listOfTagsToCopyExclShifts = new()
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

                List<string> listOfTagsToCopyTimeShifts = new()
                {
                    "TakenDateSecondsShift",
                    "TakenDateMinutesShift",
                    "TakenDateHoursShift ",
                    "TakenDateDaysShift",
                    "CreateDateSecondsShift",
                    "CreateDateMinutesShift",
                    "CreateDateHoursShift",
                    "CreateDateDaysShift"
                };

                string fileNameWithoutPath = lvi.Text;
                foreach (ColumnHeader clh in frmMainAppInstance.lvw_FileList.Columns)
                {
                    if (listOfTagsToCopyExclShifts.IndexOf(item: clh.Name.Substring(startIndex: 4)) >= 0)
                    {
                        DataRow drFileDataRow = FrmMainApp.DtFileDataCopyPool.NewRow();
                        drFileDataRow[columnName: "fileNameWithoutPath"] = fileNameWithoutPath;
                        drFileDataRow[columnName: "settingId"] = clh.Name.Substring(startIndex: 4);
                        drFileDataRow[columnName: "settingValue"] = lvi.SubItems[index: clh.Index]
                            .Text;
                        FrmMainApp.DtFileDataCopyPool.Rows.Add(row: drFileDataRow);
                    }
                }

                // when COPYING we use the main grid, therefore timeshifts can only possibly live in DtFileDataToWriteStage3ReadyToWrite
                foreach (string settingId in listOfTagsToCopyTimeShifts)
                {
                    DataRow[] dtDateShifted = FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Select(filterExpression: "fileNameWithoutPath = '" + fileNameWithoutPath + "' AND settingId = '" + settingId + "'");
                    if (dtDateShifted.Length > 0)
                    {
                        DataRow drFileDataRow = FrmMainApp.DtFileDataCopyPool.NewRow();
                        drFileDataRow[columnName: "fileNameWithoutPath"] = fileNameWithoutPath;
                        drFileDataRow[columnName: "settingId"] = settingId;
                        drFileDataRow[columnName: "settingValue"] = dtDateShifted[0][columnName: "settingValue"]
                            .ToString();
                        FrmMainApp.DtFileDataCopyPool.Rows.Add(row: drFileDataRow);
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
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        HtmlAddMarker = "";
        HsMapMarkers.Clear();

        foreach (ListViewItem lvw_FileListItem in frmMainAppInstance.lvw_FileList.SelectedItems)
        {
            DirectoryElement de = lvw_FileListItem.Tag as DirectoryElement;

            // make sure file still exists. just in case someone deleted it elsewhere
            if (File.Exists(path: de.FullPathAndName) && lvw_FileListItem.SubItems.Count > 1)
            {
                int coordCol = frmMainAppInstance.lvw_FileList.GetColumnIndex(FileListView.FileListColumns.COORDINATES);
                int coordCol = frmMainAppInstance.lvw_FileList.GetColumnIndex(column: FileListView.FileListColumns.COORDINATES);
                string firstSelectedItem = lvw_FileListItem.SubItems[index: coordCol]
                    .Text;
                if (firstSelectedItem.Replace(oldValue: FileListView.UNKNOWN_VALUE_FILE, newValue: "") != "")
                {
                    string strLat;
                    string strLng;
                    try
                    {
                        strLat = firstSelectedItem.Split(';')[0]
                            .Replace(oldChar: ',', newChar: '.');
                        strLng = firstSelectedItem.Split(';')[1]
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
                        frmMainAppInstance.tbx_lat.Text = strLat;
                        frmMainAppInstance.tbx_lng.Text = strLng;
                        HsMapMarkers.Add(item: (strLat, strLng));
                    }
                }
                else if (SResetMapToZero)
                {
                    frmMainAppInstance.tbx_lat.Text = "0";
                    frmMainAppInstance.tbx_lng.Text = "0";
                    HsMapMarkers.Add(item: ("0", "0"));
                }
                // leave as-is (most likely the last photo)
            }

            // don't try and create an preview img unless it's the last file
            if (frmMainAppInstance.lvw_FileList.FocusedItem != null && lvw_FileListItem.Text != null)
            {
                if (frmMainAppInstance.lvw_FileList.FocusedItem.Text == lvw_FileListItem.Text ||
                    frmMainAppInstance.lvw_FileList.SelectedItems[index: 0]
                        .Text ==
                    lvw_FileListItem.Text)
                {
                    if (File.Exists(path: de.FullPathAndName))
                    {
                        await HelperStatic.GenericCreateImagePreview(fileNameWithPath: de.FullPathAndName,
                                                                     initiator: "FrmMainApp");
                    }
                    else if (Directory.Exists(path: de.FullPathAndName))
                    {
                        // nothing.
                    }
                    else // check it's a drive? --> if so, don't do anything, otherwise warn the item is gone
                    {
                        if (de.Type != DirectoryElement.ElementType.Drive)
                        {
                            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorFileGoneMissing") + de.FullPathAndName, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                        }
                    }
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

        if (frmMainAppInstance != null) FrmMainApp.HandlerUpdateLabelText(
                label: frmMainAppInstance.lbl_ParseProgress,
                text: "Ready. Files: Total: " +
                      frmMainAppInstance.lvw_FileList.FileCount +
                      " Geodata: " +
                      frmMainAppInstance.lvw_FileList.CountItemsWithData(column: FileListView.FileListColumns.COORDINATES));
    }


    internal static void ExifRotate(this Image img)
    {
        // via https://stackoverflow.com/a/48347653/3968494
        if (!img.PropertyIdList.Contains(value: exifOrientationID))
        {
            return;
        }

        PropertyItem prop = img.GetPropertyItem(propid: exifOrientationID);
        int val = BitConverter.ToUInt16(value: prop.Value, startIndex: 0);
        RotateFlipType rot = RotateFlipType.RotateNoneFlipNone;

        if (val == 3 || val == 4)
        {
            rot = RotateFlipType.Rotate180FlipNone;
        }
        else if (val == 5 || val == 6)
        {
            rot = RotateFlipType.Rotate90FlipNone;
        }
        else if (val == 7 || val == 8)
        {
            rot = RotateFlipType.Rotate270FlipNone;
        }

        if (val == 2 || val == 4 || val == 5 || val == 7)
        {
            rot |= RotateFlipType.RotateNoneFlipX;
        }

        if (rot != RotateFlipType.RotateNoneFlipNone)
        {
            img.RotateFlip(rotateFlipType: rot);
            img.RemovePropertyItem(propid: exifOrientationID);
        }
    }
}