using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using GeoTagNinja.View.ListView;
using Microsoft.Web.WebView2.Core;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;

namespace GeoTagNinja.Helpers;

internal static class HelperGenericAppStartup
{
    /// <summary>
    ///     Creates the database sqlite file
    /// </summary>
    public static Task AppStartupCreateDatabaseFile()
    {
        FrmMainApp.Log.Info(message: "Starting");
        // load all settings
        try
        {
            FrmMainApp.Log.Debug(message: "applicationDataGeoTagNinjaFolder");
            string applicationDataGeoTagNinjaFolder = Path.Combine(path1: Environment.GetFolderPath(folder: Environment.SpecialFolder.ApplicationData), path2: "GeoTagNinja");
            Directory.CreateDirectory(path: applicationDataGeoTagNinjaFolder);
            HelperDataDatabaseAndStartup.DataCreateSQLiteDB();
        }
        catch (Exception ex)
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorCantCreateSQLiteDB", captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK, extraMessage: ex.Message);

            Application.Exit();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Writes defaults to sqlite if they don't exist
    /// </summary>
    public static Task AppStartupWriteDefaultSettings()
    {
        FrmMainApp.Log.Info(message: "Starting");

        // write settings for combobox defaults etc
        try
        {
            AppStartupReadSQLiteTables();
            HelperDataDatabaseAndStartup.DataWriteSQLiteSettingsDefaultSettings();
        }
        catch (Exception ex)
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorCantWriteSQLiteDB", captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK, extraMessage: ex.Message);

            Application.Exit();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     This fills the existing SQL data into DataTables in HelperVariables
    /// </summary>
    /// <returns></returns>
    public static Task AppStartupReadSQLiteTables()
    {
        try
        {
            HelperVariables.DtHelperDataApplicationSettings =
                HelperDataDatabaseAndStartup.DataReadSQLiteTable(tableName: "settings");
            HelperVariables.DtHelperDataApplicationSettingsPreQueue =
                HelperVariables.DtHelperDataApplicationSettings.Clone();
            HelperVariables.DtHelperDataApplicationLayout =
                HelperDataDatabaseAndStartup.DataReadSQLiteTable(tableName: "appLayout");
        }
        catch
        {
            //
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Reads setting for the app language selection from SQLite
    /// </summary>
    public static Task AppStartupReadAppLanguage()
    {
        FrmMainApp.Log.Info(message: "Starting");

        try
        {
            string lang = HelperDataApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: "tpg_Application",
                settingId: "cbx_Language") ?? "en"; // default to "en".

            FrmMainApp.Log.Info(message: $"AppLanguage lang is{lang}");

            FrmMainApp.AppLanguage = lang switch
            {
                // This is rather legacy poking. Basically if there is a remnant of the old database data this will convert it to "new".
                // No need to add any further languages here.
                "English" => "en",
                "French" => "fr",

                _ => lang
            };

            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(name: FrmMainApp.AppLanguage);

            Thread.CurrentThread.CurrentUICulture = new CultureInfo(name: cultureInfo.ToString());

            FrmMainApp.Log.Info(message: $"AppLanguage is{FrmMainApp.AppLanguage}");
        }
        catch (Exception ex)
        {
            FrmMainApp.Log.Fatal(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorCantLoadSQLiteDB", captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK, extraMessage: ex.Message);

            Application.Exit();
        }

        return Task.CompletedTask;
    }

    internal static Task AppStartupReadCustomCityLogic()
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

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Reads the value for API-language-use from SQLite.
    /// </summary>
    public static Task AppStartupReadAPILanguage()
    {
        FrmMainApp.Log.Info(message: "Starting");
        string TryUseGeoNamesLanguage = null;
        try
        {
            TryUseGeoNamesLanguage = HelperDataApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings,
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
                    dataTable: HelperVariables.DtHelperDataApplicationSettings,
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
                                                     .Key ?? TryUseGeoNamesLanguage; // leave as-is if fail
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Applies default settings. The way this works is that we assign to each (string) key the name of the relevant
    ///     Control, which then gets eventually read and written in a variety of places.
    ///     The way this (hopefully) works is that for each Key there should be a variable equivalent in HelperVariables and
    ///     each Value (as in KVP) there should be an instruction someplace to save a "value" (non-capital V) in SQLite.
    ///     Supposedly.
    /// </summary>
    /// <param name="settingTabPage">Which tabPage to affect</param>
    /// <param name="actuallyRunningAtStartup">Whether we're really running at the startup. If not, certain parts are skipped</param>
    public static Task AppStartupApplyDefaults(string settingTabPage,
                                               bool actuallyRunningAtStartup)
    {
        FrmMainApp.Log.Info(message: "Starting");

        // Define setting maps for different types
        Dictionary<string, Dictionary<string, string>> stringSettings = new()
        {
            [key: "tpg_Application"] = new Dictionary<string, string>
            {
                { "UserSettingArcGisApiKey", "tbx_ARCGIS_APIKey" },
                { "UserSettingGeoNamesUserName", "tbx_GeoNames_UserName" },
                { "UserSettingGeoNamesPwd", "tbx_GeoNames_Pwd" }
            },
            [key: "tpg_ImportExport_Import"] = new Dictionary<string, string>
            {
                { "UserSettingImportGPXTimeZoneToUse", "cbx_ImportUseTimeZone" }
            }
        };

        Dictionary<string, Dictionary<string, string>> intSettings = new()
        {
            [key: "tpg_ImportExport_Import"] = new Dictionary<string, string>
            {
                { "UserSettingImportGPXMaxInterpolation", "nud_GeoMaxIntSecs" },
                { "UserSettingImportGPXMaxExtrapolation", "nud_GeoMaxExtSecs" }
            }
        };

        Dictionary<string, Dictionary<string, string>> boolSettings = new()
        {
            [key: "tpg_Application"] = new Dictionary<string, string>
            {
                { "UserSettingResetMapToZeroOnMissingValue", "ckb_ResetMapToZero" },
                { "UserSettingUseDarkMode", "ckb_UseDarkMode" },
                { "UserSettingUpdatePreReleaseGTN", "ckb_UpdateCheckPreRelease" },
                { "UserSettingOnlyShowFCodePPL", "ckb_PopulatedPlacesOnly" },
                { "UserSettingUseImperial", "ckb_UseImperialNotMetric" },
                { "UserSettingShowThumbnails", "ckb_ShowThumbnails" }
            },
            [key: "tpg_ImportExport_Import"] = new Dictionary<string, string>
            {
                { "UserSettingImportGPXUseParticularTimeZone", "ckb_UseTimeZone" },
                { "UserSettingImportGPXUseDST", "ckb_UseDST" }
            }
        };

        Dictionary<string, Dictionary<string, List<string>>> radioSettings = new()
        {
            [key: "tpg_Application"] = new Dictionary<string, List<string>>
            {
                [key: "UserSettingMapColourMode"] = new()
                {
                    "rbt_MapColourModeNormal",
                    "rbt_MapColourModeDarkInverse",
                    "rbt_MapColourModeDarkPale"
                }
            },
            [key: "tpg_ImportExport_Import"] = new Dictionary<string, List<string>>
            {
                [key: "UserSettingImportGPXImportSource"] = new()
                {
                    "rbt_importOneFile",
                    "rbt_importFromCurrentFolder",
                    "rbt_importFromAnotherFolder"
                }
            }
        };

        Type staticType = typeof(HelperVariables);
        FieldInfo[] fields = staticType.GetFields(bindingAttr: BindingFlags.Static | BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            FrmMainApp.Log.Debug(message: $"Now retrieving: {field.Name}");

            TrySetStringValue(dict: stringSettings, tab: settingTabPage, field: field);
            TrySetBoolValue(dict: boolSettings, tab: settingTabPage, field: field);
            TrySetIntValue(dict: intSettings, tab: settingTabPage, field: field);
            TrySetRadioValue(dict: radioSettings, tab: settingTabPage, field: field);
        }

        if (actuallyRunningAtStartup)
        {
            ApplyDefaultColumnOrders();
            HelperVariables.UOMAbbreviated = HelperDataDatabaseAndStartup.GetUnitOfMeasureAbbreviated();
        }

        return Task.CompletedTask;
    }

    private static void TrySetStringValue(Dictionary<string, Dictionary<string, string>> dict,
                                          string tab,
                                          FieldInfo field)
    {
        if (dict.TryGetValue(key: tab, value: out Dictionary<string, string> map) &&
            map.TryGetValue(key: field.Name, value: out string controlId))
        {
            if (field.FieldType == typeof(string))
            {
                field.SetValue(obj: null, value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                    dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: tab,
                    settingId: controlId, returnBlankIfNull: true));
            }
            else
            {
                throw new ArgumentException(message: $"{field.Name} is not string.");
            }
        }
    }

    private static void TrySetBoolValue(Dictionary<string, Dictionary<string, string>> dict,
                                        string tab,
                                        FieldInfo field)
    {
        if (dict.TryGetValue(key: tab, value: out Dictionary<string, string> map) &&
            map.TryGetValue(key: field.Name, value: out string controlId))
        {
            if (field.FieldType == typeof(bool))
            {
                field.SetValue(obj: null, value: HelperDataApplicationSettings.DataReadCheckBoxSettingTrueOrFalse(
                    dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: tab,
                    settingId: controlId));
            }
            else
            {
                throw new ArgumentException(message: $"{field.Name} is not bool.");
            }
        }
    }

    private static void TrySetIntValue(Dictionary<string, Dictionary<string, string>> dict,
                                       string tab,
                                       FieldInfo field)
    {
        if (dict.TryGetValue(key: tab, value: out Dictionary<string, string> map) &&
            map.TryGetValue(key: field.Name, value: out string controlId))
        {
            if (field.FieldType == typeof(int))
            {
                field.SetValue(obj: null, value: HelperDataApplicationSettings.DataReadIntSetting(
                    dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: tab,
                    settingId: controlId));
            }
            else
            {
                throw new ArgumentException(message: $"{field.Name} is not int.");
            }
        }
    }

    private static void TrySetRadioValue(Dictionary<string, Dictionary<string, List<string>>> dict,
                                         string tab,
                                         FieldInfo field)
    {
        if (dict.TryGetValue(key: tab, value: out Dictionary<string, List<string>> map) &&
            map.TryGetValue(key: field.Name, value: out List<string> optionList))
        {
            string selected = HelperDataApplicationSettings.DataReadRadioButtonSettingTrueOrFalse(
                dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: tab,
                optionList: optionList);

            foreach (string option in optionList)
            {
                if (!string.IsNullOrWhiteSpace(value: selected))
                {
                    string value = selected.Replace(oldValue: option, newValue: "");
                    if (!string.IsNullOrWhiteSpace(value: value))
                    {
                        field.SetValue(obj: null, value: value);
                        return;
                    }
                }
            }

            field.SetValue(obj: null, value: selected); // fallback
        }
    }

    private static void ApplyDefaultColumnOrders()
    {
        IEnumerable<SourcesAndAttributes.ElementAttribute> attributes = Enum
                                                                       .GetValues(
                                                                            enumType: typeof(SourcesAndAttributes.ElementAttribute))
                                                                       .Cast<SourcesAndAttributes.ElementAttribute>()
                                                                       .Where(predicate: attr =>
                                                                            SourcesAndAttributes
                                                                               .GetElementAttributesOrderID(
                                                                                    attributeToFind: attr) > 0);

        foreach (SourcesAndAttributes.ElementAttribute attr in attributes)
        {
            string key = SourcesAndAttributes.GetElementAttributesName(attributeToFind: attr);
            if (!FileListView._cfg_Col_Order_Default.ContainsKey(key: key))
            {
                FileListView._cfg_Col_Order_Default.Add(key: key,
                    value: SourcesAndAttributes.GetElementAttributesOrderID(attributeToFind: attr));
            }
        }
    }


    /// <summary>
    ///     Makes sure there is webview2 installed and working
    /// </summary>
    public static Task AppStartupCheckWebView2()
    {
        // Check webView2 availability
        FrmMainApp.Log.Info(message: "Starting");

        try
        {
            string webView2Version = "";
            webView2Version =
                CoreWebView2Environment.GetAvailableBrowserVersionString(
                    browserExecutableFolder: null);
            FrmMainApp.Log.Trace(message: $"Check webView2 version is: {webView2Version}");
        }
        catch (Exception ex)
        {
            FrmMainApp.Log.Fatal(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorCantLoadWebView2", captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK, extraMessage: ex.Message);


            Application.Exit();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Pulls the settings for overwriting empty toponomy details if req'd
    /// </summary>
    internal static void AppStartupGetOverwriteBlankToponomy()
    {
        bool.TryParse(value: HelperDataApplicationSettings.DataReadSQLiteSettings(
            dataTable: HelperVariables.DtHelperDataApplicationSettings,
            settingTabPage: "tpg_Application",
            settingId: "ckb_ReplaceBlankToponyms"), result: out HelperVariables.ToponomyReplace);

        if (HelperVariables.ToponomyReplace)
        {
            string replaceEmpty = HelperDataApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings,
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
    internal static void AppStartupGetToponomyRadiusAndMaxRows()
    {
        string choiceCountValue = HelperDataApplicationSettings.DataReadSQLiteSettings(
                                      dataTable: HelperVariables.DtHelperDataApplicationSettings,
                                      settingTabPage: "tpg_Application",
                                      settingId: "nud_ChoiceOfferCount"
                                  ) ??
                                  "1";

        HelperVariables.ToponyMaxRowsChoiceOfferCount = choiceCountValue;

        string radiusValue = HelperDataApplicationSettings.DataReadSQLiteSettings(
                                 dataTable: HelperVariables.DtHelperDataApplicationSettings,
                                 settingTabPage: "tpg_Application",
                                 settingId: "nud_ChoiceRadius"
                             ) ??
                             "10";
        HelperVariables.ToponomyRadiusValue = radiusValue;
    }

    /// <summary>
    ///     Loads existing Favourites from SQLite into a Hashset
    /// </summary>
    /// <param name="clearDropDown"></param>
    /// <returns></returns>
    public static void AppStartupLoadFavourites(bool clearDropDown = true)
    {
        FrmMainApp.Log.Info(message: "Starting");

        FrmMainApp.Favourites.Clear();
        HelperDataFavourites.DataReadSQLiteFavourites();

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        frmMainAppInstance.ClearReloadFavouritesDropDownValues();
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
    ///     Accepts value from the -f parameter upon launch.
    /// </summary>
    /// <param name="toolStripTextBox"></param>
    public static void AppSetupInitialiseStartupFolder(ToolStripTextBox toolStripTextBox)
    {
        FrmMainApp.Log.Info(message: "Starting");

        string startupFolder = string.Empty;
        if (!string.IsNullOrEmpty(value: Program.FolderToLaunchIn))
        {
            if (Directory.Exists(path: Program.FolderToLaunchIn))
            {
                startupFolder = Program.FolderToLaunchIn;
            }
        }
        else
        {
            try
            {
                startupFolder = HelperDataApplicationSettings.DataReadSQLiteSettings(
                    dataTable: HelperVariables.DtHelperDataApplicationSettings,
                    settingTabPage: "tpg_Application",
                    settingId: "tbx_Startup_Folder"
                );
                FrmMainApp.Log.Trace(message: $"Startup Folder is: {startupFolder}");
            }
            catch (Exception ex)
            {
                FrmMainApp.Log.Fatal(message: $"Error: {ex.Message}");
                HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                    controlName: "mbx_FrmMainApp_ErrorSettingStartupFolder", captionType: MessageBoxCaption.Error,
                    buttons: MessageBoxButtons.OK, extraMessage: ex.Message);
            }
        }

        if (string.IsNullOrWhiteSpace(value: startupFolder))
        {
            startupFolder = Environment.GetFolderPath(folder: Environment.SpecialFolder.MyPictures);
            FrmMainApp.Log.Trace(message:
                $"Startup Folder is null, defaulting to SpecialFolder.MyPictures: {startupFolder}");
        }

        toolStripTextBox.Text = startupFolder.EndsWith(value: "\\") ? startupFolder : $"{startupFolder}\\";
    }
}