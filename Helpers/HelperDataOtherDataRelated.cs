using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace GeoTagNinja.Helpers;

internal static class HelperDataOtherDataRelated
{
    /// <summary>
    ///     This creates the DataTables for the main Form - been moved out here because it's otherwise tedious to keep track
    ///     of.
    /// </summary>
    public static Task GenericCreateDataTables()
    {
        FrmMainApp.Log.Info(message: "Starting");

        // DTLanguageMapping
        FrmMainApp.DTLanguageMapping = new DataTable();
        FrmMainApp.DTLanguageMapping.Clear();
        //  "ab", "аҧсуа бызшәа [Abkhaz]" 
        _ = FrmMainApp.DTLanguageMapping.Columns.Add(columnName: "languageCode");
        _ = FrmMainApp.DTLanguageMapping.Columns.Add(columnName: "languageNative");
        _ = FrmMainApp.DTLanguageMapping.Columns.Add(columnName: "languageEnglish");

        FillDTLanguageMapping();

        // DTToponomySessionData;
        FrmMainApp.DTToponomySessionData = new DataTable();
        FrmMainApp.DTToponomySessionData.Clear();
        _ = FrmMainApp.DTToponomySessionData.Columns.Add(columnName: "lat");
        _ = FrmMainApp.DTToponomySessionData.Columns.Add(columnName: "lng");
        _ = FrmMainApp.DTToponomySessionData.Columns.Add(columnName: "AdminName1");
        _ = FrmMainApp.DTToponomySessionData.Columns.Add(columnName: "AdminName2");
        _ = FrmMainApp.DTToponomySessionData.Columns.Add(columnName: "AdminName3");
        _ = FrmMainApp.DTToponomySessionData.Columns.Add(columnName: "AdminName4");
        _ = FrmMainApp.DTToponomySessionData.Columns.Add(columnName: "ToponymName");
        _ = FrmMainApp.DTToponomySessionData.Columns.Add(columnName: "CountryCode");
        _ = FrmMainApp.DTToponomySessionData.Columns.Add(columnName: "GPSAltitude");
        _ = FrmMainApp.DTToponomySessionData.Columns.Add(columnName: "timezoneId");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Gets the "FirstOrDefault" from a List of KVP
    /// </summary>
    /// <param name="kvpListIn">List (KVP) to check</param>
    /// <param name="keyEqualsWhat">Key filter</param>
    /// <returns>String of Value</returns>
    internal static string DataGetFirstOrDefaultFromKVPList(Dictionary<string, string> kvpListIn,
        string keyEqualsWhat)
    {
        return kvpListIn.FirstOrDefault(predicate: kvp => kvp.Key == keyEqualsWhat)
                        .Value;
    }

    internal static string DataGetFirstOrDefaultFromDataTable(DataTable dataTableIn, string dataColumnFilter,
        string dataColumnReturn,
        string keyEqualsWhat)
    {
        EnumerableRowCollection<DataRow> res = from row in dataTableIn.AsEnumerable()
                                               where row.Field<string>(columnName: dataColumnFilter) == keyEqualsWhat
                                               select row;

        return res.ToList()[index: 0][columnName: dataColumnReturn].ToString();
    }

    /// <summary>
    ///     Fills up the language mapping datatable
    /// </summary>
    private static void FillDTLanguageMapping()
    {
        FrmMainApp.DTLanguageMapping.Rows.Clear();

        //{ "lo", "ພາສາ [Lao]" },
        foreach (KeyValuePair<string, string> iso6391Language in HelperGenericAncillaryListsArrays
                    .GetISO_639_1_Languages())
        {
            DataRow dr = FrmMainApp.DTLanguageMapping.NewRow();
            dr[columnName: "languageCode"] = iso6391Language.Key;
            dr[columnName: "languageNative"] = iso6391Language.Value.Split('[')[0].Trim();
            dr[columnName: "languageEnglish"] =
                iso6391Language.Value.Split('[')[1].Trim().Replace(oldValue: "]", newValue: "");
            FrmMainApp.DTLanguageMapping.Rows.Add(row: dr);
        }
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
            for (int i = FrmMainApp.DTToponomySessionData.Rows.Count - 1; i >= 0; i--)
            {
                DataRow thisDr = FrmMainApp.DTToponomySessionData.Rows[index: i];
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

            FrmMainApp.DTToponomySessionData.AcceptChanges();

            // add new
            DataRow newDr = FrmMainApp.DTToponomySessionData.NewRow();
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

            FrmMainApp.DTToponomySessionData.Rows.Add(row: newDr);
            FrmMainApp.DTToponomySessionData.AcceptChanges();
        }
    }
}