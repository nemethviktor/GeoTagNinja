using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GeoTagNinja.Helpers.Localisation;

internal enum TimeShiftTypes
{
    TakenDate,
    CreateDate
}

/// <summary>
///     The hard-coded IANA time-zone list offered to the user, plus the helpers that pick an entry out of
///     it by UTC offset.
/// </summary>
/// <remarks>
///     The list is hard-coded rather than taken from <see cref="System.TimeZoneInfo" /> so that the names
///     shown to the user stay stable across Windows releases, and so that they match what is written into
///     the files.
/// </remarks>
internal static class TimeZones
{
    /// <summary>
    ///     Gets a list of formatted time zone identifiers grouped by unique standard and daylight saving offsets.
    /// </summary>
    /// <remarks>
    ///     Retrieves system time zones from Windows NLS, filters down to unique offset combinations (Standard/DST), 
    ///     sorts them chronologically by standard offset, and appends a trailing empty string to preserve compatibility.
    /// </remarks>
    /// <returns>
    ///     A <see cref="List{T}"/> of strings formatted as "(STD/DST) # TimeZoneDisplayName", plus an empty string at the end.
    /// </returns>
    internal static List<string> GetTimeZones()
    {
        ReadOnlyCollection<TimeZoneInfo> systemZones = TimeZoneInfo.GetSystemTimeZones();
        DateTime referenceYear = new DateTime(DateTime.UtcNow.Year, 1, 1);

        List<string> groupedZones = systemZones
            .Select(zone =>
            {
                // Calculate standard offset and dynamic daylight offset
                TimeSpan stdOffset = zone.BaseUtcOffset;
                TimeSpan dstOffset = zone.GetUtcOffset(GetReferenceDstDate(zone, referenceYear));

                string stdString = FormatOffset(stdOffset);
                string dstString = FormatOffset(dstOffset);

                return new
                {
                    Zone = zone,
                    StdOffset = stdOffset,
                    StdString = stdString,
                    DstString = dstString,
                    // Unique key combining standard offset and daylight offset
                    OffsetKey = $"{stdString}/{dstString}"
                };
            })
            // Group by unique (STD/DST) offset combinations to eliminate overwhelming granular lists
            .GroupBy(x => x.OffsetKey)
            // Select the primary representative zone for each distinct offset pair
            .Select(g => g.First())
            // Order chronologically by Standard UTC offset
            .OrderBy(x => x.StdOffset)
            .Select(x => $"({x.StdString}/{x.DstString}) # {x.Zone.DisplayName}")
            .ToList();

        // Preserve intentional trailing blank entry
        groupedZones.Add(string.Empty);

        return groupedZones;
    }

    /// <summary>
    ///     Formats a <see cref="TimeSpan"/> offset into a signed string representation (e.g., "+02:00" or "-05:00").
    /// </summary>
    /// <param name="offset">The UTC offset to format.</param>
    /// <returns>A formatted offset string with sign and double-digit hour and minute representation.</returns>
    private static string FormatOffset(TimeSpan offset)
    {
        char sign = offset >= TimeSpan.Zero ? '+' : '-';
        TimeSpan absOffset = offset.Duration();
        return $"{sign}{absOffset.Hours:D2}:{absOffset.Minutes:D2}";
    }

    /// <summary>
    ///     Attempts to locate a date within the given year when Daylight Saving Time is active for a zone.
    /// </summary>
    /// <param name="zone">The time zone to evaluate.</param>
    /// <param name="defaultDate">The default date to return if DST is not supported.</param>
    /// <returns>A <see cref="DateTime"/> representing a active DST period, or the default date.</returns>
    private static DateTime GetReferenceDstDate(TimeZoneInfo zone, DateTime defaultDate)
    {
        if (!zone.SupportsDaylightSavingTime)
            return defaultDate;

        // Check mid-year (July) as a primary reference point for DST in Northern hemisphere
        DateTime summerDate = new DateTime(defaultDate.Year, 7, 1);
        if (zone.IsDaylightSavingTime(summerDate))
            return summerDate;

        // Check mid-winter (January) as a reference point for DST in Southern hemisphere
        DateTime winterDate = new DateTime(defaultDate.Year, 1, 1);
        if (zone.IsDaylightSavingTime(winterDate))
            return winterDate;

        return defaultDate;
    }
}
