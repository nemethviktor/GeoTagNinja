using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using geoTagNinja;
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
    /// </summary>
    /// <returns>The version number of the latest GTN release</returns>
    private static GTNReleaseAPIResponse API_GenericGetGTNVersionFromWeb()
    {
        GTNReleaseAPIResponse returnVal = new();
        RestClient client = new(baseUrl: "https://api.github.com/")
        {
            // admittedly no idea how to do this w/o any auth (as it's not needed) but this appears to work.
            Authenticator = new HttpBasicAuthenticator(username: "demo", password: "demo")
        };
        RestRequest request_GTNVersionQuery = new(resource: "repos/nemethviktor/GeoTagNinja/releases");
        RestResponse response_GTNVersionQuery = client.ExecuteGet(request: request_GTNVersionQuery);
        if (response_GTNVersionQuery.StatusCode.ToString() == "OK")
        {
            HelperVariables.SApiOkay = true;
            JArray data = (JArray)JsonConvert.DeserializeObject(value: response_GTNVersionQuery.Content);
            GTNReleaseAPIResponse[] gtnReleasesApiResponse = GTNReleaseAPIResponse.FromJson(json: data.ToString());
            returnVal = gtnReleasesApiResponse[0]; // latest only
        }
        else
        {
            HelperVariables.SApiOkay = false;
            MessageBox.Show(text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGTNVerAPIResponse") +
                                  response_GTNVersionQuery.StatusCode,
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }

        return returnVal;
    }


    /// <summary>
    ///     Converts the API response from gitHub (to check GTN's newest version) to a DataTable
    ///     Actually the reason why this might be indicated as 0 references is because this doesn't run in Debug mode.
    /// </summary>
    /// <returns>A Datatable with (hopefully) one row of data containing the newest GTN version</returns>
    internal static DataTable DTFromAPI_GetGTNVersion()
    {
        DataTable dt_Return = new();
        dt_Return.Clear();
        dt_Return.Columns.Add(columnName: "version");

        string apiVersion = "";

        if (HelperVariables.SApiOkay)
        {
            GTNReleaseAPIResponse readJson_GTNVer = API_GenericGetGTNVersionFromWeb();
            if (readJson_GTNVer.attribute != null)
            {
                apiVersion = readJson_GTNVer.attribute;
            }
            // this will be a null value if Unauthorised, we'll ignore that.
        }

        if (HelperVariables.SApiOkay)
        {
            DataRow dr_ReturnRow = dt_Return.NewRow();
            dr_ReturnRow[columnName: "version"] = apiVersion;
            dt_Return.Rows.Add(row: dr_ReturnRow);
        }

        return dt_Return;
    }

    /// <summary>
    ///     Checks for new versions of GTN and eT.
    /// </summary>
    internal static void CheckForNewVersions()
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
            HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                                         frmMainAppInstance: null,
                                                         initiator: HelperExifExifToolOperator.INITIATOR.CHECK_4_NEW_VERSION);
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

                if (MessageBox.Show(
                        text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoNewExifToolVersionExists") +
                              newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture),
                        caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                        buttons: MessageBoxButtons.YesNo,
                        icon: MessageBoxIcon.Asterisk) ==
                    DialogResult.Yes)
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
            int currentGTNVersionBuild = Assembly.GetExecutingAssembly()
                .GetName()
                .Version.Build;

            HelperVariables.SApiOkay = true;
            DataTable dtApigtnVersion = DTFromAPI_GetGTNVersion();
            // newest may be something like "v0.5.8251"
            try // could be offline etc
            {
                string newestGTNVersionFull = dtApigtnVersion.Rows[index: 0][columnName: "version"]
                    .ToString()
                    .Replace(oldValue: "v", newValue: "");

                int newestGTNVersion = 0;

                int.TryParse(s: newestGTNVersionFull.Split('.')
                                 .Last(), result: out newestGTNVersion);

                if (newestGTNVersion > currentGTNVersionBuild)
                {
                    if (MessageBox.Show(
                            text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoNewGTNVersionExists") +
                                  newestGTNVersion,
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.YesNo,
                            icon: MessageBoxIcon.Asterisk) ==
                        DialogResult.Yes)
                    {
                        Process.Start(fileName: "https://github.com/nemethviktor/GeoTagNinja/releases/download/" + dtApigtnVersion.Rows[index: 0][columnName: "version"] + "/GeoTagNinja_Setup.msi");
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
            catch
            {
                // nothing
            }
        }
        else
        {
            FrmMainApp.Logger.Trace(message: "Not checking for new versions.");
        }
    }

    internal static class HelperAPIExifToolVersionChecker
    {
        /// <summary>
        ///     Checks for new versions of GTN and eT.
        /// </summary>
        internal static void CheckForNewVersions()
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
                HelperDataApplicationSettings.
                    // write back to SQL so it doesn't remain blank
                    DataWriteSQLiteSettings(
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
                HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                                             frmMainAppInstance: null,
                                                             initiator: HelperExifExifToolOperator.INITIATOR.CHECK_4_NEW_VERSION);
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
                    HelperDataApplicationSettings.
                        // write current to SQL
                        DataWriteSQLiteSettings(
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
                    HelperDataApplicationSettings.
                        // write current to SQL
                        DataWriteSQLiteSettings(
                            tableName: "settings",
                            settingTabPage: "generic",
                            settingId: "exifToolVer",
                            settingValue: newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture)
                        );

                    if (MessageBox.Show(
                            text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoNewExifToolVersionExists") +
                                  newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture),
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.YesNo,
                            icon: MessageBoxIcon.Asterisk) ==
                        DialogResult.Yes)
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
                int currentGTNVersionBuild = Assembly.GetExecutingAssembly()
                    .GetName()
                    .Version.Build;

                HelperVariables.SApiOkay = true;
                DataTable DtAPIGTNVersion = DTFromAPI_GetGTNVersion();
                // newest may be something like "v0.5.8251"
                try // could be offline etc
                {
                    string newestGTNVersionFull = DtAPIGTNVersion.Rows[index: 0][columnName: "version"]
                        .ToString()
                        .Replace(oldValue: "v", newValue: "");

                    int newestGTNVersion = 0;

                    int.TryParse(s: newestGTNVersionFull.Split('.')
                                     .Last(), result: out newestGTNVersion);

                    if (newestGTNVersion > currentGTNVersionBuild)
                    {
                        if (MessageBox.Show(
                                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoNewGTNVersionExists") +
                                      newestGTNVersion,
                                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                                buttons: MessageBoxButtons.YesNo,
                                icon: MessageBoxIcon.Asterisk) ==
                            DialogResult.Yes)
                        {
                            Process.Start(fileName: "https://github.com/nemethviktor/GeoTagNinja/releases/download/" + DtAPIGTNVersion.Rows[index: 0][columnName: "version"] + "/GeoTagNinja_Setup.msi");
                        }
                    }

                    HelperDataApplicationSettings.

                        // write back to SQL
                        DataWriteSQLiteSettings(
                            tableName: "settings",
                            settingTabPage: "generic",
                            settingId: "onlineVersionCheckDate",
                            settingValue: nowUnixTime.ToString()
                        );
                }
                catch
                {
                    // nothing
                }
            }
            else
            {
                FrmMainApp.Logger.Trace(message: "Not checking for new versions.");
            }
        }
    }
}