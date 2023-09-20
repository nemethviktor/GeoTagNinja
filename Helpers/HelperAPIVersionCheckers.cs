using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using geoTagNinja;
using GeoTagNinja.View.CustomMessageBox;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace GeoTagNinja.Helpers;

internal static class HelperAPIVersionCheckers
{
    /// <summary>
    ///     Responsible for pulling the latest prod-version number of exifTool from exiftool.org
    /// </summary>
    /// <returns>The version number of the currently newest exifTool uploaded to exiftool.org</returns>
    internal static decimal API_ExifGetExifToolVersionFromWeb()
    {
        decimal returnVal = 0.0m;
        decimal parsedDecimal = 0.0m;
        string onlineExifToolVer;

        bool parsedResult;
        try
        {
            onlineExifToolVer = new WebClient().DownloadString(address: "http://exiftool.org/ver.txt");
            parsedResult = decimal.TryParse(s: onlineExifToolVer, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedDecimal);
            if (parsedResult)
            {
                returnVal = parsedDecimal;
            }
            else
            {
                returnVal = 0.0m;
            }
        }
        catch
        {
            returnVal = 0.0m;
        }

        return returnVal;
    }


    /// <summary>
    ///     Responsible for pulling the latest release of GTN from gitHub
    ///     GitHub doesn't store "build" numbers as such so the easiest is to pull the datediff of the last relevant build and
    ///     2000/1/1 and then compare it to the current version.
    /// </summary>
    /// <returns>The version number of the latest GTN release</returns>
    private static int API_GenericGetGTNVersionFromWeb()
    {
        GTNReleaseAPIResponse returnVal = new();
        RestClientOptions options = new(baseUrl: "https://api.github.com")
        {
            Authenticator = new HttpBasicAuthenticator(username: "demo", password: "demo")
        };
        RestClient client = new(options: options);

        {
            // admittedly no idea how to do this w/o any auth (as it's not needed) but this appears to work.
        }
        ;
        RestRequest request_GTNVersionQuery = new(resource: "repos/nemethviktor/GeoTagNinja/releases");
        RestResponse response_GTNVersionQuery = client.ExecuteGet(request: request_GTNVersionQuery);

        int lastGTNBuildOnline = 0;
        if (response_GTNVersionQuery.StatusCode.ToString() == "OK")
        {
            HelperVariables.OperationAPIReturnedOKResponse = true;
            JArray data = (JArray)JsonConvert.DeserializeObject(value: response_GTNVersionQuery.Content);
            GTNReleaseAPIResponse[] gtnReleasesApiResponse = GTNReleaseAPIResponse.FromJson(json: data.ToString());

            // January 1, 2000
            DateTimeOffset january1st2000 = new(year: 2000, month: 1, day: 1, hour: 0, minute: 0, second: 0, offset: TimeSpan.Zero);

            DateTimeOffset lastGTNBuildDate = default;

            if (HelperVariables.UserSettingUpdatePreReleaseGTN)
            {
                lastGTNBuildDate = new DateTimeOffset(dateTime: gtnReleasesApiResponse[0].PublishedAt);
            }
            else
            {
                for (int i = 0; i < gtnReleasesApiResponse.Length; i++)
                {
                    if ((bool)!gtnReleasesApiResponse[i].Prerelease)
                    {
                        lastGTNBuildDate = new DateTimeOffset(dateTime: gtnReleasesApiResponse[i].PublishedAt);
                    }
                }
            }

            // Calculate the number of days between lastGTNBuild and January 1, 2000
            double daysDifference = (lastGTNBuildDate - january1st2000).TotalDays;

            // Add some logic to return 0 for build number if there's no result. While negative 73k isn't likely to be a problem I don't feel like experimenting.
            lastGTNBuildOnline = Math.Max(val1: 0, val2: (int)Math.Floor(d: daysDifference));
        }
        else
        {
            HelperVariables.OperationAPIReturnedOKResponse = false;
            CustomMessageBox customMessageBox = new(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_Helper_WarningGTNVerAPIResponse") +
                      response_GTNVersionQuery.StatusCode,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                    captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Warning.ToString()),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
            customMessageBox.ShowDialog();
        }

        return lastGTNBuildOnline;
    }


    /// <summary>
    ///     Checks for new versions of GTN and ExifTool.
    /// </summary>
    internal static async Task CheckForNewVersions()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        // check when the last polling took place
        long nowUnixTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        long lastCheckUnixTime = 0;

        string strLastOnlineVersionCheck = HelperDataApplicationSettings.DataReadSQLiteSettings(
            tableName: "settings",
            settingTabPage: "generic",
            settingId: "onlineVersionCheckDate"
        );

        if (strLastOnlineVersionCheck == null)
        {
            lastCheckUnixTime = nowUnixTime;
            // write back to SQL so it doesn't remain blank
            HelperDataApplicationSettings.DataWriteSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "onlineVersionCheckDate",
                settingValue: nowUnixTime.ToString(provider: CultureInfo.InvariantCulture)
            );
        }
        else
        {
            lastCheckUnixTime = long.Parse(s: strLastOnlineVersionCheck);
        }

        FrmMainApp.Logger.Trace(message: "nowUnixTime > lastCheckUnixTime:" + (nowUnixTime - lastCheckUnixTime));
        int checkUpdateVal = 604800; //604800 is a week's worth of seconds
        #if DEBUG
        checkUpdateVal = 1;
        #endif

        if (nowUnixTime > lastCheckUnixTime + checkUpdateVal)
        {
            FrmMainApp.Logger.Trace(message: "Checking for new versions.");

            // get current & newest exiftool version -- do this here at the end so it doesn't hold up the process
            ///////////////

            string exiftoolCmd = "-ver";
            await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                                         frmMainAppInstance: null,
                                                         initiator: "GenericCheckForNewVersions");
            decimal newestExifToolVersionOnline = API_ExifGetExifToolVersionFromWeb();

            FrmMainApp.Logger.Trace(message: "currentExifToolVersionLocal: " + HelperVariables._currentExifToolVersionLocal + " / newestExifToolVersionOnline: " + newestExifToolVersionOnline);

            string strCurrentExifToolVersionInSQL = HelperDataApplicationSettings.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "exifToolVer"
            );

            FrmMainApp.Logger.Trace(message: "strCurrentExifToolVersionInSQL: " + strCurrentExifToolVersionInSQL);

            if (!decimal.TryParse(s: strCurrentExifToolVersionInSQL, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out decimal currentExifToolVersionInSQL))
            {
                currentExifToolVersionInSQL = HelperVariables._currentExifToolVersionLocal;
            }

            // shouldn't really happen but...
            if (HelperVariables._currentExifToolVersionLocal != currentExifToolVersionInSQL)
            {
                // write current to SQL
                HelperDataApplicationSettings.DataWriteSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "generic",
                    settingId: "exifToolVer",
                    settingValue: HelperVariables._currentExifToolVersionLocal.ToString(provider: CultureInfo.InvariantCulture)
                );

                currentExifToolVersionInSQL = HelperVariables._currentExifToolVersionLocal;
            }

            if (newestExifToolVersionOnline > HelperVariables._currentExifToolVersionLocal && newestExifToolVersionOnline > currentExifToolVersionInSQL && HelperVariables._currentExifToolVersionLocal + newestExifToolVersionOnline > 0)
            {
                FrmMainApp.Logger.Trace(message: "Writing new version to SQL: " + newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture));
                // write current to SQL
                HelperDataApplicationSettings.DataWriteSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "generic",
                    settingId: "exifToolVer",
                    settingValue: newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture)
                );

                CustomMessageBox customMessageBox = new(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                              messageBoxName: "mbx_FrmMainApp_InfoNewExifToolVersionExists") +
                          newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture),
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Warning.ToString()),
                    buttons: MessageBoxButtons.YesNo,
                    icon: MessageBoxIcon.Asterisk);
                DialogResult dialogResult = customMessageBox.ShowDialog();
                if (dialogResult == DialogResult.Yes)
                {
                    Process.Start(fileName: "https://exiftool.org/exiftool-" + newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture) + ".zip");
                    FrmMainApp.Logger.Trace(message: "User Launched Browser to Download");
                }
                else
                {
                    FrmMainApp.Logger.Trace(message: "User Declined Launch to Download");
                }
            }

            // current version may be something like "0.5.8251.40825"
            // Assembly.GetExecutingAssembly().GetName().Version.Build is just "8251"
            // ReSharper disable once InconsistentNaming
            int currentGTNVersionBuild = Assembly.GetExecutingAssembly()
                                                 .GetName()
                                                 .Version.Build;

            HelperVariables.OperationAPIReturnedOKResponse = true;
            // ReSharper disable once InconsistentNaming
            int newestOnlineGTNVersion = 0;
            try
            {
                newestOnlineGTNVersion = API_GenericGetGTNVersionFromWeb();
            }
            catch
            {
                // ignore
            }

            if (newestOnlineGTNVersion > currentGTNVersionBuild)
            {
                CustomMessageBox customMessageBox = new(
                        text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoNewGTNVersionExists") +
                              newestOnlineGTNVersion,
                        caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Warning.ToString()),
                        buttons: MessageBoxButtons.YesNo,
                        icon: MessageBoxIcon.Asterisk)
                    ;
                DialogResult dialogResult = customMessageBox.ShowDialog();
                if (dialogResult == DialogResult.Yes)
                {
                    Process.Start(fileName: "https://github.com/nemethviktor/GeoTagNinja/releases/download/b" + newestOnlineGTNVersion.ToString(provider: CultureInfo.InvariantCulture) + "/GeoTagNinja_Setup.msi");
                }
            }

            // write back to SQL
            HelperDataApplicationSettings.DataWriteSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "onlineVersionCheckDate",
                settingValue: nowUnixTime.ToString()
            );
        }
        else
        {
            FrmMainApp.Logger.Trace(message: "Not checking for new versions.");
        }
    }
}