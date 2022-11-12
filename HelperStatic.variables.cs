using System.Collections.Generic;
using System.IO;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
    #region variables

    internal static string s_ArcGIS_APIKey;
    internal static string s_GeoNames_UserName;
    internal static string s_GeoNames_Pwd;
    internal static string s_settingsDataBasePath = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: "database.sqlite");
    internal static bool s_changeFolderIsOkay;
    internal static bool s_APIOkay = true;
    private static string s_ErrorMsg = "";
    private static string s_OutputMsg = "";
    private static readonly string s_doubleQuote = "\"";
    internal static string HTMLAddMarker;
    internal static HashSet<(string strLat, string strLng)> hs_MapMarkers = new();
    internal static double? minLat;
    internal static double? minLng;
    internal static double? maxLat;
    internal static double? maxLng;
    internal static bool s_NowSelectingAllItems = false;
    internal static bool s_ResetMapToZero;
    internal static long folderEnterLastEpoch = 0;

    #endregion
}