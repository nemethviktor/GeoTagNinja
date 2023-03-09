using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using GeoTagNinja.View.ListView;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
    private const int exifOrientationID = 0x112; //274

    /// <summary>
    ///     Updates the data sitting in the main listview if there is anything outstanding in
    ///     dt_fileDataToWriteStage3ReadyToWrite for the file
    /// </summary>
    /// <param name="lvi"></param>
    internal static Task LwvUpdateRowFromDEStage3ReadyToWrite(ListViewItem lvi)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        string tmpCoordinates;
        if (frmMainAppInstance != null)
        {
            ListView lvw = frmMainAppInstance.lvw_FileList;
            ListView.ColumnHeaderCollection lvchs = frmMainAppInstance.ListViewColumnHeaders;

            int d = lvi.Index;
            string fileNameWithoutPath = lvi.Text;
            DirectoryElement dirElemFileToModify = FrmMainApp.DirectoryElements.FindElementByItemName(
                FileNameWithPath: Path.Combine(path1: FrmMainApp.FolderName,
                                               path2: fileNameWithoutPath));
            bool takenAlreadyShifted = false;
            bool createAlreadyShifted = false;

            foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
            {
                if (dirElemFileToModify != null &&
                    dirElemFileToModify.HasSpecificAttributeWithVersion(attribute: attribute,
                                                                        version: DirectoryElement.AttributeVersion
                                                                            .Stage3ReadyToWrite))
                {
                    try
                    {
                        lvw.BeginUpdate();
                        frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: fileNameWithoutPath, color: Color.Red);

                        // theoretically we'd want to update the columns for each tag but for example when removing all data
                        // this becomes tricky bcs we're also firing a "-gps*=" tag.
                        string settingId = "clh_" + GetAttributeName(attribute: attribute);
                        string settingVal = dirElemFileToModify.GetAttributeValueString(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);

                        if (lvchs[key: settingId] != null)
                        {
                            try
                            {
                                lvi.SubItems[index: lvchs[key: settingId]
                                                 .Index]
                                    .Text = settingVal;
                                //break;
                            }
                            catch
                            {
                                // nothing - basically this could happen if user navigates out of the folder
                            }
                        }
                        else if (settingId.EndsWith(value: "Shift"))
                        {
                            int shiftedDays = 0;
                            int shiftedHours = 0;
                            int shiftedMinutes = 0;
                            int shiftedSeconds = 0;

                            if (settingId.Substring(startIndex: 4)
                                    .StartsWith(value: "Taken") &&
                                !takenAlreadyShifted)
                            {
                                shiftedDays = (int)dirElemFileToModify.GetAttributeValue<int>(
                                    attribute: ElementAttribute.TakenDateDaysShift,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: 0);

                                shiftedHours = (int)dirElemFileToModify.GetAttributeValue<int>(
                                    attribute: ElementAttribute.TakenDateHoursShift,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: 0);

                                shiftedMinutes = (int)dirElemFileToModify.GetAttributeValue<int>(
                                    attribute: ElementAttribute.TakenDateMinutesShift,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: 0);

                                shiftedSeconds = (int)dirElemFileToModify.GetAttributeValue<int>(
                                    attribute: ElementAttribute.TakenDateSecondsShift,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: 0);

                                int totalShiftedSeconds = shiftedSeconds +
                                                          shiftedMinutes * 60 +
                                                          shiftedHours * 60 * 60 +
                                                          shiftedDays * 60 * 60 * 24;

                                DateTime originalTakenDateTime = DateTime.Parse(s: FrmMainApp.OriginalTakenDateDict[key: fileNameWithoutPath], provider: CultureInfo.CurrentUICulture);

                                DateTime modifiedTakenDateTime = originalTakenDateTime.AddSeconds(value: totalShiftedSeconds);
                                lvi.SubItems[index: lvchs[key: "clh_TakenDate"]
                                                 .Index]
                                    .Text = modifiedTakenDateTime.ToString(provider: CultureInfo.CurrentUICulture);
                                takenAlreadyShifted = true;
                            }
                            else if (settingId.Substring(startIndex: 4)
                                         .StartsWith(value: "Create") &&
                                     !createAlreadyShifted)
                            {
                                shiftedDays = (int)dirElemFileToModify.GetAttributeValue<int>(
                                    attribute: ElementAttribute.CreateDateDaysShift,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: 0);
                                shiftedHours = (int)dirElemFileToModify.GetAttributeValue<int>(
                                    attribute: ElementAttribute.CreateDateHoursShift,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: 0);
                                shiftedMinutes = (int)dirElemFileToModify.GetAttributeValue<int>(
                                    attribute: ElementAttribute.CreateDateMinutesShift,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: 0);

                                shiftedSeconds = (int)dirElemFileToModify.GetAttributeValue<int>(
                                    attribute: ElementAttribute.CreateDateSecondsShift,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: 0);

                                int totalShiftedSeconds = shiftedSeconds +
                                                          shiftedMinutes * 60 +
                                                          shiftedHours * 60 * 60 +
                                                          shiftedDays * 60 * 60 * 24;

                                DateTime originalCreateDateTime = DateTime.Parse(s: FrmMainApp.OriginalCreateDateDict[key: fileNameWithoutPath], provider: CultureInfo.CurrentUICulture);

                                DateTime modifiedCreateDateTime = originalCreateDateTime.AddSeconds(value: totalShiftedSeconds);
                                lvi.SubItems[index: lvchs[key: "clh_CreateDate"]
                                                 .Index]
                                    .Text = modifiedCreateDateTime.ToString(provider: CultureInfo.CurrentUICulture);
                                createAlreadyShifted = true;
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

            // don't copy folders....
            if (File.Exists(path: Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text)))
            {
                // The reason why we're using a CopyPoolDict here rather than just read straight out of the DE is because if the user changes folder
                // ... then the DE-data would be cleared and thus cross-folder-paste wouldn't be possible
                FrmMainApp.CopyPoolDict.Clear();

                List<ElementAttribute> listOfTagsToCopy = new()
                {
                    ElementAttribute.Coordinates,
                    ElementAttribute.GPSLatitude,
                    ElementAttribute.GPSLatitudeRef,
                    ElementAttribute.GPSLongitude,
                    ElementAttribute.GPSLongitudeRef,
                    ElementAttribute.GPSSpeed,
                    ElementAttribute.GPSSpeedRef,
                    ElementAttribute.GPSAltitude,
                    ElementAttribute.GPSAltitudeRef,
                    ElementAttribute.Country,
                    ElementAttribute.CountryCode,
                    ElementAttribute.State,
                    ElementAttribute.City,
                    ElementAttribute.Sub_location,
                    ElementAttribute.DestCoordinates,
                    ElementAttribute.GPSDestLatitude,
                    ElementAttribute.GPSDestLatitudeRef,
                    ElementAttribute.GPSDestLongitude,
                    ElementAttribute.GPSDestLongitudeRef,
                    ElementAttribute.GPSImgDirection,
                    ElementAttribute.GPSImgDirectionRef,
                    ElementAttribute.TakenDate,
                    ElementAttribute.CreateDate,
                    ElementAttribute.OffsetTime,
                    ElementAttribute.TakenDateSecondsShift,
                    ElementAttribute.TakenDateMinutesShift,
                    ElementAttribute.TakenDateHoursShift,
                    ElementAttribute.TakenDateDaysShift,
                    ElementAttribute.CreateDateSecondsShift,
                    ElementAttribute.CreateDateMinutesShift,
                    ElementAttribute.CreateDateHoursShift,
                    ElementAttribute.CreateDateDaysShift
                };

                string fileNameWithoutPath = lvi.Text;
                DirectoryElement dirElemFileToCopyFrom = FrmMainApp.DirectoryElements.FindElementByItemName(FileNameWithPath: Path.Combine(path1: FrmMainApp.FolderName, path2: fileNameWithoutPath));
                foreach (ElementAttribute attribute in listOfTagsToCopy)
                {
                    // this would sit in Stage3ReadyToWrite if exists
                    if (dirElemFileToCopyFrom.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite))
                    {
                        FrmMainApp.CopyPoolDict.Add(key: attribute,
                                                    value: dirElemFileToCopyFrom.GetAttributeValueString(
                                                        attribute: attribute,
                                                        version: DirectoryElement.AttributeVersion
                                                            .Stage3ReadyToWrite));
                    }
                    else if (dirElemFileToCopyFrom.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Original))
                    {
                        FrmMainApp.CopyPoolDict.Add(key: attribute,
                                                    value: dirElemFileToCopyFrom.GetAttributeValueString(
                                                        attribute: attribute,
                                                        version: DirectoryElement.AttributeVersion.Original));
                    }
                }
            }
        }
        else
        {
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningTooManyFilesSelected"),
                            caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    ///     This drives the logic for "pasting" (as in copy-paste) the geodata from one file to others.
    ///     See further comments inside
    /// </summary>
    internal static void LwvPasteGeoData()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        // check there's anything in copy-pool
        if (FrmMainApp.CopyPoolDict.Count > 0 && frmMainAppInstance != null)
        {
            FrmPasteWhat frmPasteWhat = new(initiator: frmMainAppInstance.Name);
            frmPasteWhat.ShowDialog();
        }
        else
        {
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNothingToPaste"),
                            caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
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
            if (File.Exists(path: de.FileNameWithPath) && lvw_FileListItem.SubItems.Count > 1)
            {
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
                        frmMainAppInstance.nud_lat.Text = strLat;
                        frmMainAppInstance.nud_lng.Text = strLng;

                        frmMainAppInstance.nud_lat.Value = Convert.ToDecimal(value: strLat, provider: CultureInfo.InvariantCulture);
                        frmMainAppInstance.nud_lng.Value = Convert.ToDecimal(value: strLng, provider: CultureInfo.InvariantCulture);
                        HsMapMarkers.Add(item: (strLat, strLng));
                    }
                }
                else if (SResetMapToZero)
                {
                    frmMainAppInstance.nud_lat.Text = "0";
                    frmMainAppInstance.nud_lng.Text = "0";

                    frmMainAppInstance.nud_lat.Value = 0;
                    frmMainAppInstance.nud_lng.Value = 0;
                    HsMapMarkers.Add(item: ("0", "0"));
                }
                // leave as-is (most likely the last photo)
            }
        }
    }

    /// <summary>
    ///     This updates the lbl_ParseProgress with count of items with geodata.
    /// </summary>
    internal static void LvwCountItemsWithGeoData()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        if (frmMainAppInstance != null)
        {
            FrmMainApp.HandlerUpdateLabelText(
                label: frmMainAppInstance.lbl_ParseProgress,
                text: "Ready. Files: Total: " +
                      frmMainAppInstance.lvw_FileList.FileCount +
                      " Geodata: " +
                      frmMainAppInstance.lvw_FileList.CountItemsWithData(column: FileListView.FileListColumns.COORDINATES));
        }
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

    /// <summary>
    ///     Corrects the half-coordinate to be a valid one (in case over/under 180, which can happen if the map is
    ///     misbehaving.)
    /// </summary>
    /// <param name="coordHalfPair">Lat or Long</param>
    /// <returns>Rounded to 6, corrected Lat or Long</returns>
    internal static double GenericCorrectInvalidCoordinate(double coordHalfPair)
    {
        if (coordHalfPair < -180)
        {
            coordHalfPair = 180 - Math.Abs(value: coordHalfPair) % 180;
        }
        else if (coordHalfPair > 180)
        {
            coordHalfPair = Math.Abs(value: coordHalfPair) % 180;
        }

        coordHalfPair = Math.Round(value: coordHalfPair, digits: 6);
        return coordHalfPair;
    }
}