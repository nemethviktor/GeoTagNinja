using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace GeoTagNinja.Helpers;

internal static class HelperDataCustomCityAllocationRules
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
            string defaultCiltyAllocationLogic = "";
            foreach (string countryCode in HelperGenericAncillaryListsArrays.GetCountryCodes())
            {
                if (!string.IsNullOrEmpty(value: countryCode))
                {
                    string countryCodeAllocation = defaultCityNameIsAdminName1Arr.Contains(value: countryCode)
                        ? "AdminName1"
                        : defaultCityNameIsAdminName2Arr.Contains(value: countryCode)
                            ? "AdminName2" : defaultCityNameIsAdminName3Arr.Contains(value: countryCode)
                            ? "AdminName3" : defaultCityNameIsAdminName4Arr.Contains(value: countryCode) ? "AdminName4" : "Undefined";
                    defaultCiltyAllocationLogic +=
                        $"({HelperVariables.DoubleQuoteStr}{countryCode}{HelperVariables.DoubleQuoteStr},{HelperVariables.DoubleQuoteStr}{countryCodeAllocation}{HelperVariables.DoubleQuoteStr}),{Environment.NewLine}";
                }
            }

            // remove last ","
            defaultCiltyAllocationLogic = defaultCiltyAllocationLogic.Substring(startIndex: 0, length: defaultCiltyAllocationLogic.LastIndexOf(value: ','));

            commandText = $@"
                                INSERT INTO customCityAllocationLogic
                                        (CountryCode,TargetPointNameCustomCityLogic)
                                        VALUES{Environment.NewLine}{defaultCiltyAllocationLogic}{Environment.NewLine};"
                ;

            SQLiteCommand = new SQLiteCommand(commandText: commandText, connection: SQLiteDB);

            _ = SQLiteCommand.ExecuteNonQuery();
        }
    }

    internal static void DataCreateSQLiteCustomCityAllocationLogic()
    {
        FrmMainApp.Log.Info(message: "Starting");

        using SQLiteConnection SQLiteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        string commandText = """
                                    CREATE TABLE IF NOT EXISTS customCityAllocationLogic(
                                        CountryCode TEXT(3) NOT NULL,
                                        TargetPointNameCustomCityLogic TEXT(100) NOT NULL,
                                        PRIMARY KEY(CountryCode, TargetPointNameCustomCityLogic)
                                );
                                """
            ;

        SQLiteCommand SQLiteCommand = new(commandText: commandText, connection: SQLiteDB);

        _ = SQLiteCommand.ExecuteNonQuery();
        SQLiteDB.Close();

        DataWriteSQLiteCustomCityAllocationLogicDefaults();
    }
}