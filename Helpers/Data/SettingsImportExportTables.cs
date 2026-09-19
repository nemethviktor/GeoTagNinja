using System.Collections.Generic;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Maps each <see cref="SettingsImportExportOptions" /> member onto the SQLite table that backs it.
/// </summary>
internal static class SettingsImportExportTables
{
    /// <summary>
    ///     Retrieves the table name associated with a given setting name for import/export operations.
    /// </summary>
    /// <param name="settingName">The name of the setting whose table name is to be retrieved.</param>
    /// <returns>
    ///     The table name associated with the given setting name. If the setting name does not exist in the dictionary,
    ///     it returns null.
    /// </returns>
    public static string GetSettingsImportExportTableName(string settingName)
    {
        Dictionary<string, string> SettingsImportExportTableNames = new()
        {
            { "ApplicationSettings", "settings" },
            { "CityRulesSettings", "customCityAllocationLogic" },
            { "CustomRulesSettings", "customRules" }
        };

        return SettingsImportExportTableNames.TryGetValue(
            key: settingName, value: out string tableName)
            ? tableName
            : null;
    }
}
