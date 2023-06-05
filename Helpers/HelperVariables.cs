﻿using System.Collections.Generic;
using System.Data;

namespace GeoTagNinja.Helpers;

internal static partial class HelperVariables
{
    internal const int exifOrientationID = 0x112; //274
    internal static string SArcGisApiKey;
    internal static string SGeoNamesUserName;
    internal static string SGeoNamesPwd;
    internal static bool SChangeFolderIsOkay;
    internal static bool SApiOkay = true;
    internal static string _sErrorMsg = "";
    internal static string _sOutputMsg = "";
    internal static readonly string SDoubleQuote = "\"";
    internal static string HtmlAddMarker;
    internal static HashSet<(string strLat, string strLng)> HsMapMarkers = new();
    internal static double? MinLat;
    internal static double? MinLng;
    internal static double? MaxLat;
    internal static double? MaxLng;
    internal static double? LastLat;
    internal static double? LastLng;
    internal static bool SNowSelectingAllItems = false;
    internal static bool SResetMapToZero;
    internal static bool SOnlyShowFCodePPL = false;
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
    internal static string SSettingsDataBasePath;
    internal static DataTable DtObjectNames;
    internal static DataTable DtObjectattributesIn;
    internal static DataTable DtObjectattributesOut;
    internal static DataTable DtIsoCountryCodeMapping;
    internal static List<string> LstCityNameIsAdminName1 = new();
    internal static List<string> LstCityNameIsAdminName2 = new();
    internal static List<string> LstCityNameIsAdminName3 = new();
    internal static List<string> LstCityNameIsAdminName4 = new();
    internal static List<string> LstCityNameIsUndefined = new();
}