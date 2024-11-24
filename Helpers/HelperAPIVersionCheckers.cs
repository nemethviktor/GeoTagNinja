using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using geoTagNinja;
using GeoTagNinja.Model;
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

        // ReSharper disable once InconsistentNaming
        RestRequest request_GTNVersionQuery =
            new(resource: "repos/nemethviktor/GeoTagNinja/releases");

        // ReSharper disable once InconsistentNaming
        RestResponse response_GTNVersionQuery =
            client.ExecuteGet(request: request_GTNVersionQuery);

        // ReSharper disable once InconsistentNaming
        int lastGTNBuildOnline = 0;
        if (response_GTNVersionQuery.StatusCode.ToString() == "OK")
        {
            HelperVariables.OperationAPIReturnedOKResponse = true;
            JArray data =
                (JArray)JsonConvert.DeserializeObject(
                    value: response_GTNVersionQuery.Content);
            GTNReleaseAPIResponse[] gtnReleasesApiResponse =
                GTNReleaseAPIResponse.FromJson(json: data.ToString());

            // ReSharper disable once InconsistentNaming
            int lastGTNBuildInt = 0;
            Regex rgx = new(pattern: "[^0-9]");

            if (HelperVariables.UserSettingUpdatePreReleaseGTN)
            {
                bool _ = int.TryParse(s: rgx.Replace(input: gtnReleasesApiResponse[0].attribute, replacement: ""),
                    result: out lastGTNBuildInt);
            }
            else
            {
                for (int i = 0; i < gtnReleasesApiResponse.Length; i++)
                {
                    if ((bool)!gtnReleasesApiResponse[i].Prerelease)
                    {
                        bool _ = int.TryParse(
                            s: rgx.Replace(input: gtnReleasesApiResponse[i].attribute, replacement: ""),
                            result: out lastGTNBuildInt);
                        ;
                    }
                }
            }

            // Add some logic to return 0 for build number if there's no result. While negative 73k isn't likely to be a problem I don't feel like experimenting.
            lastGTNBuildOnline =
                Math.Max(val1: 0, val2: lastGTNBuildInt);
        }
        else
        {
            HelperVariables.OperationAPIReturnedOKResponse = false;
            CustomMessageBox customMessageBox = new(
                text: HelperControlAndMessageBoxHandling.ReturnControlText(
                          controlName: "mbx_Helper_WarningGTNVerAPIResponse",
                          fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox) +
                      response_GTNVersionQuery.StatusCode,
                caption: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: HelperControlAndMessageBoxHandling.MessageBoxCaption
                                                                   .Warning.ToString(),
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBoxCaption),
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
        FrmMainApp.Log.Info(message: "Starting");

        // check when the last polling took place
        long nowUnixTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        long lastCheckUnixTime = 0;

        string strLastOnlineVersionCheck =
            HelperDataApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings,
                settingTabPage: "generic",
                settingId: "onlineVersionCheckDate"
            );

        if (strLastOnlineVersionCheck == null)
        {
            lastCheckUnixTime = nowUnixTime;
            // write back to SQL so it doesn't remain blank
            List<AppSettingContainer> settingsToWrite = new()
            {
                new AppSettingContainer
                {
                    TableName = "settings",
                    SettingTabPage = "generic",
                    SettingId = "onlineVersionCheckDate",
                    SettingValue = nowUnixTime.ToString(provider: CultureInfo.InvariantCulture)
                }
            };
            HelperDataApplicationSettings.DataWriteSQLiteSettings(settingsToWrite: settingsToWrite);
        }
        else
        {
            lastCheckUnixTime = long.Parse(s: strLastOnlineVersionCheck);
        }

        FrmMainApp.Log.Trace(message: $"nowUnixTime > lastCheckUnixTime:{nowUnixTime - lastCheckUnixTime}");
        int checkUpdateVal = 604800; //604800 is a week's worth of seconds
    #if DEBUG
        //checkUpdateVal = 86400; // 86400 is a day's worth of seconds
        checkUpdateVal = 1;
    #endif

        if (nowUnixTime > lastCheckUnixTime + checkUpdateVal ||
            strLastOnlineVersionCheck == null)
        {
            FrmMainApp.Log.Trace(message: "Checking for new versions.");

            // get current & newest exiftool version -- do this here at the end so it doesn't hold up the process
            ///////////////

            string exiftoolCmd = "-ver";
            await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                frmMainAppInstance: null,
                initiator:
                HelperGenericAncillaryListsArrays.ExifToolInititators
                                                 .GenericCheckForNewVersions);
            int CPUBitness = Environment.Is64BitOperatingSystem ? 64 : 32;
            HelperVariables.CurrentExifToolVersionCloud =
                API_ExifGetExifToolVersionFromWeb();

            FrmMainApp.Log.Trace(message: $"currentExifToolVersionLocal: {HelperVariables
               .CurrentExifToolVersionLocal} / newestExifToolVersionOnline: {HelperVariables.CurrentExifToolVersionCloud}");

            // if cloud version is newer and there isn't an identically named file in Roaming then download
            if (HelperVariables.CurrentExifToolVersionCloud >
                HelperVariables.CurrentExifToolVersionLocal &&
                !File.Exists(path: Path.Combine(path1: HelperVariables.UserDataFolderPath,
                    path2:
                    $"exiftool-{HelperVariables.CurrentExifToolVersionCloud}_{CPUBitness}.zip")))
            {
                FrmMainApp.Log.Trace(
                    message:
                    $"Downloading newest exifTool version from the cloud. {HelperVariables.CurrentExifToolVersionCloud.ToString(
                        provider: CultureInfo
                           .InvariantCulture)}");

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
            List<AppSettingContainer> settingsToWrite = new()
            {
                new AppSettingContainer
                {
                    TableName = "settings",
                    SettingTabPage = "generic",
                    SettingId = "onlineVersionCheckDate",
                    SettingValue = nowUnixTime.ToString(provider: CultureInfo.InvariantCulture)
                }
            };
            HelperDataApplicationSettings.DataWriteSQLiteSettings(settingsToWrite: settingsToWrite);
        }
        else
        {
            FrmMainApp.Log.Trace(message: "Not checking for new versions.");
        }
    }

    /// <summary>
    ///     Attempts to download the current ExifTool version.
    /// </summary>
    /// <param name="version">The (most current) version to download.</param>
    /// <returns></returns>
    /// <see cref="FrmMainApp.FrmMainApp_FormClosing" />
    private static async Task DownloadCurrentExifToolVersion(string version)
    {
        // ReSharper disable once InconsistentNaming
        int CPUBitness = Environment.Is64BitOperatingSystem ? 64 : 32;
        string remoteUri = $"https://exiftool.org/exiftool-{version}_{CPUBitness}.zip";
        string zipPath = Path.Combine(path1: HelperVariables.UserDataFolderPath,
            path2: $"exiftool-{version}_{CPUBitness}.zip");
        try
        {
            HttpClient httpClient = new();
            using Stream stream = await httpClient.GetStreamAsync(requestUri: remoteUri);
            using FileStream fileStream = new(path: zipPath, mode: FileMode.Create);
            await stream.CopyToAsync(destination: fileStream);
            fileStream.Flush();
            fileStream.Close();
            HelperVariables.ExifToolExePathRoamingTemp = zipPath;
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
                $"https://github.com/nemethviktor/GeoTagNinja/blob/b{remoteBuild}/changeLog.md"
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