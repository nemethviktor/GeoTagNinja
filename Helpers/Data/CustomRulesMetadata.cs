using System;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Describes which data points the user-defined toponomy rules may read, test and overwrite.
/// </summary>
/// <remarks>
///     This is what populates the drop-downs on the Custom Rules tab of the settings dialog; the values are
///     also persisted, so changing a string here changes how existing saved rules are interpreted.
/// </remarks>
internal static class CustomRulesMetadata
{
    /// <summary>
    /// Gets an array of element attributes that represent geographical locations.
    /// </summary>
    /// <returns>An array of <see cref="ElementAttribute"/> values, including City, State, Sublocation, and GPSAltitude.</returns>
    internal static ElementAttribute[] ToponomyReplaces()
    {
        ElementAttribute[] result =
        {
            ElementAttribute.City,
            ElementAttribute.State,
            ElementAttribute.Sublocation,
            ElementAttribute.GPSAltitude
        };
        return result;
    }

    /// <summary>
    ///     City, State, Sublocation
    /// </summary>
    /// <returns></returns>
    internal static ElementAttribute[] CustomRulesDataTargets()
    {
        ElementAttribute[] result =
        {
            ElementAttribute.State,
            ElementAttribute.City,
            ElementAttribute.Sublocation
        };

        return result;
    }

    internal static string[] CustomCityLogicDataSources()
    {
        string[] result =
        {
            "AdminName1",
            "AdminName2",
            "AdminName3",
            "AdminName4",
            "ToponymName",
            "Undefined"
        };

        return result;
    }

    internal static string[] CustomRulesDataSources(bool isOutcome = false)
    {
        string[] result =
        {
            "AdminName1",
            "AdminName2",
            "AdminName3",
            "AdminName4",
            "ToponymName"
        };
        if (isOutcome)
        {
            Array.Resize(array: ref result, newSize: result.Length + 2);
            result[result.Length - 2] = "Null (empty)";
            result[result.Length - 1] = "Custom";
        }

        return result;
        ;
    }

    internal static string[] CustomRulesDataConditions()
    {
        string[] result =
        {
            "Is",
            "Contains",
            "StartsWith",
            "EndsWith"
        };
        return result;
    }
}
