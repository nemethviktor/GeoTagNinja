using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GeoTagNinja.Helpers;

internal static class HelperDataLanguageTZ
{
    /// <summary>
    ///     Reads the CountryCodes/Country data from the CSV file into a DT
    /// </summary>
    internal static void DataReadCountryCodeDataFromCSV()
    {
        string countryCodeCsvFilePath = Path.Combine(path1: HelperVariables.ResourcesFolderPath, path2: "isoCountryCodeMapping.csv");
        HelperVariables.DtIsoCountryCodeMapping = HelperDataCSVFileOperations.GetDataTableFromCsv(fileNameWithPath: countryCodeCsvFilePath, isUTF: true);
    }

    /// <summary>
    ///     Reads the FrmMainApp.DtIsoCountryCodeMapping and basically translates between code types. We store ALPHA-2, ALPHA-3
    ///     and plain English country names.
    /// </summary>
    /// <param name="queryWhat">ALPHA-2, ALPHA-3 and plain English country names</param>
    /// <param name="inputVal">e.g US or USA or United States of America</param>
    /// <param name="returnWhat">ALPHA-2, ALPHA-3 and plain English country names</param>
    internal static string DataReadDTCountryCodesNames(string queryWhat,
                                                       string inputVal,
                                                       string returnWhat)
    {
        EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in HelperVariables.DtIsoCountryCodeMapping.AsEnumerable()
                                                           where dataRow.Field<string>(columnName: queryWhat) == inputVal
                                                           select dataRow;

        string returnString = "";
        Parallel.ForEach(source: drDataTableData, body: dataRow =>
            {
                returnString = dataRow[columnName: returnWhat]
                    .ToString();
            })
            ;
        return returnString;
    }


    internal static string DataReadDTObjectText(string objectType,
                                                string objectName)
    {
        return (from kvp in HelperGenericAncillaryListsArrays.LanguageStringsDict
                where kvp.Key == objectType + "_" + objectName
                select kvp.Value).FirstOrDefault();
    }

    /// <summary>
    ///     Reads all the language CSV files into one table (FrmMainApp.DtLanguageLabels)
    /// </summary>
    internal static void DataReadLanguageDataFromCSV()
    {
        string languagesFolderPath = Path.Combine(path1: HelperVariables.ResourcesFolderPath, path2: "Languages");

        DataTable dtParsed = new();
        dtParsed.Clear();
        dtParsed.Columns.Add(columnName: "languageName");
        dtParsed.Columns.Add(columnName: "objectType");
        dtParsed.Columns.Add(columnName: "objectName");
        dtParsed.Columns.Add(columnName: "objectText");

        foreach (string fileNameWithPath in Directory.GetFiles(path: languagesFolderPath, searchPattern: "*.csv"))
        {
            DataTable dtObject = HelperDataCSVFileOperations.GetDataTableFromCsv(fileNameWithPath: fileNameWithPath, isUTF: true);

            dtParsed.Clear();

            string objectType = Path.GetFileNameWithoutExtension(path: fileNameWithPath); // e.g. "Button.csv" -> Button

            foreach (DataRow drObjectRow in dtObject.Rows)
            {
                string objectName = drObjectRow[columnName: "objectName"]
                    .ToString();

                for (int i = 1; i < dtObject.Columns.Count; i++)
                {
                    string languageName = dtObject.Columns[index: i]
                        .ColumnName;
                    string objectText = drObjectRow[columnName: languageName]
                        .ToString();
                    if (objectText.Length == 0)
                    {
                        objectText = null;
                    }

                    DataRow drOut = dtParsed.NewRow();
                    drOut[columnName: "languageName"] = languageName;
                    drOut[columnName: "objectType"] = objectType;
                    drOut[columnName: "objectName"] = objectName;
                    drOut[columnName: "objectText"] = objectText;
                    dtParsed.Rows.Add(row: drOut);
                }
            }

            FrmMainApp.DtLanguageLabels.Merge(table: dtParsed);
        }

        // this is far from optimal but for what we need it will do
        // it's only used for pre-caching some Form labels (for now, Edit.)
        for (int i = 1; i <= 2; i++)
        {
            // run 1 is English
            // run 2 is FrmMainApp._AppLanguage
            // hashset takes care of the rest
            string languageNameToGet = null;

            languageNameToGet = i == 1
                ? "English"
                : FrmMainApp._AppLanguage;

            // no need to waste resource.
            if (!(i == 2 && FrmMainApp._AppLanguage == "English"))
            {
                EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in FrmMainApp.DtLanguageLabels.AsEnumerable()
                                                                   where dataRow.Field<string>(columnName: "languageName") == languageNameToGet
                                                                   select dataRow;

                foreach (DataRow drObject in drDataTableData)
                {
                    if (drObject[columnName: "objectText"] != null &&
                        drObject[columnName: "objectText"]
                            .ToString()
                            .Length >
                        0)
                    {
                        string objectName = drObject[columnName: "objectType"] +
                                            "_" +
                                            drObject[columnName: "objectName"];
                        string objectText = drObject[columnName: "objectText"]
                            .ToString();

                        if (i == 2)
                        {
                            if (HelperGenericAncillaryListsArrays.LanguageStringsDict.ContainsKey(key: objectName))
                            {
                                HelperGenericAncillaryListsArrays.LanguageStringsDict.Remove(key: objectName);
                            }
                        }

                        HelperGenericAncillaryListsArrays.LanguageStringsDict.Add(key: objectName, value: objectText);
                    }
                }
            }
        }
    }
}