using System.Data;
using System.Data.SQLite;

namespace GeoTagNinja.Helpers;

/// <summary>
///     Provides static methods for managing custom rules in a SQLite database.
/// </summary>
/// <remarks>
///     This class contains methods for reading, writing, and creating custom rules in a SQLite database.
///     The custom rules are stored in a table named 'CustomRules' in the database.
///     Each custom rule is represented as a row in the 'CustomRules' table.
/// </remarks>
internal static class HelperDataCustomRules
{
    /// <summary>
    ///     Retrieves the custom rules from the SQLite database.
    /// </summary>
    /// <returns>
    ///     A DataTable containing the custom rules from the SQLite database.
    /// </returns>
    internal static DataTable DataReadSQLiteCustomRules()
    {
        using SQLiteConnection sqliteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        sqliteDB.Open();

        string sqlCommandStr = @"
                                SELECT *
                                FROM CustomRules
                                WHERE 1=1
                                ;
								"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        SQLiteDataReader reader = sqlToRun.ExecuteReader();
        DataTable dataTable = new();
        dataTable.Load(reader: reader);
        return dataTable;
    }

    /// <summary>
    ///     Writes the custom rules back to the SQLite database.
    /// </summary>
    /// <remarks>
    ///     This method updates the 'customRules' table in the SQLite database with the current state of the 'DtCustomRules'
    ///     DataTable.
    ///     It also performs cleanup operations to ensure data integrity, such as removing rows where 'TargetPointOutcome' is
    ///     'Custom' but 'TargetPointOutcomeCustom' is null,
    ///     and nullifying 'TargetPointOutcomeCustom' where 'TargetPointOutcome' is not 'Custom'.
    /// </remarks>
    internal static void DataWriteSQLiteCustomRules()
    {
        // write back
        using SQLiteConnection sqliteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        sqliteDB.Open();

        using SQLiteDataAdapter sqliteAdapter = new(commandText: @"select * from customRules", connection: sqliteDB);
        SQLiteCommandBuilder commandBuilder = new(adp: sqliteAdapter);
        _ = sqliteAdapter.Update(dataTable: HelperVariables.DtCustomRules);

        // this is stupid but Update doesn't seem to work with a delete/AcceptChange so...
        string sqlCommandStr = @"
                                DELETE FROM customRules
                                WHERE 1=1
                                    AND TargetPointOutcome = 'Custom'
                                    AND TargetPointOutcomeCustom IS NULL
                                ;
                                UPDATE customRules
                                SET TargetPointOutcomeCustom = NULL
                                WHERE 1=1
                                    AND TargetPointOutcome != 'Custom'
                                ;
                                "
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        _ = sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Creates a table for custom rules in the SQLite database if it doesn't exist.
    /// </summary>
    /// <remarks>
    ///     The table 'customRules' is created with the following columns:
    ///     'ruleId', 'CountryCode', 'DataPointName', 'DataPointConditionType', 'DataPointConditionValue',
    ///     'TargetPointName', 'TargetPointOutcome', 'TargetPointOutcomeCustom'.
    /// </remarks>
    internal static void DataCreateSQLiteCustomRules()
    {
        FrmMainApp.Log.Info(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        sqliteDB.Open();

        string sqlCommandStr = @"
                                CREATE TABLE IF NOT EXISTS customRules(
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
                                "
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        _ = sqlToRun.ExecuteNonQuery();
    }
}