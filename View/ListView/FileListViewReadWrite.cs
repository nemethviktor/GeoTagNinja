using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja;

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
        string tmpCoordinates;
        if (frmMainAppInstance != null)
        {
            ListView lvw = frmMainAppInstance.lvw_FileList;
            ListView.ColumnHeaderCollection lvchs = frmMainAppInstance.ListViewColumnHeaders;

            int d = lvi.Index;
            DirectoryElement dirElemFileToModify = FrmMainApp.DirectoryElements.FindElementByItemGUID(GUID: lvi.SubItems[index: lvw.Columns[key: "clh_GUID"]
                                                                                                                                   .Index]
                                                                                                               .Text);
            string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;
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
            if (directoryElement.Type == DirectoryElement.ElementType.File)
            {
                DEFileCount++;
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