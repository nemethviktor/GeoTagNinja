using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Geo;

/// <summary>
///     Stages a favourite's location details onto a photo that lies inside the favourite's area.
/// </summary>
/// <remarks>
///     Only favourites with Auto Assign ticked and a radius above zero take part. The photo keeps its own position
///     unless the favourite also has Snap ticked. Nothing is written to the file: the values are staged like any other
///     edit, so the user still reviews them and saves (or discards) as usual.
/// </remarks>
internal static class FavouriteAutoAssign
{
    private const double SameNumberTolerance = 1e-9;

    /// <summary>
    ///     Applies the matching auto-assign favourite, if any, to the photo.
    /// </summary>
    /// <returns>True if anything was staged.</returns>
    /// <remarks>
    ///     Safe to call over and over for the same photo: it only stages values that differ from what the photo
    ///     already shows, and it leaves a photo alone while that photo has edits of its own waiting, so it never
    ///     overwrites something the user typed.
    /// </remarks>
    internal static bool TryApply(DirectoryElement directoryElement,
                                  IEnumerable<Favourite> favourites)
    {
        if (directoryElement is not { Type: DirectoryElement.ElementType.File } ||
            directoryElement.HasDirtyAttributes())
        {
            return false;
        }

        if (!FavouriteMatcher.TryParsePosition(
                latitude: Text(de: directoryElement, attribute: ElementAttribute.GPSLatitude),
                latitudeRef: Text(de: directoryElement, attribute: ElementAttribute.GPSLatitudeRef),
                longitude: Text(de: directoryElement, attribute: ElementAttribute.GPSLongitude),
                longitudeRef: Text(de: directoryElement, attribute: ElementAttribute.GPSLongitudeRef),
                latitudeSigned: out double latitude,
                longitudeSigned: out double longitude))
        {
            return false;
        }

        Favourite favourite = FavouriteMatcher.FindAutoAssignMatch(favourites: favourites, latitude: latitude,
            longitude: longitude);
        if (favourite == null)
        {
            return false;
        }

        bool changed = false;

        // location details always follow the favourite...
        changed |= Stage(de: directoryElement, attribute: ElementAttribute.CountryCode, value: favourite.CountryCode);
        changed |= Stage(de: directoryElement, attribute: ElementAttribute.Country, value: favourite.Country);
        changed |= Stage(de: directoryElement, attribute: ElementAttribute.State, value: favourite.State);
        changed |= Stage(de: directoryElement, attribute: ElementAttribute.City, value: favourite.City);
        changed |= Stage(de: directoryElement, attribute: ElementAttribute.Sublocation, value: favourite.Sublocation);

        // ...but the position only does when asked to.
        if (favourite.Snap)
        {
            changed |= Stage(de: directoryElement, attribute: ElementAttribute.GPSLatitude,
                value: favourite.GPSLatitude);
            changed |= Stage(de: directoryElement, attribute: ElementAttribute.GPSLatitudeRef,
                value: favourite.GPSLatitudeRef);
            changed |= Stage(de: directoryElement, attribute: ElementAttribute.GPSLongitude,
                value: favourite.GPSLongitude);
            changed |= Stage(de: directoryElement, attribute: ElementAttribute.GPSLongitudeRef,
                value: favourite.GPSLongitudeRef);
            changed |= Stage(de: directoryElement, attribute: ElementAttribute.Coordinates,
                value: favourite.Coordinates);
            changed |= Stage(de: directoryElement, attribute: ElementAttribute.GPSAltitude,
                value: favourite.GPSAltitude);
            changed |= Stage(de: directoryElement, attribute: ElementAttribute.GPSAltitudeRef,
                value: favourite.GPSAltitudeRef);
        }

        return changed;
    }

    private static string Text(DirectoryElement de,
                               ElementAttribute attribute)
    {
        return de.GetAttributeValueAsString(attribute: attribute, notFoundValue: "",
            context: ValueFormatContext.RoundTrip);
    }

    /// <summary>
    ///     Stages one value, unless the favourite has nothing for it or the photo already shows the same.
    /// </summary>
    private static bool Stage(DirectoryElement de,
                              ElementAttribute attribute,
                              string value)
    {
        if (string.IsNullOrWhiteSpace(value: value))
        {
            return false;
        }

        if (IsSame(current: Text(de: de, attribute: attribute), wanted: value, attribute: attribute))
        {
            return false;
        }

        de.SetAttributeValueAnyType(attribute: attribute, value: value,
            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite, isMarkedForDeletion: false,
            context: ValueFormatContext.RoundTrip);
        return true;
    }

    private static bool IsSame(string current,
                               string wanted,
                               ElementAttribute attribute)
    {
        if (GetElementAttributesType(attributeToFind: attribute) == typeof(double) &&
            double.TryParse(s: current, style: NumberStyles.Float, provider: CultureInfo.InvariantCulture,
                result: out double currentNumber) &&
            double.TryParse(s: wanted, style: NumberStyles.Float, provider: CultureInfo.InvariantCulture,
                result: out double wantedNumber))
        {
            return Math.Abs(value: currentNumber - wantedNumber) < SameNumberTolerance;
        }

        return string.Equals(a: current?.Trim(), b: wanted.Trim(), comparisonType: StringComparison.Ordinal);
    }
}
