using System;
using System.IO;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     Builds the ExifTool format file that drives KML export (a single track as a Google Earth <c>LineString</c>).
/// </summary>
internal static class KmlExportOptions
{
    /// <summary>
    ///     True when <paramref name="outFilePath" /> asks for KML (by its extension); anything else is exported as GPX.
    /// </summary>
    internal static bool IsKmlPath(string outFilePath)
    {
        return string.Equals(a: Path.GetExtension(path: outFilePath),
            b: ".kml",
            comparisonType: StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Writes <c>out.fmt</c> (the same file the GPX export uses) with the KML track layout.
    /// </summary>
    /// <param name="includeAltitude">
    ///     Writes <c>$gpsaltitude#</c> with <c>absolute</c> altitude mode. ExifTool skips a print-format line whose tag
    ///     is missing, so with this on, files that have no altitude drop out of the track.
    /// </param>
    internal static void GenerateFMTFile(bool includeAltitude)
    {
        string altitudeMode = includeAltitude
            ? "absolute"
            : "clampToGround";
        string altitudeValue = includeAltitude
            ? "$gpsaltitude#"
            : "0";

        string fmtFileContent =
            $$"""
             #------------------------------------------------------------------------------
             # Taken from https://github.com/exiftool/exiftool/blob/master/fmt_files/kml_track.fmt
             # On 20261107
             #------------------------------------------------------------------------------
             #[HEAD]<?xml version="1.0" encoding="UTF-8"?>
             #[HEAD]<kml xmlns="http://earth.google.com/kml/2.0">
             #[HEAD]  <Document>
             #[HEAD]    <name>My Track</name>
             #[HEAD]    <open>1</open>
             #[SECT]    <Placemark>
             #[SECT]    <name>${main:directory;$_=$self->GetValue('FileName') if $self->Options('ExtractEmbedded')}</name>
             #[SECT]      <Style>
             #[SECT]        <LineStyle>
             #[SECT]          <color>#ff4499ff</color>
             #[SECT]          <width>3</width>
             #[SECT]        </LineStyle>
             #[SECT]      </Style>
             #[SECT]      <LineString>
             #[SECT]        <altitudeMode>{{altitudeMode}}</altitudeMode>
             #[SECT]        <coordinates>
             #[IF]  $gpslatitude $gpslongitude
             #[BODY]$gpslongitude#,$gpslatitude#,{{altitudeValue}}
             #[ENDS]        </coordinates>
             #[ENDS]      </LineString>
             #[ENDS]    </Placemark>
             #[TAIL]  </Document>
             #[TAIL]</kml>
             """;

        string fmtFilePath = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "out.fmt");
        File.WriteAllText(path: fmtFilePath, contents: fmtFileContent);
    }
}
