using NLog;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using WinFormsDarkThemerNinja;

namespace GeoTagNinja.Helpers.Data;

internal static class Favourites
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    #region GeoSetter Favourites/Import

    /// <summary>
    ///     Reads the gs favs (fake) xml file, converts to GTN-favs and saves.
    /// </summary>
    /// <param name="fileNameToParse"></param>
    internal static void ParseGeoSetterFavouritesXmlToFavourite(string fileNameToParse)
    {
        bool updateFavouritesInSQLiteAtTheEnd = true;
        int itemsImported = 0;

        FrmMainApp frmMainAppInstance = FrmMainApp.Instance;
        HashSet<GeoSetterFavourite> geoSetterFavourites = [];
        try
        {
            XDocument doc = XDocument.Parse(text: File.ReadAllText(path: fileNameToParse));
            foreach (XElement item in doc.Descendants(name: "geosetterpositions").Elements())
            {
                string utf8_String = null;
                try
                {
                    byte[] utf8_Bytes = Encoding.Default.GetBytes(s: (string)item.Attribute(name: "name"));
                    utf8_String = Encoding.UTF8.GetString(bytes: utf8_Bytes);
                }
                catch
                {
                    // ignore because we'll ignore this element in a line or two
                }

                if (!string.IsNullOrWhiteSpace(value: utf8_String))
                {
                    GeoSetterFavourite geoSetterFavourite = new()
                    {
                        name = utf8_String,
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
                        autoassign = int.TryParse(s: (string)item.Attribute(name: "autoassign"),
                            style: NumberStyles.Any,
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
                    _ = geoSetterFavourites.Add(item: geoSetterFavourite);
                }
            }
        }
        catch (Exception ex)
        {
            Themer.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_FrmMainApp_ErrorImportGeoSetterFavourites",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox
                    ) +
                    Environment.NewLine + $"{ex.Message}",
                icon: MessageBoxIcon.Error,
                buttons: MessageBoxButtons.OK);

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
                        Sublocation = geoSetterFavourite.subloc,
                        Radius = Math.Max(val1: 0, val2: geoSetterFavourite.radius),
                        AutoAssign = geoSetterFavourite.autoassign != 0,
                        Snap = geoSetterFavourite.snap != 0,
                        TimeZoneId = geoSetterFavourite.tz ?? ""
                    };

                    Favourite existingFavouriteWithThisName =
                        frmMainAppInstance.GetFavouriteByName(favouriteName: favourite.FavouriteName);
                    if (existingFavouriteWithThisName != null)
                    {
                        frmMainAppInstance.RemoveFavouriteByName(
                            favouriteName: existingFavouriteWithThisName.FavouriteName);
                    }

                    _ = FrmMainApp.Favourites.Add(item: favourite);
                    itemsImported++;
                }
                catch (Exception ex)
                {
                    Themer.ShowMessageBox(
                        message: HelperControlAndMessageBoxHandling.ReturnControlText(
                            controlName: "mbx_FrmMainApp_ErrorImportGeoSetterFavourites",
                            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox
                            ) +
                            Environment.NewLine + $"{ex.Message}",
                        icon: MessageBoxIcon.Error,
                        buttons: MessageBoxButtons.OK);

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
            Themer.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_FrmMainApp_InfoImportGeoSetterFavouritesDone",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox
                    ) +
                    Environment.NewLine + $"{itemsImported}",
                icon: MessageBoxIcon.Information,
                buttons: MessageBoxButtons.OK);

            frmMainAppInstance.ClearReloadFavouritesDropDownValues();
        }
    }

    #endregion

    /// <summary>
    ///     Reads the Favourites table from SQLite.
    /// </summary>
    internal static void DataReadSQLiteFavourites()
    {
        using SQLiteConnection SQLiteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        string commandText = @"
                                SELECT *
                                FROM Favourites
                                WHERE 1=1
                                ORDER BY 1;

								"
            ;

        SQLiteCommand SQLiteCommand = new(commandText: commandText, connection: SQLiteDB);

        SQLiteDataReader reader = SQLiteCommand.ExecuteReader();
        DataTable dataTable = new();
        dataTable.Load(reader: reader);
        FrmMainApp.Favourites.Clear();

        foreach (DataRow row in dataTable.Rows)
        {
            Favourite favourite = new()
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
                Sublocation = row[columnName: "Sublocation"]?.ToString(),
                Radius = ReadInt(row: row, columnName: "Radius"),
                AutoAssign = ReadInt(row: row, columnName: "AutoAssign") != 0,
                Snap = ReadInt(row: row, columnName: "Snap") != 0,
                TimeZoneId = ReadText(row: row, columnName: "TimeZoneId"),
                Keywords = FavouriteKeywords.Split(stored: ReadText(row: row, columnName: "Keywords"))
            };

            _ = FrmMainApp.Favourites.Add(item: favourite);
        }
    }

    /// <summary>
    ///     Makes a favourite out of a photo: its position, altitude and location names. The auto-assign settings start
    ///     off (no radius, nothing assigned), for the user to choose.
    /// </summary>
    /// <param name="directoryElement">The photo.</param>
    /// <param name="favouriteName">The name to give the favourite.</param>
    internal static Favourite FromDirectoryElement(DirectoryElement directoryElement,
                                                   string favouriteName)
    {
        return new Favourite
        {
            FavouriteName = favouriteName,
            GPSLatitude = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.GPSLatitude),
            GPSLatitudeRef = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.GPSLatitudeRef),
            GPSLongitude = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.GPSLongitude),
            GPSLongitudeRef = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.GPSLongitudeRef),
            GPSAltitude = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.GPSAltitude),
            GPSAltitudeRef = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.GPSAltitudeRef),
            Coordinates = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.Coordinates),
            City = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.City),
            CountryCode = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.CountryCode),
            Country = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.Country),
            State = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.State),
            Sublocation = AttributeText(de: directoryElement, attribute: SourcesAndAttributes.ElementAttribute.Sublocation)
        };
    }

    /// <summary>
    ///     An attribute as text for storing, with the app's "blank" marker turned into an actual blank.
    /// </summary>
    private static string AttributeText(DirectoryElement de,
                                        SourcesAndAttributes.ElementAttribute attribute)
    {
        string text = de.GetAttributeValueAsString(attribute: attribute, notFoundValue: "",
            context: ValueFormatContext.RoundTrip);

        return text == NullEquivalents.StringGeneric
            ? ""
            : text;
    }

    /// <summary>
    ///     Reads a text column that an older database may not have yet (or may hold NULL in).
    /// </summary>
    private static string ReadText(DataRow row,
                                   string columnName)
    {
        return row.Table.Columns.Contains(name: columnName) && row[columnName: columnName] is not DBNull
            ? row[columnName: columnName].ToString()
            : "";
    }

    /// <summary>
    ///     Reads an integer column that an older database may not have yet (or may hold NULL in).
    /// </summary>
    private static int ReadInt(DataRow row,
                               string columnName)
    {
        return int.TryParse(s: ReadText(row: row, columnName: columnName), style: NumberStyles.Integer,
            provider: CultureInfo.InvariantCulture, result: out int value)
            ? value
            : 0;
    }

    /// <summary>
    ///     Updates the name (aka renames) a particular Favourite
    /// </summary>
    /// <param name="oldName">Existing name</param>
    /// <param name="newName">New name to use</param>
    internal static void DataRenameSQLiteFavourite(string oldName,
                                                   string newName)
    {
        Log.Info(message: "Starting");

        using SQLiteConnection SQLiteDB = new(connectionString:
            $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        string commandText = @"
                                UPDATE Favourites
                                SET favouriteName = @newName
                                WHERE favouriteName = @oldName
                                ;"
            ;

        SQLiteCommand SQLiteCommand = new(commandText: commandText, connection: SQLiteDB);
        _ = SQLiteCommand.Parameters.AddWithValue(parameterName: "@oldName", value: oldName);
        _ = SQLiteCommand.Parameters.AddWithValue(parameterName: "@newName", value: newName);

        _ = SQLiteCommand.ExecuteNonQuery();
    }

    /// <summary>
    ///     Clears the whole Favourites table and rewrites it.
    ///     Obvs chatGPT because I'm lazy.
    /// </summary>
    internal static void DataWriteSQLiteClearAndUpdateFavourites()
    {
        Log.Info(message: "Starting");

        using SQLiteConnection SQLiteDB =
            new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        SQLiteDB.Open();

        using SQLiteTransaction SQLiteTransaction = SQLiteDB.BeginTransaction();

        try
        {
            // Clear the table first
            using (SQLiteCommand deleteCommand = new(commandText: "DELETE FROM Favourites;", connection: SQLiteDB,
                       transaction: SQLiteTransaction))
            {
                _ = deleteCommand.ExecuteNonQuery();
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
                Sublocation,
                Radius,
                AutoAssign,
                Snap,
                TimeZoneId,
                Keywords
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
                @Sublocation,
                @Radius,
                @AutoAssign,
                @Snap,
                @TimeZoneId,
                @Keywords
            );", connection: SQLiteDB, transaction: SQLiteTransaction))
            {
                // Add parameters once — values will be updated inside the loop
                _ = insertCommand.Parameters.Add(parameterName: "@favouriteName", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@GPSLatitude", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@GPSLatitudeRef", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@GPSLongitude", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@GPSLongitudeRef", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@GPSAltitude", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@GPSAltitudeRef", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@Coordinates", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@City", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@CountryCode", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@Country", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@State", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@Sublocation", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@Radius", parameterType: DbType.Int32);
                _ = insertCommand.Parameters.Add(parameterName: "@AutoAssign", parameterType: DbType.Int32);
                _ = insertCommand.Parameters.Add(parameterName: "@Snap", parameterType: DbType.Int32);
                _ = insertCommand.Parameters.Add(parameterName: "@TimeZoneId", parameterType: DbType.String);
                _ = insertCommand.Parameters.Add(parameterName: "@Keywords", parameterType: DbType.String);

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
                    insertCommand.Parameters[parameterName: "@Radius"].Value = Math.Max(val1: 0, val2: favourite.Radius);
                    insertCommand.Parameters[parameterName: "@AutoAssign"].Value = favourite.AutoAssign ? 1 : 0;
                    insertCommand.Parameters[parameterName: "@Snap"].Value = favourite.Snap ? 1 : 0;
                    insertCommand.Parameters[parameterName: "@TimeZoneId"].Value = favourite.TimeZoneId ?? "";
                    insertCommand.Parameters[parameterName: "@Keywords"].Value =
                        FavouriteKeywords.Join(keywords: favourite.Keywords);

                    _ = insertCommand.ExecuteNonQuery();
                }
            }

            SQLiteTransaction.Commit();
            Log.Info(message: "Favourites successfully saved.");
        }
        catch (Exception ex)
        {
            Log.Error(message: "Error saving favourites", argument: ex);
            SQLiteTransaction.Rollback();
            throw;
        }
    }

    // The "Favourites" table used to be created here as well, from a second copy of the DDL in
    // DatabaseAndStartup.DataCreateSQLiteDB. Both copies now live in DatabaseSchema, which creates the table.
    // (The column is favouriteName rather than locationName because the original name was released before it got
    // renamed; DatabaseAndStartup.DataCreateSQLiteDB migrates the old databases.)
}