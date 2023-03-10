using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using static System.String;

namespace GeoTagNinja;

public partial class FrmSettings : Form
{
    internal static DataTable dtCustomRules = new();
    internal static DataTable dtCustomCityLogic = new();
    private static List<Control> lstTpgApplicationControls = new();
    private readonly string languageSavedInSQL;
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
        lstTpgApplicationControls = helperNonstatic.GetAllControls(control: tpg_Application)
            .ToList();

        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        if (c != null)
        {
            foreach (Control cItem in c)
            {
                string parentNameToUse = GetParentNameToUse(cItem: cItem, cTpgApplication: lstTpgApplicationControls);

                if (cItem.Name == "cbx_Language" || cItem.Name == "cbx_TryUseGeoNamesLanguage")
                {
                    ComboBox cbx = (ComboBox)cItem;

                    string[] alreadyTranslatedLanguages = { "en", "fr" };
                    languageSavedInSQL = HelperStatic.DataReadSQLiteSettings(
                        tableName: "settings",
                        settingTabPage: parentNameToUse,
                        settingId: cbx.Name
                    );

                    List<KeyValuePair<string, string>> kvpLanguage = AncillaryListsArrays.GetISO_639_1_Languages();
                    for (int index = 0; index < kvpLanguage.Count; index++)
                    {
                        KeyValuePair<string, string> kvp = kvpLanguage[index: index];
                        string thisLanguage = kvp.Value;

                        if ((cItem.Name == "cbx_Language" && alreadyTranslatedLanguages.Contains(value: kvp.Key)) ||
                            cItem.Name == "cbx_TryUseGeoNamesLanguage")
                        {
                            cbx.Items.Add(item: thisLanguage);
                        }
                    }

                    if (languageSavedInSQL == null)
                    {
                        cbx.SelectedIndex = cbx.FindStringExact(s: HelperStatic.defaultEnglishString);
                    }
                    else
                    {
                        for (int i = 0; i < cbx.Items.Count; i++)
                        {
                            string value = cbx.GetItemText(item: cbx.Items[index: i]);
                            if (value.Contains(value: languageSavedInSQL))
                            {
                                cbx.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }

                else
                {
                    HelperStatic.GenericReturnControlText(cItem: cItem,
                                                          senderForm: this,
                                                          parentNameToUse: parentNameToUse);
                }
            }
        }

        _nowLoadingSettingsData = false;
    }

    /// <summary>
    ///     The groupboxes on tpg_Application interfere with the Parent Name logic so this corrects/fakes that.
    /// </summary>
    /// <param name="cItem">The Control to check if it sits in tpg_Application</param>
    /// <param name="cTpgApplication">List of Controls that are children of tabpage tpg_Application</param>
    /// <returns>The actual Parent Name if tpg_Application isn't a parent or string literal "tpg_Application" if it is.</returns>
    private static string GetParentNameToUse(Control cItem,
                                             List<Control> cTpgApplication)
    {
        string parentNameToUse = null;
        try
        {
            parentNameToUse = cItem.Parent.Name;
        }
        catch
        {
            // nothing
        }

        ;
        if (cTpgApplication.Contains(item: cItem))
        {
            parentNameToUse = "tpg_Application";
        }

        return parentNameToUse;
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
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        if (c != null)
        {
            foreach (Control cItem in c)
            {
                string parentNameToUse = GetParentNameToUse(cItem: cItem, cTpgApplication: lstTpgApplicationControls);

                {
                    if (cItem is TextBox tbx)
                    {
                        try
                        {
                            tbx.Text = HelperStatic.DataReadSQLiteSettings(
                                tableName: "settings",
                                settingTabPage: parentNameToUse,
                                settingId: cItem.Name
                            );
                        }
                        catch (InvalidOperationException) // nonesuch
                        {
                            tbx.Text = "";
                        }
                    }
                    else if (cItem is CheckBox ckb)
                    {
                        try
                        {
                            ckb.CheckState = HelperStatic.DataReadCheckBoxSettingTrueOrFalse(
                                tableName: "settings",
                                settingTabPage: parentNameToUse,
                                settingId: ckb.Name
                            )
                                ? CheckState.Checked
                                : CheckState.Unchecked;
                        }
                        catch (InvalidOperationException) // nonesuch
                        {
                            ckb.CheckState = CheckState.Unchecked;
                        }
                    }
                    else if (cItem is NumericUpDown nud)
                    {
                        string nudTempValue = HelperStatic.DataReadSQLiteSettings(
                            tableName: "settings",
                            settingTabPage: parentNameToUse,
                            settingId: cItem.Name
                        );

                        if (nudTempValue != null)
                        {
                            nud.Value = Convert.ToInt32(value: nudTempValue);
                            nud.Text = nudTempValue;
                        }
                        else
                        {
                            switch (nud.Name)
                            {
                                case "nud_ChoiceOfferCount":
                                    nud.Value = 1;
                                    nud.Text = "1";
                                    break;
                                case "nud_ChoiceRadius":
                                    nud.Value = 10;
                                    nud.Text = "10";
                                    break;
                                default:
                                    nud.Value = 1;
                                    break;
                            }
                        }
                    }
                    else if (cItem is RadioButton rbt)
                    {
                        try
                        {
                            rbt.Checked = HelperStatic.DataReadCheckBoxSettingTrueOrFalse(
                                tableName: "settings",
                                settingTabPage: parentNameToUse,
                                settingId: rbt.Name
                            )
                                ? true
                                : false;
                        }
                        catch (InvalidOperationException) // nonesuch
                        {
                            rbt.Checked = false;
                        }
                    }
                }
            }
        }

        // (re)set cbx_TryUseGeoNamesLanguage 
        if (rbt_UseGeoNamesLocalLanguage.Checked)
        {
            HelperStatic.APILanguageToUse = "local";
            cbx_TryUseGeoNamesLanguage.Enabled = false;
        }
        else
        {
            cbx_TryUseGeoNamesLanguage.Enabled = true;
        }

        _nowLoadingSettingsData = false;

        LoadCustomRulesDGV();
        LoadCustomCityLogicDGV();
    }

    /// <summary>
    ///     Loads up the datagridview from sqlite.
    ///     Some of the columns are dropdowns (technically DataGridViewComboBoxColumn) so they need to have a few extra
    ///     settings loaded.
    /// </summary>
    [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
    private void LoadCustomRulesDGV()
    {
        dtCustomRules = HelperStatic.DataReadSQLiteCustomRules();
        dgv_CustomRules.AutoGenerateColumns = false;

        BindingSource source = new();
        source.DataSource = dtCustomRules;
        dgv_CustomRules.DataSource = source;

        List<KeyValuePair<string, string>> clh_CountryCodeOptions = refreshClh_CountryCodeOptions(ckb_IncludePredeterminedCountries: HelperStatic.DataReadCheckBoxSettingTrueOrFalse(
                                                                                                      tableName: "settings",
                                                                                                      settingTabPage: "tpg_CustomRules",
                                                                                                      settingId: "ckb_IncludePredeterminedCountries"
                                                                                                  ));
        DataGridViewComboBoxColumn clh_CountryCode = new()
        {
            DataPropertyName = "CountryCode",
            Name = "clh_CountryCode",
            HeaderText = HelperStatic.DataReadDTObjectText(objectType: "ColumnHeader", objectName: "clh_Country"),
            DataSource = clh_CountryCodeOptions,
            ValueMember = "Key",
            DisplayMember = "Value"
        };

        DataGridViewComboBoxColumn clh_DataPointName = new()
        {
            DataPropertyName = "DataPointName",
            Name = "clh_DataPointName",
            HeaderText = HelperStatic.DataReadDTObjectText(objectType: "ColumnHeader", objectName: "clh_DataPointName")
        };

        // e.g.: "AdminName1","AdminName2"...
        foreach (string itemName in AncillaryListsArrays.CustomRulesDataSources())
        {
            clh_DataPointName.Items.Add(item: itemName);
        }

        DataGridViewComboBoxColumn clh_DataPointConditionType = new()
        {
            DataPropertyName = "DataPointConditionType",
            Name = "clh_DataPointConditionType",
            HeaderText = HelperStatic.DataReadDTObjectText(objectType: "ColumnHeader", objectName: "clh_DataPointConditionType")
        };

        // e.g.: "Is","Contains"...
        foreach (string itemName in AncillaryListsArrays.CustomRulesDataConditions())
        {
            clh_DataPointConditionType.Items.Add(item: itemName);
        }

        DataGridViewTextBoxColumn clh_DataPointConditionValue = new()
        {
            DataPropertyName = "DataPointConditionValue",
            Name = "clh_DataPointConditionValue",
            HeaderText = HelperStatic.DataReadDTObjectText(objectType: "ColumnHeader", objectName: "clh_DataPointConditionValue")
        };

        DataGridViewComboBoxColumn clh_TargetPointName = new()
        {
            DataPropertyName = "TargetPointName",
            Name = "clh_TargetPointName",
            HeaderText = HelperStatic.DataReadDTObjectText(objectType: "ColumnHeader", objectName: "clh_TargetPointName")
        };

        // e.g.:  "State","City"...
        foreach (string itemName in AncillaryListsArrays.CustomRulesDataTargets())
        {
            clh_TargetPointName.Items.Add(item: itemName);
        }

        DataGridViewComboBoxColumn clh_TargetPointOutcome = new()
        {
            DataPropertyName = "TargetPointOutcome",
            Name = "clh_TargetPointOutcome",
            HeaderText = HelperStatic.DataReadDTObjectText(objectType: "ColumnHeader", objectName: "clh_TargetPointOutcome")
        };

        // e.g.: "AdminName1","AdminName2"... +Null (empty)" + Custom
        foreach (string itemName in AncillaryListsArrays.CustomRulesDataSources(isOutcome: true))
        {
            clh_TargetPointOutcome.Items.Add(item: itemName);
        }

        DataGridViewTextBoxColumn clh_TargetPointOutcomeCustom = new()
        {
            DataPropertyName = "TargetPointOutcomeCustom",
            Name = "clh_TargetPointOutcomeCustom",
            HeaderText = HelperStatic.DataReadDTObjectText(objectType: "ColumnHeader", objectName: "clh_TargetPointOutcomeCustom")
        };

        dgv_CustomRules.Columns.AddRange(
            clh_CountryCode,
            clh_DataPointName,
            clh_DataPointConditionType,
            clh_DataPointConditionValue,
            clh_TargetPointName,
            clh_TargetPointOutcome,
            clh_TargetPointOutcomeCustom
        );
    }


    /// <summary>
    ///     TODO
    /// </summary>
    [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
    private void LoadCustomCityLogicDGV()
    {
        dtCustomCityLogic = HelperStatic.DataReadSQLiteCustomCityAllocationLogic();
        dgv_CustomCityLogic.AutoGenerateColumns = false;

        BindingSource source = new();
        source.DataSource = dtCustomCityLogic;
        dgv_CustomCityLogic.DataSource = source;

        List<KeyValuePair<string, string>> clh_CountryCodeOptions = refreshClh_CountryCodeOptions(ckb_IncludePredeterminedCountries: true);
        DataGridViewComboBoxColumn clh_CountryCode = new()
        {
            DataPropertyName = "CountryCode",
            Name = "clh_CountryCode",
            HeaderText = HelperStatic.DataReadDTObjectText(objectType: "ColumnHeader", objectName: "clh_Country"),
            DataSource = clh_CountryCodeOptions,
            ValueMember = "Key",
            DisplayMember = "Value",
            ReadOnly = true
        };

        DataGridViewComboBoxColumn clh_TargetPointNameCustomCityLogic = new()
        {
            DataPropertyName = "TargetPointNameCustomCityLogic",
            Name = "clh_TargetPointNameCustomCityLogic",
            HeaderText = HelperStatic.DataReadDTObjectText(objectType: "ColumnHeader", objectName: "clh_TargetPointNameCustomCityLogic")
        };

        // e.g.: "AdminName1","AdminName2"... 
        foreach (string itemName in AncillaryListsArrays.CustomCityLogicDataSources())
        {
            clh_TargetPointNameCustomCityLogic.Items.Add(item: itemName);
        }

        dgv_CustomCityLogic.Columns.AddRange(
            clh_CountryCode,
            clh_TargetPointNameCustomCityLogic
        );
    }

    /// <summary>
    ///     Refreshes the list that feeds the country-codes column. Separated because it would also be called when the relevant
    ///     checkbox changes.
    /// </summary>
    /// <returns>KVP list of country codes and countries</returns>
    private static List<KeyValuePair<string, string>> refreshClh_CountryCodeOptions(bool ckb_IncludePredeterminedCountries)
    {
        List<KeyValuePair<string, string>> clh_CountryCodeOptions = new();
        // e.g. "GBR//United Kingdom of...."
        foreach (DataRow dataRow in FrmMainApp.DtIsoCountryCodeMapping.Rows)
        {
            clh_CountryCodeOptions.Add(item: new KeyValuePair<string, string>(key: dataRow[columnName: "ISO_3166_1A3"]
                                                                                  .ToString()
                                                                              , value: dataRow[columnName: "Country"]
                                                                                  .ToString()));
        }

        // if _do not_ IncludeNonPredeterminedCountries then remove those
        if (!ckb_IncludePredeterminedCountries)
        {
            foreach (DataRow dataRow in FrmMainApp.DtIsoCountryCodeMapping.Rows)
            {
                string countryCode = dataRow[columnName: "ISO_3166_1A3"]
                    .ToString();
                if (!FrmMainApp.lstCityNameIsUndefined.Contains(item: countryCode))
                {
                    clh_CountryCodeOptions.Remove(item: new KeyValuePair<string, string>(key: dataRow[columnName: "ISO_3166_1A3"]
                                                                                             .ToString()
                                                                                         , value: dataRow[columnName: "Country"]
                                                                                             .ToString()));
                }
            }
        }

        return clh_CountryCodeOptions;
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
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        if (c != null)
        {
            foreach (Control cItem in c)
            {
                if (cItem.Name == "cbx_Language" || cItem.Name == "cbx_TryUseGeoNamesLanguage")
                {
                    ComboBox cbx = (ComboBox)cItem;
                    if ((cbx.Font.Style & FontStyle.Bold) != 0)
                    {
                        if (cbx.Name == "cbx_Language")
                        {
                            // fire a warning if language has changed. 
                            MessageBox.Show(
                                text: HelperStatic.GenericGetMessageBoxText(
                                    messageBoxName: "mbx_FrmSettings_cbx_Language_TextChanged"),
                                caption: HelperStatic.GenericGetMessageBoxCaption(captionType: "Warning"),
                                buttons: MessageBoxButtons.OK,
                                icon: MessageBoxIcon.Warning);
                        }
                        else if (cbx.Name == "cbx_TryUseGeoNamesLanguage")
                        {
                            IEnumerable<KeyValuePair<string, string>> result = AncillaryListsArrays.GetISO_639_1_Languages()
                                .Where(predicate: kvp => kvp.Value == cbx.SelectedItem.ToString());

                            HelperStatic.APILanguageToUse = result.FirstOrDefault()
                                .Key;
                        }
                    }
                }

                // this needs to be an IF rather than an ELSE IF
                if (cItem.Name == "rbt_UseGeoNamesLocalLanguage" || cItem.Name == "rbt_TryUseGeoNamesLanguage")
                {
                    RadioButton rbt = (RadioButton)cItem;
                    if ((rbt.Font.Style & FontStyle.Bold) != 0 && rbt.Checked)
                    {
                        ComboBox cbx = cbx_TryUseGeoNamesLanguage;
                        if (rbt.Name == "rbt_UseGeoNamesLocalLanguage")
                        {
                            HelperStatic.APILanguageToUse = "local";
                            cbx.Enabled = false;
                        }
                        else if (rbt.Name == "rbt_TryUseGeoNamesLanguage")
                        {
                            cbx.Enabled = true;
                            IEnumerable<KeyValuePair<string, string>> result = AncillaryListsArrays.GetISO_639_1_Languages()
                                .Where(predicate: kvp => kvp.Value == cbx.SelectedItem.ToString());

                            HelperStatic.APILanguageToUse = result.FirstOrDefault()
                                .Key;
                        }
                    }
                }
            }
        }

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

        HelperStatic.SResetMapToZero = HelperStatic.DataReadCheckBoxSettingTrueOrFalse(
            tableName: "settings",
            settingTabPage: "tpg_Application",
            settingId: "ckb_ResetMapToZero"
        );

        HelperStatic.SOnlyShowFCodePPL = HelperStatic.DataReadCheckBoxSettingTrueOrFalse(
            tableName: "settings",
            settingTabPage: "tpg_Application",
            settingId: "ckb_PopulatedPlacesOnly"
        );

        FrmMainApp.AppStartupPullOverWriteBlankToponomy();
        FrmMainApp.AppStartupPullToponomyRadiusAndMaxRows();

        // save customRules
        HelperStatic.DataWriteSQLiteCustomRules();

        HelperStatic.DataWriteSQLiteCustomCityAllocationLogic();
        // read back/refresh lists.
        FrmMainApp.AppStartupReadCustomCityLogic();

        // in case it changed or something.
        dtCustomRules = HelperStatic.DataReadSQLiteCustomRules();
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
                                         .FirstOrDefault() +
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
                                                        .FirstOrDefault() +
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
    private void tpg_FileOptions_Enter(object sender,
                                       EventArgs e)
    {
        // at this point lbx_fileExtensions.SelectedIndex == 0;
        if (lbx_fileExtensions.SelectedItems.Count == 0)
        {
            lbx_fileExtensions.SelectedIndex = 0;
        }
    }


    /// <summary>
    ///     Handles the event where any rbt's checkstate changed.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void Any_rbt_CheckedChanged(object sender,
                                        EventArgs e)
    {
        if (!_nowLoadingSettingsData)
        {
            RadioButton rbt = (RadioButton)sender;
            GroupBox gbx = (GroupBox)rbt.Parent;
            gbx.Font = new Font(prototype: gbx.Font, newStyle: FontStyle.Bold);

            string actualParentName = rbt.Parent.Name;
            switch (actualParentName)
            {
                case "gbx_GeoNamesLanguageSettings":
                    if (rbt.Name == "rbt_UseGeoNamesLocalLanguage")
                    {
                        cbx_TryUseGeoNamesLanguage.Enabled = !rbt.Checked;
                    }

                    break;
            }

            // stick it into settings-Q
            HelperNonStatic helperNonstatic = new();

            string parentNameToUse = GetParentNameToUse(cItem: (Control)sender, cTpgApplication: lstTpgApplicationControls);

            HelperStatic.DataWriteSQLiteSettings(
                tableName: "settingsToWritePreQueue",
                settingTabPage: parentNameToUse,
                settingId: rbt.Name,
                settingValue: rbt.Checked.ToString()
                    .ToLower()
            );
        }
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
            FrmSettings frmSettingsInstance = (FrmSettings)Application.OpenForms[name: "FrmSettings"];

            CheckBox ckb = (CheckBox)sender;
            ckb.Font = new Font(prototype: ckb.Font, newStyle: FontStyle.Bold);
            string cItemName = "";

            object lbi = null;
            if (frmSettingsInstance != null && frmSettingsInstance.tct_Settings.SelectedTab.Name == "tpg_FileOptions")
            {
                lbi = lbx_fileExtensions.SelectedItem;
            }

            if (lbi != null)
            {
                cItemName = lbi.ToString()
                                .Split('\t')
                                .FirstOrDefault() +
                            '_' +
                            ckb.Name;
            }
            else
            {
                cItemName = ckb.Name;
            }

            if (cItemName == "ckb_ReplaceBlankToponyms")
            {
                tbx_ReplaceBlankToponyms.Enabled = ckb.Checked;
            }

            if (cItemName == "ckb_IncludePredeterminedCountries")
            {
                List<KeyValuePair<string, string>> clh_CountryCodeOptions = refreshClh_CountryCodeOptions(ckb_IncludePredeterminedCountries: ckb_IncludePredeterminedCountries.Checked);
                DataGridViewComboBoxColumn clh_CountryCode = (DataGridViewComboBoxColumn)dgv_CustomRules.Columns[columnName: "clh_CountryCode"];
                if (clh_CountryCode != null) // shouldn't really be null but just in case.
                {
                    clh_CountryCode.DataSource = clh_CountryCodeOptions;
                }
            }

            // stick it into settings-Q
            HelperNonStatic helperNonstatic = new();
            string parentNameToUse = GetParentNameToUse(cItem: (Control)sender, cTpgApplication: lstTpgApplicationControls);

            HelperStatic.DataWriteSQLiteSettings(
                tableName: "settingsToWritePreQueue",
                settingTabPage: parentNameToUse,
                settingId: cItemName,
                settingValue: ckb.Checked.ToString()
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
            TextBox tbx = (TextBox)sender;
            tbx.Font = new Font(prototype: tbx.Font, newStyle: FontStyle.Bold);

            // stick it into settings-Q
            HelperNonStatic helperNonstatic = new();
            string parentNameToUse = GetParentNameToUse(cItem: (Control)sender, cTpgApplication: lstTpgApplicationControls);

            HelperStatic.DataWriteSQLiteSettings(
                tableName: "settingsToWritePreQueue",
                settingTabPage: parentNameToUse,
                settingId: tbx.Name,
                settingValue: tbx.Text
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
            string settingValue = cbx.Text;

            if (cbx.Name == "cbx_Language" || cbx.Name == "cbx_TryUseGeoNamesLanguage")
            {
                // convert e.g. "Français [French]" to "French"
                settingValue = settingValue.Split('[')
                    .Last()
                    .Substring(startIndex: 0, length: settingValue.Split('[')
                                                          .Last()
                                                          .Length -
                                                      1);
            }

            // stick it into settings-Q
            HelperNonStatic helperNonstatic = new();
            string parentNameToUse = GetParentNameToUse(cItem: (Control)sender, cTpgApplication: lstTpgApplicationControls);

            HelperStatic.DataWriteSQLiteSettings(
                tableName: "settingsToWritePreQueue",
                settingTabPage: parentNameToUse,
                settingId: cbx.Name,
                settingValue: settingValue
            );
        }
    }

    /// <summary>
    ///     Handles the event where any nud's value changed.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void Any_nud_ValueChanged(object sender,
                                      EventArgs e)
    {
        if (!_nowLoadingSettingsData)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            nud.Font = new Font(prototype: nud.Font, newStyle: FontStyle.Bold);

            // stick it into settings-Q
            HelperNonStatic helperNonstatic = new();
            string parentNameToUse = GetParentNameToUse(cItem: (Control)sender, cTpgApplication: lstTpgApplicationControls);

            HelperStatic.DataWriteSQLiteSettings(
                tableName: "settingsToWritePreQueue",
                settingTabPage: parentNameToUse,
                settingId: nud.Name,
                settingValue: nud.Value.ToString(provider: CultureInfo.InvariantCulture)
            );
        }
    }

    /// <summary>
    ///     Disables AcceptButton -> so that user can't hit "Enter" because it wouldn't be saved. VS is a bit silly.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Any_nud_Enter(object sender,
                               EventArgs e)
    {
        AcceptButton = null;
    }

    /// <summary>
    ///     Reinstates AcceptButton
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Any_nud_Leave(object sender,
                               EventArgs e)
    {
        AcceptButton = btn_OK;
    }

    /// <summary>
    ///     Launches the browser to open the link in the richtextbox
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void rbx_CustomRulesExplanation_LinkClicked(object sender,
                                                        LinkClickedEventArgs e)
    {
        Process.Start(fileName: e.LinkText);
    }

    /// <summary>
    ///     Displays an error msg if DGV failed validation
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void dgv_CustomRules_DataError(object sender,
                                           DataGridViewDataErrorEventArgs e)
    {
        if (e.Exception != null &&
            e.Context == DataGridViewDataErrorContexts.Commit)
        {
            MessageBox.Show(
                text: HelperStatic.GenericGetMessageBoxText(
                    messageBoxName: "mbx_FrmSettings_dgv_CustomRules_ColumnCannotBeEmpty"),
                caption: HelperStatic.GenericGetMessageBoxCaption(captionType: "Info"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    ///     Responsible for the DGV validation
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void dgv_CustomRules_RowValidating(object sender,
                                               DataGridViewCellCancelEventArgs e)
    {
        if (e.ColumnIndex ==
            dgv_CustomRules.Columns[columnName: "clh_TargetPointOutcome"]
                .Index)
        {
            string clh_TargetPointOutcomeValue = Convert.ToString(value: (dgv_CustomRules.Rows[index: e.RowIndex]
                                                                      .Cells[columnName: "clh_TargetPointOutcome"] as DataGridViewComboBoxCell)?.EditedFormattedValue);

            string clh_TargetPointOutcomeCustomValue = Convert.ToString(value: (dgv_CustomRules.Rows[index: e.RowIndex]
                                                                            .Cells[columnName: "clh_TargetPointOutcomeCustom"] as DataGridViewTextBoxCell)?.EditedFormattedValue);
            if (clh_TargetPointOutcomeValue == "Custom" && IsNullOrEmpty(value: clh_TargetPointOutcomeCustomValue))
            {
                e.Cancel = true;
                MessageBox.Show(
                    text: HelperStatic.GenericGetMessageBoxText(
                        messageBoxName: "mbx_FrmSettings_dgv_CustomRules_CustomOutcomeCannotBeEmpty"),
                    caption: HelperStatic.GenericGetMessageBoxCaption(captionType: "Info"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Warning);
            }
        }
    }

    private void btn_ResetToDefaults_Click(object sender,
                                           EventArgs e)
    {
        HelperStatic.DataWriteSQLiteCustomCityAllocationLogicDefaults(resetToDefaults: true);
        // reload
        dgv_CustomCityLogic.Columns.Clear();
        LoadCustomCityLogicDGV();
        MessageBox.Show(
            text: HelperStatic.GenericGetMessageBoxText(
                messageBoxName: "mbx_GenericDone"),
            caption: HelperStatic.GenericGetMessageBoxCaption(captionType: "Info"),
            buttons: MessageBoxButtons.OK,
            icon: MessageBoxIcon.Information);
    }
}