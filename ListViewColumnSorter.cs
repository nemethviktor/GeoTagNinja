using System;
using System.Collections;
using System.Windows.Forms;

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
        int result = 0;
        ListViewItem lvi_x = (ListViewItem)x;
        ListViewItem lvi_y = (ListViewItem)y;

        try
        {
            result = Comparer.Compare(a: lvi_x.SubItems[index: ColumnToSort]
                                          .Text, b: lvi_y.SubItems[index: ColumnToSort]
                                          .Text);
        }
        catch
        {
            result = 0; // bit redundant but for good measure.
        }

        // Inverse if descending
        if (ColumnSortOrder == SortOrder.Ascending)
        {
            return result;
        }

        return -result;
    }
}