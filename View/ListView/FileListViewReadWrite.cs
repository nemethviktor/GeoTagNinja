using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GeoTagNinja.Model.SourcesAndAttributes;
using static GeoTagNinja.View.ListView.FileListView;

namespace GeoTagNinja.View.ListView;

/// <summary>
/// An enumerable list of two items "Taken" and "Created", used for TakenDate and CreatedDate
/// </summary>
internal enum TakenOrCreated
{
    Taken,
    Created
}

internal static class FileListViewReadWrite
{
    /// <summary>
    ///     Updates the data sitting in the main listview if there is anything outstanding in
    ///     dt_fileDataToWriteStage3ReadyToWrite for the file
    /// </summary>
    /// <param name="dirElemFileToModify">The DE to modify</param>
    internal static Task ListViewUpdateRowFromDEStage3ReadyToWrite(DirectoryElement dirElemFileToModify)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        if (frmMainAppInstance != null)
        {
            System.Windows.Forms.ListView lvw = frmMainAppInstance.lvw_FileList;
            ListViewItem lvi = frmMainAppInstance.lvw_FileList.FindItemByDirectoryElement(directoryElement: dirElemFileToModify);
            int lviIndex = lvi.Index;

            System.Windows.Forms.ListView.ColumnHeaderCollection lvchs =
                frmMainAppInstance.ListViewColumnHeaders;

            if (dirElemFileToModify.HasDirtyAttributes())
            {
                frmMainAppInstance.lvw_FileList.UpdateDirectoryElementItemColour(directoryElement: dirElemFileToModify,
                    color: Color.Red);
                bool takenAlreadyShifted = false;
                bool createAlreadyShifted = false;
                lvw.BeginUpdate();
                foreach (ElementAttribute attribute in
                    (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                {
                    if (dirElemFileToModify.HasSpecificAttributeWithVersion(attribute: attribute,
                                                                            version: DirectoryElement.AttributeVersion
                                                                                                     .Stage3ReadyToWrite))
                    {
                        try
                        {
                            // Theoretically we want to update the columns for each tag but for example when removing all data
                            // this becomes tricky bcs we're also firing a "-gps*=" tag.
                            string columnHeaderName = GetElementAttributesColumnHeader(attribute);
                            string itemValue = dirElemFileToModify.GetAttributeValueAsString(
                                attribute: attribute,
                                version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                nowSavingExif: false);

                            // ElementAttribute has a column assigned (ie it's not Shift or some such) 
                            if (lvchs[key: columnHeaderName] != null)
                            {
                                try
                                {
                                    lvi.SubItems[index: lvchs[key: columnHeaderName].Index]
                                       .Text = itemValue;
                                    //break;
                                }
                                catch
                                {
                                    // nothing - basically this could happen if user navigates out of the folder
                                }
                            }
                            else if (columnHeaderName.EndsWith(value: "Shift"))
                            {
                                if (columnHeaderName.Substring(HelperVariables.COL_NAME_PREFIX.Length)
                                             .StartsWith(value: "Taken") &&
                                    !takenAlreadyShifted)
                                {
                                    int totalShiftedSeconds = GetTotalShiftedSeconds(
                                        dirElemFileToModify: dirElemFileToModify,
                                        takenOrCreated: TakenOrCreated.Taken);

                                    DateTime originalTakenDateTime =
                                        (DateTime)dirElemFileToModify
                                           .GetAttributeValue<DateTime>(
                                                attribute: ElementAttribute.TakenDate,
                                                version: DirectoryElement.AttributeVersion
                                                                         .Stage3ReadyToWrite);

                                    DateTime modifiedTakenDateTime = originalTakenDateTime.AddSeconds(value: totalShiftedSeconds);

                                    // column might be hidden.
                                    try
                                    {
                                        lvi.SubItems[
                                                index: lvchs[
                                                        key: HelperVariables.COL_NAME_PREFIX +
                                                        FileListColumns.TAKEN_DATE]
                                                   .Index]
                                           .Text = modifiedTakenDateTime.ToString(
                                            provider: CultureInfo.CurrentUICulture);
                                    }
                                    catch
                                    {
                                        // nothing
                                    }

                                    takenAlreadyShifted = true;
                                }
                                else if (columnHeaderName.Substring(HelperVariables.COL_NAME_PREFIX.Length)
                                                  .StartsWith(value: "Create") &&
                                         !createAlreadyShifted)
                                {
                                    int totalShiftedSeconds = GetTotalShiftedSeconds(
                                        dirElemFileToModify: dirElemFileToModify,
                                        takenOrCreated: TakenOrCreated.Created);

                                    DateTime originalCreateDateTime =
                                        (DateTime)dirElemFileToModify
                                           .GetAttributeValue<DateTime>(
                                                attribute: ElementAttribute.CreateDate,
                                                version: DirectoryElement.AttributeVersion
                                                                         .Stage3ReadyToWrite);

                                    DateTime modifiedCreateDateTime = originalCreateDateTime.AddSeconds(value: totalShiftedSeconds);

                                    // column might be hidden
                                    try
                                    {
                                        lvi.SubItems[
                                                index: lvchs[
                                                        key: HelperVariables.COL_NAME_PREFIX +
                                                        FileListColumns.CREATE_DATE]
                                                   .Index]
                                           .Text = modifiedCreateDateTime.ToString(
                                            provider: CultureInfo.CurrentUICulture);
                                    }
                                    catch
                                    {
                                        // nothing
                                    }

                                    createAlreadyShifted = true;
                                }
                            }

                            if (attribute is ElementAttribute.GPSLatitude or ElementAttribute.GPSLongitude)
                            {
                                GetCoordinatesAttributeUpdated(
                                    dirElemFileToModify: dirElemFileToModify,
                                    lvi: lvi,
                                    lvchs: lvchs,
                                    destination: false);
                            }
                            else if (attribute is ElementAttribute.GPSDestLatitude or ElementAttribute.GPSDestLongitude)
                            {
                                GetCoordinatesAttributeUpdated(
                                    dirElemFileToModify: dirElemFileToModify,
                                    lvi: lvi,
                                    lvchs: lvchs,
                                    destination: true);
                            }
                        }
                        catch
                        {
                            // nothing. 
                        }
                    }
                }
            }

            lvw.EndUpdate();
            if (lviIndex % 10 == 0)
            {
                Application.DoEvents();
            }
        }

        return Task.CompletedTask;

        static int GetTotalShiftedSeconds(DirectoryElement dirElemFileToModify,
                                   TakenOrCreated takenOrCreated)
        {
            ElementAttribute attributeShiftedDays = takenOrCreated == TakenOrCreated.Taken
                ? ElementAttribute.TakenDateDaysShift
                : ElementAttribute.CreateDateDaysShift;

            ElementAttribute attributeShiftedHours = takenOrCreated == TakenOrCreated.Taken
                ? ElementAttribute.TakenDateHoursShift
                : ElementAttribute.CreateDateHoursShift;

            ElementAttribute attributeShiftedMinutes = takenOrCreated == TakenOrCreated.Taken
                ? ElementAttribute.TakenDateMinutesShift
                : ElementAttribute.CreateDateMinutesShift;

            ElementAttribute attributeShiftedSeconds = takenOrCreated == TakenOrCreated.Taken
                ? ElementAttribute.TakenDateSecondsShift
                : ElementAttribute.CreateDateSecondsShift;

            int shiftedDays = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: attributeShiftedDays,
                version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                notFoundValue: 0);

            int shiftedHours = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: attributeShiftedHours,
                version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                notFoundValue: 0);

            int shiftedMinutes = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: attributeShiftedMinutes,
                version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                notFoundValue: 0);

            int shiftedSeconds = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: attributeShiftedSeconds,
                version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                notFoundValue: 0);

            int totalShiftedSeconds = shiftedSeconds +
                                      (shiftedMinutes * 60) +
                                      (shiftedHours * 60 * 60) +
                                      (shiftedDays * 60 * 60 * 24);
            return totalShiftedSeconds;
        }
    }

    /// <summary>
    /// Updates the (destination) coordinates subitem of a specified ListViewItem with latitude and longitude values retrieved from a
    /// DirectoryElement.
    /// </summary>
    /// <remarks>If either the latitude or longitude attribute is missing, the coordinates subitem will be set
    /// to an empty string.</remarks>
    /// <param name="dirElemFileToModify">The DirectoryElement from which to retrieve the latitude and longitude attribute values.</param>
    /// <param name="lvi">The ListViewItem whose coordinates subitem will be updated.</param>
    /// <param name="lvchs">The collection of column headers for the ListView, used to identify the correct subitem to update.</param>
    /// <param name="destination">true to update the destination coordinates; false to update the regular coordinates.</param>
    private static void GetCoordinatesAttributeUpdated(
        DirectoryElement dirElemFileToModify,
        ListViewItem lvi,
        System.Windows.Forms.ListView.ColumnHeaderCollection lvchs,
        bool destination)
    {
        string tmpLat = dirElemFileToModify.GetAttributeValueAsString(
            attribute: destination ? ElementAttribute.GPSDestLatitude : ElementAttribute.GPSLatitude,
            version: dirElemFileToModify.GetMaxAttributeVersion(destination ? ElementAttribute.GPSDestLatitude : ElementAttribute.GPSLatitude),
            notFoundValue: "",
            nowSavingExif: false);

        string tmpLng = dirElemFileToModify.GetAttributeValueAsString(
            attribute: destination ? ElementAttribute.GPSDestLongitude : ElementAttribute.GPSLongitude,
            version: dirElemFileToModify.GetMaxAttributeVersion(destination ? ElementAttribute.GPSDestLongitude : ElementAttribute.GPSLongitude),
            notFoundValue: "",
            nowSavingExif: false);

        string tmpCoords = $"{tmpLat};{tmpLng}" != ";"
            ? $"{tmpLat};{tmpLng}"
            : "";

        lvi.SubItems[index: lvchs[key:
            GetElementAttributesColumnHeader(destination ? ElementAttribute.DestCoordinates : ElementAttribute.Coordinates)]
            .Index]
            .Text = tmpCoords;
    }

    /// <summary>
    ///     This updates the lbl_ParseProgress with count of items with geodata.
    /// </summary>
    internal static void ListViewCountItemsWithGeoData()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        int DEFileCount = 0;
        int DEFilesWithGeoDataCount = 0;
        if (frmMainAppInstance != null)
        {
            List<DirectoryElement> lvwDirectoryElements =
                (from ListViewItem lvi in frmMainAppInstance.lvw_FileList.Items select lvi.Tag as DirectoryElement)
               .ToList();
            foreach (DirectoryElement directoryElement in lvwDirectoryElements)
            {
                if (directoryElement.Type == DirectoryElement.ElementType.File)
                {
                    DEFileCount++;
                    List<ElementAttribute> GeoDataAttributes = Enum
                                                              .GetValues(
                                                                   enumType:
                                                                   typeof(ElementAttribute))
                                                              .Cast<ElementAttribute>()
                                                              .Where(
                                                                   predicate:
                                                                   GetElementAttributesIsGeoData)
                                                              .ToList();

                    foreach (ElementAttribute geoDataAttribute in GeoDataAttributes)
                    {
                        if (directoryElement.HasSpecificAttributeWithAnyVersion(attribute: geoDataAttribute))
                        {
                            DEFilesWithGeoDataCount++;
                            break;
                        }
                    }
                }
            }
        }

        if (frmMainAppInstance != null)
        {
            FrmMainApp.HandlerUpdateLabelText(
                label: frmMainAppInstance.lbl_ParseProgress,
                text: $"Ready. Files: Total: {DEFileCount} Geodata: {DEFilesWithGeoDataCount}");
        }
    }
}