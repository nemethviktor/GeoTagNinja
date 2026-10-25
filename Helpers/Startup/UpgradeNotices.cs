using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace GeoTagNinja.Helpers.Startup;

/// <summary>
///     A message shown once to users upgrading from a build at or below <see cref="ShowIfLastRunBuildAtMost" />.
/// </summary>
internal sealed class UpgradeNotice
{
    internal UpgradeNotice(int showIfLastRunBuildAtMost,
                           string message)
    {
        ShowIfLastRunBuildAtMost = showIfLastRunBuildAtMost;
        Message = message;
    }

    /// <summary>
    ///     Shown when the build the user last ran is this or older. Set it to the last build that was released
    ///     <i>without</i> the change being announced (so it must be lower than the build that ships the notice).
    /// </summary>
    internal int ShowIfLastRunBuildAtMost { get; }

    /// <summary>
    ///     The text, in English. It deliberately lives in code rather than in <c>Strings.resx</c>: a build can go
    ///     public before translators have caught up, and a notice tied to a specific build shouldn't wait for them.
    /// </summary>
    internal string Message { get; }
}

/// <summary>
///     "Tell users who upgrade from build X or older that Y changed." The app stores the build it last ran (just
///     <c>Version.Build</c>, e.g. 9778, not the full version) in the settings table, and
///     compares it with <see cref="Registry" /> at startup.
/// </summary>
/// <remarks>
///     To announce a future change, add an entry (with its English text) to <see cref="Registry" />. A notice
///     appears once, because the stored build is raised to the current one right after it has been shown. New users
///     (nothing in the settings table yet) never see any. Users from before this tracking existed have no stored
///     build and are treated as being on <see cref="LastBuildBeforeTracking" />.
/// </remarks>
internal static class UpgradeNotices
{
    /// <summary>The last public build that did not record the build it ran (Build 9778 [20261009]).</summary>
    internal const int LastBuildBeforeTracking = 9778;

    private const string SettingTabPage = "generic";
    private const string SettingId = "lastRunBuild";

    internal static readonly IReadOnlyList<UpgradeNotice> Registry =
    [
        // The map search no longer needs an ArcGIS API key (GeoNames -> Photon -> Nominatim).
        new UpgradeNotice(showIfLastRunBuildAtMost: 9778,
            message: "The map's search box no longer needs an ArcGIS API key." +
                     Environment.NewLine + Environment.NewLine +
                     "It now looks places up with GeoNames (using the username from your Settings), then Photon, " +
                     "then Nominatim, so there is nothing to register for it any more. " +
                     "A key you saved earlier is no longer used.")
    ];

    private static List<UpgradeNotice> _pending = [];
    private static int _buildToStoreAfterShowing;

    /// <summary>
    ///     True for the run in which the settings database was brand new (set by <see cref="EvaluateAtStartup" />).
    ///     Lets first-run-only defaults, such as following Windows's light/dark mode, leave existing users alone.
    /// </summary>
    internal static bool IsFreshInstall { get; private set; }

    /// <summary>
    ///     A new user has neither a stored build nor any settings. Existing users from before the build was tracked
    ///     have settings but no stored build.
    /// </summary>
    internal static bool IsNewInstall(string storedBuild,
                                      bool settingsAlreadyExist)
    {
        return !int.TryParse(s: storedBuild, style: NumberStyles.Integer, provider: CultureInfo.InvariantCulture,
                   result: out _) &&
               !settingsAlreadyExist;
    }

    /// <summary>
    ///     The decision itself, kept free of SQLite and UI so it can be tested.
    /// </summary>
    /// <param name="storedBuild">The stored build setting, or null/blank if there is none (or it is garbage).</param>
    /// <param name="settingsAlreadyExist">False only on a fresh install, before any default has been written.</param>
    /// <param name="currentBuild">The build that is running now.</param>
    /// <param name="notices">The notices to choose from.</param>
    /// <returns>The notices to show, and the build to store (never lower than what was stored).</returns>
    internal static (List<UpgradeNotice> Pending, int BuildToStore) Evaluate(string storedBuild,
                                                                           bool settingsAlreadyExist,
                                                                           int currentBuild,
                                                                           IEnumerable<UpgradeNotice> notices)
    {
        bool hasStoredBuild = int.TryParse(s: storedBuild, style: NumberStyles.Integer,
            provider: CultureInfo.InvariantCulture, result: out int lastRunBuild);

        if (IsNewInstall(storedBuild: storedBuild, settingsAlreadyExist: settingsAlreadyExist))
        {
            return ([], currentBuild); // brand new user: nothing to announce
        }

        if (!hasStoredBuild)
        {
            lastRunBuild = LastBuildBeforeTracking;
        }

        List<UpgradeNotice> pending = notices
                                     .Where(predicate: notice => lastRunBuild <= notice.ShowIfLastRunBuildAtMost)
                                     .ToList();
        return (pending, Math.Max(val1: lastRunBuild, val2: currentBuild));
    }

    /// <summary>
    ///     Reads the stored build and works out which notices are due. Must run after the settings tables are loaded
    ///     but <i>before</i> the default settings are written (they would make a new user look like an existing one).
    /// </summary>
    internal static void EvaluateAtStartup()
    {
        try
        {
            string storedBuild = ApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings,
                settingTabPage: SettingTabPage,
                settingId: SettingId);

            bool settingsAlreadyExist = HelperVariables.DtHelperDataApplicationSettings.Rows.Count > 0;
            IsFreshInstall = IsNewInstall(storedBuild: storedBuild, settingsAlreadyExist: settingsAlreadyExist);

            (List<UpgradeNotice> pending, int buildToStore) = Evaluate(
                storedBuild: storedBuild,
                settingsAlreadyExist: settingsAlreadyExist,
                currentBuild: CurrentBuild(),
                notices: Registry);

            _pending = pending;
            _buildToStoreAfterShowing = buildToStore;

            // With nothing to show there is no reason to wait. (If there is, the build is stored once the user has seen it,
            // so a crash before then doesn't swallow the message.)
            if (pending.Count == 0 &&
                storedBuild != buildToStore.ToString(provider: CultureInfo.InvariantCulture))
            {
                WriteStoredBuild(build: buildToStore);
            }
        }
        catch (Exception ex)
        {
            _pending = [];
            FrmMainApp.Log.Error(message: $"Upgrade notices - could not evaluate: {ex.Message}");
        }
    }

    /// <summary>
    ///     Shows whatever <see cref="EvaluateAtStartup" /> found due (all in one box), then records the current build so
    ///     it doesn't appear again. Call once the main form is up.
    /// </summary>
    internal static void ShowPendingNotices()
    {
        if (_pending.Count == 0)
        {
            return;
        }

        try
        {
            ThemeHelper.ShowMessageBox(
                message: string.Join(separator: Environment.NewLine + Environment.NewLine + "----" +
                                                Environment.NewLine + Environment.NewLine,
                    values: _pending.Select(selector: notice => notice.Message)),
                caption: "GeoTagNinja",
                icon: System.Windows.Forms.MessageBoxIcon.Information);

            WriteStoredBuild(build: _buildToStoreAfterShowing);
        }
        catch (Exception ex)
        {
            FrmMainApp.Log.Error(message: $"Upgrade notices - could not show: {ex.Message}");
        }
        finally
        {
            _pending = [];
        }
    }

    /// <summary>
    ///     The build number of the running app (the "9778" in Build 9778), not the full version, which might be
    ///     restructured one day.
    /// </summary>
    private static int CurrentBuild()
    {
        return Assembly.GetExecutingAssembly().GetName().Version.Build;
    }

    private static void WriteStoredBuild(int build)
    {
        ApplicationSettings.DataWriteSQLiteSettings(settingsToWrite:
        [
            new AppSettingContainer
            {
                TableName = "settings",
                SettingTabPage = SettingTabPage,
                SettingId = SettingId,
                SettingValue = build.ToString(provider: CultureInfo.InvariantCulture)
            }
        ]);
    }
}
