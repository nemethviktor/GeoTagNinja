using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     One choosable item of a template or of "Load from Image": a field of the Edit Data form, or a few attributes
///     that only make sense together (a position is a latitude, a longitude and their references).
/// </summary>
internal sealed class TemplateGroup
{
    internal TemplateGroup(string key,
                           string english,
                           bool checkedByDefault,
                           params ElementAttribute[] attributes)
    {
        Key = key;
        English = english;
        CheckedByDefault = checkedByDefault;
        Attributes = attributes;
    }

    /// <summary>Stable key, also the caption's key in the language files (<c>lbl_...</c> or <c>chk_TemplateGroup_...</c>).</summary>
    internal string Key { get; }

    /// <summary>The caption when a language has none.</summary>
    internal string English { get; }

    /// <summary>Whether the item starts ticked. Dates and the time zone start unticked: they rarely belong in a template.</summary>
    internal bool CheckedByDefault { get; }

    /// <summary>The attributes the item carries.</summary>
    internal ElementAttribute[] Attributes { get; }
}

/// <summary>
///     What a template or "Load from Image" can carry, and how to read it from and put it onto a file.
/// </summary>
internal static class EditTemplateGroups
{
    internal static readonly IReadOnlyList<TemplateGroup> All =
    [
        new(key: "lbl_Category", english: "Category", checkedByDefault: true, ElementAttribute.Category),
        new(key: "lbl_SupplementalCategories", english: "Supplemental Categories", checkedByDefault: true,
            ElementAttribute.SupplementalCategories),
        new(key: "lbl_IPTCKeywords", english: "Keywords", checkedByDefault: true, ElementAttribute.IPTCKeywords),
        new(key: "lbl_Artist", english: "Artist", checkedByDefault: true, ElementAttribute.Artist),
        new(key: "lbl_CaptionWriter", english: "Caption Writer", checkedByDefault: true,
            ElementAttribute.CaptionWriter),
        new(key: "lbl_Source", english: "Source", checkedByDefault: true, ElementAttribute.Source),
        new(key: "lbl_Credit", english: "Credit", checkedByDefault: true, ElementAttribute.Credit),
        new(key: "lbl_SourceURL", english: "URL", checkedByDefault: true, ElementAttribute.SourceURL),
        new(key: "lbl_Copyright", english: "Copyright", checkedByDefault: true, ElementAttribute.Copyright),
        new(key: "lbl_ObjectName", english: "Object Name", checkedByDefault: true, ElementAttribute.ObjectName),
        new(key: "lbl_Headline", english: "Headline", checkedByDefault: true, ElementAttribute.Headline),
        new(key: "lbl_Caption", english: "Caption", checkedByDefault: true, ElementAttribute.Caption),
        new(key: "lbl_SpecialInstructions", english: "Special Instructions", checkedByDefault: true,
            ElementAttribute.SpecialInstructions),
        new(key: "lbl_Byline", english: "Byline", checkedByDefault: true, ElementAttribute.Byline),
        new(key: "lbl_BylineTitle", english: "Byline Title", checkedByDefault: true, ElementAttribute.BylineTitle),
        new(key: "lbl_CreatorAddress", english: "Address", checkedByDefault: true, ElementAttribute.CreatorAddress),
        new(key: "lbl_CreatorCity", english: "City", checkedByDefault: true, ElementAttribute.CreatorCity),
        new(key: "lbl_CreatorPostalCode", english: "Postal Code", checkedByDefault: true,
            ElementAttribute.CreatorPostalCode),
        new(key: "lbl_CreatorRegion", english: "State/Province", checkedByDefault: true,
            ElementAttribute.CreatorRegion),
        new(key: "lbl_CreatorCountry", english: "Country", checkedByDefault: true, ElementAttribute.CreatorCountry),
        new(key: "lbl_CreatorPhone", english: "Phone", checkedByDefault: true, ElementAttribute.CreatorPhone),
        new(key: "lbl_CreatorEmail", english: "E-Mail", checkedByDefault: true, ElementAttribute.CreatorEmail),
        new(key: "lbl_CreatorURL", english: "URL", checkedByDefault: true, ElementAttribute.CreatorURL),
        new(key: "chk_TemplateGroup_Country", english: "Country", checkedByDefault: true, ElementAttribute.CountryCode,
            ElementAttribute.Country),
        new(key: "chk_TemplateGroup_State", english: "State/Province", checkedByDefault: true,
            ElementAttribute.State),
        new(key: "chk_TemplateGroup_City", english: "City", checkedByDefault: true, ElementAttribute.City),
        new(key: "chk_TemplateGroup_Sublocation", english: "Sublocation", checkedByDefault: true,
            ElementAttribute.Sublocation),
        new(key: "chk_TemplateGroup_Position", english: "GPS position", checkedByDefault: true,
            ElementAttribute.GPSLatitude, ElementAttribute.GPSLatitudeRef, ElementAttribute.GPSLongitude,
            ElementAttribute.GPSLongitudeRef, ElementAttribute.Coordinates),
        new(key: "chk_TemplateGroup_Altitude", english: "Altitude", checkedByDefault: true,
            ElementAttribute.GPSAltitude, ElementAttribute.GPSAltitudeRef),
        new(key: "chk_TemplateGroup_Direction", english: "Image direction", checkedByDefault: true,
            ElementAttribute.GPSImgDirection, ElementAttribute.GPSImgDirectionRef),
        new(key: "chk_TemplateGroup_Destination", english: "Destination", checkedByDefault: true,
            ElementAttribute.GPSDestLatitude, ElementAttribute.GPSDestLatitudeRef, ElementAttribute.GPSDestLongitude,
            ElementAttribute.GPSDestLongitudeRef, ElementAttribute.DestCoordinates),
        new(key: "chk_TemplateGroup_TakenDate", english: "Taken date", checkedByDefault: false,
            ElementAttribute.TakenDate),
        new(key: "chk_TemplateGroup_CreateDate", english: "Created date", checkedByDefault: false,
            ElementAttribute.CreateDate),
        new(key: "chk_TemplateGroup_OffsetTime", english: "Time zone offset", checkedByDefault: false,
            ElementAttribute.OffsetTime)
    ];

    private static string Clean(string text)
    {
        return text == null || text.Trim() == NullEquivalents.StringGeneric
            ? string.Empty
            : text.Trim();
    }

    /// <summary>
    ///     The attributes a photo currently shows (its newest version of each, pending edits included), as invariant
    ///     text. Attributes with nothing to show are left out.
    /// </summary>
    internal static Dictionary<ElementAttribute, string> Capture(DirectoryElement element)
    {
        Dictionary<ElementAttribute, string> values = [];
        foreach (ElementAttribute attribute in All.SelectMany(selector: group => group.Attributes))
        {
            string text = Clean(text: element.GetAttributeValueAsString(attribute: attribute,
                notFoundValue: string.Empty,
                context: ValueFormatContext.RoundTrip));

            if (text.Length > 0 &&
                !IsBlankDate(attribute: attribute, text: text))
            {
                values[key: attribute] = text;
            }
        }

        return values;
    }

    /// <summary>The same, from values parsed straight off another file (the "Load from Image" route).</summary>
    internal static Dictionary<ElementAttribute, string> Capture(IDictionary<ElementAttribute, IConvertible> parsed)
    {
        Dictionary<ElementAttribute, string> values = [];
        foreach (ElementAttribute attribute in All.SelectMany(selector: group => group.Attributes))
        {
            if (parsed.TryGetValue(key: attribute, value: out IConvertible value))
            {
                string text = Clean(text: AttributeValueFormatter.Format(value: value,
                    context: ValueFormatContext.RoundTrip));
                if (text.Length > 0 &&
                    !IsBlankDate(attribute: attribute, text: text))
                {
                    values[key: attribute] = text;
                }
            }
        }

        return values;
    }

    /// <summary>An unset date reads back as the type's blank value, which is not something to carry over.</summary>
    private static bool IsBlankDate(ElementAttribute attribute,
                                    string text)
    {
        return GetElementAttributesType(attributeToFind: attribute) == typeof(DateTime) &&
               AttributeValueFormatter.TryParseDateTime(value: text,
                   context: ValueFormatContext.RoundTrip,
                   result: out DateTime parsed) &&
               parsed == NullEquivalents.DateTime;
    }

    /// <summary>The groups that have at least one attribute with a value in <paramref name="values" />.</summary>
    internal static List<TemplateGroup> GroupsWithValues(IReadOnlyDictionary<ElementAttribute, string> values)
    {
        return All.Where(predicate: group => group.Attributes.Any(predicate: values.ContainsKey))
                  .ToList();
    }

    /// <summary>A short rendering of a group's value for a list: the values joined, line breaks flattened, shortened.</summary>
    internal static string Preview(TemplateGroup group,
                                   IReadOnlyDictionary<ElementAttribute, string> values)
    {
        string text = string.Join(separator: ", ",
            values: group.Attributes
                         .Where(predicate: attribute => values.ContainsKey(key: attribute))
                         .Where(predicate: attribute => !GetElementAttributesName(attributeToFind: attribute)
                            .EndsWith(value: "Ref", comparisonType: StringComparison.Ordinal))
                         .Select(selector: attribute => values[key: attribute]
                                                       .Replace(oldValue: "\r", newValue: string.Empty)
                                                       .Replace(oldValue: "\n", newValue: ", ")));
        return text.Length > 50
            ? text.Substring(startIndex: 0, length: 47) + "..."
            : text;
    }

    /// <summary>Keys values by attribute name (for the template table).</summary>
    internal static Dictionary<string, string> ToNamed(IEnumerable<KeyValuePair<ElementAttribute, string>> values)
    {
        return values.ToDictionary(keySelector: pair => GetElementAttributesName(attributeToFind: pair.Key),
            elementSelector: pair => pair.Value);
    }

    /// <summary>Reads values by attribute name back, ignoring names this version does not know.</summary>
    internal static Dictionary<ElementAttribute, string> FromNamed(IReadOnlyDictionary<string, string> named)
    {
        Dictionary<ElementAttribute, string> values = [];
        foreach (ElementAttribute attribute in All.SelectMany(selector: group => group.Attributes))
        {
            if (named.TryGetValue(key: GetElementAttributesName(attributeToFind: attribute), value: out string text) &&
                !string.IsNullOrWhiteSpace(value: text))
            {
                values[key: attribute] = text;
            }
        }

        return values;
    }
}
