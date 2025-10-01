using GeoTagNinja.Model;
using System;
using System.Collections;
using System.Windows.Forms;

namespace GeoTagNinja.View.ListView;

/// <summary>
///     Comparer for columns. Currently supports only case insensitive string comparison.
/// </summary>
internal class ListViewColumnSorter : IComparer
{
    /// <summary>
    ///     comparer object for re use
    /// </summary>
    private readonly CaseInsensitiveComparer Comparer;

    /// <summary>
    ///     Sort order (limited to ascending and descending, init to asc)
    /// </summary>
    private SortOrder ColumnSortOrder;

    public ListViewColumnSorter()
    {
        SortColumn = 0;
        ColumnSortOrder = SortOrder.Ascending;
        Comparer = new CaseInsensitiveComparer();
    }

    /// <summary>
    ///     The column (sub item index) by which to sort (default 0)
    /// </summary>
    public int SortColumn { set; get; }

    /// <summary>
    ///     The sort order is either SortOrder.Ascending or SortOrder.Descending (default ascending)
    /// </summary>
    public SortOrder SortOrder
    { set => ColumnSortOrder = value is SortOrder.Ascending or SortOrder.Descending
              ? value
              : throw new ArgumentException(message: "Sort order must either by ascending or descending.");
        get => ColumnSortOrder;
    }


    /// <summary>
    ///     Compare two objects of type ListViewItem by looking at the set SortColumn.
    ///     If descending sort order is set, the inverse result is returned.
    ///     Inherited from IComparer interface.
    /// </summary>
    /// <returns>
    ///     Result of comparison: equal (0), 'x'<'y' (negative), 'x'>'y' (positive)
    /// </returns>
    public int Compare(object x,
                       object y)
    {
        // Comparison with NULL returns equal
        if (x == null && y == null)
        {
            return 0;
        }

        int result = 0;
        ListViewItem lviX = (ListViewItem)x;
        ListViewItem lviY = (ListViewItem)y;
        DirectoryElement deX = (DirectoryElement)lviX.Tag;
        DirectoryElement deY = (DirectoryElement)lviY.Tag;

        // We want to keep parent folder above all others and
        // below that all folders. This is not subject to sort order!

        // Assign type per group
        int lviElementTypeX = GetSortGroup(de: deX);
        int lviElementTypeY = GetSortGroup(de: deY);

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        System.Windows.Forms.ListView lvw_FileList = frmMainAppInstance.lvw_FileList;

        string colName = lvw_FileList.Columns[index: SortColumn]
                                     .Name.Substring(startIndex: FileListView.COL_NAME_PREFIX.Length); // no "clh_"

        // If items not in same group, group rules
        // This only applies to fileNames (as in the column FileName...Type in this sense/case is/are...Drive, Folder, File etc)
        if (lviElementTypeX != lviElementTypeY)
        {
            return lviElementTypeX < lviElementTypeY ? -1 : 1;
        }

        // Item of same group - compare their values in clicked column...
        try
        {
            // FileName is always string and has no versions
            if (colName == "FileName")
            {
                string compareWhat = lviX.SubItems[index: SortColumn]
                    .Text;
                string compareToWhat = lviY.SubItems[index: SortColumn]
                    .Text;

                result = Comparer.Compare(a: compareWhat, b: compareToWhat);
            }

            // Otherwise we need to compare appropriate item types (like INTs etc.) rather than strings (if they aren't strings)
            // ... this is to avoid having something like C# thinking the value of "2" is more than "12".
            else
            {
                SourcesAndAttributes.ElementAttribute attribute =
                    SourcesAndAttributes.GetElementAttributesElementAttribute(attributeToFind: colName);
                Type attributeType = SourcesAndAttributes.GetElementAttributesType(attributeToFind: attribute);

                // get the "highest" version available. In this case that will be either Stage3 or Orig
                DirectoryElement.AttributeVersion deXHighestAttributeVersion;
                DirectoryElement.AttributeVersion deYHighestAttributeVersion;
                deXHighestAttributeVersion = deX.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite)
                    ? DirectoryElement.AttributeVersion.Stage3ReadyToWrite
                    : DirectoryElement.AttributeVersion.Original;
                deYHighestAttributeVersion = deY.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite)
                    ? DirectoryElement.AttributeVersion.Stage3ReadyToWrite
                    : DirectoryElement.AttributeVersion.Original;

                if (attributeType == typeof(string))
                {
                    string? compareWhat = deX.GetAttributeValueString(
                        attribute: attribute, version: deXHighestAttributeVersion,
                        nowSavingExif: false);
                    string? compareToWhat = deY.GetAttributeValueString(
                        attribute: attribute, version: deYHighestAttributeVersion,
                        nowSavingExif: false);

                    result = Comparer.Compare(a: compareWhat, b: compareToWhat);
                }
                else if (attributeType == typeof(double))
                {
                    double? compareWhat = deX.GetAttributeValue<double>(
                        attribute: attribute,
                        version: deXHighestAttributeVersion);
                    double? compareToWhat = deY.GetAttributeValue<double>(
                        attribute: attribute,
                        version: deYHighestAttributeVersion);

                    result = Comparer.Compare(a: compareWhat, b: compareToWhat);
                }
                else if (attributeType == typeof(int))
                {
                    int? compareWhat = deX.GetAttributeValue<int>(
                        attribute: attribute,
                        version: deXHighestAttributeVersion);
                    int? compareToWhat = deY.GetAttributeValue<int>(
                        attribute: attribute,
                        version: deYHighestAttributeVersion);

                    result = Comparer.Compare(a: compareWhat, b: compareToWhat);
                }
                else if (attributeType == typeof(DateTime))
                {
                    DateTime? compareWhat = deX.GetAttributeValue<DateTime>(
                        attribute: attribute,
                        version: deXHighestAttributeVersion);
                    DateTime? compareToWhat = deY.GetAttributeValue<DateTime>(
                        attribute: attribute,
                        version: deYHighestAttributeVersion);

                    result = Comparer.Compare(a: compareWhat, b: compareToWhat);
                }
                else
                {
                    throw new ArgumentException(message:
                        $"Trying to get attribute type of unknown attribute with value {attribute}");
                }
            }
        }
        catch
        {
            result = 0; // bit redundant but for good measure.
        }

        // Inverse if descending - doesn't affect folders.
        return ColumnSortOrder == SortOrder.Ascending ? result : -result;
    }

    private int GetSortGroup(DirectoryElement de)
    {
        return de.Type switch
        {
            DirectoryElement.ElementType.ParentDirectory => 0,
            DirectoryElement.ElementType.MyComputer => 1,
            DirectoryElement.ElementType.Drive => 2,
            DirectoryElement.ElementType.SubDirectory => 3,
            DirectoryElement.ElementType.Unknown => 4,
            _ => 5,
        };
    }
}