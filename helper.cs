using CoenM.ExifToolLib;
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
        #endregion
        #region SQL 
        #region Database Creation SQL & Startup Checks
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
        internal static string DataSelectTbxARCGIS_APIKey_FromSQLite()
        {
            try
            {
                s_ArcGIS_APIKey = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_ARCGIS_APIKey");
                if (s_ArcGIS_APIKey == null || s_ArcGIS_APIKey == "")
                {
                    //MessageBox.Show("You'll need to provide an ArcGIS API Key in the Settings for the app to work properly.");
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
        internal static string GenericCoalesce(params string[] strings)
        {
            return strings.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        }
        public static double GenericAdjustLatLongNegative(string point)
        {
            string pointOrig = point.ToString().Replace(" ", "").Replace(',', '.');
            double pointVal = double.Parse(Regex.Replace(pointOrig, "[SWNE\"-]", ""), NumberStyles.Any, CultureInfo.InvariantCulture);
            pointVal = Math.Round(pointVal, 6);
            var multiplier = (point.Contains("S") || point.Contains("W")) ? -1 : 1; //handle south and west

            return pointVal * multiplier;
        }
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

        #endregion
        #region Exif Related
        internal static string ExifGetExifDataPoint(DataTable dtFileExif, string dataPoint)
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
                tryDataValue = ExifGetDataPointFromExif(dtFileExif, dataPoint);
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
                            tmpLatLongRefVal = ExifGetDataPointFromExif(dtFileExif, dataPoint + "Ref").Substring(0, 1);
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
                    tmpLatVal = ExifGetDataPointFromExif(dtFileExif, "GPS" + isDest + "Latitude").Replace(',', '.');
                    tmpLongVal = ExifGetDataPointFromExif(dtFileExif, "GPS" + isDest + "Longitude").Replace(',', '.');
                    if (tmpLatVal == "") { tmpLatVal = "-"; };
                    if (tmpLongVal == "") { tmpLongVal = "-"; };

                    if (ExifGetDataPointFromExif(dtFileExif, "GPS" + isDest + "LatitudeRef").Length > 0 && ExifGetDataPointFromExif(dtFileExif, "GPS" + isDest + "LongitudeRef").Length > 0)
                    {
                        tmpLatRefVal = ExifGetDataPointFromExif(dtFileExif, "GPS" + isDest + "LatitudeRef").Substring(0, 1).Replace(',', '.');
                        tmpLongRefVal = ExifGetDataPointFromExif(dtFileExif, "GPS" + isDest + "LongitudeRef").Substring(0, 1).Replace(',', '.');
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
                            tryDataValue = tryDataValue.Replace("mm", "").Replace("f/", "").Replace("f", "").Trim();
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
        internal static string ExifGetDataPointFromExif(DataTable dtFileExif, string dataPoint)
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
                        MessageBox.Show("asyncExifTool.ExecuteAsync failed: " + ex.Message);
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
                            MessageBox.Show("asyncExifTool.ExecuteAsync failed: " + ex.Message);
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
                            string str = ExifGetExifDataPoint(dt_fileExifTable, lvchs[i].Name.Substring(4));
                            lvi.SubItems[i].Text = str;
                        }
                        lvi.ForeColor = Color.Black;
                        frm_MainApp.HandlerUpdateLabelText(frm_mainAppInstance.lbl_ParseProgress, "Processing: " + lvi.Text);
                    }
                }
            }
            frm_MainApp.HandlerUpdateLabelText(frm_mainAppInstance.lbl_ParseProgress, "Ready.");
        }
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
                if(s_ErrorMsg != "")
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
                            string str = ExifGetExifDataPoint(dt_distinctFileExifTable, lvchs[i].Name.Substring(4));
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
                                    if(exiftoolTagName == "exif:GPSLatitude" || exiftoolTagName == "exif:GPSDestLatitude")
                                    {
                                        if(updateExifVal.Substring(0,1) == "-")
                                        {
                                            exifArgs += "-" + exiftoolTagName+"Ref" + "=" + "South"+ Environment.NewLine;
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
                                else
                                {
                                    exifArgs += "-" + exiftoolTagName + "=" + Environment.NewLine;
                                    //if lat/long then add Ref. 
                                    if (exiftoolTagName == "GPSLatitude" || exiftoolTagName == "GPSDestLatitude")
                                    {
                                        if (updateExifVal.Substring(0, 1) == "-")
                                        {
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "=" +Environment.NewLine;
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
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "="  + Environment.NewLine;
                                        }
                                        else
                                        {
                                            exifArgs += "-" + exiftoolTagName + "Ref" + "="  + Environment.NewLine;
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
                            else
                            {
                                exifArgs += "-" + exiftoolTagName + "=" + Environment.NewLine;
                                //if lat/long then add Ref. 
                            }
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
                            else if (data.Data != null && !data.Data.Contains("files updated") && data.Data.Length > 0)
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
        internal static geoTagNinja.GeoResponseToponomy ExifGetGeoDataFromWebToponomy(string latitude, string longitude)
        {
            if (s_GeoNames_UserName == null)
            {
                try
                {
                    s_GeoNames_UserName = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                    s_GeoNames_Pwd = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
                }
                catch
                {
                    MessageBox.Show("Failed to read default values from SQL");
                }
            }

            geoTagNinja.GeoResponseToponomy returnVal = new();
            var client = new RestClient("http://api.geonames.org/")
            {
                Authenticator = new HttpBasicAuthenticator(s_GeoNames_UserName, s_GeoNames_Pwd)
            };

            var request_Toponomy = new RestRequest("findNearbyPlaceNameJSON?lat=" + latitude + "&lng=" + longitude + "&style=FULL", Method.Get);
            var response_Toponomy = client.ExecuteGet(request_Toponomy);
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
                MessageBox.Show("GeoNames API Returned the following response:" + response_Toponomy.StatusCode.ToString(), "Uh oh...",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return returnVal;
        }
        internal static geoTagNinja.GeoResponseAltitude ExifGetGeoDataFromWebAltitude(string latitude, string longitude)
        {
            if (s_GeoNames_UserName == null)
            {
                try
                {
                    s_GeoNames_UserName = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                    s_GeoNames_Pwd = DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
                }
                catch
                {
                    MessageBox.Show("Failed to read default values from SQL");
                }
            }
            geoTagNinja.GeoResponseAltitude returnVal = new();
            var client = new RestClient("http://api.geonames.org/")
            {
                Authenticator = new HttpBasicAuthenticator(s_GeoNames_UserName, s_GeoNames_Pwd)
            };

            var request_Altitude = new RestRequest("srtm1JSON?lat=" + latitude + "&lng=" + longitude, Method.Get);
            var response_Altitude = client.ExecuteGet(request_Altitude);
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
                MessageBox.Show("GeoNames API Returned the following response:" + response_Altitude.StatusCode.ToString(), "Uh oh...",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return returnVal;
        }
        internal static DataTable ExifGetToponomyFromWeb(string lat, string lng)
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
                ReadJsonToponomy = ExifGetGeoDataFromWebToponomy(
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
        internal static DataTable ExifGetAltitudeFromWeb(string lat, string lng)
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
                readJson_Altitude = ExifGetGeoDataFromWebAltitude(
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
        #endregion
        #region FSO interaction
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
        internal static async Task FsoCheckOutstandingFiledataOkayToChangeFolderAsync()
        {
            s_changeFolderIsOkay = false;

            // check if there's anything in the write-Q
            if (frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Count > 0)
            {
                DialogResult dialogResult = MessageBox.Show("Some data has been changed - want to save? \nData will be discarded if you click No)", "Oustanding Data", MessageBoxButtons.YesNoCancel);
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
        internal static void LwvUpdateRow()
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
                    try
                    {
                        lvi.ForeColor = Color.Red;
                        lvi.SubItems[lvchs["clh_" + dr_ThisDataRow[1].ToString()].Index].Text = dr_ThisDataRow[2].ToString();
                        //break;
                    }
                    catch (NullReferenceException)
                    {
                        MessageBox.Show("No column with name " + "clh_" + dr_ThisDataRow[1].ToString());
                    }
                    tmpCoordinates = lvi.SubItems[lvchs["clh_GPSLatitude"].Index].Text + ";" + lvi.SubItems[lvchs["clh_GPSLongitude"].Index].Text;
                    lvi.SubItems[lvchs["clh_Coordinates"].Index].Text = tmpCoordinates != ";" ? tmpCoordinates : "";
                }

                catch
                {

                }
                Application.DoEvents();
            }
        }
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
                MessageBox.Show("This will only work if you have one and only one file selected.");
            }
        }
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
                Helper.LwvUpdateRow();
            }
            else
            {
                MessageBox.Show("Nothing to paste.");
            }
        }
        #endregion
    }
    internal class Helper_NonStatic
    {
        #region Generic
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
