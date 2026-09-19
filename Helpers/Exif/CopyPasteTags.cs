using System.Collections.Generic;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     The set of attributes carried over when the user copies metadata from one file and pastes it onto
///     another.
/// </summary>
internal static class CopyPasteTags
{
    internal static readonly List<ElementAttribute> TagsToCopy =
    [
        ElementAttribute.Coordinates,
        ElementAttribute.GPSLatitude,
        ElementAttribute.GPSLatitudeRef,
        ElementAttribute.GPSLongitude,
        ElementAttribute.GPSLongitudeRef,
        ElementAttribute.GPSSpeed,
        ElementAttribute.GPSSpeedRef,
        ElementAttribute.GPSAltitude,
        ElementAttribute.GPSAltitudeRef,
        ElementAttribute.Country,
        ElementAttribute.CountryCode,
        ElementAttribute.State,
        ElementAttribute.City,
        ElementAttribute.Sublocation,
        ElementAttribute.DestCoordinates,
        ElementAttribute.GPSDestLatitude,
        ElementAttribute.GPSDestLatitudeRef,
        ElementAttribute.GPSDestLongitude,
        ElementAttribute.GPSDestLongitudeRef,
        ElementAttribute.GPSImgDirection,
        ElementAttribute.GPSImgDirectionRef,
        ElementAttribute.TakenDate,
        ElementAttribute.CreateDate,
        ElementAttribute.OffsetTime,
        ElementAttribute.TakenDateSecondsShift,
        ElementAttribute.TakenDateMinutesShift,
        ElementAttribute.TakenDateHoursShift,
        ElementAttribute.TakenDateDaysShift,
        ElementAttribute.CreateDateSecondsShift,
        ElementAttribute.CreateDateMinutesShift,
        ElementAttribute.CreateDateHoursShift,
        ElementAttribute.CreateDateDaysShift
    ];
}
