using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GeoTagNinja.Helpers.Geo;

/// <summary>
///     Converts a coordinate between decimal degrees and the degrees-minutes-seconds notation (<c>N47°55'9.95"</c>).
/// </summary>
/// <remarks>
///     Numbers are invariant here, as everywhere in the app: the decimal point is a full stop in the text this
///     produces and reads, whatever the user's locale.
/// </remarks>
internal static class Sexagesimal
{
    private static readonly Regex Pattern = new(
        pattern: @"^\s*(?<pre>[NSEWnsew])?\s*(?<sign>[-+])?\s*(?<deg>\d+(?:\.\d+)?)\s*(?:°|d|deg|\s)?\s*(?:(?<min>\d+(?:\.\d+)?)\s*(?:'|′|m|min|\s)?\s*(?:(?<sec>\d+(?:\.\d+)?)\s*(?:""|″|''|s|sec)?)?)?\s*(?<post>[NSEWnsew])?\s*$",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    ///     Formats a coordinate, e.g. <c>47.91943</c> as <c>N47°55'9.95"</c>.
    /// </summary>
    /// <param name="degrees">The signed decimal degrees (south and west are negative).</param>
    /// <param name="isLatitude">Whether this is a latitude (N/S) rather than a longitude (E/W).</param>
    internal static string Format(double degrees,
                                  bool isLatitude)
    {
        char hemisphere = isLatitude
            ? degrees < 0
                ? 'S'
                : 'N'
            : degrees < 0
                ? 'W'
                : 'E';

        double absolute = Math.Abs(value: degrees);

        // work in hundredths of a second so rounding can never produce 60 seconds or 60 minutes
        long hundredths = (long)Math.Round(value: absolute * 3600.0 * 100.0, mode: MidpointRounding.AwayFromZero);
        long wholeDegrees = hundredths / 360000;
        long minutes = hundredths % 360000 / 6000;
        double seconds = hundredths % 6000 / 100.0;

        return string.Format(provider: CultureInfo.InvariantCulture,
            format: "{0}{1}°{2}'{3:0.##}\"",
            args: [hemisphere, wholeDegrees, minutes, seconds]);
    }

    /// <summary>
    ///     Reads a coordinate typed as degrees, degrees and minutes, or degrees, minutes and seconds, with the
    ///     hemisphere as a letter before or after (or as a minus sign). Plain decimal degrees are accepted too.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="isLatitude">Whether a latitude is expected; a letter of the other axis is refused.</param>
    /// <param name="degrees">The signed decimal degrees.</param>
    /// <returns>False if the text is not a coordinate, or is out of range.</returns>
    internal static bool TryParse(string text,
                                  bool isLatitude,
                                  out double degrees)
    {
        degrees = 0;
        if (string.IsNullOrWhiteSpace(value: text))
        {
            return false;
        }

        Match match = Pattern.Match(input: text);
        if (!match.Success)
        {
            return false;
        }

        string pre = match.Groups[groupname: "pre"].Value;
        string post = match.Groups[groupname: "post"].Value;
        if (pre.Length > 0 &&
            post.Length > 0)
        {
            return false;
        }

        char hemisphere = char.ToUpperInvariant(c: (pre + post).Length > 0
            ? (pre + post)[index: 0]
            : ' ');
        if (hemisphere != ' ' &&
            (isLatitude
                ? hemisphere is not ('N' or 'S')
                : hemisphere is not ('E' or 'W')))
        {
            return false;
        }

        double result = double.Parse(s: match.Groups[groupname: "deg"].Value, style: NumberStyles.Float,
            provider: CultureInfo.InvariantCulture);

        if (match.Groups[groupname: "min"].Success)
        {
            double minutes = double.Parse(s: match.Groups[groupname: "min"].Value, style: NumberStyles.Float,
                provider: CultureInfo.InvariantCulture);
            if (minutes >= 60)
            {
                return false;
            }

            result += minutes / 60.0;

            if (match.Groups[groupname: "sec"].Success)
            {
                double seconds = double.Parse(s: match.Groups[groupname: "sec"].Value, style: NumberStyles.Float,
                    provider: CultureInfo.InvariantCulture);
                if (seconds >= 60)
                {
                    return false;
                }

                result += seconds / 3600.0;
            }
        }

        bool negative = match.Groups[groupname: "sign"].Value == "-" ||
                        hemisphere is 'S' or 'W';
        if (negative)
        {
            result = -result;
        }

        double limit = isLatitude
            ? 90.0
            : 180.0;
        if (Math.Abs(value: result) > limit)
        {
            return false;
        }

        degrees = result;
        return true;
    }
}
