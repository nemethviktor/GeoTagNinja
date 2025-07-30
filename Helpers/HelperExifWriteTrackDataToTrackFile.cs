using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers;

internal class HelperExifWriteTrackDataToTrackFile
{
    /// <summary>
    ///     Writes the gpx file based on the settings.
    /// </summary>
    /// <param name="fileList">List (string) of files to be parsed/written data for.</param>
    /// <param name="outFilePath">Location of the GPX file to save to.</param>
    /// <returns></returns>
    internal static async Task ExifWriteTrackDataToTrackFile(List<string> fileList,
                                                             string outFilePath)
    {
        // ensure path is valid
        if (!string.IsNullOrWhiteSpace(value: outFilePath) &&
            fileList.Count > 0)
        {
            FrmMainApp.Log.Info(message: "Starting");

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
                $" -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 -W+ {outFilePath}  -@ {HelperVariables.DoubleQuoteStr}{argsFile}{HelperVariables.DoubleQuoteStr}";

            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

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
                                            .Where(predicate: trackFilePath => File.Exists(path: trackFilePath))
                                            .Aggregate(seed: "",
                                                 func: (current,
                                                        trackFilePath) =>
                                                     current + trackFilePath + Environment.NewLine);

            // -p
            exifArgsForOriginalFile += $"-p{Environment.NewLine}";
            // gpx.fmt
            exifArgsForOriginalFile += Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "out.fmt") +
                                       Environment.NewLine;

            exifArgsForOriginalFile += $"-overwrite_original_in_place{Environment.NewLine}";

            File.WriteAllText(path: argsFile, contents: exifArgsForOriginalFile, encoding: Encoding.UTF8);
            ///////////////
            await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                frmMainAppInstance: null,
                initiator:
                HelperGenericAncillaryListsArrays.ExifToolInititators
                                                 .ExifGetTrackSyncDataWriteTrackPath);

            ///////////////

            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmImportExportGpx_ExportCompleted",
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Information,
                buttons: MessageBoxButtons.OK,
                extraMessage: outFilePath);
        }
        else
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmImportExportGpx_FileOrFolderDoesntExist",
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Error, buttons: 
                MessageBoxButtons.OK);
        }
    }
}