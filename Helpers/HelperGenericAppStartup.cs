using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GeoTagNinja.Model;
using GeoTagNinja.View.ListView;
using Microsoft.Web.WebView2.Core;

namespace GeoTagNinja.Helpers;

internal static class HelperGenericAppStartup
{
    /// <summary>
    ///     Creates the database sqlite file
    /// </summary>
    public static void AppStartupCreateDataBaseFile()
    {
        FrmMainApp.Logger.Info(message: "Starting");
        // load all settings
        try
        {
            FrmMainApp.Logger.Debug(message: "applicationDataGeoTagNinjaFolder");
            string applicationDataGeoTagNinjaFolder = Path.Combine(path1: Environment.GetFolderPath(folder: Environment.SpecialFolder.ApplicationData), path2: "GeoTagNinja");
            Directory.CreateDirectory(path: applicationDataGeoTagNinjaFolder);
            HelperDataDatabaseAndStartup.DataCreateSQLiteDB();
        }
        catch (Exception ex)
        {
            FrmMainApp.Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantCreateSQLiteDB") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    /// <summary>
    ///     Writes defaults to sqlite if they don't exist
    /// </summary>
    public static void AppStartupWriteDefaultSettings()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        // write settings for combobox defaults etc
        try
        {
            HelperDataDatabaseAndStartup.DataWriteSQLiteSettingsDefaultSettings();
        }
        catch (Exception ex)
        {
            FrmMainApp.Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantWriteSQLiteDB") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    /// <summary>
    ///     Reads object names from SQLite
    /// </summary>
    public static void AppStartupReadAppLanguage()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        try
        {
            FrmMainApp._AppLanguage = HelperDataApplicationSettings.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "cbx_Language"
            );

            FrmMainApp.Logger.Trace(message: "AppLanguage is" + FrmMainApp._AppLanguage);
        }
        catch (Exception ex)
        {
            FrmMainApp.Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantLoadSQLiteDB") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    internal static void AppStartupReadCustomCityLogic()
    {
        HelperVariables.DtCustomCityLogic = HelperDataCustomCityAllocationRules.DataReadSQLiteCustomCityAllocationLogic();
        HelperVariables.LstCityNameIsAdminName1.Clear();
        HelperVariables.LstCityNameIsAdminName2.Clear();
        HelperVariables.LstCityNameIsAdminName3.Clear();
        HelperVariables.LstCityNameIsAdminName4.Clear();
        HelperVariables.LstCityNameIsUndefined.Clear();

        foreach (DataRow drCountryCode in HelperVariables.DtCustomCityLogic.Rows)
        {
            string countryCode = drCountryCode[columnName: "CountryCode"]
               .ToString();
            string targetPointName = drCountryCode[columnName: "TargetPointNameCustomCityLogic"]
               .ToString();
            switch (targetPointName)
            {
                case "AdminName1":
                    HelperVariables.LstCityNameIsAdminName1.Add(item: countryCode);
                    break;
                case "AdminName2":
                    HelperVariables.LstCityNameIsAdminName2.Add(item: countryCode);
                    break;
                case "AdminName3":
                    HelperVariables.LstCityNameIsAdminName3.Add(item: countryCode);
                    break;
                case "AdminName4":
                    HelperVariables.LstCityNameIsAdminName4.Add(item: countryCode);
                    break;
                case "Undefined":
                    HelperVariables.LstCityNameIsUndefined.Add(item: countryCode);
                    break;
            }
        }
    }

    /// <summary>
    ///     Reads the value for API-language-use from SQLite.
    /// </summary>
    public static void AppStartupReadAPILanguage()
    {
        FrmMainApp.Logger.Debug(message: "Starting");
        string TryUseGeoNamesLanguage = null;
        try
        {
            TryUseGeoNamesLanguage = HelperDataApplicationSettings.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "rbt_UseGeoNamesLocalLanguage"
            );

            if (TryUseGeoNamesLanguage == "true") // bit derpy but works
            {
                HelperVariables.APILanguageToUse = "local";
            }
            else
            {
                TryUseGeoNamesLanguage = HelperDataApplicationSettings.DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "tpg_Application",
                    settingId: "cbx_TryUseGeoNamesLanguage"
                );
            }
        }
        catch (Exception ex)
        {
            TryUseGeoNamesLanguage = HelperVariables.DefaultEnglishString;
        }

        if (TryUseGeoNamesLanguage != "true")
        {
            IEnumerable<KeyValuePair<string, string>> result = HelperGenericAncillaryListsArrays.GetISO_639_1_Languages()
                                                                                                .Where(predicate: kvp => kvp.Value == TryUseGeoNamesLanguage);

            HelperVariables.APILanguageToUse = result.FirstOrDefault()
                                                     .Key;
        }
    }

    /// <summary>
    ///     Applies default settings
    /// </summary>
    public static void AppStartupApplyDefaults()
    {
        // get some defaults
        FrmMainApp.Logger.Debug(message: "Starting");

        Dictionary<string, string> settingsStringBoolPairsDictionary = new()
        {
            { "UserSettingArcGisApiKey", "tbx_ARCGIS_APIKey" },
            { "UserSettingGeoNamesUserName", "tbx_GeoNames_UserName" },
            { "UserSettingGeoNamesPwd", "tbx_GeoNames_Pwd" },
            { "UserSettingResetMapToZeroOnMissingValue", "ckb_ResetMapToZero" },
            { "UserSettingUseDarkMode", "ckb_UseDarkMode" },
            { "UserSettingUpdatePreReleaseGTN", "ckb_UpdateCheckPreRelease" },
            { "UserSettingOnlyShowFCodePPL", "ckb_PopulatedPlacesOnly" },
            { "UserSettingUseImperial", "ckb_UseImperialNotMetric" }
        };

        Dictionary<string, List<string>> settingsRadioButtonPairsDictionary = new()
        {
            {
                "UserSettingMapColourMode", new List<string>
                {
                    "rbt_MapColourModeNormal",
                    "rbt_MapColourModeDarkInverse",
                    "rbt_MapColourModeDarkPale"
                }
            }
        };

        Dictionary<string, string> settingsRadioButtonReplaceWhatsDictionary = new()
        {
            { "UserSettingMapColourMode", "rbt_MapColourMode" }
        };

        Type helperVariablesTypes = typeof(HelperVariables);
        foreach (FieldInfo fieldInfo in helperVariablesTypes.GetFields(bindingAttr: BindingFlags.Static | BindingFlags.NonPublic))
        {
            try
            {
                if (settingsStringBoolPairsDictionary.ContainsKey(key: fieldInfo.Name))
                {
                    FrmMainApp.Logger.Debug(message: "Now retrieving: " + fieldInfo.Name);
                    settingsStringBoolPairsDictionary.TryGetValue(key: fieldInfo.Name, value: out string fieldInfoSettingID);
                    if (fieldInfo.FieldType == typeof(bool))
                    {
                        // In this code, fieldInfo.SetValue(null, true); is used to set the value of the static field to True.
                        // The first parameter is the object instance for instance fields, for static fields this should be null.
                        // The second parameter is the value to set.
                        fieldInfo.SetValue(obj: null, value: HelperDataApplicationSettings.DataReadCheckBoxSettingTrueOrFalse(
                                               tableName: "settings",
                                               settingTabPage: "tpg_Application",
                                               settingId: fieldInfoSettingID
                                           ));
                    }
                    else if (fieldInfo.FieldType == typeof(string))
                    {
                        fieldInfo.SetValue(obj: null, value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                                               tableName: "settings",
                                               settingTabPage: "tpg_Application",
                                               settingId: fieldInfoSettingID,
                                               returnBlankIfNull: true
                                           ));
                    }
                }
                else if (settingsRadioButtonPairsDictionary.ContainsKey(key: fieldInfo.Name))
                {
                    settingsRadioButtonPairsDictionary.TryGetValue(key: fieldInfo.Name, value: out List<string> optionList);
                    settingsRadioButtonReplaceWhatsDictionary.TryGetValue(key: fieldInfo.Name, value: out string replaceWhat);
                    fieldInfo.SetValue(obj: null, value: HelperDataApplicationSettings.DataReadRadioButtonSettingTrueOrFalse(
                                                                                           tableName: "settings",
                                                                                           settingTabPage: "tpg_Application",
                                                                                           optionList: optionList
                                                                                       )
                                                                                      .Replace(oldValue: replaceWhat, newValue: ""));
                }
            }
            catch (Exception ex)
            {
                FrmMainApp.Logger.Fatal(message: "Error: " + ex.Message);
                MessageBox.Show(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                              messageBoxName: "mbx_FrmMainApp_ErrorCantReadDefaultSQLiteDB") +
                          ex.Message,
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error);
            }
        }

        foreach (SourcesAndAttributes.ElementAttribute attribute in SourcesAndAttributes.TagsToColumnHeaderOrder.Where(
                     predicate: attribute =>
                         !FileListView._cfg_Col_Order_Default.ContainsKey(
                             key: SourcesAndAttributes.GetAttributeName(attribute: attribute))))
        {
            FileListView._cfg_Col_Order_Default.Add(
                key: SourcesAndAttributes.GetAttributeName(attribute: attribute),
                value: SourcesAndAttributes.TagsToColumnHeaderOrder.IndexOf(item: attribute));
        }
    }

    /// <summary>
    ///     Makes sure there is webview2 installed and working
    /// </summary>
    public static void AppStartupCheckWebView2()
    {
        // Check webView2 availability
        FrmMainApp.Logger.Debug(message: "Starting");

        try
        {
            string webView2Version = "";
            webView2Version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            FrmMainApp.Logger.Trace(message: "Check webView2 version is: " + webView2Version);
        }
        catch (Exception ex)
        {
            FrmMainApp.Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantLoadWebView2") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    /// <summary>
    ///     Pulls the settings for overwriting empty toponomy details if req'd
    /// </summary>
    internal static void AppStartupPullOverWriteBlankToponomy()
    {
        bool.TryParse(value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                          tableName: "settings",
                          settingTabPage: "tpg_Application",
                          settingId: "ckb_ReplaceBlankToponyms"), result: out HelperVariables.ToponomyReplace);

        if (HelperVariables.ToponomyReplace)
        {
            string replaceEmpty = HelperDataApplicationSettings.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_ReplaceBlankToponyms");

            if (!string.IsNullOrEmpty(value: replaceEmpty))
            {
                HelperVariables.ToponomyReplaceWithWhat = replaceEmpty;
            }
        }
    }

    /// <summary>
    ///     Pulls data related to user's Settings re how many choices an API pull should offer and what should be the default
    ///     radius
    /// </summary>
    internal static void AppStartupPullToponomyRadiusAndMaxRows()
    {
        string choiceCountValue = HelperDataApplicationSettings.DataReadSQLiteSettings(
                                      tableName: "settings",
                                      settingTabPage: "tpg_Application",
                                      settingId: "nud_ChoiceOfferCount"
                                  ) ??
                                  "1";

        HelperVariables.ToponomyMaxRows = choiceCountValue;

        string radiusValue = HelperDataApplicationSettings.DataReadSQLiteSettings(
                                 tableName: "settings",
                                 settingTabPage: "tpg_Application",
                                 settingId: "nud_ChoiceRadius"
                             ) ??
                             "10";
        HelperVariables.ToponomyRadiusValue = radiusValue;
    }

    /// <summary>
    ///     Loads existing favourites
    /// </summary>
    /// <param name="clearDropDown"></param>
    /// <returns></returns>
    public static DataTable AppStartupLoadFavourites(bool clearDropDown = true)
    {
        FrmMainApp.Logger.Info(message: "Starting");
        FrmMainApp.DtFavourites = HelperDataFavourites.DataReadSQLiteFavourites();
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        if (frmMainAppInstance != null && clearDropDown)
        {
            FrmMainApp.LstFavourites.Clear();
            AutoCompleteStringCollection autoCompleteCustomSource = new();
            frmMainAppInstance.cbx_Favourites.Items.Clear();
            foreach (DataRow drFavourite in FrmMainApp.DtFavourites.Rows)
            {
                string favouriteName = drFavourite[columnName: "favouriteName"]
                   .ToString();
                FrmMainApp.LstFavourites.Add(item: favouriteName);
                autoCompleteCustomSource.Add(value: favouriteName);

                frmMainAppInstance.cbx_Favourites.Items.Add(item: favouriteName);
            }

            frmMainAppInstance.cbx_Favourites.AutoCompleteSource = AutoCompleteSource.CustomSource;
            frmMainAppInstance.cbx_Favourites.AutoCompleteCustomSource = autoCompleteCustomSource;
        }

        return FrmMainApp.DtFavourites;
    }

    /// <summary>
    ///     Loads Custom Rules
    /// </summary>
    /// <returns></returns>
    public static void AppStartupLoadCustomRules()
    {
        HelperVariables.DtCustomRules = HelperDataCustomRules.DataReadSQLiteCustomRules();
    }

    /// <summary>
    ///     Sets the startup folder. Defaults to "MyPictures" if null.
    /// </summary>
    /// <param name="toolStripTextBox"></param>
    public static void AppSetupInitialiseStartupFolder(ToolStripTextBox toolStripTextBox)
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        string startupFolder = "";
        try
        {
            startupFolder = HelperDataApplicationSettings.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_Startup_Folder"
            );
            FrmMainApp.Logger.Trace(message: "Startup Folder is: " + startupFolder);
        }
        catch (Exception ex)
        {
            FrmMainApp.Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_FrmMainApp_ErrorSettingStartupFolder") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
        }

        if (startupFolder == null)
        {
            startupFolder = Environment.GetFolderPath(folder: Environment.SpecialFolder.MyPictures);
            FrmMainApp.Logger.Trace(message: "Startup Folder is null, defaulting to SpecialFolder.MyPictures: " + startupFolder);
        }

        if (startupFolder.EndsWith(value: "\\"))
        {
            toolStripTextBox.Text = startupFolder;
        }
        else
        {
            toolStripTextBox.Text = startupFolder + "\\";
        }
    }
}