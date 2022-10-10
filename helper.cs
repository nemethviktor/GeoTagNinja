using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace GeoTagNinja
{
    internal static class Helper
    {
        #region variables
        internal static string s_ArcGIS_APIKey;
        internal static string s_GeoNames_UserName;
        internal static string s_GeoNames_Pwd;
        internal static string s_settingsDataBasePath = Path.Combine(frm_MainApp.userDataFolderPath, "database.sqlite");
        internal static bool s_changeFolderIsOkay = false;
        internal static bool s_APIOkay = true;
        private static string s_ErrorMsg = "";
        private static readonly string s_doubleQuote = "\"";
        internal static bool s_ResetMapToZero;

        #endregion
        #region SQL 
        #region Database Creation SQL & Startup Checks
        /// <summary>
        /// Creates the SQLite DB if it doesn't exist yet
        /// </summary>
        internal static void DataCreateSQLiteDB()
        {
            try
            {
                // create folder in Appdata if doesn't exist

                FileInfo fi = new(s_settingsDataBasePath);

                if (fi.Exists && fi.Length == 0)
                {
                    fi.Delete();
                }

                string sqldbPath = s_settingsDataBasePath;
                if (!fi.Exists)
                {
                    try
                    {
                        SQLiteConnection.CreateFile(Path.Combine(s_settingsDataBasePath));
                        SQLiteConnection sqliteDB = new(@"Data Source=" + Path.Combine(s_settingsDataBasePath) + "; Version=3");
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
                            CREATE TABLE [toponymyData](
                                        [Lat] DECIMAL(19, 6) NOT NULL, 
                                        [Lng] DECIMAL(19, 6) NOT NULL, 
                                        [AdminName1] NTEXT, 
                                        [AdminName2] NTEXT, 
                                        [ToponymName] NTEXT, 
                                        [CountryCode] NTEXT,
                                        PRIMARY KEY([Lat], [Lng]))
                            ;
                            CREATE TABLE [altitudeData](
                                        [Lat] DECIMAL(19, 6) NOT NULL, 
                                        [Lng] DECIMAL(19, 6) NOT NULL,
                                        [Srtm1] NTEXT, 
                                        PRIMARY KEY([Lat], [Lng]))
                            ;
                            """;
                        SQLiteCommand sqlCommandStr = new(sql, sqliteDB);
                        sqlCommandStr.ExecuteNonQuery();
                        sqliteDB.Close();

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }
        /// <summary>
        /// Fills the SQLite database with defaults (such as file-type-specific settings
        /// </summary>
        internal static void DataWriteSQLiteSettingsDefaultSettings()
        {
            string[] controlNamesToAdd = new string[] {
                "ckb_AddXMPIntoFile",
                "ckb_AddXMPSideCar",
                "ckb_OverwriteOriginal"
            };
            string existingSQLVal;

            // extension-specific
            foreach (string controlName in controlNamesToAdd)
            {
                foreach (string ext in frm_MainApp.allExtensions)
                {
                    string tmptmpCtrlName = ext.Split('\t').First() + '_'; // 'tis ok as is
                    string tmpCtrlName = tmptmpCtrlName + controlName;
                    string tmpCtrlGroup = ext.Split('\t').Last().ToLower();
                    string tmpVal = "false";
                    if (controlName == "ckb_AddXMPIntoFile")
                    {
                        tmpVal = "true";
                    }

                    else if (controlName == "ckb_AddXMPSideCar")
                    {
                        if (tmpCtrlGroup.Contains("raw") || tmpCtrlGroup.Contains("tiff"))
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
        /// Reads the ObjectMapping data - basically this is to associate (exif) tags with column header names and button names
        /// The logic is that if we have for example an exif tag "FValue" then the column header representing that will be also "clh_FValue" etc
        /// This also pulls tags "in and out" associations. Admittedly I don't always grasp the myriad of exif tags and their logic so ...
        /// ... in the code some of this will be merged elsewhere.
        /// </summary>
        /// <param name="tableName">Can be the objectNames_In or _Out table </param>
        /// <param name="orderBy">A priority-order. Defaults to the first column (alpha)</param>
        /// <returns>A DataTable with the SQL data.</returns>
        internal static DataTable DataReadSQLiteObjectMapping(string tableName, string orderBy = "1")
        {
            try
            {
                using SQLiteConnection sqliteDB = new("Data Source=" + Path.Combine(frm_MainApp.resourcesFolderPath, "objectMapping.sqlite"));
                sqliteDB.Open();

                string sqlCommandStr = @"
                                SELECT *
                                FROM " + tableName + " " +
                                "ORDER BY @orderBy;"
                                ;

                SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
                sqlToRun.Parameters.AddWithValue("@orderBy", orderBy);
                SQLiteDataReader reader = sqlToRun.ExecuteReader();
                var dataTable = new DataTable();
                dataTable.Load(reader);
                return dataTable;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }
        /// <summary>
        /// Reads the ObjectMappingData for the purposes of passing it to exifTool. Admittedly this could be merged with the one above ...
        /// ... or replace it altogether but at the time of writing the initial code they were separate entities and then i left them as is.
        /// </summary>
        /// <returns>A DataTable with the complete list of tags stored in the database w/o filter.</returns>
        internal static DataTable DataReadSQLiteObjectMappingTagsToPass()
        {
            using SQLiteConnection sqliteDB = new("Data Source=" + Path.Combine(frm_MainApp.resourcesFolderPath, "objectMapping.sqlite"));
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

            SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
            SQLiteDataReader reader = sqlToRun.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);
            return dataTable;
        }
        #endregion
        #region Settings SQL
        /// <summary>
        /// Reads the user-settings and returns them to the app (such as say default starting folder.)
        /// </summary>
        /// <param name="tableName">This will generally be "settings" (but could be applayout as well). Remainder of an older design where I had tables for data lined up to be saved</param>
        /// <param name="settingTabPage">This lines up with the tab name on the Settings form</param>
        /// <param name="settingId">Name of the SettingID for which data is requested</param>
        /// <returns>String - the value of the given SettingID</returns>
        internal static string DataReadSQLiteSettings(string tableName, string settingTabPage, string settingId)
        {
            string? returnString = null;

            using (SQLiteConnection sqliteDB = new("Data Source=" + s_settingsDataBasePath))
            {
                sqliteDB.Open();

                string sqlCommandStr = @"
                                SELECT settingValue
                                FROM " + tableName + " " +
                                @"WHERE 1=1
                                    AND settingId = @settingId
                                    AND settingTabPage = @settingTabPage
;"
                                ;
                SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
                sqlToRun.Parameters.AddWithValue("@settingTabPage", settingTabPage);
                sqlToRun.Parameters.AddWithValue("@settingId", settingId);

                using var reader = sqlToRun.ExecuteReader();
                while (reader.Read())
                {
                    returnString = reader.GetString(0);
                }
            }

            return returnString;
        }
        /// <summary>
        /// Similar to the one above (which reads the data) - this one writes it.
        /// </summary>
        /// <param name="tableName">This will generally be "settings" (but could be applayout as well). Remainder of an older design where I had tables for data lined up to be saved</param>
        /// <param name="settingTabPage">This lines up with the tab name on the Settings form</param>
        /// <param name="settingId">Name of the SettingID for which data is requested</param>
        /// <param name="settingValue">The value to be stored.</param>
        internal static void DataWriteSQLiteSettings(string tableName, string settingTabPage, string settingId, string settingValue)
        {
            using SQLiteConnection sqliteDB = new("Data Source=" + s_settingsDataBasePath);
            sqliteDB.Open();

            string sqlCommandStrand = @"
                                REPLACE INTO " + tableName + " (settingTabPage, settingId, settingValue) " +
                            "VALUES (@settingTabPage, @settingId, @settingValue);"
                            ;

            SQLiteCommand sqlCommandStr = new(sqlCommandStrand, sqliteDB);
            sqlCommandStr.Parameters.AddWithValue("@settingTabPage", settingTabPage);
            sqlCommandStr.Parameters.AddWithValue("@settingId", settingId);
            sqlCommandStr.Parameters.AddWithValue("@settingValue", settingValue);
            sqlCommandStr.ExecuteNonQuery();
        }
        /// <summary>
        /// Transfers data from the "write queue" to the actual table. This is executed when the user presses the OK button in settings...
        /// ... until then the data is kept in the pre-queue table. ...
        /// The "main" table (file data table) used to have the same logic but that was converted to in-memory DataTables as writing up to thousands of tags...
        /// ... was very inefficient. Since settings only write a few tags I didn't bother doing the same. Boo me.
        /// </summary>
        internal static void DataTransferSQLiteSettings()
        {
            using SQLiteConnection sqliteDB = new("Data Source=" + s_settingsDataBasePath);
            sqliteDB.Open();

            string sqlCommandStr = @"
                                REPLACE INTO settings (settingTabPage, settingId, settingValue) " +
                            "SELECT settingTabPage, settingId, settingValue FROM settingsToWritePreQueue;"
                            ;

            SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
            sqlToRun.ExecuteNonQuery();
        }
        /// <summary>
        /// Deletes the data in the "write queue" table. Gets executed if the user presses ok/cancel on the settings form.
        /// Obvs if they press OK then DataTransferSQLiteSettings fires first.
        /// </summary>
        internal static void DataDeleteSQLitesettingsToWritePreQueue()
        {
            using SQLiteConnection sqliteDB = new("Data Source=" + s_settingsDataBasePath);
            sqliteDB.Open();

            string sqlCommandStr = @"
                                DELETE
                                FROM settingsToWritePreQueue
                                WHERE 1=1
                                    
                                
                                "
                            ;
            sqlCommandStr += ";";
            SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
            sqlToRun.ExecuteNonQuery();
        }
        /// <summary>
        /// Reads a single value (the ARCGIS key) from Settings. Admittedly redundant. Remnant of the early design. Should be changed into something more efficient.
        /// </summary>
        /// <returns>The ARCGIS API key value from Settings</returns>
        internal static string DataSelectTbxARCGIS_APIKey_FromSQLite()
        {
            try
            {
                s_ArcGIS_APIKey = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_ARCGIS_APIKey");
                if (s_ArcGIS_APIKey == null || s_ArcGIS_APIKey == "")
                {
                    //MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_WarningNoARCGISKey"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch
            {
                s_ArcGIS_APIKey = "";
            }
            return s_ArcGIS_APIKey;
        }
        #endregion
        #region language SQL
        /// <summary>
        /// Reads the language value of a specific item from the database.
        /// </summary>
        /// <param name="languageName">e.g "English"</param>
        /// <param name="objectType">e.g. "button" or "columnheader"</param>
        /// <param name="objectName">This is the name of the object e.g. "btn_OK"</param>
        /// <returns>The value of the object's labal in the given language. E.g. for btn_Cancel this will be "Cancel"</returns>
        internal static string DataReadSQLiteObjectText(string languageName, string objectType, string objectName)
        {
            string returnString = "";
            string languagesFolderPath = Path.Combine(frm_MainApp.resourcesFolderPath, "Languages");
            string languageFilePath = Path.Combine(languagesFolderPath, languageName + ".sqlite");
            string englishLanguagefilePath = Path.Combine(languagesFolderPath, "english.sqlite");
            using (SQLiteConnection sqliteDB = new("Data Source=" + languageFilePath))
            {
                sqliteDB.Open();

                string sqlCommandStr = @"
                                SELECT objectText
                                FROM " + objectType + " " +
                                @"WHERE 1=1
                                    AND objectName = @objectName
                                LIMIT 1
                                ;"
                                ;
                SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
                sqlToRun.Parameters.AddWithValue("@objectName", objectName);

                using var reader = sqlToRun.ExecuteReader();
                while (reader.Read())
                {
                    returnString = reader.GetString(0);
                }
            }

            if (returnString == "")
            {
                using SQLiteConnection sqliteDB = new("Data Source=" + englishLanguagefilePath);
                sqliteDB.Open();

                string sqlCommandStr = @"
                                SELECT objectText
                                FROM " + objectType + " " +
                                @"WHERE 1=1
                                    AND objectName = @objectName
                                LIMIT 1
                                ;"
                                ;
                SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
                sqlToRun.Parameters.AddWithValue("@objectName", objectName);

                using var reader = sqlToRun.ExecuteReader();
                while (reader.Read())
                {
                    returnString = reader.GetString(0);
                }
            }

            return returnString;
        }
        /// <summary>
        /// Generally identical to the above but with an "actionType" - basically a parameter/breakdown.
        /// </summary>
        /// <param name="languageName">e.g "English"</param>
        /// <param name="objectType">e.g. "button" or "columnheader"</param>
        /// <param name="actionType">e.g. "reading" or "writing". </param>
        /// <param name="objectName">This is the name of the object e.g. "btn_OK"</param>
        /// <returns>The value of the object's labal in the given language. E.g. for btn_Cancel this will be "Cancel"</returns>
        internal static string DataReadSQLiteObjectText(string languageName, string objectType, string actionType, string objectName)
        {
            string returnString = "";
            string resourcesFolderPath = frm_MainApp.resourcesFolderPath;
            string languagesFolderPath = Path.Combine(resourcesFolderPath, "Languages");

            string languageFilePath = Path.Combine(languagesFolderPath, languageName + ".sqlite");
            string englishLanguagefilePath = Path.Combine(languagesFolderPath, "english.sqlite");
            using (SQLiteConnection sqliteDB = new("Data Source=" + languageFilePath))
            {
                sqliteDB.Open();

                string sqlCommandStr = @"
                                SELECT objectText
                                FROM " + objectType + " " +
                                @"WHERE 1=1
                                    AND objectName = @objectName
                                    AND actionType = @actionType
                                LIMIT 1
                                ;"
                                ;
                SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
                sqlToRun.Parameters.AddWithValue("@actionType", actionType);
                sqlToRun.Parameters.AddWithValue("@objectName", objectName);

                using var reader = sqlToRun.ExecuteReader();
                while (reader.Read())
                {
                    returnString = reader.GetString(0);
                }
            }

            if (returnString == "")
            {
                using SQLiteConnection sqliteDB = new("Data Source=" + englishLanguagefilePath);
                sqliteDB.Open();

                string sqlCommandStr = @"
                                SELECT objectText
                                FROM " + objectType + " " +
                                @"WHERE 1=1
                                    AND objectName = @objectName
                                    AND actionType = @actionType
                                LIMIT 1
                                ;"
                                ;
                SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
                sqlToRun.Parameters.AddWithValue("@actionType", actionType);
                sqlToRun.Parameters.AddWithValue("@objectName", objectName);

                using var reader = sqlToRun.ExecuteReader();
                while (reader.Read())
                {
                    returnString = reader.GetString(0);
                }
            }

            return returnString;
        }
        #endregion
        #region Toponomy 
        /// <summary>
        /// Attempts to read SQLite to see if the toponomy data exists. The idea is that a particular location on the planet isn't likely to change so ...
        /// ... it's silly to keep querying the API each time for the same thing. I've explained in the readme but the API I use doesn't really follow political changes...
        /// ... as of 2022 for example it still returns Ukraine for Crimea. 
        /// </summary>
        /// <param name="lat">latitude/longitude. String values; were initially numeric but this caused problems on non-English systems and strings are easier to coerce to work.</param>
        /// <param name="lng">latitude/longitude. String values; were initially numeric but this caused problems on non-English systems and strings are easier to coerce to work.</param>
        /// <returns>DataTable with toponomy data if exists</returns>
        internal static DataTable DataReadSQLiteToponomyWholeRow(string lat, string lng)
        {
            using SQLiteConnection sqliteDB = new("Data Source=" + s_settingsDataBasePath);
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
            SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
            sqlToRun.Parameters.AddWithValue("@lat", lat);
            sqlToRun.Parameters.AddWithValue("@lng", lng);
            SQLiteDataReader reader = sqlToRun.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);
            return dataTable;
        }
        /// <summary>
        /// Identical to the above but for altitude data.
        /// </summary>
        /// <param name="lat">latitude/longitude. String values; were initially numeric but this caused problems on non-English systems and strings are easier to coerce to work.</param>
        /// <param name="lng">latitude/longitude. String values; were initially numeric but this caused problems on non-English systems and strings are easier to coerce to work.</param>
        /// <returns>DataTable with toponomy data if exists</returns>
        internal static DataTable DataReadSQLiteAltitudeWholeRow(string lat, string lng)
        {
            using SQLiteConnection sqliteDB = new("Data Source=" + s_settingsDataBasePath);
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
            SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
            sqlToRun.Parameters.AddWithValue("@lat", lat);
            sqlToRun.Parameters.AddWithValue("@lng", lng);
            SQLiteDataReader reader = sqlToRun.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);
            return dataTable;
        }
        /// <summary>
        /// Writes topopnomy data from the API to SQL for future use. I explained the logic above at DataReadSQLiteToponomyWholeRow.
        /// </summary>
        /// <param name="lat">latitude/longitude. String values; were initially numeric but this caused problems on non-English systems and strings are easier to coerce to work.</param>
        /// <param name="lng">latitude/longitude. String values; were initially numeric but this caused problems on non-English systems and strings are easier to coerce to work.</param>
        /// <param name="AdminName1">API Response for this particular tag.</param>
        /// <param name="AdminName2">API Response for this particular tag.</param>
        /// <param name="ToponymName">API Response for this particular tag.</param>
        /// <param name="CountryCode">API Response for this particular tag.</param>
        internal static void DataWriteSQLiteToponomyWholeRow(string lat, string lng, string AdminName1 = "", string AdminName2 = "", string ToponymName = "", string CountryCode = "")
        {
            using SQLiteConnection sqliteDB = new("Data Source=" + s_settingsDataBasePath);
            sqliteDB.Open();

            string sqlCommandStr = @"
                                REPLACE INTO toponymyData (lat, lng, AdminName1, AdminName2, ToponymName, CountryCode) " +
                                "VALUES (@lat, @lng, @AdminName1, @AdminName2, @ToponymName, @CountryCode);"
                                ;

            SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
            sqlToRun.Parameters.AddWithValue("@lat", lat.ToString().Replace(',', '.'));
            sqlToRun.Parameters.AddWithValue("@lng", lng.ToString().Replace(',', '.'));
            sqlToRun.Parameters.AddWithValue("@AdminName1", AdminName1);
            sqlToRun.Parameters.AddWithValue("@AdminName2", AdminName2);
            sqlToRun.Parameters.AddWithValue("@ToponymName", ToponymName);
            sqlToRun.Parameters.AddWithValue("@CountryCode", CountryCode);

            sqlToRun.ExecuteNonQuery();
        }
        /// <summary>
        /// Identical to the above but for altitude data
        /// </summary>
        /// <param name="lat">latitude/longitude. String values; were initially numeric but this caused problems on non-English systems and strings are easier to coerce to work.</param>
        /// <param name="lng">latitude/longitude. String values; were initially numeric but this caused problems on non-English systems and strings are easier to coerce to work.</param>
        /// <param name="Altitude">API Response for this particular tag.</param>
        internal static void DataWriteSQLiteAltitudeWholeRow(string lat, string lng, string Altitude = "")
        {
            using SQLiteConnection sqliteDB = new("Data Source=" + s_settingsDataBasePath);
            sqliteDB.Open();

            string sqlCommandStr = @"
                                REPLACE INTO altitudeData (lat, lng, Srtm1) " +
                                "VALUES (@lat, @lng, @Altitude);"
                                ;

            SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);
            sqlToRun.Parameters.AddWithValue("@lat", lat.ToString().Replace(',', '.'));
            sqlToRun.Parameters.AddWithValue("@lng", lng.ToString().Replace(',', '.'));
            sqlToRun.Parameters.AddWithValue("@Altitude", Altitude);

            sqlToRun.ExecuteNonQuery();
        }
        #endregion
        #region Other SQL
        /// <summary>
        /// Reads SQL and basically translates between code types. We store ALPHA-2, ALPHA-3 and plain English country names.
        /// </summary>
        /// <param name="queryWhat">ALPHA-2, ALPHA-3 and plain English country names</param>
        /// <param name="inputVal">e.g US or USA or United States of America</param>
        /// <param name="returnWhat">ALPHA-2, ALPHA-3 and plain English country names</param>
        /// <returns>ALPHA-2, ALPHA-3 and plain English country names</returns>
        internal static string DataReadSQLiteCountryCodesNames(string queryWhat, string inputVal, string returnWhat)
        {
            string returnString = "";
            // need to account for lack of data actually
            if (inputVal != " ")
            {
                string countryCodeFilePath = Path.Combine(frm_MainApp.resourcesFolderPath, "isoCountryCodeMapping.sqlite");
                using SQLiteConnection sqliteDB = new("Data Source=" + countryCodeFilePath);
                sqliteDB.Open();

                string sqlCommandStr = @"
                                SELECT " + returnWhat + "\n " +
                                "FROM isoCountryCodeMapping \n" +
                                "WHERE 1=1 \n" +
                                    "AND " + queryWhat + "= '" + inputVal + "' \n " +
                                "LIMIT 1;"
                                ;
                SQLiteCommand sqlToRun = new(sqlCommandStr, sqliteDB);

                using (var reader = sqlToRun.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        returnString = reader.GetString(0);
                    }
                }
            }
            return returnString;
        }
        #endregion
        #endregion
        #region Generic
        /// <summary>
        /// A "coalesce" function. 
        /// </summary>
        /// <param name="strings">Array of string values to be queried</param>
        /// <returns>The first non-null value</returns>
        internal static string GenericCoalesce(params string[] strings)
        {
            return strings.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        }
        /// <summary>
        /// Wrangles the actual coordinate out of a point. (e.g. 4.54 East to -4.54)
        /// </summary>
        /// <param name="point">This is a raw coordinate. Could contain numbers or things like "East" on top of numbers</param>
        /// <returns>Double - an actual coordinate</returns>
        public static double GenericAdjustLatLongNegative(string point)
        {
            string pointOrig = point.ToString().Replace(" ", "").Replace(',', '.');
            double pointVal = double.Parse(Regex.Replace(pointOrig, "[SWNE\"-]", ""), NumberStyles.Any, CultureInfo.InvariantCulture);
            pointVal = Math.Round(pointVal, 6);
            var multiplier = (point.Contains("S") || point.Contains("W")) ? -1 : 1; //handle south and west

            return pointVal * multiplier;
        }
        /// <summary>
        /// Joins two datatables. Logically similar to a SQL join.
        /// </summary>
        /// <param name="t1">Name of input table</param>
        /// <param name="t2">Name of input table</param>
        /// <param name="joinOn">Column Name to join on</param>
        /// <returns>A joined datatable</returns>
        internal static DataTable GenericJoinDataTables(DataTable t1, DataTable t2, params Func<DataRow, DataRow, bool>[] joinOn)
        {
            // via https://stackoverflow.com/a/11505884/3968494
            // usage
            // var test = JoinDataTables(transactionInfo, transactionItems,
            // (row1, row2) =>
            // row1.Field<int>("TransactionID") == row2.Field<int>("TransactionID"));

            DataTable result = new();
            foreach (DataColumn col in t1.Columns)
            {
                if (result.Columns[col.ColumnName] == null)
                    result.Columns.Add(col.ColumnName, col.DataType);
            }
            foreach (DataColumn col in t2.Columns)
            {
                if (result.Columns[col.ColumnName] == null)
                    result.Columns.Add(col.ColumnName, col.DataType);
            }
            foreach (DataRow row1 in t1.Rows)
            {
                var joinRows = t2.AsEnumerable().Where(row2 =>
                {
                    foreach (var parameter in joinOn)
                    {
                        if (!parameter(row1, row2)) return false;
                    }
                    return true;
                });
                foreach (DataRow fromRow in joinRows)
                {
                    DataRow insertRow = result.NewRow();
                    foreach (DataColumn col1 in t1.Columns)
                    {
                        insertRow[col1.ColumnName] = row1[col1.ColumnName];
                    }
                    foreach (DataColumn col2 in t2.Columns)
                    {
                        insertRow[col2.ColumnName] = fromRow[col2.ColumnName];
                    }
                    result.Rows.Add(insertRow);
                }
            }
            return result;
        }
        /// <summary>
        /// This is a special member of the objectMapping/Language and really should sit there not here. 
        /// Messageboxes are a bit more complicate to work with than "simple" objects and this takes their language-value and returns it efficiently
        /// </summary>
        /// <param name="messageBoxName">A pseudonym for the messagebox whose value is requested.</param>
        /// <returns>Messagebox text contents</returns>
        internal static string GenericGetMessageBoxText(string messageBoxName)
        {
            return Helper.DataReadSQLiteObjectText(
                        languageName: frm_MainApp.appLanguage,
                        objectType: "messageBox",
                        objectName: messageBoxName
                        );
        }
        /// <summary>
        /// Custom-made equivalent for a dialogbox w/ checkbox
        /// </summary>
        internal static class GenericCheckboxDialog
        {
            /// <summary>
            /// A custom dialogbox-like form that includes a checkbox too.
            /// TODO: make it more reusable. Atm it's a bit fixed as there's only 1 place that calls it. Basically a "source" parameter needs to be added in at some stage.
            /// </summary>
            /// <param name="labelText">String of the "main" message.</param>
            /// <param name="caption">Caption of the box - the one that appears on the top.</param>
            /// <param name="checkboxText">Text of the checkbox.</param>
            /// <param name="returnCheckboxText">A yes-no style logic that gets returned/amended to the return string if checked.</param>
            /// <param name="button1Text">Label of the button</param>
            /// <param name="returnButton1Text">String val of what's sent further if the btn is pressed</param>
            /// <param name="button2Text">Same as above</param>
            /// <param name="returnButton2Text">Same as above</param>
            /// <returns>A string that can be reused. Needs fine-tuning in the future as it's single-purpose atm. Lazy. </returns>
            internal static string ShowDialogWithCheckBox(string labelText, string caption, string checkboxText, string returnCheckboxText, string button1Text, string returnButton1Text, string button2Text, string returnButton2Text)
            {
                frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];
                string returnString = "";
                Form promptBox = new Form();
                promptBox.Text = caption;
                promptBox.ControlBox = false;
                promptBox.FormBorderStyle = FormBorderStyle.Fixed3D;
                FlowLayoutPanel panel = new FlowLayoutPanel();

                Label lblText = new Label();
                lblText.Text = labelText;
                lblText.AutoSize = true;
                panel.SetFlowBreak(lblText, true);
                panel.Controls.Add(lblText);

                Button btnYes = new Button() { Text = button1Text };
                btnYes.Click += (sender, e) =>
                {
                    returnString = returnButton1Text;
                    promptBox.Close();
                };
                btnYes.Location = new Point(10, lblText.Bottom + 5);
                btnYes.AutoSize = true;
                panel.Controls.Add(btnYes);

                Button btnNo = new Button() { Text = button2Text };
                btnNo.Click += (sender, e) =>
                {
                    returnString = returnButton2Text;
                    promptBox.Close();
                };

                btnNo.Location = new Point(btnYes.Width + 20, lblText.Bottom + 5);
                btnNo.AutoSize = true;
                panel.SetFlowBreak(btnNo, true);
                panel.Controls.Add(btnNo);

                CheckBox chk = new CheckBox();
                chk.Text = checkboxText;
                chk.AutoSize = true;
                chk.Location = new Point(10, btnYes.Bottom + 5);

                panel.Controls.Add(chk);
                panel.Padding = new Padding(5);
                panel.AutoSize = true;

                promptBox.Controls.Add(panel);
                promptBox.Size = new Size(lblText.Width + 40, chk.Bottom + 50);
                promptBox.ShowInTaskbar = false;

                promptBox.StartPosition = FormStartPosition.CenterScreen;
                promptBox.ShowDialog();

                if (chk.Checked) { returnString += returnCheckboxText; }
                // in case of idiots break glass -- basically if someone ALT+F4s then we reset stuff to "no".
                if (!returnString.Contains(returnButton1Text) && !returnString.Contains(returnButton2Text)) { returnString = returnButton2Text; };
                return returnString;
            }
        }
        #endregion
        #region Exif Related
        /// <summary>
        /// Wrangles data from raw exiftool output to presentable and standardised data.
        /// </summary>
        /// <param name="dtFileExif">Raw values tag from exiftool</param>
        /// <param name="dataPoint">Name of the exiftag we want the data for</param>
        /// <returns>Standardised exif tag output</returns>
        internal static string ExifGetStandardisedDataPointFromExif(DataTable dtFileExif, string dataPoint)
        {
            string returnVal = "";

            string tmpLatVal = "-";
            string tmpLongVal = "-";
            string tryDataValue = "-";
            string tmpLatRefVal = "-";
            string tmpLongRefVal = "-";
            string tmpLatLongRefVal = "-";

            string tmpOutLatLongVal = "";

            try
            {
                tryDataValue = ExifGetRawDataPointFromExif(dtFileExif, dataPoint);
            }
            catch
            {
                // nothing
            }

            switch (dataPoint)
            {
                case "GPSLatitude" or "GPSDestLatitude" or "GPSLongitude" or "GPSDestLongitude":
                    if (tryDataValue != "-")
                    {
                        // we want N instead of North etc.
                        try
                        {
                            tmpLatLongRefVal = ExifGetRawDataPointFromExif(dtFileExif, dataPoint + "Ref").Substring(0, 1);
                        }
                        catch
                        {
                            tmpLatLongRefVal = "-";
                        }

                        if (!tryDataValue.Contains(tmpLatLongRefVal))
                        {
                            tryDataValue = tmpLatLongRefVal + tryDataValue;
                        }
                        tmpOutLatLongVal = GenericAdjustLatLongNegative(tryDataValue).ToString().Replace(',', '.');
                        tryDataValue = tmpOutLatLongVal;
                    }
                    tryDataValue = tmpOutLatLongVal;
                    break;
                case "Coordinates" or "DestCoordinates":
                    string isDest;
                    if (dataPoint.Contains("Dest"))
                    {
                        isDest = "Dest";
                    }
                    else
                    {
                        isDest = "";
                    }
                    // this is entirely the duplicate of the above

                    // check there is lat/long
                    tmpLatVal = ExifGetRawDataPointFromExif(dtFileExif, "GPS" + isDest + "Latitude").Replace(',', '.');
                    tmpLongVal = ExifGetRawDataPointFromExif(dtFileExif, "GPS" + isDest + "Longitude").Replace(',', '.');
                    if (tmpLatVal == "") { tmpLatVal = "-"; };
                    if (tmpLongVal == "") { tmpLongVal = "-"; };

                    if (ExifGetRawDataPointFromExif(dtFileExif, "GPS" + isDest + "LatitudeRef").Length > 0 && ExifGetRawDataPointFromExif(dtFileExif, "GPS" + isDest + "LongitudeRef").Length > 0)
                    {
                        tmpLatRefVal = ExifGetRawDataPointFromExif(dtFileExif, "GPS" + isDest + "LatitudeRef").Substring(0, 1).Replace(',', '.');
                        tmpLongRefVal = ExifGetRawDataPointFromExif(dtFileExif, "GPS" + isDest + "LongitudeRef").Substring(0, 1).Replace(',', '.');
                    }
                    else
                    {
                        tmpLatRefVal = "-";
                        tmpLongRefVal = "-";
                    }

                    // check there is one bit of data for both components
                    if (tmpLatVal != "-" && tmpLongVal != "-")
                    {
                        // stick Ref at the end of LatLong
                        if (!tmpLatVal.Contains(tmpLatRefVal))
                        {
                            tmpLatVal += tmpLatRefVal;
                        }
                        if (!tmpLongVal.Contains(tmpLongRefVal))
                        {
                            tmpLongVal += tmpLongRefVal;
                        }

                        tmpLatVal = GenericAdjustLatLongNegative(tmpLatVal).ToString().Replace(',', '.');
                        tmpLongVal = GenericAdjustLatLongNegative(tmpLongVal).ToString().Replace(',', '.');
                        tryDataValue = tmpLatVal + ";" + tmpLongVal;
                    }
                    else
                    {
                        tryDataValue = "-";
                    }
                    break;
                case "GPSAltitude":
                    if (tryDataValue.Contains("m"))
                    {
                        tryDataValue = tryDataValue.Split('m')[0].ToString().Trim().Replace(',', '.');
                    }
                    if (tryDataValue.Contains("/"))
                    {
                        tryDataValue = tryDataValue.Split('/')[0].ToString().Trim().Replace(',', '.');
                    }
                    break;
                case "GPSAltitudeRef":
                    if (tryDataValue.ToLower().Contains("below") || tryDataValue.Contains("1"))
                    {
                        tryDataValue = "Below Sea Level";
                    }
                    else
                    {
                        tryDataValue = "Above Sea Level";
                    }
                    break;
                case "ExposureTime":
                    tryDataValue = tryDataValue.Replace("sec", "").Trim();
                    break;
                case "Fnumber" or "FocalLength" or "FocalLengthIn35mmFormat" or "ISO":
                    if (tryDataValue != "-")
                    {
                        if (dataPoint == "FocalLengthIn35mmFormat")
                        {
                            // at least with a Canon 40D this returns stuff like: "51.0 mm (35 mm equivalent: 81.7 mm)" so i think it's safe to assume that 
                            // this might need a bit of debugging and community feeback. or someone with decent regex knowledge
                            if (tryDataValue.Contains(':'))
                            {
                                tryDataValue = Regex.Replace(tryDataValue, @"[^\d:.]", "").Split(':').Last();
                            }
                            else
                            {
                                // this is untested. soz. feedback welcome.
                                tryDataValue = Regex.Replace(tryDataValue, @"[^\d:.]", "");
                            }
                        }
                        else
                        {
                            tryDataValue = tryDataValue.Replace("mm", "").Replace("f/", "").Replace("f", "").Replace("[", "").Replace("]", "").Trim();
                        }
                        if (tryDataValue.Contains("/"))
                        {
                            tryDataValue = Math.Round((double.Parse(tryDataValue.Split('/')[0], NumberStyles.Any, CultureInfo.InvariantCulture)) / (double.Parse(tryDataValue.Split('/')[1], NumberStyles.Any, CultureInfo.InvariantCulture)), 1).ToString();
                        }
                    }
                    break;
                default:
                    break;
            }
            returnVal = tryDataValue;
            return returnVal;
        }
        /// <summary>
        /// This translates between plain English and exiftool tags. For example if the tag we are looking for is "Model" (of the camera)..
        /// ... this will get all the possible tag names where model-info can sit and extract those from the data. E.g. if we're looking for Model ...
        /// this will get both EXIF:Model and and XMP:Model - as it does a cartesian join on the objectNames table. 
        /// </summary>
        /// <param name="dtFileExif">Raw exiftool outout of all tags</param>
        /// <param name="dataPoint">Plain English datapoint we're after</param>
        /// <returns>Value of that datapoint if exists (e.g "Canon EOS 30D") - unwrangled, raw.</returns>
        internal static string ExifGetRawDataPointFromExif(DataTable dtFileExif, string dataPoint)
        {
            string returnVal = "-";
            string tryDataValue = "-";

            DataTable dtobjectTagNames_In = GenericJoinDataTables(frm_MainApp.objectNames, frm_MainApp.objectTagNames_In,
               (row1, row2) =>
               row1.Field<string>("objectName") == row2.Field<string>("objectName"));


            DataView dvobjectTagNames_In = new(dtobjectTagNames_In)
            {
                RowFilter = "objectName = '" + dataPoint + "'",
                Sort = "valuePriorityOrder"
            };

            DataTable dtTagsWanted = dvobjectTagNames_In.ToTable(true, "objectTagName_In");

            if (dtTagsWanted.Rows.Count > 0 && dtFileExif.Rows.Count > 0)
            {
                foreach (DataRow dr in dtTagsWanted.Rows)
                {
                    try
                    {
                        string tagNameToSelect = dr[0].ToString();
                        DataRow filteredRows = dtFileExif.Select("TagName = '" + tagNameToSelect + "'").FirstOrDefault();
                        if (filteredRows != null)
                        {

                            tryDataValue = filteredRows[1]?.ToString();
                            if (tryDataValue != null && tryDataValue != "")
                            {
                                break;
                            }
                        }
                    }
                    catch (ArgumentException)
                    {
                        tryDataValue = "-";
                    }
                }
            }
            else
            {
                tryDataValue = "-";
            }
            returnVal = tryDataValue;
            return returnVal;
        }
        /// <summary>
        /// This parses the "compatible" file(name)s for exif tags. 
        /// ... "compatible" here means that the path of the file generally uses English characters only.
        /// ... "tags" here means the tags required for the software to work (such as GPS stuff), not all the tags in the whole of the file.
        /// exifToolResult is a direct ouptut of exiftool that gets read into a DT and parsed. This is a fast process and can work line-by-line for
        /// ... items in the listview that's asking for it.
        /// </summary>
        /// <param name="files">List of "compatible" filenames</param>
        /// <returns>In practice, nothing but it's responsible for sending the updated exif info back to the requester (usually a listview)</returns>
        internal static async Task ExifGetExifFromFilesCompatibleFileNames(List<string> files)
        {
            IEnumerable<string> exifToolCommand = Enumerable.Empty<string>();
            IEnumerable<string> exifToolCommandWithFileName = Enumerable.Empty<string>();

            List<string> commonArgList = new()
            {   "-a",
                //"-c '%+.6f'",
                "-s",
                "-s",
                "-struct",
                "-sort",
                "-G",
                "-ee",
            };

            foreach (string arg in commonArgList)
            {
                exifToolCommand = exifToolCommand.Concat(new[] { arg });
            }
            CancellationToken ct = CancellationToken.None;
            // add required tags
            DataTable dt_objectTagNames = DataReadSQLiteObjectMappingTagsToPass();

            foreach (DataRow dr in dt_objectTagNames.Rows)
            {
                exifToolCommand = exifToolCommand.Concat(new[] { "-" + dr["objectTagName_ToPass"] });
            }

            frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];
            string folderNameToUse = frm_mainAppInstance.tbx_FolderName.Text;

            foreach (string listElem in files)
            {
                DataTable dt_fileExifTable = new();
                dt_fileExifTable.Clear();
                dt_fileExifTable.Columns.Add("TagName");
                dt_fileExifTable.Columns.Add("TagValue");
                string exifToolResult;

                if (File.Exists(Path.Combine(folderNameToUse, listElem)))
                {
                    try
                    {
                        exifToolCommandWithFileName = exifToolCommand.Concat(new[] { Path.Combine(folderNameToUse, listElem) });
                        exifToolResult = await frm_mainAppInstance.asyncExifTool.ExecuteAsync(exifToolCommandWithFileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_ErrorAsyncExifToolExecuteAsyncFailed") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        exifToolResult = null;
                    }
                    string[] exifToolResultArr = Convert.ToString(exifToolResult).Split(
                             new string[] { "\r\n", "\r", "\n" },
                             StringSplitOptions.None
                         ).Distinct().ToArray();
                    ;

                    foreach (string fileExifDataRow in exifToolResultArr)
                    {
                        if (fileExifDataRow is not null && fileExifDataRow.Length > 0 && fileExifDataRow.Substring(0, 1) == "[")
                        {
                            string exifGroup = fileExifDataRow.Split(' ')[0].Replace("[", "").Replace("]", "");
                            string exifTagName = fileExifDataRow.Split(' ')[1].Replace(":", "");
                            string exifTagVal = fileExifDataRow.Substring(fileExifDataRow.IndexOf(':') + 2);

                            DataRow dr = dt_fileExifTable.NewRow();
                            dr["TagName"] = exifGroup + ":" + exifTagName;
                            dr["TagValue"] = exifTagVal;

                            dt_fileExifTable.Rows.Add(dr);
                        }
                    }
                    // for some files there may be data in a sidecar xmp without that data existing in the picture-file. we'll try to collect it here.
                    if (File.Exists(Path.Combine(folderNameToUse, (Path.GetFileNameWithoutExtension(Path.Combine(folderNameToUse, listElem)) + ".xmp"))))
                    {
                        string sideCarXMPFilePath = Path.Combine(folderNameToUse, (Path.GetFileNameWithoutExtension(Path.Combine(folderNameToUse, listElem)) + ".xmp"));
                        // this is totally a copypaste from above
                        try
                        {
                            exifToolCommandWithFileName = exifToolCommand.Concat(new[] { sideCarXMPFilePath });
                            exifToolResult = await frm_mainAppInstance.asyncExifTool.ExecuteAsync(exifToolCommandWithFileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_ErrorAsyncExifToolExecuteAsyncFailed") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            exifToolResult = null;
                        }
                        string[] exifToolResultArrXMP = Convert.ToString(exifToolResult).Split(
                                 new string[] { "\r\n", "\r", "\n" },
                                 StringSplitOptions.None
                             ).Distinct().ToArray();
                        ;

                        foreach (string fileExifDataRow in exifToolResultArrXMP)
                        {
                            if (fileExifDataRow is not null && fileExifDataRow.Length > 0 && fileExifDataRow.Substring(0, 1) == "[")
                            {
                                string exifGroup = fileExifDataRow.Split(' ')[0].Replace("[", "").Replace("]", "");
                                string exifTagName = fileExifDataRow.Split(' ')[1].Replace(":", "");
                                string exifTagVal = fileExifDataRow.Substring(fileExifDataRow.IndexOf(':') + 2);

                                DataRow dr = dt_fileExifTable.NewRow();
                                dr["TagName"] = exifGroup + ":" + exifTagName;
                                dr["TagValue"] = exifTagVal;

                                dt_fileExifTable.Rows.Add(dr);
                            }
                        }
                    }

                    ListViewItem lvi = frm_mainAppInstance.lvw_FileList.FindItemWithText(listElem);

                    // lvi can become null if user changes folder while a previous process is loading and listview is cleared.
                    if (lvi != null)
                    {
                        var lvchs = frm_mainAppInstance.ListViewColumnHeaders;
                        for (int i = 1; i < lvi.SubItems.Count; i++)
                        {
                            string str = ExifGetStandardisedDataPointFromExif(dt_fileExifTable, lvchs[i].Name.Substring(4));
                            lvi.SubItems[i].Text = str;
                        }
                        lvi.ForeColor = Color.Black;
                        frm_MainApp.HandlerUpdateLabelText(frm_mainAppInstance.lbl_ParseProgress, "Processing: " + lvi.Text);
                    }
                }
            }
            frm_MainApp.HandlerUpdateLabelText(frm_mainAppInstance.lbl_ParseProgress, "Ready.");
        }
        /// <summary>
        /// This parses the "incompatible" file(name)s for exif tags. 
        /// ... "incompatible" here means that the path of the file does not exclusively use English characters.
        /// ... "tags" here means the tags required for the software to work (such as GPS stuff), not all the tags in the whole of the file.
        /// The main difference between this and the "compatible" version is that this calls cmd and then puts the output into a txt file (as part of exiftoolCmd) that gets read back in
        /// ... this is slower and allows for less control but safer.
        /// </summary>
        /// <param name="files">List of "incompatible" filenames</param>
        /// <returns>In practice, nothing but it's responsible for sending the updated exif info back to the requester (usually a listview)</returns>
        internal static async Task ExifGetExifFromFilesIncompatibleFileNames(List<string> files)
        {
            #region ExifToolConfiguration
            var exifToolExe = Path.Combine(frm_MainApp.resourcesFolderPath, "exiftool.exe");
            frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];

            string folderNameToUse = frm_mainAppInstance.tbx_FolderName.Text;
            string argsFile = Path.Combine(frm_MainApp.userDataFolderPath, "exifArgs.args");
            string exiftoolCmd = " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 -w! " + s_doubleQuote + frm_MainApp.userDataFolderPath + @"\%F.txt" + s_doubleQuote + " -@ " + s_doubleQuote + argsFile + s_doubleQuote;

            File.Delete(argsFile);

            var exifArgs = new List<string> { };
            // needs a space before and after.
            string commonArgs = " -a -s -s -struct -sort -G -ee -args ";

            #endregion
            // add required tags
            DataTable dt_objectTagNames = DataReadSQLiteObjectMappingTagsToPass();

            foreach (DataRow dr in dt_objectTagNames.Rows)
            {
                exifArgs.Add(dr["objectTagName_ToPass"].ToString());
            }

            foreach (string listElem in files)
            {
                if (File.Exists(Path.Combine(folderNameToUse, listElem)))
                {
                    File.AppendAllText(argsFile, Path.Combine(folderNameToUse, listElem) + Environment.NewLine, Encoding.UTF8);
                    foreach (string arg in exifArgs)
                    {
                        File.AppendAllText(argsFile, "-" + arg + Environment.NewLine);
                    }

                }
                //// add any xmp sidecar files
                if (File.Exists(Path.Combine(folderNameToUse, (Path.GetFileNameWithoutExtension(Path.Combine(folderNameToUse, listElem)) + ".xmp"))))
                {
                    string sideCarXMPFilePath = Path.Combine(folderNameToUse, (Path.GetFileNameWithoutExtension(Path.Combine(folderNameToUse, listElem)) + ".xmp"));
                    File.AppendAllText(argsFile, sideCarXMPFilePath + Environment.NewLine, Encoding.UTF8);
                    foreach (string arg in exifArgs)
                    {
                        File.AppendAllText(argsFile, "-" + arg + Environment.NewLine);
                    }

                }
                File.AppendAllText(argsFile, "-progress" + Environment.NewLine);

            }
            File.AppendAllText(argsFile, "-execute" + Environment.NewLine);
            ///////////////
            // via https://stackoverflow.com/a/68616297/3968494
            await Task.Run(() =>
            {
                using var p = new Process();
                p.StartInfo = new ProcessStartInfo(@"c:\windows\system32\cmd.exe")
                {
                    Arguments = @"/k " + s_doubleQuote + s_doubleQuote + Path.Combine(frm_MainApp.resourcesFolderPath, "exiftool.exe") + s_doubleQuote + " " + commonArgs + exiftoolCmd + s_doubleQuote + "&& exit",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };

                p.EnableRaisingEvents = true;

                s_ErrorMsg = "";
                p.OutputDataReceived += (_, data) =>
                {
                    if (data.Data != null && data.Data.Contains("="))
                    {
                        frm_MainApp.HandlerUpdateLabelText(frm_mainAppInstance.lbl_ParseProgress, "Processing: " + data.Data.Split('[').First().Split('/').Last().Trim());
                    }
                    else if (data.Data != null && !data.Data.Contains("files created") && !data.Data.Contains("files read") && data.Data.Length > 0)
                    {
                        s_ErrorMsg += data.Data.ToString() + Environment.NewLine;
                        try { p.Kill(); } catch { } // else it will be stuck running forever
                    }
                };

                p.ErrorDataReceived += (_, data) =>
                {
                    if (data.Data != null && data.Data.Contains("="))
                    {
                        frm_MainApp.HandlerUpdateLabelText(frm_mainAppInstance.lbl_ParseProgress, "Processing: " + data.Data.Split('[').First().Split('/').Last().Trim());
                    }
                    else if (data.Data != null && !data.Data.Contains("files created") && !data.Data.Contains("files read") && data.Data.Length > 0)
                    {
                        s_ErrorMsg += data.Data.ToString() + Environment.NewLine;
                        try { p.Kill(); } catch { } // else it will be stuck running forever
                    }
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                p.Close();
                if (s_ErrorMsg != "")
                {
                    MessageBox.Show(s_ErrorMsg);
                }
                // if still here then exorcise
                try { p.Kill(); } catch { }
            });
            ///////////////

            // try to collect the txt files and then read them back into the listview.
            try
            {
                foreach (string itemText in files)
                {
                    string exifFileIn = Path.Combine(frm_MainApp.userDataFolderPath, itemText + ".txt");
                    if (File.Exists(exifFileIn))
                    {
                        DataTable dt_fileExifTable = new();
                        dt_fileExifTable.Clear();
                        dt_fileExifTable.Columns.Add("TagName");
                        dt_fileExifTable.Columns.Add("TagValue");
                        foreach (string exifTxtFileLineIn in File.ReadLines(exifFileIn))
                        {
                            string exifTagName = exifTxtFileLineIn.Split('=')[0].Substring(1);
                            string exifTagVal = exifTxtFileLineIn.Split('=')[1];

                            DataRow dr = dt_fileExifTable.NewRow();
                            dr["TagName"] = exifTagName;
                            dr["TagValue"] = exifTagVal;
                            dt_fileExifTable.Rows.Add(dr);
                        }
                        // see if there's an xmp-output too
                        string sideCarXMPFilePath = Path.Combine(frm_MainApp.userDataFolderPath, itemText.Substring(0, itemText.LastIndexOf('.')) + ".xmp" + ".txt");
                        if (File.Exists(sideCarXMPFilePath))
                        {
                            foreach (string exifTxtFileLineIn in File.ReadLines(sideCarXMPFilePath))
                            {
                                string exifTagName = exifTxtFileLineIn.Split('=')[0].Substring(1);
                                string exifTagVal = exifTxtFileLineIn.Split('=')[1];

                                DataRow dr = dt_fileExifTable.NewRow();
                                dr["TagName"] = exifTagName;
                                dr["TagValue"] = exifTagVal;
                                dt_fileExifTable.Rows.Add(dr);
                            }
                        }

                        // de-dupe. this is pretty poor performance but the dataset is small
                        DataTable dt_distinctFileExifTable = dt_fileExifTable.DefaultView.ToTable(true);

                        ListViewItem lvi = frm_mainAppInstance.lvw_FileList.FindItemWithText(itemText);

                        var lvchs = frm_mainAppInstance.ListViewColumnHeaders;
                        for (int i = 1; i < lvi.SubItems.Count; i++)
                        {
                            string str = ExifGetStandardisedDataPointFromExif(dt_distinctFileExifTable, lvchs[i].Name.Substring(4));
                            lvi.SubItems[i].Text = str;
                        }
                        lvi.ForeColor = Color.Black;
                        File.Delete(exifFileIn); // clean up
                    }
                }
            }
            catch
            {
                // nothing. errors should have already come up
            }
        }
        /// <summary>
        /// Gets the app version of the current exifTool and updates a static string with the data.
        /// </summary>
        internal static async void ExifGetExifToolVersion()
        {
            frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];
            IEnumerable<string> exifToolCommand = Enumerable.Empty<string>();
            List<string> commonArgList = new()
            {   "-ver",
            };

            foreach (string arg in commonArgList)
            {
                exifToolCommand = exifToolCommand.Concat(new[] { arg
                });
            }
            CancellationToken ct = CancellationToken.None;
            string exifToolResult;

            try
            {
                exifToolResult = await frm_mainAppInstance.asyncExifTool.ExecuteAsync(exifToolCommand);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_ErrorAsyncExifToolExecuteAsyncFailed") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                exifToolResult = null;
            }
            string[] exifToolResultArr = Convert.ToString(exifToolResult).Split(
                     new string[] { "\r\n", "\r", "\n" },
                     StringSplitOptions.None
                 ).Distinct().ToArray();
            ;

            // really this should only be 1 row but I copied from the larger code blocks and is easier that way.
            foreach (string exifToolReturnStr in exifToolResultArr)
            {
                if (exifToolReturnStr is not null && exifToolReturnStr.Length > 0)
                {
                    //this only returns something like "12.42" and that's all
                    Helper.DataWriteSQLiteSettings(
                        tableName: "settings",
                        settingTabPage: "generic",
                        settingId: "exifToolVer",
                        settingValue: exifToolReturnStr
                        );
                }
            }
        }
        /// <summary>
        /// This generates (technically, extracts) the image previews from files for the user when they click on a filename
        /// ... in whichever listview.
        /// </summary>
        /// <param name="fileName">Path of file for which the preview needs creating</param>
        /// <returns>Realistically nothing but the process generates the bitmap if possible</returns>
        internal static async Task ExifGetImagePreviews(string fileName)
        {
            #region ExifToolConfiguration
            var exifToolExe = Path.Combine(frm_MainApp.resourcesFolderPath, "exiftool.exe");

            // want to give this a different name from the usual exifArgs.args just in case that's still being accessed (as much as it shouldn't be)
            Regex rgx = new Regex("[^a-zA-Z0-9]");
            string fileNameReplaced = rgx.Replace(fileName.Replace(frm_MainApp.folderName, ""), "_");
            string argsFile = Path.Combine(frm_MainApp.userDataFolderPath, "exifArgs_getPreview_" + fileNameReplaced + ".args");
            string exiftoolCmd = " -charset utf8 -charset filename=utf8 -b -preview:all -w! " + s_doubleQuote + frm_MainApp.userDataFolderPath + @"\%F.jpg" + s_doubleQuote + " -@ " + s_doubleQuote + argsFile + s_doubleQuote;

            File.Delete(argsFile);

            #endregion
            // add required tags

            frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];

            if (File.Exists(fileName))
            {
                File.AppendAllText(argsFile, fileName + Environment.NewLine, Encoding.UTF8);
                File.AppendAllText(argsFile, "-execute" + Environment.NewLine);
            }


            ///////////////
            // via https://stackoverflow.com/a/68616297/3968494
            await Task.Run(() =>
            {

                using var p = new Process();
                p.StartInfo = new ProcessStartInfo(@"c:\windows\system32\cmd.exe")
                {
                    Arguments = @"/k " + s_doubleQuote + s_doubleQuote + Path.Combine(frm_MainApp.resourcesFolderPath, "exiftool.exe") + s_doubleQuote + " " + exiftoolCmd + s_doubleQuote + "&& exit",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };

                p.EnableRaisingEvents = true;
                p.OutputDataReceived += (_, data) =>
                {
                    // don't care
                };

                p.ErrorDataReceived += (_, data) =>
                {
                    // don't care
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                p.Close();
                // if still here then exorcise
                try { p.Kill(); } catch { }
            });
            ///////////////

        }
        /// <summary>
        /// Writes outstanding exif changes to the files (all files in the queue).
        /// This logic is very similar to the "incompatible read" above - it's safer. While it's also probably slower
        /// ... the assumption is that users will read a lot of files but will write proportionately fewer files so 
        /// ... speed is less of an essence against safety.
        /// </summary>
        /// <returns>Reastically nothing but writes the exif tags and updates the listview rows where necessary</returns>
        internal static async Task ExifWriteExifToFile()
        {
            string exifArgs = "";
            string argsFile = Path.Combine(frm_MainApp.userDataFolderPath, "exifArgsToWrite.args");
            string exiftoolCmd = " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8" + " -@ " + s_doubleQuote + argsFile + s_doubleQuote;
            frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];

            // if user switches folder in the process of writing this will keep it standard
            string folderNameToWrite = frm_mainAppInstance.tbx_FolderName.Text;
            File.Delete(argsFile);

            bool writeXMLTags = false;
            bool writeXMLSideCar = false;
            bool overwriteOriginal = false;

            // get tag names
            DataTable dt_objectTagNames_Out = GenericJoinDataTables(frm_MainApp.objectNames, frm_MainApp.objectTagNames_Out,
            (row1, row2) =>
            row1.Field<string>("objectName") == row2.Field<string>("objectName"));

            DataView dv_FileNames = new DataView(frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite);
            DataTable dt_DistinctFileNames = dv_FileNames.ToTable(true, "filePath");

            // check there's anything to write.
            foreach (DataRow dr_FileName in dt_DistinctFileNames.Rows)
            {
                string fileName = dr_FileName[0].ToString();

                exifArgs += Path.Combine(folderNameToWrite, fileName) + Environment.NewLine; //needs to include folder name
                exifArgs += "-ignoreMinorErrors" + Environment.NewLine;
                exifArgs += "-progress" + Environment.NewLine;

                DataView dv_FileWriteQueue = new DataView(frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite);
                dv_FileWriteQueue.RowFilter = "filePath = '" + fileName + "'";

                if (dv_FileWriteQueue.Count > 0)
                {
                    // get tags for this file
                    DataTable dt_FileWriteQueue = dv_FileWriteQueue.ToTable();
                    DataTable dt_objectTagNames_OutWithData = GenericJoinDataTables(dt_objectTagNames_Out, dt_FileWriteQueue,
                    (row1, row2) =>
                    row1.Field<string>("objectName") == row2.Field<string>("settingId"));
                    string fileExt = Path.GetExtension(fileName).Substring(1);

                    string exiftoolTagName;
                    string updateExifVal;

                    writeXMLTags = Convert.ToBoolean(DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExt.ToLower() + "_" + "ckb_AddXMPIntoFile")); ;
                    writeXMLSideCar = Convert.ToBoolean(DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExt.ToLower() + "_" + "ckb_AddXMPSideCar")); ;
                    overwriteOriginal = Convert.ToBoolean(DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExt.ToLower() + "_" + "ckb_OverwriteOriginal"));

                    // add tags to argsFile
                    foreach (DataRow row in dt_objectTagNames_OutWithData.Rows)
                    {
                        bool deleteAllGPSData = false;
                        // this is prob not the best way to go around this....
                        foreach (DataRow drFileTags in dt_FileWriteQueue.Rows)
                        {
                            if (drFileTags[1].ToString() == @"gps*")
                            {
                                deleteAllGPSData = true;
                            }
                        }
                        // xml only if needed
                        if (writeXMLTags)
                        {
                            if (row["objectTagName_Out"].ToString().Contains(":"))
                            {
                                exiftoolTagName = row["objectTagName_Out"].ToString();
                                updateExifVal = row["settingValue"].ToString();
                                if (updateExifVal != "")
                                {
                                    exifArgs += "-" + exiftoolTagName + "=" + updateExifVal + Environment.NewLine;
                                    //if lat/long then add Ref. 
                                    if (exiftoolTagName == "exif:GPSLatitude" || exiftoolTagName == "exif:GPSDestLatitude")
                                    {
                                        if (updateExifVal.Substring(0, 1) == "-")
                                        {
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "South" + Environment.NewLine;
                                        }
                                        else
                                        {
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "North" + Environment.NewLine;
                                        }
                                    }
                                    else if (exiftoolTagName == "exif:GPSLongitude" || exiftoolTagName == "exif:GPSDestLongitude")
                                    {
                                        if (updateExifVal.Substring(0, 1) == "-")
                                        {
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "West" + Environment.NewLine;
                                        }
                                        else
                                        {
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "East" + Environment.NewLine;
                                        }
                                    }
                                }
                                else // delete tag
                                {
                                    exifArgs += "-" + exiftoolTagName + "=" + Environment.NewLine;
                                    //if lat/long then add Ref. 
                                    if (exiftoolTagName == "GPSLatitude" || exiftoolTagName == "GPSDestLatitude")
                                    {
                                        if (updateExifVal.Substring(0, 1) == "-")
                                        {
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "=" + Environment.NewLine;
                                        }
                                        else
                                        {
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "=" + Environment.NewLine;
                                        }
                                    }
                                    else if (exiftoolTagName == "GPSLongitude" || exiftoolTagName == "GPSDestLongitude")
                                    {
                                        if (updateExifVal.Substring(0, 1) == "-")
                                        {
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "=" + Environment.NewLine;
                                        }
                                        else
                                        {
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "=" + Environment.NewLine;
                                        }
                                    }
                                }
                            }
                        }
                        // non-xml always
                        if (!row["objectTagName_Out"].ToString().Contains(":"))
                        {
                            exiftoolTagName = row["objectTagName_Out"].ToString();
                            updateExifVal = row["settingValue"].ToString();
                            if (updateExifVal != "")
                            {
                                exifArgs += "-" + exiftoolTagName + "=" + updateExifVal + Environment.NewLine;
                                //if lat/long then add Ref. 
                                if (exiftoolTagName == "GPSLatitude" || exiftoolTagName == "GPSDestLatitude")
                                {
                                    if (updateExifVal.Substring(0, 1) == "-")
                                    {
                                        exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "South" + Environment.NewLine;
                                    }
                                    else
                                    {
                                        exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "North" + Environment.NewLine;
                                    }
                                }
                                else if (exiftoolTagName == "GPSLongitude" || exiftoolTagName == "GPSDestLongitude")
                                {
                                    if (updateExifVal.Substring(0, 1) == "-")
                                    {
                                        exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "West" + Environment.NewLine;
                                    }
                                    else
                                    {
                                        exifArgs += "-" + exiftoolTagName + "Ref" + "=" + "East" + Environment.NewLine;
                                    }
                                }
                            }
                            else //delete tag
                            {
                                exifArgs += "-" + exiftoolTagName + "=" + Environment.NewLine;
                            }
                        }

                        // delete-all-gps has to come separetely as it's no a standard tag
                        // arguably this could come entirely separately and we could ignore everything else above but in reality
                        // this only deletes GPS stuff not IPTC (cities etc) so i think this is safer even if a tiny bit slower.
                        if (deleteAllGPSData)
                        {
                            exifArgs += "-gps*=" + Environment.NewLine;
                        }
                    }

                    if (overwriteOriginal)
                    {
                        exifArgs += "-overwrite_original_in_place" + Environment.NewLine;
                    }
                    exifArgs += "-iptc:codedcharacterset=utf8" + Environment.NewLine;
                }
                exifArgs += "-execute" + Environment.NewLine;
                // sidecar copying needs to be in a separate batch, as technically it's a different file
                if (writeXMLSideCar)
                {
                    exifArgs += Path.Combine(folderNameToWrite, Path.GetFileNameWithoutExtension(Path.Combine(folderNameToWrite, fileName)) + ".xmp") + Environment.NewLine; //needs to include folder name
                    exifArgs += "-progress" + Environment.NewLine;
                    exifArgs += "-ignoreMinorErrors" + Environment.NewLine;
                    exifArgs += "-tagsfromfile=" + Path.Combine(folderNameToWrite, fileName) + Environment.NewLine;
                    if (overwriteOriginal)
                    {
                        exifArgs += "-overwrite_original_in_place" + Environment.NewLine;
                    }
                    exifArgs += "-execute" + Environment.NewLine;
                }
            }
            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Clear();
            if (exifArgs != "")
            {
                File.AppendAllText(argsFile, exifArgs, Encoding.UTF8);

                ///////////////
                // via https://stackoverflow.com/a/68616297/3968494
                await Task.Run(() =>
                {

                    using var p = new Process();
                    p.StartInfo = new ProcessStartInfo(@"c:\windows\system32\cmd.exe")
                    {
                        Arguments = @"/k " + s_doubleQuote + s_doubleQuote + Path.Combine(frm_MainApp.resourcesFolderPath, "exiftool.exe") + s_doubleQuote + " " + exiftoolCmd + s_doubleQuote + "&& exit",
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                    };

                    p.EnableRaisingEvents = true;

                    //string messageStart = "Writing Exif - ";
                    p.OutputDataReceived += (_, data) =>
                        {
                            if (data.Data != null && data.Data.Contains("="))
                            {
                                string thisFileName = data.Data.Replace("=", "").Split('[').First().Trim().Split('/').Last();
                                frm_MainApp.HandlerUpdateLabelText(frm_mainAppInstance.lbl_ParseProgress, "Processing: " + thisFileName);
                                try
                                {
                                    frm_MainApp.HandlerUpdateItemColour(frm_mainAppInstance.lvw_FileList, thisFileName, Color.Black);
                                }
                                catch
                                {

                                }
                            }
                            else if (data.Data != null && !data.Data.Contains("files updated") && !data.Data.Contains("files created") && data.Data.Length > 0)
                            {
                                MessageBox.Show(data.Data);
                            }
                        };

                    p.ErrorDataReceived += (_, data) =>
                {
                    if (data.Data != null && data.Data.Contains("="))
                    {
                        string thisFileName = data.Data.Replace("=", "").Split('[').First().Trim().Split('/').Last();
                        frm_MainApp.HandlerUpdateLabelText(frm_mainAppInstance.lbl_ParseProgress, "Processing: " + thisFileName);
                        try
                        {
                            frm_MainApp.HandlerUpdateItemColour(frm_mainAppInstance.lvw_FileList, thisFileName, Color.Black);
                        }
                        catch
                        {

                        }
                    }
                    else if (data.Data != null && !data.Data.Contains("files updated") && data.Data.Length > 0)
                    {
                        MessageBox.Show(data.Data);
                    }
                };

                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    p.Close();
                    // if still here then exorcise
                    try { p.Kill(); } catch { }
                });
                ///////////////
                frm_MainApp.HandlerUpdateLabelText(frm_mainAppInstance.lbl_ParseProgress, "Ready.");
            }
        }
        /// <summary>
        /// Responsible for pulling the toponomy response for the API
        /// </summary>
        /// <param name="latitude">As on the tin.</param>
        /// <param name="longitude">As on the tin.</param>
        /// <returns>Structured toponomy response</returns>
        internal static geoTagNinja.GeoResponseToponomy API_ExifGetGeoDataFromWebToponomy(string latitude, string longitude)
        {
            if (s_GeoNames_UserName == null)
            {
                try
                {
                    s_GeoNames_UserName = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                    s_GeoNames_Pwd = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_ErrorCantReadDefaultSQLiteDB") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            geoTagNinja.GeoResponseToponomy returnVal = new();
            var client = new RestClient("http://api.geonames.org/")
            {
                Authenticator = new HttpBasicAuthenticator(s_GeoNames_UserName, s_GeoNames_Pwd)
            };

            var request_Toponomy = new RestRequest("findNearbyPlaceNameJSON?lat=" + latitude + "&lng=" + longitude + "&style=FULL", Method.Get);
            var response_Toponomy = client.ExecuteGet(request_Toponomy);
            // check API reponse is OK
            if (response_Toponomy.StatusCode.ToString() == "OK")
            {
                s_APIOkay = true;
                var data = (JObject)JsonConvert.DeserializeObject(response_Toponomy.Content);
                var geoResponse_Toponomy = geoTagNinja.GeoResponseToponomy.FromJson(data.ToString());
                returnVal = geoResponse_Toponomy;
            }
            else
            {
                s_APIOkay = false;
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_WarningGeoNamesAPIResponse") + response_Toponomy.StatusCode.ToString(), "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return returnVal;
        }
        /// <summary>
        /// Responsible for pulling the altitude response for the API
        /// </summary>
        /// <param name="latitude">As on the tin.</param>
        /// <param name="longitude">As on the tin.</param>
        /// <returns>Structured altitude response</returns>
        internal static geoTagNinja.GeoResponseAltitude API_ExifGetGeoDataFromWebAltitude(string latitude, string longitude)
        {
            if (s_GeoNames_UserName == null)
            {
                try
                {
                    s_GeoNames_UserName = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                    s_GeoNames_Pwd = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_ErrorCantReadDefaultSQLiteDB") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            geoTagNinja.GeoResponseAltitude returnVal = new();
            var client = new RestClient("http://api.geonames.org/")
            {
                Authenticator = new HttpBasicAuthenticator(s_GeoNames_UserName, s_GeoNames_Pwd)
            };

            var request_Altitude = new RestRequest("srtm1JSON?lat=" + latitude + "&lng=" + longitude, Method.Get);
            var response_Altitude = client.ExecuteGet(request_Altitude);
            // check API reponse is OK
            if (response_Altitude.StatusCode.ToString() == "OK")
            {
                s_APIOkay = true;
                var data = (JObject)JsonConvert.DeserializeObject(response_Altitude.Content);
                var geoResponseAltitude = geoTagNinja.GeoResponseAltitude.FromJson(data.ToString());
                returnVal = geoResponseAltitude;
            }
            else
            {
                s_APIOkay = false;
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_WarningGeoNamesAPIResponse") + response_Altitude.StatusCode.ToString(), "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return returnVal;
        }
        /// <summary>
        /// Responsible for pulling the latest prod-version number of exifTool from metaCPAN
        /// </summary>
        /// <returns>The version number of the currently newest exifTool uploaded to metaCPAN</returns>
        internal static geoTagNinja.ExifToolVerQueryResponse API_ExifGetExifToolVersionFromWeb()
        {
            geoTagNinja.ExifToolVerQueryResponse returnVal = new();
            var client = new RestClient("https://fastapi.metacpan.org/")
            {
                // admittedly no idea how to do this w/o any auth (as it's not needed) but this appears to work.
                Authenticator = new HttpBasicAuthenticator("demo", "demo")
            };
            var request_ExifToolVersionQuery = new RestRequest("v1/release/_search?q=Image-ExifTool%20AND%20status:latest&fields=name,status,version&size=1", Method.Get);
            var response_ExifToolVerQuery = client.ExecuteGet(request_ExifToolVersionQuery);
            if (response_ExifToolVerQuery.StatusCode.ToString() == "OK")
            {
                s_APIOkay = true;
                var data = (JObject)JsonConvert.DeserializeObject(response_ExifToolVerQuery.Content);
                var exifToolVerQueryResponse = geoTagNinja.ExifToolVerQueryResponse.FromJson(data.ToString());
                returnVal = exifToolVerQueryResponse;
            }
            else
            {
                s_APIOkay = false;
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_WarningExifToolVerAPIResponse") + response_ExifToolVerQuery.StatusCode.ToString(), "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return returnVal;
        }
        /// <summary>
        /// Responsible for pulling the latest release of GTN from gitHub
        /// </summary>
        /// <returns>The version number of the latest GTN release</returns>
        internal static geoTagNinja.GtnReleasesApiResponse API_GenericGetGTNVersionFromWeb()
        {
            geoTagNinja.GtnReleasesApiResponse returnVal = new();
            var client = new RestClient("https://api.github.com/")
            {
                // admittedly no idea how to do this w/o any auth (as it's not needed) but this appears to work.
                Authenticator = new HttpBasicAuthenticator("demo", "demo")
            };
            var request_GTNVersionQuery = new RestRequest("repos/nemethviktor/GeoTagNinja/releases", Method.Get);
            var response_GTNVersionQuery = client.ExecuteGet(request_GTNVersionQuery);
            if (response_GTNVersionQuery.StatusCode.ToString() == "OK")
            {
                s_APIOkay = true;
                var data = (JArray)JsonConvert.DeserializeObject(response_GTNVersionQuery.Content);
                var gtnReleasesApiResponse = geoTagNinja.GtnReleasesApiResponse.FromJson(data.ToString());
                returnVal = gtnReleasesApiResponse[0]; // latest only
            }
            else
            {
                s_APIOkay = false;
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_WarningGTNVerAPIResponse") + response_GTNVersionQuery.StatusCode.ToString(), "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return returnVal;
        }
        /// <summary>
        /// Performs a search in the local SQLite database for cached toponomy info and if finds it, returns that, else queries the API
        /// </summary>
        /// <param name="lat">latitude/longitude to be queried</param>
        /// <param name="lng">latitude/longitude to be queried</param>
        /// <returns>See summary. Returns the toponomy info either from SQLite if available or the API in DataTable for further processing</returns>
        internal static DataTable DTFromAPIExifGetToponomyFromWebOrSQL(string lat, string lng)
        {
            DataTable dt_Return = new DataTable();
            dt_Return.Clear();
            dt_Return.Columns.Add("CountryCode");
            dt_Return.Columns.Add("Country");
            dt_Return.Columns.Add("City");
            dt_Return.Columns.Add("State");
            dt_Return.Columns.Add("Sub_location");

            /// logic: it's likely that API info doesn't change much...ever. 
            /// Given that in 2022 Crimea is still part of Ukraine according to API response I think this data is static
            /// ....so no need to query stuff that may already be locally available.
            /// FYI this only gets amended when this button gets pressed or if map-to-location button gets pressed on the main form.
            /// ... also on an unrelated note Toponomy vs Toponmy the API calls it one thing and Wikipedia calls it another so go figure
            DataTable dt_ToponomyInSQL = DataReadSQLiteToponomyWholeRow(
                lat: lat,
                lng: lng
                );

            geoTagNinja.GeoResponseToponomy ReadJsonToponomy;

            string CountryCode = "";
            string Country = "";
            string City = "";
            string State = "";
            string Sub_location = "";

            // read from SQL
            if (dt_ToponomyInSQL.Rows.Count > 0)
            {
                CountryCode = dt_ToponomyInSQL.Rows[0]["CountryCode"].ToString();
                Country = DataReadSQLiteCountryCodesNames(
                    queryWhat: "ISO_3166_1A3",
                    inputVal: CountryCode,
                    returnWhat: "Country"
                    );
                if (CountryCode == "GBR")
                {
                    City = dt_ToponomyInSQL.Rows[0]["AdminName2"].ToString();
                    State = dt_ToponomyInSQL.Rows[0]["AdminName1"].ToString();
                    Sub_location = dt_ToponomyInSQL.Rows[0]["ToponymName"].ToString();
                }
                else
                {
                    City = dt_ToponomyInSQL.Rows[0]["ToponymName"].ToString();
                    State = dt_ToponomyInSQL.Rows[0]["AdminName1"].ToString();
                    Sub_location = dt_ToponomyInSQL.Rows[0]["AdminName2"].ToString();
                }
            }
            // read from API
            else if (s_APIOkay)
            {
                ReadJsonToponomy = API_ExifGetGeoDataFromWebToponomy(
                        latitude: lat.ToString(),
                        longitude: lng.ToString()
                        );
                // ignore if unauthorised or some such
                if (ReadJsonToponomy.Geonames != null)
                {
                    if (ReadJsonToponomy.Geonames.Length > 0)
                    {
                        string APICountryCode = ReadJsonToponomy.Geonames[0].CountryCode;
                        if (APICountryCode != null || APICountryCode != "")
                        {
                            CountryCode = DataReadSQLiteCountryCodesNames(
                                queryWhat: "ISO_3166_1A2",
                                inputVal: APICountryCode,
                                returnWhat: "ISO_3166_1A3"
                                );
                            Country = DataReadSQLiteCountryCodesNames(
                                queryWhat: "ISO_3166_1A2",
                                inputVal: APICountryCode,
                                returnWhat: "Country"
                                );
                        }

                        // api sends back some misaligned stuff for the UK
                        if (CountryCode == ("GBR"))
                        {
                            City = ReadJsonToponomy.Geonames[0].AdminName2;
                            State = ReadJsonToponomy.Geonames[0].AdminName1;
                            Sub_location = ReadJsonToponomy.Geonames[0].ToponymName;
                        }
                        else
                        {
                            City = ReadJsonToponomy.Geonames[0].ToponymName;
                            State = ReadJsonToponomy.Geonames[0].AdminName1;
                            Sub_location = ReadJsonToponomy.Geonames[0].AdminName2;
                        }
                        // write back to sql the new stuff
                        DataWriteSQLiteToponomyWholeRow(
                            lat: lat,
                            lng: lng,
                            AdminName1: ReadJsonToponomy.Geonames[0].AdminName1,
                            AdminName2: ReadJsonToponomy.Geonames[0].AdminName2,
                            ToponymName: ReadJsonToponomy.Geonames[0].ToponymName,
                            CountryCode: CountryCode
                            );
                    }
                    else if (s_APIOkay)
                    {
                        // write back empty
                        DataWriteSQLiteToponomyWholeRow(
                            lat: lat,
                            lng: lng
                            );
                    }
                }

            }
            if (s_APIOkay || dt_ToponomyInSQL.Rows.Count > 0)
            {
                DataRow dr_ReturnRow = dt_Return.NewRow();
                dr_ReturnRow["CountryCode"] = CountryCode;
                dr_ReturnRow["Country"] = Country;
                dr_ReturnRow["City"] = City;
                dr_ReturnRow["State"] = State;
                dr_ReturnRow["Sub_location"] = Sub_location;
                dt_Return.Rows.Add(dr_ReturnRow);
            }
            return dt_Return;
        }
        /// <summary>
        /// Performs a search in the local SQLite database for cached altitude info and if finds it, returns that, else queries the API
        /// </summary>
        /// <param name="lat">latitude/longitude to be queried</param>
        /// <param name="lng">latitude/longitude to be queried</param>
        /// <returns>See summary. Returns the altitude info either from SQLite if available or the API in DataTable for further processing</returns>
        internal static DataTable DTFromAPIExifGetAltitudeFromWebOrSQL(string lat, string lng)
        {
            DataTable dt_Return = new DataTable();
            dt_Return.Clear();
            dt_Return.Columns.Add("Altitude");

            string Altitude = "0";

            DataTable dt_AltitudeInSQL = DataReadSQLiteAltitudeWholeRow(
                                lat: lat,
                                lng: lng
                                );


            if (dt_AltitudeInSQL.Rows.Count > 0)
            {
                Altitude = dt_AltitudeInSQL.Rows[0]["Srtm1"].ToString();
            }
            else if (s_APIOkay)
            {
                geoTagNinja.GeoResponseAltitude readJson_Altitude;
                readJson_Altitude = API_ExifGetGeoDataFromWebAltitude(
                    latitude: lat.ToString(),
                    longitude: lng.ToString()
                    );
                if (readJson_Altitude.Srtm1 != null)
                {
                    string tmpAltitude;
                    tmpAltitude = readJson_Altitude.Srtm1.ToString().Replace(',', '.'); ;

                    // ignore if the API sends back something silly.
                    // basically i'm assuming some ppl might take pics on an airplane but even those don't fly this high.
                    // also if you're in the Mariana Trench, do send me a photo, will you?
                    if (Math.Abs(int.Parse(tmpAltitude, NumberStyles.Any, CultureInfo.InvariantCulture)) > 32000)
                    {
                        tmpAltitude = "0";
                    }

                    Altitude = tmpAltitude;
                    // write back to sql the new stuff
                    DataWriteSQLiteAltitudeWholeRow(
                        lat: lat,
                        lng: lng,
                        Altitude: Altitude
                        );
                }
                // this will be a null value if Unauthorised, we'll ignore that.
                if (readJson_Altitude.Lat == null && s_APIOkay)
                {
                    // write back blank
                    DataWriteSQLiteAltitudeWholeRow(
                        lat: lat,
                        lng: lng
                        );
                }
            }

            if (s_APIOkay || dt_AltitudeInSQL.Rows.Count > 0)
            {
                DataRow dr_ReturnRow = dt_Return.NewRow();
                dr_ReturnRow["Altitude"] = Altitude;
                dt_Return.Rows.Add(dr_ReturnRow);
            }
            return dt_Return;
        }
        /// <summary>
        /// Converts the API response from metaCPAN (to check exifTool's newest version) to a DataTable
        /// </summary>
        /// <returns>A Datatable with (hopefully) one row of data containing the newest exifTool version</returns>
        internal static DataTable DTFromAPI_GetExifToolVersion()
        {
            DataTable dt_Return = new DataTable();
            dt_Return.Clear();
            dt_Return.Columns.Add("version");

            string apiVersion = "";

            if (s_APIOkay)
            {
                geoTagNinja.ExifToolVerQueryResponse readJson_ExifToolVer;
                readJson_ExifToolVer = API_ExifGetExifToolVersionFromWeb();
                if (readJson_ExifToolVer.Hits != null)
                {
                    apiVersion = readJson_ExifToolVer.Hits.HitsHits[0].Fields.Version.ToString();
                }
                // this will be a null value if Unauthorised, we'll ignore that.
            }

            if (s_APIOkay)
            {
                DataRow dr_ReturnRow = dt_Return.NewRow();
                dr_ReturnRow["version"] = apiVersion;
                dt_Return.Rows.Add(dr_ReturnRow);
            }
            return dt_Return;
        }
        /// <summary>
        /// Converts the API response from gitHub (to check GTN's newest version) to a DataTable
        /// Actually the reason why this might be indicated as 0 references is because this doesn't run in Debug mode.
        /// </summary>
        /// <returns>A Datatable with (hopefully) one row of data containing the newest GTN version</returns>
        internal static DataTable DTFromAPI_GetGTNVersion()
        {
            DataTable dt_Return = new DataTable();
            dt_Return.Clear();
            dt_Return.Columns.Add("version");

            string apiVersion = "";

            if (s_APIOkay)
            {
                geoTagNinja.GtnReleasesApiResponse readJson_GTNVer;
                readJson_GTNVer = API_GenericGetGTNVersionFromWeb();
                if (readJson_GTNVer.TagName != null)
                {
                    apiVersion = readJson_GTNVer.TagName.ToString();
                }
                // this will be a null value if Unauthorised, we'll ignore that.
            }

            if (s_APIOkay)
            {
                DataRow dr_ReturnRow = dt_Return.NewRow();
                dr_ReturnRow["version"] = apiVersion;
                dt_Return.Rows.Add(dr_ReturnRow);
            }
            return dt_Return;
        }
        /// <summary>
        /// Loads up the Edit (file exif data) Form.
        /// </summary>
        internal static void ExifShowEditFrm()
        {
            int overallCount = 0;
            int fileCount = 0;
            int folderCount = 0;
            frm_editFileData frm_editFileData = new();
            frm_editFileData.lvw_FileListEditImages.Items.Clear();
            frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];
            foreach (ListViewItem selectedItem in frm_mainAppInstance.lvw_FileList.SelectedItems)
            {
                // only deal with files, not folders
                if (File.Exists(Path.Combine(frm_mainAppInstance.tbx_FolderName.Text, selectedItem.Text)))
                {
                    overallCount++;
                    frm_MainApp.folderName = frm_mainAppInstance.tbx_FolderName.Text;
                    frm_editFileData.lvw_FileListEditImages.Items.Add(selectedItem.Text);
                    fileCount++;
                }
                else if (Directory.Exists(Path.Combine(frm_mainAppInstance.tbx_FolderName.Text, selectedItem.Text)))
                {
                    overallCount++;
                    folderCount++;
                }
            }
            if (fileCount > 0)
            {
                frm_editFileData.ShowDialog();
            }
            // basically if the user only selected folders, do nothing
            else if (overallCount == folderCount + fileCount)
            {
                //nothing.
            }
            // we appear to have lost a file or two.
            else
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_WarningFileDisappeared"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        /// <summary>
        /// Queues up a command to remove existing geo-data. Depending on the sender this can be for one or many files.
        /// </summary>
        /// <param name="senderName">At this point this can either be the main listview or the one from Edit (file) data</param>
        internal static void ExifRemoveLocationData(string senderName)
        {
            DataRow dr_FileDataRow;
            if (senderName == "frm_editFileData")
            {
                // for the time being i'll leave this as "remove data from the active selection file" rather than "all".
                frm_editFileData frm_editFileDataInstance = (frm_editFileData)Application.OpenForms["frm_editFileData"];

                // setting this to True prevents the code from checking the values are valid numbers.
                frm_editFileData.frm_editFileDataNowRemovingGeoData = true;
                frm_editFileDataInstance.tbx_GPSLatitude.Text = "";
                frm_editFileDataInstance.tbx_GPSLongitude.Text = "";
                frm_editFileDataInstance.tbx_GPSAltitude.Text = "";
                frm_editFileDataInstance.tbx_City.Text = "";
                frm_editFileDataInstance.tbx_State.Text = "";
                frm_editFileDataInstance.tbx_Sub_location.Text = "";
                frm_editFileDataInstance.cbx_CountryCode.Text = "";
                frm_editFileDataInstance.cbx_Country.Text = "";
                frm_editFileData.frm_editFileDataNowRemovingGeoData = false;
                // no need to write back to sql because it's done automatically on textboxChange except for "special tag"

                dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage1PreQueue.NewRow();
                dr_FileDataRow["filePath"] = frm_editFileDataInstance.lvw_FileListEditImages.SelectedItems[0].Text;
                dr_FileDataRow["settingId"] = "gps*";
                dr_FileDataRow["settingValue"] = "";
                frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Add(dr_FileDataRow);
            }
            else if (senderName == "frm_mainApp")
            {
                frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];

                if (frm_mainAppInstance.lvw_FileList.SelectedItems.Count > 0)
                {
                    foreach (ListViewItem lvi in frm_mainAppInstance.lvw_FileList.SelectedItems)
                    {
                        // don't do folders...
                        if (File.Exists(Path.Combine(frm_MainApp.folderName, lvi.Text)))
                        {
                            // Latitude
                            dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "GPSLatitude";
                            dr_FileDataRow["settingValue"] = "";
                            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                            // Longitude
                            dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "GPSLongitude";
                            dr_FileDataRow["settingValue"] = "";
                            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                            // CountryCode
                            dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "CountryCode";
                            dr_FileDataRow["settingValue"] = "";
                            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                            // Country
                            dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "Country";
                            dr_FileDataRow["settingValue"] = "";
                            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                            // City
                            dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "City";
                            dr_FileDataRow["settingValue"] = "";
                            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                            // State
                            dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "State";
                            dr_FileDataRow["settingValue"] = "";
                            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                            // Sub_location
                            dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "Sub_location";
                            dr_FileDataRow["settingValue"] = "";
                            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                            // Altitude
                            dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "GPSAltitude";
                            dr_FileDataRow["settingValue"] = "";
                            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                            // Force-remove stuff --> https://exiftool.org/forum/index.php?topic=6037.0
                            dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "gps*";
                            dr_FileDataRow["settingValue"] = "";
                            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);
                        }
                    }
                }
                Helper.LwvUpdateRowFromDTWriteStage3ReadyToWrite();
            }
        }
        #endregion
        #region FSO interaction
        /// <summary>
        /// Gets the parent folder of the current path (used when user wants to move a folder up.)
        /// We can't just do a "cd.." because top level folders don't handle that logic well.
        /// </summary>
        /// <param name="path">The "current" path from which the user wants to move one level up</param>
        /// <returns>Parent path string of "current" path (as described above)</returns>
        internal static string FsoGetParent(string path)
        {
            var dir = new DirectoryInfo(path);
            string parentName;
            if (dir.Parent == null)
            {
                parentName = null;
            }
            else
            {
                parentName = dir.Parent.FullName;
            }

            return parentName;
        }
        /// <summary>
        /// When the user leaves the current folder (or refreshes it) we check if there is anything in the write-queue
        /// If the Q is empty we do as the user requested, else ask user if they want to write the data in the Q or discard it.
        /// </summary>
        /// <returns>Realistically nothing but it sets s_changeFolderIsOkay according to the user input and circumstances</returns>
        internal static async Task FsoCheckOutstandingFiledataOkayToChangeFolderAsync()
        {
            s_changeFolderIsOkay = false;

            // check if there's anything in the write-Q
            if (frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Count > 0)
            {
                DialogResult dialogResult = MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_QuestionFileQIsNotEmpty"), "Info", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    await ExifWriteExifToFile();
                    s_changeFolderIsOkay = true;
                }
                else if (dialogResult == DialogResult.No)
                {
                    frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Clear();
                    s_changeFolderIsOkay = true;
                }
            }
            else
            {
                s_changeFolderIsOkay = true;
            }
        }
        /// <summary>
        /// This is called mostly on app exit and start - delete any remaining files such as image previews, txt files and 
        /// ... anything else that isn't an sqlite file.
        /// </summary>
        internal static void FsoCleanUpUserFolder()
        {
            DirectoryInfo di = new DirectoryInfo(frm_MainApp.userDataFolderPath);

            foreach (FileInfo file in di.EnumerateFiles())
            {
                if (file.Extension != ".sqlite")
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // nothing
                    }
            }
        }
        #endregion
        #region lvw_FileList
        /// <summary>
        /// Updates the data sitting in the main listview if there is anything outstanding in dt_fileDataToWriteStage3ReadyToWrite for the file
        /// </summary>
        internal static void LwvUpdateRowFromDTWriteStage3ReadyToWrite()
        {
            frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];
            var lvchs = frm_mainAppInstance.ListViewColumnHeaders;
            ListViewItem lvi;

            string tmpCoordinates;

            foreach (DataRow dr_ThisDataRow in frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows)
            {
                try
                {
                    lvi = frm_mainAppInstance.lvw_FileList.FindItemWithText(dr_ThisDataRow[0].ToString());
                    // theoretically we'd want to update the columns for each tag but for example when removing all data
                    // this becomes tricky bcs we're also firing a "-gps*=" tag.
                    if (lvchs["clh_" + dr_ThisDataRow[1].ToString()] != null)
                    {
                        lvi.ForeColor = Color.Red;
                        lvi.SubItems[lvchs["clh_" + dr_ThisDataRow[1].ToString()].Index].Text = dr_ThisDataRow[2].ToString();
                        //break;
                    }

                    tmpCoordinates = lvi.SubItems[lvchs["clh_GPSLatitude"].Index].Text + ";" + lvi.SubItems[lvchs["clh_GPSLongitude"].Index].Text;
                    lvi.SubItems[lvchs["clh_Coordinates"].Index].Text = tmpCoordinates != ";" ? tmpCoordinates : "";
                }
                catch
                {
                    // nothing. 
                }
                Application.DoEvents();
            }
        }
        /// <summary>
        /// This drives the logic for "copying" (as in copy-paste) the geodata from one file to others.
        /// </summary>
        internal static void LwvCopyGeoData()
        {
            frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];
            if (frm_mainAppInstance.lvw_FileList.SelectedItems.Count == 1)
            {
                ListViewItem lvi = frm_mainAppInstance.lvw_FileList.SelectedItems[0];
                if (File.Exists(Path.Combine(frm_MainApp.folderName, lvi.Text)))
                {
                    frm_MainApp.dt_fileDataCopyPool.Rows.Clear();
                    var listOfTagsToCopy = new List<string> {
                            "Coordinates"
                            ,"GPSLatitude"
                            ,"GPSLatitudeRef"
                            ,"GPSLongitude"
                            ,"GPSLongitudeRef"
                            ,"GPSSpeed"
                            ,"GPSSpeedRef"
                            ,"GPSAltitude"
                            ,"GPSAltitudeRef"
                            ,"Country"
                            ,"CountryCode"
                            ,"State"
                            ,"City"
                            ,"Sub_location"
                            ,"DestCoordinates"
                            ,"GPSDestLatitude"
                            ,"GPSDestLatitudeRef"
                            ,"GPSDestLongitude"
                            ,"GPSDestLongitudeRef"
                            ,"GPSImgDirection"
                            ,"GPSImgDirectionRef"
                            };

                    foreach (ColumnHeader clh in frm_mainAppInstance.lvw_FileList.Columns)
                    {
                        if (listOfTagsToCopy.IndexOf(clh.Name.Substring(4)) >= 0)
                        {
                            DataRow dr_FileDataRow = frm_MainApp.dt_fileDataCopyPool.NewRow();
                            dr_FileDataRow["settingId"] = clh.Name.Substring(4);
                            dr_FileDataRow["settingValue"] = lvi.SubItems[clh.Index].Text;
                            frm_MainApp.dt_fileDataCopyPool.Rows.Add(dr_FileDataRow);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_WarningTooManyFilesSelected"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        /// <summary>
        /// This drives the logic for "pasting" (as in copy-paste) the geodata from one file to others.
        /// See further comments inside
        /// </summary>
        internal static void LwvPasteGeoData()
        {
            frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];
            // check there's antying in copy-pool
            if (frm_MainApp.dt_fileDataCopyPool.Rows.Count > 0)
            {
                foreach (ListViewItem lvi in frm_mainAppInstance.lvw_FileList.SelectedItems)
                {
                    if (File.Exists(Path.Combine(frm_MainApp.folderName, lvi.Text)))
                    {
                        // paste all from copy-pool
                        foreach (DataRow dr in frm_MainApp.dt_fileDataCopyPool.Rows)
                        {
                            string strToWrite;
                            if (dr[1].ToString() == "-")
                            {
                                strToWrite = "";
                            }
                            else
                            {
                                strToWrite = dr[1].ToString();
                            }
                            DataRow dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = dr[0].ToString();
                            dr_FileDataRow["settingValue"] = strToWrite;
                            frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);
                        }
                    }
                }
                // push to lvw
                Helper.LwvUpdateRowFromDTWriteStage3ReadyToWrite();
            }
            else
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_Helper_WarningNothingToPaste"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        /// <summary>
        /// Extracted method for navigating to a set of coordinates on the map.
        /// </summary>
        /// <returns>Nothing in reality</returns>
        internal async static Task LvwItemClickNavigate()
        {
            frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];
            if (frm_mainAppInstance.lvw_FileList.SelectedItems.Count == 1)
            {
                // make sure file still exists. just in case someone deleted it elsewhere
                string fileNameWithPath = Path.Combine(frm_MainApp.folderName, frm_mainAppInstance.lvw_FileList.SelectedItems[0].Text);
                if (File.Exists(fileNameWithPath) && frm_mainAppInstance.lvw_FileList.SelectedItems[0].SubItems.Count > 1)
                {
                    var firstSelectedItem = frm_mainAppInstance.lvw_FileList.SelectedItems[0].SubItems[frm_mainAppInstance.lvw_FileList.Columns["clh_Coordinates"].Index].Text;
                    if (firstSelectedItem != "-" && firstSelectedItem != "")
                    {
                        string strLat = frm_mainAppInstance.lvw_FileList.SelectedItems[0].SubItems[frm_mainAppInstance.lvw_FileList.Columns["clh_Coordinates"].Index].Text.Split(';')[0].Replace(',', '.');
                        string strLng = frm_mainAppInstance.lvw_FileList.SelectedItems[0].SubItems[frm_mainAppInstance.lvw_FileList.Columns["clh_Coordinates"].Index].Text.Split(';')[1].Replace(',', '.');

                        double parsedLat;
                        double parsedLng;
                        if (double.TryParse(strLat, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLat) && double.TryParse(strLng, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLng))
                        {
                            frm_mainAppInstance.tbx_lat.Text = strLat;
                            frm_mainAppInstance.tbx_lng.Text = strLng;
                            frm_mainAppInstance.NavigateMapGo(strLat, strLng);
                        }
                    }
                    else if (s_ResetMapToZero)
                    {
                        frm_mainAppInstance.tbx_lat.Text = "0";
                        frm_mainAppInstance.tbx_lng.Text = "0";
                        frm_mainAppInstance.NavigateMapGo("0", "0");
                    }
                    // leave as-is (most likely the last photo)
                    else
                    {
                        // don't do anything
                    }
                }
                frm_mainAppInstance.pbx_imagePreview.Image = null;

                // via https://stackoverflow.com/a/8701748/3968494
                if (File.Exists(fileNameWithPath))
                {
                    // don't try to do this when a thousand files are selected....
                    if (frm_mainAppInstance.lvw_FileList.SelectedItems.Count == 1)
                    {
                        Image img;

                        FileInfo fi = new(fileNameWithPath);
                        if (fi.Extension == ".jpg")
                        {
                            using (var bmpTemp = new Bitmap(fileNameWithPath))
                            {
                                img = new Bitmap(bmpTemp);
                                frm_mainAppInstance.pbx_imagePreview.Image = img;
                            }
                        }
                        else
                        {
                            string generatedFileName = Path.Combine(frm_MainApp.userDataFolderPath, frm_mainAppInstance.lvw_FileList.SelectedItems[0].Text + ".jpg");
                            // don't run the thing again if file has already been generated
                            if (!File.Exists(generatedFileName))
                            {
                                await Helper.ExifGetImagePreviews(fileNameWithPath);
                            }
                            //sometimes the file doesn't get created. (ie exiftool may fail to extract a preview.)
                            if (File.Exists(generatedFileName))
                            {
                                using (var bmpTemp = new Bitmap(generatedFileName))
                                {
                                    try
                                    {
                                        img = new Bitmap(bmpTemp);
                                        frm_mainAppInstance.pbx_imagePreview.Image = img;
                                    }
                                    catch
                                    {
                                        // nothing.
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Directory.Exists(fileNameWithPath))
                {
                    // nothing.
                }
                else
                {
                    MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorFileGoneMissing" + fileNameWithPath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion
    }
    internal class Helper_NonStatic
    {
        #region Generic
        /// <summary>
        /// Generic summary: these return each subcontrol for a given parent control, for example all buttons in a tabPage etc.
        /// </summary>

        // via https://stackoverflow.com/a/3426721/3968494
        internal IEnumerable<Control> GetAllControls(Control control, Type type)
        {
            var controls = control.Controls.Cast<Control>();

            return controls.SelectMany(ctrl => GetAllControls(ctrl, type))
                                      .Concat(controls)
                                      .Where(c => c.GetType() == type);
        }
        internal IEnumerable<Control> GetAllControls(Control control)
        {
            var controls = control.Controls.Cast<Control>();

            return controls.SelectMany(ctrl => GetAllControls(ctrl))
                                      .Concat(controls);
        }
        // via https://stackoverflow.com/a/19258387/3968494
        internal IEnumerable<ToolStripItem> GetMenuItems(ToolStripItem item)
        {
            if (item is ToolStripMenuItem)
            {
                foreach (ToolStripItem tsi in (item as ToolStripMenuItem).DropDownItems)
                {
                    if (tsi is ToolStripMenuItem)
                    {
                        if ((tsi as ToolStripMenuItem).HasDropDownItems)
                        {
                            foreach (ToolStripItem subItem in GetMenuItems((tsi as ToolStripMenuItem)))
                                yield return subItem;
                        }
                        yield return (tsi as ToolStripMenuItem);
                    }
                    else if (tsi is ToolStripSeparator)
                    {
                        yield return (tsi as ToolStripSeparator);
                    }
                }
            }
            else if (item is ToolStripSeparator)
            {
                yield return (item as ToolStripSeparator);
            }
        }
        #endregion
    }
}
