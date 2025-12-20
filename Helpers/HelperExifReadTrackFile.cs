using GeoTagNinja.Model;
using GeoTagNinja.View.ListView;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using WinFormsDarkThemerNinja;
using static GeoTagNinja.Helpers.HelperGenericAncillaryListsArrays;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers;

internal static class HelperExifReadTrackFile
{
    /// <summary>
    ///     Fires off a command to try to parse Track files and link them up with data in
    ///     the main grid
    /// </summary>
    /// <param name="trackFileLocationType">File or Folder</param>
    /// <param name="trackFileLocationVal">The location of the above</param>
    /// <param name="compareTZAgainst">If TZ should be compared against CreateDate or DateTimeOriginal</param>
    /// <param name="TZVal">Value as string, e.g "+01:00"</param>
    /// <param name="GeoMaxIntSecs"></param>
    /// <param name="GeoMaxExtSecs"></param>
    /// <param name="doNotReverseGeoCode">Whether reverse geocoding should be skipped</param>
    /// <param name="getTrackDataOverlay">Whether to overlay the track data</param>
    /// <param name="overlayDateList">Whether to fire up the overlay-track process and if so how</param>
    /// <param name="timeShiftSeconds">Int value if GPS time should be shifted.</param>
    /// <returns></returns>
    internal static async Task ExifGetTrackSyncData(string trackFileLocationType,
        string trackFileLocationVal,
        string compareTZAgainst,
        string TZVal,
        int GeoMaxIntSecs,
        int GeoMaxExtSecs,
        bool doNotReverseGeoCode,
        TrackOverlaySetting getTrackDataOverlay,
        List<DateTime> overlayDateList,
        int timeShiftSeconds = 0)
    {
        //HelperVariables._sErrorMsg = "";
        HelperVariables._sOutputAndErrorMsg = "";
        FrmMainApp frmMainAppInstance =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        Directory.CreateDirectory(path: $@"{HelperVariables.UserDataFolderPath}\tmpLocFiles");
        List<string> trackFileList = [];
        List<string> imageFileList = [];
        ListView lvw = frmMainAppInstance.lvw_FileList;

        if (frmMainAppInstance != null)
        {
            // trackFileList
            if (trackFileLocationType == "file")
            {
                trackFileList.Add(item: trackFileLocationVal);
            }
            else if (trackFileLocationType == "folder")
            {
                trackFileList = Directory
                               .GetFiles(path: trackFileLocationVal)
                               .Where(predicate: file => GpxExtensions()
                                                        .Any(predicate: file.ToLower()
                                                            .EndsWith))
                               .ToList();
            }

            // imageFileList
            foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.SelectedItems)
            {
                DirectoryElement dirElemFileToModify =
                    lvi.Tag as DirectoryElement;
                string pathToTag = dirElemFileToModify.FileNameWithPath;
                if (File.Exists(path: pathToTag))
                {
                    imageFileList.Add(item: pathToTag);
                }
            }

            string tmpFolder =
                Path.Combine($@"{HelperVariables.UserDataFolderPath}\tmpLocFiles");

            // this is a little superflous but...
            DirectoryInfo diTmpLocFiles = new(path: tmpFolder);

            foreach (FileInfo file in diTmpLocFiles.EnumerateFiles())
            {
                file.Delete();
            }

            // this triggers ExifTool
            await HelperExifWriteTrackDataToSideCar.ExifWriteTrackDataToSideCar(
                trackFileList: trackFileList,
                imageFileList: imageFileList,
                compareTZAgainst: compareTZAgainst,
                TZVal: TZVal,
                GeoMaxIntSecs: GeoMaxIntSecs,
                GeoMaxExtSecs: GeoMaxExtSecs,
                timeShiftSeconds: timeShiftSeconds
            );

            foreach (FileInfo exifFileIn in diTmpLocFiles.EnumerateFiles())
            {
                // note to self -> this is the xmp file in the folder produced by the track parsing, not an xmp file that belongs to a nef file etc.
                if (exifFileIn.Extension == ".xmp")
                {
                    try
                    {
                        string textInTheXMPFile = File.ReadAllText(
                            path: Path.Combine(path1: tmpFolder, path2: exifFileIn.Name));

                        XmlSerializer serializer = new(type: typeof(xmpmeta));
                        xmpmeta trackFileXMPData;
                        using (StringReader reader = new(s: textInTheXMPFile))
                        {
                            trackFileXMPData =
                                (xmpmeta)serializer.Deserialize(textReader: reader);
                        }

                        if (trackFileXMPData != null)
                        {
                            DataTable dtFileExifTable = new();
                            dtFileExifTable.Clear();
                            dtFileExifTable.Columns.Add(columnName: "attribute");
                            dtFileExifTable.Columns.Add(columnName: "TagValue");

                            PropertyInfo[] properties =
                                typeof(RDFDescription).GetProperties(
                                    bindingAttr: BindingFlags.Instance |
                                                 BindingFlags.Public);

                            foreach (PropertyInfo trackData in properties)
                            {
                                string attribute = $"exif:{trackData.Name}";
                                object tagValue =
                                    trackData.GetValue(
                                        obj: trackFileXMPData.RDF.Description);

                                DataRow dr = dtFileExifTable.NewRow();
                                dr[columnName: "attribute"] = attribute;
                                dr[columnName: "TagValue"] = tagValue;
                                dtFileExifTable.Rows.Add(row: dr);
                            }

                            // de-dupe. this is pretty poor performance but the dataset is small
                            DataTable dtDistinctFileExifTable =
                                dtFileExifTable.DefaultView.ToTable(distinct: true);

                            // note to self -> should find a way to handle multiple identically-named files somehow.
                            ListViewItem lvi =
                                frmMainAppInstance.lvw_FileList.FindItemWithText(
                                    text: exifFileIn.Name.Substring(
                                        startIndex: 0,
                                        length: exifFileIn.Name.Length - 4));

                            if (lvi != null)
                            {
                                ListView.ColumnHeaderCollection lvchs =
                                    frmMainAppInstance.ListViewColumnHeaders;

                                ElementAttribute[] toponomyDeletes =
                                {
                                    ElementAttribute.CountryCode,
                                    ElementAttribute.Country,
                                    ElementAttribute.City,
                                    ElementAttribute.State,
                                    ElementAttribute.Sublocation
                                };

                                DirectoryElement dirElemFileToModify = lvi.Tag as DirectoryElement;
                                string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;

                                // get the current stuff, either from DE3 or Orig or just blank if none.
                                // we're doing this so we can compare if the data received from the track parse is different from the one in the file
                                // if so then later we'll trigger the store-to-Stage3-process

                                string currentLat = GetCurrentValue(dirElemFileToModify: dirElemFileToModify,
                                    attribute: ElementAttribute.GPSDestLatitude);
                                string currentLng = GetCurrentValue(dirElemFileToModify: dirElemFileToModify,
                                    attribute: ElementAttribute.GPSLongitude);
                                string currentAltitude = GetCurrentValue(dirElemFileToModify: dirElemFileToModify,
                                    attribute: ElementAttribute.GPSAltitude);

                                string strLatInTrackFile = GetValueInTrackFile(
                                    dtDistinctFileExifTable: dtDistinctFileExifTable,
                                    attribute: ElementAttribute.GPSLatitude);
                                string strLngInTrackFile = GetValueInTrackFile(
                                    dtDistinctFileExifTable: dtDistinctFileExifTable,
                                    attribute: ElementAttribute.GPSLongitude);
                                string altitudeInTrackFile = GetValueInTrackFile(
                                    dtDistinctFileExifTable: dtDistinctFileExifTable,
                                    attribute: ElementAttribute.GPSAltitude);
                                string gpsDOPInTrackFile = GetValueInTrackFile(
                                    dtDistinctFileExifTable: dtDistinctFileExifTable,
                                    attribute: ElementAttribute.GPSDOP);
                                string gpsHPositioningErrorInTrackFile = GetValueInTrackFile(
                                    dtDistinctFileExifTable: dtDistinctFileExifTable,
                                    attribute: ElementAttribute.GPSHPositioningError);

                                bool coordinatesHaveChanged =
                                    !(currentLat == strLatInTrackFile &&
                                      currentLng == strLngInTrackFile &&
                                      currentAltitude == altitudeInTrackFile);
                                if (coordinatesHaveChanged)
                                {
                                    FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress,
                                        text: $"Processing: {fileNameWithoutPath}");
                                    frmMainAppInstance.lvw_FileList.UpdateItemColour(
                                        directoryElement: dirElemFileToModify, color: Color.Red);
                                    dirElemFileToModify.SetAttributeValueAnyType(
                                        attribute: ElementAttribute.GPSLatitude, value: strLatInTrackFile,
                                        version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                        isMarkedForDeletion: false);
                                    dirElemFileToModify.SetAttributeValueAnyType(
                                        attribute: ElementAttribute.GPSLongitude, value: strLngInTrackFile,
                                        version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                        isMarkedForDeletion: false);
                                    dirElemFileToModify.SetAttributeValueAnyType(
                                        attribute: ElementAttribute.GPSAltitude, value: altitudeInTrackFile,
                                        version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                        isMarkedForDeletion: false);
                                    dirElemFileToModify.SetAttributeValueAnyType(
                                        attribute: ElementAttribute.GPSDOP,
                                        value: gpsDOPInTrackFile,
                                        version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                        isMarkedForDeletion: false);
                                    dirElemFileToModify.SetAttributeValueAnyType(
                                        attribute: ElementAttribute.GPSHPositioningError,
                                        value: gpsHPositioningErrorInTrackFile,
                                        version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                        isMarkedForDeletion: false);

                                    // clear city, state etc
                                    foreach (ElementAttribute attribute in
                                             toponomyDeletes)
                                    {
                                        dirElemFileToModify.RemoveAttributeValue(
                                            attribute: attribute,
                                            version: DirectoryElement.AttributeVersion
                                                                     .Stage3ReadyToWrite);
                                    }

                                    // pull from web
                                    if (!doNotReverseGeoCode)
                                    {
                                        HelperVariables.OperationAPIReturnedOKResponse =
                                            true;
                                        DataTable dtToponomy =
                                            HelperExifReadExifData
                                               .DTFromAPIExifGetToponomyFromWebOrSQL(
                                                    lat: strLatInTrackFile.ToString(
                                                        provider: CultureInfo.InvariantCulture),
                                                    lng: strLngInTrackFile.ToString(
                                                        provider: CultureInfo.InvariantCulture),
                                                    fileNameWithoutPath: fileNameWithoutPath,
                                                    useDefaultHardcodedEnglishValues: true
                                                );

                                        if (HelperVariables
                                           .OperationAPIReturnedOKResponse)
                                        {
                                            List<(ElementAttribute attribute, string
                                                    toponomyOverwriteVal)>
                                                toponomyOverwrites =
                                                [
                                                    (ElementAttribute.CountryCode,
                                                     dtToponomy.Rows[index: 0][columnName: "CountryCode"].ToString()),
                                                    (ElementAttribute.Country,
                                                     dtToponomy.Rows[index: 0][columnName: "Country"].ToString()),
                                                    (ElementAttribute.City,
                                                     dtToponomy.Rows[index: 0][columnName: "City"].ToString()),
                                                    (ElementAttribute.State,
                                                     dtToponomy.Rows[index: 0][columnName: "State"].ToString()),
                                                    (ElementAttribute.Sublocation,
                                                     dtToponomy.Rows[index: 0][columnName: "Sublocation"].ToString())
                                                ];

                                            foreach ((ElementAttribute attribute, string
                                                     toponomyOverwriteVal) in toponomyOverwrites)
                                            {
                                                // these are all strings
                                                dirElemFileToModify
                                                   .SetAttributeValueAnyType(
                                                        attribute: attribute,
                                                        value: toponomyOverwriteVal,
                                                        version: DirectoryElement.AttributeVersion
                                                           .Stage3ReadyToWrite,
                                                        isMarkedForDeletion: false);
                                            }
                                        }
                                    }

                                    await FileListViewReadWrite
                                       .ListViewUpdateRowFromDEStage3ReadyToWrite(
                                            lvi: lvi);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // nothing. errors should have already come up
                    }
#if !DEBUG
                    finally
                    {
                        File.Delete(path: exifFileIn.FullName); // clean up
                    }
#endif
                }
            }

            // this triggers ExifTool
            if (getTrackDataOverlay is TrackOverlaySetting.OverlayForAllDates or
                TrackOverlaySetting.OverlayForOverlappingDates)
            {
                await HelperExifReadTrackFilePath.ExifReadTrackFileForMapping(trackFileList: trackFileList,
                    overlayDateSetting: getTrackDataOverlay, overlayDateList: overlayDateList, TZVal: TZVal);
                frmMainAppInstance.Request_Map_NavigateGo();
            }
        }

        DialogResult dialogResult =
            Themer.ShowMessageBoxWithResult(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_FrmImportExportGpx_AskUserWantsReport",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                icon: MessageBoxIcon.Question,
                buttons: MessageBoxButtons.YesNo);

        if (dialogResult == DialogResult.Yes)
        {
            Form reportBox = new()
            {
                ControlBox = false
            };
            FlowLayoutPanel panel = new();

            TextBox tbxText = new()
            {
                Size = new Size(width: 700, height: 400),

                Text = HelperVariables._sOutputAndErrorMsg,
                ScrollBars = ScrollBars.Vertical,
                Multiline = true,
                WordWrap = true,
                ReadOnly = true,
                SelectionStart = 1,
                SelectionLength = 0
            };

            panel.SetFlowBreak(control: tbxText, value: true);
            panel.Controls.Add(value: tbxText);

            Button btnOk = new()
            { Text = "OK" };
            btnOk.Click += (sender,
                            e) =>
            {
                reportBox.Close();
            };
            btnOk.Location = new Point(x: 10, y: tbxText.Bottom + 5);
            btnOk.AutoSize = true;
            panel.Controls.Add(value: btnOk);

            panel.Padding = new Padding(all: 5);
            panel.AutoSize = true;

            reportBox.Controls.Add(value: panel);
            reportBox.MinimumSize =
                new Size(width: tbxText.Width + 40, height: btnOk.Bottom + 50);
            reportBox.ShowInTaskbar = false;

            reportBox.StartPosition = FormStartPosition.CenterScreen;
            Themer.ApplyThemeToControl(
                control: reportBox,
                themeStyle: HelperVariables.UserSettingUseDarkMode ?
                Themer.ThemeStyle.Custom :
                Themer.ThemeStyle.Default
                );
            reportBox.ShowDialog();
        }

        string GetCurrentValue(DirectoryElement dirElemFileToModify,
                               ElementAttribute attribute)
        {
            string currentValue = dirElemFileToModify.GetAttributeValueString(
                attribute: attribute,
                version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                notFoundValue: dirElemFileToModify.GetAttributeValueString(
                    attribute: attribute,
                    version: DirectoryElement.AttributeVersion.Original,
                    notFoundValue: FrmMainApp.NullStringEquivalentGeneric, nowSavingExif: false),
                nowSavingExif: false);
            return currentValue;
        }

        string GetValueInTrackFile(DataTable dtDistinctFileExifTable,
                                   ElementAttribute attribute)
        {
            string apiValue = FrmMainApp.NullStringEquivalentGeneric;
            try
            {
                apiValue = HelperExifReadExifData.ExifGetStandardisedDataPointFromExifAsString(
                    dtFileExif: dtDistinctFileExifTable,
                    dataPoint: GetElementAttributesName(
                        attributeToFind: attribute));
            }
            catch
            {
                // ignore
            }

            return apiValue;
        }
    }
}