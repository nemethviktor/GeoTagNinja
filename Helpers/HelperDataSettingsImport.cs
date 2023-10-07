using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace GeoTagNinja.Helpers
{
    internal static class HelperDataSettingsImport
    {
        /// <summary>
        ///     Imports the specified settings from an SQLite database file into the current settings database.
        /// </summary>
        /// <param name="settingsToImportList">
        ///     A list of setting names to import. The setting names should correspond to the names
        ///     of the tables in the SQLite database.
        /// </param>
        /// <param name="importFilePath">The file path of the SQLite database from which to import the settings.</param>
        /// <returns>Returns true if the settings were successfully imported, otherwise an exception is thrown.</returns>
        /// <exception cref="System.Exception">Thrown when the provided file path does not point to a valid SQLite database.</exception>
        /// <remarks>
        ///     This method first checks if the provided file path points to a valid SQLite database. If not, an exception is
        ///     thrown.
        ///     Then, for each setting name in the provided list, it checks if a corresponding table exists in the SQLite database.
        ///     If a table exists, it is copied into the current settings database, replacing any existing table with the same
        ///     name.
        ///     During the import process, a backup of the current settings database is created. If the import process completes
        ///     without errors, the backup is deleted.
        /// </remarks>
        internal static bool DataImportSettings(List<string> settingsToImportList,
                                                string importFilePath)
        {
            // Check if importFilePath is a valid SQLite database
            if (!File.Exists(path: importFilePath) || !IsValidSQLite(filePath: importFilePath))
            {
                throw new Exception(message: "Invalid SQLite database file path.");
            }

            List<string> settingsTablesToBeImportedList = new();

            foreach (string settingName in Enum.GetNames(enumType: typeof(SettingsImportExportOptions)))
            {
                if (settingsToImportList.Contains(item: settingName))
                {
                    settingsTablesToBeImportedList.Add(
                        item: HelperGenericAncillaryListsArrays.GetSettingsImportExportTableName(settingName: settingName));
                }
            }

            // Open connections to both databases
            using (SQLiteConnection importConnection = new(connectionString: $"Data Source={importFilePath};Version=3;"))
            {
                using (SQLiteConnection settingsConnection = new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath};Version=3;"))
                {
                    importConnection.Open();
                    settingsConnection.Open();
                    // Backup the settings database
                    File.Copy(sourceFileName: HelperVariables.SettingsDatabaseFilePath, destFileName: HelperVariables.SettingsDatabaseFilePath + ".bak", overwrite: true);
                    foreach (string tableName in settingsTablesToBeImportedList)
                    {
                        // Check if the table exists in the import database
                        using (SQLiteCommand cmd = new(commandText: $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';", connection: importConnection))
                        {
                            object result = cmd.ExecuteScalar();
                            if (result != null && result.ToString() == tableName)
                            {
                                // Delete the table from the settings database
                                using (SQLiteCommand deleteCmd = new(commandText: $"DROP TABLE IF EXISTS {tableName};", connection: settingsConnection))
                                {
                                    deleteCmd.ExecuteNonQuery();
                                }

                                // Copy the table from the import database to the settings database
                                lock (importConnection)
                                {
                                    using SQLiteCommand copyCmd = new(
                                        commandText:
                                        $"ATTACH DATABASE '{HelperVariables.SettingsDatabaseFilePath}' AS toDb; CREATE TABLE toDb.{tableName} AS SELECT * FROM main.{tableName};",
                                        connection: importConnection);
                                    copyCmd.ExecuteNonQuery();
                                }
                            }

                            using SQLiteCommand detachCmd = new(
                                commandText:
                                $"DETACH DATABASE 'toDb';",
                                connection: importConnection);
                            detachCmd.ExecuteNonQuery();
                        }
                    }

                    // Close connections
                    importConnection.Close();
                    settingsConnection.Close();
                }
            }

            // Delete the backup if no errors occurred
            File.Delete(path: HelperVariables.SettingsDatabaseFilePath + ".bak");
            return true;
        }

        /// <summary>
        ///     Checks if the provided file path points to a valid SQLite database.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>Returns true if the file at the specified path is a valid SQLite database, otherwise false.</returns>
        /// <remarks>
        ///     This method attempts to open a connection to the SQLite database at the specified file path and execute a simple
        ///     query.
        ///     If any SQLite-specific exceptions or any other exceptions are thrown during this process, the method assumes that
        ///     the file is not a valid SQLite database and returns false.
        /// </remarks>

        // ReSharper disable once InconsistentNaming
        private static bool IsValidSQLite(string filePath)
        {
            try
            {
                using (SQLiteConnection connection = new(connectionString: $"Data Source={filePath};Version=3;"))
                {
                    connection.Open();
                    using (SQLiteCommand command = new(commandText: "SELECT 1", connection: connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }

                return true;
            }
            catch (SQLiteException)
            {
                // If any SQLite-specific exceptions are thrown (e.g., file is not a database),
                // we can safely assume that the file is not a valid SQLite database.
                return false;
            }
            catch (Exception)
            {
                // If any other exceptions are thrown, we also assume that the file is not a valid SQLite database.
                return false;
            }
        }
    }
}
