using System;
using System.Collections.Generic;
using System.Text;

namespace GeoTagNinja.Model;

/// <summary>
///     Reads the structured XMP values ExifTool prints with <c>-struct -args</c>, which the flat tag dictionary of
///     the read pass cannot hold directly.
/// </summary>
internal static class ExifStructTags
{
    /// <summary>The key (upper-case, as the tag set is keyed) under which ExifTool reports the contact structure.</summary>
    private const string CreatorContactInfoKey = "XMP:CREATORCONTACTINFO";

    /// <summary>The structure's fields and the flattened tag each one is exposed as.</summary>
    private static readonly (string Field, string FlatTag)[] CreatorContactFields =
    [
        ("CiAdrExtadr", "XMP:CreatorAddress"),
        ("CiAdrCity", "XMP:CreatorCity"),
        ("CiAdrPcode", "XMP:CreatorPostalCode"),
        ("CiAdrRegion", "XMP:CreatorRegion"),
        ("CiAdrCtry", "XMP:CreatorCountry"),
        ("CiTelWork", "XMP:CreatorWorkTelephone"),
        ("CiEmailWork", "XMP:CreatorWorkEmail"),
        ("CiUrlWork", "XMP:CreatorWorkURL")
    ];

    /// <summary>
    ///     Adds the flattened creator contact tags to a tag set keyed in upper case, taken from the
    ///     <c>XMP:CreatorContactInfo</c> structure when the set has one. Tags already in the set win, so a file or
    ///     sidecar that does carry the flattened tags is not overwritten.
    /// </summary>
    internal static void ExpandCreatorContactInfo(IDictionary<string, string> tagsKeyedUpper)
    {
        if (!tagsKeyedUpper.TryGetValue(key: CreatorContactInfoKey, value: out string? raw) ||
            string.IsNullOrWhiteSpace(value: raw))
        {
            return;
        }

        IDictionary<string, string> fields = ParseStruct(text: raw);
        foreach ((string field, string flatTag) in CreatorContactFields)
        {
            if (fields.TryGetValue(key: field, value: out string? value) &&
                !tagsKeyedUpper.ContainsKey(key: flatTag.ToUpperInvariant()))
            {
                tagsKeyedUpper[key: flatTag.ToUpperInvariant()] = value;
            }
        }
    }

    /// <summary>
    ///     Parses ExifTool's flat structure text, <c>{Field=value,Field=value}</c>, in which a literal <c>|</c>,
    ///     <c>,</c>, <c>{</c> or <c>}</c> inside a value is escaped with a leading <c>|</c>. Nested structures are not
    ///     expanded (the contact structure has none).
    /// </summary>
    internal static IDictionary<string, string> ParseStruct(string text)
    {
        Dictionary<string, string> result = new(comparer: StringComparer.OrdinalIgnoreCase);
        string trimmed = text.Trim();
        if (trimmed.Length < 2 ||
            trimmed[index: 0] != '{' ||
            trimmed[index: trimmed.Length - 1] != '}')
        {
            return result;
        }

        StringBuilder key = new();
        StringBuilder value = new();
        bool inValue = false;

        void Commit()
        {
            if (key.Length > 0)
            {
                result[key: key.ToString()] = value.ToString();
            }

            _ = key.Clear();
            _ = value.Clear();
            inValue = false;
        }

        for (int i = 1; i < trimmed.Length - 1; i++)
        {
            char c = trimmed[index: i];
            if (c == '|' &&
                i + 1 < trimmed.Length - 1)
            {
                _ = (inValue
                    ? value
                    : key).Append(value: trimmed[index: ++i]);
            }
            else if (c == ',')
            {
                Commit();
            }
            else if (c == '=' &&
                     !inValue)
            {
                inValue = true;
            }
            else
            {
                _ = (inValue
                    ? value
                    : key).Append(value: c);
            }
        }

        Commit();
        return result;
    }
}
