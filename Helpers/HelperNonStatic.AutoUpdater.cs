using AutoUpdaterDotNET;
using Newtonsoft.Json;
using System;
using System.Windows.Forms;
using WinFormsDarkThemerNinja;

namespace GeoTagNinja.Helpers;

internal partial class HelperNonStatic
{
    internal void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
    {
        dynamic json = JsonConvert.DeserializeObject(value: args.RemoteData);
        args.UpdateInfo = new UpdateInfoEventArgs
        {
            CurrentVersion = json.version,
            ChangelogURL = json.changelog,
            DownloadURL = json.url
        };
    }

    internal void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
    {
        if (args.Error == null)
        {
            if (args.IsUpdateAvailable)
            {
                DialogResult dialogResult;
                {
                    dialogResult = Themer.ShowMessageBoxWithResult(
                        message: $@"There is a new version ({args.CurrentVersion}) available. You are using version {args.InstalledVersion}.{Environment.NewLine}Do you want to update the application now?",
                        icon: MessageBoxIcon.Question,
                        buttons: MessageBoxButtons.YesNo);
                }

                if (dialogResult.Equals(obj: DialogResult.Yes) ||
                    dialogResult.Equals(obj: DialogResult.OK))
                {
                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args: args))
                        {
                            Application.Exit();
                        }
                    }
                    catch (Exception exception)
                    {
                        _ = MessageBox.Show(text: exception.Message, caption: exception
                                                                         .GetType()
                                                                         .ToString(), buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}