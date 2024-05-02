using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using GeoTagNinja.View.DialogAndMessageBoxes;
using static System.String;
using static GeoTagNinja.View.ListView.FileListView;

namespace GeoTagNinja;

internal enum SettingsImportExportOptions
{
    ApplicationSettings,
    CityRulesSettings,
    CustomRulesSettings
}

public partial class FrmSettings : Form
{
    private static List<Control> _lstTpgApplicationControls = new(); // do not rename
    private static List<Control> _lstTpgGeoNamesControls = new(); // do not rename
    private static bool _importHasBeenProcessed;
    private readonly string _languageSavedInSQL;
    private bool _nowLoadingSettingsData;

    /// <summary>
    ///     This Form provides an interface for the user to edit various app and file-specific settings.
    /// </summary>
    public FrmSettings()
    {
        InitializeComponent();

        // the custom logic is ugly af so no need to be pushy about it in light mode.
        if (!HelperVariables.UserSettingUseDarkMode)
        {
            tcr_Settings.DrawMode = TabDrawMode.Normal;
        }

        HelperControlThemeManager.SetThemeColour(
            themeColour: HelperVariables.UserSettingUseDarkMode
                ? ThemeColour.Dark
                : ThemeColour.Light, parentControl: this);

        // this one is largely responsible for disabling the detection of "new" (changed) data. (ie when going from "noting" to "something")
        _nowLoadingSettingsData = true;
        HelperNonStatic helperNonstatic = new();
        HelperControlAndMessageBoxHandling.ReturnControlText(
            cItem: this, senderForm: this);

        // Gets the various controls' labels and values (eg "latitude" and "51.002")
        _lstTpgApplicationControls = helperNonstatic
                                    .GetAllControls(control: tpg_Application)
                                    .ToList();
        _lstTpgGeoNamesControls = helperNonstatic.GetAllControls(control: tpg_GeoNames)
                                                 .ToList();

        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        if (c != null)
        {
            foreach (Control cItem in c)
            {
                string parentNameToUse = GetParentNameToUse(cItem: cItem);

                if (cItem.Name == "cbx_Language" ||
                    cItem.Name == "cbx_TryUseGeoNamesLanguage")
                {
                    ComboBox cbx = (ComboBox)cItem;

                    string[] alreadyTranslatedLanguages = { "en", "fr" };
                    _languageSavedInSQL =
                        HelperDataApplicationSettings.DataReadSQLiteSettings(
                            tableName: "settings",
                            settingTabPage: parentNameToUse,
                            settingId: cbx.Name
                        );

                    foreach (KeyValuePair<string, string> languagePair in
                             HelperGenericAncillaryListsArrays.GetISO_639_1_Languages())
                    {
                        if ((cItem.Name == "cbx_Language" &&
                             alreadyTranslatedLanguages.Contains(
                                 value: languagePair.Key)) ||
                            cItem.Name == "cbx_TryUseGeoNamesLanguage")

                        {
                            cbx.Items.Add(item: languagePair.Value);
                        }
                    }

                    if (_languageSavedInSQL == null)
                    {
                        cbx.SelectedIndex =
                            cbx.FindStringExact(s: HelperVariables.DefaultEnglishString);
                    }
                    else
                    {
                        for (int i = 0; i < cbx.Items.Count; i++)
                        {
                            string value = cbx.GetItemText(item: cbx.Items[index: i]);
                            if (value.Contains(value: _languageSavedInSQL))
                            {
                                cbx.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }

                else
                {
                    HelperControlAndMessageBoxHandling.ReturnControlText(cItem: cItem,
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
    /// <returns>The actual Parent Name if tpg_Application isn't a parent or string literal "tpg_Application" if it is.</returns>
    private static string GetParentNameToUse(Control cItem)
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

        if (_lstTpgApplicationControls.Contains(item: cItem) ||
            _lstTpgGeoNamesControls.Contains(item: cItem))
        {
            // yeah this is parentNameToUse = "tpg_Application" on purpose.
            // Bad planning to be fair but i split the tpg_Application into two tpgs at some point by which time users' sqlite databases would have tpg_Application as a value for these keys
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
        foreach (string ext in
                 HelperGenericAncillaryListsArrays.AllCompatibleExtensions())
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
                string parentNameToUse = GetParentNameToUse(cItem: cItem);

                {
                    if (cItem is TextBox tbx)
                    {
                        try
                        {
                            tbx.Text = HelperDataApplicationSettings
                               .DataReadSQLiteSettings(
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
                            ckb.CheckState = HelperDataApplicationSettings
                               .DataReadCheckBoxSettingTrueOrFalse(
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
                        string nudTempValue =
                            HelperDataApplicationSettings.DataReadSQLiteSettings(
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
                            rbt.Checked = HelperDataApplicationSettings
                               .DataReadCheckBoxSettingTrueOrFalse(
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
            HelperVariables.APILanguageToUse = "local";
            cbx_TryUseGeoNamesLanguage.Enabled = false;
        }
        else
        {
            cbx_TryUseGeoNamesLanguage.Enabled = true;
        }

        // (re)set the map colour mode
        if (rbt_MapColourModeDarkPale.Checked)
        {
            HelperVariables.UserSettingMapColourMode = "DarkPale";
        }
        else if (rbt_MapColourModeDarkInverse.Checked)
        {
            HelperVariables.UserSettingMapColourMode = "DarkInverse";
        }
        else
        {
            HelperVariables.UserSettingMapColourMode =
                "Normal"; // technically we could just ignore this
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
        HelperVariables.DtCustomRules = HelperDataCustomRules.DataReadSQLiteCustomRules();
        dgv_CustomRules.AutoGenerateColumns = false;

        BindingSource source = new();
        source.DataSource = HelperVariables.DtCustomRules;
        dgv_CustomRules.DataSource = source;

        Dictionary<string, string> clh_CountryCodeOptions = refreshClh_CountryCodeOptions(
            ckb_IncludePredeterminedCountries: HelperDataApplicationSettings
               .DataReadCheckBoxSettingTrueOrFalse(
                    tableName: "settings",
                    settingTabPage: "tpg_CustomRules",
                    settingId: "ckb_IncludePredeterminedCountries"
                ));
        DataGridViewComboBoxColumn clh_CountryCode = new()
        {
            DataPropertyName = "CountryCode",
            Name = COL_NAME_PREFIX + FileListColumns.COUNTRY_CODE,
            HeaderText = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: ControlType.ColumnHeader,
                objectName: COL_NAME_PREFIX + FileListColumns.COUNTRY),
            DataSource = clh_CountryCodeOptions.ToList(), // needs to be a list
            ValueMember = "Key",
            DisplayMember = "Value"
        };

        DataGridViewComboBoxColumn clh_DataPointName = new()
        {
            DataPropertyName = "DataPointName",
            Name = "clh_DataPointName",
            HeaderText = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: ControlType.ColumnHeader, objectName: "clh_DataPointName")
        };

        // e.g.: "AdminName1","AdminName2"...
        foreach (string itemName in HelperGenericAncillaryListsArrays
                    .CustomRulesDataSources())
        {
            clh_DataPointName.Items.Add(item: itemName);
        }

        DataGridViewComboBoxColumn clh_DataPointConditionType = new()
        {
            DataPropertyName = "DataPointConditionType",
            Name = "clh_DataPointConditionType",
            HeaderText = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: ControlType.ColumnHeader,
                objectName: "clh_DataPointConditionType")
        };

        // e.g.: "Is","Contains"...
        foreach (string itemName in HelperGenericAncillaryListsArrays
                    .CustomRulesDataConditions())
        {
            clh_DataPointConditionType.Items.Add(item: itemName);
        }

        DataGridViewTextBoxColumn clh_DataPointConditionValue = new()
        {
            DataPropertyName = "DataPointConditionValue",
            Name = "clh_DataPointConditionValue",
            HeaderText = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: ControlType.ColumnHeader,
                objectName: "clh_DataPointConditionValue")
        };

        DataGridViewComboBoxColumn clh_TargetPointName = new()
        {
            DataPropertyName = "TargetPointName",
            Name = "clh_TargetPointName",
            HeaderText = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: ControlType.ColumnHeader, objectName: "clh_TargetPointName")
        };

        // e.g.:  "State","City"...
        foreach (SourcesAndAttributes.ElementAttribute itemName in
                 HelperGenericAncillaryListsArrays.CustomRulesDataTargets())
        {
            clh_TargetPointName.Items.Add(item: itemName);
        }

        DataGridViewComboBoxColumn clh_TargetPointOutcome = new()
        {
            DataPropertyName = "TargetPointOutcome",
            Name = "clh_TargetPointOutcome",
            HeaderText = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: ControlType.ColumnHeader,
                objectName: "clh_TargetPointOutcome")
        };

        // e.g.: "AdminName1","AdminName2"... +Null (empty)" + Custom
        foreach (string itemName in HelperGenericAncillaryListsArrays
                    .CustomRulesDataSources(isOutcome: true))
        {
            clh_TargetPointOutcome.Items.Add(item: itemName);
        }

        DataGridViewTextBoxColumn clh_TargetPointOutcomeCustom = new()
        {
            DataPropertyName = "TargetPointOutcomeCustom",
            Name = "clh_TargetPointOutcomeCustom",
            HeaderText = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: ControlType.ColumnHeader,
                objectName: "clh_TargetPointOutcomeCustom")
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
    ///     Loads the custom city logic data into the DataGridView (dgv_CustomCityLogic) from the SQLite database.
    ///     The method first reads the data from the SQLite database into a DataTable (DtCustomCityLogic).
    ///     It then sets up the DataGridView with two columns: 'CountryCode' and 'TargetPointNameCustomCityLogic'.
    ///     The 'CountryCode' column is populated with a list of country codes and countries, and is read-only.
    ///     The 'TargetPointNameCustomCityLogic' column is populated with a list of items from the CustomCityLogicDataSources.
    /// </summary>
    [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
    private void LoadCustomCityLogicDGV()
    {
        HelperVariables.DtCustomCityLogic = HelperDataCustomCityAllocationRules
           .DataReadSQLiteCustomCityAllocationLogic();
        dgv_CustomCityLogic.AutoGenerateColumns = false;

        BindingSource source = new();
        source.DataSource = HelperVariables.DtCustomCityLogic;
        dgv_CustomCityLogic.DataSource = source;

        Dictionary<string, string> clh_CountryCodeOptions =
            refreshClh_CountryCodeOptions(ckb_IncludePredeterminedCountries: true);
        DataGridViewComboBoxColumn clh_CountryCode = new()
        {
            DataPropertyName = "CountryCode",
            Name = COL_NAME_PREFIX + FileListColumns.COUNTRY_CODE,
            HeaderText = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: ControlType.ColumnHeader,
                objectName: COL_NAME_PREFIX + FileListColumns.COUNTRY),
            DataSource = clh_CountryCodeOptions.ToList(), // needs to be a list
            ValueMember = "Key",
            DisplayMember = "Value",
            ReadOnly = true
        };

        DataGridViewComboBoxColumn clh_TargetPointNameCustomCityLogic = new()
        {
            DataPropertyName = "TargetPointNameCustomCityLogic",
            Name = "clh_TargetPointNameCustomCityLogic",
            HeaderText = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: ControlType.ColumnHeader,
                objectName: "clh_TargetPointNameCustomCityLogic")
        };

        // e.g.: "AdminName1","AdminName2"... 
        foreach (string itemName in HelperGenericAncillaryListsArrays
                    .CustomCityLogicDataSources())
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
    private static Dictionary<string, string> refreshClh_CountryCodeOptions(
        bool ckb_IncludePredeterminedCountries)
    {
        Dictionary<string, string> clh_CountryCodeOptions = new();
        // e.g. "GBR//United Kingdom of...."
        foreach (DataRow dataRow in HelperVariables.DtIsoCountryCodeMapping.Rows)
        {
            clh_CountryCodeOptions.Add(key: dataRow[columnName: "ISO_3166_1A3"]
                                          .ToString()
                                     , value: dataRow[columnName: "Country"]
                                          .ToString());
        }

        // if _do not_ IncludeNonPredeterminedCountries then remove those
        if (!ckb_IncludePredeterminedCountries)
        {
            foreach (DataRow dataRow in HelperVariables.DtIsoCountryCodeMapping.Rows)
            {
                string countryCode = dataRow[columnName: "ISO_3166_1A3"]
                   .ToString();
                if (!HelperVariables.LstCityNameIsUndefined.Contains(item: countryCode))
                {
                    clh_CountryCodeOptions.Remove(key: dataRow[columnName: "ISO_3166_1A3"]
                                                     .ToString()
                    );
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
        HelperDataApplicationSettings.DataDeleteSQLitesettingsToWritePreQueue();
        Hide();
    }

    /// <summary>
    ///     Handles the event where user clicks OK. Writes settings to SQLite and refreshes data where necessary.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    /// <remarks>
    ///     The main body of this only fires if _importHasBeenProcessed = false; the reason for this is that OK writes to the
    ///     SQLite file based on what's _in_ the Form whereas the Import logic doesn't change the Form per se.
    ///     While I could probably import into the settings' write-queue rather than the database file itself, at the moment
    ///     it's too much hassle to code and would need a pretty major rewrite.
    /// </remarks>
    private void Btn_OK_Click(object sender,
                              EventArgs e)
    {
        List<string> rbtGeoNamesLanguage = new()
        {
            "rbt_UseGeoNamesLocalLanguage",
            "rbt_TryUseGeoNamesLanguage"
        };

        List<string> rbtMapColourOptions = new()
        {
            "rbt_MapColourModeNormal",
            "rbt_MapColourModeDarkInverse",
            "rbt_MapColourModeDarkPale"
        };
        if (!_importHasBeenProcessed)
        {
            HelperNonStatic helperNonstatic = new();
            IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
            if (c != null)
            {
                foreach (Control cItem in c)
                {
                    if (cItem is CheckBox ckb)
                    {
                        if ((ckb.Font.Style & FontStyle.Bold) != 0)
                        {
                            // strictly speaking for ckb_UseDarkMode we don't need to restart the app and the checker could be called here but it'd need another loop.
                            if (ckb.Name == "ckb_UseImperialNotMetric" ||
                                ckb.Name == "ckb_UseDarkMode")
                            {
                                PromptUserToRestartApp();
                            }
                        }
                    }

                    if (cItem is ComboBox cbx)
                    {
                        // if modified
                        if ((cbx.Font.Style & FontStyle.Bold) != 0)
                        {
                            if (cbx.Name == "cbx_Language")
                            {
                                PromptUserToRestartApp();
                            }
                            else if (cbx.Name == "cbx_TryUseGeoNamesLanguage")
                            {
                                IEnumerable<KeyValuePair<string, string>> result =
                                    HelperGenericAncillaryListsArrays
                                       .GetISO_639_1_Languages()
                                       .Where(predicate: kvp =>
                                                  kvp.Value ==
                                                  cbx.SelectedItem.ToString());

                                HelperVariables.APILanguageToUse = result.FirstOrDefault()
                                   .Key;
                            }
                        }
                    }

                    if (cItem is RadioButton rbt)
                    {
                        // this needs to be an IF rather than an ELSE IF
                        if (rbtGeoNamesLanguage.Contains(item: rbt.Name))
                        {
                            // (rbt.Font.Style & FontStyle.Bold) here means that there has been a change of state and it needs saving
                            if ((rbt.Font.Style & FontStyle.Bold) != 0 &&
                                rbt.Checked)
                            {
                                ComboBox cbxLng = cbx_TryUseGeoNamesLanguage;
                                if (rbt.Name == "rbt_UseGeoNamesLocalLanguage")
                                {
                                    HelperVariables.APILanguageToUse = "local";
                                    cbxLng.Enabled = false;
                                }
                                else if (rbt.Name == "rbt_TryUseGeoNamesLanguage")
                                {
                                    cbxLng.Enabled = true;
                                    IEnumerable<KeyValuePair<string, string>> result =
                                        HelperGenericAncillaryListsArrays
                                           .GetISO_639_1_Languages()
                                           .Where(
                                                predicate: kvp =>
                                                    kvp.Value ==
                                                    cbxLng.SelectedItem
                                                          .ToString());

                                    HelperVariables.APILanguageToUse = result
                                       .FirstOrDefault()
                                       .Key;
                                }
                            }
                        }
                        else if (rbtMapColourOptions.Contains(item: rbt.Name))
                        {
                            if ((rbt.Font.Style & FontStyle.Bold) != 0 &&
                                rbt.Checked)
                            {
                                HelperVariables.UserSettingMapColourMode =
                                    rbt.Name.Replace(oldValue: "rbt_MapColourMode",
                                                     newValue: "");
                            }
                        }
                    }
                }
            }

            HelperDataApplicationSettings.DataTransferSQLiteSettings();
        }

        HelperDataApplicationSettings.DataDeleteSQLitesettingsToWritePreQueue();

        // refresh user data
        HelperGenericAppStartup.AppStartupApplyDefaults();

        HelperGenericAppStartup.AppStartupPullOverWriteBlankToponomy();
        HelperGenericAppStartup.AppStartupPullToponomyRadiusAndMaxRows();
        HelperDataCustomRules.DataWriteSQLiteCustomRules();
        HelperDataCustomCityAllocationRules.DataWriteSQLiteCustomCityAllocationLogic();
        // read back/refresh lists.
        HelperGenericAppStartup.AppStartupReadCustomCityLogic();

        // in case it changed or something.
        HelperVariables.DtCustomRules = HelperDataCustomRules.DataReadSQLiteCustomRules();
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
                string tmpCtrlVal = HelperDataApplicationSettings.DataReadSQLiteSettings(
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
                        tmpCtrlVal = HelperDataApplicationSettings.DataReadSQLiteSettings(
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

                            if (tmpCtrlGroup.Contains(value: "raw") ||
                                tmpCtrlGroup.Contains(value: "tiff") ||
                                tmpCtrlGroup.Contains(value: "dng"))
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
                        HelperDataApplicationSettings.DataWriteSQLiteSettings(
                            tableName: "settings",
                            settingTabPage: ((Control)sender).Parent.Name,
                            settingId: tmpCtrlName,
                            settingValue: tmpCtrlVal
                        );
                    }
                }

                if (tmpCtrlName.Contains(value: "ckb_ProcessOriginalFile"))
                {
                    ckb_ResetFileDateToCreated.Enabled = box.Checked;
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

            string parentNameToUse = GetParentNameToUse(cItem: (Control)sender);
            HelperDataApplicationSettings.DataWriteSQLiteSettings(
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
            FrmSettings frmSettingsInstance =
                (FrmSettings)Application.OpenForms[name: "FrmSettings"];

            CheckBox ckb = (CheckBox)sender;
            ckb.Font = new Font(prototype: ckb.Font, newStyle: FontStyle.Bold);
            string cItemName = "";

            object lbi = null;
            if (frmSettingsInstance != null &&
                frmSettingsInstance.tcr_Settings.SelectedTab.Name == "tpg_FileOptions")
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
                Dictionary<string, string> clh_CountryCodeOptions =
                    refreshClh_CountryCodeOptions(
                        ckb_IncludePredeterminedCountries:
                        ckb_IncludePredeterminedCountries.Checked);
                DataGridViewComboBoxColumn clh_CountryCode =
                    (DataGridViewComboBoxColumn)dgv_CustomRules.Columns[
                        columnName: "clh_CountryCode"];
                if (clh_CountryCode != null) // shouldn't really be null but just in case.
                {
                    BindingList<KeyValuePair<string, string>> bindingList =
                        new(
                            list: clh_CountryCodeOptions.ToList());
                    clh_CountryCode.DataSource = bindingList;
                    clh_CountryCode.DisplayMember = "Value";
                    clh_CountryCode.ValueMember = "Key";
                }
            }

            // stick it into settings-Q
            HelperNonStatic helperNonstatic = new();
            string parentNameToUse = GetParentNameToUse(cItem: (Control)sender);
            HelperDataApplicationSettings.DataWriteSQLiteSettings(
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
            string parentNameToUse = GetParentNameToUse(cItem: (Control)sender);
            HelperDataApplicationSettings.DataWriteSQLiteSettings(
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

            if (cbx.Name == "cbx_Language" ||
                cbx.Name == "cbx_TryUseGeoNamesLanguage")
            {
                // convert e.g. "Français [French]" to "French"
                settingValue = settingValue.Split('[')
                                           .Last()
                                           .Substring(startIndex: 0, length: settingValue
                                                         .Split('[')
                                                         .Last()
                                                         .Length -
                                                      1);
            }

            // stick it into settings-Q
            HelperNonStatic helperNonstatic = new();
            string parentNameToUse = GetParentNameToUse(cItem: (Control)sender);
            HelperDataApplicationSettings.DataWriteSQLiteSettings(
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
        NumericUpDown nud = (NumericUpDown)sender;
        if (!_nowLoadingSettingsData)
        {
            nud.Font = new Font(prototype: nud.Font, newStyle: FontStyle.Bold);

            // stick it into settings-Q
            HelperNonStatic helperNonstatic = new();
            string parentNameToUse = GetParentNameToUse(cItem: (Control)sender);
            HelperDataApplicationSettings.DataWriteSQLiteSettings(
                tableName: "settingsToWritePreQueue",
                settingTabPage: parentNameToUse,
                settingId: nud.Name,
                settingValue: nud.Value.ToString(provider: CultureInfo.InvariantCulture)
            );
        }

        if (nud.Name == "nud_ChoiceRadius")
        {
            string tmpLabelText =
                HelperControlAndMessageBoxHandling.ReturnControlTextAsString(
                    cItem: lbl_Miles, senderForm: this);
            lbl_Miles.Text =
                "(" +
                Math.Round(d: nud.Value / (decimal)1.60934, decimals: 2)
                    .ToString(provider: CultureInfo.CurrentCulture) +
                " " +
                tmpLabelText +
                ")"
                ;
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
            CustomMessageBox customMessageBox = new(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                    messageBoxName:
                    "mbx_FrmSettings_dgv_CustomRules_ColumnCannotBeEmpty"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                    captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption
                       .Information.ToString()),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
            customMessageBox.ShowDialog();
        }
    }

    /// <summary>
    ///     Handles the RowValidating event of the dgv_CustomRules DataGridView.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The DataGridViewCellCancelEventArgs instance containing the event data.</param>
    /// <remarks>
    ///     This method validates the data in the "clh_TargetPointOutcome" column of the DataGridView.
    ///     If the value of the "clh_TargetPointOutcome" cell is "Custom" and the corresponding "clh_TargetPointOutcomeCustom"
    ///     cell is empty,
    ///     it cancels the event, preventing the user from leaving the cell until a valid value is entered, and displays a
    ///     warning message.
    /// </remarks>
    private void dgv_CustomRules_RowValidating(object sender,
                                               DataGridViewCellCancelEventArgs e)
    {
        if (e.ColumnIndex ==
            dgv_CustomRules.Columns[columnName: "clh_TargetPointOutcome"]
                           .Index)
        {
            string clh_TargetPointOutcomeValue = Convert.ToString(
                value: (dgv_CustomRules.Rows[index: e.RowIndex]
                                       .Cells[columnName: "clh_TargetPointOutcome"] as
                    DataGridViewComboBoxCell)?.EditedFormattedValue);

            string clh_TargetPointOutcomeCustomValue = Convert.ToString(
                value: (dgv_CustomRules.Rows[index: e.RowIndex]
                                       .Cells[columnName: "clh_TargetPointOutcomeCustom"]
                    as DataGridViewTextBoxCell)?.EditedFormattedValue);
            if (clh_TargetPointOutcomeValue == "Custom" &&
                IsNullOrEmpty(value: clh_TargetPointOutcomeCustomValue))
            {
                e.Cancel = true;
                CustomMessageBox customMessageBox = new(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                        messageBoxName:
                        "mbx_FrmSettings_dgv_CustomRules_CustomOutcomeCannotBeEmpty"),
                    caption: HelperControlAndMessageBoxHandling
                       .GenericGetMessageBoxCaption(
                            captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Information.ToString()),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Warning);
                customMessageBox.ShowDialog();
            }
        }
    }

    /// <summary>
    ///     Handles the Click event of the btn_ResetToDefaults control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    /// <remarks>
    ///     This method resets the custom city allocation rules to their default values, clears the data grid view columns,
    ///     reloads the custom city logic data grid view, and displays a message box with information about the operation.
    /// </remarks>
    private void btn_ResetToDefaults_Click(object sender,
                                           EventArgs e)
    {
        HelperDataCustomCityAllocationRules
           .DataWriteSQLiteCustomCityAllocationLogicDefaults(resetToDefaults: true);
        // reload
        dgv_CustomCityLogic.Columns.Clear();
        LoadCustomCityLogicDGV();
        CustomMessageBox customMessageBox = new(
            text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                messageBoxName: "mbx_GenericDone"),
            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption
                   .Information.ToString()),
            buttons: MessageBoxButtons.OK,
            icon: MessageBoxIcon.Information);
        customMessageBox.ShowDialog();
    }


#region Import-export

    /// <summary>
    ///     Handles the Click event of the ExportSettings button.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    /// <remarks>
    ///     This method initiates the process of exporting the settings. It first gathers the settings to be exported by
    ///     showing a dialog with checkboxes.
    ///     If the user chooses to proceed with the export (by not selecting "no"), a SaveFileDialog is shown where the user
    ///     can choose the location and name of the exported SQLite database file.
    ///     The settings are then exported to the chosen file. If an error occurs during the export, a MessageBox is shown with
    ///     the error message.
    /// </remarks>
    private void btn_ExportSettings_Click(object sender,
                                          EventArgs e)
    {
        Dictionary<string, string> checkboxDictionary = GetCheckboxDictionary();

        Dictionary<string, string> buttonsDictionary = GetButtonsDictionary();

        // ReSharper disable once InconsistentNaming
        List<string> ItemsToExport = DialogWithOrWithoutCheckBox.DisplayAndReturnList(
            labelText: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                messageBoxName: "mbx_FrmSettings_QuestionWhatToExport"),
            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Question
                                              .ToString()),
            buttonsDictionary: buttonsDictionary,
            orientation: "Vertical",
            checkboxesDictionary: checkboxDictionary);

        // ignore the whole thing if "no" is part of the output
        // probably impossible that _neither_ of them are in the list but in case i missed something...
        if (!ItemsToExport.Contains(item: "no") &&
            ItemsToExport.Contains(item: "yes"))
        {
            using (SaveFileDialog exportFileDialog = new())
            {
                exportFileDialog.Filter = "SQLite Databasee|*.db";
                exportFileDialog.Title = "Save a SQLite File";
                exportFileDialog.FileName = "GeoTagNinja_Settings_Export_" +
                                            DateTime.Now.ToString(
                                                format: "yyyyMMdd_HHmm");
                exportFileDialog.ShowDialog();

                // If the file name is not an empty string open it for saving.
                if (exportFileDialog.FileName != "")
                {
                    try
                    {
                        File.Copy(
                            sourceFileName: HelperVariables.SettingsDatabaseFilePath,
                            destFileName: Path.Combine(exportFileDialog.FileName),
                            overwrite: true);
                        HelperDataSettingsExport.DataExportSettings(
                            settingsToExportList: ItemsToExport,
                            exportFilePath: Path.Combine(exportFileDialog.FileName));
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox customMessageBox = new(
                            text: HelperControlAndMessageBoxHandling
                                     .GenericGetMessageBoxText(
                                          messageBoxName:
                                          "mbx_FrmSettings_ErrorExportFailed") +
                                  ex.Message,
                            caption: HelperControlAndMessageBoxHandling
                               .GenericGetMessageBoxCaption(
                                    captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Error.ToString()),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Error);
                        customMessageBox.ShowDialog();
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Handles the Click event of the Import Settings button.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An EventArgs that contains the event data.</param>
    /// <remarks>
    ///     This method opens a dialog for the user to select a database file to import.
    ///     If a file is selected, it displays another dialog with checkboxes for the user to select specific items to import.
    ///     If the user confirms the selection, the method calls the DataImportSettings method to import the selected items
    ///     from the chosen database file.
    /// </remarks>
    private void btn_ImportSettings_Click(object sender,
                                          EventArgs e)
    {
        string? databaseFileToImport = GetDatabaseFileToImport();
        if (databaseFileToImport is not null)
        {
            Dictionary<string, string> checkboxDictionary = GetCheckboxDictionary();

            Dictionary<string, string> buttonsDictionary = GetButtonsDictionary();

            // ReSharper disable once InconsistentNaming
            List<string> ItemsToImport = DialogWithOrWithoutCheckBox.DisplayAndReturnList(
                labelText: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                    messageBoxName: "mbx_FrmSettings_QuestionWhatToImport"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                    captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption
                       .Question.ToString()),
                buttonsDictionary: buttonsDictionary,
                orientation: "Vertical",
                checkboxesDictionary: checkboxDictionary);

            // ignore the whole thing if "no" is part of the output
            // probably impossible that _neither_ of them are in the list but in case i missed something...
            if (!ItemsToImport.Contains(item: "no") &&
                ItemsToImport.Contains(item: "yes"))
            {
                _importHasBeenProcessed = HelperDataSettingsImport.DataImportSettings(
                    settingsToImportList: ItemsToImport,
                    importFilePath: databaseFileToImport);

                // this is done so that the user doesn't end up clicking OK and then triggering a re-save of what they may have queued up against warnings.
                // Cancel clears the write queue but since the database would have been overwritten that's a reasonable logical path to take.
                if (_importHasBeenProcessed)
                {
                    PromptUserToRestartApp();
                    btn_Cancel.PerformClick();
                }
            }
        }

        string GetDatabaseFileToImport()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "SQLite Database files (*.db)|*.db",
                Title = "Select a database file"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }

            return null;
        }
    }

    /// <summary>
    ///     Prompts the user to restart the application.
    /// </summary>
    /// <remarks>
    ///     This method displays a dialog box with options to restart the application now or later.
    ///     If the user chooses to restart now, the application is restarted immediately.
    ///     If the user chooses to restart later, a warning message box is displayed.
    /// </remarks>
    private void PromptUserToRestartApp()
    {
        Dictionary<string, string> buttonsDictionary = new()
        {
            {
                HelperDataLanguageTZ.DataReadDTObjectText(
                    objectType: ControlType.Button,
                    objectName: "btn_RestartNow"),
                "RestartNow"
            },
            {
                HelperDataLanguageTZ.DataReadDTObjectText(
                    objectType: ControlType.Button,
                    objectName: "btn_RestartLater"),
                "RestartLater"
            }
        };

        // ReSharper disable once InconsistentNaming
        List<string> displayAndReturnList =
            DialogWithOrWithoutCheckBox.DisplayAndReturnList(
                labelText: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                    messageBoxName: "mbx_FrmSettings_PleaseRestartApp"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                    captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption
                       .Question
                       .ToString()),
                buttonsDictionary: buttonsDictionary,
                orientation: "Horizontal",
                checkboxesDictionary: new Dictionary<string, string>());

        // basically this triggers an error in debug mode but works ok in prod.
        #if !DEBUG
        if (displayAndReturnList.Contains(item: "RestartNow"))
        {
            Process.Start(fileName: Application.ExecutablePath);
            Application.Exit();
        }
        #endif
    }

    /// <summary>
    ///     Creates and returns a dictionary representing buttons in the dialog.
    ///     Each key-value pair in the dictionary represents a button, where the key is the text displayed on the button,
    ///     and the value is the return value when the button is clicked.
    /// </summary>
    /// <returns>A dictionary of string pairs representing the buttons in the dialog.</returns>
    private static Dictionary<string, string> GetButtonsDictionary()
    {
        Dictionary<string, string> buttonsDictionary = new()
        {
            {
                HelperDataLanguageTZ.DataReadDTObjectText(
                    objectType: ControlType.Button,
                    objectName: "btn_OK"
                ),
                "yes" // that's ok
            },
            {
                HelperDataLanguageTZ.DataReadDTObjectText(
                    objectType: ControlType.Button,
                    objectName: "btn_Cancel"
                ),
                "no" // that's ok
            }
        };
        return buttonsDictionary;
    }

    /// <summary>
    ///     Creates and returns a dictionary representing checkboxes for the settings import/export dialog.
    ///     Each key-value pair in the dictionary represents a checkbox, where the key is the text displayed next to the
    ///     checkbox,
    ///     and the value is the name of the corresponding `SettingsImportExportOptions` enum value.
    /// </summary>
    /// <returns>A dictionary of string pairs representing the checkboxes in the settings import/export dialog.</returns>
    private static Dictionary<string, string> GetCheckboxDictionary()
    {
        Dictionary<string, string> checkboxDictionary = new();
        foreach (string name in Enum.GetNames(
                     enumType: typeof(SettingsImportExportOptions)))
        {
            checkboxDictionary.Add(key: HelperDataLanguageTZ.DataReadDTObjectText(
                                       objectType: ControlType.CheckBox,
                                       objectName: "ckb_ImportExport_" +
                                                   name), value: name);
        }

        return checkboxDictionary;
    }

#endregion
}