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
using System.Windows.Forms;
using GeoTagNinja.Helpers.UI;
using Themer = WinFormsDarkThemerNinja.Themer;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Exif;

internal static class DataPointInteractions
{
    /// <summary>
    ///     When "keep keywords in step with the location" is off, tells the user that removing the location data leaves
    ///     the keywords alone - the place names and <c>geo:</c> keywords some tools put there stay in the file. Says
    ///     nothing when the setting is on (it removes them) or when none of the files has any keywords to leave behind.
    /// </summary>
    /// <param name="directoryElements">The files the removal is about to run on.</param>
    internal static void NotifyIfKeywordsWillRemain(IEnumerable<DirectoryElement> directoryElements)
    {
        if (HelperVariables.UserSettingSyncGeoKeywords)
        {
            return;
        }

        bool anyKeywords = directoryElements.Any(predicate: element =>
            element.Type == DirectoryElement.ElementType.File &&
            new[] { ElementAttribute.IPTCKeywords, ElementAttribute.XMLSubjects }.Any(predicate: attribute =>
                {
                    string text = element.GetAttributeValueAsString(attribute: attribute, notFoundValue: string.Empty)
                                         .Trim();
                    return text.Length > 0 &&
                           text != NullEquivalents.StringGeneric;
                }));

        if (anyKeywords)
        {
            _ = Themer.ShowMessageBoxWithResult(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_Helper_InfoRemoveGeoDataKeywordsRemain",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                icon: MessageBoxIcon.Information,
                buttons: MessageBoxButtons.OK);
        }
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