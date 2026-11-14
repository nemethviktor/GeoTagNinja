using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoTagNinja.Model;

/// <summary>
///     Turns a favourite's keyword list into the single text value stored in SQLite, and back.
/// </summary>
/// <remarks>
///     One keyword per line, because a keyword may well contain a comma or a semicolon but never a line break.
///     Blank entries and repeats (ignoring case) are dropped on the way in and out, so the stored value is always tidy.
/// </remarks>
internal static class FavouriteKeywords
{
    private static readonly char[] LineBreaks = ['\r', '\n'];

    /// <summary>
    ///     The stored text for a list of keywords.
    /// </summary>
    internal static string Join(IEnumerable<string> keywords)
    {
        return string.Join(separator: "\n", values: Tidy(keywords: keywords));
    }

    /// <summary>
    ///     The keywords held in a stored text value. Never null.
    /// </summary>
    internal static List<string> Split(string stored)
    {
        return string.IsNullOrWhiteSpace(value: stored)
            ? []
            : Tidy(keywords: stored.Split(separator: LineBreaks, options: StringSplitOptions.RemoveEmptyEntries));
    }

    private static List<string> Tidy(IEnumerable<string> keywords)
    {
        return (keywords ?? [])
              .Select(selector: keyword => keyword?.Trim())
              .Where(predicate: keyword => !string.IsNullOrEmpty(value: keyword))
              .Distinct(comparer: StringComparer.OrdinalIgnoreCase)
              .ToList();
    }
}
