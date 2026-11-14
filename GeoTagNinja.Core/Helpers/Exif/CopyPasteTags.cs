using GeoTagNinja.Model;
using System;
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

    /// <summary>
    ///     Applies one value taken from the copy pool to a target element.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The copy pool holds values that are already typed - the <see cref="string" />, <see cref="int" />,
    ///         <see cref="double" /> or <see cref="System.DateTime" /> that came out of the source element. They are
    ///         handed straight to the target, with no trip through text.
    ///     </para>
    ///     <para>
    ///         This used to render each value to a string and re-parse it, which meant picking a culture that both
    ///         ends agreed on - impossible in general, because an invariant "07/06/2018" is 6 July, while a day-first
    ///         culture reads the same text back as 7 June. Pasting silently moved dates by up to eleven months for
    ///         users in the UK, India, Argentina and everywhere else that writes the day first. There was a
    ///         hard-coded allow-list of cultures to work around it, which could only ever cover the cultures someone
    ///         had already complained about.
    ///     </para>
    /// </remarks>
    /// <param name="target">The element receiving the value.</param>
    /// <param name="attribute">The attribute being pasted.</param>
    /// <param name="copiedValue">The value from the copy pool. A null is treated as "clear this tag".</param>
    /// <param name="version">The version slot to write into.</param>
    internal static void ApplyCopiedValue(DirectoryElement target,
                                          ElementAttribute attribute,
                                          object copiedValue,
                                          DirectoryElement.AttributeVersion version)
    {
        // A blank string has always meant "remove the tag" here; keep that.
        bool isBlank = copiedValue == null ||
                       (copiedValue is string text && text.Length == 0);

        target.SetAttributeValue(
            attribute: attribute,
            value: isBlank
                ? null
                : (IConvertible)copiedValue,
            version: version,
            isMarkedForDeletion: isBlank);
    }
}
