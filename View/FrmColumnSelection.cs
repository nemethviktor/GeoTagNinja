using GeoTagNinja.Helpers;
using System;
using System.Windows.Forms;

namespace GeoTagNinja.View;

/// <summary>
///     Form supporting the selection of columns to show hide in a ListView.
/// </summary>
public partial class FrmColumnSelection : Form
{
    // By Ref!!
    private readonly System.Windows.Forms.ListView.ColumnHeaderCollection _colList;

    /// <summary>
    ///     Creates the form
    /// </summary>
    /// <param name="colList">The ColumnHeaderCollection of the ListView</param>
    public FrmColumnSelection(System.Windows.Forms.ListView.ColumnHeaderCollection colList)
    {
        _colList = colList;
        InitializeComponent();

        btn_Cancel.Text = HelperControlAndMessageBoxHandling.ReturnControlText(
            controlName: "btn_Cancel",
            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.Button);
        btn_OK.Text = HelperControlAndMessageBoxHandling.ReturnControlText(
            controlName: "btn_OK", fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.Button);
        lbl_SelectColsTitle.Text = HelperControlAndMessageBoxHandling.ReturnControlText(
            controlName: "lbl_SelectColsTitle",
            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.Label);
        ckb_DeSelectAll.Text = HelperControlAndMessageBoxHandling.ReturnControlText(
            controlName: "ckb_DeSelectAll",
            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.CheckBox);

        foreach (ColumnHeader col in _colList)
        {
            clb_ColList.Items.Add(item: col.Text, isChecked: col.Width > 0);
        }

        clb_ColList.Focus();
    }

    /// <summary>
    ///     Executes the closing of the form, incl. taking over selection
    ///     into the actual list view columns.
    /// </summary>
    /// <param name="saveChanges">Whether the changes should be carried over to the ListView</param>
    private void PerformClosing(bool saveChanges)
    {
        if (saveChanges)
        {
            // Transfer changes to columns
            foreach (ColumnHeader col in _colList)
            {
                if (clb_ColList.CheckedItems.Contains(item: col.Text))
                {
                    if (col.Width == 0)
                    {
                        col.AutoResize(headerAutoResize: ColumnHeaderAutoResizeStyle.HeaderSize);
                    }
                }
                else
                {
                    col.Width = 0;
                }
            }

            DialogResult = DialogResult.OK;
        }
        else
        {
            DialogResult = DialogResult.Cancel;
        }

        Close();
    }

    /// <summary>
    ///     Handles click on the check box to de/select all items by setting
    ///     all items to the current state of the check box.
    /// </summary>
    private void ckb_DeSelectAll_CheckedChanged(object sender,
                                                EventArgs e)
    {
        bool state = ckb_DeSelectAll.Checked;
        for (int i = 0; i < clb_ColList.Items.Count; i++)
        {
            clb_ColList.SetItemChecked(index: i, value: state);
        }
    }

    private void btn_Generic_OK_Click(object sender,
                              EventArgs e)
    {
        PerformClosing(saveChanges: true);
    }

    private void btn_Generic_Cancel_Click(object sender,
                                  EventArgs e)
    {
        PerformClosing(saveChanges: false);
    }
}