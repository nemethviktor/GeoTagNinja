using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static GeoTagNinja.Model.DirectoryElement;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     What to tell ExifTool to write the location into a video (MP4, MOV, ...), where the EXIF GPS tags used for photos
///     do not exist.
/// </summary>
/// <remarks>
///     <para>
///         A video keeps its location as one combined text, <c>latitude, longitude[, altitude]</c>, in the QuickTime
///         <c>Keys:GPSCoordinates</c> atom (iPhone, and what Android reads) and/or <c>UserData:GPSCoordinates</c> (the
///         older <c>©xyz</c> form; Samsung writes only this one, and Samsung's Gallery and Google Photos read it). Both
///         are written every time, so every reader agrees: writing only one leaves a second, stale location behind,
///         which was found with a real Samsung S25 clip, where the other tags (EXIF, XMP) changed nothing a player
///         shows.
///     </para>
///     <para>
///         Beyond the location, only the XMP tags can be written to a video (title, caption, keywords, place names, rating,
///         creator and so on): the IPTC and EXIF tags the same attributes also carry are silently dropped by ExifTool for
///         an MP4/MOV (checked to write and read back with a real clip). Dates are not written yet (a video keeps them as UTC
///         in the QuickTime atoms; see <see cref="IsXmpWritable" />). Whatever else is staged is not written (see
///         <see cref="Plan.HasOtherEdits" />), because writing it would claim success for tags ExifTool drops.
///     </para>
/// </remarks>
internal static class VideoLocationArguments
{
    /// <summary>The attributes this writer turns into the combined location (the Ref ones are derived from the sign).</summary>
    private static readonly HashSet<ElementAttribute> LocationAttributes =
    [
        ElementAttribute.GPSLatitude,
        ElementAttribute.GPSLatitudeRef,
        ElementAttribute.GPSLongitude,
        ElementAttribute.GPSLongitudeRef,
        ElementAttribute.GPSAltitude,
        ElementAttribute.GPSAltitudeRef,
        ElementAttribute.RemoveAllGPS
    ];

    /// <summary>The attributes that carry a date: not written to a video yet (it needs its own handling of UTC and offsets).</summary>
    private static readonly HashSet<ElementAttribute> DateAttributes =
    [
        ElementAttribute.TakenDate,
        ElementAttribute.CreateDate
    ];

    /// <summary>
    ///     Whether the attribute can be written to a video: it has an XMP tag (the only metadata block ExifTool can put in an
    ///     MP4/MOV besides the QuickTime atoms), and it is neither the location (written by <see cref="Lines" />) nor a
    ///     date.
    /// </summary>
    internal static bool IsXmpWritable(ElementAttribute attribute)
    {
        if (LocationAttributes.Contains(item: attribute) ||
            DateAttributes.Contains(item: attribute))
        {
            return false;
        }

        try
        {
            return GetElementAttributesOut(attributeToFind: attribute)?.Any(predicate: IsXmpTag) == true;
        }
        catch (Exception)
        {
            // an attribute with no tag mapping at all
            return false;
        }
    }

    /// <summary>Just the XMP tags of an attribute's out-tags: the others are dropped by ExifTool for a video.</summary>
    internal static List<string> XmpTagsOnly(List<string> tags)
    {
        return tags == null
            ? []
            : tags.Where(predicate: IsXmpTag)
                  .ToList();
    }

    private static bool IsXmpTag(string tag)
    {
        return tag.StartsWith(value: "XMP", comparisonType: StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>What to write for one video.</summary>
    internal sealed class Plan
    {
        /// <summary>The ExifTool argument lines (one per tag), empty when there is nothing to write.</summary>
        internal List<string> Lines { get; } = [];

        /// <summary>The video has a staged location edit, which <see cref="Lines" /> writes.</summary>
        internal bool HasLocationEdit => Lines.Count > 0;

        /// <summary>The video has staged edits that can be written as XMP (see <see cref="IsXmpWritable" />).</summary>
        internal bool HasXmpEdit { get; set; }

        /// <summary>There is something to write to the video.</summary>
        internal bool HasWritableEdit => HasLocationEdit || HasXmpEdit;

        /// <summary>
        ///     The video has staged edits that cannot be written to it (a date, an EXIF-only value...). They are not
        ///     written, and the user is told.
        /// </summary>
        internal bool HasOtherEdits { get; set; }
    }

    /// <summary>
    ///     The text of the combined location: <c>lat, lon</c> or <c>lat, lon, alt</c>, with a dot decimal separator
    ///     whatever the culture (it is read by players, not people).
    /// </summary>
    /// <param name="latitude">Signed degrees, south negative.</param>
    /// <param name="longitude">Signed degrees, west negative.</param>
    /// <param name="altitudeMetres">The altitude in metres (below sea level negative), or null for none.</param>
    internal static string Coordinates(double latitude,
                                       double longitude,
                                       double? altitudeMetres)
    {
        string text = $"{Number(value: latitude, format: "0.######")}, {Number(value: longitude, format: "0.######")}";
        return altitudeMetres == null
            ? text
            : $"{text}, {Number(value: altitudeMetres.Value, format: "0.##")}";
    }

    /// <summary>
    ///     The argument lines that put a location into a video, or take it out.
    /// </summary>
    /// <param name="removeAll">True to remove the location instead (every tag it can live in).</param>
    /// <param name="latitude">Signed degrees; with <paramref name="longitude" />, needed to write anything.</param>
    /// <param name="longitude">Signed degrees.</param>
    /// <param name="altitudeMetres">The altitude in metres, or null to write none.</param>
    internal static List<string> Lines(bool removeAll,
                                       double? latitude,
                                       double? longitude,
                                       double? altitudeMetres)
    {
        List<string> lines = [];
        if (removeAll)
        {
            // "-gps*=" is what the photo path sends; the two named ones make it explicit for the video atoms
            lines.Add(item: "-gps*=");
            lines.Add(item: "-xmp:gps*=");
            lines.Add(item: "-Keys:GPSCoordinates=");
            lines.Add(item: "-UserData:GPSCoordinates=");
            return lines;
        }

        if (latitude == null ||
            longitude == null)
        {
            return lines;
        }

        string coordinates = Coordinates(latitude: latitude.Value, longitude: longitude.Value, altitudeMetres: altitudeMetres);
        lines.Add(item: $"-Keys:GPSCoordinates={coordinates}");
        lines.Add(item: $"-UserData:GPSCoordinates={coordinates}");
        return lines;
    }

    /// <summary>
    ///     Works out what to write for a video from what is staged on it (<see cref="AttributeVersion.Stage3ReadyToWrite" />).
    ///     A coordinate that is not itself staged is taken from the newest value the file has, so editing only the
    ///     altitude keeps the position.
    /// </summary>
    /// <param name="element">The video.</param>
    /// <param name="useImperial">Whether the model holds the altitude in feet (see <c>architecture-values.md</c>).</param>
    internal static Plan PlanFor(DirectoryElement element,
                                 bool useImperial)
    {
        Plan plan = new();

        bool locationStaged = false;
        bool removeAll = false;
        foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
        {
            if (!element.HasSpecificAttributeWithVersion(attribute: attribute, version: AttributeVersion.Stage3ReadyToWrite))
            {
                continue;
            }

            if (!LocationAttributes.Contains(item: attribute))
            {
                if (IsXmpWritable(attribute: attribute))
                {
                    plan.HasXmpEdit = true;
                }
                else
                {
                    plan.HasOtherEdits = true;
                }

                continue;
            }

            locationStaged = true;
            if (attribute == ElementAttribute.RemoveAllGPS ||
                (attribute is ElementAttribute.GPSLatitude or ElementAttribute.GPSLongitude &&
                 element.IsMarkedForDeletion(attribute: attribute, version: AttributeVersion.Stage3ReadyToWrite)))
            {
                removeAll = true;
            }
        }

        if (!locationStaged)
        {
            return plan;
        }

        double? latitude = NewestValue(element: element, attribute: ElementAttribute.GPSLatitude);
        double? longitude = NewestValue(element: element, attribute: ElementAttribute.GPSLongitude);
        double? altitude = NewestValue(element: element, attribute: ElementAttribute.GPSAltitude);
        double? altitudeMetres = altitude == null
            ? null
            : useImperial
                ? Math.Round(value: altitude.Value / HelperVariables.MetreToFeet, digits: 2)
                : altitude;

        plan.Lines.AddRange(collection: Lines(removeAll: removeAll, latitude: latitude, longitude: longitude, altitudeMetres: altitudeMetres));
        return plan;
    }

    /// <summary>
    ///     The newest value of a numeric attribute, or null when there is none or the newest version is flagged for
    ///     deletion (falling back to an older version would bring back a value the user removed).
    /// </summary>
    private static double? NewestValue(DirectoryElement element,
                                      ElementAttribute attribute)
    {
        AttributeVersion? version = element.GetMaxAttributeVersion(attribute: attribute);
        return version == null
            ? null
            : element.GetAttributeValue<double>(attribute: attribute, version: version);
    }

    private static string Number(double value,
                                 string format)
    {
        return value.ToString(format: format, provider: CultureInfo.InvariantCulture);
    }
}
