using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;

namespace GeoTagNinja.Helpers;

public enum LanguageMappingQueryOrReturnWhat
{
    Country,
    ISO_3166_1A2,
    ISO_3166_1A3,
    Numeric
}

internal static class HelperDataLanguageTZ
{
    /// <summary>
    ///     Reads the CountryCodes/Country data from the CSV file into a DT
    /// </summary>
    internal static Task DataReadCountryCodeDataFromCSV()
    {
        string countryCodeCsvFilePath = Path.Combine(
            path1: HelperVariables.ResourcesFolderPath,
            path2: "isoCountryCodeMapping.csv");

        if (!File.Exists(path: countryCodeCsvFilePath))
        {
            FrmMainApp.Log.Fatal(message: "Error: isoCountryCodeMapping.csv missing");

            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorIsoCountryCodeMappingMissing",
                captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            Application.Exit();
        }

        HelperVariables.DtIsoCountryCodeMapping =
            HelperDataCSVFileOperations.GetDataTableFromCsv(
                fileNameWithPath: countryCodeCsvFilePath, isUTF: true);

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