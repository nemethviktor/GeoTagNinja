using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using static GeoTagNinja.Model.SourcesAndAttributes;
using static GeoTagNinja.View.ListView.FileListView;

namespace GeoTagNinja.View.ListView;

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
    /// <param name="lvi"></param>
    internal static Task ListViewUpdateRowFromDEStage3ReadyToWrite(ListViewItem lvi)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        if (frmMainAppInstance != null)
        {
            System.Windows.Forms.ListView lvw = frmMainAppInstance.lvw_FileList;
            System.Windows.Forms.ListView.ColumnHeaderCollection lvchs =
                frmMainAppInstance.ListViewColumnHeaders;

            int d = lvi.Index;
            if (lvi.Tag is DirectoryElement dirElemFileToModify && dirElemFileToModify.HasDirtyAttributes())
            {
                string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;
                frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: fileNameWithoutPath, color: Color.Red);
                bool takenAlreadyShifted = false;
                bool createAlreadyShifted = false;
                lvw.BeginUpdate();
                foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                {
                    if (dirElemFileToModify.HasSpecificAttributeWithVersion(attribute: attribute,
                                                                            version: DirectoryElement.AttributeVersion
                                                                                                     .Stage3ReadyToWrite))
                    {
                        try
                        {
                            // theoretically we'd want to update the columns for each tag but for example when removing all data
                            // this becomes tricky bcs we're also firing a "-gps*=" tag.
                            string settingId = GetElementAttributesColumnHeader(attribute);
                            string settingVal = dirElemFileToModify.GetAttributeValueString(attribute: attribute,
                                                                                            version: DirectoryElement.AttributeVersion
                                                                                               .Stage3ReadyToWrite, nowSavingExif: false);

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
                                if (settingId.Substring(startIndex: 4)
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
                                                        key: COL_NAME_PREFIX +
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
                                else if (settingId.Substring(startIndex: 4)
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
                                                        key: COL_NAME_PREFIX +
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
                                string tmpLat = dirElemFileToModify.GetAttributeValueString(
                                    attribute: ElementAttribute.GPSLatitude,
                                    version: dirElemFileToModify.GetMaxAttributeVersion(ElementAttribute.GPSLatitude),
                                    notFoundValue: "", nowSavingExif: false);
                                string tmpLng = dirElemFileToModify.GetAttributeValueString(
                                    attribute: ElementAttribute.GPSLongitude,
                                    version: dirElemFileToModify.GetMaxAttributeVersion(ElementAttribute.GPSLongitude),
                                    notFoundValue: "", nowSavingExif: false);
                                string tmpCoords = tmpLat + ";" + tmpLng != ";"
                                    ? tmpLat + ";" + tmpLng
                                    : "";

                                lvi.SubItems[index: lvchs[key: GetElementAttributesColumnHeader(ElementAttribute.Coordinates)]
                                                .Index]
                                   .Text = tmpCoords;
                            }
                            else if (attribute is ElementAttribute.GPSDestLatitude or ElementAttribute.GPSDestLongitude)
                            {
                                string tmpLat = dirElemFileToModify.GetAttributeValueString(
                                    attribute: ElementAttribute.GPSDestLatitude,
                                    version: dirElemFileToModify.GetMaxAttributeVersion(ElementAttribute.GPSDestLatitude),
                                    notFoundValue: "", nowSavingExif: false);
                                string tmpLng = dirElemFileToModify.GetAttributeValueString(
                                    attribute: ElementAttribute.GPSDestLongitude,
                                    version: dirElemFileToModify.GetMaxAttributeVersion(ElementAttribute.GPSDestLongitude),
                                    notFoundValue: "", nowSavingExif: false);
                                string tmpCoords = tmpLat + ";" + tmpLng != ";"
                                    ? tmpLat + ";" + tmpLng
                                    : "";

                                lvi.SubItems[index: lvchs[key: GetElementAttributesColumnHeader(ElementAttribute.DestCoordinates)]
                                                .Index]
                                   .Text = tmpCoords;
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
            if (d % 10 == 0)
            {
                Application.DoEvents();
            }
        }

        return Task.CompletedTask;

        int GetTotalShiftedSeconds(DirectoryElement dirElemFileToModify,
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
                                      shiftedMinutes * 60 +
                                      shiftedHours * 60 * 60 +
                                      shiftedDays * 60 * 60 * 24;
            return totalShiftedSeconds;
        }
    }

    /// <summary>
    ///     This updates the lbl_ParseProgress with count of items with geodata.
    /// </summary>
    [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
    internal static void ListViewCountItemsWithGeoData()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        int DEFileCount = 0;
        int DEFilesWithGeoDataCount = 0;
        foreach (DirectoryElement directoryElement in FrmMainApp.DirectoryElements)
        {
            if ((directoryElement.Type == DirectoryElement.ElementType.File && Program.CollectionModeEnabled) ||
                (directoryElement.Type == DirectoryElement.ElementType.File && File.Exists(
                    path: Path.Combine(path1: FrmMainApp.FolderName, path2: directoryElement.ItemNameWithoutPath))))
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

        if (frmMainAppInstance != null)
        {
            FrmMainApp.HandlerUpdateLabelText(
                label: frmMainAppInstance.lbl_ParseProgress,
                text: "Ready. Files: Total: " +
                      DEFileCount +
                      " Geodata: " +
                      DEFilesWithGeoDataCount);
        }
    }
}