using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using static System.Environment;

namespace GeoTagNinja.Helpers;

internal static class HelperVariables
{
    internal const int exifOrientationID = 0x112; //274
    internal const double MetreToFeet = 3.28084;
    internal const string DoubleQuoteStr = "\"";

    internal const string ControlItemNameNotGeneric = "Not Generic";
    internal const string ResourceNameForGenericControlItems = "Generic_Strings";

    // user-defined settings updateable via Reflection
    internal static string UserSettingArcGisApiKey;
    internal static string UserSettingGeoNamesUserName;
    internal static string UserSettingGeoNamesPwd;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    internal static bool UserSettingResetMapToZeroOnMissingValue;
    internal static bool UserSettingUseDarkMode;
    internal static bool UserSettingUpdatePreReleaseGTN;
    internal static bool UserSettingOnlyShowFCodePPL = false;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    internal static string UserSettingMapColourMode; // DarkInverse or DarkPale (could be Normal too but not relevant)
    internal static bool UserSettingUseImperial = false;

    internal static bool OperationChangeFolderIsOkay;
    internal static bool OperationAPIReturnedOKResponse = true;
    internal static bool OperationNowSelectingAllItems = false;

    //internal static string _sErrorMsg = "";
    internal static string _sOutputAndErrorMsg = "";
    internal static string HTMLAddMarker;
    internal static string HTMLCreatePoints;
    internal static HashSet<(string strLat, string strLng)> HsMapMarkers = new();
    internal static List<(string strLat, string strLng)> LstTrackPath = new();
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
    internal static string ToponymaxRows = "1";
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
    internal static List<string> LstCityNameIsAdminName1 = new();
    internal static List<string> LstCityNameIsAdminName2 = new();
    internal static List<string> LstCityNameIsAdminName3 = new();
    internal static List<string> LstCityNameIsAdminName4 = new();
    internal static List<string> LstCityNameIsUndefined = new();

    internal static string ExifToolExePathRoamingTemp = string.Empty;

    internal static readonly string ExifToolExePathRoamingPerm =
        GetExifToolExePathRoamingPerm();

    private static readonly string ExifToolExePathSupplied = Path.Combine(
        path1: ResourcesFolderPath,
        path2: "exiftool.exe");

    internal static readonly string ExifToolExePathToUse = GetExifToolExePathToUse();

    internal static string UOMAbbreviated = "";
    internal static Dictionary<FileInfo, string> FileChecksumDictionary = new();


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
            Directory.CreateDirectory(path: userDataFolderPath);
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
                sourceConfigFileFi.CopyTo(destFileName: destConfigFileFi.FullName,
                    overwrite: true);
            }
        }
        else
        {
            sourceConfigFileFi.CopyTo(destFileName: destConfigFileFi.FullName,
                overwrite: true);
        }
    }
}