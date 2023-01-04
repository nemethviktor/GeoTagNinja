using System;
using System.Collections;
using System.Windows.Forms;
using GeoTagNinja.Model;

namespace GeoTagNinja;

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

    /// <summary>
    ///     Column to be sorted (inited to to 0, no validation on setting)
    /// </summary>
    private int ColumnToSort;

    public ListViewColumnSorter()
    {
        ColumnToSort = 0;
        ColumnSortOrder = SortOrder.Ascending;
        Comparer = new CaseInsensitiveComparer();
    }

    /// <summary>
    ///     The column (sub item index) by which to sort (default 0)
    /// </summary>
    public int SortColumn
    {
        set => ColumnToSort = value;
        get => ColumnToSort;
    }

    /// <summary>
    ///     The sort order is either SortOrder.Ascending or SortOrder.Descending (default ascending)
    /// </summary>
    public SortOrder SortOrder
    {
        set
        {
            if (value == SortOrder.Ascending || value == SortOrder.Descending)
            {
                ColumnSortOrder = value;
            }
            else
            {
                throw new ArgumentException(message: "Sort order must either by ascending or descending.");
            }
        }
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
        int lviTypeX = GetSortGroup(de: deX);
        int lviTypeY = GetSortGroup(de: deY);

        // If items not in same group, group rules
        if (lviTypeX != lviTypeY)
        {
            if (lviTypeX < lviTypeY)
            {
                return -1;
            }

            return 1;
        }

        // Item of same group - compare their texts in clicked column...
        try
        {
            string compareWhat = lviX.SubItems[index: ColumnToSort]
                .Text;
            string compareToWhat = lviY.SubItems[index: ColumnToSort]
                .Text;
            result = Comparer.Compare(a: compareWhat, b: compareToWhat);
        }
        catch
        {
            result = 0; // bit redundant but for good measure.
        }

        // Inverse if descending - doesn't affect folders.
        if (ColumnSortOrder == SortOrder.Ascending)
        {
            return result;
        }

        return -result;
    }

    private int GetSortGroup(DirectoryElement de)
    {
        switch(de.Type)
        {
            case DirectoryElement.ElementType.ParentDirectory:
                return 0;

            case DirectoryElement.ElementType.MyComputer:
                return 1;

            case DirectoryElement.ElementType.Drive:
                return 2;

            case DirectoryElement.ElementType.SubDirectory:
                return 3;

            case DirectoryElement.ElementType.Unknown:
                return 4;

            default:
                return 5;
        }
    }
}