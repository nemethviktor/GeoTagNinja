using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers;

internal class HelperExifReadTrackFilePath
{
    /// <summary>
    ///     This commands ExifTool to collect the track path. Very simple to run.
    ///     -> exiftool -v4 -geotag 2023-02-28.gpx NUL
    /// </summary>
    /// <param name="trackFileList">File(s) that contain the tracks to be omported/overlaid</param>
    /// <param name="overlayDateSetting">
    ///     Whether to do the overlay and if so then whether to respect the dates of selected
    ///     images
    /// </param>
    /// <param name="overlayDateList">List of dates (may be empty) that need to be included. If empty then include all.</param>
    /// <param name="TZVal">Timezone shift value</param>
    internal static async Task ExifReadTrackFileForMapping(List<string> trackFileList,
        HelperGenericAncillaryListsArrays.TrackOverlaySetting overlayDateSetting,
        List<DateTime> overlayDateList,
        string TZVal
    )
    {
        FrmMainApp.Log.Info(message: "Starting");

        string argsFile = Path.Combine(path1: HelperVariables.UserDataFolderPath,
            path2: "exifReadTrackFileForMapping.args");
        File.Delete(path: argsFile);

        string exiftoolCmd =
            $" -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 -@ {HelperVariables.DoubleQuoteStr}{argsFile}{HelperVariables.DoubleQuoteStr}";

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        // if user switches folder in the process of writing this will keep it standard
        Debug.Assert(condition: frmMainAppInstance != null, message: $"{nameof(frmMainAppInstance)} != null");
        string exifArgsForOriginalFile = "";

        // as per https://exiftool.org/forum/index.php?topic=16184.msg86958#msg86958 the logic needs to be
        /*
           -v4
           -geotag=D:\temp\2023-02-27.gpx
           NUL
         */

        exifArgsForOriginalFile += $"-v4{Environment.NewLine}";

        exifArgsForOriginalFile = trackFileList.Aggregate(seed: exifArgsForOriginalFile,
            func: (current, trackFilePath) => $"{current}-geotag={trackFilePath}{Environment.NewLine}");

        exifArgsForOriginalFile += $"NUL{Environment.NewLine}";

        File.WriteAllText(path: argsFile, contents: exifArgsForOriginalFile, encoding: Encoding.UTF8);
        ///////////////
        await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
            frmMainAppInstance: null,
            initiator:
            HelperGenericAncillaryListsArrays.ExifToolInititators
                                             .ExifGetTrackSyncDataReadTrackPath);

        ///////////////
        ReplaceLstTrackPathContents(trackOverlayDateSetting: overlayDateSetting, overlayDateList: overlayDateList,
            TZVal: TZVal);
    }

    private static void ReplaceLstTrackPathContents(
        HelperGenericAncillaryListsArrays.TrackOverlaySetting trackOverlayDateSetting,
        List<DateTime> overlayDateList,
        string TZVal)
    {
        HelperVariables.LstTrackPath.Clear(); // List<(string strLat, string strLng)>
        if (HelperVariables._sOutputAndErrorMsg.Contains(value: "GPS track start"))
        {
            try
            {
                // Example first two lines:
                //     2023:02:28 12:56:18.000 UTC - alt=20.625282 first=1 lat=38.71002621 lon=-9.13620249
                //     2023:02:28 12:56:20.000 UTC - alt=20.622257 lat=38.71003695 lon=-9.13616941
                // actually this is always in UTC format and always precisely like above.
                // tested in/on FI and HU VMs.

                foreach (string inputLine in HelperVariables._sOutputAndErrorMsg.Split(
                             separator: new[] { "\r\n", "\r", "\n" },
                             options: StringSplitOptions.None
                         ))
                {
                    if (inputLine.Contains(value: "lat=") &&
                        inputLine.Contains(value: "lon="))
                    {
                        string inputLineDateTimeStrVal = inputLine.Trim()
                                                                  .Split(separator: new[] { "UTC" },
                                                                       options: StringSplitOptions.None)[0];
                        // f...k me sideways.
                        if (!DateTime.TryParseExact(
                                s: inputLineDateTimeStrVal.Substring(startIndex: 0, length: 19),
                                format: "yyyy:MM:dd HH:mm:ss",
                                provider: CultureInfo.InvariantCulture,
                                style: DateTimeStyles.None,
                                result: out DateTime lineDateTime
                            ))
                        {
                            // I still can't quite fathom why we'd have a year:month:day format so in case the rest of the world agrees with me we retry it sensibly
                            DateTime.TryParseExact(
                                s: inputLineDateTimeStrVal.Substring(startIndex: 0, length: 19),
                                format: "yyyy-MM-dd HH:mm:ss",
                                provider: CultureInfo.InvariantCulture,
                                style: DateTimeStyles.None,
                                result: out lineDateTime
                            );
                        }

                        if (!string.IsNullOrWhiteSpace(value: TZVal))
                        {
                            // add the sucker to the datetime
                            // reminder TZVal format == "+01:00"
                            if (TZVal.StartsWith(value: "+"))
                            {
                                lineDateTime =
                                    lineDateTime.AddHours(
                                        value: double.Parse(s: TZVal.Substring(startIndex: 1, length: 2)));
                                lineDateTime =
                                    lineDateTime.AddMinutes(
                                        value: double.Parse(s: TZVal.Substring(startIndex: 4, length: 2)));
                            }
                            else if (TZVal.StartsWith(value: "-"))
                            {
                                lineDateTime = lineDateTime.AddHours(
                                    value: double.Parse(s: TZVal.Substring(startIndex: 1, length: 2)) * -1);
                                lineDateTime = lineDateTime.AddMinutes(
                                    value: double.Parse(s: TZVal.Substring(startIndex: 4, length: 2)) * -1);
                            }
                        }

                        string strLat = GetStringBetween(str: inputLine, firstString: "lat=",
                            lastString: " ");
                        string strLng = GetStringBetween(str: inputLine, firstString: "lon=",
                            lastString: Environment.NewLine);

                        // see if these are actually numbers. GetStringBetween is designed not to crash.
                        if (double.TryParse(s: strLat, style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture, result: out double latResult) &&
                            double.TryParse(s: strLng, style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture, result: out double lngResult) &&
                            (trackOverlayDateSetting ==
                             HelperGenericAncillaryListsArrays.TrackOverlaySetting.OverlayForAllDates ||
                             trackOverlayDateSetting ==
                             HelperGenericAncillaryListsArrays.TrackOverlaySetting.OverlayForOverlappingDates
                            ))
                        {
                            if (trackOverlayDateSetting == HelperGenericAncillaryListsArrays.TrackOverlaySetting
                                   .OverlayForAllDates)
                            {
                                HelperVariables.LstTrackPath.Add(item: (strLat, strLng));
                            }
                            else if (lineDateTime.Date >= overlayDateList[index: 0].Date &&
                                     lineDateTime.Date <= overlayDateList[index: 1].Date)
                            {
                                HelperVariables.LstTrackPath.Add(item: (strLat, strLng));
                            }
                        }
                    }
                }
            }
            catch
            {
                ShowTrackImportFailedMessageBox();
            }
        }
        else
        {
            ShowTrackImportFailedMessageBox();
        }

        return;

        static void ShowTrackImportFailedMessageBox()
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmImportExportGpx_TrackContainsNoData",
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Error, buttons: MessageBoxButtons.OK);
        }
    }

    /// <summary>
    ///     Gets a string between two others. If lastString is a newline then goes till the end of the original.
    /// </summary>
    /// <param name="str">String to analyse</param>
    /// <param name="firstString">Starting string</param>
    /// <param name="lastString">Ending string. If newline then returns the whole remaining string</param>
    /// <returns>The extracted string.</returns>
    private static string GetStringBetween(string str, string firstString, string lastString)
    {
        string finalString = str;
        try
        {
            int pos1 = str.IndexOf(value: firstString) + firstString.Length;
            int pos2 = 0;
            if (lastString == Environment.NewLine)
            {
                pos2 = str.Length;
            }
            else
            {
                pos2 = str.IndexOf(value: lastString, startIndex: pos1);
            }

            finalString = str.Substring(startIndex: pos1, length: pos2 - pos1);
        }
        catch
        {
            // nothing.
        }

        return finalString;
    }
}