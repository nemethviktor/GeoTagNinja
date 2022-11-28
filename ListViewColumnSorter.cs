using System;
using System.Collections;
using System.Windows.Forms;

namespace GeoTagNinja
{
    /// <summary>
    /// Comparer for columns. Currently supports only case insensitive string comparison.
    /// </summary>
    internal class ListViewColumnSorter : IComparer
    {
        /// <summary>
        /// Column to be sorted (inited to to 0, no validation on setting)
        /// </summary>
        private int ColumnToSort;

        /// <summary>
        /// Sort order (limited to ascending and descending, init to asc)
        /// </summary>
        private SortOrder ColumnSortOrder;

        /// <summary>
        /// comparer object for re use
        /// </summary>
        private CaseInsensitiveComparer Comparer;

        public ListViewColumnSorter()
        {
            ColumnToSort = 0;
            ColumnSortOrder = SortOrder.Ascending;
            Comparer = new CaseInsensitiveComparer();
        }

        /// <summary>
        /// Compare two objects of type ListViewItem by looking at the set SortColumn.
        /// If descending sort order is set, the inverse result is returned.
        /// Inherited from IComparer interface.
        /// </summary>
        /// <returns>Result of comparison: equal (0), 'x'<'y' (negative), 'x'>'y' (positive)</returns>
        public int Compare(object x, object y)
        {
            int result;
            ListViewItem lvi_x = (ListViewItem)x;
            ListViewItem lvi_y = (ListViewItem)y;

            result = Comparer.Compare(lvi_x.SubItems[ColumnToSort].Text, lvi_y.SubItems[ColumnToSort].Text);

            // Inverse if descending
            if (ColumnSortOrder == SortOrder.Ascending) {
                return result;
            } else {
                return (-result);
            }
        }

        /// <summary>
        /// The column (sub item index) by which to sort (default 0)
        /// </summary>
        public int SortColumn {
            set {
                ColumnToSort = value;
            }
            get {
                return ColumnToSort;
            }
        }

        /// <summary>
        /// The sort order is either SortOrder.Ascending or SortOrder.Descending (default ascending)
        /// </summary>
        public SortOrder SortOrder {
            set {
                if (value == SortOrder.Ascending || value == SortOrder.Descending ) {
                    ColumnSortOrder = value;
                } else {
                    throw new ArgumentException("Sort order must either by ascending or descending.");
                }
            }
            get {
                return ColumnSortOrder;
            }
        }
    }
}
