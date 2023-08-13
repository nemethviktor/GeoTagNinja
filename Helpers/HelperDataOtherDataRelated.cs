using System;
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


    /// <returns>ALPHA-2, ALPHA-3 and plain English country names</returns>
    /// <summary>
    ///     Does a filter on a DataTable - just faster.
    ///     via https://stackoverflow.com/a/47692754/3968494
    /// </summary>
    /// <param name="dt">DataTable to query</param>
    /// <param name="filePathColumnName">The "column" part of WHERE</param>
    /// <param name="filePathValue">The "value" part of WHERE</param>
    /// <returns>List of KVP String/String</returns>
    internal static Dictionary<string, string> DataReadFilterDataTable(DataTable dt,
                                                                       string filePathColumnName,
                                                                       string filePathValue)
    {
        EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in dt.AsEnumerable()
                                                           where dataRow.Field<string>(columnName: filePathColumnName) == filePathValue
                                                           select dataRow;
        Dictionary<string, string> lstReturn = new();

        Parallel.ForEach(source: drDataTableData, body: dataRow =>
            {
                string settingId = dataRow[columnName: "settingId"]
                    .ToString();
                string settingValue = dataRow[columnName: "settingValue"]
                    .ToString();
                lstReturn.Add(key: settingId, value: settingValue);
            })
            ;
        return lstReturn;
    }

    /// <summary>
    ///     A centralised way to interact with datatables containing exif data. Checks if the table already contains an element
    ///     for the given combination and if so deletes it, then writes the new data.
    /// </summary>
    /// <param name="dt">Name of the datatable. Realistically this is one of the three "queue" DTs</param>
    /// <param name="fileNameWithoutPath"></param>
    /// <param name="settingId">Name of the column or tag (e.g. GPSLatitude)</param>
    /// <param name="settingValue">Value to write</param>
    internal static void GenericUpdateAddToDataTable(DataTable dt,
                                                     string fileNameWithoutPath,
                                                     string settingId,
                                                     string settingValue)
    {
        lock (HelperGenericFileLocking.TableLock)
        {
            // delete any existing rows with the current combination
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                DataRow thisDr = dt.Rows[index: i];
                if (
                    thisDr[columnName: "ItemNameWithoutPath"]
                        .ToString() ==
                    fileNameWithoutPath &&
                    thisDr[columnName: "settingId"]
                        .ToString() ==
                    settingId
                )
                {
                    thisDr.Delete();
                }
            }

            dt.AcceptChanges();

            // add new
            DataRow newDr = dt.NewRow();
            newDr[columnName: "ItemNameWithoutPath"] = fileNameWithoutPath;
            newDr[columnName: "settingId"] = settingId;
            newDr[columnName: "settingValue"] = settingValue;
            dt.Rows.Add(row: newDr);
            dt.AcceptChanges();
        }
    }

    /// <summary>
    ///     Joins two datatables. Logically similar to a SQL join.
    /// </summary>
    /// <param name="t1">Name of input table</param>
    /// <param name="t2">Name of input table</param>
    /// <param name="joinOn">Column Name to join on</param>
    /// <returns>A joined datatable</returns>
    internal static DataTable JoinDataTables(DataTable t1,
                                             DataTable t2,
                                             params Func<DataRow, DataRow, bool>[] joinOn)
    {
        // via https://stackoverflow.com/a/11505884/3968494
        // usage
        // var test = JoinDataTables(transactionInfo, transactionItems,
        // (row1, row2) =>
        // row1.Field<int>("TransactionID") == row2.Field<int>("TransactionID"));

        DataTable result = new();
        foreach (DataColumn col in t1.Columns)
        {
            if (result.Columns[name: col.ColumnName] == null)
            {
                result.Columns.Add(columnName: col.ColumnName, type: col.DataType);
            }
        }

        foreach (DataColumn col in t2.Columns)
        {
            if (result.Columns[name: col.ColumnName] == null)
            {
                result.Columns.Add(columnName: col.ColumnName, type: col.DataType);
            }
        }

        foreach (DataRow row1 in t1.Rows)
        {
            EnumerableRowCollection<DataRow> joinRows = t2.AsEnumerable()
                .Where(predicate: row2 =>
                {
                    foreach (Func<DataRow, DataRow, bool> parameter in joinOn)
                    {
                        if (!parameter(arg1: row1, arg2: row2))
                        {
                            return false;
                        }
                    }

                    return true;
                });
            foreach (DataRow fromRow in joinRows)
            {
                DataRow insertRow = result.NewRow();
                foreach (DataColumn col1 in t1.Columns)
                {
                    insertRow[columnName: col1.ColumnName] = row1[columnName: col1.ColumnName];
                }

                foreach (DataColumn col2 in t2.Columns)
                {
                    insertRow[columnName: col2.ColumnName] = fromRow[columnName: col2.ColumnName];
                }

                result.Rows.Add(row: insertRow);
            }
        }

        return result;
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