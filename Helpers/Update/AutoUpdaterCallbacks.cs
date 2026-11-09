using AutoUpdaterDotNET;
using GeoTagNinja.Helpers.UI;
using Newtonsoft.Json;
using System;
using System.Windows.Forms;
using WinFormsDarkThemerNinja;

namespace GeoTagNinja.Helpers.Update;

/// <summary>
///     Event handlers wired into AutoUpdater.NET.
/// </summary>
/// <remarks>
///     GeoTagNinja publishes its update manifest as JSON rather than the XML AutoUpdater.NET expects, so the parse
///     event is handled manually. See <see cref="API.APIVersionCheckers" /> for the checks that decide whether an
///     update run is started at all.
/// </remarks>
internal static class AutoUpdaterCallbacks
{
    /// <summary>
    ///     Translates the project's JSON update manifest into the structure AutoUpdater.NET expects.
    /// </summary>
    /// <param name="args">The event payload carrying the raw remote data, and receiving the parsed result.</param>
    internal static void OnParseUpdateInfo(ParseUpdateInfoEventArgs args)
    {
        dynamic json = JsonConvert.DeserializeObject(value: args.RemoteData);
        args.UpdateInfo = new UpdateInfoEventArgs
        {
            CurrentVersion = json.version,
            ChangelogURL = json.changelog,
            DownloadURL = json.url
        };
    }

    /// <summary>
    ///     Offers the update to the user and, if accepted, downloads it and exits so it can be applied.
    /// </summary>
    /// <param name="args">The event payload describing the available update.</param>
    internal static void OnCheckForUpdate(UpdateInfoEventArgs args)
    {
        if (args.Error != null || !args.IsUpdateAvailable)
        {
            return;
        }

        // Show what the update brings (only released entries, see ChangelogNotes). If that isn't possible (offline,
        // no notes for the range) the plain question is asked instead.
        DialogResult? withNotes =
            Version.TryParse(input: args.CurrentVersion, result: out Version newVersion)
                ? ChangelogNotes.AskToUpdate(installedBuild: args.InstalledVersion.Build, newBuild: newVersion.Build)
                : null;

        DialogResult dialogResult = withNotes ?? Themer.ShowMessageBoxWithResult(
            message:
            $@"There is a new version ({args.CurrentVersion}) available. You are using version {args.InstalledVersion}.{Environment.NewLine}Do you want to update the application now?",
            icon: MessageBoxIcon.Question,
            buttons: MessageBoxButtons.YesNo);

        if (!dialogResult.Equals(obj: DialogResult.Yes) &&
            !dialogResult.Equals(obj: DialogResult.OK))
        {
            return;
        }

        try
        {
            if (AutoUpdater.DownloadUpdate(args: args))
            {
                Application.Exit();
            }
        }
        catch (Exception exception)
        {
            ThemeHelper.ShowMessageBox(message: exception.Message,
                caption: exception.GetType().ToString(),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
        }
    }
}
