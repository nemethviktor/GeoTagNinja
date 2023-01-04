using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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
                            CREATE TABLE [Favourites](
                                        [favouriteName] NTEXT NOT NULL PRIMARY KEY,
                                        [GPSLatitude] NTEXT NOT NULL,
                                        [GPSLatitudeRef] NTEXT NOT NULL,
                                        [GPSLongitude] NTEXT NOT NULL,
                                        [GPSLongitudeRef] NTEXT NOT NULL,
                                        [GPSAltitude] NTEXT,
                                        [GPSAltitudeRef] NTEXT,
                                        [Coordinates] NTEXT NOT NULL,
                                        [City] NTEXT,
                                        [CountryCode] NTEXT,
                                        [Country] NTEXT,
                                        [State] NTEXT,
                                        [Sub_location] NTEXT
                                        )
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
                DataCreateSQLiteFavourites();
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

    #region language & TZ DT

    internal static string DataReadDTObjectText(string objectType,
                                                string objectName)
    {
        return (from kvp in AncillaryListsArrays.commonNamesKVP
                where kvp.Key == objectType + "_" + objectName
                select kvp.Value).FirstOrDefault();
    }

    /// <summary>
    ///     Reads all the language CSV files into one table (FrmMainApp.DtLanguageLabels)
    /// </summary>
    internal static void DataReadLanguageDataFromCSV()
    {
        string languagesFolderPath = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "Languages");

        DataTable dtParsed = new();
        dtParsed.Clear();
        dtParsed.Columns.Add(columnName: "languageName");
        dtParsed.Columns.Add(columnName: "objectType");
        dtParsed.Columns.Add(columnName: "objectName");
        dtParsed.Columns.Add(columnName: "objectText");

        foreach (string fileNameWithPath in Directory.GetFiles(path: languagesFolderPath, searchPattern: "*.csv"))
        {
            DataTable dtObject = GetDataTableFromCsv(fileNameWithPath: fileNameWithPath, isUTF: true);

            dtParsed.Clear();

            string objectType = Path.GetFileNameWithoutExtension(path: fileNameWithPath); // e.g. "Button.csv" -> Button

            foreach (DataRow drObjectRow in dtObject.Rows)
            {
                string objectName = drObjectRow[columnName: "objectName"]
                    .ToString();

                for (int i = 1; i < dtObject.Columns.Count; i++)
                {
                    string languageName = dtObject.Columns[index: i]
                        .ColumnName;
                    string objectText = drObjectRow[columnName: languageName]
                        .ToString();
                    if (objectText.Length == 0)
                    {
                        objectText = null;
                    }

                    DataRow drOut = dtParsed.NewRow();
                    drOut[columnName: "languageName"] = languageName;
                    drOut[columnName: "objectType"] = objectType;
                    drOut[columnName: "objectName"] = objectName;
                    drOut[columnName: "objectText"] = objectText;
                    dtParsed.Rows.Add(row: drOut);
                }
            }

            FrmMainApp.DtLanguageLabels.Merge(table: dtParsed);
        }

        // this is far from optimal but for what we need it will do
        // it's only used for pre-caching some Form labels (for now, Edit.)
        for (int i = 1; i <= 2; i++)
        {
            // run 1 is English
            // run 2 is FrmMainApp._AppLanguage
            // hashset takes care of the rest
            string languageNameToGet = null;

            languageNameToGet = i == 1
                ? "English"
                : FrmMainApp._AppLanguage;

            // no need to waste resource.
            if (!(i == 2 && FrmMainApp._AppLanguage == "English"))
            {
                EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in FrmMainApp.DtLanguageLabels.AsEnumerable()
                                                                   where dataRow.Field<string>(columnName: "languageName") == languageNameToGet
                                                                   select dataRow;

                foreach (DataRow drObject in drDataTableData)
                {
                    if (drObject[columnName: "objectText"] != null &&
                        drObject[columnName: "objectText"]
                            .ToString()
                            .Length >
                        0)
                    {
                        string objectName = drObject[columnName: "objectType"] +
                                            "_" +
                                            drObject[columnName: "objectName"];
                        string objectText = drObject[columnName: "objectText"]
                            .ToString();

                        if (i == 2)
                        {
                            AncillaryListsArrays.commonNamesKVP.RemoveAll(match: item => item.Key.Equals(obj: objectName));
                        }

                        AncillaryListsArrays.commonNamesKVP.Add(item: new KeyValuePair<string, string>(key: objectName, value: objectText));
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Reads the Country -> TZ data from the CSV file into a DT
    /// </summary>
    internal static void DataReadTZDataFromCSV()
    {
        string countryCodeCsvFilePath = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "isoCountryCodeMapping.csv");
        FrmMainApp.DtIsoCountryCodeMapping = GetDataTableFromCsv(fileNameWithPath: countryCodeCsvFilePath, isUTF: true);
    }

    /// <summary>
    ///     Reads the FrmMainApp.DtIsoCountryCodeMapping and basically translates between code types. We store ALPHA-2, ALPHA-3
    ///     and plain English country names.
    /// </summary>
    /// <param name="queryWhat">ALPHA-2, ALPHA-3 and plain English country names</param>
    /// <param name="inputVal">e.g US or USA or United States of America</param>
    /// <param name="returnWhat">ALPHA-2, ALPHA-3 and plain English country names</param>
    internal static string DataReadDTCountryCodesNames(string queryWhat,
                                                       string inputVal,
                                                       string returnWhat)
    {
        EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in FrmMainApp.DtIsoCountryCodeMapping.AsEnumerable()
                                                           where dataRow.Field<string>(columnName: queryWhat) == inputVal
                                                           select dataRow;

        string returnString = "";
        Parallel.ForEach(source: drDataTableData, body: dataRow =>
            {
                returnString = dataRow[columnName: returnWhat]
                    .ToString();
            })
            ;
        return returnString;
    }

    #endregion

    #region favourites SQL

    /// <summary>
    ///     Creates a table for the user's "favourites".
    ///     This is a bit of a f...up because originally this was locationName and then I started using favouriteName, which
    ///     lends itself better to what it is but the columnName now has been released so...
    /// </summary>
    private static void DataCreateSQLiteFavourites()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                CREATE TABLE IF NOT EXISTS [Favourites](
                                        [locationName] NTEXT NOT NULL PRIMARY KEY,
                                        [GPSLatitude] NTEXT NOT NULL,
                                        [GPSLatitudeRef] NTEXT NOT NULL,
                                        [GPSLongitude] NTEXT NOT NULL,
                                        [GPSLongitudeRef] NTEXT NOT NULL,
                                        [GPSAltitude] NTEXT,
                                        [GPSAltitudeRef] NTEXT,
                                        [Coordinates] NTEXT NOT NULL,
                                        [City] NTEXT,
                                        [CountryCode] NTEXT,
                                        [Country] NTEXT,
                                        [State] NTEXT,
                                        [Sub_location] NTEXT
                                        )
                                ;"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Reads the favourites table
    /// </summary>
    /// <returns></returns>
    internal static DataTable DataReadSQLiteFavourites(bool structureOnly = false)
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                SELECT *
                                FROM Favourites
                                WHERE 1=1
                                ORDER BY 1

								"
            ;

        if (structureOnly)
        {
            sqlCommandStr += "LIMIT 0";
        }

        sqlCommandStr += ";";

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        SQLiteDataReader reader = sqlToRun.ExecuteReader();
        DataTable dataTable = new();
        dataTable.Load(reader: reader);
        return dataTable;
    }

    /// <summary>
    ///     Updates the name (aka renames) a particular Favourite
    /// </summary>
    /// <param name="oldName">Existing name</param>
    /// <param name="newName">New name to use</param>
    internal static void DataRenameSQLiteFavourite(string oldName,
                                                   string newName)
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                UPDATE Favourites
                                SET locationName = @newName
                                WHERE locationName = @oldName
                                ;"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@oldName", value: oldName);
        sqlToRun.Parameters.AddWithValue(parameterName: "@newName", value: newName);

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Deletes the given "favourite" from the relevant table
    /// </summary>
    /// <param name="favouriteName">Name of the "favourite" (like "home")</param>
    internal static void DataDeleteSQLiteFavourite(string favouriteName)
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                DELETE FROM Favourites
                                WHERE locationName = @favouriteName
                                ;"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@favouriteName", value: favouriteName);

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Updates an existing Favourite with values
    /// </summary>
    /// <param name="favouriteName">The one to update</param>
    /// <param name="city">value to pass</param>
    /// <param name="state">value to pass</param>
    /// <param name="subLocation">value to pass</param>
    internal static void DataWriteSQLiteUpdateFavourite(string favouriteName,
                                                        string city,
                                                        string state,
                                                        string subLocation)
    {
        FrmMainApp.Logger.Trace(message: "Starting");
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                UPDATE Favourites 
                                SET
                                    City = @City,
                                    State = @State,
                                    Sub_location = @Sub_location
                                WHERE locationName = @favouriteName;
                                "
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@favouriteName", value: favouriteName);
        sqlToRun.Parameters.AddWithValue(parameterName: "@City", value: city);
        sqlToRun.Parameters.AddWithValue(parameterName: "@State", value: state);
        sqlToRun.Parameters.AddWithValue(parameterName: "@Sub_location", value: subLocation);

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Saves a new Favourite to the table
    /// </summary>
    internal static void DataWriteSQLiteAddNewFavourite(DataRow drFavourite)
    {
        FrmMainApp.Logger.Trace(message: "Starting");
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + SSettingsDataBasePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                REPLACE INTO Favourites (
                                    locationName,
                                    GPSAltitude,
                                    GPSAltitudeRef,
                                    GPSLatitude,
                                    GPSLatitudeRef,
                                    GPSLongitude,
                                    GPSLongitudeRef,
                                    Coordinates,
                                    City,
                                    CountryCode,
                                    Country,
                                    State,
                                    Sub_location
                                    ) " +
                               "VALUES (@favouriteName, @GPSAltitude,@GPSAltitudeRef,@GPSLatitude,@GPSLatitudeRef,@GPSLongitude,@GPSLongitudeRef,@Coordinates,@City,@CountryCode,@Country,@State,@Sub_location);"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@favouriteName", value: drFavourite[columnName: "favouriteName"]
                                             .ToString());
        foreach (string tagName in AncillaryListsArrays.GetFavouriteTags())
        {
            sqlToRun.Parameters.AddWithValue(parameterName: "@" + tagName, value: drFavourite[columnName: tagName]
                                                 .ToString());
        }

        sqlToRun.ExecuteNonQuery();
    }

    #endregion

    #region Other DT

    /// <returns>ALPHA-2, ALPHA-3 and plain English country names</returns>
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