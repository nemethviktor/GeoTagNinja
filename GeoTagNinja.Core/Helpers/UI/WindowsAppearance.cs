namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     What Windows itself says about the user's appearance settings.
/// </summary>
internal static class WindowsAppearance
{
    /// <summary>
    ///     Whether Windows is set to dark mode for apps. Light when the value can't be read (Windows versions without the
    ///     setting, locked-down registry).
    /// </summary>
    internal static bool PrefersDarkApps()
    {
        try
        {
            using Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                name: @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue(name: "AppsUseLightTheme") is int appsUseLightTheme && appsUseLightTheme == 0;
        }
        catch
        {
            return false;
        }
    }
}
