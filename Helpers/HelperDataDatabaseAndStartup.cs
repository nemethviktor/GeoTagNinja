using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;

namespace GeoTagNinja.Helpers;

internal static class HelperDataDatabaseAndStartup
{
    private static readonly string DoubleQuote = @"""";

    /// <summary>
    ///     Creates the SQLite DB if it doesn't yet exist
    /// </summary>
    internal static void DataCreateSQLiteDB()
    {
        FrmMainApp.Log.Info(message: "Starting");

        try
        {
            // create folder in Appdata if doesn't exist
            FrmMainApp.Log.Trace(message: $"SettingsDatabaseFilePath is {HelperVariables.SettingsDatabaseFilePath}");
            FileInfo userDataBaseFile = new(fileName: HelperVariables.SettingsDatabaseFilePath);

            if (userDataBaseFile.Exists && userDataBaseFile.Length == 0)
            {
                FrmMainApp.Log.Trace(message: "SettingsDatabaseFilePath exists with 0 byes volume");
                userDataBaseFile.Delete();
                FrmMainApp.Log.Trace(message: "SettingsDatabaseFilePath deleted");
            }

            if (!userDataBaseFile.Exists)
            {
                FrmMainApp.Log.Trace(message: $"Creating {HelperVariables.SettingsDatabaseFilePath}");
                try
                {
                    SQLiteConnection.CreateFile(databaseFileName: Path.Combine(HelperVariables.SettingsDatabaseFilePath));
                    SQLiteConnection sqliteDB = new(connectionString:
                        $@"Data Source={Path.Combine(HelperVariables.SettingsDatabaseFilePath)}; Version=3");
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
                                             Sublocation NTEXT
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
                    _ = sqlCommandStr.ExecuteNonQuery();
                    sqliteDB.Close();
                }
                catch (Exception ex)
                {
                    FrmMainApp.Log.Fatal(message: $"Error: {ex.Message}");
                    _ = MessageBox.Show(text: ex.Message);
                }
            }
            else
            {
                HelperDataFavourites.DataCreateSQLiteFavourites();
                HelperDataCustomRules.DataCreateSQLiteCustomRules();
                HelperDataCustomCityAllocationRules.DataCreateSQLiteCustomCityAllocationLogic();
                DataWriteSQLiteRenameColumn(tableName: "Favourites", columnNameFrom: "locationName",
                    columnNameTo: "favouriteName");
                DataWriteSQLiteRenameColumn(tableName: "Favourites", columnNameFrom: "Sub_location",
                    columnNameTo: "Sublocation");
                DataWriteSQLiteRenameDataInTable(tableName: "customRules", columnName: "TargetPointName",
                    dataFrom: "Sub_location",
                    dataTo: "Sublocation");
            }
        }
        catch (Exception ex)
        {
            FrmMainApp.Log.Fatal(message: $"Error: {ex.Message}");
            _ = MessageBox.Show(text: ex.Message);
        }
    }

    /// <summary>
    ///     Fills the SQLite database with defaults (such as file-type-specific settings)
    /// </summary>
    internal static void DataWriteSQLiteSettingsDefaultSettings()
    {
        FrmMainApp.Log.Info(message: "Starting");

        // note to self. for any keys, use the Control name, not the Variable name.

        string[] booleanTypeApplicationSettingsExtensionSpecificControlNames =
        [
            "ckb_AddXMPSideCar",
            "ckb_OverwriteOriginal",
            "ckb_ProcessOriginalFile",
            "ckb_ResetFileDateToCreated"
        ];

        Dictionary<string, List<string>> booleanTypeSettingsNonExtensionSpecificControlNames = new()
        {
            {
                "tpg_Application", new List<string>
                {
                    "rbt_UseGeoNamesLocalLanguage",
                    "rbt_MapColourModeNormal"
                }
            },
            {
                "tpg_ImportExport_Import", new List<string>
                {
                    // leave this as-is (don't include the other options. this is setting a default value.)
                    "rbt_importOneFile"
                }
            }
        };

        Dictionary<string, Dictionary<string, string>> stringTypeSettingsControlNames =
            new()
            {
                {
                    "tpg_ImportExport_Import", new Dictionary<string, string>
                    {
                        { "nud_GeoMaxIntSecs", "1800" },
                        { "nud_GeoMaxExtSecs", "1800" }
                    }
                }
            };

        List<AppSettingContainer> settingsToWriteTmp = [];

        foreach (string controlName in booleanTypeApplicationSettingsExtensionSpecificControlNames)
        {
            string settingTabPage = "tpg_FileOptions";
            foreach (string ext in HelperGenericAncillaryListsArrays.AllCompatibleExtensions())
            {
                string fileExtension = ext.Split('\t').FirstOrDefault();
                string tmptmpCtrlName = $"{ext.Split('\t').FirstOrDefault()}_"; // 'tis ok as is
                string tmpCtrlName = tmptmpCtrlName + controlName;
                string tmpCtrlGroup = ext.Split('\t').Last().ToLower();
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
                    controlDefaultValue = tmpCtrlGroup.Contains(value: "raw") ||
                        tmpCtrlGroup.Contains(value: "tiff")
                        ? "false"
                        : "true";
                }

                else if (controlName == "ckb_ResetFileDateToCreated")
                {
                    controlDefaultValue = tmpCtrlGroup.Contains(value: "raw") ||
                        tmpCtrlGroup.Contains(value: "tiff")
                        ? "true"
                        : "false";
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
                 booleanTypeSettingsNonExtensionSpecificControlNames.Select(selector: keyValuePair =>
                     keyValuePair.Key))
        {
            _ = booleanTypeSettingsNonExtensionSpecificControlNames.TryGetValue(key: settingTabPage,
                value: out List<string> booleanTypeControlNameList);

            settingsToWriteTmp.AddRange(collection: booleanTypeControlNameList.Select(selector: controlName =>
                new AppSettingContainer
                {
                    TableName = "settings",
                    SettingTabPage = settingTabPage,
                    SettingId = controlName,
                    SettingValue = "true"
                }));
        }

        foreach (string settingTabPage in stringTypeSettingsControlNames.Select(selector: keyValuePair =>
                     keyValuePair.Key))
        {
            _ = stringTypeSettingsControlNames.TryGetValue(key: settingTabPage,
                value: out Dictionary<string, string> booleanTypeControlNameList);

            settingsToWriteTmp.AddRange(collection: booleanTypeControlNameList.Select(selector: controlName =>
                new AppSettingContainer
                {
                    TableName = "settings",
                    SettingTabPage = settingTabPage,
                    SettingId = controlName.Key,
                    SettingValue = controlName.Value
                }));
        }

        // language -> Add "English" as default if there isn't one defined.
        string existingSQLVal = HelperDataApplicationSettings.DataReadSQLiteSettings(
            dataTable: HelperVariables.DtHelperDataApplicationSettings,
            settingTabPage: "tpg_Application",
            settingId: "cbx_Language");
        if (existingSQLVal is "" or
            null)
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
            new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        sqliteDB.Open();

        string sqlCommandStr = $@"
                                SELECT *
                                FROM {tableName};"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        SQLiteDataReader reader = sqlToRun.ExecuteReader();
        DataTable dataTable = new();
        dataTable.Load(reader: reader);
        return dataTable;
    }

    /// <summary>
    ///     This function helps rename columns. Some of the naming logic has changed over time and since users already have
    ///     existing databases those need to be patched to work properly.
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="columnNameFrom"></param>
    /// <param name="columnNameTo"></param>
    /// <returns></returns>
    private static void DataWriteSQLiteRenameColumn(string tableName, string columnNameFrom, string columnNameTo)
    {
        try
        {
            using SQLiteConnection sqliteDB =
                new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
            sqliteDB.Open();

            // Get the schema for the columns in the database.
            DataTable colsTable = sqliteDB.GetSchema(collectionName: "Columns");

            // Query the columns schema using SQL statements to work out if the required columns exist.
            bool locationNameExists =
                colsTable.Select(filterExpression: $"COLUMN_NAME='{columnNameFrom}' AND TABLE_NAME='{tableName}'")
                         .Length != 0;
            if (locationNameExists)
            {
                string sqlCommandStr = $@"
                                ALTER TABLE {tableName}
                                RENAME COLUMN {columnNameFrom} TO {columnNameTo}

                                ;
                                "
                    ;

                SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

                _ = sqlToRun.ExecuteNonQuery();
            }

            sqliteDB.Close();
        }
        catch
        {
            // nothing
        }
    }

    /// <summary>
    ///     Renames values in a table's data
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="columnName"></param>
    /// <param name="dataFrom"></param>
    /// <param name="dataTo"></param>
    private static void DataWriteSQLiteRenameDataInTable(string tableName,
                                                         string columnName,
                                                         string dataFrom,
                                                         string dataTo)
    {
        try
        {
            using SQLiteConnection sqliteDB =
                new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
            sqliteDB.Open();

            // Get the schema for the columns in the database.
            DataTable colsTable = sqliteDB.GetSchema(collectionName: "Columns");

            string sqlCommandStr =
                $"""UPDATE {tableName} SET {columnName} = "{dataTo}" WHERE {columnName} = "{dataFrom}" """;

            SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

            _ = sqlToRun.ExecuteNonQuery();

            sqliteDB.Close();
        }
        catch (Exception ex)
        {
            Debug.Print(message: ex.Message);
        }
    }

    /// <summary>
    ///     Gets the Unit of Measure abbreviation (ie 'ft' or 'm')
    /// </summary>
    /// <returns></returns>
    internal static string GetUnitOfMeasureAbbreviated()
    {
        return ReturnControlText(controlName: HelperVariables.UserSettingUseImperial
                ? "lbl_Feet_Abbr"
                : "lbl_Metres_Abbr"
          , fakeControlType: FakeControlTypes.Label);
    }
}