using System.Linq;

namespace GeoTagNinja.Helpers.Text;

/// <summary>
///     Small string utilities that have no better home.
/// </summary>
internal static class StringOperations
{
    /// <summary>
    ///     Returns the first argument that actually carries text.
    /// </summary>
    /// <remarks>
    ///     Unlike the null-coalescing operator this also skips empty and whitespace-only values, which is what the
    ///     folder-navigation code needs: several of the paths it juggles come back as empty strings rather than null.
    /// </remarks>
    /// <param name="strings">Candidate values, in order of preference.</param>
    /// <returns>The first non-blank value, or <see langword="null" /> if they are all blank.</returns>
    internal static string Coalesce(params string[] strings)
    {
        return strings.FirstOrDefault(predicate: s => !string.IsNullOrWhiteSpace(value: s));
    }
}
