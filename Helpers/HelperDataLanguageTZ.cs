using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers;

public enum ControlType
{
    Form,
    Button,
    CheckBox,
    RadioButton,
    TextBox,
    MessageBox,
    MessageBoxCaption,
    Label,
    ComboBox,
    ListBox,
    PictureBox,
    Panel,
    DataGridView,
    TreeView,
    ListView,
    ColumnHeader,
    ToolTip,
    ToolStripButton,
    ToolStripMenuItem,
    TabPage,
    GroupBox,
    RichTextBox,
    NumericUpDown
}

internal static class HelperDataLanguageTZ
{
    /// <summary>
    ///     Determines the ControlType enumeration value that corresponds to the given Type of a control.
    /// </summary>
    /// <param name="controlType">The Type of the control for which to determine the ControlType.</param>
    /// <returns>The ControlType enumeration value that corresponds to the given Type of a control.</returns>
    /// <exception cref="ArgumentException">Thrown when an invalid control type is provided.</exception>
    public static ControlType GetControlType(Type controlType)
    {
        Dictionary<Type, ControlType> controlTypeDictionary =
            new()
            {
                { typeof(Form), ControlType.Form },
                { typeof(Button), ControlType.Button },
                { typeof(CheckBox), ControlType.CheckBox },
                { typeof(RadioButton), ControlType.RadioButton },
                { typeof(TextBox), ControlType.TextBox },
                { typeof(Label), ControlType.Label },
                { typeof(ComboBox), ControlType.ComboBox },
                { typeof(ListBox), ControlType.ListBox },
                { typeof(PictureBox), ControlType.PictureBox },
                { typeof(Panel), ControlType.Panel },
                { typeof(DataGridView), ControlType.DataGridView },
                { typeof(TreeView), ControlType.TreeView },
                { typeof(ListView), ControlType.ListView },
                { typeof(ColumnHeader), ControlType.ColumnHeader },
                { typeof(ToolTip), ControlType.ToolTip },
                { typeof(ToolStripButton), ControlType.ToolStripButton },
                { typeof(TabPage), ControlType.TabPage },
                { typeof(ToolStripMenuItem), ControlType.ToolStripMenuItem },
                { typeof(RichTextBox), ControlType.RichTextBox },
                { typeof(GroupBox), ControlType.GroupBox },
                { typeof(NumericUpDown), ControlType.NumericUpDown }
            };
        if (controlTypeDictionary.TryGetValue(key: controlType,
                                              value: out ControlType result))
        {
            return result;
        }

        throw new ArgumentException(message: string.Format(format: "{0}{1}",
                                                           arg0: "Invalid control type: ",
                                                           arg1: controlType));
    }


    /// <summary>
    ///     Reads the CountryCodes/Country data from the CSV file into a DT
    /// </summary>
    internal static void DataReadCountryCodeDataFromCSV()
    {
        string countryCodeCsvFilePath = Path.Combine(
            path1: HelperVariables.ResourcesFolderPath,
            path2: "isoCountryCodeMapping.csv");
        HelperVariables.DtIsoCountryCodeMapping =
            HelperDataCSVFileOperations.GetDataTableFromCsv(
                fileNameWithPath: countryCodeCsvFilePath, isUTF: true);
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
        EnumerableRowCollection<DataRow> drDataTableData =
            from DataRow dataRow in HelperVariables.DtIsoCountryCodeMapping.AsEnumerable()
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


    // ReSharper disable once InconsistentNaming
    internal static string DataReadDTObjectText(ControlType objectType,
                                                string objectName)
    {
        string retStrVal =
            HelperGenericAncillaryListsArrays
               .LanguageStringsDict
               .Where(predicate: kvp => string.Equals(a: kvp.Key, b: objectType + "_" + objectName,
                    comparisonType: StringComparison.OrdinalIgnoreCase))
               .Select(selector: kvp => kvp.Value)
               .FirstOrDefault();

        // this is probably not the smartest way of going about this but alas
        if (objectName.EndsWith(value: "Altitude"))
        {
            if (string.IsNullOrEmpty(value: HelperVariables.UOMAbbreviated))
            {
                FrmMainApp.GetUOMAbbreviated();
            }

            retStrVal = retStrVal + " [" + HelperVariables.UOMAbbreviated + "]";
        }

        return retStrVal;
    }

    /// <summary>
    ///     Reads all the language CSV files into one table (FrmMainApp.DtLanguageLabels)
    /// </summary>
    internal static void DataReadLanguageDataFromCSV()
    {
        string languagesFolderPath =
            Path.Combine(path1: HelperVariables.ResourcesFolderPath, path2: "Languages");

        DataTable dtParsed = new();
        dtParsed.Clear();
        dtParsed.Columns.Add(columnName: "languageName");
        dtParsed.Columns.Add(columnName: "objectType");
        dtParsed.Columns.Add(columnName: "objectName");
        dtParsed.Columns.Add(columnName: "objectText");

        foreach (string fileNameWithPath in Directory.GetFiles(
                     path: languagesFolderPath, searchPattern: "*.csv"))
        {
            DataTable dtObject =
                HelperDataCSVFileOperations.GetDataTableFromCsv(
                    fileNameWithPath: fileNameWithPath, isUTF: true);

            dtParsed.Clear();

            string objectType =
                Path.GetFileNameWithoutExtension(
                    path: fileNameWithPath); // e.g. "Button.csv" -> Button

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
                EnumerableRowCollection<DataRow> drDataTableData =
                    from DataRow dataRow in FrmMainApp.DtLanguageLabels.AsEnumerable()
                    where dataRow.Field<string>(columnName: "languageName") ==
                          languageNameToGet
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
                            if (HelperGenericAncillaryListsArrays.LanguageStringsDict
                               .ContainsKey(key: objectName))
                            {
                                HelperGenericAncillaryListsArrays.LanguageStringsDict
                                   .Remove(key: objectName);
                            }
                        }

                        HelperGenericAncillaryListsArrays.LanguageStringsDict.Add(
                            key: objectName, value: objectText);
                    }
                }
            }
        }
    }
}