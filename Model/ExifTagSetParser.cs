using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Model;

/// <summary>
///     Turns a set of raw ExifTool tags into typed attribute values.
/// </summary>
/// <remarks>
///     <para>
///         There is one reader, not two. A file read off disk arrives here as the tag dictionary ExifTool's
///         <c>GetProperties</c> produced; the track-file overlay arrives here as the sidecar XMP flattened into the
///         same shape by <see cref="TagsFromExifToolDataTable" />. Both then run the same per-attribute clean-up in
///         <see cref="TagsToModelValueTransformations.TransformTagValue" />, so a value cannot mean one thing when it
///         came from a photo and another when it came from a GPX track.
///     </para>
///     <para>
///         The output is always the CLR type <see cref="GetElementAttributesType" /> declares for the attribute -
///         never text that happens to look like a number or a date. See <c>docs/architecture-values.md</c>.
///     </para>
/// </remarks>
internal static class ExifTagSetParser
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Attributes that are never read from tags: they are either composed from other attributes (the coordinate
    ///     pairs), are UI-only edit helpers (the date shifts), or belong to the application rather than the file.
    /// </summary>
    private static readonly List<ElementAttribute> IgnoredAttributes =
    [
        ElementAttribute.Coordinates,
        ElementAttribute.DestCoordinates,
        ElementAttribute.TakenDateDaysShift,
        ElementAttribute.TakenDateHoursShift,
        ElementAttribute.TakenDateMinutesShift,
        ElementAttribute.TakenDateSecondsShift,
        ElementAttribute.CreateDateDaysShift,
        ElementAttribute.CreateDateHoursShift,
        ElementAttribute.CreateDateMinutesShift,
        ElementAttribute.CreateDateSecondsShift,
        ElementAttribute.RemoveAllGPS,
        ElementAttribute.GUID,
        ElementAttribute.Folder
    ];

    /// <summary>
    ///     Flattens the two-column table the track-file reader builds ("attribute", "TagValue") into the tag
    ///     dictionary this parser expects.
    /// </summary>
    /// <remarks>
    ///     Blank values are dropped rather than carried through. The table has a row for every property of the
    ///     sidecar's XMP description whether the track held it or not, and a present-but-empty row would otherwise
    ///     satisfy the lookup and stop it from trying the next candidate tag for that attribute.
    /// </remarks>
    /// <param name="dtFileExif">Raw ExifTool output; rows such as "exif:GPSLatitude" and their values.</param>
    /// <returns>Tag name (upper-cased) to value.</returns>
    internal static IDictionary<string, string> TagsFromExifToolDataTable(DataTable dtFileExif)
    {
        IDictionary<string, string> tags = new Dictionary<string, string>();
        if (dtFileExif == null)
        {
            return tags;
        }

        foreach (DataRow row in dtFileExif.Rows)
        {
            string tagName = row[columnIndex: 0]
              ?.ToString();
            string tagValue = row[columnIndex: 1]
              ?.ToString();

            if (string.IsNullOrEmpty(value: tagName) ||
                string.IsNullOrEmpty(value: tagValue))
            {
                continue;
            }

            tags[key: tagName.ToUpper()] = tagValue;
        }

        return tags;
    }

    /// <summary>
    ///     Parses every attribute that can be established from the given tags.
    /// </summary>
    /// <param name="dictTagsIn">The tags to parse, keyed by ExifTool tag name in any casing.</param>
    /// <returns>The typed values that could be established. Attributes not in it were not present or not parseable.</returns>
    internal static IDictionary<ElementAttribute, IConvertible> ParseTagSet(
        IDictionary<string, string> dictTagsIn)
    {
        // Create an upper-case capitalised version for case-insensitive lookup
        IDictionary<string, string> tags = new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> kvp in dictTagsIn)
        {
            tags[key: kvp.Key.ToUpper()] = kvp.Value;
        }

        IDictionary<ElementAttribute, IConvertible> parsedValues =
            new Dictionary<ElementAttribute, IConvertible>();
        List<ElementAttribute> parsedFails = [];

        foreach (ElementAttribute attribute in (IEnumerable<ElementAttribute>)Enum.GetValues(
                     enumType: typeof(ElementAttribute)))
        {
            if (!parsedValues.ContainsKey(key: attribute))
            {
                _ = ParseAttribute(
                    attribute: attribute,
                    parsedValues: parsedValues,
                    parsedFails: parsedFails,
                    tags: tags,
                    callDepth: 0);
            }
        }

        return parsedValues;
    }

    /// <summary>
    ///     Handles retrieving the value for the given attribute into the temporary value list.
    ///     If required (due to dependencies for conversion), another value retrieval is triggered. All results are
    ///     put into the passed lists.
    /// </summary>
    /// <param name="attribute">The attribute to retrieve the value for</param>
    /// <param name="parsedValues">The list of already parsed values (in case another value is needed)</param>
    /// <param name="parsedFails">The list of attributes, parsing failed for</param>
    /// <param name="tags">The tags provided to retrieve the value from, keyed upper-case</param>
    /// <param name="callDepth">The recursive call depth to allow for tracking of loops</param>
    /// <returns>True if parsing succeeded (resulting values are put into the passed lists).</returns>
    private static bool ParseAttribute(ElementAttribute attribute,
                                       IDictionary<ElementAttribute, IConvertible> parsedValues,
                                       List<ElementAttribute> parsedFails,
                                       IDictionary<string, string> tags,
                                       int callDepth)
    {
        Log.Trace(
            message:
            $"Parse attribute '{GetElementAttributesName(attributeToFind: attribute)}' at depth {callDepth}...");
        if (parsedFails.Contains(item: attribute))
        {
            return false;
        }

        if (callDepth > 10)
        {
            throw new InvalidOperationException(
                message:
                $"Reached max call depth of '{callDepth}' while parsing attribute '{GetElementAttributesName(attributeToFind: attribute)}'.");
        }

        callDepth++;
        string parseResultStr = GetDataPointFromTags(attribute: attribute, tags: tags);

        // TakenDate & CreateDate have to be sent into their
        // respective tables for querying later if user chooses time-shift.
        // TODO: replace logic with AttributeVersion concept
        if (parseResultStr != null &&
            attribute is ElementAttribute.TakenDate or ElementAttribute.CreateDate &&
            parseResultStr.Contains(value: "0000"))
        {
            return false;
        }

        // If needed, transform the attribute
        IConvertible? resTyped = null;
        try
        {
            int depthForDependencies = callDepth;
            resTyped = TagsToModelValueTransformations.TransformTagValue(
                attribute: attribute,
                parseResult: parseResultStr,
                parsed_Values: parsedValues,
                ParseMissingAttribute: delegate(ElementAttribute atrb)
                {
                    return ParseAttribute(attribute: atrb,
                        parsedValues: parsedValues,
                        parsedFails: parsedFails,
                        tags: tags,
                        callDepth: depthForDependencies);
                });
        }
        catch
        {
            Log.Error(
                message:
                $"Parse error for attribute '{attribute}': parseResultStr: {parseResultStr}, parsedValues: {parsedValues}.");
        }

        // Add it to the lists
        if (resTyped == null)
        {
            parsedFails.Add(item: attribute);
            return false;
        }

        parsedValues[key: attribute] = resTyped;
        return true;
    }

    /// <summary>
    ///     Gets the value for the given attribute from the tags, trying the tags that can hold it in order of
    ///     preference.
    /// </summary>
    /// <param name="attribute">The attribute to find a value for</param>
    /// <param name="tags">The tags to look in, keyed upper-case</param>
    /// <returns>The raw value, or null when no tag held it.</returns>
    private static string GetDataPointFromTags(ElementAttribute attribute,
                                               IDictionary<string, string> tags)
    {
        Log.Trace(message:
            $"Starting to parse dict for attribute: {GetElementAttributesName(attributeToFind: attribute)}");

        if (!IgnoredAttributes.Contains(item: attribute))
        {
            List<string> tagsWithAttributesIn =
                GetElementAttributesIn(attributeToFind: attribute);
            for (int i = 0; i < tagsWithAttributesIn.Count; i++)
            {
                if (tags.ContainsKey(key: tagsWithAttributesIn[index: i]
                       .ToUpper()))
                {
                    Log.Trace(
                        message:
                        $"Parse dict for attribute: '{GetElementAttributesName(attributeToFind: attribute)}' yielded generatedValue '{tags[key: tagsWithAttributesIn[index: i].ToUpper()]}'");
                    return tags[key: tagsWithAttributesIn[index: i]
                       .ToUpper()];
                }
            }
        }

        Log.Trace(
            message:
            $"Parse dict for attribute: '{GetElementAttributesName(attributeToFind: attribute)}' yielded no generatedValue.");
        return null;
    }
}
