using System.Collections.Generic;
using System.Data;
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
    private static string _sErrorMsg = "";
    private static string _sOutputMsg = "";
    private static readonly string SDoubleQuote = "\"";
    internal static string HtmlAddMarker;
    internal static HashSet<(string strLat, string strLng)> HsMapMarkers = new();
    internal static double? MinLat;
    internal static double? MinLng;
    internal static double? MaxLat;
    internal static double? MaxLng;
    internal static bool SNowSelectingAllItems = false;
    internal static bool SResetMapToZero;
    internal static long FolderEnterLastEpoch = 0;

    #endregion
}