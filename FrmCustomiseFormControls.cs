using GeoTagNinja.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GeoTagNinja
{
    public partial class FrmCustomiseFormControls : Form
    {
        public FrmCustomiseFormControls()
        {
            InitializeComponent();
            HelperControlThemeManager.SetThemeColour(
            themeColour: HelperVariables.UserSettingUseDarkMode
                ? ThemeColour.Dark
                : ThemeColour.Light, parentControl: this);
        }

        public void SetCustomiseControl(Control control)
        {
            AddItem(control);
        }

        public void SetCustomiseControls(System.Collections.IList controls)
        {
            foreach (Control control in controls)
            {
                AddItem(control);
                SetCustomiseControls(control.Controls);
            }
        }

        public void AddItem(Control control)
        {
            _ = cbx_ControlItem.Items.Add(control);
        }

        private void cbx_ControlItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            pgr_Main.SelectedObject = cbx_ControlItem.SelectedItem;
            lbl_ControlName.Text = ((Control)cbx_ControlItem.SelectedItem).Name;
        }

        private void cbx_ControlItem_DropDown(object sender, EventArgs e)
        {
            cbx_ControlItem.FlatStyle = FlatStyle.Popup;

            cbx_ControlItem.ForeColor = Color.White;
        }

        private void FrmCustomiseFormControls_Load(object sender, EventArgs e)
        {
            HelperNonStatic helperNonstatic = new();
            HelperControlAndMessageBoxHandling.ReturnControlText(
                cItem: this, senderForm: this);

            IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
            foreach (Control cItem in c)
            {
                if (cItem is Label or
                    GroupBox or
                    Button or
                    CheckBox or
                    TabPage or
                    RadioButton
                   )

                {
                    HelperControlAndMessageBoxHandling.ReturnControlText(
                        cItem: cItem, senderForm: this);
                }
            }
        }
    }
}
