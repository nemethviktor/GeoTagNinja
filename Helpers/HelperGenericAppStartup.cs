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
    ///     Reads object names from SQLite
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
    ///     Applies default settings
    /// </summary>
    public static Task AppStartupApplyDefaults()
    {
        // get some defaults
        FrmMainApp.Log.Info(message: "Starting");

        Dictionary<string, string> settingsStringBoolPairsDictionary = new()
        {
            { "UserSettingArcGisApiKey", "tbx_ARCGIS_APIKey" },
            { "UserSettingGeoNamesUserName", "tbx_GeoNames_UserName" },
            { "UserSettingGeoNamesPwd", "tbx_GeoNames_Pwd" },
            { "UserSettingResetMapToZeroOnMissingValue", "ckb_ResetMapToZero" },
            { "UserSettingUseDarkMode", "ckb_UseDarkMode" },
            { "UserSettingUpdatePreReleaseGTN", "ckb_UpdateCheckPreRelease" },
            { "UserSettingOnlyShowFCodePPL", "ckb_PopulatedPlacesOnly" },
            { "UserSettingUseImperial", "ckb_UseImperialNotMetric" },
            { "UserSettingShowThumbnails", "ckb_ShowThumbnails" }
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
                    FrmMainApp.Log.Debug(message: $"Now retrieving: {fieldInfo.Name}");
                    settingsStringBoolPairsDictionary.TryGetValue(key: fieldInfo.Name, value: out string fieldInfoSettingID);
                    if (fieldInfo.FieldType == typeof(bool))
                    {
                        // In this code, fieldInfo.SetValue(null, true); is used to set the value of the static field to True.
                        // The first parameter is the object instance for instance fields, for static fields this should be null.
                        // The second parameter is the value to set.
                        fieldInfo.SetValue(obj: null,
                            value: HelperDataApplicationSettings.DataReadCheckBoxSettingTrueOrFalse(
                                dataTable: HelperVariables.DtHelperDataApplicationSettings,
                                settingTabPage: "tpg_Application",
                                settingId: fieldInfoSettingID
                            ));
                    }
                    else if (fieldInfo.FieldType == typeof(string))
                    {
                        fieldInfo.SetValue(obj: null, value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                            dataTable: HelperVariables.DtHelperDataApplicationSettings,
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
                    fieldInfo.SetValue(obj: null, value: HelperDataApplicationSettings
                                                        .DataReadRadioButtonSettingTrueOrFalse(
                                                             dataTable: HelperVariables
                                                                .DtHelperDataApplicationSettings,
                                                             settingTabPage: "tpg_Application",
                                                             optionList: optionList
                                                         )
                                                        .Replace(oldValue: replaceWhat, newValue: ""));
                }
            }
            catch (Exception ex)
            {
                FrmMainApp.Log.Fatal(message: $"Error: {ex.Message}");
                HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                    controlName: "mbx_FrmMainApp_ErrorCantReadDefaultSQLiteDB", captionType: MessageBoxCaption.Error,
                    buttons: MessageBoxButtons.OK, extraMessage: ex.Message);
            }
        }

        List<SourcesAndAttributes.ElementAttribute> attributesWithValidOrderIDs = Enum
                                                            .GetValues(enumType: typeof(SourcesAndAttributes.ElementAttribute))
                                                            .Cast<SourcesAndAttributes.ElementAttribute>()
                                                            .Where(predicate: attribute =>
                                                                 SourcesAndAttributes.GetElementAttributesOrderID(
                                                                     attributeToFind: attribute) >
                                                                 0)
                                                            .ToList();

        foreach (SourcesAndAttributes.ElementAttribute attribute in
                 attributesWithValidOrderIDs.Where(
                     predicate: attribute =>
                         !FileListView._cfg_Col_Order_Default.ContainsKey(
                             key: SourcesAndAttributes.GetElementAttributesName(
                                 attributeToFind: attribute))))
        {
            FileListView._cfg_Col_Order_Default.Add(
                key: SourcesAndAttributes.GetElementAttributesName(
                    attributeToFind: attribute),
                value: SourcesAndAttributes.GetElementAttributesOrderID(
                    attributeToFind: attribute));
        }

        HelperVariables.UOMAbbreviated = HelperDataDatabaseAndStartup.GetUnitOfMeasureAbbreviated();
        return Task.CompletedTask;
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