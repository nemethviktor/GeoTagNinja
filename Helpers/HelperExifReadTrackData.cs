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
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers;

internal static class HelperExifReadTrackData
{
    /// <summary>
    ///     Fires off a command to try to parse Track listOfAsyncCompatibleFileNamesWithOutPath and link them up with data in
    ///     the main grid
    /// </summary>
    /// <param name="trackFileLocationType">File or Folder</param>
    /// <param name="trackFileLocationVal">The location of the above</param>
    /// <param name="useTZAdjust">True or False to whether to adjust Time Zone</param>
    /// <param name="compareTZAgainst">If TZ should be compared against CreateDate or DateTimeOriginal</param>
    /// <param name="TZVal">Value as string, e.g "+01:00"</param>
    /// <param name="GeoMaxIntSecs"></param>
    /// <param name="GeoMaxExtSecs"></param>
    /// <param name="doNotReverseGeoCode">Whether reverse geocoding should be skipped</param>
    /// <param name="language"></param>
    /// <param name="timeShiftSeconds">Int value if GPS time should be shifted.</param>
    /// <returns></returns>
    internal static async Task ExifGetTrackSyncData(string trackFileLocationType,
                                                    string trackFileLocationVal,
                                                    bool useTZAdjust,
                                                    string compareTZAgainst,
                                                    string TZVal,
                                                    int GeoMaxIntSecs,
                                                    int GeoMaxExtSecs,
                                                    bool doNotReverseGeoCode,
                                                    string language,
                                                    int timeShiftSeconds = 0)
    {
        HelperVariables._sErrorMsg = "";
        HelperVariables._sOutputMsg = "";
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        // this is a list of files to be tagged
        List<string> tagFileList = new();
        if (frmMainAppInstance != null)
        {
            foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.SelectedItems)
            {
                string pathToTag = Path.Combine(path1: frmMainAppInstance.tbx_FolderName.Text, path2: lvi.Text);
                if (File.Exists(path: pathToTag))
                {
                    tagFileList.Add(item: pathToTag);
                }
            }

            // this is the list of gpx or json (or other) files that contain the timestamped data. 
            List<string> trackFileList = new();
            if (trackFileLocationType == "file")
            {
                trackFileList.Add(item: trackFileLocationVal);
            }
            else if (trackFileLocationType == "folder")
            {
                trackFileList = Directory
                    .GetFiles(path: trackFileLocationVal)
                    .Where(predicate: file => HelperGenericAncillaryListsArrays.GpxExtensions()
                               .Any(predicate: file.ToLower()
                                        .EndsWith))
                    .ToList();
            }

            #region ExifToolConfiguration

            string exifToolExe = Path.Combine(path1: HelperVariables.ResourcesFolderPath, path2: "exiftool.exe");

            string folderNameToUse = frmMainAppInstance.tbx_FolderName.Text;
            string exiftoolCmd = " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 ";

            List<string> exifArgs = new();
            // needs a space before and after.
            string commonArgs = " -a -s -s -struct -sort -G -ee -args ";

            #endregion

            // add track listOfAsyncCompatibleFileNamesWithOutPath
            foreach (string trackFile in trackFileList)
            {
                exiftoolCmd += " -geotag=" + HelperVariables.SDoubleQuote + trackFile + HelperVariables.SDoubleQuote;
            }

            // add what to compare against + TZ
            string tmpTZAdjust = HelperVariables.SDoubleQuote;
            if (useTZAdjust)
            {
                tmpTZAdjust = TZVal + HelperVariables.SDoubleQuote;
            }

            exiftoolCmd += " " + HelperVariables.SDoubleQuote + "-geotime<${" + compareTZAgainst + "#}" + tmpTZAdjust;

            // time shift
            if (timeShiftSeconds < 0)
            {
                exiftoolCmd += " -geosync=" + timeShiftSeconds;
            }
            else if (timeShiftSeconds > 0)
            {
                exiftoolCmd += " -geosync=+" + timeShiftSeconds;
            }

            // add -api GeoMaxIntSecs & -api GeoMaxExtSecs
            exiftoolCmd += " -api GeoMaxIntSecs=" + GeoMaxIntSecs.ToString(provider: CultureInfo.InvariantCulture);
            exiftoolCmd += " -api GeoMaxExtSecs=" + GeoMaxExtSecs.ToString(provider: CultureInfo.InvariantCulture);

            // add "what files to act upon"
            foreach (string pathToTagFile in tagFileList)
            {
                exiftoolCmd += " " + HelperVariables.SDoubleQuote + pathToTagFile + HelperVariables.SDoubleQuote;
            }

            // verbose logging
            exiftoolCmd += " -v2";

            // add output path to tmp xmp

            // make sure tmp exists -> this goes into "our" folder
            Directory.CreateDirectory(path: HelperVariables.UserDataFolderPath + @"\tmpLocFiles");
            string tmpFolder = Path.Combine(HelperVariables.UserDataFolderPath + @"\tmpLocFiles");

            // this is a little superflous but...
            DirectoryInfo diTmpLocFiles = new(path: tmpFolder);

            foreach (FileInfo file in diTmpLocFiles.EnumerateFiles())
            {
                file.Delete();
            }

            exiftoolCmd += " " + " -srcfile " + HelperVariables.SDoubleQuote + tmpFolder + @"\%F.xmp" + HelperVariables.SDoubleQuote;
            exiftoolCmd += " -overwrite_original_in_place";

            ///////////////
            await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: commonArgs + exiftoolCmd,
                                                         frmMainAppInstance: null,
                                                         initiator: "ExifGetTrackSyncData");

            ///////////////
            //// try to collect the xmp/xml listOfAsyncCompatibleFileNamesWithOutPath and then read them back into the listview.

            foreach (FileInfo exifFileIn in diTmpLocFiles.EnumerateFiles())
            {
                if (exifFileIn.Extension == ".xmp")
                {
                    try
                    {
                        string XMP = File.ReadAllText(path: Path.Combine(path1: tmpFolder, path2: exifFileIn.Name));

                        XmlSerializer serializer = new(type: typeof(xmpmeta));
                        xmpmeta trackFileXMPData;
                        using (StringReader reader = new(s: XMP))
                        {
                            trackFileXMPData = (xmpmeta)serializer.Deserialize(textReader: reader);
                        }

                        if (trackFileXMPData != null)
                        {
                            DataTable dtFileExifTable = new();
                            dtFileExifTable.Clear();
                            dtFileExifTable.Columns.Add(columnName: "attribute");
                            dtFileExifTable.Columns.Add(columnName: "TagValue");

                            PropertyInfo[] properties = typeof(RDFDescription).GetProperties(bindingAttr: BindingFlags.Instance | BindingFlags.Public);

                            foreach (PropertyInfo trackData in properties)
                            {
                                string attribute = "exif:" + trackData.Name;
                                object tagValue = trackData.GetValue(obj: trackFileXMPData.RDF.Description);

                                DataRow dr = dtFileExifTable.NewRow();
                                dr[columnName: "attribute"] = attribute;
                                dr[columnName: "TagValue"] = tagValue;
                                dtFileExifTable.Rows.Add(row: dr);
                            }

                            // de-dupe. this is pretty poor performance but the dataset is small
                            DataTable dtDistinctFileExifTable = dtFileExifTable.DefaultView.ToTable(distinct: true);

                            ListView lvw = frmMainAppInstance.lvw_FileList;
                            ListViewItem lvi = frmMainAppInstance.lvw_FileList.FindItemWithText(text: exifFileIn.Name.Substring(startIndex: 0, length: exifFileIn.Name.Length - 4));

                            if (lvi != null)
                            {
                                ListView.ColumnHeaderCollection lvchs = frmMainAppInstance.ListViewColumnHeaders;

                                ElementAttribute[] toponomyDeletes =
                                {
                                    ElementAttribute.CountryCode,
                                    ElementAttribute.Country,
                                    ElementAttribute.City,
                                    ElementAttribute.State,
                                    ElementAttribute.Sub_location
                                };

                                string fileNameWithoutPath = lvi.Text;
                                DirectoryElement dirElemFileToModify =
                                    FrmMainApp.DirectoryElements.FindElementByItemName(
                                        FileNameWithPath: Path.Combine(path1: FrmMainApp.FolderName,
                                                                       path2: fileNameWithoutPath));

                                // get the current stuff, either from DE3 or Orig or just blank if none.
                                string currentLat = dirElemFileToModify.GetAttributeValueString(
                                    attribute: ElementAttribute.GPSLatitude,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: dirElemFileToModify.GetAttributeValueString(
                                        attribute: ElementAttribute.GPSLatitude,
                                        version: DirectoryElement.AttributeVersion.Original,
                                        notFoundValue: FrmMainApp.NullStringEquivalentGeneric));
                                string currentLng = dirElemFileToModify.GetAttributeValueString(
                                    attribute: ElementAttribute.GPSLongitude,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: dirElemFileToModify.GetAttributeValueString(
                                        attribute: ElementAttribute.GPSLongitude,
                                        version: DirectoryElement.AttributeVersion.Original,
                                        notFoundValue: FrmMainApp.NullStringEquivalentGeneric));
                                string currentAltitude = dirElemFileToModify.GetAttributeValueString(
                                    attribute: ElementAttribute.GPSAltitude,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                    notFoundValue: dirElemFileToModify.GetAttributeValueString(
                                        attribute: ElementAttribute.GPSAltitude,
                                        version: DirectoryElement.AttributeVersion.Original,
                                        notFoundValue: FrmMainApp.NullStringEquivalentGeneric));

                                string strLatInAPI = FrmMainApp.NullStringEquivalentGeneric;
                                try
                                {
                                    strLatInAPI = HelperExifReadExifData.ExifGetStandardisedDataPointFromExif(dtFileExif: dtDistinctFileExifTable, dataPoint: GetAttributeName(attribute: ElementAttribute.GPSLatitude));
                                }
                                catch
                                {
                                    // ignore
                                }

                                string strLngInAPI = FrmMainApp.NullStringEquivalentGeneric;
                                try
                                {
                                    strLngInAPI = HelperExifReadExifData.ExifGetStandardisedDataPointFromExif(dtFileExif: dtDistinctFileExifTable, dataPoint: GetAttributeName(attribute: ElementAttribute.GPSLongitude));
                                }
                                catch
                                {
                                    // ignore
                                }

                                string altitudeInAPI = FrmMainApp.NullStringEquivalentGeneric;
                                try
                                {
                                    altitudeInAPI = HelperExifReadExifData.ExifGetStandardisedDataPointFromExif(dtFileExif: dtDistinctFileExifTable, dataPoint: GetAttributeName(attribute: ElementAttribute.GPSAltitude));
                                    if (int.TryParse(s: altitudeInAPI, result: out int altitudeInAPIInt))
                                    {
                                        if (Math.Abs(value: altitudeInAPIInt) > 20000) // API is stupid
                                        {
                                            altitudeInAPI = currentAltitude;
                                        }
                                    }
                                }
                                catch
                                {
                                    // ignore
                                }

                                bool coordinatesHaveChanged = !(currentLat == strLatInAPI && currentLng == strLngInAPI && currentAltitude == altitudeInAPI);
                                if (coordinatesHaveChanged)
                                {
                                    FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);
                                    frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: fileNameWithoutPath, color: Color.Red);

                                    dirElemFileToModify.SetAttributeValueAnyType(attribute: ElementAttribute.GPSLatitude,
                                                                                 value: strLatInAPI,
                                                                                 version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite, isMarkedForDeletion: false);

                                    dirElemFileToModify.SetAttributeValueAnyType(attribute: ElementAttribute.GPSLongitude,
                                                                                 value: strLngInAPI,
                                                                                 version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite, isMarkedForDeletion: false);

                                    dirElemFileToModify.SetAttributeValueAnyType(attribute: ElementAttribute.GPSAltitude,
                                                                                 value: altitudeInAPI,
                                                                                 version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite, isMarkedForDeletion: false);

                                    // clear city, state etc
                                    foreach (ElementAttribute attribute in toponomyDeletes)
                                    {
                                        dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                                    }

                                    // pull from web
                                    if (!doNotReverseGeoCode)
                                    {
                                        HelperVariables.SApiOkay = true;
                                        DataTable dtToponomy = HelperExifReadExifData.DTFromAPIExifGetToponomyFromWebOrSQL(
                                            lat: strLatInAPI.ToString(provider: CultureInfo.InvariantCulture),
                                            lng: strLngInAPI.ToString(provider: CultureInfo.InvariantCulture),
                                            fileNameWithoutPath: fileNameWithoutPath
                                        );

                                        if (HelperVariables.SApiOkay)
                                        {
                                            List<(ElementAttribute attribute, string toponomyOverwriteVal)> toponomyOverwrites = new()
                                            {
                                                (ElementAttribute.CountryCode, dtToponomy.Rows[index: 0][columnName: "CountryCode"]
                                                     .ToString()),
                                                (ElementAttribute.Country, dtToponomy.Rows[index: 0][columnName: "Country"]
                                                     .ToString()),
                                                (ElementAttribute.City, dtToponomy.Rows[index: 0][columnName: "City"]
                                                     .ToString()),
                                                (ElementAttribute.State, dtToponomy.Rows[index: 0][columnName: "State"]
                                                     .ToString()),
                                                (ElementAttribute.Sub_location, dtToponomy.Rows[index: 0][columnName: "Sub_location"]
                                                     .ToString())
                                            };

                                            foreach ((ElementAttribute attribute, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
                                            {
                                                // these are all strings
                                                dirElemFileToModify.SetAttributeValueAnyType(attribute: toponomyDetail.attribute,
                                                                                             value: toponomyDetail.toponomyOverwriteVal,
                                                                                             version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite, isMarkedForDeletion: false);
                                            }
                                        }
                                    }

                                    await FileListViewReadWrite.ListViewUpdateRowFromDEStage3ReadyToWrite(lvi: lvi);
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
        }

        DialogResult dialogResult = MessageBox.Show(
            text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmImportGpx_AskUserWantsReport"),
            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Question"),
            buttons: MessageBoxButtons.YesNo,
            icon: MessageBoxIcon.Question);
        if (dialogResult == DialogResult.Yes)
        {
            Form reportBox = new();

            reportBox.ControlBox = false;
            FlowLayoutPanel panel = new();

            TextBox tbxText = new();
            tbxText.Size = new Size(width: 700, height: 400);

            tbxText.Text = HelperVariables._sOutputMsg;
            tbxText.ScrollBars = ScrollBars.Vertical;
            tbxText.Multiline = true;
            tbxText.WordWrap = true;
            tbxText.ReadOnly = true;

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
            reportBox.MinimumSize = new Size(width: tbxText.Width + 40, height: btnOk.Bottom + 50);
            reportBox.ShowInTaskbar = false;

            reportBox.StartPosition = FormStartPosition.CenterScreen;
            reportBox.ShowDialog();
        }
    }
}