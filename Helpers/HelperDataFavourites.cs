using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using GeoTagNinja.Model;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;

// ReSharper disable InconsistentNaming

namespace GeoTagNinja.Helpers;

internal static class HelperDataFavourites
{
    #region GeoSetter Favourites/Import

    /// <summary>
    ///     Reads the gs favs (fake) xml file, converts to GTN-favs and saves.
    /// </summary>
    /// <param name="fileNameToParse"></param>
    internal static void ParseGeoSetterFavouritesXmlToFavourite(string fileNameToParse)
    {
        bool updateFavouritesInSQLiteAtTheEnd = true;
        int itemsImported = 0;

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        HashSet<GeoSetterFavourite> geoSetterFavourites = new();
        try
        {
            XDocument doc = XDocument.Parse(text: File.ReadAllText(path: fileNameToParse));
            foreach (XElement item in doc.Descendants(name: "geosetterpositions").Elements())
            {
                GeoSetterFavourite geoSetterFavourite = new()
                {
                    name = (string)item.Attribute(name: "name"),
                    lat = double.TryParse(s: (string)item.Attribute(name: "lat"),
                        style: NumberStyles.Any,
                        provider: CultureInfo.InvariantCulture, result: out double lat)
                        ? lat
                        : 0,
                    lng = double.TryParse(s: (string)item.Attribute(name: "lng"), style: NumberStyles.Any,
                        provider: CultureInfo.InvariantCulture, result: out double lng)
                        ? lng
                        : 0,
                    radius = int.TryParse(s: (string)item.Attribute(name: "radius"), style: NumberStyles.Any,
                        provider: CultureInfo.InvariantCulture, result: out int radius)
                        ? radius
                        : 0,
                    autoassign = int.TryParse(s: (string)item.Attribute(name: "autoassign"), style: NumberStyles.Any,
                        provider: CultureInfo.InvariantCulture, result: out int autoassign)
                        ? autoassign
                        : 0,
                    snap = int.TryParse(s: (string)item.Attribute(name: "snap"), style: NumberStyles.Any,
                        provider: CultureInfo.InvariantCulture, result: out int snap)
                        ? snap
                        : 0,
                    alt = int.TryParse(s: (string)item.Attribute(name: "alt"), style: NumberStyles.Any,
                        provider: CultureInfo.InvariantCulture, result: out int alt)
                        ? alt
                        : 0,
                    tz = (string)item.Attribute(name: "tz"),
                    ctrycode = (string)item.Attribute(name: "ctrycode"),
                    ctry = (string)item.Attribute(name: "ctry"),
                    state = (string)item.Attribute(name: "state"),
                    city = (string)item.Attribute(name: "city"),
                    subloc = (string)item.Attribute(name: "subloc")
                };
                geoSetterFavourites.Add(item: geoSetterFavourite);
            }
        }
        catch (Exception ex)
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorImportGeoSetterFavourites",
                captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK,
                extraMessage: ex.Message);

            updateFavouritesInSQLiteAtTheEnd = false;
        }

        if (geoSetterFavourites.Any())
        {
            foreach (GeoSetterFavourite geoSetterFavourite in geoSetterFavourites.TakeWhile(
                         predicate: geoSetterFavourite => !string.IsNullOrWhiteSpace(value: geoSetterFavourite.name)))
            {
                try
                {
                    Favourite favourite = new()
                    {
                        FavouriteName = geoSetterFavourite.name,
                        GPSLatitude = geoSetterFavourite.lat.ToString(provider: CultureInfo.InvariantCulture),
                        GPSLatitudeRef = geoSetterFavourite.lat < 0.0 ? "South" : "North",
                        GPSLongitude = geoSetterFavourite.lng.ToString(provider: CultureInfo.InvariantCulture),
                        GPSLongitudeRef = geoSetterFavourite.lng < 0.0 ? "West" : "East",
                        GPSAltitude =
                            geoSetterFavourite.alt.ToString(
                                provider: CultureInfo.InvariantCulture), // this is always in meters
                        GPSAltitudeRef = geoSetterFavourite.alt > 0 ? "Above Sea Level" : "Below Sea Level",
                        Coordinates =
                            $"{geoSetterFavourite.lat.ToString(provider: CultureInfo.InvariantCulture)};{geoSetterFavourite.lng.ToString(provider: CultureInfo.InvariantCulture)}",
                        City = geoSetterFavourite.city,
                        CountryCode = geoSetterFavourite.ctrycode,
                        Country = geoSetterFavourite.ctry,
                        State = geoSetterFavourite.state,
                        Sublocation = geoSetterFavourite.subloc
                    };


                    Favourite existingFavouriteWithThisName =
                        frmMainAppInstance.GetFavouriteByName(favouriteName: favourite.FavouriteName);
                    if (existingFavouriteWithThisName != null)
                    {
                        frmMainAppInstance.RemoveFavouriteByName(
                            favouriteName: existingFavouriteWithThisName.FavouriteName);
                    }

                    FrmMainApp.Favourites.Add(item: favourite);
                    itemsImported++;
                }
                catch (Exception e)
                {
                    HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                        controlName: "mbx_FrmMainApp_ErrorImportGeoSetterFavourites",
                        captionType: MessageBoxCaption.Error,
                        buttons: MessageBoxButtons.OK,
                        extraMessage: e.Message);

                    updateFavouritesInSQLiteAtTheEnd = false;
                }
            }
        }
        else
        {
            updateFavouritesInSQLiteAtTheEnd = false;
        }

        if (updateFavouritesInSQLiteAtTheEnd)
        {
            DataWriteSQLiteClearAndUpdateFavourites();
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_InfoImportGeoSetterFavouritesDone",
                captionType: MessageBoxCaption.Information,
                buttons: MessageBoxButtons.OK,
                extraMessage: itemsImported.ToString());

            frmMainAppInstance.ClearReloadFavouritesDropDownValues();
        }
    }

    #endregion


    /// <summary>
    ///     Reads the Favourites table from SQLite.
    /// </summary>
    internal static void DataReadSQLiteFavourites()
    {
        using SQLiteConnection sqliteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        sqliteDB.Open();

        string sqlCommandStr = @"
                                SELECT *
                                FROM Favourites
                                WHERE 1=1
                                ORDER BY 1;

								"
            ;


        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        SQLiteDataReader reader = sqlToRun.ExecuteReader();
        DataTable dataTable = new();
        dataTable.Load(reader: reader);
        FrmMainApp.Favourites.Clear();

        foreach (DataRow row in dataTable.Rows)
        {
            Favourite fav = new()
            {
                FavouriteName = row[columnName: "favouriteName"]?.ToString(),
                GPSLatitude = row[columnName: "GPSLatitude"]?.ToString(),
                GPSLatitudeRef = row[columnName: "GPSLatitudeRef"]?.ToString(),
                GPSLongitude = row[columnName: "GPSLongitude"]?.ToString(),
                GPSLongitudeRef = row[columnName: "GPSLongitudeRef"]?.ToString(),
                GPSAltitude = row[columnName: "GPSAltitude"]?.ToString(),
                GPSAltitudeRef = row[columnName: "GPSAltitudeRef"]?.ToString(),
                Coordinates = row[columnName: "Coordinates"]?.ToString(),
                City = row[columnName: "City"]?.ToString(),
                CountryCode = row[columnName: "CountryCode"]?.ToString(),
                Country = row[columnName: "Country"]?.ToString(),
                State = row[columnName: "State"]?.ToString(),
                Sublocation = row[columnName: "Sublocation"]?.ToString()
            };

            FrmMainApp.Favourites.Add(item: fav);
        }
    }

    /// <summary>
    ///     Updates the name (aka renames) a particular Favourite
    /// </summary>
    /// <param name="oldName">Existing name</param>
    /// <param name="newName">New name to use</param>
    internal static void DataRenameSQLiteFavourite(string oldName,
                                                   string newName)
    {
        FrmMainApp.Log.Info(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
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
    ///     Clears the whole Favourites table and rewrites it.
    ///     Obvs chatGPT because I'm lazy.
    /// </summary>
    internal static void DataWriteSQLiteClearAndUpdateFavourites()
    {
        FrmMainApp.Log.Info(message: "Starting");

        using SQLiteConnection sqliteDB =
            new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        sqliteDB.Open();

        using SQLiteTransaction transaction = sqliteDB.BeginTransaction();

        try
        {
            // Clear the table first
            using (SQLiteCommand deleteCommand = new(commandText: "DELETE FROM Favourites;", connection: sqliteDB,
                       transaction: transaction))
            {
                deleteCommand.ExecuteNonQuery();
            }

            // Prepare the INSERT command once and reuse it
            using (SQLiteCommand insertCommand = new(commandText: @"
            INSERT INTO Favourites (
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
                Sublocation
            ) VALUES (
                @favouriteName,
                @GPSAltitude,
                @GPSAltitudeRef,
                @GPSLatitude,
                @GPSLatitudeRef,
                @GPSLongitude,
                @GPSLongitudeRef,
                @Coordinates,
                @City,
                @CountryCode,
                @Country,
                @State,
                @Sublocation
            );", connection: sqliteDB, transaction: transaction))
            {
                // Add parameters once — values will be updated inside the loop
                insertCommand.Parameters.Add(parameterName: "@favouriteName", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@GPSLatitude", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@GPSLatitudeRef", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@GPSLongitude", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@GPSLongitudeRef", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@GPSAltitude", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@GPSAltitudeRef", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@Coordinates", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@City", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@CountryCode", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@Country", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@State", parameterType: DbType.String);
                insertCommand.Parameters.Add(parameterName: "@Sublocation", parameterType: DbType.String);

                foreach (Favourite favourite in FrmMainApp.Favourites)
                {
                    insertCommand.Parameters[parameterName: "@favouriteName"].Value = favourite.FavouriteName;
                    insertCommand.Parameters[parameterName: "@GPSLatitude"].Value = favourite.GPSLatitude;
                    insertCommand.Parameters[parameterName: "@GPSLatitudeRef"].Value = favourite.GPSLatitudeRef;
                    insertCommand.Parameters[parameterName: "@GPSLongitude"].Value = favourite.GPSLongitude;
                    insertCommand.Parameters[parameterName: "@GPSLongitudeRef"].Value = favourite.GPSLongitudeRef;
                    insertCommand.Parameters[parameterName: "@GPSAltitude"].Value = favourite.GPSAltitude;
                    insertCommand.Parameters[parameterName: "@GPSAltitudeRef"].Value = favourite.GPSAltitudeRef;
                    insertCommand.Parameters[parameterName: "@Coordinates"].Value = favourite.Coordinates;
                    insertCommand.Parameters[parameterName: "@City"].Value = favourite.City;
                    insertCommand.Parameters[parameterName: "@CountryCode"].Value = favourite.CountryCode;
                    insertCommand.Parameters[parameterName: "@Country"].Value = favourite.Country;
                    insertCommand.Parameters[parameterName: "@State"].Value = favourite.State;
                    insertCommand.Parameters[parameterName: "@Sublocation"].Value = favourite.Sublocation;

                    insertCommand.ExecuteNonQuery();
                }
            }

            transaction.Commit();
            FrmMainApp.Log.Info(message: "Favourites successfully saved.");
        }
        catch (Exception ex)
        {
            FrmMainApp.Log.Error(message: "Error saving favourites", argument: ex);
            transaction.Rollback();
            throw;
        }
    }


    /// <summary>
    ///     Creates a table for the user's "Favourites".
    ///     This is a bit of a f...up because originally this was favouriteName and then I started using favouriteName, which
    ///     lends itself better to what it is but the columnName now has been released so...
    /// </summary>
    internal static void DataCreateSQLiteFavourites()
    {
        FrmMainApp.Log.Info(message: "Starting");

        using SQLiteConnection sqliteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
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
                                        Sublocation NTEXT
                                        )
                                ;
                                "
            ;

        SQLiteCommand sqlToRun = new(commandText: sqlCommandStr, connection: sqliteDB);

        sqlToRun.ExecuteNonQuery();
    }
}