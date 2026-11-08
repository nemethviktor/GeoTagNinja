using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Geo;

/// <summary>
///     What the keywords of a photo say about where it was taken: the place names the app itself writes into the
///     location fields, and the <c>geo:lat=</c> / <c>geo:lon=</c> pair some tools add.
/// </summary>
internal sealed class GeoKeywordLocation : IEquatable<GeoKeywordLocation>
{
    private const double SameCoordinate = 1e-9;

    internal string Country { get; set; } = string.Empty;
    internal string CountryCode { get; set; } = string.Empty;
    internal string State { get; set; } = string.Empty;
    internal string City { get; set; } = string.Empty;
    internal string Sublocation { get; set; } = string.Empty;
    internal double? Latitude { get; set; }
    internal double? Longitude { get; set; }

    /// <summary>The place names that are set, trimmed.</summary>
    internal IEnumerable<string> Names => new[] { Country, CountryCode, State, City, Sublocation }
                                         .Select(selector: name => (name ?? string.Empty).Trim())
                                         .Where(predicate: name => name.Length > 0 &&
                                                                   name != NullEquivalents.StringGeneric);

    private static bool SameNumber(double? a,
                                   double? b)
    {
        return a.HasValue == b.HasValue &&
               (!a.HasValue || Math.Abs(value: a.Value - b.Value) < SameCoordinate);
    }

    public bool Equals(GeoKeywordLocation other)
    {
        return other != null &&
               Names.SequenceEqual(second: other.Names, comparer: StringComparer.Ordinal) &&
               string.Equals(a: Country, b: other.Country, comparisonType: StringComparison.Ordinal) &&
               string.Equals(a: CountryCode, b: other.CountryCode, comparisonType: StringComparison.Ordinal) &&
               string.Equals(a: State, b: other.State, comparisonType: StringComparison.Ordinal) &&
               string.Equals(a: City, b: other.City, comparisonType: StringComparison.Ordinal) &&
               string.Equals(a: Sublocation, b: other.Sublocation, comparisonType: StringComparison.Ordinal) &&
               SameNumber(a: Latitude, b: other.Latitude) &&
               SameNumber(a: Longitude, b: other.Longitude);
    }

    public override bool Equals(object obj)
    {
        return Equals(other: obj as GeoKeywordLocation);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(obj: $"{Country}|{CountryCode}|{State}|{City}|{Sublocation}");
    }
}

/// <summary>
///     Keeps a photo's keywords in step with its location: when the location changes, the place-name keywords that
///     named the old location are swapped for ones naming the new, and the <c>geo:</c> pair is rewritten.
/// </summary>
/// <remarks>
///     <para>
///         The rule is deliberately narrow, because a keyword carries no sign of what it is. Only two kinds are ever
///         touched: a keyword that is <b>exactly</b> (ignoring case) one of the photo's own previous place names -
///         the values of its country, country code, state, city and sublocation fields before the edit - and the
///         <c>geo:lat=</c> / <c>geo:lon=</c> pair. Everything else (<c>mum</c>, <c>Bugyi</c>, a hand-typed
///         <c>holiday</c>) is left alone. A place name the user had already typed as a keyword and that is also
///         the new name stays where it was.
///     </para>
///     <para>It does nothing when the user has edited the keywords themselves in the same session.</para>
/// </remarks>
internal static class GeoKeywordSync
{
    private const string LatitudePrefix = "geo:lat=";
    private const string LongitudePrefix = "geo:lon=";

    /// <summary>
    ///     The keywords after a location change. Returns <paramref name="keywords" /> itself when the location did not change.
    /// </summary>
    /// <param name="keywords">The keywords the photo has now.</param>
    /// <param name="before">The location as it is in the file.</param>
    /// <param name="after">The location as it will be written.</param>
    internal static TagList Apply(TagList keywords,
                                  GeoKeywordLocation before,
                                  GeoKeywordLocation after)
    {
        if (before.Equals(other: after))
        {
            return keywords;
        }

        HashSet<string> newNames = new(collection: after.Names, comparer: StringComparer.OrdinalIgnoreCase);
        HashSet<string> goneNames = new(
            collection: before.Names.Where(predicate: name => !newNames.Contains(item: name)),
            comparer: StringComparer.OrdinalIgnoreCase);

        List<string> kept = keywords.Where(predicate: keyword => !goneNames.Contains(item: keyword) &&
                                                                  !keyword.StartsWith(value: LatitudePrefix,
                                                                      comparisonType: StringComparison.OrdinalIgnoreCase) &&
                                                                  !keyword.StartsWith(value: LongitudePrefix,
                                                                      comparisonType: StringComparison.OrdinalIgnoreCase))
                                    .ToList();

        HashSet<string> present = new(collection: kept, comparer: StringComparer.OrdinalIgnoreCase);
        foreach (string name in after.Names)
        {
            if (present.Add(item: name))
            {
                kept.Add(item: name);
            }
        }

        if (after.Latitude.HasValue)
        {
            kept.Add(item: LatitudePrefix + Coordinate(value: after.Latitude.Value));
        }

        if (after.Longitude.HasValue)
        {
            kept.Add(item: LongitudePrefix + Coordinate(value: after.Longitude.Value));
        }

        return TagList.From(items: kept);
    }

    private static string Coordinate(double value)
    {
        return value.ToString(format: "0.00000000", provider: CultureInfo.InvariantCulture);
    }

    private static readonly ElementAttribute[] LocationAttributes =
    [
        ElementAttribute.Country,
        ElementAttribute.CountryCode,
        ElementAttribute.State,
        ElementAttribute.City,
        ElementAttribute.Sublocation,
        ElementAttribute.GPSLatitude,
        ElementAttribute.GPSLongitude
    ];

    private static string TextOf(DirectoryElement element,
                                 ElementAttribute attribute,
                                 DirectoryElement.AttributeVersion? version)
    {
        string text = element.GetAttributeValueAsString(attribute: attribute,
            version: version,
            notFoundValue: string.Empty,
            context: ValueFormatContext.RoundTrip);
        return text == NullEquivalents.StringGeneric
            ? string.Empty
            : text.Trim();
    }

    private static double? NumberOf(string text)
    {
        return AttributeValueFormatter.TryParseDouble(value: text,
            context: ValueFormatContext.RoundTrip,
            result: out double number)
            ? number
            : null;
    }

    private static GeoKeywordLocation Snapshot(DirectoryElement element,
                                               DirectoryElement.AttributeVersion? version)
    {
        return new GeoKeywordLocation
        {
            Country = TextOf(element: element, attribute: ElementAttribute.Country, version: version),
            CountryCode = TextOf(element: element, attribute: ElementAttribute.CountryCode, version: version),
            State = TextOf(element: element, attribute: ElementAttribute.State, version: version),
            City = TextOf(element: element, attribute: ElementAttribute.City, version: version),
            Sublocation = TextOf(element: element, attribute: ElementAttribute.Sublocation, version: version),
            Latitude = NumberOf(text: TextOf(element: element, attribute: ElementAttribute.GPSLatitude,
                version: version)),
            Longitude = NumberOf(text: TextOf(element: element, attribute: ElementAttribute.GPSLongitude,
                version: version))
        };
    }

    /// <summary>
    ///     Stages the keywords of a photo that is about to be written so that they follow its pending location edit.
    /// </summary>
    /// <returns>True if keywords were staged.</returns>
    internal static bool StageFor(DirectoryElement element)
    {
        // the user has taken charge of the keywords for this save
        if (element.HasSpecificAttributeWithVersion(attribute: ElementAttribute.IPTCKeywords,
                version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite) ||
            !LocationAttributes.Any(predicate: attribute => element.HasSpecificAttributeWithVersion(
                attribute: attribute,
                version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite)))
        {
            return false;
        }

        TagList current = TagList.Parse(text: element.GetAttributeValueAsString(
            attribute: ElementAttribute.IPTCKeywords,
            notFoundValue: string.Empty,
            context: ValueFormatContext.RoundTrip));

        TagList updated = Apply(keywords: current,
            before: Snapshot(element: element, version: DirectoryElement.AttributeVersion.Original),
            after: Snapshot(element: element, version: null));

        if (updated.Equals(other: current))
        {
            return false;
        }

        element.SetAttributeValue(attribute: ElementAttribute.IPTCKeywords,
            value: updated,
            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
            isMarkedForDeletion: false);
        return true;
    }
}
