using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Exif;

internal static class DataPointInteractions
{
    /// <summary>
    ///     Wrangles the actual coordinate out of a point. (e.g. 4.54 East to -4.54)
    /// </summary>
    /// <param name="point">This is a raw coordinate. Could contain numbers or things like "East" on top of numbers</param>
    /// <returns>Double - an actual coordinate, or zero if nothing numeric could be read out of the text.</returns>
    public static double AdjustLatLongNegative(string point)
    {
        return TryAdjustLatLongNegative(point: point, coordinate: out double coordinate)
            ? coordinate
            : 0.0;
    }

    /// <summary>
    ///     As <see cref="AdjustLatLongNegative" />, but able to say that the text held no coordinate at all.
    /// </summary>
    /// <remarks>
    ///     The distinction matters to the read pipeline: a tag that cannot be understood must leave the attribute
    ///     unset, whereas zero is a perfectly valid coordinate (and, off the coast of Ghana, a real place).
    /// </remarks>
    /// <param name="point">This is a raw coordinate. Could contain numbers or things like "East" on top of numbers</param>
    /// <param name="coordinate">The coordinate read out of <paramref name="point" />, or zero on failure.</param>
    /// <returns><see langword="true" /> when a number could be read; otherwise <see langword="false" />.</returns>
    public static bool TryAdjustLatLongNegative(string point,
                                                out double coordinate)
    {
        coordinate = 0.0;
        if (string.IsNullOrWhiteSpace(value: point))
        {
            return false;
        }

        string pointOrig = point.Replace(oldValue: " ", newValue: "")
                                .Replace(oldChar: ',', newChar: '.');
        // WGS84 DM --> logic here is, before I have to spend hours digging this crap again...
        // degree stays as-is, the totality of the rest gets divided by 60.
        // so 41,53.23922526N becomes 41 + (53.23922526/60) = 41.88732
        double pointVal;
        bool parsed;
        if (pointOrig.Count(predicate: f => f == '.') == 2)
        {
            bool degreeParse = int.TryParse(s: pointOrig.Split('.')[0],
                style: NumberStyles.Any,
                provider: CultureInfo.InvariantCulture,
                result: out int degree);
            bool minuteParse = double.TryParse(s: Regex.Replace(
                    input: $"{pointOrig.Split('.')[1]}.{pointOrig.Split('.')[2]}",
                    pattern: "[SWNE\"-]",
                    replacement: ""),
                style: NumberStyles.Any,
                provider: CultureInfo.InvariantCulture,
                result: out double minute);
            minute /= 60;
            pointVal = degree + minute;
            parsed = degreeParse && minuteParse;
        }
        else
        {
            parsed =
                double.TryParse(
                    s: Regex.Replace(input: pointOrig, pattern: "[SWNE\"-]",
                        replacement: ""), style: NumberStyles.Any,
                    provider: CultureInfo.InvariantCulture, result: out pointVal);
        }

        if (!parsed)
        {
            return false;
        }

        pointVal = Math.Round(value: pointVal, digits: 6);
        int multiplier = point.Contains(value: "S") ||
                         point.Contains(value: "W") ||
                         point.StartsWith(value: "-")
            ? -1
            : 1; //handle south and west

        coordinate = pointVal * multiplier;
        return true;
    }

    /// <summary>
    ///     Queues up a command to remove existing geo-data. Depending on the sender this can be for one or many
    ///     files.
    /// </summary>
    /// <param name="senderName">At this point this can either be the main listview or the one from Edit (file) data</param>
    internal static async Task ExifRemoveLocationData(
        DirectoryElement dirElemFileToModify,
        DirectoryElement.AttributeVersion attributeVersion)
    {
        if (dirElemFileToModify.Type != DirectoryElement.ElementType.File)
        {
            return;
        }

        // GeoDataAttributes is a readonly and I don't want to modify it for the rest of the code.
        List<ElementAttribute> geoDataAttributes = Enum
                                                  .GetValues(
                                                       enumType: typeof(ElementAttribute))
                                                  .Cast<ElementAttribute>()
                                                  .Where(predicate: GetElementAttributesIsGeoData)
                                                  .ToList();

        geoDataAttributes.Add(item: ElementAttribute.RemoveAllGPS); //"gps*"

        if (ApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings,
                settingTabPage: "tpg_Application",
                settingId: "ckb_RemoveGeoDataRemovesTimeOffset") ==
            "true")
        {
            geoDataAttributes.Add(item: ElementAttribute.OffsetTime);
        }

        //if (senderName == "FrmEditFileData")
        //{
        //    FrmEditFileData frmEditFileDataInstance =
        //        (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
        //    if (frmEditFileDataInstance != null)
        //    {
        //        ListViewItem lvi =
        //            frmEditFileDataInstance.lvw_FileListEditImages
        //                                   .SelectedItems[index: 0];

        //        Helper        //        IEnumerable<Control> cGbx_GPSData =
        //            ControlTraversal.GetAllControls(
        //                control: frmEditFileDataInstance.gbx_GPSData);
        //        foreach (Control cItem_cGbx_GPSData in cGbx_GPSData)
        //        {
        //            if (cItem_cGbx_GPSData is NumericUpDown nud)
        //            {
        //                nud.Value = 0;
        //                nud.Text = "";
        //            }

        //            // no textboxes here but just in case
        //            else if (cItem_cGbx_GPSData is TextBox txt)
        //            {
        //                txt.Text = "";
        //            }
        //        }

        //        IEnumerable<Control> cGbx_LocationData =
        //            ControlTraversal.GetAllControls(
        //                control: frmEditFileDataInstance.gbx_LocationData);
        //        foreach (Control cItem_cGbx_LocationData in cGbx_LocationData)
        //        {
        //            // no nuds here but just in case
        //            if (cItem_cGbx_LocationData is NumericUpDown nud)
        //            {
        //                nud.Value = 0;
        //                nud.Text = "";
        //            }

        //            else if (cItem_cGbx_LocationData is TextBox txt)
        //            {
        //                txt.Text = "";
        //            }
        //            else if (cItem_cGbx_LocationData is ComboBox cbx)
        //            {
        //                cbx.Text = "";
        //            }
        //        }

        foreach (ElementAttribute toponomyDetail in geoDataAttributes)
        {
            dirElemFileToModify.SetAttributeValueAnyType(
                attribute: toponomyDetail,
                value: "",
                version: attributeVersion,
                isMarkedForDeletion: true);
        }

        dirElemFileToModify.SetAttributeValueAnyType(
            attribute: ElementAttribute.RemoveAllGPS,
            value: "",
            version: attributeVersion,
            isMarkedForDeletion: true);
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
            coordHalfPair = 180 - (Math.Abs(value: coordHalfPair) % 180);
        }
        else if (coordHalfPair > 180)
        {
            coordHalfPair = Math.Abs(value: coordHalfPair) % 180;
        }

        coordHalfPair = Math.Round(value: coordHalfPair, digits: 6);
        return coordHalfPair;
    }

    /// <summary>
    /// Corrects the orientation of the specified image based on its EXIF orientation data.
    /// </summary>
    /// <remarks>If the image does not contain EXIF orientation data, no changes are made. The method modifies
    /// the image in place and removes the EXIF orientation property after rotation.</remarks>
    /// <param name="img">The image to be rotated. This parameter must not be null and should contain EXIF orientation data for the method
    /// to perform any rotation.</param>
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

        if (val is 3 or
            4)
        {
            rot = RotateFlipType.Rotate180FlipNone;
        }
        else if (val is 5 or
                 6)
        {
            rot = RotateFlipType.Rotate90FlipNone;
        }
        else if (val is 7 or
                 8)
        {
            rot = RotateFlipType.Rotate270FlipNone;
        }

        if (val is 2 or
            4 or
            5 or
            7)
        {
            rot |= RotateFlipType.RotateNoneFlipX;
        }

        if (rot != RotateFlipType.RotateNoneFlipNone)
        {
            img.RotateFlip(rotateFlipType: rot);
            img.RemovePropertyItem(propid: HelperVariables.exifOrientationID);
        }
    }
}