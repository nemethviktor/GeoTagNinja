using NLog;
using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using System;
using System.Globalization;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.Startup;

/// <summary>
///     One-off warning for Windows 7 users that support for it ends in 2027. The planned move to Avalonia needs
///     .NET 8 or newer, which does not run on Windows 7. Shown once per settings database, whatever the build, so it
///     is independent of <see cref="UpgradeNotices" /> (which is keyed on the build and would also show it to new
///     Windows 7 users only if they happened to be upgrading).
/// </summary>
internal static class LegacyWindowsNotice
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string SettingTabPage = "generic";
    private const string SettingId = "win7EndOfSupportNoticeShown";

    // The text is in code, not Strings.resx, for the same reason as the upgrade notices: it is time-bound
    // and shouldn't wait for translators.
    internal const string Message =
        "Support for Windows 7 will end in 2027." +
        "\r\n\r\n" +
        "GeoTagNinja is moving to Avalonia, which requires .NET 8 or newer, and that does not run on Windows 7. " +
        "Versions released after the move will need Windows 10 or later." +
        "\r\n\r\n" +
        "This message is shown only once.";

    /// <summary>Windows 7 and Windows Server 2008 R2 report 6.1. (Newer versions only ever report higher.)</summary>
    internal static bool IsWindows7(Version osVersion)
    {
        return osVersion.Major == 6 && osVersion.Minor == 1;
    }

    /// <summary>Shows the notice if this is Windows 7 and it has not been shown before. Never throws.</summary>
    internal static void ShowIfNeeded()
    {
        try
        {
            if (!IsWindows7(osVersion: Environment.OSVersion.Version))
            {
                return;
            }

            string shown = ApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings,
                settingTabPage: SettingTabPage,
                settingId: SettingId);
            if (shown == "1")
            {
                return;
            }

            ThemeHelper.ShowMessageBox(message: Message, caption: "GeoTagNinja", icon: MessageBoxIcon.Warning);

            ApplicationSettings.DataWriteSQLiteSettings(settingsToWrite:
            [
                new AppSettingContainer
                {
                    TableName = "settings",
                    SettingTabPage = SettingTabPage,
                    SettingId = SettingId,
                    SettingValue = "1"
                }
            ]);
        }
        catch (Exception ex)
        {
            Log.Error(message: $"Windows 7 notice - could not show: {ex.Message}");
        }
    }
}
