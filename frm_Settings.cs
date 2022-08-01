using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Drawing;
using System.IO;

namespace GeoTagNinja
{
    public partial class frm_Settings : Form
    {
        
        private bool nowLoadingSettingsData;
        static List<AppSettings> appSettings_settings = new();
        public frm_Settings()
        {
            InitializeComponent();

            
            nowLoadingSettingsData = true;
            Helper_NonStatic Helper_nonstatic = new Helper_NonStatic(); 
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
        public class AppSettings
        {
            public string SettingTabPage { get; set; }
            public string SettingId { get; set; }
            public string SettingValue { get; set; }
        }
        private void Frm_Settings_Load(object sender, EventArgs e)
        {
            // set basics
            CancelButton = btn_Cancel;
            AcceptButton = btn_OK;
            nowLoadingSettingsData = true;

            // load file extensions
            this.lbx_fileExtensions.Items.Clear();
            foreach (string ext in frm_MainApp.allExtensions)
            {
                lbx_fileExtensions.Items.Add(ext);
            }

            appSettings_settings.Clear();
            // read data into a list from sqlite
            using (var dbcon = new SQLiteConnection("Data Source=" + Helper.s_settingsDataBasePath))
            {
                dbcon.Open();

                var command = dbcon.CreateCommand();
                command.CommandText = @"SELECT * FROM settings;";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    appSettings_settings.Add(new AppSettings()
                    {
                        SettingTabPage = reader.GetString(0),
                        SettingId = reader.GetString(1),
                        SettingValue = reader.GetString(2)
                    }); ;
                }
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
                                box.Text = appSettings_settings.First(item => item.SettingId == subctrl.Name && item.SettingTabPage == ctrl.Name).SettingValue;
                            }
                            catch (InvalidOperationException) // nonesuch
                            {
                                box.Text = "";
                            }
                        }
                        else if (subctrl is CheckBox)
                        {
                            // this never actually fires. Load only loads the first tabpage, which has none of these.
                        }
                    }
                }
            }
            nowLoadingSettingsData = false;
        }
        private void Btn_Cancel_Click(object sender, EventArgs e)
        {
            Helper.DataDeleteSQLitesettingsToWritePreQueue();
            this.Hide();
        }
        private void Btn_OK_Click(object sender, EventArgs e)
        {
            // push data back into sqlite
            Helper.DataTransferSQLiteSettings();
            Helper.DataDeleteSQLitesettingsToWritePreQueue();

            // refresh user data
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

            this.Hide();
        }
        private void Pbx_Browse_Startup_Folder_Click(object sender, EventArgs e)
        {
            if (fbd_StartupFolder.ShowDialog() == DialogResult.OK)
            {
                tbx_Startup_Folder.Text = fbd_StartupFolder.SelectedPath;
            }
        }
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
                            tmpCtrlVal = appSettings_settings.First(item => item.SettingTabPage == ((Control)sender).Parent.Name && item.SettingId == tmpCtrlName).SettingValue;
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
        private void Pg_fileoptions_Enter(object sender, EventArgs e)
        {
            // at this point lbx_fileExtensions.SelectedIndex == 0;
            if (lbx_fileExtensions.SelectedItems.Count == 0)
            {
                lbx_fileExtensions.SelectedIndex = 0;
            }
        }
        private void Tab_Settings_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // this is for the "new" selected page
            nowLoadingSettingsData = true;
        }
        private void Tab_Settings_Deselecting(object sender, TabControlCancelEventArgs e)
        {
            nowLoadingSettingsData = false;
            // this is for the "ex" selected page
            // this is obsolete but i'm leaving it in for now
            // TabPage ctrl = (sender as TabControl).SelectedTab;
        }
        private void Any_cbx_CheckStateChanged(object sender, EventArgs e)
        {
            if (!nowLoadingSettingsData)
            {
                CheckBox txt = (CheckBox)sender;
                txt.Font = new Font(txt.Font, FontStyle.Bold);

                var lbi = lbx_fileExtensions.SelectedItem;
                string tmpCtrlName = lbi.ToString().Split('\t').First() + '_' + ((CheckBox)sender).Name.ToString();

                // stick it into settings-Q
                Helper.DataWriteSQLiteSettings(
                    tableName: "settingsToWritePreQueue",
                    settingTabPage: ((Control)sender).Parent.Name,
                    settingId: tmpCtrlName,
                    settingValue: ((CheckBox)sender).Checked.ToString().ToLower()
                    );
            }
        }
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
