using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using GeoTagNinja.View.ListView;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers;

internal static class HelperExifDataPointInteractions
{
    /// <summary>
    ///     Wrangles the actual coordinate out of a point. (e.g. 4.54 East to -4.54)
    /// </summary>
    /// <param name="point">This is a raw coordinate. Could contain numbers or things like "East" on top of numbers</param>
    /// <returns>Double - an actual coordinate</returns>
    public static double AdjustLatLongNegative(string point)
    {
        string pointOrig = point.Replace(oldValue: " ", newValue: "")
                                .Replace(oldChar: ',', newChar: '.');
        // WGS84 DM --> logic here is, before I have to spend hours digging this crap again...
        // degree stays as-is, the totality of the rest gets divided by 60.
        // so 41,53.23922526N becomes 41 + (53.53.23922526)/60) = 41.88732
        double pointVal = 0.0;
        if (pointOrig.Count(predicate: f => f == '.') == 2)
        {
            bool degreeParse = int.TryParse(s: pointOrig.Split('.')[0],
                                            style: NumberStyles.Any,
                                            provider: CultureInfo.InvariantCulture,
                                            result: out int degree);
            bool minuteParse = double.TryParse(s: Regex.Replace(
                                                   input: pointOrig.Split('.')[1] +
                                                          "." +
                                                          pointOrig.Split('.')[2],
                                                   pattern: "[SWNE\"-]",
                                                   replacement: ""),
                                               style: NumberStyles.Any,
                                               provider: CultureInfo.InvariantCulture,
                                               result: out double minute);
            minute = minute / 60;
            pointVal = degree + minute;
        }
        else
        {
            pointVal =
                double.Parse(
                    s: Regex.Replace(input: pointOrig, pattern: "[SWNE\"-]",
                                     replacement: ""), style: NumberStyles.Any,
                    provider: CultureInfo.InvariantCulture);
        }

        pointVal = Math.Round(value: pointVal, digits: 6);
        int multiplier = point.Contains(value: "S") ||
                         point.Contains(value: "W") ||
                         point.StartsWith(value: "-")
            ? -1
            : 1; //handle south and west

        return pointVal * multiplier;
    }

    /// <summary>
    ///     Queues up a command to remove existing geo-data. Depending on the sender this can be for one or many
    ///     files.
    /// </summary>
    /// <param name="senderName">At this point this can either be the main listview or the one from Edit (file) data</param>
    internal static async Task ExifRemoveLocationData(string senderName)
    {
        // GeoDataAttributes is a readonly and I don't want to modify it for the rest of the code.
        List<ElementAttribute> geoDataAttributes = Enum
                                                  .GetValues(
                                                       enumType: typeof(ElementAttribute))
                                                  .Cast<ElementAttribute>()
                                                  .Where(predicate: attr =>
                                                             GetElementAttributesIsGeoData(
                                                                 attributeToFind: attr))
                                                  .ToList();

        geoDataAttributes.Add(item: ElementAttribute.RemoveAllGPS); //"gps*"

        if (HelperDataApplicationSettings.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "ckb_RemoveGeoDataRemovesTimeOffset") ==
            "true")
        {
            geoDataAttributes.Add(item: ElementAttribute.OffsetTime);
        }

        if (senderName == "FrmEditFileData")
        {
            FrmEditFileData frmEditFileDataInstance =
                (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
            if (frmEditFileDataInstance != null)
            {
                ListViewItem lvi =
                    frmEditFileDataInstance.lvw_FileListEditImages
                                           .SelectedItems[index: 0];

                DirectoryElement dirElemFileToModify =
                    lvi.Tag as DirectoryElement;

                HelperNonStatic helperNonstatic = new();
                IEnumerable<Control> cGbx_GPSData =
                    helperNonstatic.GetAllControls(
                        control: frmEditFileDataInstance.gbx_GPSData);
                foreach (Control cItem_cGbx_GPSData in cGbx_GPSData)
                {
                    if (cItem_cGbx_GPSData is NumericUpDown nud)
                    {
                        nud.Value = 0;
                        nud.Text = "";
                    }

                    // no textboxes here but just in case
                    else if (cItem_cGbx_GPSData is TextBox txt)
                    {
                        txt.Text = "";
                    }
                }

                IEnumerable<Control> cGbx_LocationData =
                    helperNonstatic.GetAllControls(
                        control: frmEditFileDataInstance.gbx_LocationData);
                foreach (Control cItem_cGbx_LocationData in cGbx_LocationData)
                {
                    // no nuds here but just in case
                    if (cItem_cGbx_LocationData is NumericUpDown nud)
                    {
                        nud.Value = 0;
                        nud.Text = "";
                    }

                    else if (cItem_cGbx_LocationData is TextBox txt)
                    {
                        txt.Text = "";
                    }
                    else if (cItem_cGbx_LocationData is ComboBox cbx)
                    {
                        cbx.Text = "";
                    }
                }

                if (dirElemFileToModify != null)
                {
                    foreach (ElementAttribute toponomyDetail in geoDataAttributes)
                    {
                        dirElemFileToModify.SetAttributeValueAnyType(
                            attribute: toponomyDetail,
                            value: "",
                            version: DirectoryElement.AttributeVersion
                                                     .Stage1EditFormIntraTabTransferQueue,
                            isMarkedForDeletion: true);
                    }

                    dirElemFileToModify.SetAttributeValueAnyType(
                        attribute: ElementAttribute.RemoveAllGPS,
                        value: "",
                        version: DirectoryElement.AttributeVersion
                                                 .Stage1EditFormIntraTabTransferQueue,
                        isMarkedForDeletion: true);
                }
            }
        }
        else if (senderName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance =
                (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                ListView lvw = frmMainAppInstance.lvw_FileList;
                if (lvw.SelectedItems.Count > 0)
                {
                    HelperGenericFileLocking.FileListBeingUpdated = true;
                    foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList
                                .SelectedItems)
                    {
                        DirectoryElement dirElemFileToModify =
                            lvi.Tag as DirectoryElement;
                        // don't do folders...
                        if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                        {
                            string fileNameWithPath =
                                dirElemFileToModify.FileNameWithPath;
                            string fileNameWithoutPath =
                                dirElemFileToModify.ItemNameWithoutPath;

                            // check it's not in the read-queue.
                            while (HelperGenericFileLocking.GenericLockCheckLockFile(
                                       fileNameWithoutPath: fileNameWithoutPath))
                            {
                                await Task.Delay(millisecondsDelay: 10);
                            }

                            // then put a blocker on
                            HelperGenericFileLocking.GenericLockLockFile(
                                fileNameWithoutPath: fileNameWithoutPath);
                            foreach (ElementAttribute toponomyDetail in geoDataAttributes)
                            {
                                dirElemFileToModify.SetAttributeValueAnyType(
                                    attribute: toponomyDetail,
                                    value: "",
                                    version: DirectoryElement.AttributeVersion
                                       .Stage3ReadyToWrite,
                                    isMarkedForDeletion: true);
                            }

                            dirElemFileToModify.SetAttributeValueAnyType(
                                attribute: ElementAttribute.RemoveAllGPS,
                                value: "",
                                version: DirectoryElement.AttributeVersion
                                                         .Stage3ReadyToWrite,
                                isMarkedForDeletion: true);

                            // then remove lock

                            await FileListViewReadWrite
                               .ListViewUpdateRowFromDEStage3ReadyToWrite(lvi: lvi);
                            HelperGenericFileLocking.GenericLockUnLockFile(
                                fileNameWithoutPath: fileNameWithoutPath);
                            // no need to remove the xmp here because it hasn't been added in the first place.
                        }

                        //lvw.EndUpdate();
                    }

                    HelperGenericFileLocking.FileListBeingUpdated = false;
                    FrmMainApp.RemoveGeoDataIsRunning = false;
                }
            }
        }
    }


    internal static void ExifRotate(this Image img)
    {
        // via https://stackoverflow.com/a/48347653/3968494
        if (!img.PropertyIdList.Contains(value: HelperVariables.exifOrientationID))
        {
            return;
        }

        PropertyItem prop =
            img.GetPropertyItem(propid: HelperVariables.exifOrientationID);
        int val = BitConverter.ToUInt16(value: prop.Value, startIndex: 0);
        RotateFlipType rot = RotateFlipType.RotateNoneFlipNone;

        if (val == 3 ||
            val == 4)
        {
            rot = RotateFlipType.Rotate180FlipNone;
        }
        else if (val == 5 ||
                 val == 6)
        {
            rot = RotateFlipType.Rotate90FlipNone;
        }
        else if (val == 7 ||
                 val == 8)
        {
            rot = RotateFlipType.Rotate270FlipNone;
        }

        if (val == 2 ||
            val == 4 ||
            val == 5 ||
            val == 7)
        {
            rot |= RotateFlipType.RotateNoneFlipX;
        }

        if (rot != RotateFlipType.RotateNoneFlipNone)
        {
            img.RotateFlip(rotateFlipType: rot);
            img.RemovePropertyItem(propid: HelperVariables.exifOrientationID);
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