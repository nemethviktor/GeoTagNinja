using System.Data;
using System.Data.SQLite;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Provides static methods for managing custom rules in a SQLite database.
/// </summary>
/// <remarks>
///     This class contains methods for reading and writing custom rules in a SQLite database.
///     The custom rules are stored in a table named 'CustomRules' in the database, created by
///     <see cref="DatabaseSchema" />. Each custom rule is represented as a row in that table.
/// </remarks>
internal static class CustomRules
{
    /// <summary>
    ///     Retrieves the custom rules from the SQLite database.
    /// </summary>
    /// <returns>
    ///     A DataTable containing the custom rules from the SQLite database.
    /// </returns>
    internal static DataTable DataReadSQLiteCustomRules()
    {
        using SQLiteConnection SQLiteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        string commandText = @"
                                SELECT *
                                FROM CustomRules
                                WHERE 1=1
                                ;
								"
            ;

        SQLiteCommand SQLiteCommand = new(commandText: commandText, connection: SQLiteDB);

        SQLiteDataReader reader = SQLiteCommand.ExecuteReader();
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
        using SQLiteConnection SQLiteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        using SQLiteDataAdapter SQLiteAdapter = new(commandText: @"SELECT * FROM customRules", connection: SQLiteDB);
        SQLiteCommandBuilder commandBuilder = new(adp: SQLiteAdapter);
        _ = SQLiteAdapter.Update(dataTable: HelperVariables.DtCustomRules);

        // this is stupid but Update doesn't seem to work with a delete/AcceptChange so...
        string commandText = @"
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

        SQLiteCommand SQLiteCommand = new(commandText: commandText, connection: SQLiteDB);

        _ = SQLiteCommand.ExecuteNonQuery();
    }

    // The "customRules" table used to be created here as well, from a second copy of the DDL in
    // DatabaseAndStartup.DataCreateSQLiteDB. Both copies now live in DatabaseSchema, which creates the table.
    // DataWriteSQLiteCustomRules above depends on that DDL keeping "ruleId INTEGER PRIMARY KEY AUTOINCREMENT":
    // SQLiteCommandBuilder cannot generate an UPDATE for a table it can't find a key on.
}