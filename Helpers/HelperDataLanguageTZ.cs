using System.Data;
using System.Threading.Tasks;

namespace GeoTagNinja.Helpers;

/// <summary>
/// Specifies the fields that can be queried or returned when mapping country information to language codes or
/// identifiers.
/// </summary>
/// <remarks>The order of the enumeration values is significant and must not be changed, as it is required for
/// correct parsing into a DataTable from Wikipedia sources.</remarks>
public enum LanguageMappingQueryOrReturnWhat
{
    // DO NOT REORDER (Specific order needed for parsing into DataTable from Wikipedia)
    Country,
    ISO_3166_1A2,
    ISO_3166_1A3,
    Numeric
}

internal static class HelperDataLanguageTZ
{
    /// <summary>
    ///     Reads the CountryCodes/Country data from the Wikipedia string into a DT
    ///     This is a legacy method that I left here as it was originally reading a CSV file.
    /// </summary>
    internal static Task DataReadCountryCodeDataFromWikipediaData()
    {
        HelperVariables.DtIsoCountryCodeMapping =
            HelperGenericAncillaryListsArrays.GetCountryDetailsToTable();

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Reads the FrmMainApp.DtIsoCountryCodeMapping and basically translates between code types. We store ALPHA-2, ALPHA-3
    ///     and plain English country names.
    /// </summary>
    /// <param name="queryWhat">ALPHA-2, ALPHA-3 and plain English country names</param>
    /// <param name="inputVal">e.g US or USA or United States of America</param>
    /// <param name="returnWhat">ALPHA-2, ALPHA-3 and plain English country names</param>
    internal static string DataReadDTCountryCodesNames(LanguageMappingQueryOrReturnWhat queryWhat,
        string inputVal,
        LanguageMappingQueryOrReturnWhat returnWhat)
    {
        EnumerableRowCollection<DataRow> drDataTableData =
            from DataRow dataRow in HelperVariables.DtIsoCountryCodeMapping.AsEnumerable()
            where dataRow.Field<string>(columnName: queryWhat.ToString()) == inputVal
            select dataRow;

        string returnString = "";
        _ = Parallel.ForEach(source: drDataTableData, body: dataRow =>
            {
                returnString = dataRow[columnName: returnWhat.ToString()]
                   .ToString();
            })
            ;
        return returnString;
    }
}