using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers;

internal class HelperExifWriteTrackDataToSideCar
{
    /// <summary>
    ///     This triggers the first half of the track import process - it reads the various gpx files and creates sidecars as
    ///     needed.
    /// </summary>
    /// <param name="trackFileList"></param>
    /// <param name="imageFileList"></param>
    /// <param name="compareTZAgainst"></param>
    /// <param name="TZVal"></param>
    /// <param name="GeoMaxIntSecs"></param>
    /// <param name="GeoMaxExtSecs"></param>
    /// <param name="timeShiftSeconds"></param>
    internal static async Task ExifWriteTrackDataToSideCar(List<string> trackFileList,
                                                           List<string> imageFileList,
                                                           string compareTZAgainst,
                                                           string TZVal,
                                                           int GeoMaxIntSecs = 1800,
                                                           int GeoMaxExtSecs = 1800,
                                                           int timeShiftSeconds = 0
    )
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        string argsFile = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "exifArgsToWriteForTrackExport.args");
        File.Delete(path: argsFile);

        string exiftoolCmd = " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8" + " -@ " + HelperVariables.SDoubleQuote + argsFile + HelperVariables.SDoubleQuote;

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        // if user switches folder in the process of writing this will keep it standard
        Debug.Assert(condition: frmMainAppInstance != null, message: nameof(frmMainAppInstance) + " != null");
        string exifArgsForOriginalFile = "";

        // as per https://exiftool.org/forum/index.php?msg=78652 the logic needs to be
        /*
            -geotag=D:\temp3\2023-02-27.gpx
            -geotag=D:\temp3\2023-02-28.gpx
            -geotime<${DateTimeOriginal#}+00:00
            -api
            GeoMaxIntSecs=1800
            -api
            GeoMaxExtSecs=1800
            D:\temp3\_2280104.ORF
            D:\temp3\_2280105.ORF
            -v2
            -srcfile
            C:\Users\nemet\AppData\Roaming\GeoTagNinja\tmpLocFiles\%F.xmp

         */

        foreach (string trackFilePath in trackFileList)
        {
            // -geotag=D:\temp3\2023-02-27.gpx
            exifArgsForOriginalFile += "-geotag=" + trackFilePath + Environment.NewLine;
        }

        // -geotime<${DateTimeOriginal#}+00:00
        exifArgsForOriginalFile += "-geotime<${" + compareTZAgainst + "#}" + TZVal + Environment.NewLine;

        // -geosync
        if (timeShiftSeconds < 0)
        {
            exifArgsForOriginalFile += "-geosync=" + timeShiftSeconds + Environment.NewLine;
        }
        else if (timeShiftSeconds > 0)
        {
            exifArgsForOriginalFile += "-geosync=+" + timeShiftSeconds + Environment.NewLine;
        }

        // -api
        exifArgsForOriginalFile += "-api" + Environment.NewLine;
        // GeoMaxIntSecs=1800
        exifArgsForOriginalFile += "GeoMaxIntSecs=" + GeoMaxIntSecs + Environment.NewLine;

        // -api
        exifArgsForOriginalFile += "-api" + Environment.NewLine;
        // GeoMaxExtSecs
        exifArgsForOriginalFile += "GeoMaxExtSecs=" + GeoMaxExtSecs + Environment.NewLine;

        //D:\temp3\_2280104.ORF
        //D:\temp3\_2280105.ORF
        foreach (string imageFilePath in imageFileList)
        {
            // -geotag=D:\temp3\2023-02-27.gpx
            exifArgsForOriginalFile += imageFilePath + Environment.NewLine;
        }

        // -v2
        exifArgsForOriginalFile += "-v2" + Environment.NewLine;
        // -srcfile
        exifArgsForOriginalFile += "-srcfile" + Environment.NewLine;
        // C:\Users\nemet\AppData\Roaming\GeoTagNinja\tmpLocFiles\%F.xmp
        string tmpFolder = Path.Combine(HelperVariables.UserDataFolderPath + @"\tmpLocFiles\");
        exifArgsForOriginalFile += tmpFolder + "%F.xmp" + Environment.NewLine;

        exifArgsForOriginalFile += "-overwrite_original_in_place" + Environment.NewLine;

        File.WriteAllText(path: argsFile, contents: exifArgsForOriginalFile, encoding: Encoding.UTF8);
        ///////////////
        await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                                     frmMainAppInstance: null,
                                                     initiator: "ExifGetTrackSyncData");

        ///////////////
        //// try to collect the xmp/xml listOfAsyncCompatibleFileNamesWithOutPath and then read them back into the listview.
    }
}