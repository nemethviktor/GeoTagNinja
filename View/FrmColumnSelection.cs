using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja
{
    /// <summary>
    /// Form supporting the selection of columns to show hide in a ListView.
    /// </summary>
    public partial class FrmColumnSelection : Form
    {
        // By Ref!!
        private ListView.ColumnHeaderCollection _colList = null;

        /// <summary>
        /// Creates the form
        /// </summary>
        /// <param name="ColList">The ColumnHeaderCollection of the ListView</param>
        public FrmColumnSelection(ListView.ColumnHeaderCollection ColList, string AppLanguage)
        {
            this._colList = ColList;
            InitializeComponent();

            btn_Cancel.Text = HelperStatic.DataReadSQLiteObjectText( languageName: AppLanguage,
                    objectType: "Button", objectName: "btn_Cancel");
            btn_Ok.Text = HelperStatic.DataReadSQLiteObjectText(languageName: AppLanguage,
                    objectType: "Button", objectName: "btn_OK");
            lbl_SelectColsTitle.Text = HelperStatic.DataReadSQLiteObjectText(languageName: AppLanguage,
                    objectType: "Label", objectName: "lbl_SelectColsTitle");
            cb_DeSelectAll.Text = HelperStatic.DataReadSQLiteObjectText(languageName: AppLanguage,
                    objectType: "CheckBox", objectName: "cb_DeSelectAll");

            foreach (ColumnHeader col in _colList)
            {
                clb_ColList.Items.Add(col.Text, (col.Width > 0));
            }
            clb_ColList.Focus();
        }

        /// <summary>
        /// Executes the closing of the form, incl. taking over selection
        /// into the actual list view columns.
        /// </summary>
        /// <param name="SaveChanges">Whether the changes should be carried over to the ListView</param>
        private void PerformClosing(bool SaveChanges)
        {
            if (SaveChanges)
            {
                // Transfer changes to columns
                foreach (ColumnHeader col in _colList)
                {
                    if (clb_ColList.CheckedItems.Contains(col.Text))
                    {
                        if (col.Width == 0)
                        {
                            col.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                        }
                    }
                    else
                    {
                        col.Width = 0;
                    }
                }
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.DialogResult = DialogResult.Cancel;
            }
            this.Close();
        }

        /// <summary>
        /// Handles click on the check box to de/select all items by setting
        /// all items to the current state of the check box.
        /// </summary>
        private void cb_DeSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            bool state = cb_DeSelectAll.Checked;
            for (int i = 0; i < this.clb_ColList.Items.Count; i++)
            {
                this.clb_ColList.SetItemChecked(i, state);
            }
        }

        private void btn_Ok_Click(object sender, EventArgs e)
        {
            PerformClosing(true);
        }

        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            PerformClosing(false);
        }
    }
}
