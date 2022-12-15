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
using System.Threading;
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
                    tryDataValue = outDateTime.ToString(format: CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern);
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

        DataTable dtObjectTagNameIn = null;
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
    ///     Parses the whole folder for relevant files and outputs a temp CSV that gets converted into a DataTable and fed into
    ///     the main ListView
    /// </summary>
    /// <param name="folderNameToUse">The folder to Parse</param>
    internal static async Task ExifGetExifFromFolder(string folderNameToUse)
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        // Clear the FrmMainApp.DtOriginalTakenDate && FrmMainApp.DtOriginalCreateDate tables
        FrmMainApp.Logger.Trace(message: "Clear FrmMainApp.DtOriginalTakenDate && FrmMainApp.DtOriginalCreateDate");
        FrmMainApp.DtOriginalTakenDate.Clear();
        FrmMainApp.DtOriginalCreateDate.Clear();

        if (folderNameToUse.EndsWith(value: @"\"))
        {
            folderNameToUse = folderNameToUse.Substring(startIndex: 0, length: folderNameToUse.Length - 1);
        }

        // using a .bat file actually ensures the process exits come rain or shine.
        string batchFilePath = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: "runme.bat");
        string csvFilePath = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: "out.csv");
        FrmMainApp.Logger.Trace(message: "batchFilePath: " + batchFilePath);
        FrmMainApp.Logger.Trace(message: "csvFilePath: " + csvFilePath);

        //basically if the form gets minimised it can turn to null methinks. 

        // I'm not logging the details of this method because they're basically hard-coded.
        FrmMainApp.Logger.Trace(message: "Setting Up exifTool");

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (frmMainAppInstance != null)
        {
            FrmMainApp.DtFileDataSeenInThisSession.Clear();
            string batchFileContents = "";
            string exifToolExe = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe");
            string argsFile = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: "exifArgs.args");
            string extraArgs = "";

            File.Delete(path: csvFilePath);
            File.Delete(path: batchFilePath);
            File.Delete(path: argsFile);

            List<string> exifArgs = new();

            // This will convert all Line Feeds and Carriage Returns into spaces in all the outputted data:
            // -api "Filter=s/\r|\n/ /g "

            string commonArgs = @" -api ""Filter=s/\r|\n/ /g "" -a -s -s -struct -sort -G -ee -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 ";

            // add required tags
            DataTable dtObjectTagNames = DataReadSQLiteObjectMappingTagsToPass();

            foreach (DataRow dr in dtObjectTagNames.Rows)
            {
                exifArgs.Add(item:
                             dr[columnName: "objectTagName_ToPass"]
                                 .ToString());
            }

            exifArgs.Add(item: "execute");

            extraArgs += " -ignoreMinorErrors";
            extraArgs += " -csv > " + SDoubleQuote + csvFilePath + SDoubleQuote;
            extraArgs += " -@ " + SDoubleQuote + argsFile + SDoubleQuote;

            // exiftool on its own would query a bunch of file types we don't care about so this isn't particularly neat but puts a blocker.
            string[] allowedExtensions = new string[AncillaryListsArrays.AllCompatibleExtensions()
                .Length];
            Array.Copy(sourceArray: allowedExtensions, destinationArray: AncillaryListsArrays.AllCompatibleExtensions(), length: 0);
            for (int i = 0; i < allowedExtensions.Length; i++)
            {
                allowedExtensions[i] = AncillaryListsArrays.AllCompatibleExtensions()[i]
                    .Split('\t')
                    .FirstOrDefault();

                File.AppendAllText(path: argsFile, contents: folderNameToUse + @"\*." + allowedExtensions[i] + Environment.NewLine, encoding: Encoding.UTF8);
            }

            // add XMP
            File.AppendAllText(path: argsFile, contents: folderNameToUse + @"\*.xmp" + Environment.NewLine, encoding: Encoding.UTF8);

            foreach (string arg in exifArgs)
            {
                File.AppendAllText(path: argsFile, contents: "-" + arg + Environment.NewLine);
            }

            batchFileContents = SDoubleQuote + exifToolExe + SDoubleQuote + commonArgs + extraArgs;
            File.AppendAllText(path: batchFilePath, contents: batchFileContents);

            FrmMainApp.Logger.Trace(message: "Starting exifTool");
            ///////////////
            // via https://stackoverflow.com/a/68616297/3968494
            await Task.Run(action: () =>
            {
                Process prcExifTool = new();
                prcExifTool.StartInfo.FileName = batchFilePath;

                prcExifTool.StartInfo.CreateNoWindow = true;
                prcExifTool.StartInfo.UseShellExecute = false;

                prcExifTool.Start();
                prcExifTool.WaitForExit();
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
            ///////////////

            if (File.Exists(path: csvFilePath))
            {
                FrmMainApp.Logger.Trace(message: "csvFilePath output exists OK");
                long length = new FileInfo(fileName: csvFilePath).Length;
                // technically "12" would do. If the folder doesn't contain anything useful the csv will be of 12 bytes length.
                if (length > 20)
                {
                    FrmMainApp.Logger.Trace(message: "csvFilePath output length > 20");

                    FrmMainApp.Logger.Trace(message: "Reading csvFilePath to dtCSV");
                    DataTable dtCSV = GetDataTableFromCsv(fileNameWithPath: csvFilePath);
                    FrmMainApp.Logger.Trace(message: "Reading csvFilePath to dtCSV - OK");

                    // parse file data 
                    foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.Items)
                    {
                        string fileNameWithoutPath = lvi.Text;
                        // obvs don't bother w/ non-files.
                        if (File.Exists(path: Path.Combine(path1: folderNameToUse, path2: fileNameWithoutPath)))
                        {
                            // The requirement is actually transposed. We need a table that is ...TagName/TagValue
                            DataTable dtFileExifTable = new();
                            dtFileExifTable.Clear();
                            dtFileExifTable.Columns.Add(columnName: "TagName");
                            dtFileExifTable.Columns.Add(columnName: "TagValue");

                            string fileNameWithPath = Path.Combine(path1: folderNameToUse, path2: fileNameWithoutPath);

                            // "normal" files (i.e. not XMP)
                            // The imported CSV uses fileNameWithPath but with "/" rather than "\". (ie "C:/temp")
                            string fileNameWithPathForwardSlash = fileNameWithPath
                                .Replace(oldValue: @"\", newValue: "/");

                            DataTable dtThisFileCsvData = null;
                            try
                            {
                                dtThisFileCsvData = dtCSV.Select(filterExpression: "SourceFile = '" + fileNameWithPathForwardSlash + "'")
                                    .CopyToDataTable();
                            }
                            catch
                            {
                                // not an error per se
                                FrmMainApp.Logger.Info(message: "Reading csvFilePath - " + fileNameWithoutPath + " - dtThisFileCsvData = null");
                                dtThisFileCsvData = null;
                            }

                            if (dtThisFileCsvData != null && dtThisFileCsvData.Rows.Count > 0)
                            {
                                // transpose CSV (skip #0, that's filename)
                                for (int csvCol = 1; csvCol < dtThisFileCsvData.Columns.Count; csvCol++)
                                {
                                    string tagName = dtThisFileCsvData.Columns[index: csvCol]
                                        .ToString();
                                    string tagValue = dtThisFileCsvData.Rows[index: 0][columnIndex: csvCol]
                                        .ToString();

                                    if (tagValue != "")
                                    {
                                        DataRow drThisFileTags = dtFileExifTable.NewRow();
                                        drThisFileTags[columnName: "TagName"] = tagName;
                                        drThisFileTags[columnName: "TagValue"] = tagValue;
                                        dtFileExifTable.Rows.Add(row: drThisFileTags);
                                        FrmMainApp.Logger.Trace(message: "Reading csvFilePath - " + fileNameWithoutPath + " - " + tagName + " - " + tagValue);
                                    }
                                }
                            }

                            // "xmp"
                            string sideCarXMPFilePath = Path.Combine(path1: folderNameToUse, path2: Path.GetFileNameWithoutExtension(path: fileNameWithPath) + ".xmp");
                            if (File.Exists(path: sideCarXMPFilePath))
                            {
                                fileNameWithPathForwardSlash = sideCarXMPFilePath
                                    .Replace(oldValue: @"\", newValue: "/");
                                try
                                {
                                    dtThisFileCsvData = dtCSV.Select(filterExpression: "SourceFile = '" + fileNameWithPathForwardSlash + "'")
                                        .CopyToDataTable();
                                }
                                catch
                                {
                                    FrmMainApp.Logger.Info(message: "Reading csvFilePath [xmp] - " + sideCarXMPFilePath + " - dtThisFileCsvData = null");
                                    dtThisFileCsvData = null;
                                }

                                if (dtThisFileCsvData != null && dtThisFileCsvData.Columns.Count > 0)
                                {
                                    // transpose CSV (skip #0, that's filename)
                                    for (int csvCol = 1; csvCol < dtThisFileCsvData.Columns.Count; csvCol++)
                                    {
                                        string tagName = dtThisFileCsvData.Columns[index: csvCol]
                                            .ToString();
                                        string tagValue = dtThisFileCsvData.Rows[index: 0][columnIndex: csvCol]
                                            .ToString();
                                        if (tagValue != "")
                                        {
                                            DataRow drThisFileTags = dtFileExifTable.NewRow();
                                            drThisFileTags[columnName: "TagName"] = tagName;
                                            drThisFileTags[columnName: "TagValue"] = tagValue;
                                            dtFileExifTable.Rows.Add(row: drThisFileTags);
                                            FrmMainApp.Logger.Trace(message: "Reading csvFilePath [xmp] - " + sideCarXMPFilePath + " - " + tagName + " - " + tagValue);
                                        }
                                    }
                                }
                            }

                            // de-dupe. this is pretty poor performance but the dataset is small
                            if (dtFileExifTable != null && dtFileExifTable.Rows.Count > 0)
                            {
                                dtFileExifTable = dtFileExifTable.DefaultView.ToTable(distinct: true);

                                // lvi uses fileNameWithoutPath
                                fileNameWithoutPath = Path.GetFileName(path: fileNameWithPath);

                                GenericLockLockFile(fileNameWithoutPath: fileNameWithoutPath);
                                FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);

                                ListView.ColumnHeaderCollection lvchs = frmMainAppInstance.ListViewColumnHeaders;

                                for (int i = 1; i < lvi.SubItems.Count; i++)
                                {
                                    string str = ExifGetStandardisedDataPointFromExif(dtFileExif: dtFileExifTable, dataPoint: lvchs[index: i]
                                                                                          .Name.Substring(startIndex: 4));
                                    lvi.SubItems[index: i]
                                        .Text = str;

                                    // Not logging because it gets logged in ExifGetStandardisedDataPointFromExif already.

                                    // TakenDate & CreateDate have to be sent into their respective tables for querying later if user chooses time-shift.
                                    if (lvchs[index: i]
                                            .Name ==
                                        "clh_TakenDate" &&
                                        str != "-")
                                    {
                                        DataRow drTakenDate = FrmMainApp.DtOriginalTakenDate.NewRow();
                                        drTakenDate[columnName: "fileNameWithoutPath"] = lvi.Text;
                                        drTakenDate[columnName: "settingId"] = "originalTakenDate";
                                        drTakenDate[columnName: "settingValue"] = DateTime.Parse(s: str,
                                                                                                 provider: CultureInfo.CurrentUICulture)
                                            .ToString(provider: CultureInfo.CurrentUICulture);
                                        FrmMainApp.DtOriginalTakenDate.Rows.Add(row: drTakenDate);
                                        FrmMainApp.DtOriginalTakenDate.AcceptChanges();
                                    }
                                    else if (lvchs[index: i]
                                                 .Name ==
                                             "clh_CreateDate" &&
                                             str != "-")
                                    {
                                        DataRow drCreateDate = FrmMainApp.DtOriginalCreateDate.NewRow();
                                        drCreateDate[columnName: "fileNameWithoutPath"] = lvi.Text;
                                        drCreateDate[columnName: "settingId"] = "originalCreateDate";
                                        drCreateDate[columnName: "settingValue"] = DateTime.Parse(s: str,
                                                                                                  provider: CultureInfo.CurrentUICulture)
                                            .ToString(provider: CultureInfo.CurrentUICulture);

                                        FrmMainApp.DtOriginalCreateDate.Rows.Add(row: drCreateDate);
                                        FrmMainApp.DtOriginalCreateDate.AcceptChanges();
                                    }

                                    // Not adding the xmp here because the current code logic would pull a "unified" data point.

                                    // No need to log everything three times....
                                    // FrmMainApp.Logger.Trace(message: "Adding file " + fileNameWithPath + "'s details to FrmMainApp.DtFileDataSeenInThisSession");

                                    DataRow dr = FrmMainApp.DtFileDataSeenInThisSession.NewRow();
                                    dr[columnName: "fileNameWithPath"] = fileNameWithPath;
                                    dr[columnName: "settingId"] = lvchs[index: i]
                                        .Name.Substring(startIndex: 4);
                                    dr[columnName: "settingValue"] = str;
                                    FrmMainApp.DtFileDataSeenInThisSession.Rows.Add(row: dr);
                                }

                                if (lvi.Index % 10 == 0)
                                {
                                    Application.DoEvents();
                                    // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                    FrmMainApp.HandlerLvwScrollToDataPoint(lvw: frmMainAppInstance.lvw_FileList, itemText: fileNameWithoutPath);
                                }

                                FrmMainApp.HandlerUpdateItemColour(lvw: frmMainAppInstance.lvw_FileList, itemText: fileNameWithoutPath, color: Color.Black);
                                GenericLockUnLockFile(fileNameWithoutPath: fileNameWithoutPath);
                            }
                            else
                            {
                                FrmMainApp.Logger.Info(message: "dtFileExifTable is null || dtFileExifTable has 0 rows");
                            }
                        }
                    }
                }
                else
                {
                    FrmMainApp.Logger.Trace(message: "csvFilePath output length <= 20");
                }
            }
            else
            {
                FrmMainApp.Logger.Error(message: "csvFilePath output doesn't exist");
            }
        }

        FrmMainApp.Logger.Debug(message: "Done");
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
    /// <param name="timeShiftSeconds">Int value if GPS time should be shifted.</param>
    /// <returns></returns>
    internal static async Task ExifGetTrackSyncData(string trackFileLocationType,
                                                    string trackFileLocationVal,
                                                    bool useTZAdjust,
                                                    string compareTZAgainst,
                                                    string TZVal,
                                                    int GeoMaxIntSecs,
                                                    int GeoMaxExtSecs,
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
            DirectoryInfo di_tmpLocFiles = new(path: tmpFolder);

            foreach (FileInfo file in di_tmpLocFiles.EnumerateFiles())
            {
                file.Delete();
            }

            exiftoolCmd += " " + " -srcfile " + SDoubleQuote + tmpFolder + @"\%F.xmp" + SDoubleQuote;
            exiftoolCmd += " -overwrite_original_in_place";

            ///////////////
            // via https://stackoverflow.com/a/68616297/3968494
            await Task.Run(action: () =>
            {
                using Process prcExifTool = new();
                prcExifTool.StartInfo = new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
                {
                    Arguments = @"/k " + SDoubleQuote + SDoubleQuote + Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe") + SDoubleQuote + " " + commonArgs + exiftoolCmd + SDoubleQuote + "&& exit",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                prcExifTool.EnableRaisingEvents = true;

                //s_ErrorMsg = "";
                //s_OutputMsg = "";
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
                        _sErrorMsg += data.Data.ToString() + Environment.NewLine;
                        try
                        {
                            prcExifTool.Kill();
                        }
                        catch
                        { } // else it will be stuck running forever
                    }
                };

                prcExifTool.Start();
                prcExifTool.BeginOutputReadLine();
                prcExifTool.BeginErrorReadLine();
                prcExifTool.WaitForExit();
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

            ///////////////
            //// try to collect the xmp/xml listOfAsyncCompatibleFileNamesWithOutPath and then read them back into the listview.

            foreach (FileInfo exifFileIn in di_tmpLocFiles.EnumerateFiles())
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

                                            if (lvi.Index % 10 == 0)
                                            {
                                                Application.DoEvents();
                                                // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                                FrmMainApp.HandlerLvwScrollToDataPoint(lvw: lvw, itemText: fileNameWithoutPath);
                                            }

                                            FrmMainApp.HandlerUpdateItemColour(lvw: lvw, itemText: fileNameWithoutPath, color: Color.Red);
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
                                    SApiOkay = true;
                                    DataTable dt_Toponomy = DTFromAPIExifGetToponomyFromWebOrSQL(lat: strParsedLat.ToString(provider: CultureInfo.InvariantCulture), lng: strParsedLng.ToString(provider: CultureInfo.InvariantCulture));

                                    if (SApiOkay)
                                    {
                                        List<(string toponomyOverwriteName, string toponomyOverwriteVal)> toponomyOverwrites = new();
                                        toponomyOverwrites.Add(item: ("CountryCode", dt_Toponomy.Rows[index: 0][columnName: "CountryCode"]
                                                                          .ToString()));
                                        toponomyOverwrites.Add(item: ("Country", dt_Toponomy.Rows[index: 0][columnName: "Country"]
                                                                          .ToString()));
                                        toponomyOverwrites.Add(item: ("City", dt_Toponomy.Rows[index: 0][columnName: "City"]
                                                                          .ToString()));
                                        toponomyOverwrites.Add(item: ("State", dt_Toponomy.Rows[index: 0][columnName: "State"]
                                                                          .ToString()));
                                        toponomyOverwrites.Add(item: ("Sub_location", dt_Toponomy.Rows[index: 0][columnName: "Sub_location"]
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

                                            FrmMainApp.HandlerLvwScrollToDataPoint(lvw: lvw, itemText: fileNameWithoutPath);
                                        }

                                        FrmMainApp.HandlerUpdateItemColour(lvw: lvw, itemText: fileNameWithoutPath, color: Color.Red);
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

        DialogResult dialogResult = MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_FrmImportGpx_AskUserWantsReport"), caption: "Info", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Question);
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

            //Label lblText = new Label();
            //lblText.Text = s_OutputMsg;
            //lblText.AutoSize = true;
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
    ///     Gets the app version of the current exifTool.
    ///     <returns>A double of the current exifTool version.</returns>
    /// </summary>
    private static async Task<decimal> ExifGetExifToolVersion()
    {
        string exifToolResult;
        decimal returnVal = 0.0m;
        bool parsedResult;
        decimal parsedDecimal = 0.0m;

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        IEnumerable<string> exifToolCommand = Enumerable.Empty<string>();
        List<string> commonArgList = new()
        {
            "-ver"
        };

        foreach (string arg in commonArgList)
        {
            exifToolCommand = exifToolCommand.Concat(second: new[]
            {
                arg
            });
        }

        CancellationToken ct = CancellationToken.None;

        try
        {
            exifToolResult = await frmMainAppInstance.AsyncExifTool.ExecuteAsync(args: exifToolCommand);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorAsyncExifToolExecuteAsyncFailed") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            exifToolResult = null;
        }

        string[] exifToolResultArr = Convert.ToString(value: exifToolResult)
            .Split(
                separator: new[] { "\r\n", "\r", "\n" },
                options: StringSplitOptions.None
            )
            .Distinct()
            .ToArray();

        // really this should only be 1 row but I copied from the larger code blocks and is easier that way.
        foreach (string exifToolReturnStr in exifToolResultArr)
        {
            if (exifToolReturnStr is not null && exifToolReturnStr.Length > 0)
            {
                //this only returns something like "12.42" and that's all
                parsedResult = decimal.TryParse(s: exifToolReturnStr, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedDecimal);
                if (parsedResult)
                {
                    returnVal = parsedDecimal;
                }
                else
                {
                    returnVal = 0.0m;
                }
            }
        }

        return returnVal;
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
        // via https://stackoverflow.com/a/68616297/3968494
        await Task.Run(action: () =>
        {
            using Process prcExifTool = new();
            prcExifTool.StartInfo = new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
            {
                Arguments = @"/k " + SDoubleQuote + SDoubleQuote + Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe") + SDoubleQuote + " " + exiftoolCmd + SDoubleQuote + "&& exit",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            prcExifTool.EnableRaisingEvents = true;
            prcExifTool.OutputDataReceived += (_,
                                               data) =>
            {
                // don't care
            };

            prcExifTool.ErrorDataReceived += (_,
                                              data) =>
            {
                // don't care
            };

            prcExifTool.Start();
            prcExifTool.BeginOutputReadLine();
            prcExifTool.BeginErrorReadLine();
            prcExifTool.WaitForExit();
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
        ///////////////
        FrmMainApp.Logger.Debug(message: "Done");
    }

    /// <summary>
    ///     Writes outstanding exif changes to the listOfAsyncCompatibleFileNamesWithOutPath (all
    ///     listOfAsyncCompatibleFileNamesWithOutPath in the queue).
    ///     This logic is very similar to the "incompatible read" above - it's safer. While it's also probably slower
    ///     ... the assumption is that users will read a lot of listOfAsyncCompatibleFileNamesWithOutPath but will write
    ///     proportionately fewer listOfAsyncCompatibleFileNamesWithOutPath so
    ///     ... speed is less of an essence against safety.
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
                            string tmpSettingValue = drFileTags[columnName: "settingValue"]
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
                            if (objectTagNameOut == "EXIF:DateTimeOriginal" ||
                                objectTagNameOut == "EXIF:CreateDate" ||
                                objectTagNameOut == "XMP:DateTimeOriginal" ||
                                objectTagNameOut == "XMP:CreateDate")
                            {
                                try
                                {
                                    updateExifVal = DateTime.Parse(s: settingValue)
                                        .ToString(format: "yyyy-MM-dd HH:mm:ss");
                                }
                                catch
                                {
                                    updateExifVal = "";
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
                    MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNoWriteSettingEnabled"), caption: "Errrmmm...", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
                }
            }
        }

        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();

        if (!failWriteNothingEnabled && !queueWasEmpty)
        {
            int lviIndex = 0;
            FrmMainApp.Logger.Info(message: "Starting ExifTool.");
            ///////////////
            // via https://stackoverflow.com/a/68616297/3968494
            await Task.Run(action: () =>
            {
                using Process prcExifTool = new();
                prcExifTool.StartInfo = new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
                {
                    Arguments = @"/k " + SDoubleQuote + SDoubleQuote + Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe") + SDoubleQuote + " " + exiftoolCmd + SDoubleQuote + "&& exit",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                prcExifTool.EnableRaisingEvents = true;

                //string messageStart = "Writing Exif - ";
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

                                FrmMainApp.HandlerLvwScrollToDataPoint(lvw: frmMainAppInstance.lvw_FileList, itemText: fileNameWithoutPath);
                            }

                            FrmMainApp.HandlerUpdateItemColour(lvw: frmMainAppInstance.lvw_FileList, itemText: fileNameWithoutPath, color: Color.Black);

                            if (Path.GetExtension(path: fileNameWithoutPath) == ".xmp")
                            {
                                // problem is that if only the xmp file gets overwritten then there is no indication of the original file here. 
                                // FindItemWithText -> https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.listview.finditemwithtext?view=netframework-4.8
                                // "Finds the first ListViewItem with __that begins with__ the given text value."

                                if (lviIndex % 10 == 0)
                                {
                                    Application.DoEvents();
                                    // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                    FrmMainApp.HandlerLvwScrollToDataPoint(lvw: frmMainAppInstance.lvw_FileList, itemText: fileNameWithoutPath); // this is redundant here.
                                }

                                FrmMainApp.HandlerUpdateItemColour(lvw: frmMainAppInstance.lvw_FileList, itemText: Path.GetFileNameWithoutExtension(path: fileNameWithoutPath), color: Color.Black);
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

                                FrmMainApp.HandlerLvwScrollToDataPoint(lvw: frmMainAppInstance.lvw_FileList, itemText: fileNameWithoutPath);
                            }

                            FrmMainApp.HandlerUpdateItemColour(lvw: frmMainAppInstance.lvw_FileList, itemText: fileNameWithoutPath, color: Color.Black);

                            if (Path.GetExtension(path: fileNameWithoutPath) == ".xmp")
                            {
                                // problem is that if only the xmp file gets overwritten then there is no indication of the original file here. 
                                // FindItemWithText -> https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.listview.finditemwithtext?view=netframework-4.8
                                // "Finds the first ListViewItem with __that begins with__ the given text value."
                                if (lviIndex % 10 == 0)
                                {
                                    Application.DoEvents();
                                    // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                    FrmMainApp.HandlerLvwScrollToDataPoint(lvw: frmMainAppInstance.lvw_FileList, itemText: fileNameWithoutPath); // this is redundant here
                                }

                                FrmMainApp.HandlerUpdateItemColour(lvw: frmMainAppInstance.lvw_FileList, itemText: Path.GetFileNameWithoutExtension(path: fileNameWithoutPath), color: Color.Black);
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

                prcExifTool.Start();
                prcExifTool.BeginOutputReadLine();
                prcExifTool.BeginErrorReadLine();
                prcExifTool.WaitForExit();
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
        else if (!queueWasEmpty)
        {
            FrmMainApp.Logger.Info(message: "Both file-writes disabled. Nothing Written.");
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNoWriteSettingEnabled"), caption: "Errrmmm...", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }
        else
        {
            FrmMainApp.Logger.Info(message: "Queue was empty. Nothing Written.");
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNothingInWriteQueue"), caption: "Errrmmm...", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }

        ///////////////
        FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Ready.");
        FilesAreBeingSaved = false;
    }


    /// <summary>
    ///     Responsible for pulling the toponomy response for the API
    /// </summary>
    /// <param name="latitude">As on the tin.</param>
    /// <param name="longitude">As on the tin.</param>
    /// <returns>Structured toponomy response</returns>
    private static GeoResponseToponomy API_ExifGetGeoDataFromWebToponomy(string latitude,
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
                MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorCantReadDefaultSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            }
        }

        GeoResponseToponomy returnVal = new();
        RestClient client = new(baseUrl: "http://api.geonames.org/")
        {
            Authenticator = new HttpBasicAuthenticator(username: SGeoNamesUserName, password: SGeoNamesPwd)
        };

        RestRequest request_Toponomy = new(resource: "findNearbyPlaceNameJSON?lat=" + latitude + "&lng=" + longitude + "&style=FULL");
        RestResponse response_Toponomy = client.ExecuteGet(request: request_Toponomy);
        // check API reponse is OK
        if (response_Toponomy.Content != null && response_Toponomy.Content.Contains(value: "the hourly limit of "))
        {
            SApiOkay = false;
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") + response_Toponomy.Content, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }
        else if (response_Toponomy.StatusCode.ToString() == "OK")
        {
            SApiOkay = true;
            JObject data = (JObject)JsonConvert.DeserializeObject(value: response_Toponomy.Content);
            GeoResponseToponomy geoResponse_Toponomy = GeoResponseToponomy.FromJson(Json: data.ToString());
            returnVal = geoResponse_Toponomy;
        }
        else
        {
            SApiOkay = false;
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") + response_Toponomy.StatusCode, caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
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
                MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorCantReadDefaultSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
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
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") + response_TimeZone.Content, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
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
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") + response_TimeZone.StatusCode, caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }

        return returnVal;
    }

    /// <summary>
    ///     Responsible for pulling the altitude response for the API
    /// </summary>
    /// <param name="latitude">As on the tin.</param>
    /// <param name="longitude">As on the tin.</param>
    /// <returns>Structured altitude response</returns>
    private static GeoResponseAltitude API_ExifGetGeoDataFromWebAltitude(string latitude,
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
                MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorCantReadDefaultSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            }
        }

        GeoResponseAltitude returnVal = new();
        RestClient client = new(baseUrl: "http://api.geonames.org/")
        {
            Authenticator = new HttpBasicAuthenticator(username: SGeoNamesUserName, password: SGeoNamesPwd)
        };

        RestRequest request_Altitude = new(resource: "srtm1JSON?lat=" + latitude + "&lng=" + longitude);
        RestResponse response_Altitude = client.ExecuteGet(request: request_Altitude);
        // check API reponse is OK
        if (response_Altitude.Content != null && response_Altitude.Content.Contains(value: "the hourly limit of "))
        {
            SApiOkay = false;
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") + response_Altitude.Content, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }
        else if (response_Altitude.StatusCode.ToString() == "OK")
        {
            SApiOkay = true;
            JObject data = (JObject)JsonConvert.DeserializeObject(value: response_Altitude.Content);
            GeoResponseAltitude geoResponseAltitude = GeoResponseAltitude.FromJson(Json: data.ToString());
            returnVal = geoResponseAltitude;
        }
        else
        {
            SApiOkay = false;
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") + response_Altitude.StatusCode, caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
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
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGTNVerAPIResponse") + response_GTNVersionQuery.StatusCode, caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
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
        DataTable dt_Return = new();
        dt_Return.Clear();
        dt_Return.Columns.Add(columnName: "CountryCode");
        dt_Return.Columns.Add(columnName: "Country");
        dt_Return.Columns.Add(columnName: "City");
        dt_Return.Columns.Add(columnName: "State");
        dt_Return.Columns.Add(columnName: "Sub_location");
        dt_Return.Columns.Add(columnName: "timezoneId");

        // logic: it's likely that API info doesn't change much...ever. 
        // Given that in 2022 Crimea is still part of Ukraine according to API response I think this data is static
        // ....so no need to query stuff that may already be locally available.
        // FYI this only gets amended when this button gets pressed or if map-to-location button gets pressed on the main form.
        // ... also on an unrelated note Toponomy vs Toponmy the API calls it one thing and Wikipedia calls it another so go figure
        DataTable dt_ToponomyInSQL = DataReadSQLiteToponomyWholeRow(
            lat: lat.ToString(provider: CultureInfo.InvariantCulture),
            lng: lng.ToString(provider: CultureInfo.InvariantCulture)
        );

        GeoResponseToponomy readJsonToponomy;
        GeoResponseTimeZone readJsonTimeZone;

        string CountryCode = "";
        string Country = "";
        string City = "";
        string State = "";
        string Sub_location = "";
        string timezoneId = "";

        // read from SQL
        if (dt_ToponomyInSQL.Rows.Count > 0)
        {
            CountryCode = dt_ToponomyInSQL.Rows[index: 0][columnName: "CountryCode"]
                .ToString();
            Country = DataReadSQLiteCountryCodesNames(
                    queryWhat: "ISO_3166_1A3",
                    inputVal: CountryCode,
                    returnWhat: "Country")
                ;

            timezoneId = dt_ToponomyInSQL.Rows[index: 0][columnName: "timezoneId"]
                .ToString();

            if (CountryCode == "GBR" &&
                dt_ToponomyInSQL.Rows[index: 0][columnName: "AdminName2"]
                    .ToString()
                    .Contains(value: "London"))
            {
                City = dt_ToponomyInSQL.Rows[index: 0][columnName: "AdminName2"]
                    .ToString();
                State = dt_ToponomyInSQL.Rows[index: 0][columnName: "AdminName1"]
                    .ToString();
                Sub_location = dt_ToponomyInSQL.Rows[index: 0][columnName: "ToponymName"]
                    .ToString();
            }
            else
            {
                City = dt_ToponomyInSQL.Rows[index: 0][columnName: "ToponymName"]
                    .ToString();
                State = dt_ToponomyInSQL.Rows[index: 0][columnName: "AdminName1"]
                    .ToString();
                Sub_location = dt_ToponomyInSQL.Rows[index: 0][columnName: "AdminName2"]
                    .ToString();
            }
        }
        // read from API
        else if (SApiOkay)
        {
            readJsonToponomy = API_ExifGetGeoDataFromWebToponomy(
                latitude: lat,
                longitude: lng
            );

            readJsonTimeZone = API_ExifGetGeoDataFromWebTimeZone(
                latitude: lat,
                longitude: lng
            );

            // ignore if unauthorised or some such
            if (readJsonToponomy.Geonames != null && readJsonTimeZone.TimezoneId != null)
            {
                if (readJsonToponomy.Geonames.Length + readJsonTimeZone.TimezoneId.Length > 0)
                {
                    string APICountryCode = readJsonToponomy.Geonames[0]
                        .CountryCode;
                    if (APICountryCode != null || APICountryCode != "")
                    {
                        CountryCode = DataReadSQLiteCountryCodesNames(
                            queryWhat: "ISO_3166_1A2",
                            inputVal: APICountryCode,
                            returnWhat: "ISO_3166_1A3"
                        );
                        Country = DataReadSQLiteCountryCodesNames(
                            queryWhat: "ISO_3166_1A2",
                            inputVal: APICountryCode,
                            returnWhat: "Country"
                        );
                    }

                    // api sends back some misaligned stuff for the UK
                    if (CountryCode == "GBR" &&
                        readJsonToponomy.Geonames[0]
                            .AdminName2
                            .Contains(value: "London"))
                    {
                        City = readJsonToponomy.Geonames[0]
                            .AdminName2;
                        State = readJsonToponomy.Geonames[0]
                            .AdminName1;
                        Sub_location = readJsonToponomy.Geonames[0]
                            .ToponymName;
                    }
                    else
                    {
                        City = readJsonToponomy.Geonames[0]
                            .ToponymName;
                        State = readJsonToponomy.Geonames[0]
                            .AdminName1;
                        Sub_location = readJsonToponomy.Geonames[0]
                            .AdminName2;
                    }

                    // this is already String.
                    timezoneId = readJsonTimeZone.TimezoneId;

                    // write back to sql the new stuff
                    DataWriteSQLiteToponomyWholeRow(
                        lat: lat,
                        lng: lng,
                        AdminName1: readJsonToponomy.Geonames[0]
                            .AdminName1,
                        AdminName2: readJsonToponomy.Geonames[0]
                            .AdminName2,
                        ToponymName: readJsonToponomy.Geonames[0]
                            .ToponymName,
                        CountryCode: CountryCode,
                        timezoneId: timezoneId
                    );
                }
                else if (SApiOkay)
                {
                    // write back empty
                    DataWriteSQLiteToponomyWholeRow(
                        lat: lat,
                        lng: lng
                    );
                }
            }
        }

        if (SApiOkay || dt_ToponomyInSQL.Rows.Count > 0)
        {
            DataRow dr_ReturnRow = dt_Return.NewRow();
            dr_ReturnRow[columnName: "CountryCode"] = CountryCode;
            dr_ReturnRow[columnName: "Country"] = Country;
            dr_ReturnRow[columnName: "City"] = City;
            dr_ReturnRow[columnName: "State"] = State;
            dr_ReturnRow[columnName: "Sub_location"] = Sub_location;
            dr_ReturnRow[columnName: "timezoneId"] = timezoneId;
            dt_Return.Rows.Add(row: dr_ReturnRow);
        }

        return dt_Return;
    }

    /// <summary>
    ///     Performs a search in the local SQLite database for cached altitude info and if finds it, returns that, else queries
    ///     the API
    /// </summary>
    /// <param name="lat">latitude/longitude to be queried</param>
    /// <param name="lng">latitude/longitude to be queried</param>
    /// <returns>
    ///     See summary. Returns the altitude info either from SQLite if available or the API in DataTable for further
    ///     processing
    /// </returns>
    internal static DataTable DTFromAPIExifGetAltitudeFromWebOrSQL(string lat,
                                                                   string lng)
    {
        DataTable dt_Return = new();
        dt_Return.Clear();
        dt_Return.Columns.Add(columnName: "Altitude");

        string Altitude = "0";

        DataTable dt_AltitudeInSQL = DataReadSQLiteAltitudeWholeRow(
            lat: lat,
            lng: lng
        );

        if (dt_AltitudeInSQL.Rows.Count > 0)
        {
            Altitude = dt_AltitudeInSQL.Rows[index: 0][columnName: "Srtm1"]
                .ToString();
            Altitude = Altitude.ToString(provider: CultureInfo.InvariantCulture);
        }
        else if (SApiOkay)
        {
            GeoResponseAltitude readJson_Altitude = API_ExifGetGeoDataFromWebAltitude(
                latitude: lat,
                longitude: lng
            );
            if (readJson_Altitude.Srtm1 != null)
            {
                string tmpAltitude = readJson_Altitude.Srtm1.ToString();
                tmpAltitude = tmpAltitude.ToString(provider: CultureInfo.InvariantCulture);

                // ignore if the API sends back something silly.
                // basically i'm assuming some ppl might take pics on an airplane but even those don't fly this high.
                // also if you're in the Mariana Trench, do send me a photo, will you?
                if (Math.Abs(value: int.Parse(s: tmpAltitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture)) > 32000)
                {
                    tmpAltitude = "0";
                }

                Altitude = tmpAltitude;
                // write back to sql the new stuff
                DataWriteSQLiteAltitudeWholeRow(
                    lat: lat,
                    lng: lng,
                    Altitude: Altitude
                );
            }

            // this will be a null value if Unauthorised, we'll ignore that.
            if (readJson_Altitude.Lat == null && SApiOkay)
            {
                // write back blank
                DataWriteSQLiteAltitudeWholeRow(
                    lat: lat,
                    lng: lng
                );
            }
        }

        if (SApiOkay || dt_AltitudeInSQL.Rows.Count > 0)
        {
            DataRow dr_ReturnRow = dt_Return.NewRow();
            dr_ReturnRow[columnName: "Altitude"] = Altitude;
            dt_Return.Rows.Add(row: dr_ReturnRow);
        }

        return dt_Return;
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
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningFileDisappeared"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
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

        ;
        if (senderName == "FrmEditFileData")
        {
            // for the time being i'll leave this as "remove data from the active selection file" rather than "all".
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];

            // setting this to True prevents the code from checking the values are valid numbers.
            FrmEditFileData.FrmEditFileDataNowRemovingGeoData = true;
            if (frmEditFileDataInstance != null)
            {
                string fileNameWithoutPath = frmEditFileDataInstance.lvw_FileListEditImages.SelectedItems[index: 0]
                    .Text;
                frmEditFileDataInstance.tbx_GPSLatitude.Text = "";
                frmEditFileDataInstance.tbx_GPSLongitude.Text = "";
                frmEditFileDataInstance.tbx_GPSAltitude.Text = "";
                frmEditFileDataInstance.tbx_City.Text = "";
                frmEditFileDataInstance.tbx_State.Text = "";
                frmEditFileDataInstance.tbx_Sub_location.Text = "";
                frmEditFileDataInstance.cbx_CountryCode.Text = "";
                frmEditFileDataInstance.cbx_Country.Text = "";
                FrmEditFileData.FrmEditFileDataNowRemovingGeoData = false;

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
    /// <returns>Converted Datatable</returns>
    private static DataTable GetDataTableFromCsv(string fileNameWithPath)
    {
        DataTable dt = new();
        using StreamReader reader = new(path: fileNameWithPath);
        using CsvReader csv = new(reader: reader, culture: CultureInfo.InvariantCulture);
        // Do any configuration to `CsvReader` before creating CsvDataReader.
        using CsvDataReader dr = new(csv: csv);
        dt.Load(reader: dr);

        return dt;
    }
}