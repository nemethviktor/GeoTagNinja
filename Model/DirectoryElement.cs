using System.IO;

namespace GeoTagNinja.Model;

public class DirectoryElement
{
    public enum ElementType
    {
        Drive = 0,
        SubDirectory = 1,
        ParentDirectory = 2,
        File = 3,
        MyComputer = 4,
        Unknown = 99
    }

    private string _DisplayName;

    public DirectoryElement(string itemName,
                            ElementType type,
                            string fullPathAndName)
    {
        ItemName = itemName;
        Type = type;
        FullPathAndName = fullPathAndName;
        Extension = Path.GetExtension(path: FullPathAndName);
        ;
    }

    public ElementType Type { get; }
    public string FullPathAndName { get; }
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

    public string Extension { get; }

    public override string ToString()
    {
        return ItemName;
    }
}