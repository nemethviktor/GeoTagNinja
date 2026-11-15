using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace GeoTagNinja.Helpers.Geo;

/// <summary>
///     Turns raw coordinate text from ExifTool or the map into numbers. Pure maths and parsing, so it lives in Core.
/// </summary>
internal static class CoordinateParsing
{
    /// <summary>
    ///     Wrangles the actual coordinate out of a point. (e.g. 4.54 East to -4.54)
    /// </summary>
    /// <param name="point">This is a raw coordinate. Could contain numbers or things like "East" on top of numbers</param>
    /// <returns>Double - an actual coordinate, or zero if nothing numeric could be read out of the text.</returns>
    public static double AdjustLatLongNegative(string point)
    {
        return TryAdjustLatLongNegative(point: point, coordinate: out double coordinate)
            ? coordinate
            : 0.0;
    }

    /// <summary>
    ///     As <see cref="AdjustLatLongNegative" />, but able to say that the text held no coordinate at all.
    /// </summary>
    /// <remarks>
    ///     The distinction matters to the read pipeline: a tag that cannot be understood must leave the attribute
    ///     unset, whereas zero is a perfectly valid coordinate (and, off the coast of Ghana, a real place).
    /// </remarks>
    /// <param name="point">This is a raw coordinate. Could contain numbers or things like "East" on top of numbers</param>
    /// <param name="coordinate">The coordinate read out of <paramref name="point" />, or zero on failure.</param>
    /// <returns><see langword="true" /> when a number could be read; otherwise <see langword="false" />.</returns>
    public static bool TryAdjustLatLongNegative(string point,
                                                out double coordinate)
    {
        coordinate = 0.0;
        if (string.IsNullOrWhiteSpace(value: point))
        {
            return false;
        }

        string pointOrig = point.Replace(oldValue: " ", newValue: "")
                                .Replace(oldChar: ',', newChar: '.');
        // WGS84 DM --> logic here is, before I have to spend hours digging this crap again...
        // degree stays as-is, the totality of the rest gets divided by 60.
        // so 41,53.23922526N becomes 41 + (53.23922526/60) = 41.88732
        double pointVal;
        bool parsed;
        if (pointOrig.Count(predicate: f => f == '.') == 2)
        {
            bool degreeParse = int.TryParse(s: pointOrig.Split('.')[0],
                style: NumberStyles.Any,
                provider: CultureInfo.InvariantCulture,
                result: out int degree);
            bool minuteParse = double.TryParse(s: Regex.Replace(
                    input: $"{pointOrig.Split('.')[1]}.{pointOrig.Split('.')[2]}",
                    pattern: "[SWNE\"-]",
                    replacement: ""),
                style: NumberStyles.Any,
                provider: CultureInfo.InvariantCulture,
                result: out double minute);
            minute /= 60;
            pointVal = degree + minute;
            parsed = degreeParse && minuteParse;
        }
        else
        {
            parsed =
                double.TryParse(
                    s: Regex.Replace(input: pointOrig, pattern: "[SWNE\"-]",
                        replacement: ""), style: NumberStyles.Any,
                    provider: CultureInfo.InvariantCulture, result: out pointVal);
        }

        if (!parsed)
        {
            return false;
        }

        pointVal = Math.Round(value: pointVal, digits: 6);
        int multiplier = point.Contains(value: "S") ||
                         point.Contains(value: "W") ||
                         point.StartsWith(value: "-")
            ? -1
            : 1; //handle south and west

        coordinate = pointVal * multiplier;
        return true;
    }

    /// <summary>
    ///     Corrects the half-coordinate to be a valid one (in case over/under 180, which can happen if the map is
    ///     misbehaving.)
    /// </summary>
    /// <param name="coordHalfPair">Lat or Long</param>
    /// <returns>Rounded to 6, corrected Lat or Long</returns>
    internal static double GenericCorrectInvalidCoordinate(double coordHalfPair)
    {
        if (coordHalfPair < -180)
        {
            coordHalfPair = 180 - (Math.Abs(value: coordHalfPair) % 180);
        }
        else if (coordHalfPair > 180)
        {
            coordHalfPair = Math.Abs(value: coordHalfPair) % 180;
        }

        coordHalfPair = Math.Round(value: coordHalfPair, digits: 6);
        return coordHalfPair;
    }
}
