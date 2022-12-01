using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GeoTagNinja;

public partial class FrmSettings : Form
{
    private bool _nowLoadingSettingsData;

    /// <summary>
    ///     This Form provides an interface for the user to edit various app and file-specific settings.
    /// </summary>
    public FrmSettings()
    {
        InitializeComponent();

        // this one is largely responsible for disabling the detection of "new" (changed) data. (ie when going from "noting" to "something")
        _nowLoadingSettingsData = true;
        HelperNonStatic helperNonstatic = new();
        HelperStatic.GenericReturnControlText(cItem: this, senderForm: this);

        // Gets the various controls' labels and values (eg "latitude" and "51.002")
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            HelperStatic.GenericReturnControlText(cItem: cItem, senderForm: this);

            if (cItem.Name == "cbx_Language")
            {
                string resourcesFolderPath = FrmMainApp.ResourcesFolderPath;
                string languagesFolderPath = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "Languages");

                string[] files = Directory.GetFiles(path: languagesFolderPath, searchPattern: "*.sqlite");
                foreach (string file in files)
                {
                    // this should only come up in debugging not prod but if the database is open in sqliteexpert this pulls in a blank .sqlite file
                    string fileName = file.Replace(oldValue: languagesFolderPath, newValue: "")
                        .Replace(oldValue: ".sqlite", newValue: "")
                        .Substring(startIndex: 1);
                    if (fileName.Length > 1)
                    {
                        cbx_Language.Items.Add(item: file.Replace(oldValue: languagesFolderPath, newValue: "")
                                                   .Replace(oldValue: ".sqlite", newValue: "")
                                                   .Substring(startIndex: 1));
                        cbx_Language.SelectedItem = HelperStatic.DataReadSQLiteSettings(
                            tableName: "settings",
                            settingTabPage: cItem.Parent.Name,
                            settingId: cItem.Name
                        );
                    }
                }
            }
        }

        _nowLoadingSettingsData = false;
    }

    /// <summary>
    ///     Handles loading the basic values into Settings when the user opens the Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void FrmSettings_Load(object sender,
                                  EventArgs e)
    {
        // set basics
        CancelButton = btn_Cancel;
        AcceptButton = btn_OK;

        // this one is largely responsible for disabling the detection of "new" (changed) data. (ie when going from "noting" to "something
        _nowLoadingSettingsData = true;

        // load file extensions
        lbx_fileExtensions.Items.Clear();
        foreach (string ext in AncillaryListsArrays.AllCompatibleExtensions())
        {
            lbx_fileExtensions.Items.Add(item: ext);
        }

        // get values by name
        foreach (Control ctrl in tct_Settings.Controls)
        {
            if (ctrl is TabPage)
            {
                foreach (Control subctrl in ctrl.Controls)
                {
                    if (subctrl is TextBox box)
                    {
                        try
                        {
                            box.Text = HelperStatic.DataReadSQLiteSettings(
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
                            string cbxTempValue = HelperStatic.DataReadSQLiteSettings(
                                tableName: "settings",
                                settingTabPage: ctrl.Name,
                                settingId: subctrl.Name
                            );

                            if (cbxTempValue == "true")
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

        _nowLoadingSettingsData = false;
    }

    /// <summary>
    ///     Handles the event where user clicks Cancel. Clears pre-Q and hides form.
    ///     ...Should be updated to use DTs rather than actual tables but it's low-pri.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void Btn_Cancel_Click(object sender,
                                  EventArgs e)
    {
        HelperStatic.DataDeleteSQLitesettingsToWritePreQueue();
        Hide();
    }

    /// <summary>
    ///     Handles the event where user clicks OK. Writes settings to SQLite and refreshes data where necessary.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void Btn_OK_Click(object sender,
                              EventArgs e)
    {
        // push data back into sqlite
        HelperStatic.DataTransferSQLiteSettings();
        HelperStatic.DataDeleteSQLitesettingsToWritePreQueue();

        // refresh user data
        HelperStatic.SArcGisApiKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();
        HelperStatic.SGeoNamesUserName = HelperStatic.DataReadSQLiteSettings(
            tableName: "settings",
            settingTabPage: "tpg_Application",
            settingId: "tbx_GeoNames_UserName"
        );
        HelperStatic.SGeoNamesPwd = HelperStatic.DataReadSQLiteSettings(
            tableName: "settings",
            settingTabPage: "tpg_Application",
            settingId: "tbx_GeoNames_Pwd"
        );

        string tmpSettingVal = HelperStatic.DataReadSQLiteSettings(
            tableName: "settings",
            settingTabPage: "tpg_Application",
            settingId: "ckb_ResetMapToZero"
        );
        if (tmpSettingVal == "true")
        {
            HelperStatic.SResetMapToZero = true;
        }
        else
        {
            HelperStatic.SResetMapToZero = false;
        }

        Hide();
    }

    /// <summary>
    ///     Handles the event when user clicks to browse for a default startup folder.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void Pbx_Browse_Startup_Folder_Click(object sender,
                                                 EventArgs e)
    {
        if (fbd_StartupFolder.ShowDialog() == DialogResult.OK)
        {
            tbx_Startup_Folder.Text = fbd_StartupFolder.SelectedPath;
        }
    }

    /// <summary>
    ///     This handles the listbox in the file-specific tab. Basically for each file type we have different settings
    ///     ...such as for JPGs (probably) don't create a sidecar XMP but for RAW files (probably) do.
    ///     ...This pairs up the file extensions with their setting values.
    /// </summary>
    /// <param name="sender">SettingTabPage name</param>
    /// <param name="e">Unused</param>
    private void Lbx_fileExtensions_SelectedIndexChanged(object sender,
                                                         EventArgs e)
    {
        _nowLoadingSettingsData = true;
        // try to load subcontrol values here
        foreach (Control subctrl in tpg_FileOptions.Controls)
        {
            if (subctrl is CheckBox box)
            {
                object lbi = lbx_fileExtensions.SelectedItem;
                string tmpCtrlName = lbi.ToString()
                                         .Split('\t')
                                         .First() +
                                     '_' +
                                     subctrl.Name;

                CheckBox txt = box;
                txt.Font = new Font(prototype: txt.Font, newStyle: FontStyle.Regular);
                // see if it's in the settings-change-queue
                string tmpCtrlVal = HelperStatic.DataReadSQLiteSettings(
                    tableName: "settingsToWritePreQueue",
                    settingTabPage: ((Control)sender).Parent.Name,
                    settingId: tmpCtrlName
                );

                if (tmpCtrlVal != null)
                {
                    box.Checked = bool.Parse(value: tmpCtrlVal);
                    txt.Font = new Font(prototype: txt.Font, newStyle: FontStyle.Bold);
                }
                else // nonesuch
                {
                    // if not....
                    try
                    {
                        tmpCtrlVal = HelperStatic.DataReadSQLiteSettings(
                            tableName: "settings",
                            settingTabPage: ((Control)sender).Parent.Name,
                            settingId: tmpCtrlName
                        );

                        box.Checked = bool.Parse(value: tmpCtrlVal);
                    }
                    catch (InvalidOperationException) // nonesuch
                    {
                        if (box.Name == "ckb_OverwriteOriginal")
                        {
                            box.Checked = true;
                        }
                        else if (box.Name == "ckb_AddXMPSideCar")
                        {
                            string tmptmpCtrlName = lbi.ToString()
                                                        .Split('\t')
                                                        .First() +
                                                    '_'; // 'tis ok as is
                            string tmpCtrlGroup = lbi.ToString()
                                .Split('\t')
                                .Last()
                                .ToLower();

                            if (tmpCtrlGroup.Contains(value: "raw") || tmpCtrlGroup.Contains(value: "tiff") || tmpCtrlGroup.Contains(value: "dng"))
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

                        tmpCtrlVal = box.Checked.ToString()
                            .ToLower();
                        // stick it into settings
                        HelperStatic.DataWriteSQLiteSettings(
                            tableName: "settings",
                            settingTabPage: ((Control)sender).Parent.Name,
                            settingId: tmpCtrlName,
                            settingValue: tmpCtrlVal
                        );
                    }
                }

                if (tmpCtrlName.Contains(value: "ckb_ProcessOriginalFile"))
                {
                    if (box.Checked)
                    {
                        ckb_ResetFileDateToCreated.Enabled = true;
                    }
                    else
                    {
                        ckb_ResetFileDateToCreated.Enabled = false;
                    }
                }
            }
        }

        _nowLoadingSettingsData = false;
    }

    /// <summary>
    ///     Sets the default selection to the first item in fileOptions
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void Pg_fileoptions_Enter(object sender,
                                      EventArgs e)
    {
        // at this point lbx_fileExtensions.SelectedIndex == 0;
        if (lbx_fileExtensions.SelectedItems.Count == 0)
        {
            lbx_fileExtensions.SelectedIndex = 0;
        }
    }

    /// <summary>
    ///     Handles the event when the "settings" tab gets activated (technically this fires before that)
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void Tab_Settings_Selecting(object sender,
                                        TabControlCancelEventArgs e)
    {
        // this is for the "new" selected page
        _nowLoadingSettingsData = true;
    }

    /// <summary>
    ///     Handles the event when the "settings" tab gets deactivated (technically this fires before that)
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void Tab_Settings_Deselecting(object sender,
                                          TabControlCancelEventArgs e)
    {
        _nowLoadingSettingsData = false;
    }

    /// <summary>
    ///     Handles the event where any checkbox's true/false value changes. If changed makes them bold and queues up for
    ///     saving.
    /// </summary>
    /// <param name="sender">The Control in question</param>
    /// <param name="e">Unused</param>
    private void Any_ckb_CheckStateChanged(object sender,
                                           EventArgs e)
    {
        if (!_nowLoadingSettingsData)
        {
            CheckBox txt = (CheckBox)sender;
            txt.Font = new Font(prototype: txt.Font, newStyle: FontStyle.Bold);
            string tmpCtrlName = "";
            object lbi = lbx_fileExtensions.SelectedItem;
            if (lbi != null)
            {
                tmpCtrlName = lbi.ToString()
                                  .Split('\t')
                                  .First() +
                              '_' +
                              ((CheckBox)sender).Name;
            }
            else
            {
                tmpCtrlName = ((CheckBox)sender).Name;
            }

            // stick it into settings-Q
            HelperStatic.DataWriteSQLiteSettings(
                tableName: "settingsToWritePreQueue",
                settingTabPage: ((Control)sender).Parent.Name,
                settingId: tmpCtrlName,
                settingValue: ((CheckBox)sender).Checked.ToString()
                .ToLower()
            );
        }
    }

    /// <summary>
    ///     Handles the event where any textbox's value changes. If changed makes them bold and queues up for saving.
    /// </summary>
    /// <param name="sender">The Control in question</param>
    /// <param name="e">Unused</param>
    private void Any_tbx_TextChanged(object sender,
                                     EventArgs e)
    {
        if (!_nowLoadingSettingsData)
        {
            TextBox txt = (TextBox)sender;
            txt.Font = new Font(prototype: txt.Font, newStyle: FontStyle.Bold);

            // stick it into settings-Q
            HelperStatic.DataWriteSQLiteSettings(
                tableName: "settingsToWritePreQueue",
                settingTabPage: ((Control)sender).Parent.Name,
                settingId: ((TextBox)sender).Name,
                settingValue: ((TextBox)sender).Text
            );
        }
    }

    /// <summary>
    ///     Handles the event where any combobox's dropdown/text has changed.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void Any_cbx_TextChanged(object sender,
                                     EventArgs e)
    {
        if (!_nowLoadingSettingsData)
        {
            ComboBox cbx = (ComboBox)sender;
            cbx.Font = new Font(prototype: cbx.Font, newStyle: FontStyle.Bold);

            // stick it into settings-Q
            HelperStatic.DataWriteSQLiteSettings(
                tableName: "settingsToWritePreQueue",
                settingTabPage: ((Control)sender).Parent.Name,
                settingId: ((ComboBox)sender).Name,
                settingValue: ((ComboBox)sender).Text
            );
            // fire a warning if language has changed. 
            if (((ComboBox)sender).Name == "cbx_Language")
            {
                MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmSettings_cbx_Language_TextChanged"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
            }
        }
    }
}