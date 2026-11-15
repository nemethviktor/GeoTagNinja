using System.Collections.Generic;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
/// This is a hardcoded list of strings enforced as enums for making sure that column names
/// don't get mixed up with various localised names.
/// We just use them to return as string, as-is. 
/// Don't change the capitalisation unless there's a very good reason to do so.
/// </summary>
internal enum DefaultColumnNamesFromElementAttributesForFileEditing
{
    Distance,
    CountryCode,
    Country,
    City,
    State,
    Sublocation,
    GPSAltitude,
    timezoneId,
}

/// <summary>
///     Names of the columns in the toponomy data table returned by the GeoNames lookup.
/// </summary>
/// <remarks>
///     These are deliberately kept as hard-coded English identifiers and mapped to "clh_" column keys, so
///     that switching the display language cannot change how the data table is addressed in code.
/// </remarks>
internal static class ToponomyColumns
{
    /// <summary>
    /// Provides the default mapping of English item names to their corresponding database column headers.
    /// </summary>
    /// <remarks>This dictionary is used to translate standardized item names into database column headers,
    /// enabling consistent data retrieval and manipulation. The mapping is initialized with a predefined set of item
    /// names and their associated column header strings.</remarks>
    internal static Dictionary<string, string> DefaultEnglishNamesToColumnHeaders = new()
    {
        { nameof(DefaultColumnNamesFromElementAttributesForFileEditing.Distance), "clh_Distance" },
        { nameof(DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode), "clh_CountryCode" },
        { nameof(DefaultColumnNamesFromElementAttributesForFileEditing.Country), "clh_Country" },
        { nameof(DefaultColumnNamesFromElementAttributesForFileEditing.City), "clh_City" },
        { nameof(DefaultColumnNamesFromElementAttributesForFileEditing.State), "clh_State" },
        { nameof(DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation), "clh_Sublocation" },
        { nameof(DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude), "clh_GPSAltitude" },
        { nameof(DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId), "clh_timezoneId" },
    };
}
