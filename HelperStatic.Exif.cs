﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using CsvHelper;
using geoTagNinja;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
    private static int _exifInvokeCounter;

    /// <summary>
    ///     Wrangles data from raw exiftool output to presentable and standardised data.
    /// </summary>
    /// <param name="dtFileExif">Raw values tag from exiftool</param>
    /// <param name="dataPoint">Name of the exiftag we want the data for</param>
    /// <returns>Standardised exif tag output</returns>
    private static string ExifGetStandardisedDataPointFromExif(DataTable dtFileExif,
                                                               string dataPoint)
    {
        string returnVal = "";

        string tmpLongVal = "-";
        string tryDataValue = "-";
        string tmpLatRefVal = "-";
        string tmpLongRefVal = "-";
        string tmpLatLongRefVal = "-";

        string tmpOutLatLongVal = "";

        FrmMainApp.Logger.Trace(message: "Starting - dataPoint:" + dataPoint);
        try
        {
            tryDataValue = ExifGetRawDataPointFromExif(dtFileExif: dtFileExif, dataPoint: dataPoint);
            // Not logging this bcs it gets called inside and is basically redunant here.
            // FrmMainApp.Logger.Trace(message: "dataPoint:" + dataPoint + " - ExifGetRawDataPointFromExif: " + tryDataValue);
        }
        catch (Exception ex)
        {
            FrmMainApp.Logger.Error(message: "datapoint:" + dataPoint + " - Error: " + ex.Message);
        }

        switch (dataPoint)
        {
            case "GPSLatitude" or "GPSDestLatitude" or "GPSLongitude" or "GPSDestLongitude":
                if (tryDataValue != "-")
                {
                    // we want N instead of North etc.
                    // Get the Ref Tag for the corresponding data point and thereof the first character
                    // (Should be N of North, etc.)
                    // If this character is not contained in the data point value, add it before it
                    // Finally ensure that dec sep. is "."
                    try
                    {
                        tmpLatLongRefVal = ExifGetRawDataPointFromExif(dtFileExif: dtFileExif, dataPoint: dataPoint + "Ref")
                            .Substring(startIndex: 0, length: 1);
                    }
                    catch
                    {
                        tmpLatLongRefVal = "-";
                    }

                    if (!tryDataValue.Contains(value: tmpLatLongRefVal) && tmpLatLongRefVal != "-")
                    {
                        tryDataValue = tmpLatLongRefVal + tryDataValue;
                    }

                    tmpOutLatLongVal = GenericAdjustLatLongNegative(point: tryDataValue)
                        .ToString()
                        .Replace(oldChar: ',', newChar: '.');
                    tryDataValue = tmpOutLatLongVal;
                }

                tryDataValue = tmpOutLatLongVal;
                break;
            case "Coordinates" or "DestCoordinates":
                string isDest;
                if (dataPoint.Contains(value: "Dest"))
                {
                    isDest = "Dest";
                }
                else
                {
                    isDest = "";
                }
                // this is entirely the duplicate of the above

                // check there is lat/long
                string tmpLatVal = ExifGetRawDataPointFromExif(dtFileExif: dtFileExif, dataPoint: "GPS" + isDest + "Latitude")
                    .Replace(oldChar: ',', newChar: '.');
                tmpLongVal = ExifGetRawDataPointFromExif(dtFileExif: dtFileExif, dataPoint: "GPS" + isDest + "Longitude")
                    .Replace(oldChar: ',', newChar: '.');
                if (tmpLatVal == "")
                {
                    tmpLatVal = "-";
                }

                if (tmpLongVal == "")
                {
                    tmpLongVal = "-";
                }

                if (ExifGetRawDataPointFromExif(dtFileExif: dtFileExif, dataPoint: "GPS" + isDest + "LatitudeRef")
                        .Length >
                    0 &&
                    ExifGetRawDataPointFromExif(dtFileExif: dtFileExif, dataPoint: "GPS" + isDest + "LongitudeRef")
                        .Length >
                    0)
                {
                    tmpLatRefVal = ExifGetRawDataPointFromExif(dtFileExif: dtFileExif, dataPoint: "GPS" + isDest + "LatitudeRef")
                        .Substring(startIndex: 0, length: 1)
                        .Replace(oldChar: ',', newChar: '.');
                    tmpLongRefVal = ExifGetRawDataPointFromExif(dtFileExif: dtFileExif, dataPoint: "GPS" + isDest + "LongitudeRef")
                        .Substring(startIndex: 0, length: 1)
                        .Replace(oldChar: ',', newChar: '.');
                }

                // this shouldn't really happen but ET v12.49 extracts trackfile data in the wrong format so...
                else if ((tmpLatVal.Contains(value: 'N') || tmpLatVal.Contains(value: 'S')) && (tmpLongVal.Contains(value: 'E') || tmpLongVal.Contains(value: 'W')))
                {
                    if (tmpLatVal.Contains(value: 'N'))
                    {
                        tmpLatRefVal = "N";
                    }
                    else
                    {
                        tmpLatRefVal = "S";
                    }

                    if (tmpLongVal.Contains(value: 'E'))
                    {
                        tmpLongRefVal = "E";
                    }
                    else
                    {
                        tmpLongRefVal = "W";
                    }
                }
                else
                {
                    tmpLatRefVal = "-";
                    tmpLongRefVal = "-";
                }

                // check there is one bit of data for both components
                if (tmpLatVal != "-" && tmpLongVal != "-")
                {
                    // stick Ref at the end of LatLong
                    if (!tmpLatVal.Contains(value: tmpLatRefVal))
                    {
                        tmpLatVal += tmpLatRefVal;
                    }

                    if (!tmpLongVal.Contains(value: tmpLongRefVal))
                    {
                        tmpLongVal += tmpLongRefVal;
                    }

                    tmpLatVal = GenericAdjustLatLongNegative(point: tmpLatVal)
                        .ToString()
                        .Replace(oldChar: ',', newChar: '.');
                    tmpLongVal = GenericAdjustLatLongNegative(point: tmpLongVal)
                        .ToString()
                        .Replace(oldChar: ',', newChar: '.');
                    tryDataValue = tmpLatVal + ";" + tmpLongVal;
                }
                else
                {
                    tryDataValue = "-";
                }

                break;
            case "GPSAltitude":
                if (tryDataValue.Contains(value: "m"))
                {
                    tryDataValue = tryDataValue.Split('m')[0]
                        .Trim()
                        .Replace(oldChar: ',', newChar: '.');
                }

                if (tryDataValue.Contains(value: "/"))
                {
                    if (tryDataValue.Contains(value: ",") || tryDataValue.Contains(value: "."))
                    {
                        tryDataValue = tryDataValue.Split('/')[0]
                            .Trim()
                            .Replace(oldChar: ',', newChar: '.');
                    }
                    else // attempt to convert it to decimal
                    {
                        try
                        {
                            bool parseBool = double.TryParse(s: tryDataValue.Split('/')[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double numerator);
                            parseBool = double.TryParse(s: tryDataValue.Split('/')[1], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double denominator);
                            tryDataValue = Math.Round(value: numerator / denominator, digits: 2)
                                .ToString(provider: CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            tryDataValue = "0.0";
                        }
                    }
                }

                break;
            case "GPSAltitudeRef":
                if (tryDataValue.ToLower()
                        .Contains(value: "below") ||
                    tryDataValue.Contains(value: "1"))
                {
                    tryDataValue = "Below Sea Level";
                }
                else
                {
                    tryDataValue = "Above Sea Level";
                }

                break;
            case "ExposureTime":
                tryDataValue = tryDataValue.Replace(oldValue: "sec", newValue: "")
                    .Trim();
                break;
            case "Fnumber" or "FocalLength" or "FocalLengthIn35mmFormat" or "ISO":
                if (tryDataValue != "-")
                {
                    if (dataPoint == "FocalLengthIn35mmFormat")
                    {
                        // at least with a Canon 40D this returns stuff like: "51.0 mm (35 mm equivalent: 81.7 mm)" so i think it's safe to assume that 
                        // this might need a bit of debugging and community feeback. or someone with decent regex knowledge
                        if (tryDataValue.Contains(value: ':'))
                        {
                            tryDataValue = Regex.Replace(input: tryDataValue, pattern: @"[^\d:.]", replacement: "")
                                .Split(':')
                                .Last();
                        }
                        else
                        {
                            // this is untested. soz. feedback welcome.
                            tryDataValue = Regex.Replace(input: tryDataValue, pattern: @"[^\d:.]", replacement: "");
                        }
                    }
                    else
                    {
                        tryDataValue = tryDataValue.Replace(oldValue: "mm", newValue: "")
                            .Replace(oldValue: "f/", newValue: "")
                            .Replace(oldValue: "f", newValue: "")
                            .Replace(oldValue: "[", newValue: "")
                            .Replace(oldValue: "]", newValue: "")
                            .Trim();
                    }

                    if (tryDataValue.Contains(value: "/"))
                    {
                        tryDataValue = Math.Round(value: double.Parse(s: tryDataValue.Split('/')[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture) / double.Parse(s: tryDataValue.Split('/')[1], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture), digits: 1)
                            .ToString();
                    }
                }

                break;
            case /*"FileModifyDate" or */"TakenDate" or "CreateDate":
            {
                if (DateTime.TryParse(s: tryDataValue, result: out DateTime outDateTime))
                {
                    tryDataValue = GenericStringToDateTimeBackToString(dateTimeToConvert: tryDataValue);
                }
                else
                {
                    tryDataValue = "-";
                }

                break;
            }
        }

        FrmMainApp.Logger.Trace(message: "Done - dataPoint:" +
                                         dataPoint +
                                         ": " +
                                         tryDataValue);
        returnVal = tryDataValue;
        return returnVal;
    }

    /// <summary>
    ///     This translates between plain English and exiftool tags. For example if the tag we are looking for is "Model" (of
    ///     the camera)..
    ///     ... this will get all the possible tag names where model-info can sit and extract those from the data. E.g. if
    ///     we're looking for Model ...
    ///     this will get both EXIF:Model and and XMP:Model - as it does a cartesian join on the objectNames table.
    /// </summary>
    /// <param name="dtFileExif">Raw exiftool outout of all tags</param>
    /// <param name="dataPoint">Plain English datapoint we're after</param>
    /// <returns>Value of that datapoint if exists (e.g "Canon EOS 30D") - unwrangled, raw.</returns>
    private static string ExifGetRawDataPointFromExif(DataTable dtFileExif,
                                                      string dataPoint)
    {
        FrmMainApp.Logger.Trace(message: "Starting - dataPoint:" + dataPoint);
        string returnVal = "-";
        string tryDataValue = "-";

        DataTable dtObjectTagNamesIn = GenericJoinDataTables(t1: FrmMainApp.DtObjectNames, t2: FrmMainApp.DtObjectTagNamesIn,
                                                             (row1,
                                                              row2) =>
                                                                 row1.Field<string>(columnName: "objectName") == row2.Field<string>(columnName: "objectName"));

        DataTable? dtObjectTagNameIn = null;
        try
        {
            dtObjectTagNameIn = dtObjectTagNamesIn.Select(filterExpression: "objectName = '" + dataPoint + "'")
                .CopyToDataTable();
            dtObjectTagNameIn.DefaultView.Sort = "valuePriorityOrder";
            dtObjectTagNameIn = dtObjectTagNameIn.DefaultView.ToTable();
        }
        catch
        {
            // This will always fire for anything Coordinate-related
            FrmMainApp.Logger.Info(message: "dataPoint:" + dataPoint + " - Not in dtObjectTagNameIn");
            dtObjectTagNameIn = null;
        }

        if (dtObjectTagNameIn != null)
        {
            DataTable dtTagsWanted = dtObjectTagNameIn.DefaultView.ToTable(distinct: true, "objectTagName_In");

            if (dtTagsWanted.Rows.Count > 0 && dtFileExif.Rows.Count > 0)
            {
                foreach (DataRow drTagWanted in dtTagsWanted.Rows)
                {
                    try
                    {
                        string tagNameToSelect = drTagWanted[columnIndex: 0]
                            .ToString();
                        DataRow filteredRows = dtFileExif.Select(filterExpression: "TagName = '" + tagNameToSelect + "'")
                            .FirstOrDefault();
                        if (filteredRows != null)
                        {
                            tryDataValue = filteredRows[columnIndex: 1]
                                ?.ToString();
                            if (!string.IsNullOrEmpty(value: tryDataValue))
                            {
                                FrmMainApp.Logger.Trace(message: "dataPoint:" + dataPoint + " -> " + tagNameToSelect + ": " + tryDataValue);
                                break;
                            }
                        }
                    }
                    catch (ArgumentException)
                    {
                        tryDataValue = "-";
                    }
                }
            }
            else
            {
                tryDataValue = "-";
            }
        }

        FrmMainApp.Logger.Debug(message: "Done - dataPoint:" + dataPoint);
        returnVal = tryDataValue;
        return returnVal;
    }


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
        _sErrorMsg = "";
        _sOutputMsg = "";
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
                    .Where(predicate: file => AncillaryListsArrays.GpxExtensions()
                               .Any(predicate: file.ToLower()
                                        .EndsWith))
                    .ToList();
            }

            #region ExifToolConfiguration

            string exifToolExe = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe");

            string folderNameToUse = frmMainAppInstance.tbx_FolderName.Text;
            string exiftoolCmd = " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 ";

            List<string> exifArgs = new();
            // needs a space before and after.
            string commonArgs = " -a -s -s -struct -sort -G -ee -args ";

            #endregion

            // add track listOfAsyncCompatibleFileNamesWithOutPath
            foreach (string trackFile in trackFileList)
            {
                exiftoolCmd += " -geotag=" + SDoubleQuote + trackFile + SDoubleQuote;
            }

            // add what to compare against + TZ
            string tmpTZAdjust = SDoubleQuote;
            if (useTZAdjust)
            {
                tmpTZAdjust = TZVal + SDoubleQuote;
            }

            exiftoolCmd += " " + SDoubleQuote + "-geotime<${" + compareTZAgainst + "#}" + tmpTZAdjust;

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
                exiftoolCmd += " " + SDoubleQuote + pathToTagFile + SDoubleQuote;
            }

            // verbose logging
            exiftoolCmd += " -v2";

            // add output path to tmp xmp

            // make sure tmp exists -> this goes into "our" folder
            Directory.CreateDirectory(path: FrmMainApp.UserDataFolderPath + @"\tmpLocFiles");
            string tmpFolder = Path.Combine(FrmMainApp.UserDataFolderPath + @"\tmpLocFiles");

            // this is a little superflous but...
            DirectoryInfo diTmpLocFiles = new(path: tmpFolder);

            foreach (FileInfo file in diTmpLocFiles.EnumerateFiles())
            {
                file.Delete();
            }

            exiftoolCmd += " " + " -srcfile " + SDoubleQuote + tmpFolder + @"\%F.xmp" + SDoubleQuote;
            exiftoolCmd += " -overwrite_original_in_place";

            ///////////////
            await RunExifTool(exiftoolCmd: commonArgs + exiftoolCmd,
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
                            DataTable dt_fileExifTable = new();
                            dt_fileExifTable.Clear();
                            dt_fileExifTable.Columns.Add(columnName: "TagName");
                            dt_fileExifTable.Columns.Add(columnName: "TagValue");

                            PropertyInfo[] props = typeof(RDFDescription).GetProperties(bindingAttr: BindingFlags.Instance | BindingFlags.Public);

                            foreach (PropertyInfo trackData in props)
                            {
                                string TagName = "exif:" + trackData.Name;
                                object TagValue = trackData.GetValue(obj: trackFileXMPData.RDF.Description);

                                DataRow dr = dt_fileExifTable.NewRow();
                                dr[columnName: "TagName"] = TagName;
                                dr[columnName: "TagValue"] = TagValue;
                                dt_fileExifTable.Rows.Add(row: dr);
                            }

                            // de-dupe. this is pretty poor performance but the dataset is small
                            DataTable dt_distinctFileExifTable = dt_fileExifTable.DefaultView.ToTable(distinct: true);

                            ListView lvw = frmMainAppInstance.lvw_FileList;
                            ListViewItem lvi = frmMainAppInstance.lvw_FileList.FindItemWithText(text: exifFileIn.Name.Substring(startIndex: 0, length: exifFileIn.Name.Length - 4));

                            if (lvi != null)
                            {
                                ListView.ColumnHeaderCollection lvchs = frmMainAppInstance.ListViewColumnHeaders;
                                string[] toponomyChangers = { "GPSLatitude", "GPSLongitude" };
                                string[] toponomyDeletes = { "CountryCode", "Country", "City", "State", "Sub_location" };
                                string strParsedLat = "0.0";
                                string strParsedLng = "0.0";
                                bool coordinatesHaveChanged = false;
                                string fileNameWithoutPath = lvi.Text;

                                for (int i = 1; i < lvi.SubItems.Count; i++)
                                {
                                    string tagToWrite = lvchs[index: i]
                                        .Name.Substring(startIndex: 4);
                                    string str = ExifGetStandardisedDataPointFromExif(dtFileExif: dt_distinctFileExifTable, dataPoint: tagToWrite);
                                    FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);

                                    // don't update stuff that hasn't changed
                                    if (lvi.SubItems[index: i]
                                            .Text !=
                                        str &&
                                        (AncillaryListsArrays.GpxTagsToOverwrite()
                                             .Contains(value: tagToWrite) ||
                                         tagToWrite == "Coordinates"))
                                    {
                                        lvi.SubItems[index: i]
                                            .Text = str;
                                        if (AncillaryListsArrays.GpxTagsToOverwrite()
                                            .Contains(value: tagToWrite))
                                        {
                                            if (toponomyChangers.Contains(value: tagToWrite))
                                            {
                                                coordinatesHaveChanged = true;
                                            }

                                            GenericUpdateAddToDataTable(
                                                dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                                                fileNameWithoutPath: lvi.Text,
                                                settingId: lvchs[index: i]
                                                    .Name.Substring(startIndex: 4),
                                                settingValue: str
                                            );

                                            if (tagToWrite == "GPSLatitude")
                                            {
                                                strParsedLat = str;
                                            }

                                            if (tagToWrite == "GPSLongitude")
                                            {
                                                strParsedLng = str;
                                            }

                                            if (tagToWrite == "GPSAltitude")
                                            {
                                                CurrentAltitude = null;
                                                CurrentAltitude = str;
                                            }

                                            if (lvi.Index % 10 == 0)
                                            {
                                                Application.DoEvents();
                                                // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                                frmMainAppInstance.lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath);
                                            }

                                            frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: fileNameWithoutPath, color: Color.Red);
                                        }
                                    }
                                }

                                if (coordinatesHaveChanged)
                                {
                                    // clear city, state etc
                                    foreach (string category in toponomyDeletes)
                                    {
                                        GenericUpdateAddToDataTable(
                                            dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                                            fileNameWithoutPath: lvi.Text,
                                            settingId: category,
                                            settingValue: "-"
                                        );

                                        lvi.SubItems[index: lvw.Columns[key: "clh_" + category]
                                                         .Index]
                                            .Text = "-";
                                    }

                                    // pull from web
                                    if (!doNotReverseGeoCode)
                                    {
                                        SApiOkay = true;
                                        DataTable dtToponomy = DTFromAPIExifGetToponomyFromWebOrSQL(lat: strParsedLat.ToString(provider: CultureInfo.InvariantCulture), lng: strParsedLng.ToString(provider: CultureInfo.InvariantCulture));

                                        if (SApiOkay)
                                        {
                                            List<(string toponomyOverwriteName, string toponomyOverwriteVal)> toponomyOverwrites = new();
                                            toponomyOverwrites.Add(item: ("CountryCode", dtToponomy.Rows[index: 0][columnName: "CountryCode"]
                                                                              .ToString()));
                                            toponomyOverwrites.Add(item: ("Country", dtToponomy.Rows[index: 0][columnName: "Country"]
                                                                              .ToString()));
                                            toponomyOverwrites.Add(item: ("City", dtToponomy.Rows[index: 0][columnName: "City"]
                                                                              .ToString()));
                                            toponomyOverwrites.Add(item: ("State", dtToponomy.Rows[index: 0][columnName: "State"]
                                                                              .ToString()));
                                            toponomyOverwrites.Add(item: ("Sub_location", dtToponomy.Rows[index: 0][columnName: "Sub_location"]
                                                                              .ToString()));

                                            foreach ((string toponomyOverwriteName, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
                                            {
                                                GenericUpdateAddToDataTable(
                                                    dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                                                    fileNameWithoutPath: lvi.Text,
                                                    settingId: toponomyDetail.toponomyOverwriteName,
                                                    settingValue: toponomyDetail.toponomyOverwriteVal
                                                );
                                                lvi.SubItems[index: lvw.Columns[key: "clh_" + toponomyDetail.toponomyOverwriteName]
                                                                 .Index]
                                                    .Text = toponomyDetail.toponomyOverwriteVal;
                                            }

                                            if (lvi.Index % 10 == 0)
                                            {
                                                Application.DoEvents();
                                                // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                                frmMainAppInstance.lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath);
                                            }

                                            frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: fileNameWithoutPath, color: Color.Red);
                                        }
                                    }
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
            text: GenericGetMessageBoxText(messageBoxName: "mbx_FrmImportGpx_AskUserWantsReport"),
            caption: GenericGetMessageBoxCaption(captionType: "Question"),
            buttons: MessageBoxButtons.YesNo,
            icon: MessageBoxIcon.Question);
        if (dialogResult == DialogResult.Yes)
        {
            Form reportBox = new();

            reportBox.ControlBox = false;
            FlowLayoutPanel panel = new();

            TextBox tbxText = new();
            tbxText.Size = new Size(width: 700, height: 400);

            tbxText.Text = _sOutputMsg;
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


    /// <summary>
    ///     This generates (technically, extracts) the image previews from listOfAsyncCompatibleFileNamesWithOutPath for the
    ///     user when they click on a filename
    ///     ... in whichever listview.
    /// </summary>
    /// <param name="fileNameWithoutPath">Path of file for which the preview needs creating</param>
    /// <returns>Realistically nothing but the process generates the bitmap if possible</returns>
    internal static async Task ExifGetImagePreviews(string fileNameWithoutPath)
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        #region ExifToolConfiguration

        string exifToolExe = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe");

        // want to give this a different name from the usual exifArgs.args just in case that's still being accessed (as much as it shouldn't be)
        Regex rgx = new(pattern: "[^a-zA-Z0-9]");
        string fileNameReplaced = rgx.Replace(input: fileNameWithoutPath.Replace(oldValue: FrmMainApp.FolderName, newValue: ""), replacement: "_");
        string argsFile = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: "exifArgs_getPreview_" + fileNameReplaced + ".args");
        string exiftoolCmd = " -charset utf8 -charset filename=utf8 -b -preview:GTNPreview -w! " + SDoubleQuote + FrmMainApp.UserDataFolderPath + @"\%F.jpg" + SDoubleQuote + " -@ " + SDoubleQuote + argsFile + SDoubleQuote;

        File.Delete(path: argsFile);

        #endregion

        // add required tags

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        if (File.Exists(path: Path.Combine(path1: frmMainAppInstance.tbx_FolderName.Text, path2: fileNameWithoutPath)))
        {
            File.AppendAllText(path: argsFile, contents: Path.Combine(path1: frmMainAppInstance.tbx_FolderName.Text, path2: fileNameWithoutPath) + Environment.NewLine, encoding: Encoding.UTF8);
            File.AppendAllText(path: argsFile, contents: "-execute" + Environment.NewLine);
        }

        FrmMainApp.Logger.Trace(message: "Starting ExifTool");
        ///////////////
        await RunExifTool(exiftoolCmd: exiftoolCmd,
                          frmMainAppInstance: null,
                          initiator: "ExifGetImagePreviews");
        ///////////////
        FrmMainApp.Logger.Debug(message: "Done");
    }

    /// <summary>
    ///     Writes outstanding exif changes to files.
    /// </summary>
    /// <returns>Reastically nothing but writes the exif tags and updates the listview rows where necessary</returns>
    internal static async Task ExifWriteExifToFile()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        FilesAreBeingSaved = true;
        string argsFile = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: "exifArgsToWrite.args");
        string exiftoolCmd = " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8" + " -@ " + SDoubleQuote + argsFile + SDoubleQuote;

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        // if user switches folder in the process of writing this will keep it standard
        Debug.Assert(condition: frmMainAppInstance != null, message: nameof(frmMainAppInstance) + " != null");
        string folderNameToWrite = frmMainAppInstance.tbx_FolderName.Text;

        File.Delete(path: argsFile);

        bool processOriginalFile = false;
        bool resetFileDateToCreated = false;
        bool writeXMPSideCar = false;
        bool doNotCreateBackup = false;

        bool failWriteNothingEnabled = false;
        bool queueWasEmpty = true;

        // get tag names
        DataTable dtObjectTagNamesOut = GenericJoinDataTables(t1: FrmMainApp.DtObjectNames, t2: FrmMainApp.DtObjectTagNamesOut,
                                                              (row1,
                                                               row2) =>
                                                                  row1.Field<string>(columnName: "objectName") == row2.Field<string>(columnName: "objectName"));

        DataTable dtDistinctFileNames = FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.DefaultView.ToTable(distinct: true, "fileNameWithoutPath");

        // check there's anything to write.
        foreach (DataRow drFileName in dtDistinctFileNames.Rows)
        {
            FrmMainApp.Logger.Trace(message: drFileName.ToString());

            string fileNameWithoutPath = drFileName[columnIndex: 0]
                .ToString();
            string fileNameWithPath = Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath);
            if (File.Exists(path: fileNameWithPath))
            {
                string exifArgsForOriginalFile = "";
                string exifArgsForSidecar = "";
                string fileExtension = Path.GetExtension(path: fileNameWithoutPath)
                    .Substring(startIndex: 1);

                // this is a bug in Adobe Bridge (unsure). Rating in the XMP needs to be parsed and re-saved.
                int ratingInXmp = -1;
                string xmpFileLocation = Path.Combine(path1: folderNameToWrite, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath)) + ".xmp");
                if (File.Exists(path: xmpFileLocation))
                {
                    foreach (string line in File.ReadLines(path: xmpFileLocation))
                    {
                        if (line.Contains(value: "xmp:Rating="))
                        {
                            bool _ = int.TryParse(s: line.Replace(oldValue: "xmp:Rating=", newValue: "")
                                                      .Replace(oldValue: "\"", newValue: ""), result: out ratingInXmp);
                            break;
                        }

                        if (line.Contains(value: "<xmp:Rating>"))
                        {
                            bool _ = int.TryParse(s: line.Replace(oldValue: "<xmp:Rating>", newValue: "")
                                                      .Replace(oldValue: "</xmp:Rating>", newValue: "")
                                                      .Replace(oldValue: " ", newValue: ""), result: out ratingInXmp);
                            break;
                        }
                    }
                }

                queueWasEmpty = false;

                processOriginalFile = Convert.ToBoolean(value: DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_ProcessOriginalFile"));
                resetFileDateToCreated = Convert.ToBoolean(value: DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_ResetFileDateToCreated"));
                writeXMPSideCar = Convert.ToBoolean(value: DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_AddXMPSideCar"));
                doNotCreateBackup = Convert.ToBoolean(value: DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_OverwriteOriginal"));

                List<string> tagsToDelete = new(); // this needs to be injected into the sidecar if req'd

                // it's a lot less complicated to just pretend we want both the Original File and the Sidecar updated and then not-include them later than to have a Yggdrasil of IFs scattered all over.
                // ... which latter I would inevitable f...k up at some point.

                exifArgsForOriginalFile += Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath) + Environment.NewLine; //needs to include folder name
                exifArgsForOriginalFile += "-ignoreMinorErrors" + Environment.NewLine;
                exifArgsForOriginalFile += "-progress" + Environment.NewLine;

                // this doesn't need to be sent back to the actual XMP file, it's a bug.
                if (ratingInXmp >= 0)
                {
                    exifArgsForOriginalFile += "-Rating=" + ratingInXmp + Environment.NewLine;
                }

                exifArgsForSidecar += Path.Combine(path1: folderNameToWrite, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath)) + ".xmp") + Environment.NewLine; //needs to include folder name
                exifArgsForSidecar += "-progress" + Environment.NewLine;

                // sidecar copying needs to be in a separate batch, as technically it's a different file

                if (writeXMPSideCar)
                {
                    FrmMainApp.Logger.Trace(message: drFileName + " - writeXMPSideCar - " + writeXMPSideCar);

                    if (!File.Exists(path: xmpFileLocation))
                    {
                        FrmMainApp.Logger.Trace(message: drFileName + " - writeXMPSideCar - " + writeXMPSideCar + " - File has been created.");

                        // otherwise create a new one. 
                        xmpFileLocation = Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath);
                        exifArgsForSidecar += "-tagsfromfile=" + xmpFileLocation + Environment.NewLine;
                    }
                }

                exifArgsForSidecar += "-ignoreMinorErrors" + Environment.NewLine;

                DataTable dtFileWriteQueue;
                try
                {
                    dtFileWriteQueue = FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Select(filterExpression: "fileNameWithoutPath = '" + fileNameWithoutPath + "'")
                        .CopyToDataTable();
                }
                catch
                {
                    dtFileWriteQueue = null;
                }

                if (dtFileWriteQueue != null && dtFileWriteQueue.Rows.Count > 0)
                {
                    // get tags for this file

                    DataTable dtObjectTagNamesOutWithData = GenericJoinDataTables(t1: dtObjectTagNamesOut, t2: dtFileWriteQueue,
                                                                                  (row1,
                                                                                   row2) =>
                                                                                      row1.Field<string>(columnName: "objectName") == row2.Field<string>(columnName: "settingId"));

                    string exiftoolTagName;
                    string updateExifVal;

                    bool deleteAllGPSData = false;
                    bool deleteTagAlreadyAdded = false;

                    // add tags to argsFile
                    foreach (DataRow dataRow in dtObjectTagNamesOutWithData.Rows)
                    {
                        string settingId = dataRow[columnName: "settingId"]
                            .ToString();
                        string settingValue = dataRow[columnName: "settingValue"]
                            .ToString();

                        FrmMainApp.Logger.Trace(message: drFileName + " - " + settingId + ": " + settingValue);

                        // this is prob not the best way to go around this....
                        foreach (DataRow drFileTags in dtFileWriteQueue.Rows)
                        {
                            string tmpSettingValue = drFileTags[columnName: "settingId"]
                                .ToString();
                            if (tmpSettingValue == @"gps*")
                            {
                                deleteAllGPSData = true;
                                break;
                            }
                        }

                        // non-xmp always
                        if (deleteAllGPSData && !deleteTagAlreadyAdded)
                        {
                            exifArgsForOriginalFile += "-gps*=" + Environment.NewLine;
                            exifArgsForSidecar += "-gps*=" + Environment.NewLine;
                            tagsToDelete.Add(item: "gps*");

                            // this is moved up/in here because the deletion of all gps has to come before just about anything else in case user wants to add (rather than delete) in more tags (later).

                            exifArgsForOriginalFile += "-xmp:gps*=" + Environment.NewLine;
                            exifArgsForSidecar += "-xmp:gps*=" + Environment.NewLine;

                            deleteTagAlreadyAdded = true;
                        }

                        string objectTagNameOut = dataRow[columnName: "objectTagName_Out"]
                            .ToString();
                        exiftoolTagName = dataRow[columnName: "objectTagName_Out"]
                            .ToString();
                        updateExifVal = dataRow[columnName: "settingValue"]
                            .ToString();

                        if (!objectTagNameOut.Contains(value: ":"))
                        {
                            if (updateExifVal != "")
                            {
                                exifArgsForOriginalFile += "-" + exiftoolTagName + "=" + updateExifVal + Environment.NewLine;
                                exifArgsForSidecar += "-" + exiftoolTagName + "=" + updateExifVal + Environment.NewLine;

                                //if lat/long then add Ref. 
                                if (exiftoolTagName == "GPSLatitude" ||
                                    exiftoolTagName == "GPSDestLatitude" ||
                                    exiftoolTagName == "exif:GPSLatitude" ||
                                    exiftoolTagName == "exif:GPSDestLatitude")
                                {
                                    if (updateExifVal.Substring(startIndex: 0, length: 1) == "-")
                                    {
                                        exifArgsForOriginalFile += "-" + exiftoolTagName + "Ref" + "=" + "South" + Environment.NewLine;
                                        exifArgsForSidecar += "-" + exiftoolTagName + "Ref" + "=" + "South" + Environment.NewLine;
                                    }
                                    else
                                    {
                                        exifArgsForOriginalFile += "-" + exiftoolTagName + "Ref" + "=" + "North" + Environment.NewLine;
                                        exifArgsForSidecar += "-" + exiftoolTagName + "Ref" + "=" + "North" + Environment.NewLine;
                                    }
                                }
                                else if (exiftoolTagName == "GPSLongitude" ||
                                         exiftoolTagName == "GPSDestLongitude" ||
                                         exiftoolTagName == "exif:GPSLongitude" ||
                                         exiftoolTagName == "exif:GPSDestLongitude")
                                {
                                    if (updateExifVal.Substring(startIndex: 0, length: 1) == "-")
                                    {
                                        exifArgsForOriginalFile += "-" + exiftoolTagName + "Ref" + "=" + "West" + Environment.NewLine;
                                        exifArgsForSidecar += "-" + exiftoolTagName + "Ref" + "=" + "West" + Environment.NewLine;
                                    }
                                    else
                                    {
                                        exifArgsForOriginalFile += "-" + exiftoolTagName + "Ref" + "=" + "East" + Environment.NewLine;
                                        exifArgsForSidecar += "-" + exiftoolTagName + "Ref" + "=" + "East" + Environment.NewLine;
                                    }
                                }
                            }
                            else //delete tag
                            {
                                exifArgsForOriginalFile += "-" + exiftoolTagName + "=" + Environment.NewLine;
                                exifArgsForSidecar += "-" + exiftoolTagName + "=" + Environment.NewLine;
                                tagsToDelete.Add(item: exiftoolTagName);

                                //if lat/long then add Ref. 
                                if (
                                    exiftoolTagName == "GPSLatitude" ||
                                    exiftoolTagName == "GPSDestLatitude" ||
                                    exiftoolTagName == "exif:GPSLatitude" ||
                                    exiftoolTagName == "exif:GPSDestLatitude" ||
                                    exiftoolTagName == "GPSLongitude" ||
                                    exiftoolTagName == "GPSDestLongitude" ||
                                    exiftoolTagName == "exif:GPSLongitude" ||
                                    exiftoolTagName == "exif:GPSDestLongitude"
                                )
                                {
                                    exifArgsForOriginalFile += "-" + exiftoolTagName + "Ref" + "=" + Environment.NewLine;
                                    exifArgsForSidecar += "-" + exiftoolTagName + "Ref" + "=" + Environment.NewLine;
                                    tagsToDelete.Add(item: exiftoolTagName + "Ref");
                                }
                            }
                        }
                        else
                        {
                            if (objectTagNameOut == "EXIF:DateTimeOriginal" || // TakenDate
                                objectTagNameOut == "EXIF:CreateDate" || // CreateDate
                                objectTagNameOut == "XMP:DateTimeOriginal" || // TakenDate
                                objectTagNameOut == "XMP:CreateDate" // CreateDate
                               )
                            {
                                bool isTakenDate = false;
                                bool isCreateDate = false;
                                if (objectTagNameOut == "EXIF:DateTimeOriginal" || objectTagNameOut == "XMP:DateTimeOriginal")
                                {
                                    isTakenDate = true;
                                }
                                else if (objectTagNameOut == "EXIF:CreateDate" || objectTagNameOut == "XMP:CreateDate")
                                {
                                    isCreateDate = true;
                                }

                                try
                                {
                                    updateExifVal = DateTime.Parse(s: settingValue)
                                        .ToString(format: "yyyy-MM-dd HH:mm:ss");
                                }
                                catch
                                {
                                    updateExifVal = "";
                                }

                                if (isCreateDate)
                                {
                                    // update FrmMainApp.DtOriginalCreateDate -- there should be only 1 row
                                    for (int i = FrmMainApp.DtOriginalCreateDate.Rows.Count - 1; i >= 0; i--)
                                    {
                                        DataRow dr = FrmMainApp.DtOriginalCreateDate.Rows[index: i];
                                        if (dr[columnName: "fileNameWithoutPath"]
                                                .ToString() ==
                                            fileNameWithoutPath)
                                        {
                                            dr.Delete();
                                        }

                                        break;
                                    }

                                    FrmMainApp.DtOriginalCreateDate.AcceptChanges();
                                    if (updateExifVal != "")
                                    {
                                        DataRow drCreateDate = FrmMainApp.DtOriginalCreateDate.NewRow();
                                        drCreateDate[columnName: "fileNameWithoutPath"] = fileNameWithoutPath;
                                        drCreateDate[columnName: "settingId"] = "originalCreateDate";
                                        drCreateDate[columnName: "settingValue"] = GenericStringToDateTime(dateTimeToConvert: updateExifVal);

                                        FrmMainApp.DtOriginalCreateDate.Rows.Add(row: drCreateDate);
                                        FrmMainApp.DtOriginalCreateDate.AcceptChanges();
                                    }
                                }
                                else if (isTakenDate)
                                {
                                    // update FrmMainApp.DtOriginalTakenDate -- there should be only 1 row
                                    for (int i = FrmMainApp.DtOriginalTakenDate.Rows.Count - 1; i >= 0; i--)
                                    {
                                        DataRow dr = FrmMainApp.DtOriginalTakenDate.Rows[index: i];
                                        if (dr[columnName: "fileNameWithoutPath"]
                                                .ToString() ==
                                            fileNameWithoutPath)
                                        {
                                            dr.Delete();
                                        }

                                        break;
                                    }

                                    FrmMainApp.DtOriginalTakenDate.AcceptChanges();
                                    if (updateExifVal != "")
                                    {
                                        DataRow drTakenDate = FrmMainApp.DtOriginalTakenDate.NewRow();
                                        drTakenDate[columnName: "fileNameWithoutPath"] = fileNameWithoutPath;
                                        drTakenDate[columnName: "settingId"] = "originalTakenDate";
                                        drTakenDate[columnName: "settingValue"] = GenericStringToDateTime(dateTimeToConvert: updateExifVal);

                                        FrmMainApp.DtOriginalTakenDate.Rows.Add(row: drTakenDate);
                                        FrmMainApp.DtOriginalTakenDate.AcceptChanges();
                                    }
                                }
                            }

                            FrmMainApp.Logger.Trace(message: drFileName + " - " + exiftoolTagName + ": " + updateExifVal);

                            exifArgsForOriginalFile += "-" + exiftoolTagName + "=" + updateExifVal + Environment.NewLine;
                            exifArgsForSidecar += "-" + exiftoolTagName + "=" + updateExifVal + Environment.NewLine;
                        }
                    }
                }

                if (doNotCreateBackup)
                {
                    exifArgsForOriginalFile += "-overwrite_original_in_place" + Environment.NewLine;
                }

                exifArgsForOriginalFile += "-iptc:codedcharacterset=utf8" + Environment.NewLine;

                if (resetFileDateToCreated)
                {
                    exifArgsForOriginalFile += "-filemodifydate<datetimeoriginal" + Environment.NewLine;
                    exifArgsForOriginalFile += "-filecreatedate<datetimeoriginal" + Environment.NewLine;
                }

                exifArgsForOriginalFile += "-IPTCDigest=" + Environment.NewLine;

                exifArgsForOriginalFile += "-execute" + Environment.NewLine;

                if (processOriginalFile)
                {
                    File.AppendAllText(path: argsFile, contents: exifArgsForOriginalFile, encoding: Encoding.UTF8);
                }

                if (writeXMPSideCar)
                {
                    if (doNotCreateBackup)
                    {
                        //exifArgsForSidecar += "-IPTCDigest=" + Environment.NewLine;
                        exifArgsForSidecar += "-overwrite_original_in_place" + Environment.NewLine;
                    }

                    exifArgsForSidecar += "-execute" + Environment.NewLine;
                    File.AppendAllText(path: argsFile, contents: exifArgsForSidecar, encoding: Encoding.UTF8);
                }

                if (!processOriginalFile && !writeXMPSideCar)
                {
                    failWriteNothingEnabled = true;
                    FrmMainApp.Logger.Info(message: "Both file-writes disabled. Nothing Written.");
                    MessageBox.Show(
                        text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNoWriteSettingEnabled"),
                        caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                        buttons: MessageBoxButtons.OK,
                        icon: MessageBoxIcon.Warning);
                }
            }
        }

        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();

        if (!failWriteNothingEnabled && !queueWasEmpty)
        {
            FrmMainApp.Logger.Info(message: "Starting ExifTool.");
            ///////////////

            ;
            await RunExifTool(exiftoolCmd: exiftoolCmd,
                              frmMainAppInstance: frmMainAppInstance,
                              initiator: "ExifWriteExifToFile");
        }
        else if (!queueWasEmpty)
        {
            FrmMainApp.Logger.Info(message: "Both file-writes disabled. Nothing Written.");
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNoWriteSettingEnabled"),
                            caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }
        else
        {
            FrmMainApp.Logger.Info(message: "Queue was empty. Nothing Written.");
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNothingInWriteQueue"),
                            caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }

        ///////////////
        FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Ready.");
        FilesAreBeingSaved = false;
    }

    /// <summary>
    ///     This is a unified method for calling ExifTool. Any special "dealings" are done inside the message handling.
    ///     (possibly the wrong spot to do it)
    /// </summary>
    /// <param name="exiftoolCmd">Variables that need to be passed to ET</param>
    /// <param name="frmMainAppInstance">
    ///     Only relevant when called from ExifWriteExifToFile -> used to update the values/colour
    ///     in the main listView
    /// </param>
    /// <param name="initiator">String value of "who called it". </param>
    /// <returns>Empty Task</returns>
    private static async Task RunExifTool(string exiftoolCmd,
                                          FrmMainApp frmMainAppInstance,
                                          string initiator)
    {
        int lviIndex = 0;
        _exifInvokeCounter += 1;
        FrmMainApp.Logger.Trace(message: "Start EXIF Tool number " + _exifInvokeCounter + " for " + initiator + " with cmdLine: " + exiftoolCmd);
        await Task.Run(action: () =>
        {
            using Process prcExifTool = new();

            prcExifTool.StartInfo = new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
            {
                Arguments = @"/c " + SDoubleQuote + SDoubleQuote + Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe") + SDoubleQuote + " " + exiftoolCmd + SDoubleQuote,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            prcExifTool.EnableRaisingEvents = true;

            switch (initiator)
            {
                case "ExifWriteExifToFile":
                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (data.Data != null && data.Data.Contains(value: "="))
                        {
                            string fileNameWithoutPath = data.Data.Replace(oldValue: "=", newValue: "")
                                .Split('[')
                                .FirstOrDefault()
                                .Trim()
                                .Split('/')
                                .Last();
                            FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);
                            FrmMainApp.Logger.Debug(message: "Writing " + fileNameWithoutPath + " [this is via OutputDataReceived]");
                            try
                            {
                                if (lviIndex % 10 == 0)
                                {
                                    Application.DoEvents();

                                    // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                    frmMainAppInstance.lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath);
                                }

                                frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: fileNameWithoutPath, color: Color.Black);

                                if (Path.GetExtension(path: fileNameWithoutPath) == ".xmp")
                                {
                                    // problem is that if only the xmp file gets overwritten then there is no indication of the original file here. 
                                    // FindItemWithText -> https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.listview.finditemwithtext?view=netframework-4.8
                                    // "Finds the first ListViewItem with __that begins with__ the given text value."

                                    if (lviIndex % 10 == 0)
                                    {
                                        Application.DoEvents();
                                        // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                        frmMainAppInstance.lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath); // this is redundant here.
                                    }

                                    frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: Path.GetFileNameWithoutExtension(path: fileNameWithoutPath), color: Color.Black);
                                }
                            }
                            catch
                            {
                                // ignored
                            }

                            lviIndex++;
                        }
                        else if (data.Data != null && !data.Data.Contains(value: "files updated") && !data.Data.Contains(value: "files created") && data.Data.Length > 0)
                        {
                            MessageBox.Show(text: data.Data);
                        }
                    };

                    prcExifTool.ErrorDataReceived += (_,
                                                      data) =>
                    {
                        if (data.Data != null && data.Data.Contains(value: "="))
                        {
                            string fileNameWithoutPath = data.Data.Replace(oldValue: "=", newValue: "")
                                .Split('[')
                                .FirstOrDefault()
                                .Trim()
                                .Split('/')
                                .Last();
                            FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);
                            FrmMainApp.Logger.Debug(message: "Writing " + fileNameWithoutPath + " [this is via ErrorDataReceived]");

                            try
                            {
                                {
                                    Application.DoEvents();
                                    // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                    frmMainAppInstance.lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath);
                                }

                                frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: fileNameWithoutPath, color: Color.Black);

                                if (Path.GetExtension(path: fileNameWithoutPath) == ".xmp")
                                {
                                    // problem is that if only the xmp file gets overwritten then there is no indication of the original file here. 
                                    // FindItemWithText -> https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.listview.finditemwithtext?view=netframework-4.8
                                    // "Finds the first ListViewItem with __that begins with__ the given text value."
                                    if (lviIndex % 10 == 0)
                                    {
                                        Application.DoEvents();
                                        // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                        frmMainAppInstance.lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath); // this is redundant here
                                    }

                                    frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: Path.GetFileNameWithoutExtension(path: fileNameWithoutPath), color: Color.Black);
                                }
                            }
                            catch
                            {
                                // ignored
                            }

                            lviIndex++;
                        }
                        else if (data.Data != null && !data.Data.Contains(value: "files updated") && data.Data.Length > 0)
                        {
                            MessageBox.Show(text: data.Data);
                        }
                    };
                    break;
                case "GenericCheckForNewVersions":
                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (data.Data != null && data.Data.Length > 0)
                        {
                            _sOutputMsg += data.Data.ToString() + Environment.NewLine;
                        }

                        decimal.TryParse(s: _sOutputMsg.Replace(oldValue: "\r", newValue: "")
                                             .Replace(oldValue: "\n", newValue: ""),
                                         provider: CultureInfo.InvariantCulture,
                                         style: NumberStyles.Any,
                                         result: out _currentExifToolVersionLocal
                        );
                    };

                    break;
                case "ExifGetTrackSyncData":
                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (data.Data != null && data.Data.Length > 0)
                        {
                            _sOutputMsg += data.Data.ToString() + Environment.NewLine;
                        }
                    };
                    break;
                default:
                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (data.Data != null && data.Data.Length > 0)
                        {
                            _sOutputMsg += data.Data.ToString() + Environment.NewLine;
                        }
                    };

                    prcExifTool.ErrorDataReceived += (_,
                                                      data) =>
                    {
                        if (data.Data != null && data.Data.Length > 0)
                        {
                            _sOutputMsg += "ERROR: " + data.Data.ToString() + Environment.NewLine;
                        }
                    };
                    break;
            }

            FrmMainApp.Logger.Trace(message: "EXIF number " + _exifInvokeCounter + ": Start");
            prcExifTool.Start();
            prcExifTool.BeginOutputReadLine();
            prcExifTool.BeginErrorReadLine();
            FrmMainApp.Logger.Trace(message: "EXIF number " + _exifInvokeCounter + ": Wait for Exit");
            prcExifTool.WaitForExit();
            FrmMainApp.Logger.Trace(message: "EXIF number " + _exifInvokeCounter + ": Close");
            prcExifTool.Close();
            FrmMainApp.Logger.Trace(message: "Closing exifTool");

            // if still here then exorcise
            try
            {
                FrmMainApp.Logger.Trace(message: "Killing exifTool");
                prcExifTool.Kill();
            }
            catch
            {
                // "funnily" enough this seems to persist for some reason. Unsure why.
                FrmMainApp.Logger.Error(message: "Killing exifTool failed");
            }
        });
    }


    /// <summary>
    ///     Responsible for pulling the toponomy response for the API
    /// </summary>
    /// <param name="latitude">As on the tin.</param>
    /// <param name="longitude">As on the tin.</param>
    /// <returns>Structured toponomy response</returns>
    private static GeoResponseToponomy API_ExifGetGeoDataFromWebToponomy(string latitude,
                                                                         string longitude,
                                                                         string radius)
    {
        if (SGeoNamesUserName == null)
        {
            try
            {
                SGeoNamesUserName = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                SGeoNamesPwd = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorCantReadDefaultSQLiteDB") +
                          ex.Message,
                    caption: GenericGetMessageBoxCaption(captionType: "Error"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error);
            }
        }

        GeoResponseToponomy returnVal = new();
        RestClient client = new(baseUrl: "http://api.geonames.org/")
        {
            Authenticator = new HttpBasicAuthenticator(username: SGeoNamesUserName, password: SGeoNamesPwd)
        };

        RestRequest requestToponomy = new(resource: "findNearbyPlaceNameJSON?lat=" + latitude + "&lng=" + longitude + "&lang=" + APILanguageToUse + "&style=FULL&radius=" + radius + "&maxRows=" + ToponomyMaxRows);
        RestResponse responseToponomy = client.ExecuteGet(request: requestToponomy);
        // check API reponse is OK
        if (responseToponomy.Content != null && responseToponomy.Content.Contains(value: "the hourly limit of "))
        {
            SApiOkay = false;
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") +
                                  responseToponomy.Content,
                            caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }
        else if (responseToponomy.StatusCode.ToString() == "OK")
        {
            SApiOkay = true;
            JObject data = (JObject)JsonConvert.DeserializeObject(value: responseToponomy.Content);
            GeoResponseToponomy geoResponseToponomy = GeoResponseToponomy.FromJson(Json: data.ToString());
            returnVal = geoResponseToponomy;
        }
        else
        {
            SApiOkay = false;
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") +
                                  responseToponomy.StatusCode,
                            caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }

        return returnVal;
    }

    /// <summary>
    ///     Responsible for pulling the TZ response for the API
    /// </summary>
    /// <param name="latitude">As on the tin.</param>
    /// <param name="longitude">As on the tin.</param>
    /// <returns>Structured TZ response</returns>
    private static GeoResponseTimeZone API_ExifGetGeoDataFromWebTimeZone(string latitude,
                                                                         string longitude)
    {
        if (SGeoNamesUserName == null)
        {
            try
            {
                SGeoNamesUserName = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                SGeoNamesPwd = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorCantReadDefaultSQLiteDB") +
                          ex.Message,
                    caption: GenericGetMessageBoxCaption(captionType: "Error"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error);
            }
        }

        GeoResponseTimeZone returnVal = new();
        RestClient client = new(baseUrl: "http://api.geonames.org/")
        {
            Authenticator = new HttpBasicAuthenticator(username: SGeoNamesUserName, password: SGeoNamesPwd)
        };

        RestRequest request_TimeZone = new(resource: "timezoneJSON?lat=" + latitude + "&lng=" + longitude);
        RestResponse response_TimeZone = client.ExecuteGet(request: request_TimeZone);
        // check API reponse is OK
        if (response_TimeZone.Content != null && response_TimeZone.Content.Contains(value: "the hourly limit of "))
        {
            SApiOkay = false;
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") +
                                  response_TimeZone.Content,
                            caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }
        else if (response_TimeZone.StatusCode.ToString() == "OK")
        {
            SApiOkay = true;
            JObject data = (JObject)JsonConvert.DeserializeObject(value: response_TimeZone.Content);
            GeoResponseTimeZone geoResponseTimeZone = GeoResponseTimeZone.FromJson(Json: data.ToString());
            returnVal = geoResponseTimeZone;
        }
        else
        {
            SApiOkay = false;
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") +
                                  response_TimeZone.StatusCode,
                            caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }

        return returnVal;
    }


    /// <summary>
    ///     Responsible for pulling the latest prod-version number of exifTool from exiftool.org
    /// </summary>
    /// <returns>The version number of the currently newest exifTool uploaded to exiftool.org</returns>
    internal static decimal API_ExifGetExifToolVersionFromWeb()
    {
        decimal returnVal = 0.0m;
        decimal parsedDecimal = 0.0m;
        string onlineExifToolVer;

        bool parsedResult;
        try
        {
            onlineExifToolVer = new WebClient().DownloadString(address: "http://exiftool.org/ver.txt");
            parsedResult = decimal.TryParse(s: onlineExifToolVer, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedDecimal);
            if (parsedResult)
            {
                returnVal = parsedDecimal;
            }
            else
            {
                returnVal = 0.0m;
            }
        }
        catch
        {
            returnVal = 0.0m;
        }

        return returnVal;
    }

    /// <summary>
    ///     Responsible for pulling the latest release of GTN from gitHub
    /// </summary>
    /// <returns>The version number of the latest GTN release</returns>
    private static GtnReleasesApiResponse API_GenericGetGTNVersionFromWeb()
    {
        GtnReleasesApiResponse returnVal = new();
        RestClient client = new(baseUrl: "https://api.github.com/")
        {
            // admittedly no idea how to do this w/o any auth (as it's not needed) but this appears to work.
            Authenticator = new HttpBasicAuthenticator(username: "demo", password: "demo")
        };
        RestRequest request_GTNVersionQuery = new(resource: "repos/nemethviktor/GeoTagNinja/releases");
        RestResponse response_GTNVersionQuery = client.ExecuteGet(request: request_GTNVersionQuery);
        if (response_GTNVersionQuery.StatusCode.ToString() == "OK")
        {
            SApiOkay = true;
            JArray data = (JArray)JsonConvert.DeserializeObject(value: response_GTNVersionQuery.Content);
            GtnReleasesApiResponse[] gtnReleasesApiResponse = GtnReleasesApiResponse.FromJson(json: data.ToString());
            returnVal = gtnReleasesApiResponse[0]; // latest only
        }
        else
        {
            SApiOkay = false;
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGTNVerAPIResponse") +
                                  response_GTNVersionQuery.StatusCode,
                            caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }

        return returnVal;
    }

    /// <summary>
    ///     Performs a search in the local SQLite database for cached toponomy info and if finds it, returns that, else queries
    ///     the API
    /// </summary>
    /// <param name="lat">latitude/longitude to be queried</param>
    /// <param name="lng">latitude/longitude to be queried</param>
    /// <returns>
    ///     See summary. Returns the toponomy info either from SQLite if available or the API in DataTable for further
    ///     processing
    /// </returns>
    internal static DataTable DTFromAPIExifGetToponomyFromWebOrSQL(string lat,
                                                                   string lng)
    {
        DataTable dtReturn = new();
        dtReturn.Clear();
        dtReturn.Columns.Add(columnName: "Distance"); // this won't actually be used for data purposes.
        dtReturn.Columns.Add(columnName: "CountryCode");
        dtReturn.Columns.Add(columnName: "Country");
        dtReturn.Columns.Add(columnName: "City");
        dtReturn.Columns.Add(columnName: "State");
        dtReturn.Columns.Add(columnName: "Sub_location");
        dtReturn.Columns.Add(columnName: "GPSAltitude");
        dtReturn.Columns.Add(columnName: "timezoneId");

        EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in FrmMainApp.DtToponomySessionData.AsEnumerable()
                                                           where dataRow.Field<string>(columnName: "lat") == lat && dataRow.Field<string>(columnName: "lng") == lng
                                                           select dataRow;

        List<DataRow> lstReturnToponomyData = drDataTableData.ToList();

        GeoResponseToponomy readJsonToponomy;

        string? Distance = "";
        string? CountryCode = "";
        string? Country = "";
        string? City = "";
        string? State = "";
        string? Sub_location = "";
        string? Altitude = "0";
        string? timezoneId = "";

        bool includePredeterminedCountries = DataReadCheckBoxSettingTrueOrFalse(
            tableName: "settings",
            settingTabPage: "tpg_CustomRules",
            settingId: "ckb_IncludePredeterminedCountries"
        );

        bool stopProcessingRules = DataReadCheckBoxSettingTrueOrFalse(
            tableName: "settings",
            settingTabPage: "tpg_CustomRules",
            settingId: "ckb_StopProcessingRules"
        );

        // As per https://github.com/nemethviktor/GeoTagNinja/issues/38#issuecomment-1356844255 (see below comment a few lines down)

        // read from SQL
        if (lstReturnToponomyData.Count > 0)
        {
            bool isPredeterminedCountry = false;

            CountryCode = lstReturnToponomyData[index: 0][columnName: "CountryCode"]
                .ToString();
            Country = DataReadDTCountryCodesNames(
                    queryWhat: "ISO_3166_1A3",
                    inputVal: CountryCode,
                    returnWhat: "Country")
                ;

            Altitude = lstReturnToponomyData[index: 0][columnName: "GPSAltitude"]
                .ToString();

            timezoneId = lstReturnToponomyData[index: 0][columnName: "timezoneId"]
                .ToString();

            string? AdminName1InSQL = lstReturnToponomyData[index: 0][columnName: "AdminName1"]
                .ToString();
            string? AdminName2InSQL = lstReturnToponomyData[index: 0][columnName: "AdminName2"]
                .ToString();
            string? AdminName3InSQL = lstReturnToponomyData[index: 0][columnName: "AdminName3"]
                .ToString();
            string? AdminName4InSQL = lstReturnToponomyData[index: 0][columnName: "AdminName4"]
                .ToString();
            string? ToponymNameInSQL = lstReturnToponomyData[index: 0][columnName: "ToponymName"]
                .ToString();

            // In a country where you know, which admin level the cities belong to (see arrays), use the adminNameX as city name.
            // If the toponymName doesn't match the adminNameX, use the toponymName as sublocation name. toponymNames ...
            // ... for populated places may be city names or names of some populated entity below city level, but they're never used for something above city level.
            // In a country where city names are not assigned to a specific admin level, I'd use the toponymName as the city name and leave the sublocation name blank.

            if (AncillaryListsArrays.CityNameIsAdminName1Arr.Contains(value: CountryCode) ||
                AncillaryListsArrays.CityNameIsAdminName2Arr.Contains(value: CountryCode) ||
                AncillaryListsArrays.CityNameIsAdminName3Arr.Contains(value: CountryCode) ||
                AncillaryListsArrays.CityNameIsAdminName4Arr.Contains(value: CountryCode)
               )
            {
                isPredeterminedCountry = true;

                Sub_location = ToponymNameInSQL;

                if (AncillaryListsArrays.CityNameIsAdminName1Arr.Contains(value: CountryCode))
                {
                    City = AdminName1InSQL;
                    State = "";
                }
                else if (AncillaryListsArrays.CityNameIsAdminName2Arr.Contains(value: CountryCode))
                {
                    City = AdminName2InSQL;
                }
                else if (AncillaryListsArrays.CityNameIsAdminName3Arr.Contains(value: CountryCode))
                {
                    City = AdminName3InSQL;
                }
                else if (AncillaryListsArrays.CityNameIsAdminName4Arr.Contains(value: CountryCode))
                {
                    City = AdminName4InSQL;
                }

                if (City == Sub_location)
                {
                    Sub_location = "";
                }

                if (!AncillaryListsArrays.CityNameIsAdminName1Arr.Contains(value: CountryCode))
                {
                    State = AdminName1InSQL;
                }
            }

            if (!isPredeterminedCountry || includePredeterminedCountries)
            {
                bool customRuleChangedState = false;
                bool customRuleChangedCity = false;
                bool customRuleChangedSub_location = false;

                EnumerableRowCollection<DataRow> drCustomRulesData = from DataRow dataRow in FrmSettings.dtCustomRules.AsEnumerable()
                                                                     where dataRow.Field<string>(columnName: "CountryCode") == CountryCode
                                                                     select dataRow;

                if (drCustomRulesData.Any())
                {
                    foreach (DataRow dataRow in drCustomRulesData)
                    {
                        string DataPointName = dataRow[columnName: "DataPointName"]
                            .ToString();

                        string DataPointConditionType = dataRow[columnName: "DataPointConditionType"]
                            .ToString();

                        string DataPointValueInSQL = null;

                        switch (DataPointName)
                        {
                            case "AdminName1":
                                DataPointValueInSQL = AdminName1InSQL;
                                break;
                            case "AdminName2":
                                DataPointValueInSQL = AdminName2InSQL;
                                break;
                            case "AdminName3":
                                DataPointValueInSQL = AdminName3InSQL;
                                break;
                            case "AdminName4":
                                DataPointValueInSQL = AdminName4InSQL;
                                break;
                            case "ToponymName":
                                DataPointValueInSQL = ToponymNameInSQL;
                                break;
                        }

                        // don't bother if null
                        if (!string.IsNullOrEmpty(value: DataPointValueInSQL))
                        {
                            string? DataPointConditionValue = dataRow[columnName: "DataPointConditionValue"]
                                .ToString();
                            string? DataPointValueInSQLLC = DataPointValueInSQL.ToLower();
                            string? DataPointConditionValueLC = DataPointConditionValue.ToLower();
                            bool comparisonIsTrue = false;
                            switch (DataPointConditionType)
                            {
                                case "Is":
                                    if (DataPointValueInSQLLC == DataPointConditionValueLC)
                                    {
                                        comparisonIsTrue = true;
                                    }

                                    break;
                                case "Contains":
                                    if (DataPointValueInSQLLC.Contains(value: DataPointConditionValueLC))
                                    {
                                        comparisonIsTrue = true;
                                    }

                                    break;
                                case "StartsWith":
                                    if (DataPointValueInSQLLC.StartsWith(value: DataPointConditionValueLC))
                                    {
                                        comparisonIsTrue = true;
                                    }

                                    break;
                                case "EndsWith":
                                    if (DataPointValueInSQLLC.EndsWith(value: DataPointConditionValueLC))
                                    {
                                        comparisonIsTrue = true;
                                    }

                                    break;
                            }

                            if (comparisonIsTrue && ((stopProcessingRules && !customRuleChangedSub_location) || !stopProcessingRules))
                            {
                                string? TargetPointName = dataRow[columnName: "TargetPointName"]
                                    .ToString();
                                string? TargetPointOutcome = dataRow[columnName: "TargetPointOutcome"]
                                    .ToString();
                                string? TargetPointOutcomeCustom = dataRow[columnName: "TargetPointOutcomeCustom"]
                                    .ToString();

                                switch (TargetPointName)
                                {
                                    case "State":
                                        switch (TargetPointOutcome)
                                        {
                                            case "AdminName1":
                                                State = AdminName1InSQL;
                                                break;
                                            case "AdminName2":
                                                State = AdminName2InSQL;
                                                break;
                                            case "AdminName3":
                                                State = AdminName3InSQL;
                                                break;
                                            case "AdminName4":
                                                State = AdminName4InSQL;
                                                break;
                                            case "ToponymName":
                                                State = ToponymNameInSQL;
                                                break;
                                            case "Null (empty)":
                                                State = "";
                                                break;
                                            case "Custom":
                                                State = TargetPointOutcomeCustom;
                                                break;
                                        }

                                        customRuleChangedState = true;
                                        break;
                                    case "City":
                                        switch (TargetPointOutcome)
                                        {
                                            case "AdminName1":
                                                City = AdminName1InSQL;
                                                break;
                                            case "AdminName2":
                                                City = AdminName2InSQL;
                                                break;
                                            case "AdminName3":
                                                City = AdminName3InSQL;
                                                break;
                                            case "AdminName4":
                                                City = AdminName4InSQL;
                                                break;
                                            case "ToponymName":
                                                City = ToponymNameInSQL;
                                                break;
                                            case "Null (empty)":
                                                City = "";
                                                break;
                                            case "Custom":
                                                City = TargetPointOutcomeCustom;
                                                break;
                                        }

                                        customRuleChangedCity = true;
                                        break;
                                    case "Sub_location":
                                        switch (TargetPointOutcome)
                                        {
                                            //todo dontprocessmorerules
                                            case "AdminName1":
                                                Sub_location = AdminName1InSQL;
                                                break;
                                            case "AdminName2":
                                                Sub_location = AdminName2InSQL;
                                                break;
                                            case "AdminName3":
                                                Sub_location = AdminName3InSQL;
                                                break;
                                            case "AdminName4":
                                                Sub_location = AdminName4InSQL;
                                                break;
                                            case "ToponymName":
                                                Sub_location = ToponymNameInSQL;
                                                break;
                                            case "Null (empty)":
                                                Sub_location = "";
                                                break;
                                            case "Custom":
                                                Sub_location = TargetPointOutcomeCustom;
                                                break;
                                        }

                                        customRuleChangedSub_location = true;
                                        break;
                                }
                            }
                        }
                    }
                }

                if (!customRuleChangedState)
                {
                    State = AdminName1InSQL;
                }

                if (!customRuleChangedCity)
                {
                    City = ToponymNameInSQL;
                }

                if (!customRuleChangedSub_location)
                {
                    Sub_location = "";
                }
            }

            DataRow drReturnRow = dtReturn.NewRow();
            drReturnRow[columnName: "Distance"] = Distance;
            drReturnRow[columnName: "CountryCode"] = CountryCode;
            drReturnRow[columnName: "Country"] = Country;
            drReturnRow[columnName: "City"] = City;
            drReturnRow[columnName: "State"] = State;
            drReturnRow[columnName: "Sub_location"] = Sub_location;
            drReturnRow[columnName: "GPSAltitude"] = Altitude;
            drReturnRow[columnName: "timezoneId"] = timezoneId;

            dtReturn.Rows.Add(row: drReturnRow);
        }
        // read from API
        else if (SApiOkay)
        {
            bool isPredeterminedCountry = false;

            readJsonToponomy = API_ExifGetGeoDataFromWebToponomy(
                latitude: lat,
                longitude: lng,
                radius: ToponomyRadiusValue
            );

            // if that returns nothing then try again with something bigger.
            try
            {
                if (readJsonToponomy.Geonames != null)
                {
                    if (readJsonToponomy.Geonames.Length == 0)
                    {
                        readJsonToponomy = API_ExifGetGeoDataFromWebToponomy(
                            latitude: lat,
                            longitude: lng,
                            radius: "300"
                        );
                    }
                }
                else
                {
                    MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_HelperStaticExifNoAPI"),
                                    caption: GenericGetMessageBoxCaption(captionType: "Error"),
                                    buttons: MessageBoxButtons.OK,
                                    icon: MessageBoxIcon.Error);
                }
            }
            catch
            {
                MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_HelperStaticExifNoAPI"),
                                caption: GenericGetMessageBoxCaption(captionType: "Error"),
                                buttons: MessageBoxButtons.OK,
                                icon: MessageBoxIcon.Error);
            }

            // ignore if unauthorised or some such
            if (readJsonToponomy.Geonames != null)
            {
                if (readJsonToponomy.Geonames.Length > 0)
                {
                    // this is to pseudo-replicate the dtReturn table but for SQL, which has a different logic. (of course it does.)
                    DataTable dtWriteToSQLite = new();
                    dtWriteToSQLite.Clear();
                    dtWriteToSQLite.Columns.Add(columnName: "lat");
                    dtWriteToSQLite.Columns.Add(columnName: "lng");
                    dtWriteToSQLite.Columns.Add(columnName: "AdminName1");
                    dtWriteToSQLite.Columns.Add(columnName: "AdminName2");
                    dtWriteToSQLite.Columns.Add(columnName: "AdminName3");
                    dtWriteToSQLite.Columns.Add(columnName: "AdminName4");
                    dtWriteToSQLite.Columns.Add(columnName: "ToponymName");
                    dtWriteToSQLite.Columns.Add(columnName: "CountryCode");
                    dtWriteToSQLite.Columns.Add(columnName: "GPSAltitude");
                    dtWriteToSQLite.Columns.Add(columnName: "timezoneId");

                    for (int index = 0; index < readJsonToponomy.Geonames.Length; index++)
                    {
                        DataRow drApiToponomyRow = dtReturn.NewRow();
                        DataRow drWriteToSqLiteRow = dtWriteToSQLite.NewRow();

                        string APICountryCode = readJsonToponomy.Geonames[index]
                            .CountryCode;
                        if (APICountryCode.Length == 2)
                        {
                            CountryCode = DataReadDTCountryCodesNames(
                                queryWhat: "ISO_3166_1A2",
                                inputVal: APICountryCode,
                                returnWhat: "ISO_3166_1A3"
                            );
                            Country = DataReadDTCountryCodesNames(
                                queryWhat: "ISO_3166_1A2",
                                inputVal: APICountryCode,
                                returnWhat: "Country"
                            );
                        }

                        bool _ = double.TryParse(s: readJsonToponomy.Geonames[index]
                                                     .Srtm3.ToString(), result: out double tmpAlt);
                        try
                        {
                            // can return 32768 or -32768 in some cases. this is the API's "fault" (not that of the code.)
                            if (Math.Abs(value: tmpAlt) > 32000.0)
                            {
                                if (!string.IsNullOrEmpty(value: CurrentAltitude))
                                {
                                    _ = double.TryParse(s: CurrentAltitude, result: out tmpAlt);
                                }
                                else
                                {
                                    tmpAlt = 0.0;
                                }
                            }
                        }
                        catch
                        {
                            tmpAlt = 0.0;
                        }

                        Altitude = tmpAlt.ToString();

                        // this is already String.
                        timezoneId = readJsonToponomy.Geonames[index]
                            .Timezone.TimeZoneId;

                        Distance = readJsonToponomy.Geonames[index]
                            .Distance;

                        string? AdminName1InAPI = readJsonToponomy.Geonames[index]
                            .AdminName1;
                        string? AdminName2InAPI = readJsonToponomy.Geonames[index]
                            .AdminName2;
                        string? AdminName3InAPI = readJsonToponomy.Geonames[index]
                            .AdminName3;
                        string? AdminName4InAPI = readJsonToponomy.Geonames[index]
                            .AdminName4;
                        string? ToponymNameInAPI = readJsonToponomy.Geonames[index]
                            .ToponymName;

                        // Comments are copied from above.
                        // In a country where you know, which admin level the cities belong to (see arrays), use the adminNameX as city name.
                        // If the toponymName doesn't match the adminNameX, use the toponymName as sublocation name. toponymNames ...
                        // ... for populated places may be city names or names of some populated entity below city level, but they're never used for something above city level.
                        // In a country where city names are not assigned to a specific admin level, I'd use the toponymName as the city name and leave the sublocation name blank.

                        if (AncillaryListsArrays.CityNameIsAdminName1Arr.Contains(value: CountryCode) ||
                            AncillaryListsArrays.CityNameIsAdminName2Arr.Contains(value: CountryCode) ||
                            AncillaryListsArrays.CityNameIsAdminName3Arr.Contains(value: CountryCode) ||
                            AncillaryListsArrays.CityNameIsAdminName4Arr.Contains(value: CountryCode)
                           )
                        {
                            isPredeterminedCountry = true;

                            Sub_location = readJsonToponomy.Geonames[index]
                                .ToponymName;
                            if (AncillaryListsArrays.CityNameIsAdminName1Arr.Contains(value: CountryCode))
                            {
                                City = AdminName1InAPI;
                                State = "";
                            }
                            else if (AncillaryListsArrays.CityNameIsAdminName2Arr.Contains(value: CountryCode))
                            {
                                City = AdminName2InAPI;
                            }
                            else if (AncillaryListsArrays.CityNameIsAdminName3Arr.Contains(value: CountryCode))
                            {
                                City = AdminName3InAPI;
                            }
                            else if (AncillaryListsArrays.CityNameIsAdminName4Arr.Contains(value: CountryCode))
                            {
                                City = AdminName4InAPI;
                            }

                            if (City == Sub_location)
                            {
                                Sub_location = "";
                            }

                            if (!AncillaryListsArrays.CityNameIsAdminName1Arr.Contains(value: CountryCode))
                            {
                                State = AdminName1InAPI;
                            }
                        }

                        if (!isPredeterminedCountry || includePredeterminedCountries)
                        {
                            bool customRuleChangedState = false;
                            bool customRuleChangedCity = false;
                            bool customRuleChangedSub_location = false;

                            EnumerableRowCollection<DataRow> drCustomRulesData = from DataRow dataRow in FrmSettings.dtCustomRules.AsEnumerable()
                                                                                 where dataRow.Field<string>(columnName: "CountryCode") == CountryCode
                                                                                 select dataRow;

                            if (drCustomRulesData.Any())
                            {
                                foreach (DataRow dataRow in drCustomRulesData)
                                {
                                    string? DataPointName = dataRow[columnName: "DataPointName"]
                                        .ToString();

                                    string? DataPointConditionType = dataRow[columnName: "DataPointConditionType"]
                                        .ToString();

                                    string? DataPointValueInAPI = null;
                                    switch (DataPointName)
                                    {
                                        case "AdminName1":
                                            DataPointValueInAPI = AdminName1InAPI;
                                            break;
                                        case "AdminName2":
                                            DataPointValueInAPI = AdminName2InAPI;
                                            break;
                                        case "AdminName3":
                                            DataPointValueInAPI = AdminName3InAPI;
                                            break;
                                        case "AdminName4":
                                            DataPointValueInAPI = AdminName4InAPI;
                                            break;
                                        case "ToponymName":
                                            DataPointValueInAPI = ToponymNameInAPI;
                                            break;
                                    }

                                    // don't bother if null
                                    if (!string.IsNullOrEmpty(value: DataPointValueInAPI))
                                    {
                                        string? DataPointConditionValue = dataRow[columnName: "DataPointConditionValue"]
                                            .ToString();
                                        string? DataPointValueInAPILC = DataPointValueInAPI?.ToLower();
                                        string? DataPointConditionValueLC = DataPointConditionValue.ToLower();
                                        bool comparisonIsTrue = false;
                                        switch (DataPointConditionType)
                                        {
                                            case "Is":
                                                if (DataPointValueInAPILC == DataPointConditionValueLC)
                                                {
                                                    comparisonIsTrue = true;
                                                }

                                                break;
                                            case "Contains":
                                                if (DataPointValueInAPILC.Contains(value: DataPointConditionValueLC))
                                                {
                                                    comparisonIsTrue = true;
                                                }

                                                break;
                                            case "StartsWith":
                                                if (DataPointValueInAPILC.StartsWith(value: DataPointConditionValueLC))
                                                {
                                                    comparisonIsTrue = true;
                                                }

                                                break;
                                            case "EndsWith":
                                                if (DataPointValueInAPILC.EndsWith(value: DataPointConditionValueLC))
                                                {
                                                    comparisonIsTrue = true;
                                                }

                                                break;
                                        }

                                        if (comparisonIsTrue && ((stopProcessingRules && !customRuleChangedSub_location) || !stopProcessingRules))
                                        {
                                            string? TargetPointName = dataRow[columnName: "TargetPointName"]
                                                .ToString();
                                            string? TargetPointOutcome = dataRow[columnName: "TargetPointOutcome"]
                                                .ToString();
                                            string? TargetPointOutcomeCustom = dataRow[columnName: "TargetPointOutcomeCustom"]
                                                .ToString();

                                            switch (TargetPointName)
                                            {
                                                case "State":
                                                    switch (TargetPointOutcome)
                                                    {
                                                        case "AdminName1":
                                                            State = AdminName1InAPI;
                                                            break;
                                                        case "AdminName2":
                                                            State = AdminName2InAPI;
                                                            break;
                                                        case "AdminName3":
                                                            State = AdminName3InAPI;
                                                            break;
                                                        case "AdminName4":
                                                            State = AdminName4InAPI;
                                                            break;
                                                        case "ToponymName":
                                                            State = ToponymNameInAPI;
                                                            break;
                                                        case "Null (empty)":
                                                            State = "";
                                                            break;
                                                        case "Custom":
                                                            State = TargetPointOutcomeCustom;
                                                            break;
                                                    }

                                                    customRuleChangedState = true;
                                                    break;
                                                case "City":
                                                    switch (TargetPointOutcome)
                                                    {
                                                        case "AdminName1":
                                                            City = AdminName1InAPI;
                                                            break;
                                                        case "AdminName2":
                                                            City = AdminName2InAPI;
                                                            break;
                                                        case "AdminName3":
                                                            City = AdminName3InAPI;
                                                            break;
                                                        case "AdminName4":
                                                            City = AdminName4InAPI;
                                                            break;
                                                        case "ToponymName":
                                                            City = ToponymNameInAPI;
                                                            break;
                                                        case "Null (empty)":
                                                            City = "";
                                                            break;
                                                        case "Custom":
                                                            City = TargetPointOutcomeCustom;
                                                            break;
                                                    }

                                                    customRuleChangedCity = true;
                                                    break;
                                                case "Sub_location":
                                                    switch (TargetPointOutcome)
                                                    {
                                                        case "AdminName1":
                                                            Sub_location = AdminName1InAPI;
                                                            break;
                                                        case "AdminName2":
                                                            Sub_location = AdminName2InAPI;
                                                            break;
                                                        case "AdminName3":
                                                            Sub_location = AdminName3InAPI;
                                                            break;
                                                        case "AdminName4":
                                                            Sub_location = AdminName4InAPI;
                                                            break;
                                                        case "ToponymName":
                                                            Sub_location = ToponymNameInAPI;
                                                            break;
                                                        case "Null (empty)":
                                                            Sub_location = "";
                                                            break;
                                                        case "Custom":
                                                            Sub_location = TargetPointOutcomeCustom;
                                                            break;
                                                    }

                                                    customRuleChangedSub_location = true;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!customRuleChangedState)
                            {
                                State = AdminName1InAPI;
                            }

                            if (!customRuleChangedCity)
                            {
                                City = ToponymNameInAPI;
                            }

                            if (!customRuleChangedSub_location)
                            {
                                Sub_location = "";
                            }
                        }

                        // add to return-table to offer to user
                        drApiToponomyRow[columnName: "Distance"] = Distance;
                        drApiToponomyRow[columnName: "CountryCode"] = CountryCode;
                        drApiToponomyRow[columnName: "Country"] = Country;
                        drApiToponomyRow[columnName: "City"] = City;
                        drApiToponomyRow[columnName: "State"] = State;
                        drApiToponomyRow[columnName: "Sub_location"] = Sub_location;
                        drApiToponomyRow[columnName: "GPSAltitude"] = Altitude;
                        drApiToponomyRow[columnName: "timezoneId"] = timezoneId;

                        dtReturn.Rows.Add(row: drApiToponomyRow);

                        // write back the new stuff to sql

                        drWriteToSqLiteRow[columnName: "lat"] = lat;
                        drWriteToSqLiteRow[columnName: "lng"] = lng;
                        drWriteToSqLiteRow[columnName: "AdminName1"] = readJsonToponomy.Geonames[index]
                            .AdminName1;
                        drWriteToSqLiteRow[columnName: "AdminName2"] = readJsonToponomy.Geonames[index]
                            .AdminName2;
                        drWriteToSqLiteRow[columnName: "AdminName3"] = readJsonToponomy.Geonames[index]
                            .AdminName3;
                        drWriteToSqLiteRow[columnName: "AdminName4"] = readJsonToponomy.Geonames[index]
                            .AdminName4;
                        drWriteToSqLiteRow[columnName: "ToponymName"] = readJsonToponomy.Geonames[index]
                            .ToponymName;
                        drWriteToSqLiteRow[columnName: "CountryCode"] = CountryCode;
                        drWriteToSqLiteRow[columnName: "GPSAltitude"] = Altitude;
                        drWriteToSqLiteRow[columnName: "timezoneId"] = timezoneId;

                        dtWriteToSQLite.Rows.Add(row: drWriteToSqLiteRow);
                    }

                    if (dtReturn.Rows.Count == 1)
                    {
                        // not adding anything to dtReturn because it has 1 row, and that's the one that will be returned.

                        GenericUpdateAddToDataTableTopopnomy(
                            lat: dtWriteToSQLite.Rows[index: 0][columnName: "lat"]
                                .ToString(),
                            lng: dtWriteToSQLite.Rows[index: 0][columnName: "lng"]
                                .ToString(),
                            adminName1: dtWriteToSQLite.Rows[index: 0][columnName: "AdminName1"]
                                .ToString(),
                            adminName2: dtWriteToSQLite.Rows[index: 0][columnName: "AdminName2"]
                                .ToString(),
                            adminName3: dtWriteToSQLite.Rows[index: 0][columnName: "AdminName3"]
                                .ToString(),
                            adminName4: dtWriteToSQLite.Rows[index: 0][columnName: "AdminName4"]
                                .ToString(),
                            toponymName: dtWriteToSQLite.Rows[index: 0][columnName: "ToponymName"]
                                .ToString(),
                            countryCode: dtWriteToSQLite.Rows[index: 0][columnName: "CountryCode"]
                                .ToString(),
                            altitude: dtWriteToSQLite.Rows[index: 0][columnName: "GPSAltitude"]
                                .ToString(),
                            timezoneId: dtWriteToSQLite.Rows[index: 0][columnName: "timezoneId"]
                                .ToString()
                        );
                    }
                    else
                    {
                        int useDr = useDrRow(dtIn: dtReturn);
                        dtReturn = dtReturn.AsEnumerable()
                            .Where(predicate: (row,
                                               index) => index == useDr)
                            .CopyToDataTable();

                        dtWriteToSQLite = dtWriteToSQLite.AsEnumerable()
                            .Where(predicate: (row,
                                               index) => index == useDr)
                            .CopyToDataTable();

                        // [0] because we just killed off the other rows above.
                        GenericUpdateAddToDataTableTopopnomy(
                            lat: dtWriteToSQLite.Rows[index: 0][columnName: "lat"]
                                .ToString(),
                            lng: dtWriteToSQLite.Rows[index: 0][columnName: "lng"]
                                .ToString(),
                            adminName1: dtWriteToSQLite.Rows[index: 0][columnName: "AdminName1"]
                                .ToString(),
                            adminName2: dtWriteToSQLite.Rows[index: 0][columnName: "AdminName2"]
                                .ToString(),
                            adminName3: dtWriteToSQLite.Rows[index: 0][columnName: "AdminName3"]
                                .ToString(),
                            adminName4: dtWriteToSQLite.Rows[index: 0][columnName: "AdminName4"]
                                .ToString(),
                            toponymName: dtWriteToSQLite.Rows[index: 0][columnName: "ToponymName"]
                                .ToString(),
                            countryCode: dtWriteToSQLite.Rows[index: 0][columnName: "CountryCode"]
                                .ToString(),
                            altitude: dtWriteToSQLite.Rows[index: 0][columnName: "GPSAltitude"]
                                .ToString(),
                            timezoneId: dtWriteToSQLite.Rows[index: 0][columnName: "timezoneId"]
                                .ToString()
                        );

                        int useDrRow(DataTable dtIn)
                        {
                            Form pickRowBox = new();

                            pickRowBox.ControlBox = false;
                            FlowLayoutPanel panel = new();

                            ListView lvwDataChoices = new();
                            lvwDataChoices.Size = new Size(width: 800, height: 200);
                            lvwDataChoices.View = System.Windows.Forms.View.Details;
                            lvwDataChoices.Columns.Add(text: "Index");
                            foreach (DataColumn dc in dtIn.Columns)
                            {
                                lvwDataChoices.Columns.Add(text: dc.ColumnName, width: -2);
                            }

                            foreach (DataRow drItem in dtReturn.Rows)
                            {
                                ListViewItem lvi = new(text: dtReturn.Rows.IndexOf(row: drItem)
                                                           .ToString());
                                foreach (DataColumn dc in dtIn.Columns)
                                {
                                    lvi.SubItems.Add(text: drItem[column: dc]
                                                         .ToString());
                                }

                                lvwDataChoices.Items.Add(value: lvi);
                            }

                            lvwDataChoices.MultiSelect = false;
                            lvwDataChoices.FullRowSelect = true;

                            panel.Controls.Add(value: lvwDataChoices);
                            panel.SetFlowBreak(control: lvwDataChoices, value: true);

                            Button btnOk = new()
                                { Text = "OK" };
                            btnOk.Click += (sender,
                                            e) =>
                            {
                                pickRowBox.Close();
                            };
                            btnOk.Location = new Point(x: 10, y: lvwDataChoices.Bottom + 5);
                            btnOk.AutoSize = true;
                            panel.Controls.Add(value: btnOk);

                            panel.Padding = new Padding(all: 5);
                            panel.AutoSize = true;

                            pickRowBox.Controls.Add(value: panel);
                            pickRowBox.MinimumSize = new Size(width: lvwDataChoices.Width + 40, height: btnOk.Bottom + 50);
                            pickRowBox.ShowInTaskbar = false;

                            pickRowBox.StartPosition = FormStartPosition.CenterScreen;
                            pickRowBox.ShowDialog();

                            try
                            {
                                return lvwDataChoices.SelectedItems[index: 0]
                                    .Index;
                            }
                            catch
                            {
                                return 0;
                            }
                        }
                    }
                }
                else if (SApiOkay)
                {
                    // write back empty
                    GenericUpdateAddToDataTableTopopnomy(
                        lat: lat,
                        lng: lng,
                        adminName1: "",
                        adminName2: "",
                        adminName3: "",
                        adminName4: "",
                        toponymName: "",
                        countryCode: "",
                        altitude: "",
                        timezoneId: ""
                    );
                }
            }
        }

        return dtReturn;
    }


    /// <summary>
    ///     Converts the API response from gitHub (to check GTN's newest version) to a DataTable
    ///     Actually the reason why this might be indicated as 0 references is because this doesn't run in Debug mode.
    /// </summary>
    /// <returns>A Datatable with (hopefully) one row of data containing the newest GTN version</returns>
    private static DataTable DTFromAPI_GetGTNVersion()
    {
        DataTable dt_Return = new();
        dt_Return.Clear();
        dt_Return.Columns.Add(columnName: "version");

        string apiVersion = "";

        if (SApiOkay)
        {
            GtnReleasesApiResponse readJson_GTNVer = API_GenericGetGTNVersionFromWeb();
            if (readJson_GTNVer.TagName != null)
            {
                apiVersion = readJson_GTNVer.TagName;
            }
            // this will be a null value if Unauthorised, we'll ignore that.
        }

        if (SApiOkay)
        {
            DataRow dr_ReturnRow = dt_Return.NewRow();
            dr_ReturnRow[columnName: "version"] = apiVersion;
            dt_Return.Rows.Add(row: dr_ReturnRow);
        }

        return dt_Return;
    }

    /// <summary>
    ///     Loads up the Edit (file exif data) Form.
    /// </summary>
    internal static void ExifShowEditFrm()
    {
        int overallCount = 0;
        int fileCount = 0;
        int folderCount = 0;
        FrmEditFileData FrmEditFileData = new();
        FrmEditFileData.lvw_FileListEditImages.Items.Clear();
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        foreach (ListViewItem selectedItem in frmMainAppInstance.lvw_FileList.SelectedItems)
        {
            // only deal with listOfAsyncCompatibleFileNamesWithOutPath, not folders
            if (File.Exists(path: Path.Combine(path1: frmMainAppInstance.tbx_FolderName.Text, path2: selectedItem.Text)))
            {
                overallCount++;
                FrmMainApp.FolderName = frmMainAppInstance.tbx_FolderName.Text;
                FrmEditFileData.lvw_FileListEditImages.Items.Add(text: selectedItem.Text);
                fileCount++;
            }
            else if (Directory.Exists(path: Path.Combine(path1: frmMainAppInstance.tbx_FolderName.Text, path2: selectedItem.Text)))
            {
                overallCount++;
                folderCount++;
            }
        }

        if (fileCount > 0)
        {
            FrmEditFileData.ShowDialog();
        }
        // basically if the user only selected folders, do nothing
        else if (overallCount == folderCount + fileCount)
        {
            //nothing.
        }
        // we appear to have lost a file or two.
        else
        {
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningFileDisappeared"),
                            caption: GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    ///     Queues up a command to remove existing geo-data. Depending on the sender this can be for one or many
    ///     listOfAsyncCompatibleFileNamesWithOutPath.
    /// </summary>
    /// <param name="senderName">At this point this can either be the main listview or the one from Edit (file) data</param>
    internal static async Task ExifRemoveLocationData(string senderName)
    {
        List<string> toponomyOverwrites = new()
        {
            "GPSLatitude",
            "GPSLongitude",
            "CountryCode",
            "Country",
            "City",
            "State",
            "Sub_location",
            "GPSAltitude",
            "gps*"
        };

        if (DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "ckb_RemoveGeoDataRemovesTimeOffset") ==
            "true")

        {
            toponomyOverwrites.Add(item: "OffsetTime");
        }

        // sort is pointless here but otherwise it pokes my OCD.
        toponomyOverwrites.Sort();

        if (senderName == "FrmEditFileData")
        {
            // for the time being i'll leave this as "remove data from the active selection file" rather than "all".
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];

            // setting this to True prevents the code from checking the values are valid numbers.

            if (frmEditFileDataInstance != null)
            {
                string fileNameWithoutPath = frmEditFileDataInstance.lvw_FileListEditImages.SelectedItems[index: 0]
                    .Text;

                HelperNonStatic helperNonstatic = new();
                IEnumerable<Control> cGbx_GPSData = helperNonstatic.GetAllControls(control: frmEditFileDataInstance.gbx_GPSData);
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

                IEnumerable<Control> cGbx_LocationData = helperNonstatic.GetAllControls(control: frmEditFileDataInstance.gbx_LocationData);
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

                foreach (string toponomyDetail in toponomyOverwrites)
                {
                    GenericUpdateAddToDataTable(
                        dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                        fileNameWithoutPath: fileNameWithoutPath,
                        settingId: toponomyDetail,
                        settingValue: ""
                    );
                }

                GenericUpdateAddToDataTable(
                    dt: FrmMainApp.DtFileDataToWriteStage1PreQueue,
                    fileNameWithoutPath: fileNameWithoutPath,
                    settingId: "gps*",
                    settingValue: ""
                );
            }
        }
        else if (senderName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                ListView lvw = frmMainAppInstance.lvw_FileList;
                if (lvw.SelectedItems.Count > 0)
                {
                    FileListBeingUpdated = true;
                    foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.SelectedItems)
                    {
                        // don't do folders...
                        string fileNameWithPath = Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text);
                        string fileNameWithoutPath = lvi.Text;
                        if (File.Exists(path: fileNameWithPath))
                        {
                            //lvw.BeginUpdate();
                            // check it's not in the read-queue.
                            while (GenericLockCheckLockFile(fileNameWithoutPath: fileNameWithoutPath))
                            {
                                await Task.Delay(millisecondsDelay: 10);
                            }

                            // then put a blocker on
                            GenericLockLockFile(fileNameWithoutPath: fileNameWithoutPath);
                            foreach (string toponomyDetail in toponomyOverwrites)
                            {
                                GenericUpdateAddToDataTable(
                                    dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                                    fileNameWithoutPath: fileNameWithoutPath,
                                    settingId: toponomyDetail,
                                    settingValue: ""
                                );
                            }

                            // then remove lock

                            await LwvUpdateRowFromDTWriteStage3ReadyToWrite(lvi: lvi);
                            GenericLockUnLockFile(fileNameWithoutPath: fileNameWithoutPath);
                            // no need to remove the xmp here because it hasn't been added in the first place.
                        }

                        //lvw.EndUpdate();
                    }

                    FileListBeingUpdated = false;
                    FrmMainApp.RemoveGeoDataIsRunning = false;
                }
            }
        }
    }

    /// <summary>
    ///     Parses a CSV file to a DataTable
    ///     I've given up the original logic of OLEDB because it gets very bitchy with Culture stuff. This works better.
    /// </summary>
    /// <param name="fileNameWithPath">Path of CSV file</param>
    /// <param name="isUTF">whether the file is UTF8-encoded</param>
    /// <returns>Converted Datatable</returns>
    private static DataTable GetDataTableFromCsv(string fileNameWithPath,
                                                 bool isUTF)
    {
        DataTable dt = new();
        StreamReader reader;
        if (isUTF)
        {
            reader = new StreamReader(path: fileNameWithPath, encoding: Encoding.UTF8);
        }
        else
        {
            reader = new StreamReader(path: fileNameWithPath);
        }

        using CsvReader csv = new(reader: reader, culture: CultureInfo.InvariantCulture);
        // Do any configuration to `CsvReader` before creating CsvDataReader.
        using CsvDataReader dr = new(csv: csv);
        dt.Load(reader: dr);

        return dt;
    }


    /// <summary>
    ///     Checks and replaces blank toponomy values as required
    /// </summary>
    /// <param name="settingId"></param>
    /// <param name="settingValue"></param>
    /// <returns></returns>
    internal static string ReplaceBlankToponomy(string settingId,
                                                string settingValue)
    {
        string retStr = settingValue;
        if (AncillaryListsArrays.ToponomyReplaces()
                .Contains(value: settingId) &&
            ToponomyReplace &&
            settingValue.Length == 0)
        {
            retStr = ToponomyReplaceWithWhat;
        }

        return retStr;
    }
}