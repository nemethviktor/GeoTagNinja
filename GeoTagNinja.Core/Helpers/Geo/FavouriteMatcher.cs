using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GeoTagNinja.Helpers.Geo;

/// <summary>
///     Works out which favourite, if any, a position falls inside. Pure maths on text and numbers, so it is tested
///     without a photo or a database.
/// </summary>
internal static class FavouriteMatcher
{
    private const double EarthRadiusMetres = 6371008.8;

    /// <summary>
    ///     Great-circle distance between two positions, in metres (haversine).
    /// </summary>
    internal static double DistanceMetres(double latitude1,
                                          double longitude1,
                                          double latitude2,
                                          double longitude2)
    {
        double lat1 = ToRadians(degrees: latitude1);
        double lat2 = ToRadians(degrees: latitude2);
        double deltaLat = lat2 - lat1;
        double deltaLon = ToRadians(degrees: longitude2 - longitude1);

        double a = (Math.Sin(a: deltaLat / 2) * Math.Sin(a: deltaLat / 2)) +
                   (Math.Cos(d: lat1) * Math.Cos(d: lat2) * Math.Sin(a: deltaLon / 2) * Math.Sin(a: deltaLon / 2));

        return 2 * EarthRadiusMetres * Math.Asin(d: Math.Min(val1: 1.0, val2: Math.Sqrt(d: a)));
    }

    /// <summary>
    ///     Reads a signed position from the way the app keeps one: unsigned invariant numbers, each with a
    ///     reference ("North", "S", "West" ...). A southern or western reference makes the value negative; any other
    ///     (or missing) reference leaves the number as written.
    /// </summary>
    /// <returns>False if either number is missing, unreadable or out of range.</returns>
    internal static bool TryParsePosition(string latitude,
                                          string latitudeRef,
                                          string longitude,
                                          string longitudeRef,
                                          out double latitudeSigned,
                                          out double longitudeSigned)
    {
        latitudeSigned = 0;
        longitudeSigned = 0;

        if (!TryParseSigned(value: latitude, reference: latitudeRef, negativeInitial: 'S', result: out double lat) ||
            !TryParseSigned(value: longitude, reference: longitudeRef, negativeInitial: 'W', result: out double lon) ||
            Math.Abs(value: lat) > 90 ||
            Math.Abs(value: lon) > 180)
        {
            return false;
        }

        latitudeSigned = lat;
        longitudeSigned = lon;
        return true;
    }

    /// <summary>
    ///     The position of a favourite.
    /// </summary>
    internal static bool TryGetPosition(Favourite favourite,
                                        out double latitude,
                                        out double longitude)
    {
        latitude = 0;
        longitude = 0;

        return favourite != null &&
               TryParsePosition(latitude: favourite.GPSLatitude, latitudeRef: favourite.GPSLatitudeRef,
                   longitude: favourite.GPSLongitude, longitudeRef: favourite.GPSLongitudeRef,
                   latitudeSigned: out latitude, longitudeSigned: out longitude);
    }

    /// <summary>
    ///     The auto-assign favourite whose area contains the position. Where several do, the one whose centre is
    ///     nearest wins, and a tie goes to the name that sorts first, so the answer never depends on list order.
    /// </summary>
    /// <returns>The favourite, or null if none matches.</returns>
    internal static Favourite FindAutoAssignMatch(IEnumerable<Favourite> favourites,
                                                  double latitude,
                                                  double longitude)
    {
        return (favourites ?? [])
              .Where(predicate: favourite => favourite is { AutoAssign: true, Radius: > 0 })
              .Select(selector: favourite => TryGetPosition(favourite: favourite, latitude: out double favLat,
                                                 longitude: out double favLon)
                  ? (Favourite: favourite,
                      Distance: DistanceMetres(latitude1: latitude, longitude1: longitude, latitude2: favLat,
                          longitude2: favLon))
                  : (Favourite: favourite, Distance: double.PositiveInfinity))
              .Where(predicate: candidate => candidate.Distance <= candidate.Favourite.Radius)
              .OrderBy(keySelector: candidate => candidate.Distance)
              .ThenBy(keySelector: candidate => candidate.Favourite.FavouriteName,
                   comparer: StringComparer.OrdinalIgnoreCase)
              .Select(selector: candidate => candidate.Favourite)
              .FirstOrDefault();
    }

    private static bool TryParseSigned(string value,
                                       string reference,
                                       char negativeInitial,
                                       out double result)
    {
        if (!double.TryParse(s: value, style: NumberStyles.Float, provider: CultureInfo.InvariantCulture,
                result: out result) ||
            double.IsNaN(d: result) ||
            double.IsInfinity(d: result))
        {
            result = 0;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(value: reference) &&
            char.ToUpperInvariant(c: reference.Trim()[index: 0]) == negativeInitial)
        {
            result = -Math.Abs(value: result);
        }

        return true;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
