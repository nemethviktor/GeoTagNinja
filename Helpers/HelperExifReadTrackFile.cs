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
using GeoTagNinja.Model;
using GeoTagNinja.View.DialogAndMessageBoxes;
using GeoTagNinja.View.ListView;
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

        Directory.CreateDirectory(path: HelperVariables.UserDataFolderPath +
                                        @"\tmpLocFiles");
        List<string> trackFileList = new();
        List<string> imageFileList = new();
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
                Path.Combine(HelperVariables.UserDataFolderPath + @"\tmpLocFiles");

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
                if (exifFileIn.Extension == ".xmp")
                {
                    try
                    {
                        string XMP = File.ReadAllText(
                            path: Path.Combine(path1: tmpFolder, path2: exifFileIn.Name));

                        XmlSerializer serializer = new(type: typeof(xmpmeta));
                        xmpmeta trackFileXMPData;
                        using (StringReader reader = new(s: XMP))
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
                                string attribute = "exif:" + trackData.Name;
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

                            lvw = frmMainAppInstance.lvw_FileList;
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
                                    ElementAttribute.Sub_location
                                };

                                DirectoryElement dirElemFileToModify =
                                    lvi.Tag as DirectoryElement;
                                string fileNameWithoutPath =
                                    dirElemFileToModify.ItemNameWithoutPath;

                                // get the current stuff, either from DE3 or Orig or just blank if none.
                                string currentLat =
                                    dirElemFileToModify.GetAttributeValueString(
                                        attribute: ElementAttribute.GPSLatitude,
                                        version: DirectoryElement.AttributeVersion
                                                                 .Stage3ReadyToWrite,
                                        notFoundValue: dirElemFileToModify
                                           .GetAttributeValueString(
                                                attribute: ElementAttribute.GPSLatitude,
                                                version: DirectoryElement.AttributeVersion
                                                                         .Original,
                                                notFoundValue: FrmMainApp
                                                   .NullStringEquivalentGeneric,
                                                nowSavingExif: false),
                                        nowSavingExif: false);
                                string currentLng =
                                    dirElemFileToModify.GetAttributeValueString(
                                        attribute: ElementAttribute.GPSLongitude,
                                        version: DirectoryElement.AttributeVersion
                                                                 .Stage3ReadyToWrite,
                                        notFoundValue: dirElemFileToModify
                                           .GetAttributeValueString(
                                                attribute: ElementAttribute.GPSLongitude,
                                                version: DirectoryElement.AttributeVersion
                                                                         .Original,
                                                notFoundValue: FrmMainApp
                                                   .NullStringEquivalentGeneric,
                                                nowSavingExif: false),
                                        nowSavingExif: false);
                                string currentAltitude =
                                    dirElemFileToModify.GetAttributeValueString(
                                        attribute: ElementAttribute.GPSAltitude,
                                        version: DirectoryElement.AttributeVersion
                                                                 .Stage3ReadyToWrite,
                                        notFoundValue: dirElemFileToModify
                                           .GetAttributeValueString(
                                                attribute: ElementAttribute.GPSAltitude,
                                                version: DirectoryElement.AttributeVersion
                                                                         .Original,
                                                notFoundValue: FrmMainApp
                                                   .NullStringEquivalentGeneric,
                                                nowSavingExif: false),
                                        nowSavingExif: false);

                                string strLatInAPI =
                                    FrmMainApp.NullStringEquivalentGeneric;
                                try
                                {
                                    strLatInAPI =
                                        HelperExifReadExifData
                                           .ExifGetStandardisedDataPointFromExif(
                                                dtFileExif: dtDistinctFileExifTable,
                                                dataPoint: GetElementAttributesName(
                                                    attributeToFind: ElementAttribute
                                                       .GPSLatitude));
                                }
                                catch
                                {
                                    // ignore
                                }

                                string strLngInAPI =
                                    FrmMainApp.NullStringEquivalentGeneric;
                                try
                                {
                                    strLngInAPI =
                                        HelperExifReadExifData
                                           .ExifGetStandardisedDataPointFromExif(
                                                dtFileExif: dtDistinctFileExifTable,
                                                dataPoint: GetElementAttributesName(
                                                    attributeToFind: ElementAttribute
                                                       .GPSLongitude));
                                }
                                catch
                                {
                                    // ignore
                                }

                                string altitudeInAPI =
                                    FrmMainApp.NullStringEquivalentGeneric;
                                try
                                {
                                    altitudeInAPI =
                                        HelperExifReadExifData
                                           .ExifGetStandardisedDataPointFromExif(
                                                dtFileExif: dtDistinctFileExifTable,
                                                dataPoint: GetElementAttributesName(
                                                    attributeToFind: ElementAttribute
                                                       .GPSAltitude));
                                    if (int.TryParse(s: altitudeInAPI,
                                                     result: out int altitudeInAPIInt))
                                    {
                                        if (Math.Abs(value: altitudeInAPIInt) >
                                            20000) // API is stupid
                                        {
                                            altitudeInAPI = currentAltitude;
                                        }
                                    }
                                }
                                catch
                                {
                                    // ignore
                                }

                                bool coordinatesHaveChanged =
                                    !(currentLat == strLatInAPI &&
                                      currentLng == strLngInAPI &&
                                      currentAltitude == altitudeInAPI);
                                if (coordinatesHaveChanged)
                                {
                                    FrmMainApp.HandlerUpdateLabelText(
                                        label: frmMainAppInstance.lbl_ParseProgress,
                                        text: "Processing: " + fileNameWithoutPath);
                                    frmMainAppInstance.lvw_FileList.UpdateItemColour(
                                        itemText: fileNameWithoutPath, color: Color.Red);

                                    dirElemFileToModify.SetAttributeValueAnyType(
                                        attribute: ElementAttribute.GPSLatitude,
                                        value: strLatInAPI,
                                        version: DirectoryElement.AttributeVersion
                                                                 .Stage3ReadyToWrite,
                                        isMarkedForDeletion: false);

                                    dirElemFileToModify.SetAttributeValueAnyType(
                                        attribute: ElementAttribute.GPSLongitude,
                                        value: strLngInAPI,
                                        version: DirectoryElement.AttributeVersion
                                                                 .Stage3ReadyToWrite,
                                        isMarkedForDeletion: false);

                                    dirElemFileToModify.SetAttributeValueAnyType(
                                        attribute: ElementAttribute.GPSAltitude,
                                        value: altitudeInAPI,
                                        version: DirectoryElement.AttributeVersion
                                                                 .Stage3ReadyToWrite,
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
                                                    lat: strLatInAPI.ToString(
                                                        provider: CultureInfo
                                                           .InvariantCulture),
                                                    lng: strLngInAPI.ToString(
                                                        provider: CultureInfo
                                                           .InvariantCulture),
                                                    fileNameWithoutPath:
                                                    fileNameWithoutPath
                                                );

                                        if (HelperVariables
                                           .OperationAPIReturnedOKResponse)
                                        {
                                            List<(ElementAttribute attribute, string
                                                    toponomyOverwriteVal)>
                                                toponomyOverwrites = new()
                                                {
                                                    (ElementAttribute.CountryCode,
                                                     dtToponomy.Rows[index: 0][
                                                             columnName: "CountryCode"]
                                                        .ToString()),
                                                    (ElementAttribute.Country,
                                                     dtToponomy.Rows[index: 0][
                                                             columnName: "Country"]
                                                        .ToString()),
                                                    (ElementAttribute.City,
                                                     dtToponomy.Rows[index: 0][
                                                             columnName: "City"]
                                                        .ToString()),
                                                    (ElementAttribute.State,
                                                     dtToponomy.Rows[index: 0][
                                                             columnName: "State"]
                                                        .ToString()),
                                                    (ElementAttribute.Sub_location,
                                                     dtToponomy.Rows[index: 0][
                                                             columnName: "Sub_location"]
                                                        .ToString())
                                                };

                                            foreach ((ElementAttribute attribute, string
                                                     toponomyOverwriteVal) toponomyDetail
                                                     in toponomyOverwrites)
                                            {
                                                // these are all strings
                                                dirElemFileToModify
                                                   .SetAttributeValueAnyType(
                                                        attribute: toponomyDetail
                                                           .attribute,
                                                        value: toponomyDetail
                                                           .toponomyOverwriteVal,
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
                    finally
                    {
                        File.Delete(path: exifFileIn.FullName); // clean up
                    }
                }
            }

            // this triggers ExifTool
            if (getTrackDataOverlay == TrackOverlaySetting.OverlayForAllDates ||
                getTrackDataOverlay == TrackOverlaySetting.OverlayForOverlappingDates)
            {
                await HelperExifReadTrackFilePath.ExifReadTrackFileForMapping(trackFileList: trackFileList,
                    overlayDateSetting: getTrackDataOverlay, overlayDateList: overlayDateList, TZVal: TZVal);
                frmMainAppInstance.Request_Map_NavigateGo();
            }
        }

        CustomMessageBox customMessageBox = new(
            text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                messageBoxName: "mbx_FrmImportExportGpx_AskUserWantsReport"),
            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Question
                                                               .ToString()),
            buttons: MessageBoxButtons.YesNo,
            icon: MessageBoxIcon.Question);
        DialogResult dialogResult = customMessageBox.ShowDialog();
        if (dialogResult == DialogResult.Yes)
        {
            Form reportBox = new();

            reportBox.ControlBox = false;
            FlowLayoutPanel panel = new();

            TextBox tbxText = new();
            tbxText.Size = new Size(width: 700, height: 400);

            tbxText.Text = HelperVariables._sOutputAndErrorMsg;
            tbxText.ScrollBars = ScrollBars.Vertical;
            tbxText.Multiline = true;
            tbxText.WordWrap = true;
            tbxText.ReadOnly = true;
            tbxText.SelectionStart = 1;
            tbxText.SelectionLength = 0;

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
            HelperControlThemeManager.SetThemeColour(
                themeColour: HelperVariables.UserSettingUseDarkMode
                    ? ThemeColour.Dark
                    : ThemeColour.Light,
                parentControl: reportBox);
            reportBox.ShowDialog();
        }
    }
}