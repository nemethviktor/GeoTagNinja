using System;
using System.Windows.Forms;
using GeoTagNinja.Helpers;

namespace GeoTagNinja;

/// <summary>
///     Form supporting the selection of columns to show hide in a ListView.
/// </summary>
public partial class FrmColumnSelection : Form
{
    // By Ref!!
    private readonly ListView.ColumnHeaderCollection _colList;

    /// <summary>
    ///     Creates the form
    /// </summary>
    /// <param name="ColList">The ColumnHeaderCollection of the ListView</param>
    public FrmColumnSelection(ListView.ColumnHeaderCollection ColList,
                              string AppLanguage)
    {
        _colList = ColList;
        InitializeComponent();

        btn_Cancel.Text = HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: "Button", objectName: "btn_Cancel");
        btn_Ok.Text = HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: "Button", objectName: "btn_OK");
        lbl_SelectColsTitle.Text = HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: "Label", objectName: "lbl_SelectColsTitle");
        ckb_DeSelectAll.Text = HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: "CheckBox", objectName: "ckb_DeSelectAll");

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
    /// <param name="SaveChanges">Whether the changes should be carried over to the ListView</param>
    private void PerformClosing(bool SaveChanges)
    {
        if (SaveChanges)
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

    private void btn_Ok_Click(object sender,
                              EventArgs e)
    {
        PerformClosing(SaveChanges: true);
    }

    private void btn_Cancel_Click(object sender,
                                  EventArgs e)
    {
        PerformClosing(SaveChanges: false);
    }
}