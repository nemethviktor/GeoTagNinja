using System;
using System.IO;

namespace GeoTagNinja.Helpers.Exif;

internal enum ExportFileOrder
{
    DateTimeOriginal,
    GPSDateTime,
    CreateDate,
    FileName
}

internal enum ExportFileFMTTimeBasis
{
    DateTimeOriginal,
    GPSDateTime,
    CreateDate
}

/// <summary>
///     Builds the ExifTool format file that drives GPX export.
/// </summary>
internal static class GpxExportOptions
{
    internal static void GenerateFMTFile(bool includeAltitude,
                                         string exportFileFMTTimeBasis)
    {
        string fmtFileContent = "";
        fmtFileContent +=
            $"""
             #------------------------------------------------------------------------------
             # Taken from https://github.com/exiftool/exiftool/blob/master/fmt_files/gpx.fmt
             # On 20240713
             #------------------------------------------------------------------------------
             #[HEAD]<?xml version="1.0" encoding="utf-8"?>
             #[HEAD]<gpx version="1.0"
             #[HEAD] creator="ExifTool $ExifToolVersion"
             #[HEAD] xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
             #[HEAD] xmlns="http://www.topografix.com/GPX/1/0"
             #[HEAD] xsi:schemaLocation="http://www.topografix.com/GPX/1/0 http://www.topografix.com/GPX/1/0/gpx.xsd">
             #[HEAD]<trk>
             #[HEAD]<number>1</number>
             #[HEAD]<trkseg>
             #[IF]  $gpslatitude $gpslongitude
             #[BODY]<trkpt lat="$gpslatitude#" lon="$gpslongitude#">{Environment.NewLine}
             """;
        if (includeAltitude)
        {
            fmtFileContent +=
                $"""#[BODY]  <ele>$gpsaltitude#</ele>{Environment.NewLine}""";
            ;
        }

        fmtFileContent +=
            """#[BODY]  <time>${replaceme#;DateFmt("%Y-%m-%dT%H:%M:%S%fZ")}</time>""".Replace(oldValue: "replaceme",
                newValue: exportFileFMTTimeBasis.ToLower()) + Environment.NewLine;

        fmtFileContent +=
            """
            #[BODY]</trkpt>
            #[TAIL]</trkseg>
            #[TAIL]</trk>
            #[TAIL]</gpx>
            """;

        string fmtFilePath = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "out.fmt");
        File.WriteAllText(path: fmtFilePath, contents: fmtFileContent);
    }
}
