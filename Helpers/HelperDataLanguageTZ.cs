using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.View.DialogAndMessageBoxes;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;

namespace GeoTagNinja.Helpers;

[SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
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
            CustomMessageBox customMessageBox = new(
                text: ReturnControlText(
                    controlName:
                    "mbx_FrmMainApp_ErrorIsoCountryCodeMappingMissing",
                    fakeControlType: FakeControlTypes.MessageBox),
                caption: ReturnControlText(
                    controlName: MessageBoxCaption.Error.ToString(),
                    fakeControlType: FakeControlTypes.MessageBoxCaption),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            customMessageBox.ShowDialog();
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
        Parallel.ForEach(source: drDataTableData, body: dataRow =>
            {
                returnString = dataRow[columnName: returnWhat.ToString()]
                   .ToString();
            })
            ;
        return returnString;
    }
}