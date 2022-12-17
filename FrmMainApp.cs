using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using geoTagNinja;
using GeoTagNinja.Model;
using GeoTagNinja.Properties;
using Microsoft.Web.WebView2.Core;
using NLog;
using NLog.Config;
using NLog.Targets;
using TimeZoneConverter;
using static System.Environment;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
namespace GeoTagNinja;

public partial class FrmMainApp : Form
{
    /// <summary>
    ///     These two make the elements of the main listview accessible to other classes.
    /// </summary>
    public ListView.ListViewItemCollection ListViewItems => lvw_FileList.Items;

    public ListView.ColumnHeaderCollection ListViewColumnHeaders => lvw_FileList.Columns;

    /// <summary>
    ///     Returns the currently set application language for localization.
    /// </summary>
    public string AppLanguage => _AppLanguage;


    private DirectoryElementCollection _directoryElements = new DirectoryElementCollection();
    /// <summary>
    ///     Returns the list of elements in the currently opened directory.
    /// </summary>
    public DirectoryElementCollection DirectoryElements => _directoryElements;

    #region Variables

    internal static readonly string ResourcesFolderPath = Path.Combine(path1: AppDomain.CurrentDomain.BaseDirectory, path2: "Resources");
    internal static readonly string UserDataFolderPath = Path.Combine(path1: GetFolderPath(folder: SpecialFolder.ApplicationData), path2: "GeoTagNinja");
    internal const string DoubleQuote = "\"";

    internal const string ParentFolder = "..";
    internal static string LatCoordinate;
    internal static string LngCoordinate;

    internal static DataTable DtLangaugeLabels;
    internal static DataTable DtObjectNames;
    internal static DataTable DtObjectTagNamesIn;
    internal static DataTable DtObjectTagNamesOut;
    internal static string FolderName;
    internal static string _AppLanguage = "english"; // default to english

    internal static string ShowLocToMapDialogChoice = "default";
    internal FrmSettings FrmSettings;
    internal FrmEditFileData FrmEditFileData;
    internal FrmImportGpx FrmImportGpx;


    // this is for copy-paste
    internal static DataTable DtFileDataCopyPool;
    internal static DataTable DtFileDataPastePool;
    internal static string FileDateCopySourceFileNameWithPath;

    // these are for queueing files up
    internal static DataTable DtFileDataToWriteStage1PreQueue;
    internal static DataTable DtFileDataToWriteStage2QueuePendingSave;
    internal static DataTable DtFileDataToWriteStage3ReadyToWrite;

    // just to keep myself sane here....
    // the "queue" tables have the following structure:
    // (columnName: "fileNameWithoutPath");
    // (columnName: "settingId");
    // (columnName: "settingValue");

    // this is for checking if files need to be re-parsed.
    //internal static DataTable DtFilesSeenInThisSession;
    internal static DataTable DtFileDataSeenInThisSession;

    internal static bool RemoveGeoDataIsRunning;

    // these are for storing the inital values of TakenDate and CreateDate. Needed for TimeShift.
    internal static DataTable DtOriginalTakenDate;
    internal static DataTable DtOriginalCreateDate;

    #endregion


    #region Form/App Related

    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     This is the main Form for the app. This particular section handles the initialisation of the form and loading
    ///     various defaults.
    /// </summary>
    public FrmMainApp()
    {
        #region Logging

        // Set up logging

        LoggingConfiguration config = new();
        string logFileLocation = Path.Combine(path1: UserDataFolderPath, path2: "logfile.txt");
        File.Delete(path: logFileLocation);
        FileTarget logfile = new(name: "logfile") { FileName = logFileLocation };
        #if(DEBUG)
        config.AddRule(minLevel: LogLevel.Trace, maxLevel: LogLevel.Fatal, target: logfile);
        #else
        config.AddRule(minLevel: LogLevel.Info, maxLevel: LogLevel.Fatal, target: logfile);
        #endif

        logfile.Layout = @"${longdate}|${level:uppercase=true}|${callsite:includeNamespace=false:includeSourcePath=false:methodName=true}|${message:withexception=true}";
        ConsoleTarget logconsole = new(name: "logconsole");

        config.AddRule(minLevel: LogLevel.Info, maxLevel: LogLevel.Fatal, target: logconsole);

        // Apply config           
        LogManager.Configuration = config;

        #endregion

        Logger.Info(message: "Starting");

        AppStartupCreateDataBaseFile();
        AppStartupWriteDefaultSettings();
        AppStartupReadObjectNames();
        AppStartupApplyDefaults();
        AppStartupCheckWebView2();
        AppStartupInitializeComponentFrmMainApp();
        AppStartupEnableDoubleBuffering();

        FormClosing += FrmMainApp_FormClosing;

        HelperStatic.GenericCreateDataTables();
        Logger.Info(message: "Done");
    }

    /// <summary>
    ///     Handles the initial loading - adds various elements and ensures the app functions.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void FrmMainApp_Load(object sender,
                                       EventArgs e)
    {
        Logger.Info(message: "Starting");
        // icon

        Logger.Trace(message: "Setting Icon");
        Icon = Resources.AppIcon;

        // clear both tables, just in case + generic cleanup
        try
        {
            Logger.Debug(message: "Clear DtFileDataToWriteStage1PreQueue");
            DtFileDataToWriteStage1PreQueue.Rows.Clear();

            Logger.Debug(message: "Clear DtFileDataToWriteStage3ReadyToWrite");
            DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorClearingFileDataQTables") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        try
        {
            HelperStatic.FsoCleanUpUserFolder();
        }
        catch (Exception ex)
        {
            // not really fatal
            Logger.Error(message: "Error: " + ex.Message);
        }

        // Setup the List View
        try
        {
            lvw_FileList.ReadAndApplySetting(AppLanguage, DtObjectNames);
        }
        catch (Exception ex)
        {
            Logger.Error(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorResizingColumns") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // can't log inside.
        Logger.Debug(message: "Run CoreWebView2InitializationCompleted");
        wbv_MapArea.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;

        AppSetupInitialiseStartupFolder();

        // initialise webView2
        await InitialiseWebView();

        // assign labels to objects
        AppStartupAssignLabelsToObjects();

        // load lvwFileList
        lvwFileList_LoadOrUpdate();

        Logger.Trace(message: "Assign 'Enter' Key behaviour to tbx_lng");
        tbx_lng.KeyPress += (sndr,
                             ev) =>
        {
            if (ev.KeyChar.Equals(obj: (char)13))
            {
                btn_NavigateMapGo.PerformClick();
                ev.Handled = true; // suppress default handling
            }
        };

        AppStartupPullLastLatLngFromSettings();

        NavigateMapGo();

        await HelperStatic.GenericCheckForNewVersions();

        Logger.Info(message: "Done.");
    }


    /// <summary>
    ///     Initialises the map in the app and browses to the default or last-used location.
    /// </summary>
    /// <returns></returns>
    private async Task InitialiseWebView()
    {
        Logger.Debug(message: "Starting");

        try
        {
            // silly thing dumps the folder by default right into Program Files where it can't write further due to permission issues
            // need to move it elsewhere.
            Logger.Trace(message: "await CoreWebView2Environment");
            CoreWebView2Environment c2Wv = await CoreWebView2Environment.CreateAsync(browserExecutableFolder: null,
                                                                                     userDataFolder: Path.GetTempPath(),
                                                                                     options: new CoreWebView2EnvironmentOptions(additionalBrowserArguments: null, language: "en"));
            await wbv_MapArea.EnsureCoreWebView2Async(environment: c2Wv);
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewEnsureCoreWebView2Async") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        try
        {
            if (wbv_MapArea.CoreWebView2 != null)
            {
                Logger.Trace(message: "CoreWebView2.Settings.IsWebMessageEnabled");
                wbv_MapArea.CoreWebView2.Settings.IsWebMessageEnabled = true;
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewIsWebMessageEnabled") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // read the "map.html" file.
        string htmlCode = "";
        try
        {
            Logger.Trace(message: "Read map.html file");
            htmlCode = File.ReadAllText(path: Path.Combine(path1: ResourcesFolderPath, path2: "map.html"));
            Logger.Trace(message: "Read map.html file OK");
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Read map.html file - Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewReadHTMLFile") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        try
        {
            Logger.Trace(message: "ParseCoordsFromHTMLFile");
            string strLatCoordinate = tbx_lat.Text.Replace(oldChar: ',', newChar: '.');
            string strLngCoordinate = tbx_lng.Text.Replace(oldChar: ',', newChar: '.');

            double parsedLat;
            double parsedLng;
            if (double.TryParse(s: strLatCoordinate, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strLngCoordinate, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
            {
                LatCoordinate = strLatCoordinate;
                LngCoordinate = strLngCoordinate;
                Logger.Trace(message: "ParseCoordsFromHTMLFile OK");
                Logger.Trace(message: "ParseCoordsFromHTMLFile - LatCoordinate: " + strLatCoordinate + " - LngCoordinate: " + strLngCoordinate);
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewParseCoordsFromHTMLFile") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // replace hard-coded values in the html code
        try
        {
            if (HelperStatic.SArcGisApiKey == null)
            {
                Logger.Trace(message: "Replace hard-coded values in the html code - SArcGisApiKey is null");
                HelperStatic.SArcGisApiKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();
                Logger.Trace(message: "Replace hard-coded values in the html code - SArcGisApiKey obtained from SQLite OK");
            }

            htmlCode = htmlCode.Replace(oldValue: "yourApiKey", newValue: HelperStatic.SArcGisApiKey);
            htmlCode = htmlCode.Replace(oldValue: "replaceLat", newValue: LatCoordinate);
            htmlCode = htmlCode.Replace(oldValue: "replaceLng", newValue: LngCoordinate);
            Logger.Trace(message: "Replace hard-coded values in the html code - Replacing Data OK");
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewReplaceStringInHTMLFile") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // show the decoded location on the map
        try
        {
            Logger.Trace(message: "NavigateToString");
            wbv_MapArea.NavigateToString(htmlContent: htmlCode);
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewNavigateToStringInHTMLFile") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        try
        {
            Logger.Trace(message: "wbv_MapArea.WebMessageReceived");
            wbv_MapArea.WebMessageReceived += wbv_MapArea_WebMessageReceived;
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error:" + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewWebMessageReceived") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    /// <summary>
    ///     When the app closes we want to make sure there's nothing in the write-queue.
    ///     ...once that's dealt with we write the details of the app layout (e.g. column widths) to sqlite.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void FrmMainApp_FormClosing(object sender,
                                              FormClosingEventArgs e)
    {
        Logger.Debug(message: "Starting");

        Logger.Trace(message: "DtFileDataToWriteStage3ReadyToWrite.Rows.Count:" + DtFileDataToWriteStage3ReadyToWrite.Rows.Count);
        // check if there is any data in the write-Q
        if (DtFileDataToWriteStage3ReadyToWrite.Rows.Count > 0)
        {
            DialogResult dialogResult = MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_QuestionFileQIsNotEmpty"), caption: "Info", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                while (HelperStatic.FileListBeingUpdated || HelperStatic.FilesAreBeingSaved)
                {
                    await Task.Delay(millisecondsDelay: 10);
                }

                Logger.Debug(message: "Starting ExifWriteExifToFile");
                await HelperStatic.ExifWriteExifToFile();

                // shouldn't be needed but just in case.
                HelperStatic.FilesAreBeingSaved = false;

                Logger.Debug(message: "Finished ExifWriteExifToFile");
            }
            else if (dialogResult == DialogResult.No)
            {
                Logger.Debug(message: "User Chose not to Save.");
                DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
            }
        }

        // Write column widths to db
        Logger.Trace(message: "Write column widths to db");
        lvw_FileList.PersistSettings();

        // Write lat/long for future reference to db
        Logger.Trace(message: "Write lat/long for future reference to db [lat/lng]: " + tbx_lat.Text + "/" + tbx_lng.Text);
        HelperStatic.DataWriteSQLiteSettings(
            tableName: "settings",
            settingTabPage: "generic",
            settingId: "lastLat",
            settingValue: tbx_lat.Text
        );
        HelperStatic.DataWriteSQLiteSettings(
            tableName: "settings",
            settingTabPage: "generic",
            settingId: "lastLng",
            settingValue: tbx_lng.Text
        );

        // clean up
        Logger.Trace(message: "Set pbx_imagePreview.Image = null");
        pbx_imagePreview.Image = null; // unlocks files. theoretically.

        HelperStatic.FsoCleanUpUserFolder();
    }

    #endregion


    #region Map Stuff

    [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
    public class MapGpsCoordinates
    {
        public double lat { get; set; } // note to self: don't allow ReSharper to rename these.
        public double lng { get; set; } // note to self: don't allow ReSharper to rename these.
    }

    /// <summary>
    ///     Provides an interaction layer between the map and the app. The reason why we're using string instead of proper
    ///     numbers
    ///     ... is that the API only deals with English-formatted numbers whereas we can't force that necessarily on the user
    ///     if they have
    ///     ... other Culture setting.
    ///     ... Also if the user zooms out too much they can click on a map-area (coordinate) that's not "real" so we are
    ///     dealing with that in this code.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void wbv_MapArea_WebMessageReceived(object sender,
                                                CoreWebView2WebMessageReceivedEventArgs e)
    {
        string jsonString = e.WebMessageAsJson;

        MapGpsCoordinates mapGpsCoordinates =
            JsonSerializer.Deserialize<MapGpsCoordinates>(json: jsonString);
        string strLat = mapGpsCoordinates?.lat.ToString(provider: CultureInfo.InvariantCulture);
        string strLng = mapGpsCoordinates?.lng.ToString(provider: CultureInfo.InvariantCulture);
        double.TryParse(s: strLat, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double dblLat); // trust me i hate this f...king culture thing as much as possible...
        double.TryParse(s: strLng, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double dblLng); // trust me i hate this f...king culture thing as much as possible...
        // if the user zooms out too much they can encounter an "unreal" coordinate.

        if (dblLng < -180)
        {
            dblLng = 180 - Math.Abs(value: dblLng) % 180;
        }
        else if (dblLng > 180)
        {
            dblLng = Math.Abs(value: dblLng) % 180;
        }

        tbx_lat.Text = Math.Round(value: dblLat, digits: 6)
            .ToString(provider: CultureInfo.InvariantCulture);
        tbx_lng.Text = Math.Round(value: dblLng, digits: 6)
            .ToString(provider: CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Needed for the proper functioning of webview2
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void webView_CoreWebView2InitializationCompleted(object sender,
                                                             CoreWebView2InitializationCompletedEventArgs e)
    { }

    /// <summary>
    ///     Handles the clicking on Go button
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_NavigateMapGo_Click(object sender,
                                         EventArgs e)
    {
        HelperStatic.HsMapMarkers.Clear();
        HelperStatic.HsMapMarkers.Add(item: (tbx_lat.Text.Replace(oldChar: ',', newChar: '.'), tbx_lng.Text.Replace(oldChar: ',', newChar: '.')));
        NavigateMapGo();
    }

    /// <summary>
    ///     Handles the clicking on "ToFile" button. See comments above re: why we're using strings (culture-related issue)
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void btn_loctToFile_Click(object sender,
                                            EventArgs e)
    {
        string strGpsLatitude = tbx_lat.Text.Replace(oldChar: ',', newChar: '.');
        string strGpsLongitude = tbx_lng.Text.Replace(oldChar: ',', newChar: '.');
        double parsedLat;
        double parsedLng;
        GeoResponseToponomy readJsonToponomy = new();
        GeoResponseAltitude readJsonAltitude = new();

        // lat/long gets written regardless of update-toponomy-choice
        if (double.TryParse(s: strGpsLatitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strGpsLongitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
        {
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                {
                    // don't do folders...
                    string fileNameWithPath = Path.Combine(path1: FolderName, path2: lvi.Text);
                    string fileNameWithoutPath = lvi.Text;
                    if (File.Exists(path: fileNameWithPath))
                    {
                        // check it's not in the read-queue.
                        while (HelperStatic.GenericLockCheckLockFile(fileNameWithoutPath: fileNameWithoutPath))
                        {
                            await Task.Delay(millisecondsDelay: 10);
                        }

                        // Latitude
                        HelperStatic.GenericUpdateAddToDataTable(
                            dt: DtFileDataToWriteStage3ReadyToWrite,
                            fileNameWithoutPath: fileNameWithoutPath,
                            settingId: "GPSLatitude",
                            settingValue: strGpsLatitude
                        );

                        // Longitude
                        HelperStatic.GenericUpdateAddToDataTable(
                            dt: DtFileDataToWriteStage3ReadyToWrite,
                            fileNameWithoutPath: fileNameWithoutPath,
                            settingId: "GPSLongitude",
                            settingValue: strGpsLongitude
                        );
                    }
                }
            }
        }

        if (double.TryParse(s: strGpsLatitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strGpsLongitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
        {
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                if (!ShowLocToMapDialogChoice.Contains(value: "_remember"))
                {
                    // via https://stackoverflow.com/a/17385937/3968494
                    ShowLocToMapDialogChoice = HelperStatic.GenericCheckboxDialog.ShowDialogWithCheckBox(
                        labelText: HelperStatic.DataReadSQLiteObjectText(
                            languageName: AppLanguage,
                            objectType: "Label",
                            objectName: "lbl_QuestionAddToponomy"
                        ),
                        caption: "Info",
                        checkboxText: HelperStatic.DataReadSQLiteObjectText(
                            languageName: AppLanguage,
                            objectType: "CheckBox",
                            objectName: "ckb_QuestionAddToponomyDontAskAgain"
                        ),
                        returnCheckboxText: "_remember",
                        button1Text: HelperStatic.DataReadSQLiteObjectText(
                            languageName: AppLanguage,
                            objectType: "Button",
                            objectName: "btn_Yes"
                        ),
                        returnButton1Text: "yes",
                        button2Text: HelperStatic.DataReadSQLiteObjectText(
                            languageName: AppLanguage,
                            objectType: "Button",
                            objectName: "btn_No"
                        ),
                        returnButton2Text: "no"
                    );
                }

                if (ShowLocToMapDialogChoice != "default") // basically user can alt+f4 from the box, which is dumb but nonetheless would break the code.
                {
                    HelperStatic.FileListBeingUpdated = true;
                    foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                    {
                        // don't do folders...
                        string fileNameWithPath = Path.Combine(path1: FolderName, path2: lvi.Text);
                        string fileNameWithoutPath = Path.GetFileName(path: fileNameWithPath);
                        if (File.Exists(path: fileNameWithPath))
                        {
                            while (HelperStatic.GenericLockCheckLockFile(fileNameWithoutPath: fileNameWithoutPath))
                            {
                                await Task.Delay(millisecondsDelay: 10);
                            }

                            DataTable dtToponomy = new();
                            DataTable dtAltitude = new();
                            if (ShowLocToMapDialogChoice.Contains(value: "yes"))
                            {
                                lvwUpdateTagsFromWeb(strGpsLatitude: strGpsLatitude, strGpsLongitude: strGpsLongitude, lvi: lvi);
                            }
                        }

                        await HelperStatic.LwvUpdateRowFromDTWriteStage3ReadyToWrite(lvi: lvi);
                    }

                    HelperStatic.FileListBeingUpdated = false;
                }
            }
        }

        // Not logging this.
        HelperStatic.LvwCountItemsWithGeoData();
    }

    /// <summary>
    ///     Handles the navigation to a coordinate on the map. Replaces hard-coded values w/ user-provided ones
    ///     ... and executes the navigation action.
    /// </summary>
    private void NavigateMapGo()
    {
        Logger.Debug(message: "Starting");

        string htmlCode = "";
        HelperStatic.HtmlAddMarker = "";
        double? dblMinLat = default;
        double? dblMinLng = default;
        double? dblMaxLat = default;
        double? dblMaxLng = default;

        // lazy
        string strLatCoordinate = "0";
        string strLngCoordinate = "0";

        try
        {
            htmlCode = File.ReadAllText(path: Path.Combine(path1: ResourcesFolderPath, path2: "map.html"));
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "ReadAllText map.html Error:" + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorNavigateMapGoHTMLCode") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        if (HelperStatic.SArcGisApiKey == null)
        {
            HelperStatic.SArcGisApiKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();
        }

        Logger.Trace(message: "HelperStatic.SArcGisApiKey == null: " + (HelperStatic.SArcGisApiKey == null));

        if (HelperStatic.HsMapMarkers.Count > 0)
        {
            foreach ((string strLat, string strLng) locationCoord in HelperStatic.HsMapMarkers)
            {
                HelperStatic.HtmlAddMarker += "var marker = L.marker([" + locationCoord.strLat + ", " + locationCoord.strLng + "]).addTo(map).openPopup();" + "\n";
                strLatCoordinate = locationCoord.strLat;
                strLngCoordinate = locationCoord.strLng;

                // set scene for mix/max so map zoom can be set automatically
                {
                    if (dblMinLat == null)
                    {
                        dblMinLat = double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture);
                        dblMaxLat = dblMinLat;
                    }

                    if (double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture) < dblMinLat)
                    {
                        dblMinLat = double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture);
                    }

                    if (double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture) > dblMaxLat)
                    {
                        dblMaxLat = double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture);
                    }

                    if (dblMinLng == null)
                    {
                        dblMinLng = double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture);
                        dblMaxLng = dblMinLng;
                    }

                    if (double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture) < dblMinLng)
                    {
                        dblMinLng = double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture);
                    }

                    if (double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture) > dblMaxLng)
                    {
                        dblMaxLng = double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture);
                    }

                    HelperStatic.MinLat = dblMinLat;
                    HelperStatic.MinLng = dblMinLng;
                    HelperStatic.MaxLat = dblMaxLat;
                    HelperStatic.MaxLng = dblMaxLng;

                    Logger.Trace(message: "Added marker: strLatCoordinate: " + strLatCoordinate + " / strLngCoordinate:" + strLngCoordinate);
                }
            }
        }

        Logger.Trace(message: "Added " + HelperStatic.HsMapMarkers.Count + " map markers.");
        Logger.Trace(message: "Replacing Hard-Coded Values in HTML.");

        if (HelperStatic.HsMapMarkers.Count > 0)
        {
            HelperStatic.LastLat = double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture);
            HelperStatic.LastLng = double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture);

            htmlCode = htmlCode.Replace(oldValue: "{ HTMLAddMarker }", newValue: HelperStatic.HtmlAddMarker);
        }
        else
        {
            htmlCode = htmlCode.Replace(oldValue: "{ HTMLAddMarker }", newValue: "");
        }

        htmlCode = htmlCode.Replace(oldValue: "replaceLat", newValue: HelperStatic.LastLat.ToString().Replace(oldChar: ',', newChar: '.'));
        htmlCode = htmlCode.Replace(oldValue: "replaceLng", newValue: HelperStatic.LastLng.ToString().Replace(oldChar: ',', newChar: '.'));

        htmlCode = htmlCode.Replace(oldValue: "replaceMinLat", newValue: HelperStatic.MinLat.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));
        htmlCode = htmlCode.Replace(oldValue: "replaceMinLng", newValue: HelperStatic.MinLng.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));
        htmlCode = htmlCode.Replace(oldValue: "replaceMaxLat", newValue: HelperStatic.MaxLat.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));
        htmlCode = htmlCode.Replace(oldValue: "replaceMaxLng", newValue: HelperStatic.MaxLng.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));

        htmlCode = htmlCode.Replace(oldValue: "yourApiKey", newValue: HelperStatic.SArcGisApiKey);
        Logger.Trace(message: "Replacing Hard-Coded Values in HTML. - OK");

        // can't log inside.
        Logger.Trace(message: "Calling wbv_MapArea.NavigateToString");
        wbv_MapArea.NavigateToString(htmlContent: htmlCode);
        Logger.Trace(message: "Calling wbv_MapArea.NavigateToString - OK");
    }

    #endregion


    #region File

    /// <summary>
    ///     Handles the tmi_File_SaveAll_Click event -> triggers the file-save process
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void tmi_File_SaveAll_Click(object sender,
                                              EventArgs e)
    {
        // i think having an Item active can cause a lock on it
        while (HelperStatic.FileListBeingUpdated || HelperStatic.FilesAreBeingSaved)
        {
            await Task.Delay(millisecondsDelay: 10);
        }

        lvw_FileList.SelectedItems.Clear();
        // also the problem here is that the exiftoolAsync can still be running and locking the file.

        await HelperStatic.ExifWriteExifToFile();
        // shouldn't be needed but just in case.
        HelperStatic.FilesAreBeingSaved = false;
        DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
    }

    /// <summary>
    ///     Handles the tmi_File_EditFiles_Click event -> opens the File Edit Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_EditFiles_Click(object sender,
                                          EventArgs e)
    {
        if (lvw_FileList.SelectedItems.Count > 0)
        {
            Logger.Debug(message: "Starting");

            int fileCount = 0;

            Logger.Trace(message: "Trigger FrmEditFileData");
            FrmEditFileData = new FrmEditFileData();
            FrmEditFileData.lvw_FileListEditImages.Items.Clear();

            Logger.Trace(message: "Add Files To lvw_FileListEditImages");
            foreach (ListViewItem selectedItem in lvw_FileList.SelectedItems)
            {
                if (File.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: selectedItem.Text)))
                {
                    FolderName = tbx_FolderName.Text;
                    FrmEditFileData.lvw_FileListEditImages.Items.Add(text: selectedItem.Text);
                    fileCount++;
                    Logger.Trace(message: "Added " + selectedItem.Text);
                }
            }

            if (fileCount > 0)
            {
                Logger.Trace(message: "FrmEditFileData Get objectTexts");
                FrmEditFileData.Text = HelperStatic.DataReadSQLiteObjectText(
                    languageName: AppLanguage,
                    objectType: "Form",
                    objectName: "FrmEditFileData"
                );

                Logger.Trace(message: "FrmEditFileData ShowDialog");
                FrmEditFileData.ShowDialog();
            }
        }
    }

    /// <summary>
    ///     Handles the tmi_File_CopyGeoData_Click event -> triggers LwvCopyGeoData
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_CopyGeoData_Click(object sender,
                                            EventArgs e)
    {
        HelperStatic.LwvCopyGeoData();
    }

    /// <summary>
    ///     Handles the tmi_File_PasteGeoData_Click event -> triggers LwvPasteGeoData
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_PasteGeoData_Click(object sender,
                                             EventArgs e)
    {
        HelperStatic.LwvPasteGeoData();
    }

    /// <summary>
    ///     Handles the tmi_File_ImportGPX_Click event -> Brings up the FrmImportGpx to import track
    ///     files
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_ImportGPX_Click(object sender,
                                          EventArgs e)
    {
        FrmImportGpx = new FrmImportGpx();
        FrmImportGpx.Text = HelperStatic.DataReadSQLiteObjectText(
            languageName: AppLanguage,
            objectType: "Form",
            objectName: "FrmImportGpx"
        );
        FrmImportGpx.ShowDialog();
    }

    /// <summary>
    ///     Handles the tmi_File_Quit_Click event -> cleans the user-folder then quits the app
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_Quit_Click(object sender,
                                     EventArgs e)
    {
        HelperStatic.FsoCleanUpUserFolder();
        Application.Exit();
    }

    #endregion


    #region Settings

    /// <summary>
    ///     Handles the tmi_Settings_Settings_Click event -> brings up the Settings Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_Settings_Settings_Click(object sender,
                                             EventArgs e)
    {
        FrmSettings = new FrmSettings();
        FrmSettings.Text = HelperStatic.DataReadSQLiteObjectText(
            languageName: AppLanguage,
            objectType: "Form",
            objectName: "FrmSettings"
        );
        FrmSettings.ShowDialog();
    }

    #endregion


    #region Help

    /// <summary>
    ///     Handles the tmi_Help_About_Click event -> brings up the About Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_Help_About_Click(object sender,
                                      EventArgs e)
    {
        FrmAboutBox frmAboutBox = new();
        frmAboutBox.ShowDialog();
    }

    #endregion


    #region TaskBar Stuff

    /// <summary>
    ///     Handles the tsb_Refresh_lvwFileList_Click event -> checks if there is anything in the write-Q
    ///     ... then cleans up the user-folder and triggers lvwFileList_LoadOrUpdate
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void tsb_Refresh_lvwFileList_Click(object sender,
                                                     EventArgs e)
    {
        Logger.Debug(message: "Starting");

        HelperStatic.SChangeFolderIsOkay = false;
        await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
        if (HelperStatic.SChangeFolderIsOkay)
        {
            if (Directory.Exists(path: tbx_FolderName.Text))
            {
                if (!tbx_FolderName.Text.EndsWith(value: "\\"))
                {
                    tbx_FolderName.Text += "\\";
                }

                try
                {
                    lvw_FileList.ClearData();
                    _directoryElements.Clear();
                    HelperStatic.FsoCleanUpUserFolder();
                    FolderName = tbx_FolderName.Text;
                    lvwFileList_LoadOrUpdate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInvalidFolder") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                }
            }
            else if (tbx_FolderName.Text == SpecialFolder.MyComputer.ToString())
            {
                lvwFileList_LoadOrUpdate();
            }
            else
            {
                MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInvalidFolder"), caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    ///     Performs a pull of Toponomy & Altitude info for all of the selected files.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void tsb_GetAllFromWeb_Click(object sender,
                                               EventArgs e)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        ListView lvw = frmMainAppInstance.lvw_FileList;
        if (lvw.SelectedItems.Count > 0)
        {
            foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.SelectedItems)
            {
                // don't do folders...
                string fileNameWithPath = Path.Combine(path1: FolderName, path2: lvi.Text);
                string fileNameWithoutPath = lvi.Text;
                if (File.Exists(path: fileNameWithPath))
                {
                    // check it's not in the read-queue.
                    while (HelperStatic.GenericLockCheckLockFile(fileNameWithoutPath: fileNameWithoutPath))
                    {
                        await Task.Delay(millisecondsDelay: 10);
                    }

                    string strGpsLatitude = lvi.SubItems[index: lvw.Columns[key: "clh_GPSLatitude"]
                                                             .Index]
                        .Text.ToString(provider: CultureInfo.InvariantCulture);
                    string strGpsLongitude = lvi.SubItems[index: lvw.Columns[key: "clh_GPSLongitude"]
                                                              .Index]
                        .Text.ToString(provider: CultureInfo.InvariantCulture);
                    double parsedLat = 0.0;
                    double parsedLng = 0.0;
                    if (double.TryParse(s: strGpsLatitude,
                                        style: NumberStyles.Any,
                                        provider: CultureInfo.InvariantCulture,
                                        result: out parsedLat) &&
                        double.TryParse(s: strGpsLongitude,
                                        style: NumberStyles.Any,
                                        provider: CultureInfo.InvariantCulture,
                                        result: out parsedLng))
                    {
                        lvwUpdateTagsFromWeb(strGpsLatitude: strGpsLatitude, strGpsLongitude: strGpsLongitude, lvi: lvi);
                    }
                }
            }
        }

        //done
        HandlerUpdateLabelText(label: lbl_ParseProgress, text: "");
    }

    /// <summary>
    ///     Pulls data from the various APIs and fills up the listView
    /// </summary>
    /// <param name="strGpsLatitude">Latitude as string</param>
    /// <param name="strGpsLongitude">Longitude as string</param>
    /// <param name="lvi">ListViewItem in the the main grid</param>
    private void lvwUpdateTagsFromWeb(string strGpsLatitude,
                                      string strGpsLongitude,
                                      ListViewItem lvi)
    {
        string fileNameWithoutPath = lvi.Text;
        DataTable dtToponomy = HelperStatic.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
        if (dtToponomy.Rows.Count > 0)
        {
            // Send off to SQL
            List<(string toponomyOverwriteName, string toponomyOverwriteVal)> toponomyOverwrites = new();
            toponomyOverwrites.Add(item: ("CountryCode", dtToponomy.Rows[index: 0][columnName: "CountryCode"]
                                              .ToString()));
            toponomyOverwrites.Add(item: ("Country", dtToponomy.Rows[index: 0][columnName: "Country"]
                                              .ToString()));
            toponomyOverwrites.Add(item: ("City", dtToponomy.Rows[index: 0][columnName: "City"]
                                              .ToString()));
            toponomyOverwrites.Add(item: ("State", dtToponomy.Rows[index: 0][columnName: "State"]
                                              .ToString()));
            toponomyOverwrites.Add(item: ("Sub_location", dtToponomy.Rows[index: 0][columnName: "Sub_location"]
                                              .ToString()));

            // timeZone is a bit special but that's just how we all love it....not.
            string TZ = dtToponomy.Rows[index: 0][columnName: "timeZoneId"]
                .ToString();

            DateTime createDate;
            bool _ = DateTime.TryParse(s: lvi.SubItems[index: lvw_FileList.Columns[key: "clh_CreateDate"]
                                                           .Index]
                                           .Text.ToString(provider: CultureInfo.InvariantCulture), result: out createDate);

            try
            {
                string IANATZ = TZConvert.IanaToWindows(ianaTimeZoneName: TZ);
                string TZOffset;
                TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(id: IANATZ);

                TZOffset = tst.GetUtcOffset(dateTime: createDate)
                    .ToString()
                    .Substring(startIndex: 0, length: tst.GetUtcOffset(dateTime: createDate)
                                                          .ToString()
                                                          .Length -
                                                      3);
                if (!TZOffset.StartsWith(value: "-"))
                {
                    toponomyOverwrites.Add(item: ("OffsetTime", "+" + TZOffset));
                }
                else
                {
                    toponomyOverwrites.Add(item: ("OffsetTime", TZOffset));
                }
            }
            catch
            {
                // don't do anything.
            }

            foreach ((string toponomyOverwriteName, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
            {
                HelperStatic.GenericUpdateAddToDataTable(
                    dt: DtFileDataToWriteStage3ReadyToWrite,
                    fileNameWithoutPath: lvi.Text,
                    settingId: toponomyDetail.toponomyOverwriteName,
                    settingValue: toponomyDetail.toponomyOverwriteVal
                );

                lvi.SubItems[index: lvw_FileList.Columns[key: "clh_" + toponomyDetail.toponomyOverwriteName]
                                 .Index]
                    .Text = toponomyDetail.toponomyOverwriteVal;
            }

            if (lvi.Index % 10 == 0)
            {
                Application.DoEvents();
                // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath);
            }

            HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);
            lvw_FileList.UpdateItemColour(itemText: fileNameWithoutPath, color: Color.Red);
        }

        DataTable dtAltitude = HelperStatic.DTFromAPIExifGetAltitudeFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
        if (dtAltitude.Rows.Count > 0)
        {
            HelperStatic.GenericUpdateAddToDataTable(
                dt: DtFileDataToWriteStage3ReadyToWrite,
                fileNameWithoutPath: lvi.Text,
                settingId: "GPSAltitude",
                settingValue: dtAltitude.Rows[index: 0][columnName: "Altitude"]
                    .ToString()
            );
            if (lvi.Index % 10 == 0)
            {
                Application.DoEvents();
                // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath);
            }

            HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);
            lvw_FileList.UpdateItemColour(itemText: fileNameWithoutPath, color: Color.Red);
        }
    }

    /// <summary>
    ///     Generally similar to the above.(btn_Refresh_lvwFileList_Click)
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void btn_ts_Refresh_lvwFileList_Click(object sender,
                                                        EventArgs e)
    {
        Logger.Debug(message: "Starting");

        HelperStatic.SChangeFolderIsOkay = false;
        await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
        Logger.Trace(message: "SChangeFolderIsOkay: " + HelperStatic.SChangeFolderIsOkay);

        if (HelperStatic.SChangeFolderIsOkay)
        {
            tsb_Refresh_lvwFileList_Click(sender: this, e: EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Handles the btn_OneFolderUp_Click event -> Ensures the write-Q is emtpy, that the parent folder exists and
    ///     ... if all's well then moves folder. On error moves to C:\
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void btn_OneFolderUp_Click(object sender,
                                             EventArgs e)
    {
        Logger.Debug(message: "Starting");

        HelperStatic.SChangeFolderIsOkay = false;
        await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
        Logger.Trace(message: "SChangeFolderIsOkay: " + HelperStatic.SChangeFolderIsOkay);

        if (HelperStatic.SChangeFolderIsOkay)
        {
            string? tmpStrParent = null;
            string? tmpStrRoot = null;
            // this is a bit derp but alas
            if (tbx_FolderName.Text.EndsWith(value: "\\"))
            {
                try
                {
                    tmpStrParent = HelperStatic.FsoGetParent(path: tbx_FolderName.Text);
                }
                catch
                {
                    tmpStrParent = HelperStatic.GenericCoalesce(
                        Directory.GetDirectoryRoot(path: tbx_FolderName.Text)
                        , "C:"
                    );
                }

                tmpStrRoot = HelperStatic.GenericCoalesce(
                    Directory.GetDirectoryRoot(path: tbx_FolderName.Text),
                    "C:"
                );
                tbx_FolderName.Text = HelperStatic.GenericCoalesce(tmpStrParent, tmpStrRoot);
            }

            Application.DoEvents();
            FolderName = tbx_FolderName.Text;

            Logger.Trace(message: "FolderName: " + FolderName);

            btn_ts_Refresh_lvwFileList_Click(sender: this, e: EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Handles the tsb_EditFile_Click event -> shows the Edit File Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tsb_EditFile_Click(object sender,
                                    EventArgs e)
    {
        HelperStatic.ExifShowEditFrm();
    }

    /// <summary>
    ///     Handles the tsb_RemoveGeoData_Click event -> calls HelperStatic.ExifRemoveLocationData to remove GeoData from all
    ///     selected files
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void tsb_RemoveGeoData_Click(object sender,
                                               EventArgs e)
    {
        // if user is impatient and hammer-spams the button it could create a very long queue of nothing-useful.
        if (!RemoveGeoDataIsRunning)
        {
            RemoveGeoDataIsRunning = true;
            await HelperStatic.ExifRemoveLocationData(senderName: "FrmMainApp");
            RemoveGeoDataIsRunning = false;
            HelperStatic.LvwCountItemsWithGeoData();
        }
    }

    /// <summary>
    ///     Handles the tsb_ImportGPX_Click event -> shows the FrmImportGpx Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tsb_ImportGPX_Click(object sender,
                                     EventArgs e)
    {
        bool validFilesToImport = false;
        foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
        {
            if (File.Exists(path: Path.Combine(path1: FolderName, path2: lvi.Text)))
            {
                validFilesToImport = true;
                break;
            }
        }

        if (validFilesToImport)
        {
            FrmImportGpx frmImportGpx = new();
            frmImportGpx.ShowDialog();
        }
        else
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmImportGpx_NoFileSelected"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    ///     Handles various keypress events. -> currently for tbx_FolderName when pressing Enter it will move to the folder
    ///     ... if value is a folder subject to the usual "move folder" requirements.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void tbx_FolderName_KeyDown(object sender,
                                              KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            HelperStatic.SChangeFolderIsOkay = false;
            await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
            if (HelperStatic.SChangeFolderIsOkay)
            {
                btn_ts_Refresh_lvwFileList_Click(sender: this, e: new EventArgs());
            }
        }
    }

    /// <summary>
    ///     This handles the event when the user clicks into the textbox -> current value gets selected.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tbx_FolderName_Enter(object sender,
                                      EventArgs e)
    {
        tbx_FolderName.SelectAll();
    }

    /// <summary>
    ///     Handles the tsb_SaveFiles_Click event -> triggers ExifWriteExifToFile
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void tsb_SaveFiles_Click(object sender,
                                           EventArgs e)
    {
        while (HelperStatic.FileListBeingUpdated || HelperStatic.FilesAreBeingSaved)
        {
            await Task.Delay(millisecondsDelay: 10);
        }

        // i think having an Item active can cause a lock on it
        lvw_FileList.SelectedItems.Clear();
        // also the problem here is that the exiftoolAsync can still be running and locking the file.

        await HelperStatic.ExifWriteExifToFile();
        // shouldn't be needed but just in case.
        HelperStatic.FilesAreBeingSaved = false;
        DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
    }

    #endregion


    #region lvw_FileList Interaction

    /// <summary>
    ///     Responsible for updating the main listview. For each file depending on the "compatible" or "incompatible" naming
    ///     ... it assigns the outstanding files according to compatibility and then runs
    ///     the respective exiftool commands.
    ///     Also I've introduced a "Please Wait" Form to block the Main Form from being interacted with while the folder is
    ///     refreshing. Soz but needed.
    /// </summary>
    private async void lvwFileList_LoadOrUpdate()
    {
        Logger.Debug(message: "Starting");

        Logger.Trace(message: "Clear lvw_FileList");
        lvw_FileList.ClearData();
        _directoryElements.Clear();
        Application.DoEvents();
        HelperStatic.FilesBeingProcessed.Clear();
        RemoveGeoDataIsRunning = false;

        #region PleaseWaitBox

        Logger.Trace(message: "Create PleaseWaitBox");
        Form pleaseWaitBox = new();
        pleaseWaitBox.Text = "Wait...";
        pleaseWaitBox.ControlBox = false;
        FlowLayoutPanel panel = new();

        Label lblText = new();
        lblText.Text = HelperStatic.DataReadSQLiteObjectText(
            languageName: AppLanguage,
            objectType: "Label",
            objectName: "lbl_FolderIsLoading"
        );
        lblText.AutoSize = true;

        panel.Controls.Add(value: lblText);

        panel.Padding = new Padding(all: 5);
        panel.AutoSize = true;

        pleaseWaitBox.Controls.Add(value: panel);
        pleaseWaitBox.Size = new Size(width: panel.Width + 10, height: panel.Height + 5);
        pleaseWaitBox.ShowInTaskbar = false;

        pleaseWaitBox.StartPosition = FormStartPosition.CenterScreen;
        Logger.Trace(message: "Show PleaseWaitBox");
        pleaseWaitBox.Show();
        Logger.Trace(message: "Disable FrmMainApp");
        Enabled = false;

        #endregion

        Logger.Trace(message: "tbx_FolderName.Text: " + tbx_FolderName.Text);
        if (tbx_FolderName.Text != null)
        {
            // this shouldn't really happen but just in case
            Logger.Trace(message: "FolderName: " + FolderName);
            if (FolderName is null)
            {
                if (Directory.Exists(path: tbx_FolderName.Text))
                {
                    // nothing
                }
                else
                {
                    tbx_FolderName.Text = @"C:\";
                }

                FolderName = tbx_FolderName.Text;
                Logger.Trace(message: "FolderName [was null, now updated]: " + FolderName);
            }

            ParseCurrentDirectoryToDEs();
            lvw_FileList.ReloadFromDEs(_directoryElements);

            Logger.Trace(message: "Calling ExifGetExifFromFolder - " + FolderName);
            await HelperStatic.ExifGetExifFromFolder(folderNameToUse: FolderName);
            Logger.Trace(message: "Finished ExifGetExifFromFolder - " + FolderName);
        }

        HelperStatic.FileListBeingUpdated = false;
        HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Ready.");
        Logger.Trace(message: "Enable FrmMainApp");
        Enabled = true;
        Logger.Trace(message: "Hide PleaseWaitBox");
        pleaseWaitBox.Hide();

        // Not logging this.
        HelperStatic.LvwCountItemsWithGeoData();
    }

    private void ParseCurrentDirectoryToDEs()
    {
        // Special Case is "MyComputer"...
        // Only list drives...
        if (FolderName == SpecialFolder.MyComputer.ToString())
        {
            Logger.Trace(message: "Listing Drives");
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                Logger.Trace(message: "Drive:" + drive.Name);
                _directoryElements.Add(new DirectoryElement(
                    itemName: drive.Name,
                    type: DirectoryElement.ElementType.Drive,
                    fullPathAndName: drive.RootDirectory.FullName
                    ));
            }

            Logger.Trace(message: "Listing Drives - OK");
            return;
        }

        // add a parent folder. "dot dot"
        try
        {
            Logger.Trace(message: "Files: Adding Parent Folder");
            string tmpStrParent = HelperStatic.FsoGetParent(path: tbx_FolderName.Text);
            if (tmpStrParent != null && tmpStrParent != SpecialFolder.MyComputer.ToString())
            {
                _directoryElements.Add(new DirectoryElement(
                    itemName: ParentFolder,
                    type: DirectoryElement.ElementType.ParentDirectory,
                    fullPathAndName: tmpStrParent
                    ));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(message: "Error: " + ex.Message);
        }


        // list folders, ReparsePoint means these are links.
        HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Processing: Directories");
        Logger.Trace(message: "Listing Folders");
        List<string> dirs = new();
        try
        {
            DirectoryInfo di = new(path: FolderName);
            foreach (DirectoryInfo directoryInfo in di.GetDirectories())
            {
                if (directoryInfo.Attributes.ToString()
                        .Contains(value: "Directory") &&
                    !directoryInfo.Attributes.ToString()
                        .Contains(value: "ReparsePoint"))
                {
                    Logger.Trace(message: "Folder: " + directoryInfo.Name);
                    _directoryElements.Add(new DirectoryElement(
                        itemName: directoryInfo.Name,
                        type: DirectoryElement.ElementType.SubDirectory,
                        fullPathAndName: directoryInfo.FullName
                        ));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(message: "Error: " + ex.Message);
            MessageBox.Show(text: ex.Message);
        }
        Logger.Trace(message: "Listing Folders - OK");



        Logger.Trace(message: "Loading allowedExtensions");
        string[] allowedExtensions = new string[AncillaryListsArrays.AllCompatibleExtensions()
            .Length];
        Array.Copy(sourceArray: allowedExtensions, destinationArray: AncillaryListsArrays.AllCompatibleExtensions(), length: 0);
        for (int i = 0; i < allowedExtensions.Length; i++)
        {
            allowedExtensions[i] = AncillaryListsArrays.AllCompatibleExtensions()[i]
                .Split('\t')
                .FirstOrDefault();
            List<string> subItemList = new();
            foreach (ColumnHeader columnHeader in lvw_FileList.Columns)
            {
                if (columnHeader.Name != "clh_FileName")
                {
                    subItemList.Add(item: "");
                }
            }
        }

        Logger.Trace(message: "Loading allowedExtensions - OK");

        // list files that have whitelisted extensions
        Logger.Trace(message: "Files: Listing Files");
        List<string> listFilesWithPath = new();
        try
        {
            listFilesWithPath = Directory
                .GetFiles(path: FolderName)
                .Where(predicate: file => allowedExtensions.Any(predicate: file.ToLower()
                                                                    .EndsWith))
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.Trace(message: "Files: Listing Files - Error: " + ex.Message);
            MessageBox.Show(text: ex.Message);
        }

        Logger.Trace(message: "Files: Listing Files - OK");

        listFilesWithPath = listFilesWithPath.OrderBy(keySelector: o => o)
            .ToList();

        Logger.Trace(message: "Listing Files");
        foreach (string fileNameWithPath in listFilesWithPath)
        {
            Logger.Trace(message: "File: " + fileNameWithPath);
            string fileNameWithoutPath = Path.GetFileName(path: fileNameWithPath);
            HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);
            _directoryElements.Add(new DirectoryElement(
                itemName: Path.GetFileName(path: fileNameWithoutPath),
                type: DirectoryElement.ElementType.File,
                fullPathAndName: fileNameWithPath
                        ));
        }
        Logger.Trace(message: "Listing Folders - OK");
    }


    /// <summary>
    ///     Handles the lvw_FileList_MouseDoubleClick event -> if user clicked on a folder then enter, if a file then edit
    ///     ... else warn and don't do anything.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void lvw_FileList_MouseDoubleClick(object sender,
                                                     MouseEventArgs e)
    {
        Logger.Debug(message: "Starting");

        ListViewHitTestInfo info = lvw_FileList.HitTest(x: e.X, y: e.Y);
        ListViewItem item = info.Item;

        if (item != null)
        {
            Logger.Trace(message: "item: " + item.Text);

            bool isDrive = HelperStatic.LvwItemIsDrive(lvwFileListItem: item);
            // if .. (parent) then do a folder-up
            if (item.Text == ParentFolder)
            {
                btn_OneFolderUp_Click(sender: sender, e: EventArgs.Empty);
            }
            // if this is a folder or drive, enter
            else if (Directory.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: item.Text)) || isDrive)
            {
                // check for outstanding files first and save if user wants
                HelperStatic.SChangeFolderIsOkay = false;
                await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
                if (HelperStatic.SChangeFolderIsOkay)
                {
                    if (Directory.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: item.Text)))
                    {
                        tbx_FolderName.Text = Path.Combine(path1: tbx_FolderName.Text, path2: item.Text);
                    }
                    else
                    {
                        // itemText.Text here will be something like "C_Windows_320GB_M2_nVME (C:\)"
                        // so just extract whatever is in the parentheses
                        tbx_FolderName.Text = item.Text.Split('(')
                                                  .Last()
                                                  .Split(')')
                                                  .FirstOrDefault() +
                                              @"\";
                    }

                    btn_ts_Refresh_lvwFileList_Click(sender: this, e: EventArgs.Empty);
                }
            }
            // if this is a file
            else if (File.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: item.Text)))
            {
                Logger.Trace(message: "Trigger FrmEditFileData");
                FrmEditFileData = new FrmEditFileData();

                FolderName = tbx_FolderName.Text;

                Logger.Trace(message: "Add File To lvw_FileListEditImages");
                FrmEditFileData.lvw_FileListEditImages.Items.Add(text: item.Text);

                Logger.Trace(message: "FrmEditFileData Get objectTexts");
                FrmEditFileData.Text = HelperStatic.DataReadSQLiteObjectText(
                    languageName: AppLanguage,
                    objectType: "Form",
                    objectName: "FrmEditFileData"
                );

                Logger.Trace(message: "FrmEditFileData ShowDialog");
                FrmEditFileData.ShowDialog();
            }
        }
        else
        {
            lvw_FileList.SelectedItems.Clear();
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_WarningNoItemSelected"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    ///     Handles an update of map location and image preview based on selected file
    /// </summary>
    private async Task lvw_HandleSelectionChange()
    {
        if (lvw_FileList.FocusedItem != null)
        {
            await HelperStatic.LvwItemClickNavigate();
            // it's easier to call the create-preview here than in the other one because focusedItems misbehave/I don't quite understand it/them
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                string fileNameWithPath = Path.Combine(FolderName +
                                                       lvw_FileList.SelectedItems[index: 0]
                                                           .Text);

                if (File.Exists(path: fileNameWithPath))
                {
                    await HelperStatic.LvwItemCreatePreview(
                        fileNameWithPath: fileNameWithPath);
                }
                else
                {
                    pbx_imagePreview.Image = null;
                }
            }

            NavigateMapGo();
            // pbx_imagePreview.Image = null;
        }
    }

    /// <summary>
    ///     Technically same as lvw_FileList_KeyDown but movement is a bit b...chy with "Down".
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void lvw_FileList_KeyUp(object sender,
                                          KeyEventArgs e)
    {
        if (
            e.KeyCode == Keys.PageUp ||
            e.KeyCode == Keys.PageDown ||
            e.KeyCode == Keys.Up ||
            e.KeyCode == Keys.Down ||
            e.KeyCode == Keys.Home ||
            e.KeyCode == Keys.End
        )
        {
            await lvw_HandleSelectionChange();
        }
    }

    /// <summary>
    ///     Handles the various keypress combinations. See inline comments for details.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">The key pressed</param>
    private async void lvw_FileList_KeyDown(object sender,
                                            KeyEventArgs e)
    {
        // control A -> select all
        if (e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
        {
            HelperStatic.SNowSelectingAllItems = true;

            for (int i = 0; i < lvw_FileList.Items.Count; i++)
            {
                lvw_FileList.Items[index: i]
                    .Selected = true;
                // so because there is no way to do a proper "select all" w/o looping i only want to run the "navigate" (which is triggered on select-state-change at the end)
                if (i == lvw_FileList.Items.Count - 1)
                {
                    HelperStatic.SNowSelectingAllItems = false;
                    await HelperStatic.LvwItemClickNavigate();
                    NavigateMapGo();
                }
            }

            // just in case...
            HelperStatic.SNowSelectingAllItems = false;
        }

        // Shift Ctrl C -> copy details
        else if (e.Control && e.Shift && e.KeyCode == Keys.C)
        {
            HelperStatic.LwvCopyGeoData();
        }

        // Shift Ctrl V -> paste details
        else if (e.Control && e.Shift && e.KeyCode == Keys.V)
        {
            HelperStatic.LwvPasteGeoData();
        }

        // Ctrl Enter -> Edit File
        else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Enter)
        {
            HelperStatic.ExifShowEditFrm();
        }

        // Backspace -> Up one folder
        else if (e.KeyCode == Keys.Back)
        {
            btn_OneFolderUp_Click(sender: sender, e: EventArgs.Empty);
        }

        // Enter  -> enter if folder / drive
        else if (e.KeyCode == Keys.Enter)
        {
            if (lvw_FileList.SelectedItems.Count == 1)
            {
                ListViewItem item = lvw_FileList.SelectedItems[index: 0];
                string folderToEnter = item.Text;

                // if .. (parent) then do a folder-up
                if (folderToEnter == ParentFolder)
                {
                    btn_OneFolderUp_Click(sender: sender, e: EventArgs.Empty);
                }
                // if this is a folder or drive, enter
                else if (Directory.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: item.Text)))
                {
                    // check for outstanding files first and save if user wants
                    HelperStatic.SChangeFolderIsOkay = false;
                    await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
                    if (HelperStatic.SChangeFolderIsOkay)
                    {
                        if (Directory.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: item.Text)))
                        {
                            tbx_FolderName.Text = Path.Combine(path1: tbx_FolderName.Text, path2: item.Text);
                        }

                        btn_ts_Refresh_lvwFileList_Click(sender: this, e: EventArgs.Empty);
                    }
                }
            }
        }

        // F5 -> Refresh folder
        else if (e.KeyCode == Keys.F5)
        {
            tsb_Refresh_lvwFileList_Click(sender: sender, e: EventArgs.Empty);
            e.Handled = true;
        }

        // Control S -> Save files
        else if (e.Control && e.KeyCode == Keys.S)
        {
            while (HelperStatic.FileListBeingUpdated || HelperStatic.FilesAreBeingSaved)
            {
                await Task.Delay(millisecondsDelay: 10);
            }

            // i think having an Item active can cause a lock on it
            lvw_FileList.SelectedItems.Clear();

            // sort alphabetically
            DtFileDataToWriteStage3ReadyToWrite.DefaultView.Sort = "fileNameWithoutPath ASC";
            DtFileDataToWriteStage3ReadyToWrite = DtFileDataToWriteStage3ReadyToWrite.DefaultView.ToTable();

            await HelperStatic.ExifWriteExifToFile();
            // shouldn't be needed but just in case.
            HelperStatic.FilesAreBeingSaved = false;
            DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
        }
    }


    /// <summary>
    ///     Watches for the user to lift the mouse button while over the listview. This will trigger the collection of
    ///     coordinates and map them.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void lvw_FileList_MouseUp(object sender,
                                            MouseEventArgs e)
    {
        await lvw_HandleSelectionChange();
    }

    #endregion


    #region handlers

    /// <summary>
    ///     Updates the Text of any Label from outside the thread.
    /// </summary>
    /// <param name="label">The Label Control that needs updating</param>
    /// <param name="text">The Text that will be assigned</param>
    internal static void HandlerUpdateLabelText(Label label,
                                                string text)
    {
        // If the current thread is not the UI thread, InvokeRequired will be true
        if (label.InvokeRequired)
        {
            // If so, call Invoke, passing it a lambda expression which calls
            // UpdateText with the same label and text, but on the UI thread instead.
            label.Invoke(method: (Action)(() => HandlerUpdateLabelText(label: label, text: text)));
            return;
        }

        // If we're running on the UI thread, we'll get here, and can safely update 
        // the label's text.
        label.Text = text;
        label.Refresh();
    }


    /// <summary>
    ///     This is a dummy. It's here to help me find where things go wrong when clicked on the map. Doesn't do anything.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void wbv_MapArea_Click(object sender,
                                   EventArgs e)
    {
        // note to self: the one you're looking for is called wbv_MapArea_WebMessageReceived
    }

    private void FrmMainApp_ResizeBegin(object sender,
                                        EventArgs e)
    {
        wbv_MapArea.Hide();
    }

    private void FrmMainApp_ResizeEnd(object sender,
                                      EventArgs e)
    {
        wbv_MapArea.Show();
    }

    private void selectColumnsToolStripMenuItem_Click(object sender,
                                                      EventArgs e)
    {
        lvw_FileList.ShowColumnSelectionDialog();
    }

    #endregion
}


public static class ControlExtensions
{
    /// <summary>
    ///     Makes sure the Control in question gets doubleBufferPropertyInfo enabled/disabled.
    ///     ...Realistically we're using this to assign doubleBufferPropertyInfo = enabled to the main listView.
    ///     ...This helps stop the flickering on updating the various data points and/or rows (Items).
    /// </summary>
    /// <param name="control">The Control that needs the value assigned</param>
    /// <param name="enable">Bool true or false (aka on or off)</param>
    public static void DoubleBuffered(this Control control,
                                      bool enable)
    {
        PropertyInfo doubleBufferPropertyInfo = control.GetType()
            .GetProperty(name: "DoubleBuffered", bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic);
        doubleBufferPropertyInfo.SetValue(obj: control, value: enable, index: null);
    }
}