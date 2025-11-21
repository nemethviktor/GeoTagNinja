using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers;

internal static class HelperDataSettingsExport
{
    /// <summary>
    ///     Exports the specified settings to a SQLite database file.
    /// </summary>
    /// <param name="settingsToExportList">A list of settings to be exported. Each setting is represented by a string.</param>
    /// <param name="exportFilePath">The path to the SQLite database file where the settings will be exported.</param>
    /// <remarks>
    ///     This method assumes that the exportFilePath already exists. It loops through each SettingsImportExportOptions and
    ///     checks if it is included in the settingsToExportList. If it is, the corresponding table name is added to a list of
    ///     tables to be kept. All other tables in the SQLite database are deleted.
    ///     After the export operation, a message box is displayed to inform the user that the operation is complete.
    /// </remarks>
    internal static void DataExportSettings(List<string> settingsToExportList,
                                            string exportFilePath)
    {
        // exportFilePath must exist already because it's been called in a way that this is ensured
        // so we loop through foreach (SettingsImportExportOptions) and see if everyone's there

        List<string> settingsTablesToBeKeptList = [];

        foreach (string settingName in Enum.GetNames(enumType: typeof(SettingsImportExportOptions)))
        {
            if (settingsToExportList.Contains(item: settingName))
            {
                settingsTablesToBeKeptList.Add(
                    item: HelperGenericAncillaryListsArrays.GetSettingsImportExportTableName(settingName: settingName));
            }
        }

        KeepSQLiteTables(tablesToKeep: settingsTablesToBeKeptList, exportFilePath: exportFilePath);

        // finally
        HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(controlName: "mbx_GenericDone",
            captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Information,
            buttons: MessageBoxButtons.OK);
        return;

        // ReSharper disable once InconsistentNaming
        static void KeepSQLiteTables(List<string> tablesToKeep,
                                     string exportFilePath)
        {
            List<string> systemTables = ["sqlite_sequence"];
            List<string> tablesToDelete = [];
            using (SQLiteConnection connection = new(connectionString: $"Data Source={exportFilePath};"))
            {
                connection.Open();
                using SQLiteCommand command = new(commandText: "SELECT name FROM sqlite_master WHERE type='table'", connection: connection);
                using SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string tableName = reader.GetString(i: 0);
                    if (!tablesToKeep.Contains(item: tableName) && !systemTables.Contains(item: tableName))
                    {
                        tablesToDelete.Add(item: tableName);
                    }
                }
            }

            foreach (string tableName in tablesToDelete)
            {
                DeleteSQLiteTable(tableName: tableName, exportFilePath: exportFilePath);
            }
        }
    }

    /// <summary>
    ///     Deletes a specified SQLite table from a given SQLite database file.
    /// </summary>
    /// <param name="tableName">The name of the SQLite table to be deleted.</param>
    /// <param name="exportFilePath">The file path of the SQLite database file where the table is located.</param>
    private static void DeleteSQLiteTable(string tableName,
                                          string exportFilePath)
    {
        using SQLiteConnection connection = new(connectionString: $"Data Source={exportFilePath};");
        connection.Open();
        using SQLiteCommand command = new(connection: connection);
        command.CommandText = $"DROP TABLE IF EXISTS {tableName};";
        _ = command.ExecuteNonQuery();
    }
}