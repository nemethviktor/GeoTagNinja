using GeoTagNinja.Model;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

// ReSharper disable InconsistentNaming

namespace GeoTagNinja.Helpers;

internal static class HelperDataApplicationSettings
{
    #region Read

    /// <summary>
    ///     Checks the SQLite database for checkbox-settings and returns true/false accordingly
    /// </summary>
    /// <param name="dataTable">The DataTable form which the data is read</param>
    /// <param name="settingTabPage">TabPage of the above</param>
    /// <param name="settingId">The Checkbox's name itself</param>
    /// <returns>true or false</returns>
    internal static bool DataReadCheckBoxSettingTrueOrFalse(DataTable dataTable, string settingTabPage,
        string settingId)
    {
        EnumerableRowCollection<DataRow> drDataTableData =
            from DataRow dataRow in dataTable.AsEnumerable()
            where dataRow.Field<string>(columnName: "settingTabPage") == settingTabPage &&
                  dataRow.Field<string>(columnName: "settingId") ==
                  settingId
            select dataRow;

        DataRow dataRows = drDataTableData.FirstOrDefault();
        return dataRows != null && dataRows[columnName: "settingValue"].ToString() == "true";
    }

    /// <summary>
    ///     Checks the SQLite database for integer settings
    /// </summary>
    /// <param name="dataTable">The DataTable form which the data is read</param>
    /// <param name="settingTabPage">TabPage of the above</param>
    /// <param name="settingId">The Control's name itself</param>
    /// <returns>Integer-converted value or 0 if fails</returns>
    internal static int DataReadIntSetting(DataTable dataTable,
                                           string settingTabPage,
                                           string settingId)
    {
        EnumerableRowCollection<DataRow> drDataTableData =
            from DataRow dataRow in dataTable.AsEnumerable()
            where dataRow.Field<string>(columnName: "settingTabPage") == settingTabPage &&
                  dataRow.Field<string>(columnName: "settingId") ==
                  settingId
            select dataRow;

        DataRow dataRows = drDataTableData.FirstOrDefault();
        int retVal = 0;
        if (dataRows != null)
        {
            string strVal = dataRows[columnName: "settingValue"].ToString();
            bool _ = int.TryParse(s: strVal,
                result: out retVal);
        }

        return retVal;
    }

    /// <summary>
    ///     Checks the SQLite database for radio button settings and returns the selected option.
    /// </summary>
    /// <param name="dataTable">The DataTable form which the data is read</param>
    /// <param name="settingTabPage">The tab page of the setting.</param>
    /// <param name="optionList">The list of options for the radio button.</param>
    /// <returns>The selected option from the radio button setting.</returns>
    internal static string DataReadRadioButtonSettingTrueOrFalse(DataTable dataTable,
                                                                 string settingTabPage,
                                                                 List<string> optionList)
    {
        string whichValueIsTrue = "";
        foreach (string optionValue in optionList)
        {
            if (DataReadCheckBoxSettingTrueOrFalse(
                    dataTable: dataTable,
                    settingTabPage: settingTabPage,
                    settingId: optionValue
                ))
            {
                whichValueIsTrue = optionValue.Replace(oldValue: "rbt_MapColourMode", newValue: "");
                break;
            }
        }

        return whichValueIsTrue;
    }

    /// <summary>
    ///     Reads the SQLite table into a DataTable. Easier to manipulate/read.
    /// </summary>
    /// <param name="dataTable"></param>
    /// <param name="settingTabPage"></param>
    /// <param name="settingId"></param>
    /// <param name="returnBlankIfNull"></param>
    /// <returns></returns>
    public static string? DataReadSQLiteSettings(DataTable dataTable, string settingTabPage, string settingId,
        bool returnBlankIfNull = false)
    {
        EnumerableRowCollection<DataRow> drDataTableData =
            from DataRow dataRow in dataTable.AsEnumerable()
            where dataRow.Field<string>(columnName: "settingTabPage") == settingTabPage &&
                  dataRow.Field<string>(columnName: "settingId") ==
                  settingId
            select dataRow;

        DataRow dataRows = drDataTableData.FirstOrDefault();
        return dataRows != null &&
            !string.IsNullOrWhiteSpace(value: dataRows[columnName: "settingValue"].ToString())
            ? dataRows[columnName: "settingValue"].ToString()
            : returnBlankIfNull ? "" : null;
    }

    #endregion

    #region Write

    /// <summary>
    ///     Similar to the one above (which reads the data) - this one writes it.
    /// </summary>
    /// <param name="settingsToWrite">List of settings to be written/overwritten</param>
    internal static void DataWriteSQLiteSettings(List<AppSettingContainer> settingsToWrite)
    {
        using SQLiteConnection SQLiteDB =
            new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();
        using SQLiteCommand commandText = new(connection: SQLiteDB);
        using SQLiteTransaction SQLiteTransaction = SQLiteDB.BeginTransaction();
        foreach (AppSettingContainer appSettingContainer in settingsToWrite)
        {
            commandText.CommandText = $@"
                                        DELETE FROM {appSettingContainer.TableName} WHERE settingTabPage = @settingTabPage AND settingId = @settingId;"
                ;

            _ = commandText.Parameters.AddWithValue(parameterName: "@settingTabPage",
                value: appSettingContainer.SettingTabPage);
            _ = commandText.Parameters.AddWithValue(parameterName: "@settingId", value: appSettingContainer.SettingId);
            _ = commandText.ExecuteNonQuery();
        }

        foreach (AppSettingContainer appSettingContainer in settingsToWrite)
        {
            commandText.CommandText =
                $@"
                INSERT INTO {appSettingContainer.TableName}  (settingTabPage, settingId, settingValue) VALUES (@settingTabPage, @settingId, @settingValue);";

            _ = commandText.Parameters.AddWithValue(parameterName: "@settingTabPage",
                value: appSettingContainer.SettingTabPage);
            _ = commandText.Parameters.AddWithValue(parameterName: "@settingId", value: appSettingContainer.SettingId);
            _ = commandText.Parameters.AddWithValue(parameterName: "@settingValue",
                value: appSettingContainer.SettingValue);
            _ = commandText.ExecuteNonQuery();
        }

        SQLiteTransaction.Commit();

        // refresh main datatables
        _ = HelperGenericAppStartup.AppStartupReadSQLiteTables();
    }

    #endregion

    #region Transfer

    /// <summary>
    ///     Transfers data from the "write queue" to the actual table. This is executed when the user presses the OK button in
    ///     settings until then the data is kept in the pre-queue table.
    /// </summary>
    internal static void DataTransferSQLiteSettingsFromPreQueue()
    {
        List<AppSettingContainer> preQueueAppSettingContainer =
            (from DataRow dataRow in HelperVariables.DtHelperDataApplicationSettingsPreQueue.Rows
             select new AppSettingContainer
             {
                 TableName = "settings",
                 SettingTabPage = dataRow[columnName: "settingTabPage"].ToString(),
                 SettingId = dataRow[columnName: "settingId"].ToString(),
                 SettingValue = dataRow[columnName: "settingValue"].ToString()
             }).ToList();

        DataWriteSQLiteSettings(settingsToWrite: preQueueAppSettingContainer);
    }

    #endregion

    #region Delete & Cleanup

    /// <summary>
    ///     This is largely me being a derp and doing a manual cleanup. My original SQL script was a bit buggy and so we have a
    ///     potential plethora of unused and possibly errouneous setting tokens.
    /// </summary>
    internal static void DataDeleteSQLiteSettingsCleanup()
    {
        using SQLiteConnection SQLiteDB =
            new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        string commandText = @"
                                DELETE 
                                FROM   [settings]
                                WHERE  [rowid] NOT IN (SELECT MAX ([rowid])
                                       FROM   [settings]
                                       GROUP  BY
                                                 [settingTabPage], 
                                                 [settingId]);
                                "
            ;
        commandText += ";";
        SQLiteCommand SQLiteCommand = new(commandText: commandText, connection: SQLiteDB);
        _ = SQLiteCommand.ExecuteNonQuery();
    }

    /// <summary>
    ///     This just compresses the database. Though I don't expect it'd be a large file in the first place but unlikely to
    ///     hurt.
    /// </summary>
    internal static void DataVacuumDatabase()
    {
        using SQLiteConnection SQLiteDB =
            new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        string commandText = @"VACUUM;"
            ;
        commandText += ";";
        SQLiteCommand SQLiteCommand
            = new(commandText: commandText, connection: SQLiteDB);
        _ = SQLiteCommand.ExecuteNonQuery();
    }

    #endregion
}