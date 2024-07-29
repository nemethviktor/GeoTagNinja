using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GeoTagNinja.Model;

namespace GeoTagNinja.Helpers;

internal static class HelperDataDatabaseAndStartup
{
    /// <summary>
    ///     Creates the SQLite DB if it doesn't exist yet
    /// </summary>
    internal static void DataCreateSQLiteDB()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        try
        {
            // create folder in Appdata if doesn't exist
            FrmMainApp.Logger.Trace(message: "SettingsDatabaseFilePath is " + HelperVariables.SettingsDatabaseFilePath);
            FileInfo userDataBaseFile = new(fileName: HelperVariables.SettingsDatabaseFilePath);

            if (userDataBaseFile.Exists && userDataBaseFile.Length == 0)
            {
                FrmMainApp.Logger.Trace(message: "SettingsDatabaseFilePath exists with 0 byes volume");
                userDataBaseFile.Delete();
                FrmMainApp.Logger.Trace(message: "SettingsDatabaseFilePath deleted");
            }

            if (!userDataBaseFile.Exists)
            {
                FrmMainApp.Logger.Trace(message: "Creating " + HelperVariables.SettingsDatabaseFilePath);
                try
                {
                    SQLiteConnection.CreateFile(databaseFileName: Path.Combine(HelperVariables.SettingsDatabaseFilePath));
                    SQLiteConnection sqliteDB = new(connectionString: @"Data Source=" + Path.Combine(HelperVariables.SettingsDatabaseFilePath) + "; Version=3");
                    sqliteDB.Open();

                    string sql = """
                                 CREATE TABLE settings(
                                     settingTabPage TEXT(255)    NOT NULL,
                                     settingId TEXT(255)         NOT NULL, 
                                     settingValue NTEXT(2000)    DEFAULT "",
                                     PRIMARY KEY(settingTabPage, settingId)
                                 );
                                 CREATE TABLE appLayout(
                                     settingTabPage TEXT(255)    NOT NULL,
                                     settingId TEXT(255)         NOT NULL, 
                                     settingValue NTEXT(2000)    DEFAULT "",
                                     PRIMARY KEY(settingTabPage, settingId)
                                 );
                                 CREATE TABLE Favourites(
                                             favouriteName NTEXT NOT NULL PRIMARY KEY,
                                             GPSLatitude NTEXT NOT NULL,
                                             GPSLatitudeRef NTEXT NOT NULL,
                                             GPSLongitude NTEXT NOT NULL,
                                             GPSLongitudeRef NTEXT NOT NULL,
                                             GPSAltitude NTEXT,
                                             GPSAltitudeRef NTEXT,
                                             Coordinates NTEXT NOT NULL,
                                             City NTEXT,
                                             CountryCode NTEXT,
                                             Country NTEXT,
                                             State NTEXT,
                                             Sub_location NTEXT
                                             )
                                 ;
                                 CREATE TABLE customRules(
                                             ruleId INTEGER PRIMARY KEY AUTOINCREMENT,
                                             CountryCode NTEXT NOT NULL,
                                             DataPointName NTEXT NOT NULL,
                                             DataPointConditionType NTEXT NOT NULL,
                                             DataPointConditionValue NTEXT NOT NULL,
                                             TargetPointName NTEXT NOT NULL,
                                             TargetPointOutcome NTEXT NOT NULL,
                                             TargetPointOutcomeCustom NTEXT
                                             )
                                 ;
                                 CREATE TABLE IF NOT EXISTS customCityAllocationLogic(
                                             CountryCode TEXT(3) NOT NULL,
                                             TargetPointNameCustomCityLogic TEXT(100) NOT NULL,
                                             PRIMARY KEY(CountryCode, TargetPointNameCustomCityLogic)
                                             )
                                 ;
                                 """;
                    SQLiteCommand sqlCommandStr = new(commandText: sql, connection: sqliteDB);
                    sqlCommandStr.ExecuteNonQuery();
                    sqliteDB.Close();
                }
                catch (Exception ex)
                {
                    FrmMainApp.Logger.Fatal(message: "Error: " + ex.Message);
                    MessageBox.Show(text: ex.Message);
                }
            }
            else
            {
                HelperDataFavourites.DataCreateSQLiteFavourites();
                HelperDataCustomRules.DataCreateSQLiteCustomRules();
                HelperDataCustomCityAllocationRules.DataCreateSQLiteCustomCityAllocationLogic();
                HelperDataFavourites.DataWriteSQLiteRenameFavouritesLocationNameCol();
            }
        }
        catch (Exception ex)
        {
            FrmMainApp.Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: ex.Message);
        }
    }

    /// <summary>
    ///     Fills the SQLite database with defaults (such as file-type-specific settings)
    /// </summary>
    internal static void DataWriteSQLiteSettingsDefaultSettings()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        string[] extensionSpecificControlNamesToAdd =
        {
            "ckb_AddXMPSideCar",
            "ckb_OverwriteOriginal",
            "ckb_ProcessOriginalFile",
            "ckb_ResetFileDateToCreated"
        };

        Dictionary<string, List<string>> notExtensionSpecificControlNamesToAdd = new()
        {
            {
                "tpg_Application", new List<string>
                {
                    "rbt_UseGeoNamesLocalLanguage",
                    "rbt_MapColourModeNormal"
                }
            }
        };


        // extension-specific
        List<AppSettingContainer> settingsToWriteTmp = new();

        foreach (string controlName in extensionSpecificControlNamesToAdd)
        {
            string settingTabPage = "tpg_FileOptions";
            foreach (string ext in HelperGenericAncillaryListsArrays.AllCompatibleExtensions())
            {
                string fileExtension = ext.Split('\t')
                                          .FirstOrDefault();
                string tmptmpCtrlName = ext.Split('\t')
                                           .FirstOrDefault() +
                                        '_'; // 'tis ok as is
                string tmpCtrlName = tmptmpCtrlName + controlName;
                string tmpCtrlGroup = ext.Split('\t')
                                         .Last()
                                         .ToLower();
                string controlDefaultValue = "false";

                if (controlName == "ckb_AddXMPSideCar")
                {
                    controlDefaultValue = HelperGenericAncillaryListsArrays
                                         .FileExtensionsThatUseXMP()
                                         .Contains(value: fileExtension)
                        ? "true"
                        : "false";
                }
                else if (controlName == "ckb_ProcessOriginalFile")
                {
                    if (tmpCtrlGroup.Contains(value: "raw") ||
                        tmpCtrlGroup.Contains(value: "tiff"))
                    {
                        controlDefaultValue = "false";
                    }
                    else
                    {
                        controlDefaultValue = "true";
                    }
                }

                else if (controlName == "ckb_ResetFileDateToCreated")
                {
                    if (tmpCtrlGroup.Contains(value: "raw") ||
                        tmpCtrlGroup.Contains(value: "tiff"))
                    {
                        controlDefaultValue = "true";
                    }
                    else
                    {
                        controlDefaultValue = "false";
                    }
                }

                else if (controlName == "ckb_OverwriteOriginal")
                {
                    controlDefaultValue = "true";
                }

                settingsToWriteTmp.Add(item: new AppSettingContainer
                {
                    TableName = "settings",
                    SettingTabPage = settingTabPage,
                    SettingId = tmpCtrlName,
                    SettingValue = controlDefaultValue
                });
            }
        }

        foreach (string settingTabPage in
                 notExtensionSpecificControlNamesToAdd.Select(selector: keyValuePair => keyValuePair.Key))
        {
            notExtensionSpecificControlNamesToAdd.TryGetValue(key: settingTabPage,
                value: out List<string> controlNameList);

            settingsToWriteTmp.AddRange(collection: controlNameList.Select(selector: controlName =>
                new AppSettingContainer
                {
                    TableName = "settings", SettingTabPage = settingTabPage, SettingId = controlName,
                    SettingValue = "true"
                }));
        }


        // language -> Add "English" as default if there isn't one defined.
        string existingSQLVal = HelperDataApplicationSettings.DataReadSQLiteSettings(
            dataTable: HelperVariables.DtHelperDataApplicationSettings,
            settingTabPage: "tpg_Application",
            settingId: "cbx_Language");
        if (existingSQLVal == "" ||
            existingSQLVal is null)
        {
            settingsToWriteTmp.Add(item: new AppSettingContainer
            {
                TableName = "settings",
                SettingTabPage = "tpg_Application",
                SettingId = "cbx_Language",
                SettingValue = "English"
            });
        }

        // just make sure we don't overwrite existing data with what's supposed to be a "default".
        List<AppSettingContainer> settingsToWrite = (from appSettingContainerTmp in settingsToWriteTmp
            let contains = HelperVariables.DtHelperDataApplicationSettings.AsEnumerable().Any(predicate: row =>
                row[columnName: "settingTabPage"].ToString() == appSettingContainerTmp.SettingTabPage &&
                row[columnName: "settingId"].ToString() == appSettingContainerTmp.SettingId)
            where !contains
            select appSettingContainerTmp).ToList();

        if (settingsToWrite.Count > 0)
        {
            HelperDataApplicationSettings.DataWriteSQLiteSettings(settingsToWrite: settingsToWrite);
        }
    }


    /// <summary>
    ///     Generic method to read a SQLite table into a DataTable
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    internal static DataTable DataReadSQLiteTable(string tableName)
    {
        using SQLiteConnection sqliteDB =
            new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                SELECT *
                                FROM " + tableName + ";"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        SQLiteDataReader reader = sqlToRun.ExecuteReader();
        DataTable dataTable = new();
        dataTable.Load(reader: reader);
        return dataTable;
    }
}