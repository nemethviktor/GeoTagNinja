using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoTagNinja.Helpers.Geo;

/// <summary>
///     The fields the Manage Favourites tree can be grouped by. The order here is the order the levels nest in.
/// </summary>
internal enum FavouriteGroupKey
{
    CountryCode,
    Country,
    State,
    City,
    Coordinates
}

/// <summary>
///     Works out where a favourite sits in the grouped tree of the Manage Favourites dialog.
/// </summary>
internal static class FavouriteGrouping
{
    /// <summary>
    ///     The groups to start with: country, state and city, as in GeoSetter.
    /// </summary>
    internal static readonly IReadOnlyList<FavouriteGroupKey> DefaultKeys =
        [FavouriteGroupKey.Country, FavouriteGroupKey.State, FavouriteGroupKey.City];

    /// <summary>
    ///     The value of one grouping field for a favourite, or null if it has none.
    /// </summary>
    internal static string ValueOf(Favourite favourite,
                                   FavouriteGroupKey key)
    {
        string value = key switch
        {
            FavouriteGroupKey.CountryCode => favourite.CountryCode,
            FavouriteGroupKey.Country => favourite.Country,
            FavouriteGroupKey.State => favourite.State,
            FavouriteGroupKey.City => favourite.City,
            FavouriteGroupKey.Coordinates => favourite.Coordinates,
            _ => throw new ArgumentOutOfRangeException(paramName: nameof(key), actualValue: key,
                message: "Unknown grouping field.")
        };

        return string.IsNullOrWhiteSpace(value: value)
            ? null
            : value.Trim();
    }

    /// <summary>
    ///     The branch names leading to a favourite: one per ticked field, nested in enum order whatever order the
    ///     fields were ticked in. A favourite with no value for a field goes under <paramref name="blankLabel" />,
    ///     so it never disappears from the tree.
    /// </summary>
    internal static List<string> PathFor(Favourite favourite,
                                         IEnumerable<FavouriteGroupKey> keys,
                                         string blankLabel)
    {
        return (keys ?? [])
              .Distinct()
              .OrderBy(keySelector: key => key)
              .Select(selector: key => ValueOf(favourite: favourite, key: key) ?? blankLabel)
              .ToList();
    }

    /// <summary>
    ///     Favourites in the order the tree lists them: by name, ignoring case.
    /// </summary>
    internal static IEnumerable<Favourite> InDisplayOrder(IEnumerable<Favourite> favourites)
    {
        return favourites.OrderBy(keySelector: favourite => favourite.FavouriteName,
            comparer: StringComparer.CurrentCultureIgnoreCase);
    }

    /// <summary>
    ///     A name for a new favourite that no existing one already has: <paramref name="baseName" /> itself if it is
    ///     free, otherwise "<paramref name="baseName" /> (2)", "(3)" and so on.
    /// </summary>
    internal static string UniqueName(string baseName,
                                      IEnumerable<string> existingNames)
    {
        HashSet<string> taken = new(collection: existingNames ?? [], comparer: StringComparer.OrdinalIgnoreCase);
        if (!taken.Contains(item: baseName))
        {
            return baseName;
        }

        int counter = 2;
        while (taken.Contains(item: $"{baseName} ({counter})"))
        {
            counter++;
        }

        return $"{baseName} ({counter})";
    }
}
