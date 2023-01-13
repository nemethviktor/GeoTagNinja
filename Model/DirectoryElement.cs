using NLog;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.EntitySql;
using System.Globalization;
using System.IO;
using static GeoTagNinja.Model.DirectoryElement;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Model;

/// <summary>
/// An element in a folder/directory.
/// </summary>
public class DirectoryElement
{
    /// <summary>
    /// Classification of a directory element in terms of its type.
    /// </summary>
    public enum ElementType
    {
        Drive = 0,
        SubDirectory = 1,
        ParentDirectory = 2,
        File = 3,
        MyComputer = 4,
        Unknown = 99
    }

    /// <summary>
    /// Possible versions of an element. Initial loads receive the
    /// original version tag, updates the modified one.
    /// </summary>
    public enum AttributeVersion
    {
        Original,
        Modified
    }

    // We need a non generics super class that can be referenced
    // independent of the concrete values type (generic).
    public class AttributeValueContainer
    {
        internal Type _myValueType = null;
        internal IDictionary _valueDict = null;
        public Type MyValueType { get => _myValueType; }
        public IDictionary ValueDict { get => _valueDict; }
    }

    public class AttributeValuesString : AttributeValueContainer
    {
        public AttributeValuesString(string initialValue = null, AttributeVersion initialVersion = AttributeVersion.Original)
        {
            _myValueType = typeof(string);
            _valueDict = new Dictionary<AttributeVersion, string>();
            if (initialValue != null) _valueDict[initialVersion] = initialValue;
        }
    }

    public class AttributeValuesInt : AttributeValueContainer
    {
        public AttributeValuesInt(int? initialValue = null, AttributeVersion initialVersion = AttributeVersion.Original)
        {
            _myValueType = typeof(int);
            _valueDict = new Dictionary<AttributeVersion, int>();
            if (initialValue != null) _valueDict[initialVersion] = initialValue;
        }
    }

    public class AttributeValuesDouble : AttributeValueContainer
    {
        public AttributeValuesDouble(double? initialValue = null, AttributeVersion initialVersion = AttributeVersion.Original)
        {
            _myValueType = typeof(double);
            _valueDict = new Dictionary<AttributeVersion, double>();
            if (initialValue != null) _valueDict[initialVersion] = initialValue;
        }
    }


    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #region private variables

    private string _DisplayName;

    private IDictionary<ElementAttribute, AttributeValueContainer> _Attributes;

    #endregion

    #region Properties

    /// <summary>
    /// The element type (get only)
    /// </summary>
    public ElementType Type { get; }

    /// <summary>
    /// The fully qualified path incl. its name (get only)
    /// </summary>
    public string FullPathAndName { get; }

    /// <summary>
    /// The element name (get only)
    /// </summary>
    public string ItemName { get; }

    /// <summary>
    ///     Returns the set display name (text to display). If it was not
    ///     set, it returns the ItemName.
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (_DisplayName == null)
            {
                return ItemName;
            }

            return _DisplayName;
        }
        set => _DisplayName = value;
    }

    /// <summary>
    /// The extension (get only)
    /// </summary>
    public string Extension { get; }

    /// <summary>
    /// The sidecar file associated with this directory element.
    /// </summary>
    public string SidecarFile { set; get; }

    #endregion


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="itemName">The name of the element</param>
    /// <param name="type">The ElementType of it</param>
    /// <param name="fullPathAndName">The fully qualified path incl. its name</param>
    public DirectoryElement(string itemName,
                            ElementType type,
                            string fullPathAndName)
    {
        ItemName = itemName;
        Type = type;
        FullPathAndName = fullPathAndName;
        Extension = Path.GetExtension(path: FullPathAndName);
        
        _Attributes = new Dictionary<ElementAttribute, AttributeValueContainer>();
    }



    /// <summary>
    /// Checks the given value container for which version to return
    /// depending on the version requested.
    /// </summary>
    private AttributeVersion? CheckWhichVersion(AttributeValueContainer avc, AttributeVersion? versionRequested)
    {
        if ((versionRequested == null | versionRequested == AttributeVersion.Modified) & (avc.ValueDict.Contains(AttributeVersion.Modified)))
            return AttributeVersion.Modified;
        else if ((versionRequested == null | versionRequested == AttributeVersion.Original) & (avc.ValueDict.Contains(AttributeVersion.Original)))
            return AttributeVersion.Original;
        else
            // Version not found
            return null;
    }


    public string GetAttributeValueString(ElementAttribute attribute,
        AttributeVersion? version = null,
        string notFoundValue = null)
    {
        if (!_Attributes.ContainsKey(attribute))
            return notFoundValue;
        AttributeValueContainer avc = _Attributes[attribute];
        Type attributeType = avc.MyValueType;

        AttributeVersion? versionCheck = CheckWhichVersion(avc, version);
        if (versionCheck == null) return notFoundValue;
        AttributeVersion versionToReturn = (AttributeVersion)versionCheck;

        // Retrieve and return value
        if (attributeType == typeof(string))
        {
            IDictionary<AttributeVersion, string> str_dict = (IDictionary<AttributeVersion, string>)avc.ValueDict;
            return str_dict[versionToReturn];
        }
        else if (attributeType == typeof(int))
        {
            IDictionary<AttributeVersion, int> int_dict = (IDictionary<AttributeVersion, int>)avc.ValueDict;
            int int_value = int_dict[versionToReturn];
            return int_value.ToString(provider: CultureInfo.InvariantCulture);
        }
        else if (attributeType == typeof(double))
        {
            IDictionary<AttributeVersion, double> double_dict = (IDictionary<AttributeVersion, double>)avc.ValueDict;
            double double_value = double_dict[versionToReturn];
            return double_value.ToString(provider: CultureInfo.InvariantCulture);
        }

        // Should not get to here
        throw new ArgumentException($"Failed to retrieve attribute '{GetAttributeName(attribute)}" +
                $"' of type '{attributeType.Name}" +
                $"' by requesting its value with type 'string' due to conversion issues.");
    }


    /// <summary>
    /// Returns the value of an attribute.
    /// 
    /// If the version needed is given only that version is checked for and returned.
    /// If no version is given, the latest version (ie. modified) is returned if
    /// it exists, otherwise original.
    /// </summary>
    /// <param name="attribute">The attribute to return the value for</param>
    /// <param name="version">The version to return or null if latest version</param>
    /// <param name="notFoundValue">The value to return if no suitable value was found</param>
    /// <returns></returns>
    public Nullable<T> GetAttributeValue<T>(ElementAttribute attribute,
        AttributeVersion? version = null,
        Nullable<T> notFoundValue = null) where T: struct
    {
        if (!_Attributes.ContainsKey(attribute))
            return notFoundValue;
        AttributeValueContainer avc = _Attributes[attribute];

        // First check for matching type
        // requesting a string is always allowed
        Type requestType = typeof(T);
        Type attributeType = avc.MyValueType;
        if ((requestType != attributeType) & (requestType != typeof(string)))
            throw new ArgumentException($"Failed to retrieve attribute '{GetAttributeName(attribute)}" +
                $"' of type '{attributeType.Name}" +
                $"' due to requesting with incompatible return type '{requestType.Name}'.");

        AttributeVersion? versionCheck = CheckWhichVersion(avc, version);
        if (versionCheck == null) return notFoundValue;
        AttributeVersion versionToReturn = (AttributeVersion)versionCheck;

        // Retrieve and return value
        if (attributeType == typeof(int))
        {
            IDictionary<AttributeVersion, int> int_dict = (IDictionary<AttributeVersion, int>)avc.ValueDict;
            int int_value = int_dict[versionToReturn];
            if (requestType == typeof(int))
                return (T)Convert.ChangeType(int_value, typeof(T));
        }
        else if (attributeType == typeof(double))
        {
            IDictionary<AttributeVersion, double> double_dict = (IDictionary<AttributeVersion, double>)avc.ValueDict;
            double double_value = double_dict[versionToReturn];
            if (requestType == typeof(double))
                return (T)Convert.ChangeType(double_value, typeof(T));
        }

        // Should not get to here
        throw new ArgumentException($"Failed to retrieve attribute '{GetAttributeName(attribute)}" +
                $"' of type '{attributeType.Name}" +
                $"' by requesting its value with type '{requestType.Name}' due to conversion issues.");
    }


    /// <summary>
    /// Set the value for the given attribute.
    /// </summary>
    /// <param name="attribute">The attribute to set the value for</param>
    /// <param name="value">The value to set (as string)</param>
    /// <param name="version">The version to set it with (opt., defaults to Modified)</param>
    public void SetAttributeValue(ElementAttribute attribute, IConvertible value, AttributeVersion version = AttributeVersion.Modified)
    {
        Type attributeType = GetAttributeType(attribute);
        if (attributeType != value.GetType())
            throw new ArgumentException($"Error, while trying to set the attribute {GetAttributeName(attribute)}" +
                $" of item '{ItemName}'. The type '{value.GetType().Name}' of the value to set  does not contain " +
                $"match the expected type '{attributeType.Name}'.");

        if (_Attributes.ContainsKey(attribute))
        {
            _Attributes[attribute].ValueDict[version] = value;
            return;
        }

        AttributeValueContainer avc;
        if (attributeType == typeof(double))
            avc = new AttributeValuesDouble(initialValue: (double)value, initialVersion: version);
        else if (attributeType == typeof(int))
            avc = new AttributeValuesInt(initialValue: (int)value, initialVersion: version);
        else
            avc = new AttributeValuesString(initialValue: (string)value, initialVersion: version);
        _Attributes[attribute] = avc;
    }


    private (string, string) GetDataPointFromTags(ElementAttribute attribute, IDictionary<string, string> tags)
    {
        Logger.Trace("Starting to parse dict for attribute: " + GetAttributeName(attribute));

        if (!TagsToAttributesOrder.ContainsKey(attribute))
        {
            throw new ArgumentException($"Error, while trying to parse the dictionary of item '" +
                $"{ItemName}' for attribute '{GetAttributeName(attribute)}" +
                $"': The TagsToAttributesOrder does not contain a definition of which tags to use for this attribute.");
        }

        List<string> orderedTags = TagsToAttributesOrder[attribute];
        for (int i = 0; i < orderedTags.Count; i++)
        {
            if (tags.ContainsKey(orderedTags[i].ToUpper()))
            {
                Logger.Trace($"Parse dict for attribute: '{GetAttributeName(attribute)}" +
                    $"' yielded value '{tags[orderedTags[i].ToUpper()]}'");
                return (orderedTags[i], tags[orderedTags[i].ToUpper()]);
            }
        }

        Logger.Trace($"Parse dict for attribute: '{GetAttributeName(attribute)}" +
                    "' yielded no value.");
        return (null, null);
    }


    private bool ParseAttribute(ElementAttribute attribute,
        IDictionary<ElementAttribute, IConvertible> parsed_Values,
        List<ElementAttribute> parsed_Fails,
        IDictionary<string, string> tags,
        int call_depth)
    {
        Logger.Trace($"Parse attribute '{GetAttributeName(attribute)}' at depth {call_depth.ToString()}...");
        if (parsed_Fails.Contains(attribute))
            return false;
        if (call_depth > 10)
            throw new InvalidOperationException($"Reached max call depth of '{call_depth.ToString()}" +
                $"' while parsing attribute '{GetAttributeName(attribute)}'.");
        call_depth++;
        (string chosenTag, string parse_result) = GetDataPointFromTags(attribute, tags);

        #region Create a history

        // TakenDate & CreateDate have to be sent into their
        // respective tables for querying later if user chooses time-shift.
        // TODO: replace logic with AttributeVersion concept
        if (parse_result != null)
        {
            switch (attribute)
            {
                case ElementAttribute.TakenDate:
                    DataRow drTakenDate = FrmMainApp.DtOriginalTakenDate.NewRow();
                    drTakenDate[columnName: "fileNameWithoutPath"] = this.ItemName;
                    drTakenDate[columnName: "settingId"] = "originalTakenDate";
                    drTakenDate[columnName: "settingValue"] =
                        DateTime.Parse(s: parse_result, provider: CultureInfo.CurrentUICulture)
                        .ToString(provider: CultureInfo.CurrentUICulture);
                    FrmMainApp.DtOriginalTakenDate.Rows.Add(row: drTakenDate);
                    FrmMainApp.DtOriginalTakenDate.AcceptChanges();
                    break;

                case ElementAttribute.CreateDate:
                    DataRow drCreateDate = FrmMainApp.DtOriginalCreateDate.NewRow();
                    drCreateDate[columnName: "fileNameWithoutPath"] = this.ItemName;
                    drCreateDate[columnName: "settingId"] = "originalCreateDate";
                    drCreateDate[columnName: "settingValue"] =
                        DateTime.Parse(s: parse_result, provider: CultureInfo.CurrentUICulture)
                        .ToString(provider: CultureInfo.CurrentUICulture);
                    FrmMainApp.DtOriginalCreateDate.Rows.Add(row: drCreateDate);
                    FrmMainApp.DtOriginalCreateDate.AcceptChanges();
                    break;
            }
            // Not adding the xmp here because the current code logic would pull a "unified" data point.

            // Add to list of file attributes seen
            // TODO: Understand where this is used and check how the model can support this
            DataRow dr = FrmMainApp.DtFileDataSeenInThisSession.NewRow();
            dr[columnName: "fileNameWithPath"] = this.FullPathAndName;
            dr[columnName: "settingId"] = GetAttributeName(attribute);
            dr[columnName: "settingValue"] = parse_result;
            FrmMainApp.DtFileDataSeenInThisSession.Rows.Add(row: dr);
        }

        #endregion

        // If needed, transform the attribute
        IConvertible res_typed = null;
        switch(attribute)
        {
            case ElementAttribute.GPSAltitude:
                res_typed = TagsToModelValueTransformations.T2M_GPSAltitude(parse_result);
                break;

            case ElementAttribute.GPSAltitudeRef:
                res_typed = TagsToModelValueTransformations.T2M_AltitudeRef(parse_result);
                break;

            case ElementAttribute.GPSLatitude:
            case ElementAttribute.GPSDestLatitude:
            case ElementAttribute.GPSLongitude:
            case ElementAttribute.GPSDestLongitude:
                res_typed = TagsToModelValueTransformations.T2M_GPSLatLong(attribute, parse_result, parsed_Values,
                    delegate (ElementAttribute atrb) {
                        return ParseAttribute(atrb, parsed_Values, parsed_Fails, tags, call_depth);
                    });
                break;

            case ElementAttribute.Fnumber:
            case ElementAttribute.FocalLength:
            case ElementAttribute.FocalLengthIn35mmFormat:
            case ElementAttribute.ISO:
                res_typed = TagsToModelValueTransformations.T2M_F_FocalLength_ISO(attribute, parse_result);
                break;

            default:
                // Just take the string
                res_typed = parse_result;
                break;
        }


        // Add it to the lists
        if (res_typed == null)
        {
            parsed_Fails.Add(attribute);
            return false;
        }

        parsed_Values[attribute] = res_typed;
        return true;
    }



    public void ParseAttributesFromExifToolOutput(IDictionary<string, string> tags_in)
    {
        Logger.Trace($"Parse dict for item '{ItemName}'...");
        IEnumerable<ElementAttribute> possibleAttributes = (IEnumerable<ElementAttribute>)Enum.GetValues(typeof(ElementAttribute));

        // Create an all-upper version
        IDictionary<string, string> tags = new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> kvp in tags_in)
            tags[kvp.Key.ToUpper()] = kvp.Value;

        // First, clear previous values
        _Attributes.Clear();

        // Temporary store for parsed values.
        // Allows recursive parsing if one attribute requires another one
        // to be already parsed without publishing the results already.
        IDictionary<ElementAttribute, IConvertible> parsed_Values = new Dictionary<ElementAttribute, IConvertible>();
        // List of missing attributes (must be separate, as IConvertible
        // is not nullable
        List<ElementAttribute> parsed_Fails = new List<ElementAttribute>();

        // Parse values into temp. store
        foreach (ElementAttribute attribute in possibleAttributes)
        {
            // If it was already parsed due to another attrib needing it, do not parse again
            if (!parsed_Values.ContainsKey(attribute))
                ParseAttribute(attribute, parsed_Values, parsed_Fails, tags, 0);
        }

        // Add all parsed attributes to the published list
        foreach (ElementAttribute attribute in parsed_Values.Keys)
            SetAttributeValue(attribute, parsed_Values[attribute], AttributeVersion.Original);
        Logger.Trace($"Parse dict for item '{ItemName}' - OK");
    }
}