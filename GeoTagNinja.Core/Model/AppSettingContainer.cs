namespace GeoTagNinja.Model;

/// <summary>
///     A generic class for a skeleton of setting (app-settings) compatible with the SQLite tables to store the app/user
///     preferences into.
/// </summary>
internal class AppSettingContainer
{
    internal string TableName { get; set; }
    internal string SettingTabPage { get; set; }
    internal string SettingId { get; set; }
    internal string SettingValue { get; set; }
}