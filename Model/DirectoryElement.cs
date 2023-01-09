using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
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

    public class AttributeValues : Dictionary<AttributeVersion, string>
    {
        public AttributeValues(string initialValue = null, AttributeVersion initialVersion = AttributeVersion.Original)
        {
            if (initialValue != null) this[initialVersion] = initialValue;
        }
    }


    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #region private variables

    private string _DisplayName;

    private IDictionary<ElementAttribute, AttributeValues> _Attributes;

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
        
        _Attributes = new Dictionary<ElementAttribute, AttributeValues>();
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
    public string GetAttributeValue(ElementAttribute attribute,
        AttributeVersion? version = null,
        string notFoundValue = null)
    {
        if (_Attributes.ContainsKey(attribute))
        {
            AttributeValues av = _Attributes[attribute];
            if ((version == null | version == AttributeVersion.Modified) & (av.ContainsKey(AttributeVersion.Modified)))
                    return av[AttributeVersion.Modified];
            if ((version == null | version == AttributeVersion.Original) & (av.ContainsKey(AttributeVersion.Original)))
                    return av[AttributeVersion.Original];
        }

        return notFoundValue;
    }

    /// <summary>
    /// Set the value for the given attribute.
    /// </summary>
    /// <param name="attribute">The attribute to set the value for</param>
    /// <param name="value">The value to set (as string)</param>
    /// <param name="version">The version to set it with (opt., defaults to Modified)</param>
    public void SetAttributeValue(ElementAttribute attribute, string value, AttributeVersion version = AttributeVersion.Modified)
    {
        if (!_Attributes.ContainsKey(attribute))
        {
            _Attributes[attribute] = new AttributeValues(value, version);
        }
        else
        {
            _Attributes[attribute][version] = value;
        }
    }


    private string GetStandardisedDataPoint(ElementAttribute attribute, IDictionary<string, string> tags)
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
            if (tags.ContainsKey(orderedTags[i]))
            {
                Logger.Trace($"Parse dict for attribute: '{GetAttributeName(attribute)}" +
                    $"' yielded value '{tags[orderedTags[i]]}'");
                return tags[orderedTags[i]];
            }
        }

        Logger.Trace($"Parse dict for attribute: '{GetAttributeName(attribute)}" +
                    "' yielded no value.");
        return null;
    }


    public void ParseAttributesFromExifToolOutput(IDictionary<string, string> tags)
    {
        Logger.Trace($"Parse dict for item '{ItemName}'...");
        IEnumerable<ElementAttribute> possibleAttributes = (IEnumerable<ElementAttribute>)Enum.GetValues(typeof(ElementAttribute));

        // First, clear previous values
        _Attributes.Clear();

        foreach (ElementAttribute attribute in possibleAttributes)
        {
            // ExifGetStandardisedDataPointFromExif(dtFileExif: dtFileExifTable, dataPoint: lvchs[index: i]
            //                            .Name.Substring(startIndex: 4));
            string parse_result = GetStandardisedDataPoint(attribute, tags);

            #region Create a history

            // TakenDate & CreateDate have to be sent into their
            // respective tables for querying later if user chooses time-shift.
            // TODO: replace logic with AttributeVersion concept
            if (parse_result != null) {
                switch(attribute)
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

            // Finally add attribute (if set)
            if (parse_result != null)
                SetAttributeValue(attribute, parse_result, AttributeVersion.Original);
        }
        Logger.Trace($"Parse dict for item '{ItemName}' - OK");
    }
}