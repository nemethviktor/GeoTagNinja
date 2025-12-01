using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using static System.Environment;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace GeoTagNinja.Helpers;

internal static class HelperVariables
{
    internal const int exifOrientationID = 0x112; //274
    internal const double MetreToFeet = 3.28084;
    internal const string DoubleQuoteStr = "\"";

    internal const string ControlItemNameNotGeneric = "Not Generic";
    internal const string ResourceNameForGenericControlItems = "Generic_Strings";

    #region User Defined (via Reflection)

    #region strings

    internal static string UserSettingArcGisApiKey;
    internal static string UserSettingGeoNamesUserName;
    internal static string UserSettingGeoNamesPwd;
    internal static string UserSettingMapColourMode; // DarkInverse or DarkPale (could be Normal too but not relevant)
    internal static string UserSettingImportGPXImportSource;
    internal static string UserSettingImportGPXTimeZoneToUse;

    #endregion

    #region bools

    /// <summary>
    /// Whether to use Zero values where coords are missing
    /// </summary>
    internal static bool UserSettingResetMapToZeroOnMissingValue;

    /// <summary>
    /// Whether to use dark mode
    /// </summary>
    internal static bool UserSettingUseDarkMode;

    /// <summary>
    /// Whether to check for pre-release GTN versions
    /// </summary>
    internal static bool UserSettingUpdatePreReleaseGTN;

    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    internal static bool UserSettingOnlyShowFCodePPL = false;
    internal static bool UserSettingShowThumbnails;

    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    internal static bool UserSettingUseImperial = false;

    internal static bool UserSettingImportGPXUseParticularTimeZone;
    internal static bool UserSettingImportGPXUseDST;

    internal static bool UserSettingUseLastUsedFolderAsStartup;

    #endregion

    #region integers

    internal static int UserSettingImportGPXMaxInterpolation;
    internal static int UserSettingImportGPXMaxExtrapolation;

    #endregion

    #endregion

    internal static bool OperationChangeFolderIsOkay;
    internal static bool OperationAPIReturnedOKResponse = true;

    internal static bool OperationNowSelectingAllItems = false;

    //internal static string _sErrorMsg = "";
    internal static string _sOutputAndErrorMsg = "";
    internal static string HTMLAddMarker;
    internal static string HTMLCreatePoints;
    internal static string HTMLDefaultLayer = "lyr_streets_osm";
    internal static HashSet<(string strLat, string strLng)> HsMapMarkers = [];
    internal static List<(string strLat, string strLng)> LstTrackPath = [];
    internal static double? MinLat;
    internal static double? MinLng;
    internal static double? MaxLat;
    internal static double? MaxLng;
    internal static double? LastLat;
    internal static double? LastLng;
    internal static decimal CurrentExifToolVersionLocal;
    internal static decimal CurrentExifToolVersionCloud;
    internal static bool ToponomyReplace = false;
    internal static string ToponomyReplaceWithWhat = null;
    internal static string ToponyMaxRowsChoiceOfferCount = "1";
    internal static string ToponomyRadiusValue = "10";

    internal static string?
        CurrentAltitude; // this is needed bcs it can happen that a file has altitude, the api returns -32k and then we'd end up with something worse than what it was originally.

    internal static readonly string DefaultEnglishString = "en";
    internal static string APILanguageToUse;
    internal static DataTable DtCustomRules = new();
    internal static DataTable DtCustomCityLogic = new();

    internal static DataTable DtHelperDataApplicationSettingsPreQueue = new();
    internal static DataTable DtHelperDataApplicationSettings = new();
    internal static DataTable DtHelperDataApplicationLayout = new();

    internal static readonly string ResourcesFolderPath = GetResourcesFolderString();
    internal static readonly string UserDataFolderPath = GetRoamingFolderString();

    internal static readonly string SettingsDatabaseFilePath =
        GetSettingsDatabaseFilePath();

    internal static DataTable DtIsoCountryCodeMapping;
    internal static List<string> LstCityNameIsAdminName1 = [];
    internal static List<string> LstCityNameIsAdminName2 = [];
    internal static List<string> LstCityNameIsAdminName3 = [];
    internal static List<string> LstCityNameIsAdminName4 = [];
    internal static List<string> LstCityNameIsUndefined = [];

    internal static string ExifToolExePathRoamingTemp = string.Empty;

    internal static readonly string ExifToolExePathRoamingPerm =
        GetExifToolExePathRoamingPerm();

    private static readonly string ExifToolExePathSupplied = Path.Combine(
        path1: ResourcesFolderPath,
        path2: "exiftool.exe");

    internal static readonly string ExifToolExePathToUse = GetExifToolExePathToUse();

    internal static string UOMAbbreviated = "";
    internal static Dictionary<string, string> FileChecksumDictionary = [];

    internal static HashSet<string> errorsAlreadyShownHashSet = [];

    // this is a bug where, upon closing the app the "do you want to write the queue?" question comes up twice but i can't seem to track it properly so this is to avoid that happening.
    internal static bool AppIsClosingAndWriteFileQuestionHasBeenAsked = false;

    /// <summary>
    ///     Pulls (and creates if necessary) the Roaming/Users subfolder for the app.
    /// </summary>
    /// <returns>Path name of the Roaming/Users subfolder</returns>
    private static string GetRoamingFolderString()
    {
        string userDataFolderPath = Path.Combine(
            path1: GetFolderPath(folder: SpecialFolder.ApplicationData),
            path2: "GeoTagNinja");

        if (!Directory.Exists(path: userDataFolderPath))
        {
            _ = Directory.CreateDirectory(path: userDataFolderPath);
        }

        return userDataFolderPath;
    }

    /// <summary>
    ///     Gets the app's resources folder location.
    /// </summary>
    /// <returns>The app's resources folder location.</returns>
    private static string GetResourcesFolderString()
    {
        return
            Path.Combine(path1: AppDomain.CurrentDomain.BaseDirectory,
                path2: "Resources");
    }

    /// <summary>
    ///     Gets the sqlite file location containing the database info.
    /// </summary>
    /// <returns>The sqlite file location containing the database info</returns>
    private static string GetSettingsDatabaseFilePath()
    {
        return Path.Combine(
            path1: UserDataFolderPath, path2: "database.sqlite");
    }

    /// <summary>
    ///     Path name of the "normal" exiftool - file doesn't have to exist as such.
    /// </summary>
    /// <returns>Path name of the normal exiftool</returns>
    private static string GetExifToolExePathRoamingPerm()
    {
        return Path.Combine(
            path1: UserDataFolderPath,
            path2:
            "exiftool.exe");
    }

    /// <summary>
    ///     Establish what exiftool to use.
    /// </summary>
    /// <returns></returns>
    private static string GetExifToolExePathToUse()
    {
        if (File.Exists(
                path: ExifToolExePathRoamingPerm))
        {
            EnsureExifToolConfigFileHasBeenCopied();
            return ExifToolExePathRoamingPerm;
        }

        return ExifToolExePathSupplied;
    }

    /// <summary>
    ///     Copies the supplied exiftool config file to Roaming if 1) it doesn't exist or 2) exists but there's been an update
    ///     to the supplied one.
    /// </summary>
    private static void EnsureExifToolConfigFileHasBeenCopied()
    {
        string configFileName = ".ExifTool_config";

        FileInfo sourceConfigFileFi =
            new(fileName: Path.Combine(path1: ResourcesFolderPath,
                path2: configFileName));

        FileInfo destConfigFileFi =
            new(fileName: Path.Combine(path1: UserDataFolderPath,
                path2: sourceConfigFileFi.Name));
        if (destConfigFileFi.Exists)
        {
            if (sourceConfigFileFi.LastWriteTime > destConfigFileFi.LastWriteTime)
            {
                _ = sourceConfigFileFi.CopyTo(destFileName: destConfigFileFi.FullName,
                    overwrite: true);
            }
        }
        else
        {
            _ = sourceConfigFileFi.CopyTo(destFileName: destConfigFileFi.FullName,
                overwrite: true);
        }
    }
}