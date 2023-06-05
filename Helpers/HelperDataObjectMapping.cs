using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers;

internal static class HelperDataObjectMapping
{
    /// <summary>
    ///     Reads the ObjectMapping data - basically this is to associate (exif) tags with column header names and button names
    ///     The logic is that if we have for example an exif tag "FValue" then the column header representing that will be also
    ///     "clh_FValue" etc
    ///     This also pulls tags "in and out" associations. Admittedly I don't always grasp the myriad of exif tags and their
    ///     logic so ...
    ///     ... in the code some of this will be merged elsewhere.
    /// </summary>
    /// <param name="tableName">Can be the objectNames_In or _Out table </param>
    /// <param name="orderBy">A priority-order. Defaults to the first column (alpha)</param>
    /// <returns>A DataTable with the SQL data.</returns>
    internal static DataTable DataReadSQLiteObjectMapping(string tableName,
                                                          string orderBy = "1")
    {
        try
        {
            using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + Path.Combine(path1: HelperVariables.ResourcesFolderPath, path2: "objectMapping.sqlite"));
            sqliteDB.Open();

            string sqlCommandStr = @"
                                SELECT *
                                FROM " +
                                   tableName +
                                   " " +
                                   "ORDER BY @orderBy;"
                ;

            SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
            sqlToRun.Parameters.AddWithValue(parameterName: "@orderBy", value: orderBy);
            SQLiteDataReader reader = sqlToRun.ExecuteReader();
            DataTable dataTable = new();
            dataTable.Load(reader: reader);
            return dataTable;
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: ex.Message);
            return null;
        }
    }

    /// <summary>
    ///     Reads the ObjectMappingData for the purposes of passing it to exifTool. Admittedly this could be merged with the
    ///     one above ...
    ///     ... or replace it altogether but at the time of writing the initial code they were separate entities and then i
    ///     left them as is.
    /// </summary>
    /// <returns>A DataTable with the complete list of tags stored in the database w/o filter.</returns>
    private static DataTable DataReadSQLiteObjectMappingTagsToPass()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + Path.Combine(path1: HelperVariables.ResourcesFolderPath, path2: "objectMapping.sqlite"));
        sqliteDB.Open();

        string sqlCommandStr = @"
                                SELECT DISTINCT objectTagName_In AS objectattribute_ToPass
                                FROM objectTagNames_In
                                UNION 
                                SELECT DISTINCT objectTagName_Out AS objectattribute_ToPass
                                FROM objectTagNames_Out
                                ORDER BY 1;
                                "
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        SQLiteDataReader reader = sqlToRun.ExecuteReader();
        DataTable dataTable = new();
        dataTable.Load(reader: reader);
        return dataTable;
    }
}