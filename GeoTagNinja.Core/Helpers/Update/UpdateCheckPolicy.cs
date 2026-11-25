using System;

namespace GeoTagNinja.Helpers.Update;

/// <summary>
///     Decides whether this run may go online to look for a newer GeoTagNinja or ExifTool.
/// </summary>
/// <remarks>
///     Normally that is the user's "Check for updates" setting (<see cref="HelperVariables.UserSettingCheckForUpdates" />,
///     on by default; when on, the checks run at most once a week, see <c>APIVersionCheckers</c>).
///     <para>
///         Windows 7 draft: the planned move to Avalonia needs .NET 8, which does not run on Windows 7 (see
///         <c>LegacyWindowsNotice</c>), so a Windows 7 machine can never use an update released after the move and
///         there is no point offering it one. <see cref="ForceOffOnWindows7" /> is the switch that will turn the
///         checks off for good on Windows 7 (and grey the setting out, see <see cref="IsLockedOff" />). It is
///         <b>deliberately false</b>: nothing here changes behaviour yet. To arm it, flip the constant in the build
///         that should stop offering updates to Windows 7 (and say so in the changelog and the Help).
///     </para>
/// </remarks>
internal static class UpdateCheckPolicy
{
    /// <summary>
    ///     Not armed. When true, update checks are permanently off on Windows 7, whatever the user's setting says.
    /// </summary>
    internal const bool ForceOffOnWindows7 = false;

    /// <summary>
    ///     True when the user's choice is overridden to "off" and cannot be changed (only ever true on Windows 7 once
    ///     <see cref="ForceOffOnWindows7" /> is armed). The Settings checkbox shows it as unticked and disabled.
    /// </summary>
    internal static bool IsLockedOff => IsLockedOffFor(osVersion: Environment.OSVersion.Version,
        forceOffOnWindows7: ForceOffOnWindows7);

    /// <summary>
    ///     True when the app may check for updates now: the setting is on and it is not locked off.
    /// </summary>
    internal static bool IsEnabled => IsEnabledFor(userSetting: HelperVariables.UserSettingCheckForUpdates,
        osVersion: Environment.OSVersion.Version,
        forceOffOnWindows7: ForceOffOnWindows7);

    /// <summary>The pure form of <see cref="IsLockedOff" />, so it can be tested without a Windows 7 machine.</summary>
    internal static bool IsLockedOffFor(Version osVersion,
                                        bool forceOffOnWindows7)
    {
        return forceOffOnWindows7 && Startup.LegacyWindowsNotice.IsWindows7(osVersion: osVersion);
    }

    /// <summary>The pure form of <see cref="IsEnabled" />.</summary>
    internal static bool IsEnabledFor(bool userSetting,
                                      Version osVersion,
                                      bool forceOffOnWindows7)
    {
        return userSetting && !IsLockedOffFor(osVersion: osVersion, forceOffOnWindows7: forceOffOnWindows7);
    }
}
