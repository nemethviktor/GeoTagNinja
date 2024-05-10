using System;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using Newtonsoft.Json;

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
                //if (args.Mandatory.Value)
                //{
                //    dialogResult =
                //        MessageBox.Show(
                //            text:
                //            $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. This is required update. Press Ok to begin updating the application.",
                //            caption: @"Update Available",
                //            buttons: MessageBoxButtons.OK,
                //            icon: MessageBoxIcon.Information);
                //}
                //else
                {
                    dialogResult =
                        MessageBox.Show(
                            text:
                            $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. Do you want to update the application now?",
                            caption: @"Update Available",
                            buttons: MessageBoxButtons.YesNo,
                            icon: MessageBoxIcon.Information);
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
                        MessageBox.Show(text: exception.Message, caption: exception
                                           .GetType()
                                           .ToString(), buttons: MessageBoxButtons.OK,
                                        icon: MessageBoxIcon.Error);
                    }
                }
            }
            //else
            //{
            //    MessageBox.Show(
            //        text: @"There is no update available please try again later.",
            //        caption: @"No update available",
            //        buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
            //}
        }
        //if (args.Error is WebException)
        //{
        //    MessageBox.Show(
        //        text:
        //        @"There is a problem reaching update server. Please check your internet connection and try again later.",
        //        caption: @"Update Check Failed", buttons: MessageBoxButtons.OK,
        //        icon: MessageBoxIcon.Error);
        //}
        //else
        //{
        //    MessageBox.Show(text: args.Error.Message,
        //                    caption: args.Error.GetType()
        //                                 .ToString(), buttons: MessageBoxButtons.OK,
        //                    icon: MessageBoxIcon.Error);
        //}
    }
}