using System.Data;
using System.Data.SQLite;

namespace GeoTagNinja.Helpers;

internal static class HelperDataCustomRules
{
    internal static DataTable DataReadSQLiteCustomRules()
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SSettingsDataBasePath);
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

    internal static void DataWriteSQLiteCustomRules()
    {
        // write back
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SSettingsDataBasePath);
        sqliteDB.Open();

        using SQLiteDataAdapter sqliteAdapter = new(commandText: @"select * from customRules", connection: sqliteDB);
        SQLiteCommandBuilder commandBuilder = new(adp: sqliteAdapter);
        sqliteAdapter.Update(dataTable: HelperVariables.DtCustomRules);

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

        sqlToRun.ExecuteNonQuery();
    }


    internal static void DataCreateSQLiteCustomRules()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SSettingsDataBasePath);
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

        sqlToRun.ExecuteNonQuery();
    }
}