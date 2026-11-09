using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     Builds the ExifTool format file that drives KML export, and tidies its output.
///     The KML is a Google <c>gx:Track</c> (timestamps + coordinates), not a plain <c>LineString</c> like ExifTool's
///     <c>kml_track.fmt</c>: a LineString has no times, so neither ExifTool nor this app's own import could use it for
///     geotagging. <c>gx:Track</c> is what Google Earth, GPSBabel and ExifTool's <c>-geotag</c> read.
/// </summary>
internal static class KmlExportOptions
{
    private const string WhenTag = "<when>";
    private const string CoordTag = "<gx:coord>";

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
    ///     ExifTool prints one <c>when</c> + <c>gx:coord</c> pair per photo; <see cref="GroupTrackPoints" /> then puts them
    ///     in the order other software expects.
    /// </summary>
    /// <param name="includeAltitude">
    ///     Writes the real altitude (with <c>absolute</c> altitude mode); otherwise 0 with <c>clampToGround</c>.
    /// </param>
    /// <param name="exportFileFMTTimeBasis">The date tag used for the timestamps (as for GPX).</param>
    internal static void GenerateFMTFile(bool includeAltitude,
                                         string exportFileFMTTimeBasis)
    {
        string timeTag = exportFileFMTTimeBasis.ToLower();

        // ExifTool drops a print-format line whose tag is missing. Every tag used below is therefore required by the
        // IF line, so a photo is either in the track completely or not at all and the <when> / <gx:coord> counts match.
        string requiredTags = $"$gpslatitude $gpslongitude ${timeTag}" +
                              (includeAltitude
                                  ? " $gpsaltitude"
                                  : "");
        string altitudeMode = includeAltitude
            ? "absolute"
            : "clampToGround";
        // ExifTool's own KML reader (and so this app's import) only accepts "lon lat alt" triplets, so without altitude
        // we write 0 and leave it to clampToGround to make other software ignore it.
        string coordinate = includeAltitude
            ? "$gpslongitude# $gpslatitude# $gpsaltitude#"
            : "$gpslongitude# $gpslatitude# 0";

        string fmtFileContent =
            $$"""
              #------------------------------------------------------------------------------
              # Based on https://github.com/exiftool/exiftool/blob/master/fmt_files/kml_track.fmt
              # On 20261107, changed from LineString to gx:Track so that the times are kept
              #------------------------------------------------------------------------------
              #[HEAD]<?xml version="1.0" encoding="UTF-8"?>
              #[HEAD]<kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2">
              #[HEAD]  <Document>
              #[HEAD]    <name>My Track</name>
              #[HEAD]    <open>1</open>
              #[HEAD]    <Placemark>
              #[HEAD]      <name>My Track</name>
              #[HEAD]      <Style>
              #[HEAD]        <LineStyle>
              #[HEAD]          <color>ff4499ff</color>
              #[HEAD]          <width>3</width>
              #[HEAD]        </LineStyle>
              #[HEAD]      </Style>
              #[HEAD]      <gx:Track>
              #[HEAD]        <altitudeMode>{{altitudeMode}}</altitudeMode>
              #[IF]  {{requiredTags}}
              #[BODY]        <when>${{{timeTag}}#;DateFmt("%Y-%m-%dT%H:%M:%S%fZ")}</when>
              #[BODY]        <gx:coord>{{coordinate}}</gx:coord>
              #[TAIL]      </gx:Track>
              #[TAIL]    </Placemark>
              #[TAIL]  </Document>
              #[TAIL]</kml>
              """;

        string fmtFilePath = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "out.fmt");
        File.WriteAllText(path: fmtFilePath, contents: fmtFileContent);
    }

    /// <summary>
    ///     Rewrites ExifTool's output so that all <c>&lt;when&gt;</c> lines come first, followed by all
    ///     <c>&lt;gx:coord&gt;</c> lines, in the same order. That is the layout Google's documentation uses, and the one
    ///     other software is most certain to accept; ExifTool itself can only print them as alternating pairs.
    /// </summary>
    internal static void GroupTrackPoints(string outFilePath)
    {
        if (!File.Exists(path: outFilePath))
        {
            return;
        }

        string[] lines = File.ReadAllLines(path: outFilePath, encoding: Encoding.UTF8);
        List<string> head = [];
        List<string> whens = [];
        List<string> coords = [];
        List<string> tail = [];

        foreach (string line in lines)
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith(value: WhenTag, comparisonType: StringComparison.Ordinal))
            {
                whens.Add(item: line);
            }
            else if (trimmed.StartsWith(value: CoordTag, comparisonType: StringComparison.Ordinal))
            {
                coords.Add(item: line);
            }
            else if (whens.Count == 0 &&
                     coords.Count == 0)
            {
                head.Add(item: line);
            }
            else
            {
                tail.Add(item: line);
            }
        }

        List<string> result = [..head, ..whens, ..coords, ..tail];
        File.WriteAllLines(path: outFilePath, contents: result, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
