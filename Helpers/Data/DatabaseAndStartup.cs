using GeoTagNinja.Helpers.FileSystem;
using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static GeoTagNinja.Helpers.UI.HelperControlAndMessageBoxHandling;

namespace GeoTagNinja.Helpers.Data;

internal static class DatabaseAndStartup
{
    private static readonly string DoubleQuote = @"""";

    /// <summary>
    ///     Creates the SQLite DB if it doesn't yet exist, adds whatever tables a database written by an earlier
    ///     version is missing, and runs the naming migrations.
    /// </summary>
    /// <remarks>
    ///     The schema itself lives in <see cref="DatabaseSchema" />, so that this and <see cref="SettingsImport" />
    ///     cannot end up disagreeing about what a table looks like. It used to be spelled out here for a new file and
    ///     a second time in <see cref="Favourites" />, <see cref="CustomRules" /> and
    ///     <see cref="CustomCityAllocationRules" /> for an existing one.
    /// </remarks>
    internal static void DataCreateSQLiteDB()
    {
        FrmMainApp.Log.Info(message: "Starting");

        try
        {
            FrmMainApp.Log.Trace(message: $"SettingsDatabaseFilePath is {HelperVariables.SettingsDatabaseFilePath}");
            FileInfo userDataBaseFile = new(fileName: HelperVariables.SettingsDatabaseFilePath);

            // a zero-byte file is an interrupted create rather than a database, and EnsureSettingsDatabaseExists
            // starts it over, so it doesn't count as pre-existing here either.
            bool userDataBaseFileAlreadyExisted = userDataBaseFile.Exists && userDataBaseFile.Length > 0;
            FrmMainApp.Log.Trace(message: $"SettingsDatabaseFilePath already exists: {userDataBaseFileAlreadyExisted}");

            DatabaseSchema.EnsureSettingsDatabaseExists();

            // this used to run only for a database that already existed, which meant a fresh installation spent its
            // whole first session with an empty city allocation table and picked up the defaults on the next launch.
            CustomCityAllocationRules.DataWriteSQLiteCustomCityAllocationLogicDefaults();

            if (userDataBaseFileAlreadyExisted)
            {
                // some of the naming logic has changed over time; patch up the databases that predate it.
                DataWriteSQLiteRenameColumn(tableName: DatabaseSchema.TableNameFavourites,
                    columnNameFrom: "locationName",
                    columnNameTo: "favouriteName");
                DataWriteSQLiteRenameColumn(tableName: DatabaseSchema.TableNameFavourites,
                    columnNameFrom: "Sub_location",
                    columnNameTo: "Sublocation");
                DataWriteSQLiteRenameDataInTable(tableName: DatabaseSchema.TableNameCustomRules,
                    columnName: "TargetPointName",
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
            foreach (string ext in SupportedFileExtensions.AllCompatibleExtensions())
            {
                string fileExtension = ext.Split('\t').FirstOrDefault();
                string tmptmpCtrlName = $"{ext.Split('\t').FirstOrDefault()}_"; // 'tis ok as is
                string tmpCtrlName = tmptmpCtrlName + controlName;
                string tmpCtrlGroup = ext.Split('\t').Last().ToLower();
                string controlDefaultValue = "false";

                if (controlName == "ckb_AddXMPSideCar")
                {
                    controlDefaultValue = SupportedFileExtensions.FileExtensionsThatUseXMP()
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
        string existingSQLVal = ApplicationSettings.DataReadSQLiteSettings(
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
            ApplicationSettings.DataWriteSQLiteSettings(settingsToWrite: settingsToWrite);
        }
    }

    /// <summary>
    ///     Generic method to read a SQLite table into a DataTable
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    internal static DataTable DataReadSQLiteTable(string tableName)
    {
        using SQLiteConnection SQLiteDB =
            new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        string commandText = $@"
                                SELECT *
                                FROM {tableName};"
            ;

        SQLiteCommand SQLiteCommand = new(commandText: commandText, connection: SQLiteDB);

        SQLiteDataReader reader = SQLiteCommand.ExecuteReader();
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
            using SQLiteConnection SQLiteDB =
                new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
            SQLiteDB.Open();

            // Get the schema for the columns in the database.
            DataTable colsTable = SQLiteDB.GetSchema(collectionName: "Columns");

            // Query the columns schema using commandText statements to work out if the required columns exist.
            bool locationNameExists =
                colsTable.Select(filterExpression: $"COLUMN_NAME='{columnNameFrom}' AND TABLE_NAME='{tableName}'")
                         .Length != 0;
            if (locationNameExists)
            {
                string commandText = $@"
                                ALTER TABLE {tableName}
                                RENAME COLUMN {columnNameFrom} TO {columnNameTo}

                                ;
                                "
                    ;

                SQLiteCommand SQLiteCommand = new(commandText: commandText, connection: SQLiteDB);

                _ = SQLiteCommand.ExecuteNonQuery();
            }

            SQLiteDB.Close();
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
            using SQLiteConnection SQLiteDB =
                new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
            SQLiteDB.Open();

            // Get the schema for the columns in the database.
            DataTable colsTable = SQLiteDB.GetSchema(collectionName: "Columns");

            string commandText =
                $"""UPDATE {tableName} SET {columnName} = "{dataTo}" WHERE {columnName} = "{dataFrom}" """;

            SQLiteCommand SQLiteCommand = new(commandText: commandText, connection: SQLiteDB);

            _ = SQLiteCommand.ExecuteNonQuery();

            SQLiteDB.Close();
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