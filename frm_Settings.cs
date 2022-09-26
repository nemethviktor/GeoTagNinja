using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Data;

namespace GeoTagNinja
{
    public partial class frm_Settings : Form
    {
        private bool nowLoadingSettingsData;
        /// <summary>
        /// This Form provides an interface for the user to edit various app and file-specific settings.
        /// </summary>
        public frm_Settings()
        {
            InitializeComponent();

            // this one is largely resonsible for disabling the detection of "new" (changed) data. (ie when going from "noting" to "something")
            nowLoadingSettingsData = true;
            Helper_NonStatic Helper_nonstatic = new Helper_NonStatic();

            // Gets the various controls' labels and values (eg "latitude" and "51.002")
            IEnumerable<Control> c = Helper_nonstatic.GetAllControls(this);
            foreach (Control cItem in c)
            {
                if (cItem.GetType() == typeof(Label) || cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(Button) || cItem.GetType() == typeof(CheckBox) || cItem.GetType() == typeof(TabPage) || cItem.GetType() == typeof(RichTextBox))
                {
                    // for some reason there is no .Last() being offered here
                    cItem.Text = Helper.DataReadSQLiteObjectText(
                        languageName: frm_MainApp.appLanguage,
                        objectType: (cItem.GetType().ToString().Split('.')[cItem.GetType().ToString().Split('.').Length - 1]),
                        objectName: cItem.Name
                        );
                }
                else if (cItem.GetType() == typeof(TextBox) || cItem.GetType() == typeof(ComboBox))
                {
                    if (cItem.Name != "cbx_Language")
                    {
                        cItem.Text = Helper.DataReadSQLiteSettings(
                            tableName: "settings",
                            settingTabPage: cItem.Parent.Name,
                            settingId: cItem.Name
                            );
                    }
                    else
                    {
                        string resourcesFolderPath = frm_MainApp.resourcesFolderPath;
                        string languagesFolderPath = Path.Combine(frm_MainApp.resourcesFolderPath, "Languages");

                        string[] files = System.IO.Directory.GetFiles(languagesFolderPath, "*.sqlite");
                        foreach (string file in files)
                        {
                            // this should only come up in debugging not prod but if the database is open in sqliteexpert this pulls in a blank .sqlite file
                            string fileName = file.Replace(languagesFolderPath, "").Replace(".sqlite", "").Substring(1);
                            if (fileName.Length > 1)
                            {
                                this.cbx_Language.Items.Add(file.Replace(languagesFolderPath, "").Replace(".sqlite", "").Substring(1));
                                this.cbx_Language.SelectedItem = Helper.DataReadSQLiteSettings(
                                    tableName: "settings",
                                    settingTabPage: cItem.Parent.Name,
                                    settingId: cItem.Name
                                    );
                            }
                        }
                    }
                }
            }
            nowLoadingSettingsData = false;
        }
        /// <summary>
        /// Handles loading the basic values into Settings when the user opens the Form
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void Frm_Settings_Load(object sender, EventArgs e)
        {
            // set basics
            CancelButton = btn_Cancel;
            AcceptButton = btn_OK;

            // this one is largely resonsible for disabling the detection of "new" (changed) data. (ie when going from "noting" to "something
            nowLoadingSettingsData = true;

            // load file extensions
            this.lbx_fileExtensions.Items.Clear();
            foreach (string ext in frm_MainApp.allExtensions)
            {
                lbx_fileExtensions.Items.Add(ext);
            }

            // get values by name
            foreach (Control ctrl in this.tct_Settings.Controls)
            {
                if (ctrl is TabPage)
                {
                    foreach (Control subctrl in ctrl.Controls)
                    {
                        if (subctrl is TextBox box)
                        {
                            try
                            {
                                box.Text = Helper.DataReadSQLiteSettings(
                                    tableName: "settings",
                                    settingTabPage: ctrl.Name,
                                    settingId: subctrl.Name
                                    );

                            }
                            catch (InvalidOperationException) // nonesuch
                            {
                                box.Text = "";
                            }
                        }
                        else if (subctrl is CheckBox cbx)
                        {
                            try
                            {
                                string cbxTempValue;
                                cbxTempValue = Helper.DataReadSQLiteSettings(
                                    tableName: "settings",
                                    settingTabPage: ctrl.Name,
                                    settingId: subctrl.Name
                                    );
                                if(cbxTempValue == "true")
                                {
                                    cbx.CheckState = CheckState.Checked;
                                }
                                else
                                {
                                    cbx.CheckState = CheckState.Unchecked;
                                }

                            }
                            catch (InvalidOperationException) // nonesuch
                            {
                                cbx.CheckState = CheckState.Unchecked;
                            }
                        }
                    }
                }
            }
            nowLoadingSettingsData = false;
        }
        /// <summary>
        /// Handles the event where user clicks Cancel. Clears pre-Q and hides form.
        /// ...Should be updated to use DTs rather than actual tables but it's low-pri.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void Btn_Cancel_Click(object sender, EventArgs e)
        {
            Helper.DataDeleteSQLitesettingsToWritePreQueue();
            this.Hide();
        }
        /// <summary>
        /// Handles the event where user clicks OK. Writes settings to SQLite and refreshes data where necessary.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void Btn_OK_Click(object sender, EventArgs e)
        {
            // push data back into sqlite
            Helper.DataTransferSQLiteSettings();
            Helper.DataDeleteSQLitesettingsToWritePreQueue();

            // refresh user data
            string tmpSettingVal;
            Helper.s_ArcGIS_APIKey = Helper.DataSelectTbxARCGIS_APIKey_FromSQLite();
            Helper.s_GeoNames_UserName = Helper.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_GeoNames_UserName"
                );
            Helper.s_GeoNames_Pwd = Helper.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_GeoNames_Pwd"
                );

            tmpSettingVal = Helper.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "ckb_ResetMapToZero"
                );
            if (tmpSettingVal == "true")
            {
                Helper.s_ResetMapToZero = true;
            }
            else
            {
                Helper.s_ResetMapToZero = false;
            }

            this.Hide();
        }
        /// <summary>
        /// Handles the event when user clicks to browse for a default startup folder.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void Pbx_Browse_Startup_Folder_Click(object sender, EventArgs e)
        {
            if (fbd_StartupFolder.ShowDialog() == DialogResult.OK)
            {
                tbx_Startup_Folder.Text = fbd_StartupFolder.SelectedPath;
            }
        }
        /// <summary>
        /// This handles the listbox in the file-specific tab. Basically for each file type we have different settings
        /// ...such as for JPGs (probably) don't create a sidecar XMP but for RAW files (probably) do.
        /// ...This pairs up the file extensions with their setting values.
        /// </summary>
        /// <param name="sender">SettingTabPage name</param>
        /// <param name="e">Unused</param>
        private void Lbx_fileExtensions_SelectedIndexChanged(object sender, EventArgs e)
        {
            nowLoadingSettingsData = true;
            // try to load subcontrol values here
            foreach (Control subctrl in this.tpg_FileOptions.Controls)
            {
                if (subctrl is CheckBox box)
                {
                    var lbi = lbx_fileExtensions.SelectedItem;
                    string tmpCtrlName = lbi.ToString().Split('\t').First() + '_' + subctrl.Name;

                    CheckBox txt = box;
                    txt.Font = new Font(txt.Font, FontStyle.Regular);
                    // see if it's in the settings-change-queue
                    string tmpCtrlVal;
                    tmpCtrlVal = Helper.DataReadSQLiteSettings(
                        tableName: "settingsToWritePreQueue",
                        settingTabPage: ((Control)sender).Parent.Name,
                        settingId: tmpCtrlName
                        );

                    if (tmpCtrlVal != null)
                    {
                        box.Checked = bool.Parse(tmpCtrlVal);
                        txt.Font = new Font(txt.Font, FontStyle.Bold);
                    }
                    else // nonesuch
                    {
                        // if not....
                        try
                        {
                            tmpCtrlVal = Helper.DataReadSQLiteSettings(
                                    tableName: "settings",
                                    settingTabPage: ((Control)sender).Parent.Name,
                                    settingId: tmpCtrlName
                                    );

                            box.Checked = bool.Parse(tmpCtrlVal);
                        }
                        catch (InvalidOperationException) // nonesuch
                        {
                            if (box.Name == "ckb_OverwriteOriginal")
                            {
                                box.Checked = true;
                            }
                            else if (box.Name == "ckb_AddXMPSideCar")
                            {
                                string tmptmpCtrlName = lbi.ToString().Split('\t').First() + '_'; // 'tis ok as is
                                string tmpCtrlGroup = (lbi.ToString().Split('\t').Last()).ToLower();

                                if (tmpCtrlGroup.Contains("raw") || tmpCtrlGroup.Contains("tiff") || tmpCtrlGroup.Contains("dng"))
                                {
                                    box.Checked = true;
                                }
                                else
                                {
                                    box.Checked = false;
                                }
                            }
                            else // shouldn't be any at this point
                            {
                                box.Checked = false;
                            }
                            tmpCtrlVal = (box.Checked).ToString().ToLower();
                            // stick it into settings
                            Helper.DataWriteSQLiteSettings(
                                tableName: "settings",
                                settingTabPage: ((Control)sender).Parent.Name,
                                settingId: tmpCtrlName,
                                settingValue: tmpCtrlVal
                                );
                        }
                    }
                }
            }
            nowLoadingSettingsData = false;
        }
        /// <summary>
        /// Sets the default selection to the first item in fileOptions
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void Pg_fileoptions_Enter(object sender, EventArgs e)
        {
            // at this point lbx_fileExtensions.SelectedIndex == 0;
            if (lbx_fileExtensions.SelectedItems.Count == 0)
            {
                lbx_fileExtensions.SelectedIndex = 0;
            }
        }
        /// <summary>
        /// Handles the event when the "settings" tab gets activated (technically this fires before that)
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void Tab_Settings_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // this is for the "new" selected page
            nowLoadingSettingsData = true;
        }
        /// <summary>
        /// Handles the event when the "settings" tab gets deactivated (technically this fires before that)
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void Tab_Settings_Deselecting(object sender, TabControlCancelEventArgs e)
        {
            nowLoadingSettingsData = false;
        }
        /// <summary>
        /// Handles the event where any checkbox's true/false value changes. If changed makes them bold and queues up for saving.
        /// </summary>
        /// <param name="sender">The Control in question</param>
        /// <param name="e">Unused</param>
        private void Any_cbx_CheckStateChanged(object sender, EventArgs e)
        {
            if (!nowLoadingSettingsData)
            {
                CheckBox txt = (CheckBox)sender;
                txt.Font = new Font(txt.Font, FontStyle.Bold);
                string tmpCtrlName = "";
                var lbi = lbx_fileExtensions.SelectedItem;
                if (lbi != null)
                {
                    tmpCtrlName = lbi.ToString().Split('\t').First() + '_' + ((CheckBox)sender).Name.ToString();
                }
                else
                {
                    tmpCtrlName = ((CheckBox)sender).Name.ToString();
                }

                // stick it into settings-Q
                Helper.DataWriteSQLiteSettings(
                    tableName: "settingsToWritePreQueue",
                    settingTabPage: ((Control)sender).Parent.Name,
                    settingId: tmpCtrlName,
                    settingValue: ((CheckBox)sender).Checked.ToString().ToLower()
                    );
            }
        }
        /// <summary>
        /// Handles the event where any textbox's value changes. If changed makes them bold and queues up for saving.
        /// </summary>
        /// <param name="sender">The Control in question</param>
        /// <param name="e">Unused</param>
        private void Any_tbx_TextChanged(object sender, EventArgs e)
        {
            if (!nowLoadingSettingsData)
            {
                TextBox txt = (TextBox)sender;
                txt.Font = new Font(txt.Font, FontStyle.Bold);

                // stick it into settings-Q
                Helper.DataWriteSQLiteSettings(
                    tableName: "settingsToWritePreQueue",
                    settingTabPage: ((Control)sender).Parent.Name,
                    settingId: ((TextBox)sender).Name,
                    settingValue: ((TextBox)sender).Text
                    );
            }
        }

    }
}
