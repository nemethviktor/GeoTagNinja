using GeoTagNinja.View.Forms;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace GeoTagNinja.Helpers.Data;

internal static class SettingsImport
{
    /// <summary>
    ///     The alias the file being imported from is attached under. Not a real database name - it only has to be
    ///     something that can't collide with <c>main</c> or <c>temp</c>.
    /// </summary>
    private const string ImportSchemaName = "fromDb";

    /// <summary>
    ///     The schema name of the settings database itself once the import file has been attached to the connection.
    /// </summary>
    private const string SettingsSchemaName = "main";

    /// <summary>
    ///     Imports the specified settings from an SQLite database file into the current settings database.
    /// </summary>
    /// <param name="settingsToImportList">
    ///     A list of setting names to import. The setting names should correspond to the members of
    ///     <see cref="SettingsImportExportOptions" />.
    /// </param>
    /// <param name="importFilePath">The file path of the SQLite database from which to import the settings.</param>
    /// <returns>True if something was imported, false if the selection resolved to no tables at all.</returns>
    /// <exception cref="System.Exception">Thrown when the provided file path does not point to a valid SQLite database.</exception>
    /// <remarks>
    ///     <para>
    ///         Each selected table is dropped from the settings database, re-created from
    ///         <see cref="DatabaseSchema" /> and then filled from the import file. Re-creating it rather than copying
    ///         the table over wholesale is what keeps the imported database in the shape the rest of the app expects:
    ///         the previous implementation used <c>CREATE TABLE ... AS SELECT</c>, which copies data and column names
    ///         but no primary key, no <c>NOT NULL</c> and no <c>AUTOINCREMENT</c>. A <c>customRules</c> table imported
    ///         that way lost <c>ruleId INTEGER PRIMARY KEY AUTOINCREMENT</c>, and <see cref="SQLiteCommandBuilder" />
    ///         in <see cref="CustomRules.DataWriteSQLiteCustomRules" /> cannot generate an <c>UPDATE</c> against a
    ///         table with no key - so editing custom rules threw from then on.
    ///     </para>
    ///     <para>
    ///         Only the columns the two schemas have in common are copied, so a settings file exported by an older
    ///         version of the app still imports; the columns it doesn't know about keep their defaults.
    ///     </para>
    ///     <para>
    ///         The whole thing runs in one transaction, on top of a file-level backup taken beforehand. If anything
    ///         fails the transaction rolls back, the backup is put back in place and the exception is rethrown, so the
    ///         user is never left with a half-imported database. The backup is deleted once the import has succeeded.
    ///     </para>
    /// </remarks>
    internal static bool DataImportSettings(List<string> settingsToImportList,
                                            string importFilePath)
    {
        FrmMainApp.Log.Info(message: "Starting");

        // Check if importFilePath is a valid SQLite database
        if (!File.Exists(path: importFilePath) || !IsValidSQLite(filePath: importFilePath))
        {
            throw new Exception(message: "Invalid SQLite database file path.");
        }

        List<string> settingsTablesToBeImportedList = [];

        foreach (string settingName in Enum.GetNames(enumType: typeof(SettingsImportExportOptions)))
        {
            if (settingsToImportList.Contains(item: settingName))
            {
                string tableName =
                    SettingsImportExportTables.GetSettingsImportExportTableName(settingName: settingName);

                // an enum member with no table behind it is a bug rather than a user error, but there is no point
                // taking the app down over it - the remaining tables can still be imported.
                if (tableName is null || !DatabaseSchema.IsKnownTable(tableName: tableName))
                {
                    FrmMainApp.Log.Error(
                        message: $"'{settingName}' does not map to a table of the settings database - skipping it.");
                    continue;
                }

                settingsTablesToBeImportedList.Add(item: tableName);
            }
        }

        if (settingsTablesToBeImportedList.Count == 0)
        {
            FrmMainApp.Log.Info(message: "Nothing was selected for import.");
            return false;
        }

        // The settings database is created at startup, but it can be missing (a fresh profile, a deleted or
        // zero-byte file, an interrupted first run) or be missing individual tables, and importing into a database
        // that isn't there fails on the backup copy below. Put it back into its canonical shape first - this is a
        // no-op for an existing, healthy database.
        DatabaseSchema.EnsureSettingsDatabaseExists();

        string settingsDatabaseFilePath = HelperVariables.SettingsDatabaseFilePath;
        string backupFilePath = $"{settingsDatabaseFilePath}.bak";

        File.Copy(sourceFileName: settingsDatabaseFilePath, destFileName: backupFilePath, overwrite: true);

        try
        {
            ImportTables(tableNames: settingsTablesToBeImportedList,
                settingsDatabaseFilePath: settingsDatabaseFilePath,
                importFilePath: importFilePath);
        }
        catch (Exception ex)
        {
            FrmMainApp.Log.Error(message: $"Import failed, restoring the backup. Error: {ex.Message}");
            RestoreBackup(backupFilePath: backupFilePath, settingsDatabaseFilePath: settingsDatabaseFilePath);

            throw;
        }

        // Delete the backup if no errors occurred
        File.Delete(path: backupFilePath);
        FrmMainApp.Log.Info(message: "Done");

        return true;
    }

    /// <summary>
    ///     Replaces the given tables of the settings database with the ones in the import file, in one transaction.
    /// </summary>
    /// <param name="tableNames">The tables to replace.</param>
    /// <param name="settingsDatabaseFilePath">The file path of the settings database.</param>
    /// <param name="importFilePath">The file path of the SQLite database to import from.</param>
    private static void ImportTables(List<string> tableNames,
                                     string settingsDatabaseFilePath,
                                     string importFilePath)
    {
        using SQLiteConnection settingsConnection = new(connectionString:
            $"Data Source={settingsDatabaseFilePath};Version=3;");
        settingsConnection.Open();

        // ATTACH is not allowed inside a transaction, hence the order here.
        AttachDatabase(connection: settingsConnection, databaseFilePath: importFilePath);

        try
        {
            using SQLiteTransaction transaction = settingsConnection.BeginTransaction();

            foreach (string tableName in tableNames)
            {
                ImportTable(connection: settingsConnection, tableName: tableName);
            }

            transaction.Commit();
        }
        finally
        {
            DetachDatabase(connection: settingsConnection);
        }
    }

    /// <summary>
    ///     Replaces one table of the settings database with the one of the same name in the attached import file.
    /// </summary>
    /// <param name="connection">An open connection to the settings database, with the import file attached to it.</param>
    /// <param name="tableName">The name of the table to replace.</param>
    /// <remarks>
    ///     A table that isn't in the import file is left alone rather than emptied - the user asked for that group of
    ///     settings to be imported, and a file that doesn't carry them shouldn't wipe the ones they have.
    /// </remarks>
    private static void ImportTable(SQLiteConnection connection,
                                    string tableName)
    {
        if (!DatabaseSchema.TableExists(connection: connection, schemaName: ImportSchemaName, tableName: tableName))
        {
            FrmMainApp.Log.Info(
                message: $"Table '{tableName}' is not in the import file - leaving the current one as it is.");

            return;
        }

        List<string> importColumnNames = DatabaseSchema.GetColumnNames(connection: connection,
            schemaName: ImportSchemaName, tableName: tableName);

        DatabaseSchema.RecreateTableEmpty(connection: connection, tableName: tableName);

        List<string> columnNamesToCopy = DatabaseSchema
                                        .GetColumnNames(connection: connection, schemaName: SettingsSchemaName,
                                             tableName: tableName)
                                        .Where(predicate: columnName => importColumnNames.Contains(value: columnName,
                                             comparer: StringComparer.OrdinalIgnoreCase))
                                        .ToList();

        if (columnNamesToCopy.Count == 0)
        {
            FrmMainApp.Log.Error(
                message:
                $"Table '{tableName}' in the import file has no column in common with the current one - it has been left empty.");

            return;
        }

        string columnList = string.Join(separator: ", ",
            values: columnNamesToCopy.Select(selector: DatabaseSchema.QuoteIdentifier));
        string quotedTableName = DatabaseSchema.QuoteIdentifier(identifier: tableName);

        using SQLiteCommand copyCommand = new(
            commandText:
            $"INSERT INTO {SettingsSchemaName}.{quotedTableName} ({columnList}) SELECT {columnList} FROM {ImportSchemaName}.{quotedTableName};",
            connection: connection);
        int rowsImported = copyCommand.ExecuteNonQuery();

        FrmMainApp.Log.Info(message: $"Imported {rowsImported} row(s) into '{tableName}'.");
    }

    /// <summary>
    ///     Attaches the file being imported from to the connection under <see cref="ImportSchemaName" />.
    /// </summary>
    /// <param name="connection">An open connection to the settings database.</param>
    /// <param name="databaseFilePath">The file path of the SQLite database to attach.</param>
    /// <remarks>
    ///     The path is passed as a parameter rather than pasted into the statement - it comes from a file dialog and
    ///     can perfectly well contain an apostrophe, which used to break the statement.
    /// </remarks>
    private static void AttachDatabase(SQLiteConnection connection,
                                       string databaseFilePath)
    {
        using SQLiteCommand attachCommand = new(
            commandText:
            $"ATTACH DATABASE @databaseFilePath AS {DatabaseSchema.QuoteIdentifier(identifier: ImportSchemaName)};",
            connection: connection);
        _ = attachCommand.Parameters.AddWithValue(parameterName: "@databaseFilePath", value: databaseFilePath);
        _ = attachCommand.ExecuteNonQuery();
    }

    /// <summary>
    ///     Detaches the file being imported from.
    /// </summary>
    /// <param name="connection">An open connection to the settings database, with the import file attached to it.</param>
    /// <remarks>
    ///     This runs in a finally block, so a failure here must not replace whatever exception got us there. The
    ///     connection is closed straight afterwards anyway, which detaches everything regardless.
    /// </remarks>
    private static void DetachDatabase(SQLiteConnection connection)
    {
        try
        {
            using SQLiteCommand detachCommand = new(
                commandText: $"DETACH DATABASE {DatabaseSchema.QuoteIdentifier(identifier: ImportSchemaName)};",
                connection: connection);
            _ = detachCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            FrmMainApp.Log.Error(message: $"Could not detach the import file. Error: {ex.Message}");
        }
    }

    /// <summary>
    ///     Puts the pre-import copy of the settings database back in place.
    /// </summary>
    /// <param name="backupFilePath">The file path of the backup taken before the import started.</param>
    /// <param name="settingsDatabaseFilePath">The file path of the settings database.</param>
    private static void RestoreBackup(string backupFilePath,
                                      string settingsDatabaseFilePath)
    {
        try
        {
            File.Copy(sourceFileName: backupFilePath, destFileName: settingsDatabaseFilePath, overwrite: true);
            File.Delete(path: backupFilePath);
        }
        catch (Exception ex)
        {
            // deliberately leave the backup on disk - at this point it may be the only intact copy of the user's
            // settings, and the file name says what it is.
            FrmMainApp.Log.Error(
                message: $"Could not restore '{backupFilePath}', leaving it in place. Error: {ex.Message}");
        }
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
    private static bool IsValidSQLite(string filePath)
    {
        try
        {
            using SQLiteConnection connection = new(connectionString: $"Data Source={filePath};Version=3;");
            connection.Open();
            using (SQLiteCommand command = new(commandText: "SELECT 1", connection: connection))
            {
                _ = command.ExecuteNonQuery();
            }

            connection.Close();

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
