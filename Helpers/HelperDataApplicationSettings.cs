using System.Data.SQLite;

namespace GeoTagNinja.Helpers;

internal static class HelperDataApplicationSettings
{
    /// <summary>
    ///     Deletes the data in the "write queue" table. Gets executed if the user presses ok/cancel on the settings form.
    ///     Obvs if they press OK then DataTransferSQLiteSettings fires first.
    /// </summary>
    internal static void DataDeleteSQLitesettingsToWritePreQueue()
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                DELETE
                                FROM settingsToWritePreQueue
                                WHERE 1=1
                                    
                                
                                "
            ;
        sqlCommandStr += ";";
        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Checks the SQLite database for checkbox-settings and returns true/false accordingly
    /// </summary>
    /// <param name="tableName">TableName where the particular checkbox is - this is almost always "settings"</param>
    /// <param name="settingTabPage">TabPage of the above</param>
    /// <param name="settingId">The Checkbox's name itself</param>
    /// <returns>true or false</returns>
    internal static bool DataReadCheckBoxSettingTrueOrFalse(string tableName,
                                                            string settingTabPage,
                                                            string settingId)
    {
        string valueInSQL = DataReadSQLiteSettings(
            tableName: tableName,
            settingTabPage: settingTabPage,
            settingId: settingId
        );
        if (valueInSQL == "true")
        {
            return true;
        }

        return false;
    }


    /// <summary>
    ///     Reads the user-settings and returns them to the app (such as say default starting folder.)
    /// </summary>
    /// <param name="tableName">
    ///     This will generally be "settings" (but could be applayout as well). Remainder of an older
    ///     design where I had tables for data lined up to be saved
    /// </param>
    /// <param name="settingTabPage">This lines up with the tab name on the Settings form</param>
    /// <param name="settingId">Name of the SettingID for which data is requested</param>
    /// <returns>String - the value of the given SettingID</returns>
    internal static string DataReadSQLiteSettings(string tableName,
                                                  string settingTabPage,
                                                  string settingId)
    {
        string returnString = null;

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                SELECT settingValue
                                FROM " +
                               tableName +
                               " " +
                               @"WHERE 1=1
                                    AND settingId = @settingId
                                    AND settingTabPage = @settingTabPage
                                    ;"
            ;
        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@settingTabPage", value: settingTabPage);
        sqlToRun.Parameters.AddWithValue(parameterName: "@settingId", value: settingId);

        using SQLiteDataReader reader = sqlToRun.ExecuteReader();
        while (reader.Read())
        {
            returnString = reader.GetString(i: 0);
        }

        return returnString;
    }

    /// <summary>
    ///     Reads a single value (the ARCGIS key) from Settings. Admittedly redundant. Remnant of the early design. Should be
    ///     changed into something more efficient.
    /// </summary>
    /// <returns>The ARCGIS API key value from Settings</returns>
    internal static string DataSelectTbxARCGIS_APIKey_FromSQLite()
    {
        try
        {
            HelperVariables.SArcGisApiKey = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_ARCGIS_APIKey");
        }
        catch
        {
            HelperVariables.SArcGisApiKey = "";
        }

        return HelperVariables.SArcGisApiKey;
    }

    /// <summary>
    ///     Transfers data from the "write queue" to the actual table. This is executed when the user presses the OK button in
    ///     settings...
    ///     ... until then the data is kept in the pre-queue table. ...
    ///     The "main" table (file data table) used to have the same logic but that was converted to in-memory DataTables as
    ///     writing up to thousands of tags...
    ///     ... was very inefficient. Since settings only write a few tags I didn't bother doing the same. Boo me.
    /// </summary>
    internal static void DataTransferSQLiteSettings()
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                REPLACE INTO settings (settingTabPage, settingId, settingValue) " +
                               "SELECT settingTabPage, settingId, settingValue FROM settingsToWritePreQueue;"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Similar to the one above (which reads the data) - this one writes it.
    /// </summary>
    /// <param name="tableName">
    ///     This will generally be "settings" (but could be applayout as well). Remainder of an older
    ///     design where I had tables for data lined up to be saved
    /// </param>
    /// <param name="settingTabPage">This lines up with the tab name on the Settings form</param>
    /// <param name="settingId">Name of the SettingID for which data is requested</param>
    /// <param name="settingValue">The value to be stored.</param>
    internal static void DataWriteSQLiteSettings(string tableName,
                                                 string settingTabPage,
                                                 string settingId,
                                                 string settingValue)
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStrand = @"
                                REPLACE INTO " +
                                  tableName +
                                  " (settingTabPage, settingId, settingValue) " +
                                  "VALUES (@settingTabPage, @settingId, @settingValue);"
            ;

        SQLiteCommand sqlCommandStr = new(commandText: sqlCommandStrand, connection: sqliteDB);
        sqlCommandStr.Parameters.AddWithValue(parameterName: "@settingTabPage", value: settingTabPage);
        sqlCommandStr.Parameters.AddWithValue(parameterName: "@settingId", value: settingId);
        sqlCommandStr.Parameters.AddWithValue(parameterName: "@settingValue", value: settingValue);
        sqlCommandStr.ExecuteNonQuery();
    }
}