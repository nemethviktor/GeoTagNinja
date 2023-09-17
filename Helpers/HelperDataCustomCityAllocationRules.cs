using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace GeoTagNinja.Helpers;

internal static class HelperDataCustomCityAllocationRules
{
    internal static DataTable DataReadSQLiteCustomCityAllocationLogic()
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                SELECT *
                                FROM customCityAllocationLogic
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

    internal static void DataWriteSQLiteCustomCityAllocationLogic()
    {
        // write back
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
        sqliteDB.Open();

        using SQLiteDataAdapter sqliteAdapter = new(commandText: @"select * from customCityAllocationLogic", connection: sqliteDB);
        SQLiteCommandBuilder commandBuilder = new(adp: sqliteAdapter);
        sqliteAdapter.Update(dataTable: HelperVariables.DtCustomCityLogic);
    }

    internal static void DataWriteSQLiteCustomCityAllocationLogicDefaults(bool resetToDefaults = false)
    {
        FrmMainApp.Logger.Debug(message: "Starting");
        string[] defaultCityNameIsAdminName1Arr = { "LIE", "SMR", "MNE", "MKD", "MLT", "SVN" };
        string[] defaultCityNameIsAdminName2Arr = { "ALA", "BRA", "COL", "CUB", "CYP", "DNK", "FRO", "GTM", "HND", "HRV", "ISL", "LUX", "LVA", "NIC", "NLD", "NOR", "PRI", "PRT", "ROU", "SWE" };
        string[] defaultCityNameIsAdminName3Arr = { "AUT", "CHE", "CHL", "CZE", "EST", "ESP", "FIN", "GRC", "ITA", "PAN", "PER", "POL", "SRB", "SVK", "USA", "ZAF" };
        string[] defaultCityNameIsAdminName4Arr = { "BEL", "DEU", "FRA", "GUF", "GLP", "MTQ" };

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
        sqliteDB.Open();

        string sqlCommandStr = null;
        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr);

        if (resetToDefaults)
        {
            // "SQLite does not have an explicit TRUNCATE TABLE command like other databases" -- what a bunch of idiots.
            sqlCommandStr = "DELETE FROM customCityAllocationLogic;";
            sqlToRun = new SQLiteCommand(commandText: sqlCommandStr, connection: sqliteDB);
            sqlToRun.ExecuteNonQuery();
        }

        sqlCommandStr = "SELECT COUNT(*) FROM customCityAllocationLogic;";

        sqlToRun = new SQLiteCommand(commandText: sqlCommandStr, connection: sqliteDB);

        // fill w defaults if empty
        if (!(Convert.ToInt32(value: sqlToRun.ExecuteScalar()) > 0))
        {
            string defaultCiltyAllocationLogic = "";
            foreach (string countryCode in HelperGenericAncillaryListsArrays.GetCountryCodes())
            {
                if (!string.IsNullOrEmpty(value: countryCode))
                {
                    string countryCodeAllocation;
                    if (defaultCityNameIsAdminName1Arr.Contains(value: countryCode))
                    {
                        countryCodeAllocation = "AdminName1";
                    }
                    else if (defaultCityNameIsAdminName2Arr.Contains(value: countryCode))
                    {
                        countryCodeAllocation = "AdminName2";
                    }
                    else if (defaultCityNameIsAdminName3Arr.Contains(value: countryCode))
                    {
                        countryCodeAllocation = "AdminName3";
                    }
                    else if (defaultCityNameIsAdminName4Arr.Contains(value: countryCode))
                    {
                        countryCodeAllocation = "AdminName4";
                    }
                    else
                    {
                        countryCodeAllocation = "Undefined";
                    }

                    defaultCiltyAllocationLogic += "(" + HelperVariables.DoubleQuoteStr + countryCode + HelperVariables.DoubleQuoteStr + "," + HelperVariables.DoubleQuoteStr + countryCodeAllocation + HelperVariables.DoubleQuoteStr + ")," + Environment.NewLine;
                }
            }

            // remove last ","
            defaultCiltyAllocationLogic = defaultCiltyAllocationLogic.Substring(startIndex: 0, length: defaultCiltyAllocationLogic.LastIndexOf(value: ','));

            sqlCommandStr = @"
                                INSERT INTO customCityAllocationLogic
                                        (CountryCode,TargetPointNameCustomCityLogic)
                                        VALUES" +
                            Environment.NewLine +
                            defaultCiltyAllocationLogic +
                            Environment.NewLine +
                            ";"
                ;

            sqlToRun = new SQLiteCommand(commandText: sqlCommandStr, connection: sqliteDB);

            sqlToRun.ExecuteNonQuery();
        }
    }


    internal static void DataCreateSQLiteCustomCityAllocationLogic()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
        sqliteDB.Open();

        string sqlCommandStr = """
                                    CREATE TABLE IF NOT EXISTS customCityAllocationLogic(
                                        CountryCode TEXT(3) NOT NULL,
                                        TargetPointNameCustomCityLogic TEXT(100) NOT NULL,
                                        PRIMARY KEY(CountryCode, TargetPointNameCustomCityLogic)
                                );
                                """
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        sqlToRun.ExecuteNonQuery();
        sqliteDB.Close();

        DataWriteSQLiteCustomCityAllocationLogicDefaults();
    }
}