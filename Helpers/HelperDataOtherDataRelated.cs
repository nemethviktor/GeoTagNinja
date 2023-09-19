using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace GeoTagNinja.Helpers;

internal static class HelperDataOtherDataRelated
{
    /// <summary>
    ///     This creates the DataTables for the main Form - been moved out here because it's otherwise tedious to keep track
    ///     of.
    /// </summary>
    public static void GenericCreateDataTables()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        // DtLanguageLabels
        FrmMainApp.DtLanguageLabels = new DataTable();
        FrmMainApp.DtLanguageLabels.Clear();
        FrmMainApp.DtLanguageLabels.Columns.Add(columnName: "languageName");
        FrmMainApp.DtLanguageLabels.Columns.Add(columnName: "objectType");
        FrmMainApp.DtLanguageLabels.Columns.Add(columnName: "objectName");
        FrmMainApp.DtLanguageLabels.Columns.Add(columnName: "objectText");

        // DtToponomySessionData;
        FrmMainApp.DtToponomySessionData = new DataTable();
        FrmMainApp.DtToponomySessionData.Clear();
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "lat");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "lng");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "AdminName1");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "AdminName2");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "AdminName3");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "AdminName4");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "ToponymName");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "CountryCode");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "GPSAltitude");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "timezoneId");
    }

    /// <summary>
    ///     Gets the "FirstOrDefault" from a List of KVP
    /// </summary>
    /// <param name="lstIn">List (KVP) to check</param>
    /// <param name="keyEqualsWhat">Key filter</param>
    /// <returns>String of Value</returns>
    internal static string DataGetFirstOrDefaultFromKVPList(Dictionary<string, string> lstIn,
                                                            string keyEqualsWhat)
    {
        return lstIn.FirstOrDefault(predicate: kvp => kvp.Key == keyEqualsWhat)
            .Value;
    }


    /// <summary>
    ///     Updates the sessions storage for the Toponomy DT
    /// </summary>
    /// <param name="lat">string value of lat</param>
    /// <param name="lng">string value of lng</param>
    /// <param name="adminName1">Value to write</param>
    /// <param name="adminName2">Value to write</param>
    /// <param name="adminName3">Value to write</param>
    /// <param name="adminName4">Value to write</param>
    /// <param name="toponymName">Value to write</param>
    /// <param name="countryCode">Value to write</param>
    /// <param name="altitude">Value to write</param>
    /// <param name="timezoneId">Value to write</param>
    internal static void UpdateAddToDataTableTopopnomy(
        string lat,
        string lng,
        string adminName1,
        string adminName2,
        string adminName3,
        string adminName4,
        string toponymName,
        string countryCode,
        string altitude,
        string timezoneId
    )
    {
        lock (HelperGenericFileLocking.TableLock)
        {
            // delete any existing rows with the current combination
            for (int i = FrmMainApp.DtToponomySessionData.Rows.Count - 1; i >= 0; i--)
            {
                DataRow thisDr = FrmMainApp.DtToponomySessionData.Rows[index: i];
                if (
                    thisDr[columnName: "lat"]
                        .ToString() ==
                    lat &&
                    thisDr[columnName: "lng"]
                        .ToString() ==
                    lng
                )
                {
                    thisDr.Delete();
                }
            }

            FrmMainApp.DtToponomySessionData.AcceptChanges();

            // add new
            DataRow newDr = FrmMainApp.DtToponomySessionData.NewRow();
            newDr[columnName: "lat"] = lat;
            newDr[columnName: "lng"] = lng;
            newDr[columnName: "AdminName1"] = adminName1;
            newDr[columnName: "AdminName2"] = adminName2;
            newDr[columnName: "AdminName3"] = adminName3;
            newDr[columnName: "AdminName4"] = adminName4;
            newDr[columnName: "ToponymName"] = toponymName;
            newDr[columnName: "CountryCode"] = countryCode;
            newDr[columnName: "GPSAltitude"] = altitude;
            newDr[columnName: "timezoneId"] = timezoneId;

            FrmMainApp.DtToponomySessionData.Rows.Add(row: newDr);
            FrmMainApp.DtToponomySessionData.AcceptChanges();
        }
    }
}