using System.Collections.Generic;
using System.IO;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
    #region variables

    internal static string SArcGisApiKey;
    internal static string SGeoNamesUserName;
    internal static string SGeoNamesPwd;
    private static readonly string SSettingsDataBasePath = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: "database.sqlite");
    internal static bool SChangeFolderIsOkay;
    internal static bool SApiOkay = true;
    internal static string _sErrorMsg = "";
    private static string _sOutputMsg = "";
    private static readonly string SDoubleQuote = "\"";
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
    private static decimal _currentExifToolVersionLocal;
    internal static bool ToponomyReplace = false;
    internal static string ToponomyReplaceWithWhat = null;
    internal static string ToponomyMaxRows = "1";
    internal static string ToponomyRadiusValue = "10";

    #endregion
}