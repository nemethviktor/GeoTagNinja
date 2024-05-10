using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using geoTagNinja;
using GeoTagNinja.View.DialogAndMessageBoxes;
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
    /// <returns>The version number of the currently newest exifTool uploaded to exiftool.org or 0.0m if that fails.</returns>
    private static decimal API_ExifGetExifToolVersionFromWeb()
    {
        decimal returnVal = 0.0m;
        decimal parsedDecimal = 0.0m;
        string onlineExifToolVer;

        bool parsedResult;
        try
        {
            onlineExifToolVer =
                new WebClient().DownloadString(address: "http://exiftool.org/ver.txt");
            parsedResult = decimal.TryParse(s: onlineExifToolVer, style: NumberStyles.Any,
                                            provider: CultureInfo.InvariantCulture,
                                            result: out parsedDecimal);
            returnVal = parsedResult
                ? parsedDecimal
                : 0.0m;
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
        RestRequest request_GTNVersionQuery =
            new(resource: "repos/nemethviktor/GeoTagNinja/releases");
        RestResponse response_GTNVersionQuery =
            client.ExecuteGet(request: request_GTNVersionQuery);

        int lastGTNBuildOnline = 0;
        if (response_GTNVersionQuery.StatusCode.ToString() == "OK")
        {
            HelperVariables.OperationAPIReturnedOKResponse = true;
            JArray data =
                (JArray)JsonConvert.DeserializeObject(
                    value: response_GTNVersionQuery.Content);
            GTNReleaseAPIResponse[] gtnReleasesApiResponse =
                GTNReleaseAPIResponse.FromJson(json: data.ToString());

            // January 1, 2000
            DateTimeOffset january1st2000 = new(year: 2000, month: 1, day: 1, hour: 0,
                                                minute: 0, second: 0,
                                                offset: TimeSpan.Zero);

            DateTimeOffset lastGTNBuildDate = default;

            if (HelperVariables.UserSettingUpdatePreReleaseGTN)
            {
                lastGTNBuildDate =
                    new DateTimeOffset(dateTime: gtnReleasesApiResponse[0].PublishedAt);
            }
            else
            {
                for (int i = 0; i < gtnReleasesApiResponse.Length; i++)
                {
                    if ((bool)!gtnReleasesApiResponse[i].Prerelease)
                    {
                        lastGTNBuildDate =
                            new DateTimeOffset(
                                dateTime: gtnReleasesApiResponse[i].PublishedAt);
                    }
                }
            }

            // Calculate the number of days between lastGTNBuild and January 1, 2000
            double daysDifference = (lastGTNBuildDate - january1st2000).TotalDays;

            // Add some logic to return 0 for build number if there's no result. While negative 73k isn't likely to be a problem I don't feel like experimenting.
            lastGTNBuildOnline =
                Math.Max(val1: 0, val2: (int)Math.Floor(d: daysDifference));
        }
        else
        {
            HelperVariables.OperationAPIReturnedOKResponse = false;
            CustomMessageBox customMessageBox = new(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_Helper_WarningGTNVerAPIResponse") +
                      response_GTNVersionQuery.StatusCode,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                    captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption
                       .Warning.ToString()),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
            customMessageBox.ShowDialog();
        }

        return lastGTNBuildOnline;
    }


    /// <summary>
    ///     Checks for new versions of GTN and ExifTool.
    /// </summary>
    [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
    internal static async Task CheckForNewVersions()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        // check when the last polling took place
        long nowUnixTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        long lastCheckUnixTime = 0;

        string strLastOnlineVersionCheck =
            HelperDataApplicationSettings.DataReadSQLiteSettings(
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

        FrmMainApp.Logger.Trace(message: "nowUnixTime > lastCheckUnixTime:" +
                                         (nowUnixTime - lastCheckUnixTime));
        int checkUpdateVal = 604800; //604800 is a week's worth of seconds
        #if DEBUG
        checkUpdateVal = 86400; // 86400 is a day's worth of seconds
        //checkUpdateVal = 1; 
        #endif

        if (nowUnixTime > lastCheckUnixTime + checkUpdateVal ||
            strLastOnlineVersionCheck == null)
        {
            FrmMainApp.Logger.Trace(message: "Checking for new versions.");

            // get current & newest exiftool version -- do this here at the end so it doesn't hold up the process
            ///////////////

            string exiftoolCmd = "-ver";
            await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                                         frmMainAppInstance: null,
                                                         initiator:
                                                         "GenericCheckForNewVersions");
            HelperVariables.CurrentExifToolVersionCloud =
                API_ExifGetExifToolVersionFromWeb();

            FrmMainApp.Logger.Trace(message: "currentExifToolVersionLocal: " +
                                             HelperVariables
                                                .CurrentExifToolVersionLocal +
                                             " / newestExifToolVersionOnline: " +
                                             HelperVariables.CurrentExifToolVersionCloud);

            // if cloud version is newer and there isn't an identically named file in Roaming then download
            if (HelperVariables.CurrentExifToolVersionCloud >
                HelperVariables.CurrentExifToolVersionLocal &&
                !File.Exists(path: Path.Combine(path1: HelperVariables.UserDataFolderPath,
                                                path2:
                                                $"exiftool-{HelperVariables.CurrentExifToolVersionCloud}.zip")))
            {
                FrmMainApp.Logger.Trace(
                    message: "Downloading newest exifTool version from the cloud. " +
                             HelperVariables.CurrentExifToolVersionCloud.ToString(
                                 provider: CultureInfo
                                    .InvariantCulture));

                // get new version from online - it will get "armed" on app exit.
                await DownloadCurrentExifToolVersion(
                    version: HelperVariables.CurrentExifToolVersionCloud.ToString(
                        provider: CultureInfo.InvariantCulture));
            }

            // Done w ExifTool and move on to checking GTN stuff
            // current version may be something like "0.5.8251.40825"
            // Assembly.GetExecutingAssembly().GetName().Version.Build is just "8251"
            string currentGTNVersionBuildMajor = Assembly.GetExecutingAssembly()
                                                         .GetName()
                                                         .Version.Major
                                                         .ToString(
                                                              provider: CultureInfo
                                                                 .InvariantCulture);

            string currentGTNVersionBuildMinor = Assembly.GetExecutingAssembly()
                                                         .GetName()
                                                         .Version.Minor
                                                         .ToString(
                                                              provider: CultureInfo
                                                                 .InvariantCulture);

            int currentGTNVersionBuild = Assembly.GetExecutingAssembly()
                                                 .GetName()
                                                 .Version.Build;

            HelperVariables.OperationAPIReturnedOKResponse = true;
            int newestOnlineGTNVersion = 0;
            try
            {
                newestOnlineGTNVersion = API_GenericGetGTNVersionFromWeb();
            }
            catch
            {
                // ignore
            }

            CreateJsonForUpdate(localMajor: currentGTNVersionBuildMajor,
                                localMinor: currentGTNVersionBuildMinor,
                                remoteBuild: newestOnlineGTNVersion.ToString(
                                    provider: CultureInfo.InvariantCulture));

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

    /// <summary>
    ///     Attempts to download the current ExifTool version and extracts it too.
    /// </summary>
    /// <param name="version">The (most current) version to download.</param>
    /// <returns></returns>
    private static async Task DownloadCurrentExifToolVersion(string version)
    {
        string remoteUri = $"https://exiftool.org/exiftool-{version}.zip";
        string zipPath = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                                      path2: $"exiftool-{version}.zip");
        try
        {
            HttpClient httpClient = new();
            using Stream stream = await httpClient.GetStreamAsync(requestUri: remoteUri);
            using FileStream fileStream = new(path: zipPath, mode: FileMode.Create);
            await stream.CopyToAsync(destination: fileStream);
            fileStream.Flush();
            fileStream.Close();
            ZipFile.ExtractToDirectory(
                sourceArchiveFileName: zipPath,
                destinationDirectoryName: HelperVariables.UserDataFolderPath);
        }
        catch
        {
            // ignore
        }
    }

    private static void CreateJsonForUpdate(string localMajor,
                                            string localMinor,
                                            string remoteBuild)
    {
        // Create JSON object
        var jsonObject = new
        {
            // don't care/can't query remote major and minor
            version = $"{localMajor}.{localMinor}.{remoteBuild}.0",
            url =
                $"https://github.com/nemethviktor/GeoTagNinja/releases/download/b{remoteBuild}/GeoTagNinja_Setup.msi",
            changelog =
                $"https://github.com/nemethviktor/GeoTagNinja/blob/b{remoteBuild}/changelog.md"
        };

        string jsonContent =
            JsonConvert.SerializeObject(value: jsonObject,
                                        formatting: Formatting.Indented);

        string updateJsonPath =
            Path.Combine(path1: HelperVariables.UserDataFolderPath,
                         path2: "updateJsonData.json");

        if (File.Exists(path: updateJsonPath))
        {
            File.Delete(path: updateJsonPath);
        }

        File.WriteAllText(path: updateJsonPath, contents: jsonContent);
    }
}