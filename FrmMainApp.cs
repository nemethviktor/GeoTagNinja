using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExifToolWrapper;
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

    /// <summary>
    ///     Returns the list of elements in the currently opened directory.
    /// </summary>
    public DirectoryElementCollection DirectoryElements { get; } = new();

    private void btn_ManageFavourites_Click(object sender,
                                            EventArgs e)
    {
        DataTable dtFavourites = AppStartupLoadFavourites();
        if (dtFavourites.Rows.Count > 0)
        {
            FrmManageFavourites frmManageFavouritesInstance = new();
            frmManageFavouritesInstance.ShowDialog();
        }
        else
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_NoFavouritesDefined"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
        }
    }

    #region Variables

    internal static readonly string ResourcesFolderPath = Path.Combine(path1: AppDomain.CurrentDomain.BaseDirectory, path2: "Resources");
    internal static readonly string UserDataFolderPath = Path.Combine(path1: GetFolderPath(folder: SpecialFolder.ApplicationData), path2: "GeoTagNinja");
    internal const string DoubleQuote = "\"";

    public const string ParentFolder = "..";

    internal static DataTable DtLanguageLabels;
    internal static DataTable DtObjectNames;
    internal static DataTable DtObjectTagNamesIn;
    internal static DataTable DtObjectTagNamesOut;
    internal static DataTable DtIsoCountryCodeMapping;
    internal static string FolderName;
    internal static string _AppLanguage = "English"; // default to english
    internal static List<string> LstFavourites = new();

    internal static string ShowLocToMapDialogChoice = "default";
    internal FrmSettings FrmSettings;
    internal FrmEditFileData FrmEditFileData;
    internal FrmImportGpx FrmImportGpx;

    internal string _mapHtmlTemplateCode = "";

    internal static bool RemoveGeoDataIsRunning;

    // this is for copy-paste
    internal static DataTable DtFileDataCopyPool;
    internal static DataTable DtFileDataPastePool;
    internal static string FileDateCopySourceFileNameWithPath;

    // just to keep myself sane here....
    // the "queue" tables have the following structure:
    // (columnName: "fileNameWithoutPath");
    // (columnName: "settingId");
    // (columnName: "settingValue");

    // these are for queueing files up
    internal static DataTable DtFileDataToWriteStage1PreQueue;
    internal static DataTable DtFileDataToWriteStage2QueuePendingSave;
    internal static DataTable DtFileDataToWriteStage3ReadyToWrite;

    // this is for checking if files need to be re-parsed.
    internal static DataTable DtFileDataSeenInThisSession;

    internal static DataTable DtToponomySessionData;


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

        if (!Directory.Exists(path: UserDataFolderPath))
        {
            Directory.CreateDirectory(path: UserDataFolderPath);
        }

        // Set up logging
        LoggingConfiguration config = new();

        string logFileLocation = Path.Combine(path1: UserDataFolderPath, path2: "logfile.txt");
        if (File.Exists(path: logFileLocation))
        {
        File.Delete(path: logFileLocation);
        }

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

        HelperStatic.GenericCreateDataTables();
        AppStartupCreateDataBaseFile();
        AppStartupWriteDefaultSettings();
        AppStartupReadObjectNamesAndLanguage();
        AppStartupReadAPILanguage();
        AppStartupApplyDefaults();
        AppStartupCheckWebView2();
        AppStartupInitializeComponentFrmMainApp();
        AppStartupEnableDoubleBuffering();

        FormClosing += FrmMainApp_FormClosing;

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
            lvw_FileList.ReadAndApplySetting(appLanguage: AppLanguage, objectNames: DtObjectNames);
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

        AppStartupLoadFavourites();
        AppStartupLoadCustomRules();
        AppStartupPullLastLatLngFromSettings();
        AppStartupPullOverWriteBlankToponomy();
        AppStartupPullToponomyRadiusAndMaxRows();
        NavigateMapGo();

        await HelperStatic.GenericCheckForNewVersions();

        Logger.Info(message: "Done.");
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
        HelperStatic.HsMapMarkers.Add(item: parseLatLngTextBox());
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
                        labelText: HelperStatic.DataReadDTObjectText(
                            objectType: "Label",
                            objectName: "lbl_QuestionAddToponomy"
                        ),
                        caption: "Info",
                        checkboxText: HelperStatic.DataReadDTObjectText(
                            objectType: "CheckBox",
                            objectName: "ckb_QuestionAddToponomyDontAskAgain"
                        ),
                        returnCheckboxText: "_remember",
                        button1Text: HelperStatic.DataReadDTObjectText(
                            objectType: "Button",
                            objectName: "btn_Yes"
                        ),
                        returnButton1Text: "yes",
                        button2Text: HelperStatic.DataReadDTObjectText(
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
    ///     Parses the tbx_lat and tbx_lng text boxes.
    ///     If the contents is a valid double, returns touple (lat, lng)
    ///     with values as string and dec separator ".".
    ///     Otherwise default "0" is returned for both.
    /// </summary>
    private (string, string) parseLatLngTextBox()
    {
        Logger.Trace(message: "Starting parseLatLngTextBox ...");

        // Default values if text field is empty
        string LatCoordinate = "0";
        string LngCoordinate = "0";

        // Get txtbox contents
        string strLatCoordinate = "";
        string strLngCoordinate = "";
        if (tbx_lat.Text != null)
        {
            strLatCoordinate = tbx_lat.Text.Replace(oldChar: ',', newChar: '.');
        }

        if (tbx_lng.Text != null)
        {
            strLngCoordinate = tbx_lng.Text.Replace(oldChar: ',', newChar: '.');
        }

        // Check if it's a valid double -> otherwise defaults above
        try
        {
            Logger.Trace(message: "parseLatLngTextBox");
            double parsedLat;
            double parsedLng;
            if (double.TryParse(s: strLatCoordinate, style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture, result: out parsedLat) &&
                double.TryParse(s: strLngCoordinate, style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture, result: out parsedLng))
            {
                LatCoordinate = strLatCoordinate;
                LngCoordinate = strLngCoordinate;
                Logger.Trace(message: "parseLatLngTextBox OK - LatCoordinate: " + strLatCoordinate + " - LngCoordinate: " + strLngCoordinate);
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorNavigateMapGoHTMLCode") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        return (LatCoordinate, LngCoordinate);
    }


    private void updateWebView(IDictionary<string, string> replacements)
    {
        string htmlCode = _mapHtmlTemplateCode;

        // If set, replace arcgis key
        if (HelperStatic.SArcGisApiKey != null)
        {
            htmlCode = htmlCode.Replace(oldValue: "yourApiKey", newValue: HelperStatic.SArcGisApiKey);
        }

        Logger.Trace(message: "HelperStatic.SArcGisApiKey == null: " + (HelperStatic.SArcGisApiKey == null));

        foreach (KeyValuePair<string, string> replacement in replacements)
        {
            Logger.Trace(message: string.Format(format: "Replace: {0} -> {1}",
                                                arg0: replacement.Key, arg1: replacement.Value));
            htmlCode = htmlCode.Replace(oldValue: replacement.Key, newValue: replacement.Value);
        }

        // show the decoded location on the map
        try
        {
            Logger.Trace(message: "Calling wbv_MapArea.NavigateToString");
            wbv_MapArea.NavigateToString(htmlContent: htmlCode);
            Logger.Trace(message: "Calling wbv_MapArea.NavigateToString - OK");
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewNavigateToStringInHTMLFile") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }


    /// <summary>
    ///     Handles the navigation to a coordinate on the map. Replaces hard-coded values w/ user-provided ones
    ///     ... and executes the navigation action.
    /// </summary>
    private void NavigateMapGo()
    {
        Logger.Debug(message: "Starting");

        // Set up replacements
        IDictionary<string, string> htmlReplacements = new Dictionary<string, string>();

        HelperStatic.HtmlAddMarker = "";
        double dblMinLat = 900;
        double dblMinLng = 900;
        double dblMaxLat = -1;
        double dblMaxLng = -1;

        // Add markers on map for every marker-item and
        // find viewing rect. for map (min / max of all markers to enclose all of them)
        if (HelperStatic.HsMapMarkers.Count > 0)
        {
            double dLat = 0;
            double dLng = 0;
            foreach ((string strLat, string strLng) locationCoord in HelperStatic.HsMapMarkers)
            {
                // Add marker location
                HelperStatic.HtmlAddMarker += "var marker = L.marker([" + locationCoord.strLat + ", " + locationCoord.strLng + "]).addTo(map).openPopup();" + "\n";

                // Update viewing rectangle if neede
                dLat = double.Parse(s: locationCoord.strLat, provider: CultureInfo.InvariantCulture);
                dLng = double.Parse(s: locationCoord.strLng, provider: CultureInfo.InvariantCulture);
                dblMinLat = Math.Min(val1: dblMinLat, val2: dLat);
                dblMaxLat = Math.Max(val1: dblMaxLat, val2: dLat);
                dblMinLng = Math.Min(val1: dblMinLng, val2: dLng);
                dblMaxLng = Math.Max(val1: dblMaxLng, val2: dLng);

                Logger.Trace(message: "Added marker: strLatCoordinate: " + locationCoord.strLat + " / strLngCoordinate:" + locationCoord.strLng);
            }

            HelperStatic.LastLat = dLat;
            HelperStatic.LastLng = dLng;

            HelperStatic.MinLat = dblMinLat;
            HelperStatic.MinLng = dblMinLng;
            HelperStatic.MaxLat = dblMaxLat;
            HelperStatic.MaxLng = dblMaxLng;
            htmlReplacements.Add(key: "{ HTMLAddMarker }", value: HelperStatic.HtmlAddMarker);
        }
        else
        {
            // No markers added
            htmlReplacements.Add(key: "{ HTMLAddMarker }", value: "");
        }

        Logger.Trace(message: "Added " + HelperStatic.HsMapMarkers.Count + " map markers.");

        htmlReplacements.Add(key: "replaceLat", value: HelperStatic.LastLat.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));
        htmlReplacements.Add(key: "replaceLng", value: HelperStatic.LastLng.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));
        htmlReplacements.Add(key: "replaceMinLat", value: HelperStatic.MinLat.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));
        htmlReplacements.Add(key: "replaceMinLng", value: HelperStatic.MinLng.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));
        htmlReplacements.Add(key: "replaceMaxLat", value: HelperStatic.MaxLat.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));
        htmlReplacements.Add(key: "replaceMaxLng", value: HelperStatic.MaxLng.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));

        updateWebView(replacements: htmlReplacements);
    }


    /// <summary>
    ///     Initialises the map in the app and browses to the default or last-used location.
    /// </summary>
    /// <returns></returns>
    private async Task InitialiseWebView()
    {
        Logger.Debug(message: "Starting");

        // Create Browser Connection
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

        // Initialize WebView
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
        try
        {
            Logger.Trace(message: "Read map.html file");
            _mapHtmlTemplateCode = File.ReadAllText(path: Path.Combine(path1: ResourcesFolderPath, path2: "map.html"));
            Logger.Trace(message: "Read map.html file OK");
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Read map.html file - Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewReadHTMLFile") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // Get the ArcGis API Key
        if (HelperStatic.SArcGisApiKey == null)
        {
            Logger.Trace(message: "Replace hard-coded values in the html code - SArcGisApiKey is null");
            HelperStatic.SArcGisApiKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();
            Logger.Trace(message: "Replace hard-coded values in the html code - SArcGisApiKey obtained from SQLite OK");
        }

        // Parse coords from lat/lng text box
        (string LatCoordinate, string LngCoordinate) = parseLatLngTextBox();

        // Set up replacements
        IDictionary<string, string> htmlReplacements = new Dictionary<string, string>();
        htmlReplacements.Add(key: "replaceLat", value: LatCoordinate);
        htmlReplacements.Add(key: "replaceLng", value: LngCoordinate);

        // Show on Map
        updateWebView(replacements: htmlReplacements);

        // Set up event handler for clicks in map
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
                FrmEditFileData.Text = HelperStatic.DataReadDTObjectText(
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
        FrmImportGpx.Text = HelperStatic.DataReadDTObjectText(
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
                    DirectoryElements.Clear();
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
        if (frmMainAppInstance != null)
        {
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
        HelperStatic.CurrentAltitude = null;
        HelperStatic.CurrentAltitude = lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
            .SubItems[index: lvw_FileList.Columns[key: "clh_GPSAltitude"]
                          .Index]
            .Text.ToString(provider: CultureInfo.InvariantCulture);

        DataTable dtToponomy = HelperStatic.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
        if (dtToponomy.Rows.Count > 0)
        {
            // Send off to SQL
            List<(string toponomyOverwriteName, string toponomyOverwriteVal)> toponomyOverwrites = new();
            toponomyOverwrites.Add(item: ("CountryCode", dtToponomy.Rows[index: 0][columnName: "CountryCode"]
                                              .ToString()));
            toponomyOverwrites.Add(item: ("Country", dtToponomy.Rows[index: 0][columnName: "Country"]
                                              .ToString()));

            foreach (string toponomyReplace in AncillaryListsArrays.ToponomyReplaces())
            {
                string settingId = toponomyReplace;
                string settingVal = HelperStatic.ReplaceBlankToponomy(settingId: settingId, settingValue: dtToponomy.Rows[index: 0][columnName: toponomyReplace]
                                                                          .ToString());
                toponomyOverwrites.Add(item: (settingId, settingVal));
            }

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
        DirectoryElements.Clear();
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
        lblText.Text = HelperStatic.DataReadDTObjectText(
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

            // Clear Tables that keep track of things...
            Logger.Trace(message: "Clear FrmMainApp.DtOriginalTakenDate && FrmMainApp.DtOriginalCreateDate");
            FrmMainApp.DtOriginalTakenDate.Clear();
            FrmMainApp.DtOriginalCreateDate.Clear();

            // Load data (and add to tables)
            DirectoryElements.ParseFolderToDEs(FolderName, delegate (string statusText) {
                HandlerUpdateLabelText(label: lbl_ParseProgress, text: statusText);
            });

            // Show
            lvw_FileList.ReloadFromDEs(directoryElements: DirectoryElements);

            Logger.Trace(message: "Calling ExifGetExifFromFolder - " + FolderName);
            // await HelperStatic.ExifGetExifFromFolder(folderNameToUse: FolderName);
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

        if (item == null)
        {
            lvw_FileList.SelectedItems.Clear();
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_WarningNoItemSelected"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
            return;
        }

        DirectoryElement item_de = (DirectoryElement)item.Tag;
        Logger.Trace(message: "item: " + item.Text);

        switch(item_de.Type)
        {
            // if .. (parent) then do a folder-up
            case DirectoryElement.ElementType.ParentDirectory:
                btn_OneFolderUp_Click(sender: sender, e: EventArgs.Empty);
                break;

            // if this is a folder or drive, enter
            case DirectoryElement.ElementType.SubDirectory:
            case DirectoryElement.ElementType.MyComputer:
            case DirectoryElement.ElementType.Drive:
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

                break;

            // Edit file
            case DirectoryElement.ElementType.File:
                Logger.Trace(message: "Trigger FrmEditFileData");
                FrmEditFileData = new FrmEditFileData();

                Logger.Trace(message: "Add File To lvw_FileListEditImages");
                FrmEditFileData.lvw_FileListEditImages.Items.Add(text: item_de.ItemName);

                Logger.Trace(message: "FrmEditFileData Get objectTexts");
                FrmEditFileData.Text = HelperStatic.DataReadDTObjectText(
                    objectType: "Form",
                    objectName: "FrmEditFileData"
                );

                Logger.Trace(message: "FrmEditFileData ShowDialog");
                FrmEditFileData.ShowDialog();
                break;
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
                    await HelperStatic.GenericCreateImagePreview(
                        fileNameWithPath: fileNameWithPath, initiator: "FrmMainApp");
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
    ///     Handles the tmi_Settings_Settings_Click event -> brings up the Settings Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_Settings_Settings_Click(object sender,
                                             EventArgs e)
    {
        FrmSettings = new FrmSettings();
        FrmSettings.Text = HelperStatic.DataReadDTObjectText(
            objectType: "Form",
            objectName: "FrmSettings"
        );
        FrmSettings.ShowDialog();
    }

    /// <summary>
    ///     Brings up the Manage Favourites Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_Settings_Favourites_Click(object sender,
                                               EventArgs e)
    {
        DataTable dtFavourites = AppStartupLoadFavourites();
        if (dtFavourites.Rows.Count > 0)
        {
            FrmManageFavourites frmManageFavouritesInstance = new();
            frmManageFavouritesInstance.ShowDialog();
        }
        else
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_NoFavouritesDefined"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
        }
    }

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

    private void btn_SaveLocation_Click(object sender,
                                        EventArgs e)
    {
        if (cbx_Favourites.Text.Length > 0)
        {
            ListView lvw = lvw_FileList;
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                ListViewItem lvi = lvw_FileList.SelectedItems[index: 0];

                string favouriteName = cbx_Favourites.Text;

                DataTable dtFavourite = HelperStatic.DataReadSQLiteFavourites(structureOnly: true);
                dtFavourite.Clear();
                DataRow drFavourite = dtFavourite.NewRow();
                drFavourite[columnName: "favouriteName"] = favouriteName;

                foreach (string tagName in AncillaryListsArrays.GetFavouriteTags())
                {
                    string addStr = lvi.SubItems[index: lvw.Columns[key: "clh_" + tagName]
                                                      .Index]
                    .Text.ToString(provider: CultureInfo.InvariantCulture);

                    if (addStr == "-")
                {
                        addStr = "";
                }

                    drFavourite[columnName: tagName] = addStr;
                }

                HelperStatic.DataDeleteSQLiteFavourite(favouriteName: favouriteName);

                HelperStatic.DataWriteSQLiteAddNewFavourite(drFavourite: drFavourite);

                DataTable dtFavourites = AppStartupLoadFavourites();
                MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoFavouriteSaved"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
            }

            else
            {
                MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_WarningNoItemSelected"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoFavouriteNameCannotBeBlank"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }
    }

    private async void btn_LoadFavourite_Click(object sender,
                                               EventArgs e)
    {
        string favouriteToLoad = cbx_Favourites.Text;

        // pull favs (this needs doing each time as user may have changed it)
        DataTable dtFavourites = AppStartupLoadFavourites();

        if (LstFavourites.Contains(item: favouriteToLoad))
        {
            EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in dtFavourites.AsEnumerable()
                                                               where dataRow.Field<string>(columnName: "favouriteName") == favouriteToLoad
                                                               select dataRow;

            DataRow drFavouriteData = drDataTableData.FirstOrDefault();

            bool filesAreSelected = false;
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                {
                    string fileNameWithoutPath = lvi.Text;
                    if (File.Exists(path: Path.Combine(path1: FolderName, path2: fileNameWithoutPath)))
                    {
                        filesAreSelected = true;
                        break;
                    }
                }
            }

            if (filesAreSelected && drFavouriteData != null)
            {
                foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                {
                    string fileNameWithoutPath = lvi.Text;
                    if (File.Exists(path: Path.Combine(path1: FolderName, path2: fileNameWithoutPath)))
                    {
                        foreach (string favouriteTag in AncillaryListsArrays.GetFavouriteTags())
                        {
                            string settingId = favouriteTag;
                            string settingValue = drFavouriteData[columnName: settingId]
                                .ToString();

                            HelperStatic.GenericUpdateAddToDataTable(
                                dt: DtFileDataToWriteStage3ReadyToWrite,
                                fileNameWithoutPath: fileNameWithoutPath,
                                settingId: settingId,
                                settingValue: settingValue
                            );
                        }

                        await HelperStatic.LwvUpdateRowFromDTWriteStage3ReadyToWrite(lvi: lvi);
                    }
                }
            }

            else
            {
                MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_WarningNoItemSelected"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoFavouriteNotValid"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
        }
    }

    /// <summary>
    /// Clears cached data for any selected items if there is any to clear
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void tmi_removeCachedData_Click(object sender,
                                            EventArgs e)
    {
        bool dataHasBeenRemoved = false;
        foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
        {
            try
            {
                string lat = lvi.SubItems[index: lvw_FileList.Columns[key: "clh_GPSLatitude"]
                                              .Index]
                    .Text;
                string lng = lvi.SubItems[index: lvw_FileList.Columns[key: "clh_GPSLongitude"]
                                              .Index]
                    .Text;

                for (int i = DtToponomySessionData.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow dr = DtToponomySessionData.Rows[index: i];
                    if (dr[columnName: "lat"]
                            .ToString() ==
                        lat &&
                        dr[columnName: "lng"]
                            .ToString() ==
                        lng)
                    {
                        dr.Delete();
                }

                    dataHasBeenRemoved = true;
                }

                DtToponomySessionData.AcceptChanges();
            }
            catch
            {
                // nothing
            }
        }

        if (dataHasBeenRemoved)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoCahcedDataRemoved"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoCahcedDataNotRemoved"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
        }
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