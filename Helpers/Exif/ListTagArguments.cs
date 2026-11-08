using System.Collections.Generic;
using System.Linq;
using GeoTagNinja.Model;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     Builds the ExifTool argument lines that write a multi-value tag. Kept separate from
///     <c>WriteSaveToFile</c> so the rule can be tested without a file or an ExifTool process.
/// </summary>
internal static class ListTagArguments
{
    /// <summary>
    ///     The lines that replace the whole content of a list tag. ExifTool builds a list from repeated assignments
    ///     (<c>-Tag=a -Tag=b</c> gives two items and drops whatever was there), whereas <c>-Tag=a, b</c> would store a
    ///     single item containing a comma. An empty list yields one empty assignment, which deletes the tag. (Emptying
    ///     first and then appending with <c>+=</c> looks equivalent but does not work: ExifTool keeps the old items.)
    /// </summary>
    /// <param name="tag">The ExifTool tag name, with its group, e.g. <c>IPTC:Keywords</c>.</param>
    /// <param name="items">The items to store.</param>
    internal static IReadOnlyList<string> Build(string tag,
                                                TagList items)
    {
        return items.IsEmpty
            ? [$"-{tag}="]
            : items.Select(selector: item => $"-{tag}={item}")
                   .ToList();
    }
}
