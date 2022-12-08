using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
    #region Database Creation SQL & Startup Checks

    /// <summary>
    ///     Creates the SQLite DB if it doesn't exist yet
    /// </summary>
    internal static void DataCreateSQLiteDB()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        try
        {
            // create folder in Appdata if doesn't exist
            string sqldbPath = SSettingsDataBasePath;
            FrmMainApp.Logger.Trace(message: "SSettingsDataBasePath is " + SSettingsDataBasePath);
            FileInfo fi = new(fileName: SSettingsDataBasePath);

            if (fi.Exists && fi.Length == 0)
            {
                FrmMainApp.Logger.Trace(message: "SSettingsDataBasePath exists");
                fi.Delete();
                FrmMainApp.Logger.Trace(message: "SSettingsDataBasePath deleted");
            }

            if (!fi.Exists)
            {
                FrmMainApp.Logger.Trace(message: "Creating " + SSettingsDataBasePath);
                try
                {
                    SQLiteConnection.CreateFile(databaseFileName: Path.Combine(SSettingsDataBasePath));
                    SQLiteConnection sqliteDB = new(connectionString: @"Data Source=" + Path.Combine(SSettingsDataBasePath) + "; Version=3");
                    sqliteDB.Open();

                    string sql = """
                            CREATE TABLE settings(
                                settingTabPage TEXT(255)    NOT NULL,
                                settingId TEXT(255)         NOT NULL, 
                                settingValue NTEXT(2000)    DEFAULT "",
                                PRIMARY KEY([settingTabPage], [settingId])
                            );
                            CREATE TABLE settingsToWritePreQueue(
                                settingTabPage TEXT(255)    NOT NULL,
                                settingId TEXT(255)         NOT NULL, 
                                settingValue NTEXT(2000)    DEFAULT "",
                                PRIMARY KEY([settingTabPage], [settingId])
                            );
                            CREATE TABLE appLayout(
                                settingTabPage TEXT(255)    NOT NULL,
                                settingId TEXT(255)         NOT NULL, 
                                settingValue NTEXT(2000)    DEFAULT "",
                                PRIMARY KEY([settingTabPage], [settingId])
                            );
                            DROP TABLE IF EXISTS [toponymyData];
                            CREATE TABLE [toponymyData](
                                        [Lat] DECIMAL(19, 6) NOT NULL, 
                                        [Lng] DECIMAL(19, 6) NOT NULL, 
                                        [AdminName1] NTEXT, 
                                        [AdminName2] NTEXT, 
                                        [ToponymName] NTEXT, 
                                        [CountryCode] NTEXT,
                                        [timezoneId] NTEXT,
                                        PRIMARY KEY([Lat], [Lng]))
                            ;
                            DROP TABLE IF EXISTS [altitudeData];
                            CREATE TABLE [altitudeData](
                                        [Lat] DECIMAL(19, 6) NOT NULL, 
                                        [Lng] DECIMAL(19, 6) NOT NULL,
                                        [Srtm1] NTEXT, 
                                        PRIMARY KEY([Lat], [Lng]))
                            ;
                            """;
                    SQLiteCommand sqlCommandStr = new(commandText: sql, connection: sqliteDB);
                    sqlCommandStr.ExecuteNonQuery();
                    sqliteDB.Close();
                }
                catch (Exception ex)
                {
                    FrmMainApp.Logger.Fatal(message: "Error: " + ex.Message);
                    MessageBox.Show(text: ex.Message);
                }
            }
            else
            {
                DataDeleteSQLiteToponomy();
                DataDeleteSQLiteAltitude();
            }
        }
        catch (Exception ex)
        {
            FrmMainApp.Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: ex.Message);
        }
    }

    /// <summary>
    ///     Fills the SQLite database with defaults (such as file-type-specific settings
    /// </summary>
    internal static void DataWriteSQLiteSettingsDefaultSettings()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        string[] controlNamesToAdd =
        {
            "ckb_AddXMPSideCar",
            "ckb_OverwriteOriginal",
            "ckb_ProcessOriginalFile",
            "ckb_ResetFileDateToCreated"
        };
        string existingSQLVal;

        // extension-specific
        foreach (string controlName in controlNamesToAdd)
        {
            foreach (string ext in AncillaryListsArrays.AllCompatibleExtensions())
            {
                string tmptmpCtrlName = ext.Split('\t')
                                            .FirstOrDefault() +
                                        '_'; // 'tis ok as is
                string tmpCtrlName = tmptmpCtrlName + controlName;
                string tmpCtrlGroup = ext.Split('\t')
                    .Last()
                    .ToLower();
                string tmpVal = "false";

                if (controlName == "ckb_AddXMPSideCar")
                {
                    if (tmpCtrlGroup.Contains(value: "raw") || tmpCtrlGroup.Contains(value: "tiff"))
                    {
                        tmpVal = "true";
                    }
                    else
                    {
                        tmpVal = "false";
                    }
                }
                else if (controlName == "ckb_ProcessOriginalFile")
                {
                    tmpVal = "true";
                    //if (tmpCtrlGroup.Contains(value: "raw") || tmpCtrlGroup.Contains(value: "tiff"))
                    //{
                    //    tmpVal = "false";
                    //}
                    //else
                    //{
                    //    tmpVal = "true";
                    //}
                }

                else if (controlName == "ckb_ResetFileDateToCreated")
                {
                    if (tmpCtrlGroup.Contains(value: "raw") || tmpCtrlGroup.Contains(value: "tiff"))
                    {
                        tmpVal = "true";
                    }
                    else
                    {
                        tmpVal = "false";
                    }
                }

                else if (controlName == "ckb_OverwriteOriginal")
                {
                    tmpVal = "true";
                }

                existingSQLVal = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: tmpCtrlName);
                if (existingSQLVal == "" || existingSQLVal is null)
                {
                    DataWriteSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: tmpCtrlName, settingValue: tmpVal);
                }
            }
        }

        // language
        existingSQLVal = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "cbx_Language");
        if (existingSQLVal == "" || existingSQLVal is null)
        {
            DataWriteSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "cbx_Language", settingValue: "English");
        }
    }

    #endregion

    #region objectMapping SQL

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
            using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "objectMapping.sqlite"));
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

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "objectMapping.sqlite"));
        sqliteDB.Open();

        string sqlCommandStr = @"
                                SELECT DISTINCT objectTagName_In AS objectTagName_ToPass
                                FROM objectTagNames_In
                                UNION 
                                SELECT DISTINCT objectTagName_Out AS objectTagName_ToPass
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

    #endregion

    #region Settings SQL

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

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
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
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
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
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                REPLACE INTO settings (settingTabPage, settingId, settingValue) " +
                               "SELECT settingTabPage, settingId, settingValue FROM settingsToWritePreQueue;"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Deletes the data in the "write queue" table. Gets executed if the user presses ok/cancel on the settings form.
    ///     Obvs if they press OK then DataTransferSQLiteSettings fires first.
    /// </summary>
    internal static void DataDeleteSQLitesettingsToWritePreQueue()
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
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
    ///     Reads a single value (the ARCGIS key) from Settings. Admittedly redundant. Remnant of the early design. Should be
    ///     changed into something more efficient.
    /// </summary>
    /// <returns>The ARCGIS API key value from Settings</returns>
    internal static string DataSelectTbxARCGIS_APIKey_FromSQLite()
    {
        try
        {
            SArcGisApiKey = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_ARCGIS_APIKey");
            if (SArcGisApiKey == null || SArcGisApiKey == "")
            {
                //MessageBox.Show(HelperStatic.GenericGetMessageBoxText("mbx_Helper_WarningNoARCGISKey"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch
        {
            SArcGisApiKey = "";
        }

        return SArcGisApiKey;
    }

    #endregion

    #region language SQL

    /// <summary>
    ///     Reads the language value of a specific item from the database.
    /// </summary>
    /// <param name="languageName">e.g "English"</param>
    /// <param name="objectType">e.g. "button" or "columnheader"</param>
    /// <param name="objectName">This is the name of the object e.g. "btn_OK"</param>
    /// <returns>The value of the object's labal in the given language. E.g. for btn_Cancel this will be "Cancel"</returns>
    internal static string DataReadSQLiteObjectText(string languageName,
                                                    string objectType,
                                                    string objectName)
    {
        string returnString = "";
        string languagesFolderPath = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "Languages");
        string languageFilePath = Path.Combine(path1: languagesFolderPath, path2: languageName + ".sqlite");
        string englishLanguagefilePath = Path.Combine(path1: languagesFolderPath, path2: "english.sqlite");
        using (SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + languageFilePath))
        {
            sqliteDB.Open();

            string sqlCommandStr = @"
                                SELECT objectText
                                FROM " +
                                   objectType +
                                   " " +
                                   @"WHERE 1=1
                                    AND objectName = @objectName
                                LIMIT 1
                                ;"
                ;
            SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
            sqlToRun.Parameters.AddWithValue(parameterName: "@objectName", value: objectName);

            using SQLiteDataReader reader = sqlToRun.ExecuteReader();
            while (reader.Read())
            {
                returnString = reader.GetString(i: 0);
            }
        }

        if (returnString == "")
        {
            using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + englishLanguagefilePath);
            sqliteDB.Open();

            string sqlCommandStr = @"
                                SELECT objectText
                                FROM " +
                                   objectType +
                                   " " +
                                   @"WHERE 1=1
                                    AND objectName = @objectName
                                LIMIT 1
                                ;"
                ;
            SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
            sqlToRun.Parameters.AddWithValue(parameterName: "@objectName", value: objectName);

            using SQLiteDataReader reader = sqlToRun.ExecuteReader();
            while (reader.Read())
            {
                returnString = reader.GetString(i: 0);
            }
        }

        return returnString;
    }

    /// <summary>
    ///     Generally identical to the above but with an "actionType" - basically a parameter/breakdown.
    /// </summary>
    /// <param name="languageName">e.g "English"</param>
    /// <param name="objectType">e.g. "button" or "columnheader"</param>
    /// <param name="actionType">e.g. "reading" or "writing". </param>
    /// <param name="objectName">This is the name of the object e.g. "btn_OK"</param>
    /// <returns>The value of the object's labal in the given language. E.g. for btn_Cancel this will be "Cancel"</returns>
    internal static string DataReadSQLiteObjectText(string languageName,
                                                    string objectType,
                                                    string actionType,
                                                    string objectName)
    {
        string returnString = "";
        string resourcesFolderPath = FrmMainApp.ResourcesFolderPath;
        string languagesFolderPath = Path.Combine(path1: resourcesFolderPath, path2: "Languages");

        string languageFilePath = Path.Combine(path1: languagesFolderPath, path2: languageName + ".sqlite");
        string englishLanguagefilePath = Path.Combine(path1: languagesFolderPath, path2: "english.sqlite");
        using (SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + languageFilePath))
        {
            sqliteDB.Open();

            string sqlCommandStr = @"
                                SELECT objectText
                                FROM " +
                                   objectType +
                                   " " +
                                   @"WHERE 1=1
                                    AND objectName = @objectName
                                    AND actionType = @actionType
                                LIMIT 1
                                ;"
                ;
            SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
            sqlToRun.Parameters.AddWithValue(parameterName: "@actionType", value: actionType);
            sqlToRun.Parameters.AddWithValue(parameterName: "@objectName", value: objectName);

            using SQLiteDataReader reader = sqlToRun.ExecuteReader();
            while (reader.Read())
            {
                returnString = reader.GetString(i: 0);
            }
        }

        if (returnString == "")
        {
            using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + englishLanguagefilePath);
            sqliteDB.Open();

            string sqlCommandStr = @"
                                SELECT objectText
                                FROM " +
                                   objectType +
                                   " " +
                                   @"WHERE 1=1
                                    AND objectName = @objectName
                                    AND actionType = @actionType
                                LIMIT 1
                                ;"
                ;
            SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
            sqlToRun.Parameters.AddWithValue(parameterName: "@actionType", value: actionType);
            sqlToRun.Parameters.AddWithValue(parameterName: "@objectName", value: objectName);

            using SQLiteDataReader reader = sqlToRun.ExecuteReader();
            while (reader.Read())
            {
                returnString = reader.GetString(i: 0);
            }
        }

        return returnString;
    }

    #endregion

    #region Toponomy SQL

    /// <summary>
    ///     Attempts to read SQLite to see if the toponomy data exists. The idea is that a particular location on the planet
    ///     isn't likely to change so ...
    ///     ... it's silly to keep querying the API each time for the same thing. I've explained in the readme but the API I
    ///     use doesn't really follow political changes...
    ///     ... as of 2022 for example it still returns Ukraine for Crimea.
    /// </summary>
    /// <param name="lat">
    ///     latitude/longitude. String values; were initially numeric but this caused problems on non-English
    ///     systems and strings are easier to coerce to work.
    /// </param>
    /// <param name="lng">
    ///     latitude/longitude. String values; were initially numeric but this caused problems on non-English
    ///     systems and strings are easier to coerce to work.
    /// </param>
    /// <returns>DataTable with toponomy data if exists</returns>
    private static DataTable DataReadSQLiteToponomyWholeRow(string lat,
                                                            string lng)
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                SELECT *
                                FROM toponymyData
                                WHERE 1=1
									AND lat = @lat
									AND lng = @lng
                                ;
								"
            ;
        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@lat", value: lat);
        sqlToRun.Parameters.AddWithValue(parameterName: "@lng", value: lng);
        SQLiteDataReader reader = sqlToRun.ExecuteReader();
        DataTable dataTable = new();
        dataTable.Load(reader: reader);
        return dataTable;
    }

    /// <summary>
    ///     Identical to the above but for altitude data.
    /// </summary>
    /// <param name="lat">
    ///     latitude/longitude. String values; were initially numeric but this caused problems on non-English
    ///     systems and strings are easier to coerce to work.
    /// </param>
    /// <param name="lng">
    ///     latitude/longitude. String values; were initially numeric but this caused problems on non-English
    ///     systems and strings are easier to coerce to work.
    /// </param>
    /// <returns>DataTable with toponomy data if exists</returns>
    private static DataTable DataReadSQLiteAltitudeWholeRow(string lat,
                                                            string lng)
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                SELECT *
                                FROM altitudeData
                                WHERE 1=1
									AND lat = @lat
									AND lng = @lng
                                ;
								"
            ;
        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@lat", value: lat);
        sqlToRun.Parameters.AddWithValue(parameterName: "@lng", value: lng);
        SQLiteDataReader reader = sqlToRun.ExecuteReader();
        DataTable dataTable = new();
        dataTable.Load(reader: reader);
        return dataTable;
    }

    /// <summary>
    ///     Writes topopnomy data from the API to SQL for future use. I explained the logic above at
    ///     DataReadSQLiteToponomyWholeRow.
    /// </summary>
    /// <param name="lat">
    ///     latitude/longitude. String values; were initially numeric but this caused problems on non-English
    ///     systems and strings are easier to coerce to work.
    /// </param>
    /// <param name="lng">
    ///     latitude/longitude. String values; were initially numeric but this caused problems on non-English
    ///     systems and strings are easier to coerce to work.
    /// </param>
    /// <param name="AdminName1">API Response for this particular tag.</param>
    /// <param name="AdminName2">API Response for this particular tag.</param>
    /// <param name="ToponymName">API Response for this particular tag.</param>
    /// <param name="CountryCode">API Response for this particular tag.</param>
    private static void DataWriteSQLiteToponomyWholeRow(string lat,
                                                        string lng,
                                                        string AdminName1 = "",
                                                        string AdminName2 = "",
                                                        string ToponymName = "",
                                                        string CountryCode = "",
                                                        string timezoneId = ""
    )
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                REPLACE INTO toponymyData (lat, lng, AdminName1, AdminName2, ToponymName, CountryCode, timezoneId) " +
                               "VALUES (@lat, @lng, @AdminName1, @AdminName2, @ToponymName, @CountryCode, @timezoneId);"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@lat", value: lat.ToString(provider: CultureInfo.InvariantCulture));
        sqlToRun.Parameters.AddWithValue(parameterName: "@lng", value: lng.ToString(provider: CultureInfo.InvariantCulture));
        sqlToRun.Parameters.AddWithValue(parameterName: "@AdminName1", value: AdminName1);
        sqlToRun.Parameters.AddWithValue(parameterName: "@AdminName2", value: AdminName2);
        sqlToRun.Parameters.AddWithValue(parameterName: "@ToponymName", value: ToponymName);
        sqlToRun.Parameters.AddWithValue(parameterName: "@CountryCode", value: CountryCode);
        sqlToRun.Parameters.AddWithValue(parameterName: "@timezoneId", value: timezoneId);

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Clears the data from the Toponomy table. Run at session start.
    ///     This was changed from a simple DELETE FROM because timezoneId had been added as of 20221128
    /// </summary>
    private static void DataDeleteSQLiteToponomy()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                DROP TABLE IF EXISTS [toponymyData];
                                CREATE TABLE [toponymyData](
                                            [Lat] DECIMAL(19, 6) NOT NULL, 
                                            [Lng] DECIMAL(19, 6) NOT NULL, 
                                            [AdminName1] NTEXT, 
                                            [AdminName2] NTEXT, 
                                            [ToponymName] NTEXT, 
                                            [CountryCode] NTEXT,
                                            [timezoneId] NTEXT,
                                            PRIMARY KEY([Lat], [Lng]))
                                ;"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Clears the data from the Altitude table. Run at session start.
    /// </summary>
    private static void DataDeleteSQLiteAltitude()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                DELETE FROM altitudeData;"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Identical to the above but for altitude data
    /// </summary>
    /// <param name="lat">
    ///     latitude/longitude. String values; were initially numeric but this caused problems on non-English
    ///     systems and strings are easier to coerce to work.
    /// </param>
    /// <param name="lng">
    ///     latitude/longitude. String values; were initially numeric but this caused problems on non-English
    ///     systems and strings are easier to coerce to work.
    /// </param>
    /// <param name="Altitude">API Response for this particular tag.</param>
    private static void DataWriteSQLiteAltitudeWholeRow(string lat,
                                                        string lng,
                                                        string Altitude = "")
    {
        FrmMainApp.Logger.Trace(message: "Starting - lat: " + lat + " lng: " + lng + " Altitude: " + Altitude);
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                REPLACE INTO altitudeData (lat, lng, Srtm1) " +
                               "VALUES (@lat, @lng, @Altitude);"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@lat", value: lat.ToString(provider: CultureInfo.InvariantCulture));
        sqlToRun.Parameters.AddWithValue(parameterName: "@lng", value: lng.ToString(provider: CultureInfo.InvariantCulture));
        sqlToRun.Parameters.AddWithValue(parameterName: "@Altitude", value: Altitude.ToString(provider: CultureInfo.InvariantCulture));

        sqlToRun.ExecuteNonQuery();
    }

    #endregion

    #region Other SQL

    /// <summary>
    ///     Reads SQL and basically translates between code types. We store ALPHA-2, ALPHA-3 and plain English country names.
    /// </summary>
    /// <param name="queryWhat">ALPHA-2, ALPHA-3 and plain English country names</param>
    /// <param name="inputVal">e.g US or USA or United States of America</param>
    /// <param name="returnWhat">ALPHA-2, ALPHA-3 and plain English country names</param>
    /// <returns>ALPHA-2, ALPHA-3 and plain English country names</returns>
    internal static string DataReadSQLiteCountryCodesNames(string queryWhat,
                                                           string inputVal,
                                                           string returnWhat)
    {
        string returnString = "";
        // need to account for lack of data actually
        if (inputVal != " ")
        {
            string countryCodeSQLiteFilePath = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "isoCountryCodeMapping.sqlite");
            using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + countryCodeSQLiteFilePath);
            sqliteDB.Open();

            string sqlCommandStr = @"
                                SELECT " +
                                   returnWhat +
                                   "\n " +
                                   "FROM isoCountryCodeMapping \n" +
                                   "WHERE 1=1 \n" +
                                   "AND " +
                                   queryWhat +
                                   "= '" +
                                   inputVal +
                                   "' \n " +
                                   "LIMIT 1;"
                ;
            SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

            using SQLiteDataReader reader = sqlToRun.ExecuteReader();
            while (reader.Read())
            {
                returnString = reader.GetString(i: 0);
            }
        }

        return returnString;
    }

    /// <summary>
    ///     Does a filter on a DataTable - just faster.
    ///     via https://stackoverflow.com/a/47692754/3968494
    /// </summary>
    /// <param name="dt">DataTable to query</param>
    /// <param name="filePathColumnName">The "column" part of WHERE</param>
    /// <param name="filePathValue">The "value" part of WHERE</param>
    /// <returns>List of KVP String/String</returns>
    internal static List<KeyValuePair<string, string>> DataReadFilterDataTable(DataTable dt,
                                                                               string filePathColumnName,
                                                                               string filePathValue)
    {
        EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in dt.AsEnumerable()
                                                           where dataRow.Field<string>(columnName: filePathColumnName) == filePathValue
                                                           select dataRow;
        List<KeyValuePair<string, string>> lstReturn = new();

        Parallel.ForEach(source: drDataTableData, body: dataRow =>
            {
                string settingId = dataRow[columnName: "settingId"]
                    .ToString();
                string settingValue = dataRow[columnName: "settingValue"]
                    .ToString();
                lstReturn.Add(item: new KeyValuePair<string, string>(key: settingId, value: settingValue));
            })
            ;
        return lstReturn;
    }

    /// <summary>
    ///     Gets the "FirstOrDefault" from a List of KVP
    /// </summary>
    /// <param name="lstIn">List (KVP) to check</param>
    /// <param name="keyEqualsWhat">Key filter</param>
    /// <returns>String of Value</returns>
    internal static string DataGetFirstOrDefaultFromKVPList(List<KeyValuePair<string, string>> lstIn,
                                                            string keyEqualsWhat)
    {
        return lstIn.FirstOrDefault(predicate: kvp => kvp.Key == keyEqualsWhat)
            .Value;
    }

    #endregion
}