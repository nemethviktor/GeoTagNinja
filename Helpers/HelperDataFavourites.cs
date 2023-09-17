using System.Data;
using System.Data.SQLite;
using GeoTagNinja.Model;

namespace GeoTagNinja.Helpers;

internal static class HelperDataFavourites
{
    /// <summary>
    ///     Deletes the given "favourite" from the relevant table
    /// </summary>
    /// <param name="favouriteName">Name of the "favourite" (like "home")</param>
    internal static void DataDeleteSQLiteFavourite(string favouriteName)
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                DELETE FROM Favourites
                                WHERE favouriteName = @favouriteName
                                ;"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@favouriteName", value: favouriteName);

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Reads the favourites table
    /// </summary>
    /// <returns></returns>
    internal static DataTable DataReadSQLiteFavourites(bool structureOnly = false)
    {
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
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

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                UPDATE Favourites
                                SET favouriteName = @newName
                                WHERE favouriteName = @oldName
                                ;"
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@oldName", value: oldName);
        sqlToRun.Parameters.AddWithValue(parameterName: "@newName", value: newName);

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Saves a new Favourite to the table
    /// </summary>
    internal static void DataWriteSQLiteAddNewFavourite(DataRow drFavourite)
    {
        FrmMainApp.Logger.Trace(message: "Starting");
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                REPLACE INTO Favourites (
                                    favouriteName,
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
        foreach (SourcesAndAttributes.ElementAttribute attribute in HelperGenericAncillaryListsArrays.GetFavouriteTags())
        {
            string attributeStr = SourcesAndAttributes.GetAttributeName(attribute: attribute);
            sqlToRun.Parameters.AddWithValue(parameterName: "@" + attribute, value: drFavourite[columnName: attributeStr]
                                                 .ToString());
        }

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     Updates an existing Favourite with values
    /// </summary>
    /// <param name="favouriteName">The one to update</param>
    /// <param name="countryCode"></param>
    /// <param name="city">value to pass</param>
    /// <param name="state">value to pass</param>
    /// <param name="subLocation">value to pass</param>
    internal static void DataWriteSQLiteUpdateFavourite(string favouriteName,
                                                        string countryCode,
                                                        string city,
                                                        string state,
                                                        string subLocation)
    {
        FrmMainApp.Logger.Trace(message: "Starting");
        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                UPDATE Favourites 
                                SET
                                    CountryCode = @CountryCode,
                                    City = @City,
                                    State = @State,
                                    Sub_location = @Sub_location
                                WHERE favouriteName = @favouriteName;
                                "
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);
        sqlToRun.Parameters.AddWithValue(parameterName: "@favouriteName", value: favouriteName);
        sqlToRun.Parameters.AddWithValue(parameterName: "@CountryCode", value: countryCode);
        sqlToRun.Parameters.AddWithValue(parameterName: "@City", value: city);
        sqlToRun.Parameters.AddWithValue(parameterName: "@State", value: state);
        sqlToRun.Parameters.AddWithValue(parameterName: "@Sub_location", value: subLocation);

        sqlToRun.ExecuteNonQuery();
    }


    /// <summary>
    ///     Creates a table for the user's "favourites".
    ///     This is a bit of a f...up because originally this was favouriteName and then I started using favouriteName, which
    ///     lends itself better to what it is but the columnName now has been released so...
    /// </summary>
    internal static void DataCreateSQLiteFavourites()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
        sqliteDB.Open();

        string sqlCommandStr = @"
                                CREATE TABLE IF NOT EXISTS Favourites(
                                        favouriteName NTEXT NOT NULL PRIMARY KEY,
                                        GPSLatitude NTEXT NOT NULL,
                                        GPSLatitudeRef NTEXT NOT NULL,
                                        GPSLongitude NTEXT NOT NULL,
                                        GPSLongitudeRef NTEXT NOT NULL,
                                        GPSAltitude NTEXT,
                                        GPSAltitudeRef NTEXT,
                                        Coordinates NTEXT NOT NULL,
                                        City NTEXT,
                                        CountryCode NTEXT,
                                        Country NTEXT,
                                        State NTEXT,
                                        Sub_location NTEXT
                                        )
                                ;
                                "
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        sqlToRun.ExecuteNonQuery();
    }

    /// <summary>
    ///     See comment above but generally the problem here is that I started the coding as locationName and then at some
    ///     stage renamed the code to favouriteName so I need to check and eventually rename any user's data if the old column
    ///     name exists.
    /// </summary>
    /// <returns></returns>
    internal static void DataWriteSQLiteRenameFavouritesLocationNameCol()
    {
        try
        {
            using SQLiteConnection sqliteDB = new(connectionString: "Data Source=" + HelperVariables.SettingsDatabaseFilePath);
            sqliteDB.Open();

            // Get the schema for the columns in the database.
            DataTable colsTable = sqliteDB.GetSchema(collectionName: "Columns");

            // Query the columns schema using SQL statements to work out if the required columns exist.
            bool locationNameExists = colsTable.Select(filterExpression: "COLUMN_NAME='locationName' AND TABLE_NAME='Favourites'")
                                          .Length !=
                                      0;
            bool favouriteNameExists = colsTable.Select(filterExpression: "COLUMN_NAME='favouriteName' AND TABLE_NAME='Favourites'")
                                           .Length !=
                                       0;
            if (locationNameExists)
            {
                string sqlCommandStr = @"
                                ALTER TABLE Favourites
                                RENAME COLUMN locationName TO favouriteName

                                ;
                                "
                    ;

                SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

                sqlToRun.ExecuteNonQuery();
            }

            sqliteDB.Close();
        }
        catch
        {
            // nothing
        }
    }
}