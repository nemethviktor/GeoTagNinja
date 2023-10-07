using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;

namespace GeoTagNinja.Helpers;

internal static class HelperDataCSVFileOperations
{
    /// <summary>
    ///     Parses a CSV file to a DataTable
    ///     I've given up the original logic of OLEDB because it gets very bitchy with Culture stuff. This works better.
    /// </summary>
    /// <param name="fileNameWithPath">Path of CSV file</param>
    /// <param name="isUTF">whether the file is UTF8-encoded</param>
    /// <returns>Converted Datatable</returns>
    internal static DataTable GetDataTableFromCsv(string fileNameWithPath,
                                                  bool isUTF)
    {
        DataTable dt = new();
        StreamReader reader;
        reader = isUTF
            ? new StreamReader(path: fileNameWithPath, encoding: Encoding.UTF8)
            : new StreamReader(path: fileNameWithPath);

        using CsvReader csv = new(reader: reader, culture: CultureInfo.InvariantCulture);
        // Do any configuration to `CsvReader` before creating CsvDataReader.
        using CsvDataReader dr = new(csv: csv);
        dt.Load(reader: dr);

        return dt;
    }
}