using GeoTagNinja.Helpers.API;
using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.Update;

/// <summary>
///     Gets the changelog from where it is safe to read it and shows it, before and after an update. The selection
///     rules are in <see cref="ChangelogParser" />.
/// </summary>
/// <remarks>
///     Before an update the text is fetched from the <i>release tag</i> (<c>b{build}</c>), never from the default
///     branch, so nothing written for a later release can appear; the build-number filter is a second guard. After an
///     update the copy installed next to the exe is used (the installer ships it), with the tag as a fallback. Every
///     failure is swallowed (and logged): the changelog is a courtesy and must never get in the way of starting or
///     updating the app.
/// </remarks>
internal static class ChangelogNotes
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string SettingTabPage = "tpg_Application";
    private const string SettingId = "ckb_ShowChangelogAfterUpdate";

    /// <summary>
    ///     What the changelog's unnumbered "upcoming" entry stands for. For someone who takes pre-releases (the
    ///     `Check for pre-releases` setting) the changelog at a pre-release's tag, or installed with one, has the
    ///     pre-release itself as that entry, so it is shown as <paramref name="build" />. For everyone else it is never
    ///     shown (0).
    /// </summary>
    private static int UpcomingAs(int build)
    {
        return HelperVariables.UserSettingUpdatePreReleaseGTN
            ? build
            : 0;
    }

    /// <summary>The changelog as it was at the release of <paramref name="build" />.</summary>
    internal static string TagUrl(int build)
    {
        return
            $"https://raw.githubusercontent.com/nemethviktor/GeoTagNinja/b{build.ToString(provider: CultureInfo.InvariantCulture)}/changelog.md";
    }

    /// <summary>Downloads the changelog of a release; null if that is not possible (offline, no such tag, ...).</summary>
    private static string FetchFromTag(int build)
    {
        try
        {
            return new TimedWebClient { Timeout = 4000, Encoding = Encoding.UTF8 }.DownloadString(
                address: TagUrl(build: build));
        }
        catch (Exception ex)
        {
            Log.Info(message: $"Changelog: could not fetch the notes for build {build}: {ex.Message}");
            return null;
        }
    }

    /// <summary>The changelog installed with the running app; null if it is missing.</summary>
    private static string ReadInstalled()
    {
        try
        {
            string path = Path.Combine(path1: AppDomain.CurrentDomain.BaseDirectory, path2: "changelog.md");
            return File.Exists(path: path)
                ? File.ReadAllText(path: path, encoding: Encoding.UTF8)
                : null;
        }
        catch (Exception ex)
        {
            Log.Info(message: $"Changelog: could not read the installed copy: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Asks whether to update, showing what the update brings. Null if the notes could not be had (the caller
    ///     then asks in the plain way).
    /// </summary>
    /// <param name="installedBuild">The running build.</param>
    /// <param name="newBuild">The release on offer.</param>
    /// <returns>Yes to update, No for later; null when there is nothing to show.</returns>
    internal static DialogResult? AskToUpdate(int installedBuild,
                                              int newBuild)
    {
        try
        {
            List<ChangelogSection> sections = ChangelogParser.Between(markdown: FetchFromTag(build: newBuild),
                afterBuild: installedBuild, upToBuild: newBuild, upcomingAsBuild: UpcomingAs(build: newBuild));
            if (sections.Count == 0)
            {
                return null;
            }

            using FrmChangelog dialog = new(mode: FrmChangelog.Mode.BeforeUpdate,
                intro: $"Version {newBuild} is available. You are using version {installedBuild}. " +
                       "This is what has changed since then. Do you want to update now?",
                sections: sections);
            return dialog.ShowDialog(owner: FrmMainApp.Instance);
        }
        catch (Exception ex)
        {
            Log.Warn(message: $"Changelog: could not show the notes before the update: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     After an update: shows what is new since the build the user last ran, unless they have turned that off.
    ///     Does nothing for a first install (<paramref name="lastRunBuild" /> is not lower than the current build).
    /// </summary>
    /// <param name="lastRunBuild">The build the app ran last time.</param>
    /// <param name="currentBuild">The running build.</param>
    internal static void ShowAfterUpdate(int lastRunBuild,
                                         int currentBuild)
    {
        if (!HelperVariables.UserSettingShowChangelogAfterUpdate || lastRunBuild >= currentBuild)
        {
            return;
        }

        try
        {
            string markdown = ReadInstalled() ?? FetchFromTag(build: currentBuild);
            List<ChangelogSection> sections = ChangelogParser.Between(markdown: markdown,
                afterBuild: lastRunBuild, upToBuild: currentBuild, upcomingAsBuild: UpcomingAs(build: currentBuild));
            if (sections.Count == 0)
            {
                return;
            }

            using FrmChangelog dialog = new(mode: FrmChangelog.Mode.AfterUpdate,
                intro: $"GeoTagNinja has been updated to version {currentBuild}. " +
                       "This is what has changed since the version you used last.",
                sections: sections);
            _ = dialog.ShowDialog(owner: FrmMainApp.Instance);

            if (dialog.DoNotShowAgain)
            {
                HelperVariables.UserSettingShowChangelogAfterUpdate = false;
                ApplicationSettings.DataWriteSQLiteSettings(settingsToWrite:
                [
                    new AppSettingContainer
                    {
                        TableName = "settings",
                        SettingTabPage = SettingTabPage,
                        SettingId = SettingId,
                        SettingValue = "false"
                    }
                ]);
            }
        }
        catch (Exception ex)
        {
            Log.Warn(message: $"Changelog: could not show the notes after the update: {ex.Message}");
        }
    }
}
