using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     The canonical schema of the settings database (<see cref="HelperVariables.SettingsDatabaseFilePath" />).
/// </summary>
/// <remarks>
///     <para>
///         This is the single source of truth for the shape of every table in that database. The DDL used to be
///         written out three times over - once inline in <see cref="DatabaseAndStartup.DataCreateSQLiteDB" /> for a
///         brand new file, once more in each of <see cref="Favourites" />, <see cref="CustomRules" /> and
///         <see cref="CustomCityAllocationRules" /> for a file that already existed, and implicitly a fourth time in
///         <see cref="SettingsImport" />, which rebuilt tables with <c>CREATE TABLE ... AS SELECT</c> and thereby
///         silently dropped every primary key, <c>NOT NULL</c> and <c>AUTOINCREMENT</c> in them. Anything that creates
///         a table now asks this class for the statement, so the copies cannot drift apart again.
///     </para>
///     <para>
///         The statements are deliberately byte-for-byte what earlier versions of the app wrote, apart from
///         <c>IF NOT EXISTS</c>: SQLite does not compare the schema of an existing table against the one in the
///         statement, so an existing database is left exactly as it was and only missing tables are created.
///     </para>
/// </remarks>
internal static class DatabaseSchema
{
    internal const string TableNameSettings = "settings";
    internal const string TableNameAppLayout = "appLayout";
    internal const string TableNameFavourites = "Favourites";
    internal const string TableNameCustomRules = "customRules";
    internal const string TableNameCustomCityAllocationLogic = "customCityAllocationLogic";

    /// <summary>
    ///     Every table the settings database is expected to have, in creation order.
    /// </summary>
    internal static readonly IReadOnlyList<string> AllTableNames =
    [
        TableNameSettings,
        TableNameAppLayout,
        TableNameFavourites,
        TableNameCustomRules,
        TableNameCustomCityAllocationLogic
    ];

    private static readonly Dictionary<string, string> CreateTableStatements =
        new(comparer: StringComparer.OrdinalIgnoreCase)
        {
            {
                TableNameSettings, """
                                   CREATE TABLE IF NOT EXISTS settings(
                                       settingTabPage TEXT(255)    NOT NULL,
                                       settingId TEXT(255)         NOT NULL,
                                       settingValue NTEXT(2000)    DEFAULT "",
                                       PRIMARY KEY(settingTabPage, settingId)
                                   );
                                   """
            },
            {
                TableNameAppLayout, """
                                    CREATE TABLE IF NOT EXISTS appLayout(
                                        settingTabPage TEXT(255)    NOT NULL,
                                        settingId TEXT(255)         NOT NULL,
                                        settingValue NTEXT(2000)    DEFAULT "",
                                        PRIMARY KEY(settingTabPage, settingId)
                                    );
                                    """
            },
            {
                TableNameFavourites, """
                                     CREATE TABLE IF NOT EXISTS Favourites(
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
                                     );
                                     """
            },
            {
                TableNameCustomRules, """
                                      CREATE TABLE IF NOT EXISTS customRules(
                                          ruleId INTEGER PRIMARY KEY AUTOINCREMENT,
                                          CountryCode NTEXT NOT NULL,
                                          DataPointName NTEXT NOT NULL,
                                          DataPointConditionType NTEXT NOT NULL,
                                          DataPointConditionValue NTEXT NOT NULL,
                                          TargetPointName NTEXT NOT NULL,
                                          TargetPointOutcome NTEXT NOT NULL,
                                          TargetPointOutcomeCustom NTEXT
                                      );
                                      """
            },
            {
                TableNameCustomCityAllocationLogic, """
                                                    CREATE TABLE IF NOT EXISTS customCityAllocationLogic(
                                                        CountryCode TEXT(3) NOT NULL,
                                                        TargetPointNameCustomCityLogic TEXT(100) NOT NULL,
                                                        PRIMARY KEY(CountryCode, TargetPointNameCustomCityLogic)
                                                    );
                                                    """
            }
        };

    /// <summary>
    ///     Returns the <c>CREATE TABLE IF NOT EXISTS</c> statement for one of the settings database's tables.
    /// </summary>
    /// <param name="tableName">The name of the table. Case-insensitive.</param>
    /// <returns>The statement that creates the table in its canonical shape.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the table is not part of the settings database.</exception>
    internal static string GetCreateTableStatement(string tableName)
    {
        if (!CreateTableStatements.TryGetValue(key: tableName, value: out string createTableStatement))
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(tableName), actualValue: tableName,
                message: "There is no such table in the settings database.");
        }

        return createTableStatement;
    }

    /// <summary>
    ///     Whether the given name belongs to a table of the settings database.
    /// </summary>
    /// <param name="tableName">The name of the table. Case-insensitive.</param>
    /// <returns>True if the table is part of the canonical schema.</returns>
    internal static bool IsKnownTable(string tableName)
    {
        return tableName is not null && CreateTableStatements.ContainsKey(key: tableName);
    }

    /// <summary>
    ///     Makes sure the settings database file exists and that every table of the canonical schema is present in it.
    /// </summary>
    /// <remarks>
    ///     Safe to call repeatedly and safe to call against an existing database - existing tables and their contents
    ///     are left untouched. Unlike <see cref="DatabaseAndStartup.DataCreateSQLiteDB" /> this throws rather than
    ///     showing a message box, so callers can decide what to do about the failure.
    /// </remarks>
    internal static void EnsureSettingsDatabaseExists()
    {
        string settingsDatabaseFilePath = HelperVariables.SettingsDatabaseFilePath;

        string settingsDatabaseFolderPath = Path.GetDirectoryName(path: settingsDatabaseFilePath);
        if (!string.IsNullOrEmpty(value: settingsDatabaseFolderPath))
        {
            _ = Directory.CreateDirectory(path: settingsDatabaseFolderPath);
        }

        FileInfo settingsDatabaseFile = new(fileName: settingsDatabaseFilePath);

        // a zero-byte file is what an interrupted create leaves behind; SQLite would happily open it but every
        // table would be missing, so start over instead.
        if (settingsDatabaseFile.Exists && settingsDatabaseFile.Length == 0)
        {
            settingsDatabaseFile.Delete();
            settingsDatabaseFile.Refresh();
        }

        if (!settingsDatabaseFile.Exists)
        {
            SQLiteConnection.CreateFile(databaseFileName: settingsDatabaseFilePath);
        }

        using SQLiteConnection SQLiteDB = new(connectionString:
            $"Data Source={settingsDatabaseFilePath};Version=3");
        SQLiteDB.Open();

        EnsureAllTablesExist(connection: SQLiteDB);
    }

    /// <summary>
    ///     Creates whichever tables of the canonical schema are missing from the connected database.
    /// </summary>
    /// <param name="connection">An open connection to the settings database.</param>
    internal static void EnsureAllTablesExist(SQLiteConnection connection)
    {
        foreach (string tableName in AllTableNames)
        {
            EnsureTableExists(connection: connection, tableName: tableName);
        }
    }

    /// <summary>
    ///     Creates one table of the canonical schema if the connected database doesn't have it yet.
    /// </summary>
    /// <param name="connection">An open connection to the settings database.</param>
    /// <param name="tableName">The name of the table. Case-insensitive.</param>
    internal static void EnsureTableExists(SQLiteConnection connection,
                                           string tableName)
    {
        using SQLiteCommand SQLiteCommand = new(
            commandText: GetCreateTableStatement(tableName: tableName),
            connection: connection);
        _ = SQLiteCommand.ExecuteNonQuery();
    }

    /// <summary>
    ///     Drops a table and re-creates it empty, in its canonical shape.
    /// </summary>
    /// <param name="connection">An open connection to the settings database.</param>
    /// <param name="tableName">The name of the table. Case-insensitive.</param>
    /// <remarks>
    ///     Used by the settings import, which replaces a table wholesale. Going through a drop rather than a
    ///     <c>DELETE FROM</c> also repairs tables left behind by older versions of the importer, which rebuilt them
    ///     with <c>CREATE TABLE ... AS SELECT</c> and lost every constraint in the process.
    /// </remarks>
    internal static void RecreateTableEmpty(SQLiteConnection connection,
                                            string tableName)
    {
        string createTableStatement = GetCreateTableStatement(tableName: tableName);

        using (SQLiteCommand dropCommand = new(
                   commandText: $"DROP TABLE IF EXISTS {QuoteIdentifier(identifier: tableName)};",
                   connection: connection))
        {
            _ = dropCommand.ExecuteNonQuery();
        }

        using SQLiteCommand createCommand = new(commandText: createTableStatement, connection: connection);
        _ = createCommand.ExecuteNonQuery();
    }

    /// <summary>
    ///     Whether a table exists in one of the databases on the connection.
    /// </summary>
    /// <param name="connection">An open connection.</param>
    /// <param name="schemaName">The schema to look in - <c>main</c>, or the alias an attached database was given.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>True if the table exists.</returns>
    internal static bool TableExists(SQLiteConnection connection,
                                     string schemaName,
                                     string tableName)
    {
        using SQLiteCommand SQLiteCommand = new(
            commandText:
            $"SELECT 1 FROM {QuoteIdentifier(identifier: schemaName)}.sqlite_master WHERE type = 'table' AND name = @tableName;",
            connection: connection);
        _ = SQLiteCommand.Parameters.AddWithValue(parameterName: "@tableName", value: tableName);

        return SQLiteCommand.ExecuteScalar() is not null;
    }

    /// <summary>
    ///     Returns the column names of a table, in declaration order.
    /// </summary>
    /// <param name="connection">An open connection.</param>
    /// <param name="schemaName">The schema to look in - <c>main</c>, or the alias an attached database was given.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>The column names, or an empty list if the table doesn't exist.</returns>
    internal static List<string> GetColumnNames(SQLiteConnection connection,
                                                string schemaName,
                                                string tableName)
    {
        List<string> columnNames = [];

        using SQLiteCommand SQLiteCommand = new(
            commandText:
            $"PRAGMA {QuoteIdentifier(identifier: schemaName)}.table_info({QuoteIdentifier(identifier: tableName)});",
            connection: connection);
        using SQLiteDataReader reader = SQLiteCommand.ExecuteReader();

        int nameOrdinal = -1;
        while (reader.Read())
        {
            if (nameOrdinal < 0)
            {
                nameOrdinal = reader.GetOrdinal(name: "name");
            }

            columnNames.Add(item: reader.GetString(i: nameOrdinal));
        }

        return columnNames;
    }

    /// <summary>
    ///     Wraps an identifier in double quotes so it can be pasted into a statement safely.
    /// </summary>
    /// <param name="identifier">A table, column or schema name.</param>
    /// <returns>The quoted identifier.</returns>
    /// <remarks>
    ///     Table and column names cannot be passed as parameters, and the import reads column names out of a file the
    ///     user picked, so they do have to be quoted rather than trusted.
    /// </remarks>
    internal static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace(oldValue: "\"", newValue: "\"\"")}\"";
    }
}
