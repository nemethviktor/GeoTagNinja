using NLog;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsDarkThemerNinja;

namespace GeoTagNinja.Helpers.Exif;

internal class WriteTrackDataToTrackFile
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Writes the track file (GPX or KML, depending on the extension of <paramref name="outFilePath" />) based on the settings.
    ///     The matching <c>out.fmt</c> must have been generated first (<c>GpxExportOptions</c> / <c>KmlExportOptions</c>).
    /// </summary>
    /// <param name="fileList">List (string) of files to be parsed/written data for.</param>
    /// <param name="outFilePath">Location of the GPX/KML file to save to.</param>
    /// <returns></returns>
    internal static async Task ExifWriteTrackDataToTrackFile(List<string> fileList,
                                                             string outFilePath)
    {
        // ensure path is valid
        if (!string.IsNullOrWhiteSpace(value: outFilePath) &&
            fileList.Count > 0)
        {
            Log.Info(message: "Starting");

            string argsFile = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                path2: "exifArgsToWriteForTrackExport.args");
            {
                if (File.Exists(path: argsFile))
                {
                    File.Delete(path: argsFile);
                }
            }

            if (File.Exists(path: outFilePath))
            {
                // also delete the output file if exists otherwise it will be appended.
                File.Delete(path: outFilePath);
            }

            string exiftoolCmd =
                $" -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 -W+ {HelperVariables.DoubleQuoteStr}{outFilePath}{HelperVariables.DoubleQuoteStr}  -@ {HelperVariables.DoubleQuoteStr}{argsFile}{HelperVariables.DoubleQuoteStr}";

            FrmMainApp frmMainAppInstance = FrmMainApp.Instance;

            // if user switches folder in the process of writing this will keep it standard
            Debug.Assert(condition: frmMainAppInstance != null, message: $"{nameof(frmMainAppInstance)} != null");

            // run file as exiftool -@ args.args -W+ out.gpx
            // as per https://exiftool.org/forum/index.php?topic=16222.0 the logic needs to be
            /*
                d:\temp\dests\_DSC1458.dng
                d:\temp\dests\_DSC1459.dng
                -p
                gpx.fmt
             */

            string exifArgsForOriginalFile = fileList
                                            .Where(predicate: File.Exists)
                                            .Aggregate(seed: "",
                                                 func: (current,
                                                        trackFilePath) =>
                                                     current + trackFilePath + Environment.NewLine);

            // -p
            exifArgsForOriginalFile += $"-p{Environment.NewLine}";
            // out.fmt (gpx or kml)
            exifArgsForOriginalFile += Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "out.fmt") +
                                       Environment.NewLine;

            exifArgsForOriginalFile += $"-overwrite_original_in_place{Environment.NewLine}";

            File.WriteAllText(path: argsFile, contents: exifArgsForOriginalFile, encoding: Encoding.UTF8);
            ///////////////
            await ExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                frmMainAppInstance: null,
                initiator:
                ExifToolInititators
                                                 .ExifGetTrackSyncDataWriteTrackPath);

            // ExifTool prints KML points as when/coord pairs; other software expects all whens, then all coords
            if (KmlExportOptions.IsKmlPath(outFilePath: outFilePath))
            {
                KmlExportOptions.GroupTrackPoints(outFilePath: outFilePath);
            }

            ///////////////

            Themer.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_FrmImportExportGpx_ExportCompleted",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                icon: MessageBoxIcon.Information,
                buttons: MessageBoxButtons.OK);
        }
        else
        {
            Themer.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_FrmImportExportGpx_FileOrFolderDoesntExist",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                icon: MessageBoxIcon.Error,
                buttons: MessageBoxButtons.OK);
        }
    }
}