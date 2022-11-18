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
using geoTagNinja;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
    #region Exif Related

    /// <summary>
    ///     Wrangles data from raw exiftool output to presentable and standardised data.
    /// </summary>
    /// <param name="dtFileExif">Raw values tag from exiftool</param>
    /// <param name="dataPoint">Name of the exiftag we want the data for</param>
    /// <returns>Standardised exif tag output</returns>
    internal static string ExifGetStandardisedDataPointFromExif(DataTable dtFileExif,
                                                                string dataPoint)
    {
        string returnVal = "";

        string tmpLongVal = "-";
        string tryDataValue = "-";
        string tmpLatRefVal = "-";
        string tmpLongRefVal = "-";
        string tmpLatLongRefVal = "-";

        string tmpOutLatLongVal = "";

        try
        {
            tryDataValue = ExifGetRawDataPointFromExif(dtFileExif: dtFileExif, dataPoint: dataPoint);
        }
        catch
        {
            // nothing
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

                ;
                if (tmpLongVal == "")
                {
                    tmpLongVal = "-";
                }

                ;

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
                            double numerator;
                            double denominator;
                            bool parseBool = double.TryParse(s: tryDataValue.Split('/')[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out numerator);
                            parseBool = double.TryParse(s: tryDataValue.Split('/')[1], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out denominator);
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
        }

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
    internal static string ExifGetRawDataPointFromExif(DataTable dtFileExif,
                                                       string dataPoint)
    {
        string returnVal = "-";
        string tryDataValue = "-";

        DataTable dtobjectTagNames_In = GenericJoinDataTables(t1: FrmMainApp.ObjectNames, t2: FrmMainApp.ObjectTagNamesIn,
                                                              (row1,
                                                               row2) =>
                                                                  row1.Field<string>(columnName: "objectName") == row2.Field<string>(columnName: "objectName"));

        DataView dvobjectTagNames_In = new(table: dtobjectTagNames_In)
        {
            RowFilter = "objectName = '" + dataPoint + "'",
            Sort = "valuePriorityOrder"
        };

        DataTable dtTagsWanted = dvobjectTagNames_In.ToTable(distinct: true, "objectTagName_In");

        if (dtTagsWanted.Rows.Count > 0 && dtFileExif.Rows.Count > 0)
        {
            foreach (DataRow dr in dtTagsWanted.Rows)
            {
                try
                {
                    string tagNameToSelect = dr[columnIndex: 0]
                        .ToString();
                    DataRow filteredRows = dtFileExif.Select(filterExpression: "TagName = '" + tagNameToSelect + "'")
                        .FirstOrDefault();
                    if (filteredRows != null)
                    {
                        tryDataValue = filteredRows[columnIndex: 1]
                            ?.ToString();
                        if (tryDataValue != null && tryDataValue != "")
                        {
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

        returnVal = tryDataValue;
        return returnVal;
    }

    /// <summary>
    ///     This parses the "compatible" file(name)s for exif tags.
    ///     ... "compatible" here means that the path of the file generally uses English characters only.
    ///     ... "tags" here means the tags required for the software to work (such as GPS stuff), not all the tags in the whole
    ///     of the file.
    ///     exifToolResult is a direct ouptut of exiftool that gets read into a DT and parsed. This is a fast process and can
    ///     work line-by-line for
    ///     ... items in the listview that's asking for it.
    /// </summary>
    /// <param name="files">List of "compatible" filenames</param>
    /// <param name="folderEnterEpoch">
    ///     This is for session-checking -> if the user was to move folders while the call is
    ///     executing and the new folder has identical file names w/o this the wrong data could show
    /// </param>
    /// <returns>
    ///     In practice, nothing but it's responsible for sending the updated exif info back to the requester (usually a
    ///     listview)
    /// </returns>
    internal static async Task ExifGetExifFromFilesCompatibleFileNames(List<string> files,
                                                                       long folderEnterEpoch)
    {
        IEnumerable<string> exifToolCommand = Enumerable.Empty<string>();
        IEnumerable<string> exifToolCommandWithFileName = Enumerable.Empty<string>();
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (FrmMainAppInstance != null)
        {
            List<string> commonArgList = new()
            {
                "-a",
                "-s",
                "-s",
                "-struct",
                "-sort",
                "-G",
                "-ee",
                "-ignoreMinorErrors"
            };

            foreach (string arg in commonArgList)
            {
                exifToolCommand = exifToolCommand.Concat(second: new[] { arg });
            }

            CancellationToken ct = CancellationToken.None;
            // add required tags
            DataTable dt_objectTagNames = DataReadSQLiteObjectMappingTagsToPass();

            foreach (DataRow dr in dt_objectTagNames.Rows)
            {
                exifToolCommand = exifToolCommand.Concat(second: new[] { "-" + dr[columnName: "objectTagName_ToPass"] });
            }

            string folderNameToUse = FrmMainAppInstance.tbx_FolderName.Text;

            foreach (string listElem in files)
            {
                DataTable dt_fileExifTable = new();
                dt_fileExifTable.Clear();
                dt_fileExifTable.Columns.Add(columnName: "TagName");
                dt_fileExifTable.Columns.Add(columnName: "TagValue");
                string exifToolResult;

                if (File.Exists(path: Path.Combine(path1: folderNameToUse, path2: listElem)))
                {
                    try
                    {
                        exifToolCommandWithFileName = exifToolCommand.Concat(second: new[] { Path.Combine(path1: folderNameToUse, path2: listElem) });
                        exifToolResult = await FrmMainAppInstance.AsyncExifTool.ExecuteAsync(args: exifToolCommandWithFileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorAsyncExifToolExecuteAsyncFailed") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                        exifToolResult = null;
                    }

                    string[] exifToolResultArr;

                    // if user closes app this could return a null
                    if (exifToolResult != null)
                    {
                        exifToolResultArr = Convert.ToString(value: exifToolResult)
                            .Split(
                                separator: new[] { "\r\n", "\r", "\n" },
                                options: StringSplitOptions.None
                            )
                            .Distinct()
                            .ToArray();
                        ;

                        foreach (string fileExifDataRow in exifToolResultArr)
                        {
                            if (fileExifDataRow is not null && fileExifDataRow.Length > 0 && fileExifDataRow.Substring(startIndex: 0, length: 1) == "[")
                            {
                                string exifGroup = fileExifDataRow.Split(' ')[0]
                                    .Replace(oldValue: "[", newValue: "")
                                    .Replace(oldValue: "]", newValue: "");
                                string exifTagName = fileExifDataRow.Split(' ')[1]
                                    .Replace(oldValue: ":", newValue: "");
                                string exifTagVal = fileExifDataRow.Substring(startIndex: fileExifDataRow.IndexOf(value: ':') + 2);

                                DataRow dr = dt_fileExifTable.NewRow();
                                dr[columnName: "TagName"] = exifGroup + ":" + exifTagName;
                                dr[columnName: "TagValue"] = exifTagVal;

                                dt_fileExifTable.Rows.Add(row: dr);
                            }
                        }
                    }

                    // for some files there may be data in a sidecar xmp without that data existing in the picture-file. we'll try to collect it here.
                    if (File.Exists(path: Path.Combine(path1: folderNameToUse, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToUse, path2: listElem)) + ".xmp")))
                    {
                        string sideCarXMPFilePath = Path.Combine(path1: folderNameToUse, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToUse, path2: listElem)) + ".xmp");
                        // this is totally a copypaste from above
                        try
                        {
                            exifToolCommandWithFileName = exifToolCommand.Concat(second: new[] { sideCarXMPFilePath });
                            exifToolResult = await FrmMainAppInstance.AsyncExifTool.ExecuteAsync(args: exifToolCommandWithFileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorAsyncExifToolExecuteAsyncFailed") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                            exifToolResult = null;
                        }

                        if (exifToolResult != null)
                        {
                            string[] exifToolResultArrXMP = Convert.ToString(value: exifToolResult)
                                .Split(
                                    separator: new[] { "\r\n", "\r", "\n" },
                                    options: StringSplitOptions.None
                                )
                                .Distinct()
                                .ToArray();
                            ;

                            foreach (string fileExifDataRow in exifToolResultArrXMP)
                            {
                                if (fileExifDataRow is not null && fileExifDataRow.Length > 0 && fileExifDataRow.Substring(startIndex: 0, length: 1) == "[")
                                {
                                    string exifGroup = fileExifDataRow.Split(' ')[0]
                                        .Replace(oldValue: "[", newValue: "")
                                        .Replace(oldValue: "]", newValue: "");
                                    string exifTagName = fileExifDataRow.Split(' ')[1]
                                        .Replace(oldValue: ":", newValue: "");
                                    string exifTagVal = fileExifDataRow.Substring(startIndex: fileExifDataRow.IndexOf(value: ':') + 2);

                                    DataRow dr = dt_fileExifTable.NewRow();
                                    dr[columnName: "TagName"] = exifGroup + ":" + exifTagName;
                                    dr[columnName: "TagValue"] = exifTagVal;

                                    dt_fileExifTable.Rows.Add(row: dr);
                                }
                            }
                        }
                    }

                    ListViewItem lvi = FrmMainAppInstance.lvw_FileList.FindItemWithText(text: listElem);

                    // lvi can become null if user changes folder while a previous process is loading and listview is cleared.
                    // also just make sure we're in the same session.
                    if (lvi != null && folderEnterLastEpoch == folderEnterEpoch)
                    {
                        ListView.ColumnHeaderCollection lvchs = FrmMainAppInstance.ListViewColumnHeaders;
                        for (int i = 1; i < lvi.SubItems.Count; i++)
                        {
                            string str = ExifGetStandardisedDataPointFromExif(dtFileExif: dt_fileExifTable, dataPoint: lvchs[index: i]
                                                                                  .Name.Substring(startIndex: 4));
                            lvi.SubItems[index: i]
                                .Text = str;
                        }

                        lvi.ForeColor = Color.Black;
                        FrmMainApp.HandlerUpdateLabelText(label: FrmMainAppInstance.lbl_ParseProgress, text: "Processing: " + lvi.Text);
                    }
                }
            }

            FrmMainApp.HandlerUpdateLabelText(label: FrmMainAppInstance.lbl_ParseProgress, text: "Ready.");
        }
    }

    /// <summary>
    ///     This parses the "incompatible" file(name)s for exif tags.
    ///     ... "incompatible" here means that the path of the file does not exclusively use English characters.
    ///     ... "tags" here means the tags required for the software to work (such as GPS stuff), not all the tags in the whole
    ///     of the file.
    ///     The main difference between this and the "compatible" version is that this calls cmd and then puts the output into
    ///     a txt file (as part of exiftoolCmd) that gets read back in
    ///     ... this is slower and allows for less control but safer.
    /// </summary>
    /// <param name="files">List of "incompatible" filenames</param>
    /// ///
    /// <param name="folderEnterEpoch">
    ///     This is for session-checking -> if the user was to move folders while the call is
    ///     executing and the new folder has identical file names w/o this the wrong data could show
    /// </param>
    /// <returns>
    ///     In practice, nothing but it's responsible for sending the updated exif info back to the requester (usually a
    ///     listview)
    /// </returns>
    internal static async Task ExifGetExifFromFilesIncompatibleFileNames(List<string> files,
                                                                         long folderEnterEpoch)
    {
        #region ExifToolConfiguration

        //basically if the form gets minimised it can turn to null methinks. 
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (FrmMainAppInstance != null)
        {
            string exifToolExe = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe");

            string folderNameToUse = FrmMainAppInstance.tbx_FolderName.Text;
            string argsFile = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: "exifArgs.args");
            string exiftoolCmd = " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 -w! " + s_doubleQuote + FrmMainApp.UserDataFolderPath + @"\%F.txt" + s_doubleQuote + " -@ " + s_doubleQuote + argsFile + s_doubleQuote;

            File.Delete(path: argsFile);

            List<string> exifArgs = new();
            // needs a space before and after.
            string commonArgs = " -a -s -s -struct -sort -G -ee -args ";

            #endregion

            // add required tags
            DataTable dt_objectTagNames = DataReadSQLiteObjectMappingTagsToPass();

            foreach (DataRow dr in dt_objectTagNames.Rows)
            {
                exifArgs.Add(item: dr[columnName: "objectTagName_ToPass"]
                                 .ToString());
            }

            foreach (string listElem in files)
            {
                if (File.Exists(path: Path.Combine(path1: folderNameToUse, path2: listElem)))
                {
                    File.AppendAllText(path: argsFile, contents: Path.Combine(path1: folderNameToUse, path2: listElem) + Environment.NewLine, encoding: Encoding.UTF8);
                    foreach (string arg in exifArgs)
                    {
                        File.AppendAllText(path: argsFile, contents: "-" + arg + Environment.NewLine);
                    }
                }

                //// add any xmp sidecar files
                if (File.Exists(path: Path.Combine(path1: folderNameToUse, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToUse, path2: listElem)) + ".xmp")))
                {
                    string sideCarXMPFilePath = Path.Combine(path1: folderNameToUse, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToUse, path2: listElem)) + ".xmp");
                    File.AppendAllText(path: argsFile, contents: sideCarXMPFilePath + Environment.NewLine, encoding: Encoding.UTF8);
                    foreach (string arg in exifArgs)
                    {
                        File.AppendAllText(path: argsFile, contents: "-" + arg + Environment.NewLine);
                    }
                }

                File.AppendAllText(path: argsFile, contents: "-ignoreMinorErrors" + Environment.NewLine);
                File.AppendAllText(path: argsFile, contents: "-progress" + Environment.NewLine);
            }

            File.AppendAllText(path: argsFile, contents: "-execute" + Environment.NewLine);
            ///////////////
            // via https://stackoverflow.com/a/68616297/3968494
            await Task.Run(action: () =>
            {
                using Process p = new();
                p.StartInfo = new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
                {
                    Arguments = @"/k " + s_doubleQuote + s_doubleQuote + Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe") + s_doubleQuote + " " + commonArgs + exiftoolCmd + s_doubleQuote + "&& exit",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                p.EnableRaisingEvents = true;

                s_ErrorMsg = "";
                p.OutputDataReceived += (_,
                                         data) =>
                {
                    if (data.Data != null && data.Data.Contains(value: "="))
                    {
                        FrmMainApp.HandlerUpdateLabelText(label: FrmMainAppInstance.lbl_ParseProgress, text: "Processing: " +
                                                                                                             data.Data.Split('[')
                                                                                                                 .First()
                                                                                                                 .Split('/')
                                                                                                                 .Last()
                                                                                                                 .Trim());
                    }
                    else if (data.Data != null && !data.Data.Contains(value: "files created") && !data.Data.Contains(value: "files read") && data.Data.Length > 0)
                    {
                        s_ErrorMsg += data.Data.ToString() + Environment.NewLine;
                        try
                        {
                            p.Kill();
                        }
                        catch
                        { } // else it will be stuck running forever
                    }
                };

                p.ErrorDataReceived += (_,
                                        data) =>
                {
                    if (data.Data != null && data.Data.Contains(value: "="))
                    {
                        FrmMainApp.HandlerUpdateLabelText(label: FrmMainAppInstance.lbl_ParseProgress, text: "Processing: " +
                                                                                                             data.Data.Split('[')
                                                                                                                 .First()
                                                                                                                 .Split('/')
                                                                                                                 .Last()
                                                                                                                 .Trim());
                    }
                    else if (data.Data != null && !data.Data.Contains(value: "files created") && !data.Data.Contains(value: "files read") && data.Data.Length > 0)
                    {
                        s_ErrorMsg += data.Data.ToString() + Environment.NewLine;
                        try
                        {
                            p.Kill();
                        }
                        catch
                        { } // else it will be stuck running forever
                    }
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                p.Close();
                if (s_ErrorMsg != "")
                {
                    MessageBox.Show(text: s_ErrorMsg);
                }

                // if still here then exorcise
                try
                {
                    p.Kill();
                }
                catch
                { }
            });
            ///////////////

            // try to collect the txt files and then read them back into the listview.
            try
            {
                foreach (string itemText in files)
                {
                    string exifFileIn = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: itemText + ".txt");
                    if (File.Exists(path: exifFileIn))
                    {
                        DataTable dt_fileExifTable = new();
                        dt_fileExifTable.Clear();
                        dt_fileExifTable.Columns.Add(columnName: "TagName");
                        dt_fileExifTable.Columns.Add(columnName: "TagValue");
                        foreach (string exifTxtFileLineIn in File.ReadLines(path: exifFileIn))
                        {
                            string exifTagName = exifTxtFileLineIn.Split('=')[0]
                                .Substring(startIndex: 1);
                            string exifTagVal = exifTxtFileLineIn.Split('=')[1];

                            DataRow dr = dt_fileExifTable.NewRow();
                            dr[columnName: "TagName"] = exifTagName;
                            dr[columnName: "TagValue"] = exifTagVal;
                            dt_fileExifTable.Rows.Add(row: dr);
                        }

                        // see if there's an xmp-output too
                        string sideCarXMPFilePath = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: itemText.Substring(startIndex: 0, length: itemText.LastIndexOf(value: '.')) + ".xmp" + ".txt");
                        if (File.Exists(path: sideCarXMPFilePath))
                        {
                            foreach (string exifTxtFileLineIn in File.ReadLines(path: sideCarXMPFilePath))
                            {
                                string exifTagName = exifTxtFileLineIn.Split('=')[0]
                                    .Substring(startIndex: 1);
                                string exifTagVal = exifTxtFileLineIn.Split('=')[1];

                                DataRow dr = dt_fileExifTable.NewRow();
                                dr[columnName: "TagName"] = exifTagName;
                                dr[columnName: "TagValue"] = exifTagVal;
                                dt_fileExifTable.Rows.Add(row: dr);
                            }
                        }

                        // de-dupe. this is pretty poor performance but the dataset is small
                        DataTable dt_distinctFileExifTable = dt_fileExifTable.DefaultView.ToTable(distinct: true);

                        if (folderEnterLastEpoch == folderEnterEpoch)
                        {
                            ListViewItem lvi = FrmMainAppInstance.lvw_FileList.FindItemWithText(text: itemText);

                            ListView.ColumnHeaderCollection lvchs = FrmMainAppInstance.ListViewColumnHeaders;
                            for (int i = 1; i < lvi.SubItems.Count; i++)
                            {
                                string str = ExifGetStandardisedDataPointFromExif(dtFileExif: dt_distinctFileExifTable, dataPoint: lvchs[index: i]
                                                                                      .Name.Substring(startIndex: 4));
                                lvi.SubItems[index: i]
                                    .Text = str;
                            }

                            lvi.ForeColor = Color.Black;
                        }

                        File.Delete(path: exifFileIn); // clean up
                    }
                }
            }
            catch
            {
                // nothing. errors should have already come up
            }
        }
    }

    /// <summary>
    ///     Fires off a command to try to parse Track files and link them up with data in the main grid
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
        s_ErrorMsg = "";
        s_OutputMsg = "";
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
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        string folderNameToUse = FrmMainAppInstance.tbx_FolderName.Text;
        string exiftoolCmd = " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 ";

        List<string> exifArgs = new();
        // needs a space before and after.
        string commonArgs = " -a -s -s -struct -sort -G -ee -args ";

        #endregion

        // add track files
        foreach (string trackFile in trackFileList)
        {
            exiftoolCmd += " -geotag=" + s_doubleQuote + trackFile + s_doubleQuote;
        }

        // add what to compare against + TZ
        string tmpTZAdjust = s_doubleQuote;
        if (useTZAdjust)
        {
            tmpTZAdjust = TZVal + s_doubleQuote;
        }

        exiftoolCmd += " " + s_doubleQuote + "-geotime<${" + compareTZAgainst + "#}" + tmpTZAdjust;

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

        // add "what folder to act upon"
        exiftoolCmd += " " + s_doubleQuote + FrmMainAppInstance.tbx_FolderName.Text.TrimEnd('\\') + s_doubleQuote;

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

        exiftoolCmd += " " + " -srcfile " + s_doubleQuote + tmpFolder + @"\%F.xmp" + s_doubleQuote;
        exiftoolCmd += " -overwrite_original_in_place";

        ///////////////
        // via https://stackoverflow.com/a/68616297/3968494
        await Task.Run(action: () =>
        {
            using Process p = new();
            p.StartInfo = new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
            {
                Arguments = @"/k " + s_doubleQuote + s_doubleQuote + Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe") + s_doubleQuote + " " + commonArgs + exiftoolCmd + s_doubleQuote + "&& exit",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            p.EnableRaisingEvents = true;

            //s_ErrorMsg = "";
            //s_OutputMsg = "";
            p.OutputDataReceived += (_,
                                     data) =>
            {
                if (data.Data != null && data.Data.Length > 0)
                {
                    s_OutputMsg += data.Data.ToString() + Environment.NewLine;
                }
            };

            p.ErrorDataReceived += (_,
                                    data) =>
            {
                if (data.Data != null && data.Data.Length > 0)
                {
                    s_ErrorMsg += data.Data.ToString() + Environment.NewLine;
                    try
                    {
                        p.Kill();
                    }
                    catch
                    { } // else it will be stuck running forever
                }
            };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            p.Close();
            if (s_ErrorMsg != "")
            {
                //MessageBox.Show(s_ErrorMsg);
            }

            // if still here then exorcise
            try
            {
                p.Kill();
            }
            catch
            { }
        });

        ///////////////
        //// try to collect the xmp/xml files and then read them back into the listview.

        foreach (FileInfo exifFileIn in di_tmpLocFiles.EnumerateFiles())
        {
            if (exifFileIn.Extension == ".xmp")
            {
                try
                {
                    string xml = File.ReadAllText(path: Path.Combine(path1: tmpFolder, path2: exifFileIn.Name));

                    XmlSerializer serializer = new(type: typeof(xmpmeta));
                    xmpmeta trackFileXMLData;
                    using (StringReader reader = new(s: xml))
                    {
                        trackFileXMLData = (xmpmeta)serializer.Deserialize(textReader: reader);
                        //trackFileXMLData.RDF.Description.GPSAltitude
                    }

                    if (trackFileXMLData != null)
                    {
                        DataTable dt_fileExifTable = new();
                        dt_fileExifTable.Clear();
                        dt_fileExifTable.Columns.Add(columnName: "TagName");
                        dt_fileExifTable.Columns.Add(columnName: "TagValue");

                        PropertyInfo[] props = typeof(RDFDescription).GetProperties(bindingAttr: BindingFlags.Instance | BindingFlags.Public);

                        foreach (PropertyInfo trackData in props)
                        {
                            string TagName = "exif:" + trackData.Name;
                            object TagValue = trackData.GetValue(obj: trackFileXMLData.RDF.Description);

                            DataRow dr = dt_fileExifTable.NewRow();
                            dr[columnName: "TagName"] = TagName;
                            dr[columnName: "TagValue"] = TagValue;
                            dt_fileExifTable.Rows.Add(row: dr);
                        }

                        // de-dupe. this is pretty poor performance but the dataset is small
                        DataTable dt_distinctFileExifTable = dt_fileExifTable.DefaultView.ToTable(distinct: true);

                        ListView lvw = FrmMainAppInstance.lvw_FileList;
                        ListViewItem lvi = FrmMainAppInstance.lvw_FileList.FindItemWithText(text: exifFileIn.Name.Substring(startIndex: 0, length: exifFileIn.Name.Length - 4));

                        if (lvi != null)
                        {
                            ListView.ColumnHeaderCollection lvchs = FrmMainAppInstance.ListViewColumnHeaders;
                            string[] toponomyChangers = { "GPSLatitude", "GPSLongitude" };
                            string[] toponomyDeletes = { "CountryCode", "Country", "City", "State", "Sub_location" };
                            string strParsedLat = "0.0";
                            string strParsedLng = "0.0";
                            bool coordinatesHaveChanged = false;

                            for (int i = 1; i < lvi.SubItems.Count; i++)
                            {
                                string tagToWrite = lvchs[index: i]
                                    .Name.Substring(startIndex: 4);
                                string str = ExifGetStandardisedDataPointFromExif(dtFileExif: dt_distinctFileExifTable, dataPoint: tagToWrite);
                                FrmMainApp.HandlerUpdateLabelText(label: FrmMainAppInstance.lbl_ParseProgress, text: "Processing: " + lvi.Text);

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
                                            filePath: lvi.Text,
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

                                        lvi.ForeColor = Color.Red;
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
                                        filePath: lvi.Text,
                                        settingId: category,
                                        settingValue: "-"
                                    );

                                    lvi.SubItems[index: lvw.Columns[key: "clh_" + category]
                                                     .Index]
                                        .Text = "-";
                                }

                                // pull from web
                                s_APIOkay = true;
                                DataTable dt_Toponomy = DTFromAPIExifGetToponomyFromWebOrSQL(lat: strParsedLat.ToString(provider: CultureInfo.InvariantCulture), lng: strParsedLng.ToString(provider: CultureInfo.InvariantCulture));

                                if (s_APIOkay)
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
                                            filePath: lvi.Text,
                                            settingId: toponomyDetail.toponomyOverwriteName,
                                            settingValue: toponomyDetail.toponomyOverwriteVal
                                        );
                                        lvi.SubItems[index: lvw.Columns[key: "clh_" + toponomyDetail.toponomyOverwriteName]
                                                         .Index]
                                            .Text = toponomyDetail.toponomyOverwriteVal;
                                    }

                                    lvi.ForeColor = Color.Red;
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

        DialogResult dialogResult = MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_FrmImportGpx_AskUserWantsReport"), caption: "Info", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Question);
        if (dialogResult == DialogResult.Yes)
        {
            Form reportBox = new();

            reportBox.ControlBox = false;
            FlowLayoutPanel panel = new();

            TextBox tbxText = new();
            tbxText.Size = new Size(width: 700, height: 400);

            tbxText.Text = s_OutputMsg;
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
    internal static async Task<decimal> ExifGetExifToolVersion()
    {
        string exifToolResult;
        decimal returnVal = 0.0m;
        bool parsedResult;
        decimal parsedDecimal = 0.0m;

        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
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
            exifToolResult = await FrmMainAppInstance.AsyncExifTool.ExecuteAsync(args: exifToolCommand);
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
        ;

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
    ///     This generates (technically, extracts) the image previews from files for the user when they click on a filename
    ///     ... in whichever listview.
    /// </summary>
    /// <param name="fileName">Path of file for which the preview needs creating</param>
    /// <returns>Realistically nothing but the process generates the bitmap if possible</returns>
    internal static async Task ExifGetImagePreviews(string fileName)
    {
        #region ExifToolConfiguration

        string exifToolExe = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe");

        // want to give this a different name from the usual exifArgs.args just in case that's still being accessed (as much as it shouldn't be)
        Regex rgx = new(pattern: "[^a-zA-Z0-9]");
        string fileNameReplaced = rgx.Replace(input: fileName.Replace(oldValue: FrmMainApp.FolderName, newValue: ""), replacement: "_");
        string argsFile = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: "exifArgs_getPreview_" + fileNameReplaced + ".args");
        string exiftoolCmd = " -charset utf8 -charset filename=utf8 -b -preview:GTNPreview -w! " + s_doubleQuote + FrmMainApp.UserDataFolderPath + @"\%F.jpg" + s_doubleQuote + " -@ " + s_doubleQuote + argsFile + s_doubleQuote;

        File.Delete(path: argsFile);

        #endregion

        // add required tags

        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        if (File.Exists(path: fileName))
        {
            File.AppendAllText(path: argsFile, contents: fileName + Environment.NewLine, encoding: Encoding.UTF8);
            File.AppendAllText(path: argsFile, contents: "-execute" + Environment.NewLine);
        }

        ///////////////
        // via https://stackoverflow.com/a/68616297/3968494
        await Task.Run(action: () =>
        {
            using Process p = new();
            p.StartInfo = new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
            {
                Arguments = @"/k " + s_doubleQuote + s_doubleQuote + Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe") + s_doubleQuote + " " + exiftoolCmd + s_doubleQuote + "&& exit",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            p.EnableRaisingEvents = true;
            p.OutputDataReceived += (_,
                                     data) =>
            {
                // don't care
            };

            p.ErrorDataReceived += (_,
                                    data) =>
            {
                // don't care
            };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            p.Close();
            // if still here then exorcise
            try
            {
                p.Kill();
            }
            catch
            { }
        });
        ///////////////
    }

    /// <summary>
    ///     Writes outstanding exif changes to the files (all files in the queue).
    ///     This logic is very similar to the "incompatible read" above - it's safer. While it's also probably slower
    ///     ... the assumption is that users will read a lot of files but will write proportionately fewer files so
    ///     ... speed is less of an essence against safety.
    /// </summary>
    /// <returns>Reastically nothing but writes the exif tags and updates the listview rows where necessary</returns>
    internal static async Task ExifWriteExifToFile()
    {
        string exifArgs = "";
        string argsFile = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: "exifArgsToWrite.args");
        string exiftoolCmd = " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8" + " -@ " + s_doubleQuote + argsFile + s_doubleQuote;
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        // if user switches folder in the process of writing this will keep it standard
        string folderNameToWrite = FrmMainAppInstance.tbx_FolderName.Text;
        File.Delete(path: argsFile);

        bool writeXMLTags = false;
        bool writeXMLSideCar = false;
        bool overwriteOriginal = false;

        // get tag names
        DataTable dt_objectTagNames_Out = GenericJoinDataTables(t1: FrmMainApp.ObjectNames, t2: FrmMainApp.ObjectTagNamesOut,
                                                                (row1,
                                                                 row2) =>
                                                                    row1.Field<string>(columnName: "objectName") == row2.Field<string>(columnName: "objectName"));

        DataView dv_FileNames = new(table: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite);
        DataTable dt_DistinctFileNames = dv_FileNames.ToTable(distinct: true, "filePath");

        // check there's anything to write.
        foreach (DataRow dr_FileName in dt_DistinctFileNames.Rows)
        {
            string fileName = dr_FileName[columnIndex: 0]
                .ToString();

            List<string> tagsToDelete = new(); // this needs to be injected into the sidecar if req'd

            exifArgs += Path.Combine(path1: folderNameToWrite, path2: fileName) + Environment.NewLine; //needs to include folder name
            exifArgs += "-ignoreMinorErrors" + Environment.NewLine;
            exifArgs += "-progress" + Environment.NewLine;

            DataView dv_FileWriteQueue = new(table: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite);
            dv_FileWriteQueue.RowFilter = "filePath = '" + fileName + "'";

            if (dv_FileWriteQueue.Count > 0)
            {
                // get tags for this file
                DataTable dt_FileWriteQueue = dv_FileWriteQueue.ToTable();
                DataTable dt_objectTagNames_OutWithData = GenericJoinDataTables(t1: dt_objectTagNames_Out, t2: dt_FileWriteQueue,
                                                                                (row1,
                                                                                 row2) =>
                                                                                    row1.Field<string>(columnName: "objectName") == row2.Field<string>(columnName: "settingId"));
                string fileExt = Path.GetExtension(path: fileName)
                    .Substring(startIndex: 1);

                string exiftoolTagName;
                string updateExifVal;

                writeXMLTags = Convert.ToBoolean(value: DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExt.ToLower() + "_" + "ckb_AddXMPIntoFile"));
                ;
                writeXMLSideCar = Convert.ToBoolean(value: DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExt.ToLower() + "_" + "ckb_AddXMPSideCar"));
                ;
                overwriteOriginal = Convert.ToBoolean(value: DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExt.ToLower() + "_" + "ckb_OverwriteOriginal"));

                bool deleteAllGPSData = false;
                bool deleteTagAlreadyAdded = false;
                // add tags to argsFile
                foreach (DataRow row in dt_objectTagNames_OutWithData.Rows)
                {
                    // this is prob not the best way to go around this....
                    foreach (DataRow drFileTags in dt_FileWriteQueue.Rows)
                    {
                        if (drFileTags[columnIndex: 1]
                                .ToString() ==
                            @"gps*")
                        {
                            deleteAllGPSData = true;
                            break;
                        }
                    }

                    // non-xml always
                    if (deleteAllGPSData && !deleteTagAlreadyAdded)
                    {
                        exifArgs += "-gps*=" + Environment.NewLine;
                        tagsToDelete.Add(item: "gps*");

                        // this is moved up/in here because the deletion of all gps has to come before just about anything else in case user wants to add (rather than delete) in more tags (later).
                        if (writeXMLTags)
                        {
                            exifArgs += "-xmp:gps*=" + Environment.NewLine;
                        }

                        deleteTagAlreadyAdded = true;
                    }

                    if (!row[columnName: "objectTagName_Out"]
                            .ToString()
                            .Contains(value: ":"))
                    {
                        exiftoolTagName = row[columnName: "objectTagName_Out"]
                            .ToString();
                        updateExifVal = row[columnName: "settingValue"]
                            .ToString();
                        if (updateExifVal != "")
                        {
                            exifArgs += "-" + exiftoolTagName + "=" + updateExifVal + Environment.NewLine;
                            //if lat/long then add Ref. 
                            if (exiftoolTagName == "GPSLatitude" || exiftoolTagName == "GPSDestLatitude")
                            {
                                if (updateExifVal.Substring(startIndex: 0, length: 1) == "-")
                                {
                                    exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "South" + Environment.NewLine;
                                }
                                else
                                {
                                    exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "North" + Environment.NewLine;
                                }
                            }
                            else if (exiftoolTagName == "GPSLongitude" || exiftoolTagName == "GPSDestLongitude")
                            {
                                if (updateExifVal.Substring(startIndex: 0, length: 1) == "-")
                                {
                                    exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "West" + Environment.NewLine;
                                }
                                else
                                {
                                    exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "East" + Environment.NewLine;
                                }
                            }
                        }
                        else //delete tag
                        {
                            exifArgs += "-" + exiftoolTagName + "=" + Environment.NewLine;
                            tagsToDelete.Add(item: exiftoolTagName);
                        }
                    }

                    // xml only if needed
                    if (writeXMLTags)
                    {
                        if (row[columnName: "objectTagName_Out"]
                            .ToString()
                            .Contains(value: ":"))
                        {
                            exiftoolTagName = row[columnName: "objectTagName_Out"]
                                .ToString();
                            updateExifVal = row[columnName: "settingValue"]
                                .ToString();
                            if (updateExifVal != "")
                            {
                                exifArgs += "-" + exiftoolTagName + "=" + updateExifVal + Environment.NewLine;
                                //if lat/long then add Ref. 
                                if (exiftoolTagName == "exif:GPSLatitude" || exiftoolTagName == "exif:GPSDestLatitude")
                                {
                                    if (updateExifVal.Substring(startIndex: 0, length: 1) == "-")
                                    {
                                        exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "South" + Environment.NewLine;
                                    }
                                    else
                                    {
                                        exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "North" + Environment.NewLine;
                                    }
                                }
                                else if (exiftoolTagName == "exif:GPSLongitude" || exiftoolTagName == "exif:GPSDestLongitude")
                                {
                                    if (updateExifVal.Substring(startIndex: 0, length: 1) == "-")
                                    {
                                        exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "West" + Environment.NewLine;
                                    }
                                    else
                                    {
                                        exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "East" + Environment.NewLine;
                                    }
                                }
                            }
                            else // delete tag
                            {
                                exifArgs += "-" + exiftoolTagName + "=" + Environment.NewLine;
                                tagsToDelete.Add(item: exiftoolTagName);

                                //if lat/long then add Ref. 
                                if (exiftoolTagName == "GPSLatitude" || exiftoolTagName == "GPSDestLatitude" || exiftoolTagName == "GPSLongitude" || exiftoolTagName == "GPSDestLongitude")
                                {
                                    exifArgs += "-" + exiftoolTagName + "Ref" + "=" + Environment.NewLine;
                                    tagsToDelete.Add(item: exiftoolTagName + "Ref");
                                }
                            }
                        }
                    }
                }

                if (overwriteOriginal)
                {
                    exifArgs += "-overwrite_original_in_place" + Environment.NewLine;
                }

                exifArgs += "-iptc:codedcharacterset=utf8" + Environment.NewLine;
            }

            exifArgs += "-execute" + Environment.NewLine;
            // sidecar copying needs to be in a separate batch, as technically it's a different file
            if (writeXMLSideCar)
            {
                exifArgs += Path.Combine(path1: folderNameToWrite, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToWrite, path2: fileName)) + ".xmp") + Environment.NewLine; //needs to include folder name
                exifArgs += "-progress" + Environment.NewLine;
                exifArgs += "-ignoreMinorErrors" + Environment.NewLine;
                // problem here is that tagsfromFile only ADDS tags but doesn't REMOVE anything.
                exifArgs += "-tagsfromfile=" + Path.Combine(path1: folderNameToWrite, path2: fileName) + Environment.NewLine;
                foreach (string tagToDelete in tagsToDelete)
                {
                    exifArgs += "-" + tagToDelete + "=" + Environment.NewLine;
                }

                if (overwriteOriginal)
                {
                    exifArgs += "-overwrite_original_in_place" + Environment.NewLine;
                }

                exifArgs += "-execute" + Environment.NewLine;
            }
        }

        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
        if (exifArgs != "")
        {
            File.AppendAllText(path: argsFile, contents: exifArgs, encoding: Encoding.UTF8);

            ///////////////
            // via https://stackoverflow.com/a/68616297/3968494
            await Task.Run(action: () =>
            {
                using Process p = new();
                p.StartInfo = new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
                {
                    Arguments = @"/k " + s_doubleQuote + s_doubleQuote + Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe") + s_doubleQuote + " " + exiftoolCmd + s_doubleQuote + "&& exit",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                p.EnableRaisingEvents = true;

                //string messageStart = "Writing Exif - ";
                p.OutputDataReceived += (_,
                                         data) =>
                {
                    if (data.Data != null && data.Data.Contains(value: "="))
                    {
                        string thisFileName = data.Data.Replace(oldValue: "=", newValue: "")
                            .Split('[')
                            .First()
                            .Trim()
                            .Split('/')
                            .Last();
                        FrmMainApp.HandlerUpdateLabelText(label: FrmMainAppInstance.lbl_ParseProgress, text: "Processing: " + thisFileName);
                        try
                        {
                            FrmMainApp.HandlerUpdateItemColour(lvw: FrmMainAppInstance.lvw_FileList, item: thisFileName, color: Color.Black);
                        }
                        catch
                        { }
                    }
                    else if (data.Data != null && !data.Data.Contains(value: "files updated") && !data.Data.Contains(value: "files created") && data.Data.Length > 0)
                    {
                        MessageBox.Show(text: data.Data);
                    }
                };

                p.ErrorDataReceived += (_,
                                        data) =>
                {
                    if (data.Data != null && data.Data.Contains(value: "="))
                    {
                        string thisFileName = data.Data.Replace(oldValue: "=", newValue: "")
                            .Split('[')
                            .First()
                            .Trim()
                            .Split('/')
                            .Last();
                        FrmMainApp.HandlerUpdateLabelText(label: FrmMainAppInstance.lbl_ParseProgress, text: "Processing: " + thisFileName);
                        try
                        {
                            FrmMainApp.HandlerUpdateItemColour(lvw: FrmMainAppInstance.lvw_FileList, item: thisFileName, color: Color.Black);
                        }
                        catch
                        { }
                    }
                    else if (data.Data != null && !data.Data.Contains(value: "files updated") && data.Data.Length > 0)
                    {
                        MessageBox.Show(text: data.Data);
                    }
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                p.Close();
                // if still here then exorcise
                try
                {
                    p.Kill();
                }
                catch
                { }
            });
            ///////////////
            FrmMainApp.HandlerUpdateLabelText(label: FrmMainAppInstance.lbl_ParseProgress, text: "Ready.");
        }
    }

    /// <summary>
    ///     Responsible for pulling the toponomy response for the API
    /// </summary>
    /// <param name="latitude">As on the tin.</param>
    /// <param name="longitude">As on the tin.</param>
    /// <returns>Structured toponomy response</returns>
    internal static GeoResponseToponomy API_ExifGetGeoDataFromWebToponomy(string latitude,
                                                                          string longitude)
    {
        if (s_GeoNames_UserName == null)
        {
            try
            {
                s_GeoNames_UserName = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                s_GeoNames_Pwd = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
            }
            catch (Exception ex)
            {
                MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorCantReadDefaultSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            }
        }

        GeoResponseToponomy returnVal = new();
        RestClient client = new(baseUrl: "http://api.geonames.org/")
        {
            Authenticator = new HttpBasicAuthenticator(username: s_GeoNames_UserName, password: s_GeoNames_Pwd)
        };

        RestRequest request_Toponomy = new(resource: "findNearbyPlaceNameJSON?lat=" + latitude + "&lng=" + longitude + "&style=FULL");
        RestResponse response_Toponomy = client.ExecuteGet(request: request_Toponomy);
        // check API reponse is OK
        if (response_Toponomy.StatusCode.ToString() == "OK")
        {
            s_APIOkay = true;
            JObject data = (JObject)JsonConvert.DeserializeObject(value: response_Toponomy.Content);
            GeoResponseToponomy geoResponse_Toponomy = GeoResponseToponomy.FromJson(Json: data.ToString());
            returnVal = geoResponse_Toponomy;
        }
        else
        {
            s_APIOkay = false;
            MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") + response_Toponomy.StatusCode, caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }

        return returnVal;
    }

    /// <summary>
    ///     Responsible for pulling the altitude response for the API
    /// </summary>
    /// <param name="latitude">As on the tin.</param>
    /// <param name="longitude">As on the tin.</param>
    /// <returns>Structured altitude response</returns>
    internal static GeoResponseAltitude API_ExifGetGeoDataFromWebAltitude(string latitude,
                                                                          string longitude)
    {
        if (s_GeoNames_UserName == null)
        {
            try
            {
                s_GeoNames_UserName = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                s_GeoNames_Pwd = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
            }
            catch (Exception ex)
            {
                MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorCantReadDefaultSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            }
        }

        GeoResponseAltitude returnVal = new();
        RestClient client = new(baseUrl: "http://api.geonames.org/")
        {
            Authenticator = new HttpBasicAuthenticator(username: s_GeoNames_UserName, password: s_GeoNames_Pwd)
        };

        RestRequest request_Altitude = new(resource: "srtm1JSON?lat=" + latitude + "&lng=" + longitude);
        RestResponse response_Altitude = client.ExecuteGet(request: request_Altitude);
        // check API reponse is OK
        if (response_Altitude.StatusCode.ToString() == "OK")
        {
            s_APIOkay = true;
            JObject data = (JObject)JsonConvert.DeserializeObject(value: response_Altitude.Content);
            GeoResponseAltitude geoResponseAltitude = GeoResponseAltitude.FromJson(Json: data.ToString());
            returnVal = geoResponseAltitude;
        }
        else
        {
            s_APIOkay = false;
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

            ;
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
    internal static GtnReleasesApiResponse API_GenericGetGTNVersionFromWeb()
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
            s_APIOkay = true;
            JArray data = (JArray)JsonConvert.DeserializeObject(value: response_GTNVersionQuery.Content);
            GtnReleasesApiResponse[] gtnReleasesApiResponse = GtnReleasesApiResponse.FromJson(json: data.ToString());
            returnVal = gtnReleasesApiResponse[0]; // latest only
        }
        else
        {
            s_APIOkay = false;
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

        /// logic: it's likely that API info doesn't change much...ever. 
        /// Given that in 2022 Crimea is still part of Ukraine according to API response I think this data is static
        /// ....so no need to query stuff that may already be locally available.
        /// FYI this only gets amended when this button gets pressed or if map-to-location button gets pressed on the main form.
        /// ... also on an unrelated note Toponomy vs Toponmy the API calls it one thing and Wikipedia calls it another so go figure
        DataTable dt_ToponomyInSQL = DataReadSQLiteToponomyWholeRow(
            lat: lat.ToString(provider: CultureInfo.InvariantCulture),
            lng: lng.ToString(provider: CultureInfo.InvariantCulture)
        );

        GeoResponseToponomy ReadJsonToponomy;

        string CountryCode = "";
        string Country = "";
        string City = "";
        string State = "";
        string Sub_location = "";

        // read from SQL
        if (dt_ToponomyInSQL.Rows.Count > 0)
        {
            CountryCode = dt_ToponomyInSQL.Rows[index: 0][columnName: "CountryCode"]
                .ToString();
            Country = DataReadSQLiteCountryCodesNames(
                queryWhat: "ISO_3166_1A3",
                inputVal: CountryCode,
                returnWhat: "Country"
            );
            if (CountryCode == "GBR")
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
        else if (s_APIOkay)
        {
            ReadJsonToponomy = API_ExifGetGeoDataFromWebToponomy(
                latitude: lat,
                longitude: lng
            );
            // ignore if unauthorised or some such
            if (ReadJsonToponomy.Geonames != null)
            {
                if (ReadJsonToponomy.Geonames.Length > 0)
                {
                    string APICountryCode = ReadJsonToponomy.Geonames[0]
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
                    if (CountryCode == "GBR")
                    {
                        City = ReadJsonToponomy.Geonames[0]
                            .AdminName2;
                        State = ReadJsonToponomy.Geonames[0]
                            .AdminName1;
                        Sub_location = ReadJsonToponomy.Geonames[0]
                            .ToponymName;
                    }
                    else
                    {
                        City = ReadJsonToponomy.Geonames[0]
                            .ToponymName;
                        State = ReadJsonToponomy.Geonames[0]
                            .AdminName1;
                        Sub_location = ReadJsonToponomy.Geonames[0]
                            .AdminName2;
                    }

                    // write back to sql the new stuff
                    DataWriteSQLiteToponomyWholeRow(
                        lat: lat,
                        lng: lng,
                        AdminName1: ReadJsonToponomy.Geonames[0]
                            .AdminName1,
                        AdminName2: ReadJsonToponomy.Geonames[0]
                            .AdminName2,
                        ToponymName: ReadJsonToponomy.Geonames[0]
                            .ToponymName,
                        CountryCode: CountryCode
                    );
                }
                else if (s_APIOkay)
                {
                    // write back empty
                    DataWriteSQLiteToponomyWholeRow(
                        lat: lat,
                        lng: lng
                    );
                }
            }
        }

        if (s_APIOkay || dt_ToponomyInSQL.Rows.Count > 0)
        {
            DataRow dr_ReturnRow = dt_Return.NewRow();
            dr_ReturnRow[columnName: "CountryCode"] = CountryCode;
            dr_ReturnRow[columnName: "Country"] = Country;
            dr_ReturnRow[columnName: "City"] = City;
            dr_ReturnRow[columnName: "State"] = State;
            dr_ReturnRow[columnName: "Sub_location"] = Sub_location;
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
        else if (s_APIOkay)
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
            if (readJson_Altitude.Lat == null && s_APIOkay)
            {
                // write back blank
                DataWriteSQLiteAltitudeWholeRow(
                    lat: lat,
                    lng: lng
                );
            }
        }

        if (s_APIOkay || dt_AltitudeInSQL.Rows.Count > 0)
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
    internal static DataTable DTFromAPI_GetGTNVersion()
    {
        DataTable dt_Return = new();
        dt_Return.Clear();
        dt_Return.Columns.Add(columnName: "version");

        string apiVersion = "";

        if (s_APIOkay)
        {
            GtnReleasesApiResponse readJson_GTNVer = API_GenericGetGTNVersionFromWeb();
            if (readJson_GTNVer.TagName != null)
            {
                apiVersion = readJson_GTNVer.TagName;
            }
            // this will be a null value if Unauthorised, we'll ignore that.
        }

        if (s_APIOkay)
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
        FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        foreach (ListViewItem selectedItem in FrmMainAppInstance.lvw_FileList.SelectedItems)
        {
            // only deal with files, not folders
            if (File.Exists(path: Path.Combine(path1: FrmMainAppInstance.tbx_FolderName.Text, path2: selectedItem.Text)))
            {
                overallCount++;
                FrmMainApp.FolderName = FrmMainAppInstance.tbx_FolderName.Text;
                FrmEditFileData.lvw_FileListEditImages.Items.Add(text: selectedItem.Text);
                fileCount++;
            }
            else if (Directory.Exists(path: Path.Combine(path1: FrmMainAppInstance.tbx_FolderName.Text, path2: selectedItem.Text)))
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
    ///     Queues up a command to remove existing geo-data. Depending on the sender this can be for one or many files.
    /// </summary>
    /// <param name="senderName">At this point this can either be the main listview or the one from Edit (file) data</param>
    internal static void ExifRemoveLocationData(string senderName)
    {
        if (senderName == "FrmEditFileData")
        {
            // for the time being i'll leave this as "remove data from the active selection file" rather than "all".
            FrmEditFileData FrmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];

            // setting this to True prevents the code from checking the values are valid numbers.
            FrmEditFileData.FrmEditFileDataNowRemovingGeoData = true;
            FrmEditFileDataInstance.tbx_GPSLatitude.Text = "";
            FrmEditFileDataInstance.tbx_GPSLongitude.Text = "";
            FrmEditFileDataInstance.tbx_GPSAltitude.Text = "";
            FrmEditFileDataInstance.tbx_City.Text = "";
            FrmEditFileDataInstance.tbx_State.Text = "";
            FrmEditFileDataInstance.tbx_Sub_location.Text = "";
            FrmEditFileDataInstance.cbx_CountryCode.Text = "";
            FrmEditFileDataInstance.cbx_Country.Text = "";
            FrmEditFileData.FrmEditFileDataNowRemovingGeoData = false;
            // no need to write back to sql because it's done automatically on textboxChange except for "special tag"

            GenericUpdateAddToDataTable(
                dt: FrmMainApp.DtFileDataToWriteStage1PreQueue,
                filePath: FrmEditFileDataInstance.lvw_FileListEditImages.SelectedItems[index: 0]
                    .Text,
                settingId: "gps*",
                settingValue: ""
            );
        }
        else if (senderName == "FrmMainApp")
        {
            FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

            if (FrmMainAppInstance.lvw_FileList.SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in FrmMainAppInstance.lvw_FileList.SelectedItems)
                {
                    // don't do folders...
                    if (File.Exists(path: Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text)))
                    {
                        string[] toponomyOverwrites =
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

                        foreach (string toponomyDetail in toponomyOverwrites)
                        {
                            GenericUpdateAddToDataTable(
                                dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                                filePath: lvi.Text,
                                settingId: toponomyDetail,
                                settingValue: ""
                            );
                        }
                    }
                }
            }

            LwvUpdateRowFromDTWriteStage3ReadyToWrite();
        }
    }

    #endregion
}