﻿using System;
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
    ///     TODO
    /// </summary>
    internal static async Task ExifWriteTrackDataToTrackFile(List<string> fileList,
        string outFilePath
    )
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        string argsFile = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "exifArgsToWriteForTrackExport.args");
        File.Delete(path: argsFile);

        // also delete the output file if exists otherwise it will be appended.
        File.Delete(path: outFilePath);

        string exiftoolCmd =
            " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 -W+ " +
            outFilePath + " " +
            " -@ " + HelperVariables.DoubleQuoteStr + argsFile + HelperVariables.DoubleQuoteStr;

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        // if user switches folder in the process of writing this will keep it standard
        Debug.Assert(condition: frmMainAppInstance != null, message: nameof(frmMainAppInstance) + " != null");

        // run file as exiftool -@ args.args -W+ out.gpx
        // as per https://exiftool.org/forum/index.php?topic=16222.0 the logic needs to be
        /*
            d:\temp\dests\_DSC1458.dng
            d:\temp\dests\_DSC1459.dng
            -p
            gpx.fmt
         */

        string exifArgsForOriginalFile = fileList.Where(predicate: trackFilePath => File.Exists(path: trackFilePath))
                                                 .Aggregate(seed: "",
                                                      func: (current, trackFilePath) =>
                                                          current + trackFilePath + Environment.NewLine);

        // -p
        exifArgsForOriginalFile += "-p" + Environment.NewLine;
        // gpx.fmt
        exifArgsForOriginalFile += Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "out.fmt") +
                                   Environment.NewLine;

        exifArgsForOriginalFile += "-overwrite_original_in_place" + Environment.NewLine;

        File.WriteAllText(path: argsFile, contents: exifArgsForOriginalFile, encoding: Encoding.UTF8);
        ///////////////
        await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
            frmMainAppInstance: null,
            initiator:
            HelperGenericAncillaryListsArrays.ExifToolInititators
                                             .ExifGetTrackSyncDataWriteTrackPath);

        ///////////////
    }
}