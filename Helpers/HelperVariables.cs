using System.Collections.Generic;
using System.Data;

namespace GeoTagNinja.Helpers;

internal static class HelperVariables
{
    internal const int exifOrientationID = 0x112; //274
    internal const double MetreToFeet = 3.28084;
    internal const string DoubleQuoteStr = "\"";

    // user-defined settings updateable via Reflection
    internal static string UserSettingArcGisApiKey;
    internal static string UserSettingGeoNamesUserName;
    internal static string UserSettingGeoNamesPwd;
    internal static bool UserSettingResetMapToZeroOnMissingValue;
    internal static bool UserSettingUseDarkMode;
    internal static bool UserSettingUpdatePreReleaseGTN;
    internal static bool UserSettingOnlyShowFCodePPL = false;
    internal static string UserSettingMapColourMode; // DarkInverse or DarkPale (could be Normal too but not relevant)
    internal static bool UserSettingUseImperial = false;

    internal static bool OperationChangeFolderIsOkay;
    internal static bool OperationAPIReturnedOKResponse = true;
    internal static bool OperationNowSelectingAllItems = false;

    internal static string _sErrorMsg = "";
    internal static string _sOutputMsg = "";
    internal static string HTMLAddMarker;
    internal static string HTMLCreatePoints;
    internal static HashSet<(string strLat, string strLng)> HsMapMarkers = new();
    internal static double? MinLat;
    internal static double? MinLng;
    internal static double? MaxLat;
    internal static double? MaxLng;
    internal static double? LastLat;
    internal static double? LastLng;
    internal static decimal _currentExifToolVersionLocal;
    internal static bool ToponomyReplace = false;
    internal static string ToponomyReplaceWithWhat = null;
    internal static string ToponomyMaxRows = "1";
    internal static string ToponomyRadiusValue = "10";
    internal static string? CurrentAltitude; // this is needed bcs it can happen that a file has altitude, the api returns -32k and then we'd end up with something worse than what it was originally.
    internal static readonly string DefaultEnglishString = "English [English]";
    internal static string APILanguageToUse;
    internal static DataTable DtCustomRules = new();
    internal static DataTable DtCustomCityLogic = new();
    internal static string ResourcesFolderPath;
    internal static string UserDataFolderPath;
    internal static string SettingsDatabaseFilePath;
    internal static DataTable DtIsoCountryCodeMapping;
    internal static List<string> LstCityNameIsAdminName1 = new();
    internal static List<string> LstCityNameIsAdminName2 = new();
    internal static List<string> LstCityNameIsAdminName3 = new();
    internal static List<string> LstCityNameIsAdminName4 = new();
    internal static List<string> LstCityNameIsUndefined = new();

    internal static string UOMAbbreviated = "";
}