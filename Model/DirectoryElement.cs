using System.Collections.Generic;
using System.IO;

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


    #region private variables

    private string _DisplayName;

    /// <summary>
    /// Dictionary of Dictionary: Source --> (PropertyKey --> PropertyValue)
    /// </summary>
    private IDictionary<SourcesAndAttributes.Sources, IDictionary<string, string>> _Attributes;

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
        
        _Attributes = new Dictionary<SourcesAndAttributes.Sources, IDictionary<string, string>>();
    }

    public override string ToString()
    {
        return ItemName;
    }


    private string GetConsolidatedPropertyValue(string propertyName)
    {
        return "";
    }


    public string GetPropertyValue(string propertyName,
        SourcesAndAttributes.Sources? source = null, bool notFoundAsNull = false)
    {
        // Case no source given
        if (source == null) return GetConsolidatedPropertyValue(propertyName);

        // Source is given
        SourcesAndAttributes.Sources source_nn = (SourcesAndAttributes.Sources)source;
        if (_Attributes.ContainsKey(source_nn))
        {
            IDictionary<string, string> sp = _Attributes[source_nn];
            if (sp.ContainsKey(propertyName)) return sp[propertyName];
        }

        // Handle "not found" case:
        if (notFoundAsNull) return null;
        else throw new KeyNotFoundException("The property '" + propertyName + "' is not set for Item '" + ItemName + "' and source '" + source_nn + "'");
    }


    public void SetPropertyValue(SourcesAndAttributes.Sources source, string propertyName, string propertyValue)
    {
        IDictionary<string, string>  sp = _Attributes[source];
        sp[propertyName] = propertyValue;
    }


    public void ReplaceAttributesList(SourcesAndAttributes.Sources source, IDictionary<string, string> newCollection)
    {
        _Attributes[source] = newCollection;
    }
}