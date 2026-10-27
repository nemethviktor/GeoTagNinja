using GeoTagNinja.Model;
using System.Collections.Generic;
using System.Globalization;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Stores a setting that is changed from a menu rather than from the Settings dialog.
/// </summary>
/// <remarks>
///     <para>
///         The settings system is keyed by control-style names: a setting is a row in the <c>settings</c> table whose
///         <c>settingId</c> is the name of the control that used to edit it (<c>ckb_…</c> for a check box, <c>nud_…</c> for
///         a number, and so on), under the tab page that control lived on. Import/export copies the whole table, and
///         <c>AppStartup.AppStartupApplyDefaults</c> maps each id to a <see cref="HelperVariables" /> field.
///     </para>
///     <para>
///         Moving a setting into a menu therefore needs <b>no migration</b> as long as its id stays the same: the row keeps
///         working, existing users keep their value, and only the control in <c>FrmSettings</c> goes away. These helpers
///         write through <see cref="ApplicationSettings.DataWriteSQLiteSettings" />, which also refreshes the in-memory
///         settings tables, so everything else that reads settings sees the change at once. (Renaming an id <i>would</i>
///         need a one-off migration of the stored row.) See "Moving a setting into a menu" in <c>development-notes.md</c>.
///     </para>
/// </remarks>
internal static class MenuSettings
{
    /// <summary>The tab page of the Settings dialog that holds general application settings.</summary>
    internal const string ApplicationTab = "tpg_Application";

    /// <summary>Stores a text value for a setting id.</summary>
    internal static void Write(string settingId,
                               string value,
                               string settingTabPage = ApplicationTab)
    {
        ApplicationSettings.DataWriteSQLiteSettings(settingsToWrite:
        [
            new AppSettingContainer
            {
                TableName = "settings",
                SettingTabPage = settingTabPage,
                SettingId = settingId,
                SettingValue = value
            }
        ]);
    }

    /// <summary>Stores a boolean the way the check boxes do ("true"/"false").</summary>
    internal static void WriteBool(string settingId,
                                   bool value,
                                   string settingTabPage = ApplicationTab) =>
        Write(settingId: settingId, value: value ? "true" : "false", settingTabPage: settingTabPage);

    /// <summary>Stores a number the way the numeric boxes do (invariant digits).</summary>
    internal static void WriteInt(string settingId,
                                  int value,
                                  string settingTabPage = ApplicationTab) =>
        Write(settingId: settingId, value: value.ToString(provider: CultureInfo.InvariantCulture), settingTabPage: settingTabPage);

    /// <summary>The same row as an <see cref="AppSettingContainer">, for callers that batch several writes.</see></summary>
    internal static AppSettingContainer Row(string settingId,
                                            string value,
                                            string settingTabPage = ApplicationTab) => new()
    {
        TableName = "settings",
        SettingTabPage = settingTabPage,
        SettingId = settingId,
        SettingValue = value
    };

    /// <summary>Writes several settings in one transaction.</summary>
    internal static void WriteMany(IEnumerable<AppSettingContainer> rows) =>
        ApplicationSettings.DataWriteSQLiteSettings(settingsToWrite: [.. rows]);
}
