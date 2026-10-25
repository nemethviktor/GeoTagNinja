using GeoTagNinja.Helpers.Localisation;
using GeoTagNinja.View.Forms;
using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace GeoTagNinja.Helpers.Data;

internal static class CustomCityAllocationRules
{
    internal static DataTable DataReadSQLiteCustomCityAllocationLogic()
    {
        using SQLiteConnection SQLiteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        string commandText = @"
                                SELECT *
                                FROM customCityAllocationLogic
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

    internal static void DataWriteSQLiteCustomCityAllocationLogic()
    {
        // write back
        using SQLiteConnection SQLiteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        using SQLiteDataAdapter SQLiteAdapter = new(commandText: @"select * from customCityAllocationLogic", connection: SQLiteDB);
        SQLiteCommandBuilder commandBuilder = new(adp: SQLiteAdapter);
        _ = SQLiteAdapter.Update(dataTable: HelperVariables.DtCustomCityLogic);
    }

    internal static void DataWriteSQLiteCustomCityAllocationLogicDefaults(bool resetToDefaults = false)
    {
        FrmMainApp.Log.Info(message: "Starting");
        string[] defaultCityNameIsAdminName1Arr = { "LIE", "SMR", "MNE", "MKD", "MLT", "SVN" };
        string[] defaultCityNameIsAdminName2Arr = { "ALA", "BRA", "COL", "CUB", "CYP", "DNK", "FRO", "GTM", "HND", "HRV", "ISL", "LUX", "LVA", "NIC", "NLD", "NOR", "PRI", "PRT", "ROU", "SWE" };
        string[] defaultCityNameIsAdminName3Arr = { "AUT", "CHE", "CHL", "CZE", "EST", "ESP", "FIN", "GRC", "ITA", "PAN", "PER", "POL", "SRB", "SVK", "USA", "ZAF" };
        string[] defaultCityNameIsAdminName4Arr = { "BEL", "DEU", "FRA", "GUF", "GLP", "MTQ" };

        using SQLiteConnection SQLiteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        string commandText = null;
        SQLiteCommand SQLiteCommand = new(commandText: commandText);

        if (resetToDefaults)
        {
            // "SQLite does not have an explicit TRUNCATE TABLE command like other databases" -- what a bunch of idiots.
            commandText = "DELETE FROM customCityAllocationLogic;";
            SQLiteCommand = new SQLiteCommand(commandText: commandText, connection: SQLiteDB);
            _ = SQLiteCommand.ExecuteNonQuery();
        }

        commandText = "SELECT COUNT(*) FROM customCityAllocationLogic;";

        SQLiteCommand = new SQLiteCommand(commandText: commandText, connection: SQLiteDB);

        // fill w defaults if empty
        if (!(Convert.ToInt32(value: SQLiteCommand.ExecuteScalar()) > 0))
        {
            // The values are bound as parameters. They used to be spliced in as double-quoted "literals", which newer
            // SQLite builds read as column names ("no such column: ZWE") and which broke seeding on a fresh install.
            using SQLiteTransaction seedTransaction = SQLiteDB.BeginTransaction();
            int seededRows = 0;
            foreach (string countryCode in Countries.GetCountryCodes())
            {
                if (!string.IsNullOrEmpty(value: countryCode))
                {
                    string countryCodeAllocation = defaultCityNameIsAdminName1Arr.Contains(value: countryCode)
                        ? "AdminName1"
                        : defaultCityNameIsAdminName2Arr.Contains(value: countryCode)
                            ? "AdminName2" : defaultCityNameIsAdminName3Arr.Contains(value: countryCode)
                            ? "AdminName3" : defaultCityNameIsAdminName4Arr.Contains(value: countryCode) ? "AdminName4" : "Undefined";

                    using SQLiteCommand insertCommand = new(
                        commandText: "INSERT INTO customCityAllocationLogic (CountryCode,TargetPointNameCustomCityLogic) VALUES (@countryCode, @allocation);",
                        connection: SQLiteDB,
                        transaction: seedTransaction);
                    _ = insertCommand.Parameters.AddWithValue(parameterName: "@countryCode", value: countryCode);
                    _ = insertCommand.Parameters.AddWithValue(parameterName: "@allocation", value: countryCodeAllocation);
                    _ = insertCommand.ExecuteNonQuery();
                    seededRows++;
                }
            }

            seedTransaction.Commit();
            FrmMainApp.Log.Info(message: $"Seeded customCityAllocationLogic with {seededRows} default rows");
        }
    }

    // The "customCityAllocationLogic" table used to be created here as well, from a second copy of the DDL in
    // DatabaseAndStartup.DataCreateSQLiteDB. Both copies now live in DatabaseSchema, which creates the table;
    // DataCreateSQLiteDB then calls DataWriteSQLiteCustomCityAllocationLogicDefaults above to seed it.
}