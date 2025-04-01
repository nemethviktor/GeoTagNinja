using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using geoTagNinja;
using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using GeoTagNinja.View.DialogAndMessageBoxes;
using GeoTagNinja.View.EditFileForm;
using GeoTagNinja.View.ListView;
using Microsoft.Web.WebView2.Core;
using Microsoft.WindowsAPICodePack.Taskbar;
using NLog;
using NLog.Config;
using NLog.Targets;
using TimeZoneConverter;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;
using static GeoTagNinja.Model.SourcesAndAttributes;
using HelperControlAndMessageBoxCustomMessageBoxManager =
    GeoTagNinja.Helpers.HelperControlAndMessageBoxCustomMessageBoxManager;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace GeoTagNinja;

[SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
public partial class FrmMainApp : Form
{
#region Constants / Fields / Variables

#region Constants

    internal const string DoubleQuote = "\"";

    internal const string ParentFolder = "..";
    internal const string NullStringEquivalentGeneric = "-";
    internal const string NullStringEquivalentBlank = ""; // fml.
    internal const string NullStringEquivalentZero = "0"; // fml.
    internal const int NullIntEquivalent = 0;
    internal const double NullDoubleEquivalent = 0.0;

    internal static readonly DateTime NullDateTimeEquivalent =
        new(year: 1, month: 1, day: 1, hour: 0, minute: 0, second: 0);

#endregion

#region Fields

    /// <summary>
    ///     The EXIFTool used in this application.
    ///     Note that it must be disposed of (done by Form_Closing)!
    /// </summary>
    private readonly ExifTool _ExifTool = new();

    /// <summary>
    ///     The server that receives messages from clients via our named pipe.
    /// </summary>
    private readonly SingleInstance_PipeServer NamedPipeServer;

    /// <summary>
    ///     These two make the elements of the main listview accessible to other classes.
    /// </summary>
    public ListView.ListViewItemCollection ListViewItems => lvw_FileList.Items;

    public ListView.ColumnHeaderCollection ListViewColumnHeaders => lvw_FileList.Columns;

    /// <summary>
    ///     Returns the currently set application language for localization.
    /// </summary>
    private static string _AppLanguage => AppLanguage;

    /// <summary>
    ///     Returns the list of elements in the currently opened directory.
    /// </summary>
    public static DirectoryElementCollection DirectoryElements { get; } = new();

#endregion

#region Variables

    internal static DataTable DTLanguageMapping;
    internal static DataTable DtFavourites;

    // CustomCityLogic

    internal static string FolderName;
    internal static string AppLanguage; // default to english
    internal static List<string> LstFavourites = new();

    private static bool _showLocToMapDialogChoice = true;
    private static bool _rememberLocToMapDialogChoice;

    // ReSharper disable once InconsistentNaming
    private FrmSettings FrmSettings;

    // ReSharper disable once InconsistentNaming
    internal FrmEditFileData FrmEditFileData;

    // ReSharper disable once InconsistentNaming
    private FrmImportExportGpx FrmImportExportGpx;

    private string _mapHtmlTemplateCode = "";

    internal static bool RemoveGeoDataIsRunning;
    private static bool _StopProcessingRows;

    // this is for copy-paste
    // the elements are: EA, Value, Changed?
    internal static Dictionary<ElementAttribute, Tuple<string, bool>> CopyPoolDict = new();

    // this is for checking if files need to be re-parsed.
    internal static DataTable DTToponomySessionData;

    internal static List<string> filesToEditGUIDStringList = new();

    internal static readonly TaskbarManager TaskbarManagerInstance =
        TaskbarManager.Instance;

    internal CancellationTokenSource _cts = new();
    private CancellationToken _token;
    internal static bool FlatMode;
    private static bool _ignoreFlatMode;

#endregion

#endregion

#region Form/App Related

    internal static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     This is the main Form for the app. This particular section handles the initialisation of the form and loading
    ///     various defaults.
    /// </summary>
    public FrmMainApp()
    {
        _ = InitialiseApplication();

    #region Define Logging Config

        // Set up logging
        LoggingConfiguration config = new();

        string logFileLocation =
            Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "logfile.txt");
        if (File.Exists(path: logFileLocation))
        {
            File.Delete(path: logFileLocation);
        }

        FileTarget logfile = new(name: "logfile") { FileName = logFileLocation };
    #if DEBUG
        config.AddRule(minLevel: LogLevel.Trace, maxLevel: LogLevel.Fatal,
            target: logfile);
    #else
        config.AddRule(minLevel: LogLevel.Info, maxLevel: LogLevel.Fatal, target: logfile);
    #endif

        logfile.Layout =
            @"${longdate}|${level:uppercase=true}|${callsite:includeNamespace=false:includeSourcePath=false:methodName=true}|${message:withexception=true}";
        ConsoleTarget logconsole = new(name: "logconsole");

        config.AddRule(minLevel: LogLevel.Info, maxLevel: LogLevel.Fatal,
            target: logconsole);

        // Apply config           
        LogManager.Configuration = config;

    #endregion

        int procID = Process.GetCurrentProcess()
                            .Id;
        Log.Info(message: $"Constructor: Starting GTN with process ID {procID}");
        Log.Info(message: $"Collection mode: {Program.CollectionModeEnabled}");
        if (Program.CollectionModeEnabled)
        {
            Log.Info(message: $"Collection source: {Program.CollectionFileLocation}");
        }

        if (Program.SingleInstanceHighlander)
        {
            NamedPipeServer =
                new SingleInstance_PipeServer(messageCallback: PipeCmd_ShowMessage);
        }

        DirectoryElements.ExifTool = _ExifTool;


        Log.Info(message: "Constructor: Done");
    }

    /// <summary>
    ///     Async method to force all startup elements into one group, which are then awaited.
    ///     The idea is that user shouldn't see stuff like "tmi_help" changing its shape into Help while waiting for the app to
    ///     boot up.
    /// </summary>
    /// <returns></returns>
    private async Task<Task> InitialiseApplication()
    {
        Visible = false;
        SuspendLayout();
        Task[] tasks =
        [
            HelperDataOtherDataRelated.GenericCreateDataTables(),
            HelperGenericAppStartup.AppStartupCreateDatabaseFile(),
            HelperGenericAppStartup.AppStartupWriteDefaultSettings(),
            HelperGenericAppStartup.AppStartupReadSQLiteTables(),
            HelperGenericAppStartup.AppStartupReadAppLanguage(),
            HelperGenericAppStartup.AppStartupReadCustomCityLogic(),
            HelperGenericAppStartup.AppStartupReadAPILanguage(),
            HelperGenericAppStartup.AppStartupApplyDefaults(),
            HelperDataLanguageTZ.DataReadCountryCodeDataFromCSV(),
            HelperGenericAppStartup.AppStartupCheckWebView2(),
            AppStartupInitializeComponentFrmMainApp(),
            AppStartupSetAppTheme(),
            AppStartupEnableDoubleBuffering()
        ];

        FormClosing += FrmMainApp_FormClosing;
        //AppStartupApplyVisualStyleDefaults();
        ResumeLayout();
        Visible = true;

        await Task.WhenAll(tasks: tasks);
        return Task.CompletedTask;
    }


    /// <summary>
    ///     Handles the initial loading - adds various elements and ensures the app functions.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void FrmMainApp_Load(object sender,
        EventArgs e)
    {
        Log.Info(message: "OnLoad: Starting");
        // icon

        Log.Trace(message: "Setting Icon");

        // clear both tables, just in case + generic cleanup
        try
        {
            Log.Debug(message: "Remove Stage 1 AttributeValues");
            DirectoryElements.CleanupAllDataInStage(
                attributeVersion: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue);
            Log.Debug(message: "Remove Stage 3 AttributeValues");
            DirectoryElements.CleanupAllDataInStage(
                attributeVersion: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorClearingFileDataQTables", captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK);
        }

        try
        {
            HelperFileSystemOperators.FsoCleanUpUserFolder();
        }
        catch (Exception ex)
        {
            // not really fatal
            Log.Error(message: $"Error: {ex.Message}");
        }

        // Setup the List View
        try
        {
            lvw_FileList.View = System.Windows.Forms.View.LargeIcon;
            lvw_FileList.TileSize = new Size(width: 128, height: 128);
            lvw_FileList.OwnerDraw = true;
            lvw_FileList.ReadAndApplySetting(appLanguage: _AppLanguage);
        }
        catch (Exception ex)
        {
            Log.Error(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorResizingColumns", captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK);
        }


        // can't log inside.
        Log.Debug(message: "Run CoreWebView2InitializationCompleted");
        wbv_MapArea.CoreWebView2InitializationCompleted +=
            webView_CoreWebView2InitializationCompleted;

        if (!Program.CollectionModeEnabled)
        {
            HelperGenericAppStartup.AppSetupInitialiseStartupFolder(
                toolStripTextBox: tbx_FolderName);
        }

        // initialise webView2
        await InitialiseWebView();

        // assign labels to objects
        AppStartupAssignLabelsToObjects();

        // load lvwFileList
        lvw_FileList_LoadOrUpdate();

        splitContainerMain.Paint += splitContainerControl_Paint;
        splitContainerMain.Invalidate();

        Log.Trace(message: "Assign 'Enter' Key behaviour to tbx_lng");
        nud_lng.KeyPress += (sndr,
            ev) =>
        {
            if (ev.KeyChar.Equals(obj: (char)13))
            {
                btn_NavigateMapGo.PerformClick();
                ev.Handled = true; // suppress default handling
            }
        };

        HelperGenericAppStartup.AppStartupLoadFavourites();
        HelperGenericAppStartup.AppStartupLoadCustomRules();
        AppStartupGetLastLatLngFromSettings();
        HelperGenericAppStartup.AppStartupGetOverwriteBlankToponomy();
        HelperGenericAppStartup.AppStartupGetToponomyRadiusAndMaxRows();
        Request_Map_NavigateGo();

        await HelperAPIVersionCheckers.CheckForNewVersions();
        LaunchAutoUpdater();
        Log.Info(message: "OnLoad: Done.");
    }

    /// <summary>
    ///     This fires up the autoupdater
    /// </summary>
    private static void LaunchAutoUpdater()
    {
        HelperNonStatic updateHelper = new();
    #if DEBUG
        //AutoUpdater.InstalledVersion = new Version(version: "1.2"); // here for testing only.
    #endif
        AutoUpdater.Synchronous =
            true; // needs to be true otherwise the single pipe instance crashes. (well, I think _that_ crashes, something does.)
        AutoUpdater.ParseUpdateInfoEvent += updateHelper.AutoUpdaterOnParseUpdateInfoEvent;
        AutoUpdater.CheckForUpdateEvent += updateHelper.AutoUpdaterOnCheckForUpdateEvent;

        string updateJsonPath =
            Path.Combine(path1: HelperVariables.UserDataFolderPath,
                path2: "updateJsonData.json");
        AutoUpdater.Start(appCast: Path.Combine(updateJsonPath));
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
        Log.Debug(message: "OnClose: Starting");

        NamedPipeServer.stopServing();

        // this will trigger a write-to-file question/process
        await HelperFileSystemOperators.FsoCheckOutstandingFileDataOkayToChangeFolderAsync(isTheAppClosing: true);

        PerformAppClosingProcedure();
    }

    private void PerformAppClosingProcedure(bool extractNewExifTool = true)
    {
        // Write column widths to db
        Log.Trace(message: "Write column widths to db");
        lvw_FileList.PersistSettings();

        AppClosingPersistData();

        // Clean up
        Log.Trace(message: "Set pbx_imagePreview.Image = null");
        pbx_imagePreview.Image = null; // unlocks files. theoretically.
        HelperDataApplicationSettings.DataDeleteSQLitesettingsCleanup();
        HelperDataApplicationSettings.DataVacuumDatabase();

        // Shut down ExifTool
        Log.Debug(message: "OnClose: Dispose ExifTool");
        _ExifTool.Dispose();

        // Unzip new exiftool version if there is one
        if (File.Exists(path: HelperVariables.ExifToolExePathRoamingTemp) && extractNewExifTool)
        {
            try
            {
                // okay this is a bit silly now but given that the ET distrib is no longer a single-file there's a lot of fuckery to be dealt with
                // the zip file has a structure such as c:\Users\nemet\AppData\Roaming\GeoTagNinja\exiftool-12.89_64.zip\exiftool-12.89_64\exiftool(-k).exe 
                // i swear to all the f...king gods this has been the most useless move i've seen with ET development in the last decade.
                // so we do the following
                // 1: delete exiftool
                File.Delete(path: HelperVariables.ExifToolExePathRoamingPerm);

                // 2: delete the exiftool_files folder and anything in it.
                string exifToolFilesDir =
                    Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "exiftool_files");
                if (Directory.Exists(path: exifToolFilesDir))
                {
                    Directory.Delete(path: exifToolFilesDir, recursive: true);
                }

                // 2b: this shouldn't really happen but anyway:
                string tempExtractDir = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                    path2: Path.GetFileNameWithoutExtension(path: HelperVariables.ExifToolExePathRoamingTemp));
                if (Directory.Exists(path: tempExtractDir))
                {
                    Directory.Delete(path: tempExtractDir, recursive: true);
                }

                // 3: unzip
                ZipFile.ExtractToDirectory(
                    sourceArchiveFileName: HelperVariables.ExifToolExePathRoamingTemp,
                    destinationDirectoryName: HelperVariables.UserDataFolderPath);

                // 4: move to parent
                Directory.Move(
                    sourceDirName: Path.Combine(path1: tempExtractDir, path2: "exiftool_files"),
                    destDirName: Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "exiftool_files"));

                File.Move(sourceFileName: Path.Combine(path1: tempExtractDir, path2: "exiftool(-k).exe"),
                    destFileName: HelperVariables.ExifToolExePathRoamingPerm);
            }
            catch
            {
                // nothing. basically if there's no exiftool.exe in this folder the app will temporarily revert to the prepackaged one.
            }
        }

        // Clean up Roaming folder
        HelperFileSystemOperators.FsoCleanUpUserFolder();
        Log.Debug(message: "OnClose: Done.");
    }

    private void AppClosingPersistData()
    {
        // Write lat/long + visual settings for future reference to db
        Log.Debug(message: "Write lat/long + visual settings for future reference to db");
        List<AppSettingContainer> settingsToWrite = new();
        List<KeyValuePair<string, string>> persistDataSettingsList = new()
        {
            new KeyValuePair<string, string>(key: "lastLat", value: nud_lat.Text),
            new KeyValuePair<string, string>(key: "lastLng", value: nud_lng.Text),
            new KeyValuePair<string, string>(key: "splitContainerMainSplitterDistance",
                value: splitContainerMain.SplitterDistance.ToString(provider: CultureInfo.InvariantCulture)),
            new KeyValuePair<string, string>(key: "splitContainerLeftTopSplitterDistance",
                value: splitContainerLeftTop.SplitterDistance.ToString(provider: CultureInfo.InvariantCulture))
        };
        settingsToWrite.AddRange(collection: persistDataSettingsList.Select(selector: persistDataSetting =>
            new AppSettingContainer
            {
                TableName = "settings", SettingTabPage = "generic", SettingId = persistDataSetting.Key,
                SettingValue = persistDataSetting.Value
            }));
        HelperDataApplicationSettings.DataWriteSQLiteSettings(settingsToWrite: settingsToWrite);

        // Log stuff
        foreach (KeyValuePair<string, string> keyValuePair in persistDataSettingsList)
        {
            Log.Debug(
                message:
                $"Writing setting.settingId {keyValuePair.Key}, setting.settingValue {keyValuePair.Value}.");
        }
    }


    private void PipeCmd_ShowMessage(string text)
    {
        HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(controlName: "",
            captionType: MessageBoxCaption.Information,
            buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information,
            extraMessage: $"Pipe Server has this message:\n{text}",
            textSource: HelperControlAndMessageBoxCustomMessageBoxManager.MessageBoxTextSource.MANUAL);
    }

#endregion

#region Map Stuff

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

        MapWebMessage mapWebMessage = JsonSerializer.Deserialize<MapWebMessage>(json: jsonString);

        string layerName = mapWebMessage?.layer;
        HelperVariables.HTMLDefaultLayer = layerName switch
        {
            "Streets" => "lyr_streets",
            "Satellite" => "lyr_satellite",
            _ => HelperVariables.HTMLDefaultLayer
        };

        string strLat = mapWebMessage?.lat.ToString(provider: CultureInfo.InvariantCulture);
        string strLng = mapWebMessage?.lng.ToString(provider: CultureInfo.InvariantCulture);
        bool isDragged = mapWebMessage is
        {
            isDragged: true
        };
        double.TryParse(s: strLat, style: NumberStyles.Any,
            provider: CultureInfo.InvariantCulture,
            result: out
            double dblLat); // trust me i hate this f...king culture thing as much as possible...
        double.TryParse(s: strLng, style: NumberStyles.Any,
            provider: CultureInfo.InvariantCulture,
            result: out
            double dblLng); // trust me i hate this f...king culture thing as much as possible...
        // if the user zooms out too much they can encounter an "unreal" coordinate.

        double correctedDblLat =
            HelperExifDataPointInteractions.GenericCorrectInvalidCoordinate(
                coordHalfPair: dblLat);
        double correctedDblLng =
            HelperExifDataPointInteractions.GenericCorrectInvalidCoordinate(
                coordHalfPair: dblLng);
        nud_lat.Text = correctedDblLat.ToString(provider: CultureInfo.InvariantCulture);
        nud_lng.Text = correctedDblLng.ToString(provider: CultureInfo.InvariantCulture);

        nud_lat.Value = Convert.ToDecimal(value: correctedDblLat,
            provider: CultureInfo.InvariantCulture);
        nud_lng.Value = Convert.ToDecimal(value: correctedDblLng,
            provider: CultureInfo.InvariantCulture);

        if (isDragged && AskIfUserWantsToSaveDraggedMapData())
        {
            btn_loctToFile.PerformClick();
        }
    }

    /// <summary>
    ///     Needed for the proper functioning of webview2
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void webView_CoreWebView2InitializationCompleted(object sender,
        CoreWebView2InitializationCompletedEventArgs e)
    {
    }

    /// <summary>
    ///     Checks if the user wants to have a "dragged datapoint" actioned to be sent onto selected files.
    /// </summary>
    /// <returns></returns>
    private bool AskIfUserWantsToSaveDraggedMapData()
    {
        return HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBoxWithResult(
            controlName: "mbx_FrmMainApp_QuestionAddDraggedDataPointToFiles", captionType: MessageBoxCaption.Question,
            buttons: MessageBoxButtons.YesNo) == DialogResult.Yes;
    }

    /// <summary>
    ///     Handles the clicking on Go button
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_NavigateMapGo_Click(object sender,
        EventArgs e)
    {
        HelperVariables.LstTrackPath.Clear();
        HelperVariables.HsMapMarkers.Clear();
        HelperVariables.HsMapMarkers.Add(item: ParseLatLngTextBox());
        Request_Map_NavigateGo();
    }

    /// <summary>
    ///     Handles the clicking on "ToFile" button. See comments above re: why we're using strings (culture-related issue)
    ///     This now also handles "btn_loctToFileDestination" click as well.
    /// </summary>
    /// <param name="sender">Name of the button that has been clicked</param>
    /// <param name="e">Unused</param>
    [SuppressMessage(category: "ReSharper", checkId: "PossibleNullReferenceException")]
    private async void btn_loctToFile_Click(object sender,
                                            EventArgs e)
    {
        // convert selected lat/long to str
        string strGPSLatitudeOnTheMap = nud_lat.Text.Replace(oldChar: ',', newChar: '.');
        string strGPSLongitudeOnTheMap = nud_lng.Text.Replace(oldChar: ',', newChar: '.');
        _StopProcessingRows = false;
        GeoResponseToponomy readJsonToponomy = new();

        Button btn = (Button)sender;
        string senderName = btn.Name;

        // lat/long gets written regardless of update-toponomy-choice
        if (double.TryParse(s: strGPSLatitudeOnTheMap,
                style: NumberStyles.Any,
                provider: CultureInfo.InvariantCulture,
                result: out double _) &&
            double.TryParse(s: strGPSLongitudeOnTheMap,
                style: NumberStyles.Any,
                provider: CultureInfo.InvariantCulture,
                result: out double _))
        {
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                HelperGenericFileLocking.FileListBeingUpdated = true;
                foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                {
                    DirectoryElement dirElemFileToModify =
                        lvi.Tag as DirectoryElement;
                    // don't do folders...
                    if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                    {
                        string fileNameWithoutPath =
                            dirElemFileToModify.ItemNameWithoutPath;

                        // check it's not in the read-queue.
                        while (HelperGenericFileLocking.GenericLockCheckLockFile(
                                   fileNameWithoutPath: fileNameWithoutPath))
                        {
                            await Task.Delay(millisecondsDelay: 10);
                        }

                        if (senderName == "btn_loctToFile")
                        {
                            string tmpCoords =
                                $"{strGPSLatitudeOnTheMap};{strGPSLongitudeOnTheMap}" !=
                                ";"
                                    ? $"{strGPSLatitudeOnTheMap};{strGPSLongitudeOnTheMap}"
                                    : "";

                            List<(ElementAttribute attribute, string value)> attributes =
                                new()
                                {
                                    (ElementAttribute.GPSLatitude,
                                     strGPSLatitudeOnTheMap),
                                    (ElementAttribute.GPSLongitude,
                                     strGPSLongitudeOnTheMap),
                                    (ElementAttribute.Coordinates, tmpCoords)
                                };
                            foreach ((ElementAttribute attribute, string value) in
                                     attributes)
                            {
                                dirElemFileToModify.SetAttributeValueAnyType(
                                    attribute: attribute,
                                    value: value,
                                    version: DirectoryElement.AttributeVersion
                                                             .Stage3ReadyToWrite,
                                    isMarkedForDeletion: false);
                            }

                            if (!_rememberLocToMapDialogChoice)
                            {
                                ShowLocToMapDialog();
                            }

                            DataTable dtToponomy = new();
                            DataTable dtAltitude = new();
                            if (_showLocToMapDialogChoice)
                            {
                                lvw_FileList_UpdateTagsFromWeb(
                                    strGpsLatitude: strGPSLatitudeOnTheMap,
                                    strGpsLongitude: strGPSLongitudeOnTheMap, lvi: lvi);
                            }
                        }
                        else if (senderName == "btn_loctToFileDestination")
                        {
                            string tmpCoords =
                                $"{strGPSLatitudeOnTheMap};{strGPSLongitudeOnTheMap}" !=
                                ";"
                                    ? $"{strGPSLatitudeOnTheMap};{strGPSLongitudeOnTheMap}"
                                    : "";

                            List<(ElementAttribute attribute, string value)>
                                attributesAndValues = new()
                                {
                                    (ElementAttribute.GPSDestLatitude,
                                     strGPSLatitudeOnTheMap),
                                    (ElementAttribute.GPSDestLongitude,
                                     strGPSLongitudeOnTheMap),
                                    (ElementAttribute.DestCoordinates, tmpCoords)
                                };
                            foreach ((ElementAttribute attribute, string value) in
                                     attributesAndValues)
                            {
                                dirElemFileToModify.SetAttributeValueAnyType(
                                    attribute: attribute,
                                    value: value,
                                    version: DirectoryElement.AttributeVersion
                                                             .Stage3ReadyToWrite,
                                    isMarkedForDeletion: false);
                            }
                        }

                        await FileListViewReadWrite
                           .ListViewUpdateRowFromDEStage3ReadyToWrite(lvi: lvi);
                    }
                }

                HelperGenericFileLocking.FileListBeingUpdated = false;
            }
        }

        // Not logging this.
        FileListViewReadWrite.ListViewCountItemsWithGeoData();
        return;

        static void ShowLocToMapDialog()
        {
            Dictionary<string, string> checkboxDictionary = new()
            {
                {
                    ReturnControlText(controlName: "ckb_QuestionDontAskAgain",
                        fakeControlType: FakeControlTypes.CheckBox),
                    "_remember"
                }
            };
            Dictionary<string, string> buttonsDictionary = new()
            {
                {
                    ReturnControlText(controlName: "btn_Yes",
                        fakeControlType: FakeControlTypes.Button),
                    "yes"
                },
                {
                    ReturnControlText(controlName: "btn_No",
                        fakeControlType: FakeControlTypes.Button),
                    "no"
                }
            };

            // via https://stackoverflow.com/a/17385937/3968494
            List<string> getLocToMapDialogChoice =
                DialogWithOrWithoutCheckBox.DisplayAndReturnList(
                    labelText: ReturnControlText(controlName: "lbl_QuestionAddToponomy",
                        fakeControlType: FakeControlTypes.Label),
                    caption: ReturnControlText(
                        controlName: MessageBoxCaption.Question.ToString(),
                        fakeControlType: FakeControlTypes.MessageBoxCaption),
                    buttonsDictionary: buttonsDictionary,
                    orientation: "Horizontal", checkboxesDictionary: checkboxDictionary);

            _showLocToMapDialogChoice = getLocToMapDialogChoice.Contains(item: "yes");
            _rememberLocToMapDialogChoice =
                getLocToMapDialogChoice.Contains(item: "_remember");
        }
    }


    /// <summary>
    ///     Parses the tbx_lat and tbx_lng text boxes.
    ///     If the contents is a valid double, returns touple (lat, lng)
    ///     with values as string and dec separator ".".
    ///     Otherwise default "0" is returned for both.
    /// </summary>
    private (string, string) ParseLatLngTextBox()
    {
        Log.Trace(message: "Starting parseLatLngTextBox ...");

        // Default values if text field is empty
        string LatCoordinate = "0";
        string LngCoordinate = "0";

        // Get txtbox contents
        string strLatCoordinate = "";
        string strLngCoordinate = "";
        if (!string.IsNullOrEmpty(value: nud_lat.Text))
        {
            strLatCoordinate = nud_lat.Text.Replace(oldChar: ',', newChar: '.');
        }

        if (!string.IsNullOrEmpty(value: nud_lng.Text))
        {
            strLngCoordinate = nud_lng.Text.Replace(oldChar: ',', newChar: '.');
        }

        // Check if it's a valid double -> otherwise defaults above
        try
        {
            Log.Trace(message: "parseLatLngTextBox");
            // ReSharper disable once NotAccessedOutParameterVariable
            double parsedLat;

            // ReSharper disable once NotAccessedOutParameterVariable
            double parsedLng;

            if (double.TryParse(s: strLatCoordinate, style: NumberStyles.Any,
                    provider: CultureInfo.InvariantCulture,
                    result: out parsedLat) &&
                double.TryParse(s: strLngCoordinate, style: NumberStyles.Any,
                    provider: CultureInfo.InvariantCulture,
                    result: out parsedLng))
            {
                LatCoordinate = strLatCoordinate;
                LngCoordinate = strLngCoordinate;
                Log.Trace(message:
                    $"parseLatLngTextBox OK - LatCoordinate: {strLatCoordinate} - LngCoordinate: {strLngCoordinate}");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorNavigateMapGoHTMLCode",
                captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK,
                extraMessage: ex.Message);
        }

        return (LatCoordinate, LngCoordinate);
    }


    private void UpdateWebView(IDictionary<string, string> replacements)
    {
        string htmlCode = _mapHtmlTemplateCode;

        // If set, replace arcgis key
        if (HelperVariables.UserSettingArcGisApiKey != null)
        {
            htmlCode = htmlCode.Replace(oldValue: "yourApiKey",
                newValue: HelperVariables
                   .UserSettingArcGisApiKey);
        }

        Log.Trace(message:
            $"HelperStatic.UserSettingArcGisApiKey == null: {HelperVariables.UserSettingArcGisApiKey == null}");

        foreach (KeyValuePair<string, string> replacement in replacements)
        {
            Log.Trace(message: string.Format(format: "Replace: {0} -> {1}",
                arg0: replacement.Key,
                arg1: replacement.Value));
            htmlCode =
                htmlCode.Replace(oldValue: replacement.Key, newValue: replacement.Value);
        }

        // show the decoded location on the map
        try
        {
            Log.Trace(message: "Calling wbv_MapArea.NavigateToString");
            wbv_MapArea.NavigateToString(htmlContent: htmlCode);
            Log.Trace(message: "Calling wbv_MapArea.NavigateToString - OK");
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorInitializeWebViewNavigateToStringInHTMLFile",
                captionType: MessageBoxCaption.Error, buttons: MessageBoxButtons.OK, extraMessage: ex.Message);
        }
    }


    /// <summary>
    ///     Handles the navigation to a coordinate on the map. Replaces hard-coded values w/ user-provided ones
    ///     ... and executes the navigation action.
    /// </summary>
    [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
    internal void Request_Map_NavigateGo()
    {
        Log.Info(message: "Starting");

        // Set up replacements
        IDictionary<string, string> htmlReplacements = new Dictionary<string, string>();

        HelperVariables.HTMLAddMarker = "";
        HelperVariables.HTMLCreatePoints = ""; // this is ok as-is, won't break the map if stays so.
        double dblLat = 0;
        double dblLng = 0;
        double dblMinLat = 180;
        double dblMinLng = 180;
        double dblMaxLat = -180;
        double dblMaxLng = -180;

        // Add markers on map for every marker-item and
        // find viewing rect. for map (min / max of all markers to enclose all of them)
        if (HelperVariables.HsMapMarkers.Count > 0 ||
            HelperVariables.LstTrackPath.Count > 0)
        {
            if (HelperVariables.HsMapMarkers.Count > 0)
            {
                foreach ((string strLat, string strLng) locationCoord in HelperVariables.HsMapMarkers)
                {
                    AssignViewingRectangle(locationCoord: locationCoord, addMarker: true);
                }
            }

            htmlReplacements.Add(key: "{ HTMLAddMarker }",
                value: HelperVariables.HTMLAddMarker);

            htmlReplacements.Add(key: " { HTMLSelectDefaultLayer }",
                value: "var map = L.map('map', { layers: [" + HelperVariables.HTMLDefaultLayer + "]});");

            htmlReplacements.Add(key: "{ HTMLSelectFirstLayer }",
                value: HelperVariables.HTMLDefaultLayer == "lyr_streets"
                    ? """
                      var baseMaps = {
                          "Satellite": lyr_satellite,
                          "Streets": lyr_streets
                      };
                      """
                    : """
                      var baseMaps = {
                          "Streets": lyr_streets,
                          "Satellite": lyr_satellite
                      };
                      """);


            if (HelperVariables.LstTrackPath.Count > 0)
            {
                foreach ((string strLat, string strLng) locationCoord in HelperVariables.LstTrackPath)
                {
                    {
                        AssignViewingRectangle(locationCoord: locationCoord, addMarker: false);
                    }
                }
            }

            HelperVariables.LastLat = dblLat;
            HelperVariables.LastLng = dblLng;

            HelperVariables.MinLat = dblMinLat;
            HelperVariables.MinLng = dblMinLng;
            HelperVariables.MaxLat = dblMaxLat;
            HelperVariables.MaxLng = dblMaxLng;
        }
        else
        {
            // No markers added
            htmlReplacements.Add(key: "{ HTMLAddMarker }", value: "");
        }

        Log.Trace(message: $"Added {HelperVariables.HsMapMarkers.Count} map markers.");

        string createPointsStr = "";
        string showLinesStr = "";
        string showPointsStr = "";
        string showFOVStr = "";
        string showDestinationPolyLineStr = "";
        string multiCoordsDefaultStr = """
                                       var #multiCoordsNum# = [
                                           [#multiCoordsList#]
                                       ];
                                       var plArray = [];
                                       for(var i=0; i<#multiCoordsNum#.length; i++) {
                                           plArray.push(L.polyline(#multiCoordsNum#[i]).addTo(map));
                                       }
                                       try{
                                       
                                           L.polylineDecorator(#multiCoordsNum#, {
                                               patterns: [
                                                   {offset: 25, repeat: 50, symbol: L.Symbol.arrowHead({pixelSize: 15, pathOptions: {fillOpacity: 1, weight: 0}})}
                                               ]
                                           }).addTo(map);
                                       }catch(e){
                                        
                                       }
                                       """;
        string mapStyleFilter = HelperVariables.UserSettingMapColourMode switch
        {
            "DarkInverse" =>
                "filter: invert(100%) hue-rotate(180deg) brightness(95%) contrast(90%);",
            "DarkPale" => "filter: brightness(55%) contrast(90%);",
            _ => ""
        };

        string mapStyleCSS = """
                             #map {
                             	border: 3px !important;
                             	border-color: black;
                             	bottom: 0;
                             	height: 100% !important;
                             	left: 0;
                             	position: fixed;
                             	right: 0;
                             	top: 0;
                             	width: 100% !important;
                             	#replaceMe#
                             }
                             """.Replace(oldValue: "#replaceMe#",
            newValue: mapStyleFilter);

        // check there is one and only one DE selected and add ImgDirection if there's any
        // ... or that the gpx-import list has values
        ListView lvw = lvw_FileList;

        Dictionary<string, HashSet<string>>
            dictDestinations =
                new(); // we use a HashSet here because i want to avoid unnecessary duplication

        AddTrackPathDataToDictDestinations(dictDestinations: dictDestinations);

        if (lvw.SelectedItems.Count == 1)
        {
            DirectoryElement directoryElement =
                lvw.SelectedItems[index: 0].Tag as DirectoryElement;
            // ReSharper disable once PossibleNullReferenceException
            double? imgDirection = directoryElement.GetAttributeValue<double>(
                attribute: ElementAttribute.GPSImgDirection,
                version: directoryElement.GetMaxAttributeVersion(
                    attribute: ElementAttribute.GPSImgDirection),
                notFoundValue: null);

            // If the data is copy-pasted then the value itself could be 0 with a mark-for-delete
            // GetMaxAttributeVersion returns null if IsMarkedForDeletion
            if (directoryElement
                   .GetMaxAttributeVersion(
                        attribute: ElementAttribute.GPSImgDirection) ==
                null)
            {
                imgDirection = null;
            }

            double? GPSLatitude = directoryElement.GetAttributeValue<double>(
                attribute: ElementAttribute.GPSLatitude,
                version: directoryElement.GetMaxAttributeVersion(
                    attribute: ElementAttribute.GPSLatitude),
                notFoundValue: null);

            double? GPSLongitude = directoryElement.GetAttributeValue<double>(
                attribute: ElementAttribute.GPSLongitude,
                version: directoryElement.GetMaxAttributeVersion(
                    attribute: ElementAttribute.GPSLongitude),
                notFoundValue: null);

            string GPSLatitudeStr = directoryElement.GetAttributeValueString(
                attribute: ElementAttribute.GPSLatitude,
                version: directoryElement.GetMaxAttributeVersion(
                    attribute: ElementAttribute.GPSLatitude),
                notFoundValue: null, nowSavingExif: false);

            string? GPSLongitudeStr = directoryElement.GetAttributeValueString(
                attribute: ElementAttribute.GPSLongitude,
                version: directoryElement.GetMaxAttributeVersion(
                    attribute: ElementAttribute.GPSLongitude),
                notFoundValue: null, nowSavingExif: false);

            double? FocalLength = directoryElement.GetAttributeValue<double>(
                attribute: ElementAttribute.FocalLength,
                version: directoryElement.GetMaxAttributeVersion(
                    attribute: ElementAttribute.FocalLength),
                notFoundValue: 50);

            double? FocalLengthIn35mmFormat = directoryElement.GetAttributeValue<double>(
                attribute: ElementAttribute.FocalLengthIn35mmFormat,
                version: directoryElement.GetMaxAttributeVersion(
                    attribute: ElementAttribute.FocalLengthIn35mmFormat),
                notFoundValue: 50);

            if (imgDirection != null &&
                GPSLatitude != null &&
                GPSLongitude != null)
            {
                // this is for the line
                (double, double) targetCoordinates =
                        HelperGenericCalculations.CalculateTargetCoordinates(
                            startLatitude: (double)GPSLatitude,
                            startLongitude: (double)GPSLongitude,
                            gpsImgDirection: (double)imgDirection,
                            distance: 10)
                    ;

                // this is for the FOV cone
                createPointsStr = string.Format(format: """
                                                        /* Create points */
                                                        let points = [
                                                            {{
                                                                type: 'point',
                                                                coordinates: L.latLng({0}),
                                                                color: '#27ae60',
                                                                priority: 1,
                                                            }},
                                                            {{
                                                                type: 'point',
                                                                coordinates: L.latLng({1}),
                                                                color: '#f44334',
                                                                priority: 2,
                                                            }},
                                                        ];
                                                        """,
                    arg0: $"{GPSLatitudeStr},{GPSLongitudeStr}",
                    arg1: $"{targetCoordinates.Item1
                                              .ToString(
                                                   provider: CultureInfo
                                                      .InvariantCulture)
                                              .Replace(
                                                   oldChar: ',',
                                                   newChar: '.')},{targetCoordinates.Item2
                                                  .ToString(
                                                       provider: CultureInfo
                                                          .InvariantCulture)
                                                  .Replace(
                                                       oldChar: ',',
                                                       newChar: '.')}");

                showLinesStr = """
                               /* Show lines */
                               const line = points.map(point => point.coordinates);
                               L.polyline(line, {color: '#178a00'}).addTo(map);
                               """;

                showPointsStr = """
                                /* Show points */
                                points.sort((a, b) => a.priority - b.priority);

                                points.forEach(point => {
                                	switch (point.type) {
                                		case 'point':
                                			L.circleMarker(point.coordinates, {
                                				radius: 8,
                                				fillColor: point.color,
                                				fillOpacity: 1,
                                				color: '#fff',
                                				weight: 3,
                                			}).addTo(map);
                                			break;
                                
                                	}
                                });
                                """;

                if (FocalLength != null &&
                    FocalLengthIn35mmFormat != null)
                {
                    (List<(double, double)>, List<(double, double)>) FOVCoordinates =
                            HelperGenericCalculations
                               .CalculateFovCoordinatesFromSensorSize(
                                    startLatitude: (double)GPSLatitude,
                                    startLongitude: (double)GPSLongitude,
                                    gpsImgDirection: (double)imgDirection,
                                    sensorSize: HelperGenericCalculations
                                       .EstimateSensorSize(
                                            focalLength: (double)FocalLength,
                                            focalLengthIn35mmFilm:
                                            (double)FocalLengthIn35mmFormat),
                                    focalLength: (double)FocalLength,
                                    distance: 1)
                        ;
                    showFOVStr = string.Format(format: """
                                                       /* Show polygon/triangle */
                                                       var polygon = L.polygon([
                                                           [{0}],
                                                           [{1}],
                                                           [{2}]
                                                       ]).addTo(map);
                                                       """,
                        arg0: $"{GPSLatitudeStr},{GPSLongitudeStr}",
                        arg1: HelperGenericCalculations
                           .ConvertFOVCoordListsToString(
                                sourceList: FOVCoordinates.Item1),
                        arg2: HelperGenericCalculations
                           .ConvertFOVCoordListsToString(
                                sourceList: FOVCoordinates.Item2));
                }
            }
        }

        // if we have more than one, check if there are any w/ Destination coords
        else
        {
            foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
            {
                DirectoryElement directoryElement = (DirectoryElement)lvi.Tag;
                // probably needs checking but de.Coordinates and de.DestCoordinates don't return anything here, so we're splitting it into four.
                string GPSLatitudeStr = directoryElement.GetAttributeValueString(
                    attribute: ElementAttribute.GPSLatitude,
                    version: directoryElement.GetMaxAttributeVersion(
                        attribute: ElementAttribute.GPSLatitude),
                    notFoundValue: null, nowSavingExif: false);

                string? GPSLongitudeStr = directoryElement.GetAttributeValueString(
                    attribute: ElementAttribute.GPSLongitude,
                    version: directoryElement.GetMaxAttributeVersion(
                        attribute: ElementAttribute.GPSLongitude),
                    notFoundValue: null, nowSavingExif: false);

                string GPSDestLatitudeStr = directoryElement.GetAttributeValueString(
                    attribute: ElementAttribute.GPSDestLatitude,
                    version: directoryElement.GetMaxAttributeVersion(
                        attribute: ElementAttribute.GPSDestLatitude),
                    notFoundValue: null, nowSavingExif: false);

                // If the data is copy-pasted then the value itself could be 0 with a mark-for-delete
                // GetMaxAttributeVersion returns null if IsMarkedForDeletion
                if (directoryElement
                       .GetMaxAttributeVersion(
                            attribute: ElementAttribute.GPSDestLatitude) ==
                    null)
                {
                    GPSDestLatitudeStr = null;
                }

                string? GPSDestLongitudeStr = directoryElement.GetAttributeValueString(
                    attribute: ElementAttribute.GPSDestLongitude,
                    version: directoryElement.GetMaxAttributeVersion(
                        attribute: ElementAttribute.GPSDestLongitude),
                    notFoundValue: null, nowSavingExif: false);

                // If the data is copy-pasted then the value itself could be 0 with a mark-for-delete
                // GetMaxAttributeVersion returns null if IsMarkedForDeletion
                if (directoryElement
                       .GetMaxAttributeVersion(
                            attribute: ElementAttribute.GPSDestLongitude) ==
                    null)
                {
                    GPSDestLongitudeStr = null;
                }

                if (!(string.IsNullOrWhiteSpace(value: GPSLatitudeStr) ||
                      string.IsNullOrWhiteSpace(value: GPSDestLatitudeStr) ||
                      string.IsNullOrWhiteSpace(value: GPSLongitudeStr) ||
                      string.IsNullOrWhiteSpace(value: GPSDestLongitudeStr)))
                {
                    string destCoords =
                        $"[{GPSDestLatitudeStr},{GPSDestLongitudeStr}]";
                    string gpsCoords = $"[{GPSLatitudeStr},{GPSLongitudeStr}]";
                    if (!dictDestinations.ContainsKey(key: destCoords))
                    {
                        dictDestinations[key: destCoords] = new HashSet<string>();
                    }

                    dictDestinations[key: destCoords]
                       .Add(item: gpsCoords);
                }
            }
        }

        if (dictDestinations.Count == 0 &&
            HelperVariables.LstTrackPath.Count == 0)
        {
            showDestinationPolyLineStr = ""; // reset if false alarm
        }
        else
        {
            // build a line of lines
            showDestinationPolyLineStr = BuildDestinationPolyLineStr(multiCoordsDefaultStr: multiCoordsDefaultStr);
        }


        List<(string key, string value)> replacements = new()
        {
            ("replaceLat", HelperVariables.LastLat.ToString()
                                          .Replace(oldChar: ',', newChar: '.')),
            ("replaceLng", HelperVariables.LastLng.ToString()
                                          .Replace(oldChar: ',', newChar: '.')),
            ("replaceMinLat", HelperVariables.MinLat.ToString()
                                             .Replace(oldChar: ',', newChar: '.')),
            ("replaceMinLng", HelperVariables.MinLng.ToString()
                                             .Replace(oldChar: ',', newChar: '.')),
            ("replaceMaxLat", HelperVariables.MaxLat.ToString()
                                             .Replace(oldChar: ',', newChar: '.')),
            ("replaceMaxLng", HelperVariables.MaxLng.ToString()
                                             .Replace(oldChar: ',', newChar: '.')),
            ("{ HTMLMapStyleCSS }", mapStyleCSS),
            ("{ HTMLCreatePoints }", createPointsStr),
            ("{ HTMLShowLines }", showLinesStr),
            ("{ HTMLShowPoints }", showPointsStr),
            ("{ HTMLShowFOVPolygon }", showFOVStr),
            ("{ HTMLShowPolyLine }", showDestinationPolyLineStr)
        };
        foreach ((string key, string value) in replacements)
        {
            htmlReplacements.Add(key: key, value: value);
        }

        UpdateWebView(replacements: htmlReplacements);
        return;

        void AddTrackPathDataToDictDestinations(Dictionary<string, HashSet<string>> dictDestinations)
        {
            if (HelperVariables.LstTrackPath.Count != 0)
            {
                // bit of a trick here and also to make coding easier. 
                // i'm faking HelperVariables.LstTrackPath into dictDestinations

                // dictDestinations:
                // key: "[51.493224,0.121858]"
                // value: "[[51.491087,0.119283], [51.491915,0.121091], [51.493224,0.121858]]"

                // logically then the last item in the list (which is formatted differently) is the key
                string destCoords =
                    $"[{HelperVariables.LstTrackPath.Last().strLat},{HelperVariables.LstTrackPath.Last().strLng}]";
                if (!dictDestinations.ContainsKey(key: destCoords))
                {
                    dictDestinations[key: destCoords] = new HashSet<string>();
                }

                for (int i = 0; i < HelperVariables.LstTrackPath.Count; i++)
                {
                    string gpsCoords =
                        $"[{HelperVariables.LstTrackPath[index: i].strLat},{HelperVariables.LstTrackPath[index: i].strLng}]";
                    dictDestinations[key: destCoords]
                       .Add(item: gpsCoords);
                }
            }
        }

        double GetMinLatitude(double dLat)
        {
            return Math.Min(val1: dblMinLat, val2: dLat);
        }

        double GetMaxLatitude(double dLat)
        {
            return Math.Max(val1: dblMaxLat, val2: dLat);
        }

        double GetMinLongitude(double dLng)
        {
            return Math.Min(val1: dblMinLng, val2: dLng);
        }

        double GetMaxLongitude(double dLng)
        {
            return Math.Max(val1: dblMaxLng, val2: dLng);
        }


        void AssignViewingRectangle((string strLat, string strLng) locationCoord, bool addMarker)
        {
            if (addMarker)
            {
                // Add marker location
                HelperVariables.HTMLAddMarker +=
                    $"var marker = L.marker([{locationCoord.strLat}, {locationCoord.strLng}],{{\ndraggable: true,\nautoPan: true\n}}).addTo(map).openPopup();\n";

                Log.Trace(message:
                    $"Added marker: strLatCoordinate: {locationCoord.strLat} / strLngCoordinate:{locationCoord.strLng}");
            }

            // Update viewing rectangle if needed
            dblLat = double.Parse(s: locationCoord.strLat,
                provider: CultureInfo.InvariantCulture);
            dblLng = double.Parse(s: locationCoord.strLng,
                provider: CultureInfo.InvariantCulture);

            dblMinLat = GetMinLatitude(dLat: dblLat);
            dblMaxLat = GetMaxLatitude(dLat: dblLat);
            dblMinLng = GetMinLongitude(dLng: dblLng);
            dblMaxLng = GetMaxLongitude(dLng: dblLng);
        }

        string BuildDestinationPolyLineStr(string multiCoordsDefaultStr)
        {
            // for each (numbered) destination group...
            for (int dictDestinationCounter = 0;
                 dictDestinationCounter < dictDestinations.Count;
                 dictDestinationCounter++)
            {
                string multiCoordsListStr = "";
                string multiCoordsNum = dictDestinationCounter.ToString();
                string multiCoordsStr = multiCoordsDefaultStr.Replace(
                    oldValue: "#multiCoordsNum#",
                    newValue: $"multiCoords{multiCoordsNum}");
                foreach (string gpsCoord in dictDestinations
                                           .ElementAt(index: dictDestinationCounter)
                                           .Value)
                {
                    multiCoordsListStr += $"{gpsCoord},";
                }

                multiCoordsListStr += dictDestinations
                                     .ElementAt(index: dictDestinationCounter)
                                     .Key;

                multiCoordsStr = multiCoordsStr.Replace(
                    oldValue: "#multiCoordsList#", newValue: multiCoordsListStr);
                showDestinationPolyLineStr += multiCoordsStr;
            }

            return showDestinationPolyLineStr;
        }
    }


    /// <summary>
    ///     Initialises the map in the app and browses to the default or last-used location.
    /// </summary>
    /// <returns></returns>
    private async Task InitialiseWebView()
    {
        Log.Info(message: "Starting");

        // Create Browser Connection
        try
        {
            // silly thing dumps the folder by default right into Program Files where it can't write further due to permission issues
            // need to move it elsewhere.
            Log.Trace(message: "await CoreWebView2Environment");
            CoreWebView2Environment c2Wv = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: Path.GetTempPath(),
                options: new CoreWebView2EnvironmentOptions(
                    additionalBrowserArguments: null, language: "en"));
            await wbv_MapArea.EnsureCoreWebView2Async(environment: c2Wv);
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorInitializeWebViewEnsureCoreWebView2Async",
                captionType: MessageBoxCaption.Error, buttons: MessageBoxButtons.OK, extraMessage: ex.Message);
        }

        // Initialize WebView
        try
        {
            if (wbv_MapArea.CoreWebView2 != null)
            {
                Log.Trace(message: "CoreWebView2.Settings.IsWebMessageEnabled");
                wbv_MapArea.CoreWebView2.Settings.IsWebMessageEnabled = true;
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorInitializeWebViewIsWebMessageEnabled",
                captionType: MessageBoxCaption.Error, buttons: MessageBoxButtons.OK, extraMessage: ex.Message);
        }

        // read the "map.html" file.
        try
        {
            Log.Trace(message: "Read map.html file");
            _mapHtmlTemplateCode = File.ReadAllText(
                path: Path.Combine(path1: HelperVariables.ResourcesFolderPath,
                    path2: "map.html"));
            Log.Trace(message: "Read map.html file OK");
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Read map.html file - Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorInitializeWebViewReadHTMLFile", captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK, extraMessage: ex.Message);
        }

        // Get the ArcGis API Key
        if (string.IsNullOrEmpty(value: HelperVariables.UserSettingArcGisApiKey))
        {
            Log.Trace(
                message:
                "Replace hard-coded values in the html code - UserSettingArcGisApiKey is null");
            HelperVariables.UserSettingArcGisApiKey =
                HelperDataApplicationSettings.DataReadSQLiteSettings(
                    dataTable: HelperVariables.DtHelperDataApplicationSettings,
                    settingTabPage: "tpg_Application",
                    settingId: "tbx_ARCGIS_APIKey",
                    returnBlankIfNull: true);
            Log.Trace(
                message:
                "Replace hard-coded values in the html code - UserSettingArcGisApiKey obtained from SQLite OK");
        }

        // Parse coords from lat/lng text box
        (string LatCoordinate, string LngCoordinate) = ParseLatLngTextBox();

        // Set up replacements
        IDictionary<string, string> htmlReplacements = new Dictionary<string, string>();
        htmlReplacements.Add(key: "replaceLat", value: LatCoordinate);
        htmlReplacements.Add(key: "replaceLng", value: LngCoordinate);

        // Show on Map
        UpdateWebView(replacements: htmlReplacements);

        // Set up event handler for clicks in map
        try
        {
            Log.Trace(message: "wbv_MapArea.WebMessageReceived");
            wbv_MapArea.WebMessageReceived += wbv_MapArea_WebMessageReceived;
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Error:{ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorInitializeWebViewWebMessageReceived",
                captionType: MessageBoxCaption.Error, buttons: MessageBoxButtons.OK, extraMessage: ex.Message);
        }
    }

#endregion

#region File (that is, the "File" menu tree)

    /// <summary>
    ///     Handles the tmi_File_SaveAll_Click event -> triggers the file-save process
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void tmi_File_SaveAll_Click(object sender,
        EventArgs e)
    {
        // i think having an Item active can cause a lock on it
        while (HelperGenericFileLocking.FileListBeingUpdated ||
               HelperGenericFileLocking.FilesAreBeingSaved)
        {
            await Task.Delay(millisecondsDelay: 10);
        }

        lvw_FileList.SelectedItems.Clear();
        // also the problem here is that the exiftoolAsync can still be running and locking the file.

        await HelperExifWriteSaveToFile.ExifWriteExifToFile();
        HelperGenericFileLocking.FilesAreBeingSaved = false;
        //DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
    }

    /// <summary>
    ///     Handles the tmi_File_EditFiles_Click event -> opens the File Edit Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_EditFiles_Click(object sender,
        EventArgs e)
    {
        filesToEditGUIDStringList.Clear();

        ListView lvw = lvw_FileList;
        foreach (ListViewItem lvi in lvw.SelectedItems)
        {
            DirectoryElement directoryElement =
                lvi.Tag as DirectoryElement;

            filesToEditGUIDStringList.Add(
                item: directoryElement.GetAttributeValueString(
                    attribute: ElementAttribute.GUID,
                    version: DirectoryElement.AttributeVersion
                                             .Original, // GUIDs don't change
                    notFoundValue: null, nowSavingExif: false));
        }

        EditFileFormGeneric.ShowFrmEditFileData();
    }

    /// <summary>
    ///     Handles the tmi_File_CopyGeoData_Click event -> triggers LwvCopyGeoData
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_CopyGeoData_Click(object sender,
        EventArgs e)
    {
        FileListViewCopyPaste.ListViewCopyGeoData();
    }

    /// <summary>
    ///     Handles the tmi_File_PasteGeoData_Click event -> triggers LwvPasteGeoData
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_PasteGeoData_Click(object sender,
        EventArgs e)
    {
        FileListViewCopyPaste.ListViewPasteGeoData();
    }

    /// <summary>
    ///     Handles the tmi_File_ImportExportGPX_Click event -> Brings up the FrmImportExportGpx to import track
    ///     files
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_ImportExportGPX_Click(object sender,
        EventArgs e)
    {
        // warn user that this is a bad idea in flatmode.
        if (!FlatMode ||
            (FlatMode && HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBoxWithResult(
                controlName: "mbx_FrmMainApp_QuestionRunImportExportInFlatMode",
                captionType: MessageBoxCaption.Question,
                buttons: MessageBoxButtons.YesNo) == DialogResult.Yes))
        {
            FrmImportExportGpx = new FrmImportExportGpx();
            FrmImportExportGpx.Text = ReturnControlText(
                fakeControlType: FakeControlTypes.Form,
                controlName: "FrmImportExportGpx"
            );
            FrmImportExportGpx.ShowDialog();
        }
    }

    /// <summary>
    ///     Handles the tmi_File_Quit_Click event -> cleans the user-folder then quits the app
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_Quit_Click(object sender,
        EventArgs e)
    {
        HelperFileSystemOperators.FsoCleanUpUserFolder();
        Application.Exit();
    }


    /// <summary>
    ///     Fires if FlatMode is on and the folder is being changed/refreshed.
    /// </summary>
    /// <returns></returns>
    private bool AskIfUserWantsToDisableFlatMode()
    {
        return HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBoxWithResult(
                   controlName: "mbx_FrmMainApp_QuestionDisableFlatMode", captionType: MessageBoxCaption.Question,
                   buttons: MessageBoxButtons.YesNo) ==
               DialogResult.Yes;
    }

    private void tmiFileFlatModeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        FlatMode = !FlatMode;
        _ignoreFlatMode = true;
        tsb_Refresh_lvwFileList.PerformClick();
        _ignoreFlatMode = false;
    }

#endregion

#region FrmMainApp's TaskBar Stuff

    /// <summary>
    ///     Handles the tsb_Refresh_lvwFileList_Click event -> checks if there is anything in the write-Q
    ///     ... then cleans up the user-folder and triggers lvw_FileList_LoadOrUpdate
    ///     Also checks if FlatMode is on and asks the user if they want to turn it off.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void tsb_Refresh_lvwFileList_Click(object sender,
        EventArgs e)
    {
        Log.Info(message: "Starting");

        HelperVariables.OperationChangeFolderIsOkay = false;
        if (FlatMode && !_ignoreFlatMode)
        {
            FlatMode = !AskIfUserWantsToDisableFlatMode();
        }

        await HelperFileSystemOperators
           .FsoCheckOutstandingFileDataOkayToChangeFolderAsync(isTheAppClosing: false);
        if (HelperVariables.OperationChangeFolderIsOkay)
        {
            if (!Program.CollectionModeEnabled)
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
                        // DirectoryElements.Clear();
                        HelperFileSystemOperators.FsoCleanUpUserFolder();
                        FolderName = tbx_FolderName.Text;
                        await lvw_FileList_LoadOrUpdate();
                    }
                    catch (Exception ex)
                    {
                        ShowInvalidFolderMessage(exceptionMessage: ex.Message);
                    }
                }
                else if (tbx_FolderName.Text == Environment.SpecialFolder.MyComputer.ToString())
                {
                    await lvw_FileList_LoadOrUpdate();
                }

                else
                {
                    ShowInvalidFolderMessage();
                }
            }
            else
            {
                await lvw_FileList_LoadOrUpdate();
            }
        }

        return;

        void ShowInvalidFolderMessage(string exceptionMessage = "")
        {
            DialogResult dialogResult = HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBoxWithResult(
                controlName: "mbx_FrmMainApp_ErrorInvalidFolder", captionType: MessageBoxCaption.Question,
                buttons: MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
            {
                tbx_FolderName.Text = @"C:\";
                tbx_FolderName.Select();
                SendKeys.Send(keys: "{ENTER}");
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
        HelperVariables.OperationAPIReturnedOKResponse = true;
        _StopProcessingRows = false;
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (frmMainAppInstance != null)
        {
            ListView lvw = frmMainAppInstance.lvw_FileList;
            if (lvw.SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in
                         frmMainAppInstance.lvw_FileList.SelectedItems)
                {
                    DirectoryElement dirElemFileToModify = lvi.Tag as DirectoryElement;
                    // don't do folders...
                    if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                    {
                        string fileNameWithPath = dirElemFileToModify.FileNameWithPath;
                        string fileNameWithoutPath =
                            dirElemFileToModify.ItemNameWithoutPath;

                        // check it's not in the read-queue.
                        while (HelperGenericFileLocking.GenericLockCheckLockFile(
                                   fileNameWithoutPath: fileNameWithoutPath))
                        {
                            // ReSharper disable once MethodSupportsCancellation
                            await Task.Delay(millisecondsDelay: 10);
                        }

                        string strGpsLatitude = lvi.SubItems[
                                                        index: lvw
                                                              .Columns[
                                                                   key: FileListView.COL_NAME_PREFIX +
                                                                        FileListView.FileListColumns
                                                                           .GPS_LATITUDE]
                                                              .Index]
                                                   .Text.ToString(
                                                        provider: CultureInfo
                                                           .InvariantCulture);
                        string strGpsLongitude = lvi.SubItems[
                                                         index: lvw
                                                               .Columns[
                                                                    key: FileListView.COL_NAME_PREFIX +
                                                                         FileListView.FileListColumns
                                                                            .GPS_LONGITUDE]
                                                               .Index]
                                                    .Text.ToString(
                                                         provider: CultureInfo
                                                            .InvariantCulture);
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
                            lvw_FileList_UpdateTagsFromWeb(
                                strGpsLatitude: strGpsLatitude,
                                strGpsLongitude: strGpsLongitude, lvi: lvi);
                        }
                    }
                }
            }
        }

        //done
        HandlerUpdateLabelText(label: lbl_ParseProgress, text: "");
    }


    /// <summary>
    ///     Generally similar to the above.(btn_Refresh_lvwFileList_Click)
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void btn_ts_Refresh_lvwFileList_Click(object sender,
        EventArgs e)
    {
        Log.Info(message: "Starting");

        HelperVariables.OperationChangeFolderIsOkay = false;
        await HelperFileSystemOperators
           .FsoCheckOutstandingFileDataOkayToChangeFolderAsync(isTheAppClosing: false);
        Log.Trace(message: $"OperationChangeFolderIsOkay: {HelperVariables.OperationChangeFolderIsOkay}");

        if (HelperVariables.OperationChangeFolderIsOkay)
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
        Log.Info(message: "Starting");

        HelperVariables.OperationChangeFolderIsOkay = false;
        await HelperFileSystemOperators
           .FsoCheckOutstandingFileDataOkayToChangeFolderAsync(isTheAppClosing: false);
        Log.Trace(message: $"OperationChangeFolderIsOkay: {HelperVariables.OperationChangeFolderIsOkay}");

        if (HelperVariables.OperationChangeFolderIsOkay)
        {
            string? tmpStrParent = null;
            string? tmpStrRoot = null;
            // this is a bit derp but alas
            if (tbx_FolderName.Text.EndsWith(value: "\\"))
            {
                try
                {
                    tmpStrParent =
                        HelperFileSystemOperators.FsoGetParent(path: tbx_FolderName.Text);
                }
                catch
                {
                    tmpStrParent = HelperGenericTypeOperations.Coalesce(
                        Directory.GetDirectoryRoot(path: tbx_FolderName.Text)
                      , "C:"
                    );
                }

                tmpStrRoot = HelperGenericTypeOperations.Coalesce(
                    Directory.GetDirectoryRoot(path: tbx_FolderName.Text),
                    "C:"
                );
                tbx_FolderName.Text =
                    HelperGenericTypeOperations.Coalesce(tmpStrParent, tmpStrRoot);
            }

            Application.DoEvents();
            FolderName = tbx_FolderName.Text;

            Log.Trace(message: $"FolderName: {FolderName}");

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
        filesToEditGUIDStringList.Clear();

        ListView lvw = lvw_FileList;
        foreach (ListViewItem lvi in lvw.SelectedItems)
        {
            DirectoryElement directoryElement =
                lvi.Tag as DirectoryElement;

            filesToEditGUIDStringList.Add(
                item: directoryElement.GetAttributeValueString(
                    attribute: ElementAttribute.GUID,
                    version: DirectoryElement.AttributeVersion
                                             .Original, // GUIDs don't change
                    notFoundValue: null, nowSavingExif: false));
        }

        EditFileFormGeneric.ShowFrmEditFileData();
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
        try
        {
            // if user is impatient and hammer-spams the button it could create a very long queue of nothing-useful.
            if (!RemoveGeoDataIsRunning)
            {
                RemoveGeoDataIsRunning = true;
                await HelperExifDataPointInteractions.ExifRemoveLocationData(
                    senderName: "FrmMainApp");
                RemoveGeoDataIsRunning = false;
                RemoveCachedData(displayMessage: false); // remove cache. otherwise the app will report silly things.
                FileListViewReadWrite.ListViewCountItemsWithGeoData();
            }
        }
        catch (Exception ex)
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorRemoveGeoDataFailed",
                captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK,
                extraMessage: ex.Message);
        }
    }

    /// <summary>
    ///     Handles the tsb_ImportExportGPX_Click event -> shows the FrmImportExportGpx Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tsb_ImportExportGPX_Click(object sender,
        EventArgs e)
    {
        bool validFilesToImport = false;
        foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
        {
            DirectoryElement dirElemFileToModify =
                lvi.Tag as DirectoryElement;
            if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
            {
                validFilesToImport = true;
                break;
            }
        }

        if (validFilesToImport)
        {
            FrmImportExportGpx frmImportGpx = new();
            frmImportGpx.StartPosition = FormStartPosition.CenterScreen;
            frmImportGpx.ShowDialog();
        }
        else
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmImportExportGpx_NoFileSelected", captionType: MessageBoxCaption.Warning,
                buttons: MessageBoxButtons.OK);
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
            HelperVariables.OperationChangeFolderIsOkay = false;
            await HelperFileSystemOperators
               .FsoCheckOutstandingFileDataOkayToChangeFolderAsync(isTheAppClosing: false);
            if (HelperVariables.OperationChangeFolderIsOkay)
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
        while (HelperGenericFileLocking.FileListBeingUpdated ||
               HelperGenericFileLocking.FilesAreBeingSaved)
        {
            await Task.Delay(millisecondsDelay: 10);
        }

        // i think having an Item active can cause a lock on it
        lvw_FileList.SelectedItems.Clear();
        // also the problem here is that the exiftoolAsync can still be running and locking the file.

        await HelperExifWriteSaveToFile.ExifWriteExifToFile();
        HelperGenericFileLocking.FilesAreBeingSaved = false;
        //DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
    }

#endregion

#region Themeing

    // via https://stackoverflow.com/a/75716080/3968494
    private void ListView_DrawColumnHeader(object sender,
        DrawListViewColumnHeaderEventArgs e)
    {
        Color foreColor = HelperVariables.UserSettingUseDarkMode
            ? Color.White
            : Color.Black;

        Color backColor = HelperVariables.UserSettingUseDarkMode
            ? ColorTranslator.FromHtml(htmlColor: "#404040")
            : SystemColors.ControlDark;

        //Fills one solid background for each cell.
        using (SolidBrush backColorkBrush = new(color: backColor))
        {
            e.Graphics.FillRectangle(brush: backColorkBrush, rect: e.Bounds);
        }

        //Draw the borders for the header around each cell.
        using (Pen foreColorPen = new(color: foreColor))
        {
            e.Graphics.DrawRectangle(pen: foreColorPen, rect: e.Bounds);
        }

        using (SolidBrush foreColorBrush = new(color: foreColor))
        {
            StringFormat stringFormat = GetStringFormat();

            //Do some padding, since these draws right up next to the border for Left/Near.  Will need to change this if you use Right/Far
            Rectangle rect = e.Bounds;
            rect.X += 2;
            e.Graphics.DrawString(s: e.Header.Text, font: e.Font, brush: foreColorBrush,
                layoutRectangle: rect, format: stringFormat);
        }
    }

    private StringFormat GetStringFormat()
    {
        return new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
    }

    private void ListView_DrawItem(object sender,
        DrawListViewItemEventArgs e)
    {
        e.DrawDefault = true;
    }

    private void ListView_DrawSubItem(object sender,
        DrawListViewSubItemEventArgs e)
    {
        e.DrawDefault = true;
    }


    // via https://stackoverflow.com/questions/9260303/how-to-change-menu-hover-color
    private class DarkMenuStripRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuStripRenderer() : base(professionalColorTable: new DarkColours())
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.White;

            base.OnRenderItemText(e: e);
        }
    }


    private class DarkColours : ProfessionalColorTable
    {
        public override Color MenuItemBorder =>
            ColorTranslator.FromHtml(htmlColor: "#BAB9B9");

        public override Color MenuBorder =>
            Color.Silver; //added for changing the menu border

        public override Color MenuItemPressedGradientBegin =>
            ColorTranslator.FromHtml(htmlColor: "#4C4A48");

        public override Color MenuItemPressedGradientEnd =>
            ColorTranslator.FromHtml(htmlColor: "#5F5D5B");

        public override Color ToolStripBorder =>
            ColorTranslator.FromHtml(htmlColor: "#4C4A48");

        public override Color MenuItemSelectedGradientBegin =>
            ColorTranslator.FromHtml(htmlColor: "#4C4A48");

        public override Color MenuItemSelectedGradientEnd =>
            ColorTranslator.FromHtml(htmlColor: "#5F5D5B");

        public override Color MenuItemSelected =>
            ColorTranslator.FromHtml(htmlColor: "#5F5D5B");

        public override Color ToolStripDropDownBackground =>
            ColorTranslator.FromHtml(htmlColor: "#404040");

        public override Color ToolStripGradientBegin =>
            ColorTranslator.FromHtml(htmlColor: "#404040");

        public override Color ToolStripGradientEnd =>
            ColorTranslator.FromHtml(htmlColor: "#404040");

        public override Color ToolStripGradientMiddle =>
            ColorTranslator.FromHtml(htmlColor: "#404040");

        public override Color ImageMarginGradientBegin =>
            ColorTranslator.FromHtml(htmlColor: "#404040");

        public override Color ImageMarginGradientEnd =>
            ColorTranslator.FromHtml(htmlColor: "#404040");

        public override Color ImageMarginGradientMiddle =>
            ColorTranslator.FromHtml(htmlColor: "#404040");

        public override Color ImageMarginRevealedGradientBegin =>
            ColorTranslator.FromHtml(htmlColor: "#404040");

        public override Color ImageMarginRevealedGradientEnd =>
            ColorTranslator.FromHtml(htmlColor: "#404040");

        public override Color ImageMarginRevealedGradientMiddle =>
            ColorTranslator.FromHtml(htmlColor: "#404040");
    }

#endregion

#region lvw_FileList Interaction

    /// <summary>
    ///     Pulls data from the various APIs and fills up the listView
    /// </summary>
    /// <param name="strGpsLatitude">Latitude as string</param>
    /// <param name="strGpsLongitude">Longitude as string</param>
    /// <param name="lvi">ListViewItem in the the main grid</param>
    private void lvw_FileList_UpdateTagsFromWeb(string strGpsLatitude,
        string strGpsLongitude,
        ListViewItem lvi)
    {
        if (!_StopProcessingRows)
        {
            DirectoryElement dirElemFileToModify =
                lvi.Tag as DirectoryElement;
            string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;

            HelperVariables.CurrentAltitude = null;
            HelperVariables.CurrentAltitude = lvw_FileList
                                             .FindItemWithText(text: fileNameWithoutPath)
                                             .SubItems[
                                                  index: lvw_FileList
                                                        .Columns[
                                                             key: FileListView.COL_NAME_PREFIX +
                                                                  FileListView.FileListColumns.GPS_ALTITUDE]
                                                        .Index]
                                             .Text.ToString(
                                                  provider: CultureInfo.InvariantCulture);

            DataTable dtToponomy =
                HelperExifReadExifData.DTFromAPIExifGetToponomyFromWebOrSQL(
                    lat: strGpsLatitude,
                    lng: strGpsLongitude,
                    fileNameWithoutPath: fileNameWithoutPath);
            if (dtToponomy.Rows.Count > 0)
            {
                // Send off to SQL
                List<(ElementAttribute attribute, string toponomyOverwriteVal)>
                    toponomyOverwrites = new()
                    {
                        (ElementAttribute.CountryCode, dtToponomy.Rows[index: 0][columnName: ReturnControlText(
                             fakeControlType: FakeControlTypes.ColumnHeader,
                             controlName: "clh_CountryCode")].ToString()),
                        (ElementAttribute.Country, dtToponomy.Rows[index: 0][columnName: ReturnControlText(
                             fakeControlType: FakeControlTypes.ColumnHeader,
                             controlName: "clh_Country")].ToString())
                    };

                foreach (ElementAttribute attribute in HelperGenericAncillaryListsArrays.ToponomyReplaces())
                {
                    string colName = ReturnControlText(
                        fakeControlType: FakeControlTypes.ColumnHeader,
                        controlName: $"clh_{attribute}");
                    string settingVal = HelperExifReadExifData.ReplaceBlankToponomy(
                        settingId: attribute,
                        settingValue: dtToponomy.Rows[index: 0][columnName: colName]
                                                .ToString());
                    toponomyOverwrites.Add(item: (attribute, settingVal));
                }

                // timeZone is a bit special but that's just how we all love it....not.
                string TZ = dtToponomy.Rows[index: 0][columnName: ReturnControlText(
                    fakeControlType: FakeControlTypes.ColumnHeader,
                    controlName: "clh_timezoneId")].ToString();

                DateTime createDate;
                bool _ = DateTime.TryParse(s: lvi.SubItems[index: lvw_FileList
                                                                 .Columns[
                                                                      key: FileListView.COL_NAME_PREFIX +
                                                                           FileListView.FileListColumns
                                                                              .CREATE_DATE]
                                                                 .Index]
                                                 .Text.ToString(
                                                      provider: CultureInfo
                                                         .InvariantCulture),
                    result: out createDate);

                try
                {
                    string IANATZ = TZConvert.IanaToWindows(ianaTimeZoneName: TZ);
                    string TZOffset;
                    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(id: IANATZ);

                    TZOffset = tst.GetUtcOffset(dateTime: createDate)
                                  .ToString()
                                  .Substring(startIndex: 0, length: tst
                                                                   .GetUtcOffset(dateTime: createDate)
                                                                   .ToString()
                                                                   .Length -
                                                                    3);
                    toponomyOverwrites.Add(
                        item: !TZOffset.StartsWith(value: NullStringEquivalentGeneric)
                            ? (ElementAttribute.OffsetTime, $"+{TZOffset}")
                            : (ElementAttribute.OffsetTime, TZOffset));
                }
                catch
                {
                    // don't do anything.
                }

                foreach ((ElementAttribute attribute, string toponomyOverwriteVal)
                         toponomyDetail in toponomyOverwrites)
                {
                    dirElemFileToModify.SetAttributeValueAnyType(
                        attribute: toponomyDetail.attribute,
                        value: toponomyDetail.toponomyOverwriteVal,
                        version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                        isMarkedForDeletion: false);

                    lvi.SubItems[index: lvw_FileList
                                       .Columns[
                                            key: GetElementAttributesColumnHeader(
                                                attributeToFind:
                                                toponomyDetail.attribute)]
                                       .Index]
                       .Text = toponomyDetail.toponomyOverwriteVal;
                }

                if (lvi.Index % 10 == 0)
                {
                    Application.DoEvents();
                    // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                    lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath);
                }

                HandlerUpdateLabelText(label: lbl_ParseProgress,
                    text: $"Processing: {fileNameWithoutPath}");
                lvw_FileList.UpdateItemColour(directoryElement: dirElemFileToModify, color: Color.Red);
            }
            else
            {
                Dictionary<string, string> checkboxDictionary = new()
                {
                    {
                        ReturnControlText(
                            fakeControlType: FakeControlTypes.CheckBox,
                            controlName: "ckb_QuestionStopProcessingRows"
                        ),
                        "_stopprocessing"
                    }
                };
                Dictionary<string, string> buttonsDictionary = new()
                {
                    {
                        ReturnControlText(
                            fakeControlType: FakeControlTypes.Button,
                            controlName: "btn_Yes"
                        ),
                        "yes"
                    },
                    {
                        ReturnControlText(
                            fakeControlType: FakeControlTypes.Button,
                            controlName: "btn_No"
                        ),
                        "no"
                    }
                };

                // ReSharper disable once InconsistentNaming
                List<string> APIHandlingChoice =
                    DialogWithOrWithoutCheckBox.DisplayAndReturnList(
                        labelText: ReturnControlText(
                            controlName: "mbx_FrmMainApp_QuestionNoRowsFromAPI",
                            fakeControlType: FakeControlTypes.MessageBox),
                        caption: ReturnControlText(
                            controlName: MessageBoxCaption.Question.ToString(),
                            fakeControlType: FakeControlTypes.MessageBoxCaption),
                        buttonsDictionary: buttonsDictionary,
                        orientation: "Horizontal",
                        checkboxesDictionary: checkboxDictionary);

                if (APIHandlingChoice.Contains(value: "yes"))
                {
                    List<(ElementAttribute attribute, string toponomyOverwriteVal)>
                        toponomyOverwrites = new();
                    toponomyOverwrites.Add(item: (ElementAttribute.CountryCode, null));
                    toponomyOverwrites.Add(item: (ElementAttribute.Country, null));

                    foreach (ElementAttribute attribute in
                             HelperGenericAncillaryListsArrays.ToponomyReplaces())
                    {
                        toponomyOverwrites.Add(item: (attribute, null));
                    }

                    foreach ((ElementAttribute attribute, string toponomyOverwriteVal)
                             toponomyDetail in toponomyOverwrites)
                    {
                        dirElemFileToModify.SetAttributeValueAnyType(
                            attribute: toponomyDetail.attribute,
                            value: toponomyDetail.toponomyOverwriteVal,
                            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                            isMarkedForDeletion: false);

                        lvi.SubItems[index: lvw_FileList
                                           .Columns[
                                                key: GetElementAttributesColumnHeader(
                                                    attributeToFind: toponomyDetail
                                                       .attribute)]
                                           .Index]
                           .Text = toponomyDetail.toponomyOverwriteVal;
                    }
                }

                // nothing
                if (APIHandlingChoice.Contains(value: "_stopprocessing"))
                {
                    _StopProcessingRows = true;
                }
            }
        }
    }

    /// <summary>
    ///     Responsible for updating the main listview.
    ///     Also I've introduced a "Please Wait" Form to block the Main Form from being interacted with while the folder is
    ///     refreshing. Soz but needed.
    /// </summary>
    private async Task lvw_FileList_LoadOrUpdate()
    {
        Log.Info(message: "Starting");
        // Initialize a new CancellationTokenSource

        _cts = new CancellationTokenSource();
        _token = _cts.Token;

        Log.Trace(message: "Clear lvw_FileList");
        lvw_FileList.ClearData();
        // DirectoryElements.Clear();
        HelperVariables.LstTrackPath.Clear();
        Application.DoEvents();
        HelperGenericFileLocking.FilesBeingProcessed.Clear();
        RemoveGeoDataIsRunning = false;


    #region FrmPleaseWaitBox

        FrmPleaseWaitBox frmPleaseWaitBox = new();
        Enabled = false;
        frmPleaseWaitBox.Show();

    #endregion

        // Clear Tables that keep track of the current folder...
        Log.Trace(message: "Clear OriginalTakenDateDict and OriginalCreateDateDict");

        tbx_FolderName.Enabled = !Program.CollectionModeEnabled;

        if (Program.CollectionModeEnabled)
        {
            try
            {
                Log.Trace(message: "FolderName: disabled - using collectionModeEnabled");
                tbx_FolderName.Text =
                    @"** collectionMode enabled **"; // point here is that this doesn't exist and as such will block certain operations (like "go up one level"), which is what we want.

                // Load data (and add to DEs)
                await DirectoryElements.ParseFolderOrFileListToDEsAsync(
                    folderOrCollectionFileName: Program.CollectionFileLocation,
                    processSubFolders: false,
                    updateProgressHandler: delegate(string statusText)
                    {
                        Invoke(method: new Action(() =>
                            HandlerUpdateLabelText(label: lbl_ParseProgress, text: statusText)));
                    },
                    collectionModeEnabled: Program.CollectionModeEnabled,
                    cts: _cts
                );
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(value: "Operation canceled by user.");
            }
            finally
            {
                // Ensure `cts` is disposed here
                _cts?.Dispose();
                _cts = null;
            }
        }
        // not collectionModeEnabled
        else
        {
            tbx_FolderName.Enabled = true;

            Log.Trace(message: $"tbx_FolderName.Text: {tbx_FolderName.Text}");
            if (tbx_FolderName.Text != null)
            {
                // this shouldn't really happen but just in case
                Log.Trace(message: $"FolderName: {FolderName}");
                if (FolderName is null)
                {
                    if (!Directory.Exists(path: tbx_FolderName.Text))
                    {
                        tbx_FolderName.Text = @"C:\";
                    }

                    FolderName = tbx_FolderName.Text;
                    Log.Trace(message: $"FolderName [was null, now updated]: {FolderName}");
                }

                // Load data (and add to DEs)
                try
                {
                    await DirectoryElements.ParseFolderOrFileListToDEsAsync(
                        folderOrCollectionFileName: FolderName,
                        processSubFolders: FlatMode,
                        updateProgressHandler: delegate(string statusText)
                        {
                            Invoke(method: new Action(() =>
                                HandlerUpdateLabelText(label: lbl_ParseProgress, text: statusText)));
                        },
                        collectionModeEnabled: Program.CollectionModeEnabled,
                        cts: _cts
                    );
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine(value: "Operation canceled by user.");
                }
                finally
                {
                    // Ensure `cts` is disposed here
                    _cts?.Dispose();
                    _cts = null;
                }
            }
        }

        // Show Form
        lvw_FileList.ReloadFromDEs(directoryElements: DirectoryElements);

        HelperGenericFileLocking.FileListBeingUpdated = false;
        HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Ready.");
        Log.Trace(message: "Enable FrmMainApp");
        frmPleaseWaitBox.Close();
        Log.Trace(message: "Hide PleaseWaitBox");

        // Not logging this.
        FileListViewReadWrite.ListViewCountItemsWithGeoData();
    }

#endregion

#region Events

    /// <summary>
    ///     Handles the lvw_FileList_MouseDoubleClick event -> if user clicked on a folder then enter, if a file then edit
    ///     ... else warn and don't do anything.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void lvw_FileList_MouseDoubleClick(object sender,
        MouseEventArgs e)
    {
        Log.Info(message: "Starting");

        ListViewHitTestInfo info = lvw_FileList.HitTest(x: e.X, y: e.Y);
        ListViewItem item = info.Item;

        if (item == null)
        {
            lvw_FileList.SelectedItems.Clear();
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_WarningNoItemSelected", captionType: MessageBoxCaption.Warning,
                buttons: MessageBoxButtons.OK);
            return;
        }

        DirectoryElement directoryElement = (DirectoryElement)item.Tag;
        Log.Trace(message: $"item: {item.Text}");

        switch (directoryElement.Type)
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
                HelperVariables.OperationChangeFolderIsOkay = false;
                await HelperFileSystemOperators
                   .FsoCheckOutstandingFileDataOkayToChangeFolderAsync(isTheAppClosing: false);
                if (HelperVariables.OperationChangeFolderIsOkay)
                {
                    if (Directory.Exists(
                            path: Path.Combine(path1: tbx_FolderName.Text,
                                path2: item.Text)))
                    {
                        tbx_FolderName.Text =
                            Path.Combine(path1: tbx_FolderName.Text, path2: item.Text);
                    }
                    else
                    {
                        // itemText.Text here will be something like "C_Windows_320GB_M2_nVME (C:\)"
                        // so just extract whatever is in the parentheses
                        tbx_FolderName.Text = $@"{item.Text.Split('(')
                                                      .Last()
                                                      .Split(')')
                                                      .FirstOrDefault()}\";
                    }

                    btn_ts_Refresh_lvwFileList_Click(sender: this, e: EventArgs.Empty);
                }

                break;

            // Edit file
            case DirectoryElement.ElementType.File:
                Log.Trace(message: "Trigger FrmEditFileData");
                filesToEditGUIDStringList.Clear();

                filesToEditGUIDStringList.Add(
                    item: directoryElement.GetAttributeValueString(
                        attribute: ElementAttribute.GUID,
                        version: DirectoryElement.AttributeVersion
                                                 .Original, // GUIDs don't change
                        notFoundValue: null, nowSavingExif: false));

                Log.Trace(message: "Add File To lvw_FileListEditImages");
                EditFileFormGeneric.ShowFrmEditFileData();
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
            FileListViewMapNavigation.ListViewItemClickNavigate();

            lvw_ExifData.Items.Clear();

            // it's easier to call the create-preview here than in the other one because focusedItems misbehave/I don't quite understand it/them
            // also we'll push the DE/Exif data into lvw_ExifData here.
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                ListViewItem lvi = lvw_FileList.SelectedItems[index: 0];
                DirectoryElement dirElemFileToModify =
                    lvw_FileList.SelectedItems[index: 0].Tag as DirectoryElement;

                await UpdateLvwExifDataItems(directoryElement: dirElemFileToModify);
                string fileNameWithPath = dirElemFileToModify.FileNameWithPath;

                if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                {
                    await HelperExifReadGetImagePreviews.GenericCreateImagePreview(
                        directoryElement: dirElemFileToModify,
                        initiator: HelperExifReadGetImagePreviews.Initiator.FrmMainAppPictureBox);
                }
                else
                {
                    pbx_imagePreview.Image = null;
                }
            }

            Request_Map_NavigateGo();
            // pbx_imagePreview.Image = null;
        }
    }

    /// <summary>
    ///     This updates the listview with the information gathered from the files
    /// </summary>
    /// <param name="directoryElement"></param>
    /// <returns></returns>
    private Task UpdateLvwExifDataItems(DirectoryElement directoryElement)
    {
        // fill lvw_ExifData
        foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(
                     enumType: typeof(ElementAttribute)))

        {
            List<ElementAttribute> attributesWithValidOrderIDs =
                Enum.GetValues(enumType: typeof(ElementAttribute))
                    .Cast<ElementAttribute>()
                    .Where(predicate: attribute =>
                         GetElementAttributesOrderID(
                             attributeToFind
                             : attribute) >
                         0)
                    .ToList();

            if (attributesWithValidOrderIDs.Contains(item: attribute))
            {
                ListViewItem lvi = new()
                {
                    Text = GetElementAttributesName(attributeToFind: attribute)
                };
                lvi.SubItems.Add(text: directoryElement.GetAttributeValueString(
                    attribute: attribute,
                    version: DirectoryElement.AttributeVersion.Original,
                    notFoundValue: null, nowSavingExif: false));
                lvi.SubItems.Add(text: directoryElement.GetAttributeValueString(
                    attribute: attribute,
                    version: DirectoryElement.AttributeVersion
                                             .Stage3ReadyToWrite,
                    notFoundValue: null, nowSavingExif: false));
                lvw_ExifData.Items.Add(value: lvi);
            }
        }

        foreach (ColumnHeader columnHeader in lvw_ExifData.Columns)
        {
            columnHeader.Width = -2;
        }

        return Task.CompletedTask;
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
        if (e.Modifiers == Keys.Control &&
            e.KeyCode == Keys.A)
        {
            HelperVariables.OperationNowSelectingAllItems = true;

            for (int i = 0; i < lvw_FileList.Items.Count; i++)
            {
                lvw_FileList.Items[index: i]
                            .Selected = true;
                // so because there is no way to do a proper "select all" w/o looping i only want to run the "navigate" (which is triggered on select-state-change at the end)
                if (i == lvw_FileList.Items.Count - 1)
                {
                    HelperVariables.OperationNowSelectingAllItems = false;
                    FileListViewMapNavigation.ListViewItemClickNavigate();
                    Request_Map_NavigateGo();
                }
            }

            // just in case...
            HelperVariables.OperationNowSelectingAllItems = false;
        }

        // Shift Ctrl C -> copy details
        else if (e.Control &&
                 e.Shift &&
                 e.KeyCode == Keys.C)
        {
            FileListViewCopyPaste.ListViewCopyGeoData();
        }

        // Shift Ctrl V -> paste details
        else if (e.Control &&
                 e.Shift &&
                 e.KeyCode == Keys.V)
        {
            FileListViewCopyPaste.ListViewPasteGeoData();
        }

        // Ctrl Enter -> Edit File
        else if (e.Modifiers == Keys.Control &&
                 e.KeyCode == Keys.Enter)
        {
            filesToEditGUIDStringList.Clear();

            ListView lvw = lvw_FileList;
            foreach (ListViewItem lvi in lvw.SelectedItems)
            {
                DirectoryElement directoryElement =
                    lvi.Tag as DirectoryElement;

                filesToEditGUIDStringList.Add(
                    item: directoryElement.GetAttributeValueString(
                        attribute: ElementAttribute.GUID,
                        version: DirectoryElement.AttributeVersion
                                                 .Original, // GUIDs don't change
                        notFoundValue: null, nowSavingExif: false));
            }

            EditFileFormGeneric.ShowFrmEditFileData();
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
                ListViewItem lvi = lvw_FileList.SelectedItems[index: 0];
                DirectoryElement dirElemFileToModify =
                    lvi.Tag as DirectoryElement;

                // if .. (parent) then do a folder-up
                if (dirElemFileToModify.Type ==
                    DirectoryElement.ElementType.ParentDirectory)
                {
                    btn_OneFolderUp_Click(sender: sender, e: EventArgs.Empty);
                }
                // if this is a folder or drive, enter
                else if (dirElemFileToModify.Type ==
                         DirectoryElement.ElementType.SubDirectory)
                {
                    // check for outstanding files first and save if user wants
                    HelperVariables.OperationChangeFolderIsOkay = false;
                    await HelperFileSystemOperators
                       .FsoCheckOutstandingFileDataOkayToChangeFolderAsync(isTheAppClosing: false);
                    if (HelperVariables.OperationChangeFolderIsOkay)
                    {
                        if (Directory.Exists(path: dirElemFileToModify.FileNameWithPath))
                        {
                            tbx_FolderName.Text = dirElemFileToModify.FileNameWithPath;
                        }

                        btn_ts_Refresh_lvwFileList_Click(
                            sender: this, e: EventArgs.Empty);
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
        else if (e.Control &&
                 e.KeyCode == Keys.S)
        {
            while (HelperGenericFileLocking.FileListBeingUpdated ||
                   HelperGenericFileLocking.FilesAreBeingSaved)
            {
                await Task.Delay(millisecondsDelay: 10);
            }

            // i think having an Item active can cause a lock on it
            lvw_FileList.SelectedItems.Clear();

            await HelperExifWriteSaveToFile.ExifWriteExifToFile();
            HelperGenericFileLocking.FilesAreBeingSaved = false;
            //DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
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


    /// <summary>
    ///     Handles the tmi_Settings_Settings_Click event -> brings up the Settings Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_Settings_Settings_Click(object sender,
        EventArgs e)
    {
        FrmSettings = new FrmSettings();
        FrmSettings.Text = ReturnControlText(
            fakeControlType: FakeControlTypes.Form,
            controlName: "FrmSettings"
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
        DtFavourites = HelperGenericAppStartup.AppStartupLoadFavourites();
        if (DtFavourites.Rows.Count > 0)
        {
            FrmManageFavourites frmManageFavouritesInstance = new();
            frmManageFavouritesInstance.ShowDialog();
        }
        else
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_NoFavouritesDefined", captionType: MessageBoxCaption.Information,
                buttons: MessageBoxButtons.OK);
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

    private void tmi_Help_FeedbackFeatureRequest_Click(object sender,
        EventArgs e)
    {
        Process.Start(
            fileName:
            "https://github.com/nemethviktor/GeoTagNinja/issues/new?template=feature_request.md");
    }

    private void tmi_Help_BugReport_Click(object sender,
        EventArgs e)
    {
        Process.Start(
            fileName:
            "https://github.com/nemethviktor/GeoTagNinja/issues/new?template=bug_report.md");
    }


    private void tsb_FeedbackFeatureRequest_Click(object sender,
        EventArgs e)
    {
        Process.Start(
            fileName:
            "https://github.com/nemethviktor/GeoTagNinja/issues/new?template=feature_request.md");
    }

    private void tsb_BugReport_Click(object sender,
        EventArgs e)
    {
        Process.Start(
            fileName:
            "https://github.com/nemethviktor/GeoTagNinja/issues/new?template=bug_report.md");
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
            label.Invoke(method: (Action)(() =>
                HandlerUpdateLabelText(label: label, text: text)));
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

                DataTable dtFavourite =
                    HelperDataFavourites.DataReadSQLiteFavourites(structureOnly: true);
                dtFavourite.Clear();
                DataRow drFavourite = dtFavourite.NewRow();
                drFavourite[columnName: "favouriteName"] = favouriteName;

                foreach (ElementAttribute attribute in HelperGenericAncillaryListsArrays
                            .GetFavouriteTags())
                {
                    string colName = GetElementAttributesName(attributeToFind: attribute);
                    string addStr = lvi.SubItems[
                                            index: lvw
                                                  .Columns[
                                                       key:
                                                       GetElementAttributesColumnHeader(
                                                           attributeToFind: attribute)]
                                                  .Index]
                                       .Text.ToString(
                                            provider: CultureInfo.InvariantCulture);

                    if (addStr == NullStringEquivalentGeneric)
                    {
                        addStr = "";
                    }

                    drFavourite[columnName: colName] = addStr;
                }

                HelperDataFavourites.DataDeleteSQLiteFavourite(
                    favouriteName: favouriteName);
                HelperDataFavourites.DataWriteSQLiteAddNewFavourite(
                    drFavourite: drFavourite);

                DtFavourites = HelperGenericAppStartup.AppStartupLoadFavourites();
                HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                    controlName: "mbx_FrmMainApp_InfoFavouriteSaved", captionType: MessageBoxCaption.Information,
                    buttons: MessageBoxButtons.OK);
            }

            else
            {
                HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                    controlName: "mbx_FrmMainApp_WarningNoItemSelected", captionType: MessageBoxCaption.Warning,
                    buttons: MessageBoxButtons.OK);
            }
        }
        else
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_InfoFavouriteNameCannotBeBlank", captionType: MessageBoxCaption.Warning,
                buttons: MessageBoxButtons.OK);
        }
    }

    private async void btn_LoadFavourite_Click(object sender,
        EventArgs e)
    {
        string favouriteToLoad = cbx_Favourites.Text;

        // pull favs (this needs doing each time as user may have changed it)

        DtFavourites = HelperGenericAppStartup.AppStartupLoadFavourites();

        if (LstFavourites.Contains(item: favouriteToLoad))
        {
            EnumerableRowCollection<DataRow> drDataTableData =
                from DataRow dataRow in DtFavourites.AsEnumerable()
                where dataRow.Field<string>(columnName: "favouriteName") ==
                      favouriteToLoad
                select dataRow;

            DataRow drFavouriteData = drDataTableData.FirstOrDefault();

            bool filesAreSelected = false;
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                {
                    DirectoryElement dirElemFileToModify =
                        lvi.Tag as DirectoryElement;
                    if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
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
                    DirectoryElement dirElemFileToModify =
                        lvi.Tag as DirectoryElement;
                    if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                    {
                        foreach (ElementAttribute attribute in
                                 HelperGenericAncillaryListsArrays.GetFavouriteTags())
                        {
                            string colName =
                                GetElementAttributesName(attributeToFind: attribute);
                            string settingValue = drFavouriteData[columnName: colName]
                               .ToString();

                            dirElemFileToModify.SetAttributeValueAnyType(
                                attribute: attribute,
                                value: settingValue,
                                version: DirectoryElement.AttributeVersion
                                                         .Stage3ReadyToWrite,
                                isMarkedForDeletion: false);
                        }

                        await FileListViewReadWrite
                           .ListViewUpdateRowFromDEStage3ReadyToWrite(lvi: lvi);
                    }
                }
            }

            else
            {
                HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                    controlName: "mbx_FrmMainApp_WarningNoItemSelected", captionType: MessageBoxCaption.Warning,
                    buttons: MessageBoxButtons.OK);
            }
        }
        else
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_InfoFavouriteNotValid", captionType: MessageBoxCaption.Information,
                buttons: MessageBoxButtons.OK);
        }
    }

    /// <summary>
    ///     Clears cached data for any selected items if there is any to clear
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <param name="displayMessage"></param>
    private void cmi_removeCachedData_Click(object sender,
                                            EventArgs e)
    {
        RemoveCachedData(displayMessage: true);
    }

    /// <summary>
    ///     Removes any (session) cached data for the selected DE(s)
    /// </summary>
    /// <param name="displayMessage"></param>
    private void RemoveCachedData(bool displayMessage)
    {
        bool dataHasBeenRemoved = false;
        foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
        {
            DirectoryElement directoryElement = lvi.Tag as DirectoryElement;
            if (directoryElement.Type == DirectoryElement.ElementType.File)
            {
                try
                {
                    string lat = lvi
                                .SubItems[
                                     index: lvw_FileList
                                           .Columns[
                                                key: FileListView.COL_NAME_PREFIX +
                                                     FileListView.FileListColumns.GPS_LATITUDE].Index].Text;
                    string lng = lvi
                                .SubItems[
                                     index: lvw_FileList
                                           .Columns[
                                                key: FileListView.COL_NAME_PREFIX +
                                                     FileListView.FileListColumns.GPS_LONGITUDE].Index].Text;

                    for (int i = DTToponomySessionData.Rows.Count - 1; i >= 0; i--)
                    {
                        DataRow dr = DTToponomySessionData.Rows[index: i];
                        if (dr[columnName: "lat"].ToString() == lat &&
                            dr[columnName: "lng"].ToString() == lng)
                        {
                            dr.Delete();
                        }

                        dataHasBeenRemoved = true;
                    }

                    DTToponomySessionData.AcceptChanges();

                    // also remove from hash dictionary

                    HelperVariables.FileChecksumDictionary.Remove(key: directoryElement.FileNameWithPath);
                }

                catch
                {
                    // nothing
                }
            }
        }

        if (displayMessage)
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: dataHasBeenRemoved
                    ? "mbx_FrmMainApp_InfoCachedDataRemoved"
                    : "mbx_FrmMainApp_InfoCachedDataNotRemoved", captionType: MessageBoxCaption.Information,
                buttons: MessageBoxButtons.OK);
        }
    }

    private void cbx_Favourites_SelectedValueChanged(object sender,
                                                     EventArgs e)
    {
        string favouriteToLoad = cbx_Favourites.Text;

        // pull favs (this needs doing each time as user may have changed it)
        DtFavourites =
            HelperGenericAppStartup.AppStartupLoadFavourites(clearDropDown: false);
        cbx_Favourites.Text = favouriteToLoad;
        if (LstFavourites.Contains(item: favouriteToLoad))
        {
            EnumerableRowCollection<DataRow> drDataTableData =
                from DataRow dataRow in DtFavourites.AsEnumerable()
                where dataRow.Field<string>(columnName: "favouriteName") ==
                      favouriteToLoad
                select dataRow;

            DataRow drFavouriteData = drDataTableData.FirstOrDefault();

            if (drFavouriteData != null)
            {
                double favLat = double.Parse(s: drFavouriteData[columnName: "GPSLatitude"]
                       .ToString(),
                    provider: CultureInfo.InvariantCulture);
                double favLng = double.Parse(
                    s: drFavouriteData[columnName: "GPSLongitude"]
                       .ToString(), provider: CultureInfo.InvariantCulture);

                // this may be redundant but lazy to check
                char favLatRef = drFavouriteData[columnName: "GPSLatitudeRef"]
                   .ToString()[index: 0];
                char favLngRef = drFavouriteData[columnName: "GPSLongitudeRef"]
                   .ToString()[index: 0];

                if (favLatRef == 'S')
                {
                    favLat = Math.Abs(value: favLat) * -1;
                }

                if (favLngRef == 'W')
                {
                    favLng = Math.Abs(value: favLng) * -1;
                }

                nud_lat.Text = favLat.ToString(provider: CultureInfo.InvariantCulture);
                nud_lng.Text = favLng.ToString(provider: CultureInfo.InvariantCulture);

                nud_lat.Value =
                    Convert.ToDecimal(value: favLat,
                        provider: CultureInfo.InvariantCulture);
                nud_lng.Value =
                    Convert.ToDecimal(value: favLng,
                        provider: CultureInfo.InvariantCulture);

                btn_NavigateMapGo_Click(sender: null, e: null);
            }
        }
    }

    /// <summary>
    ///     Manages favourites
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btn_ManageFavourites_Click(object sender,
        EventArgs e)
    {
        DtFavourites = HelperGenericAppStartup.AppStartupLoadFavourites();
        if (DtFavourites.Rows.Count > 0)
        {
            FrmManageFavourites frmManageFavouritesInstance = new();
            frmManageFavouritesInstance.ShowDialog();
        }
        else
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_NoFavouritesDefined", captionType: MessageBoxCaption.Information,
                buttons: MessageBoxButtons.OK);
        }
    }


    private void cmi_OpenCoordsInAPI_Click(object sender,
        EventArgs e)
    {
        bool selectionIsValid = false;
        string GPSLatStr = NullStringEquivalentGeneric;
        string GPSLngStr = NullStringEquivalentGeneric;
        ListView lvw = lvw_FileList;

        if (lvw.SelectedItems.Count == 1)
        {
            ListViewItem lvi = lvw_FileList.SelectedItems[index: 0];
            DirectoryElement dirElemFileToModify =
                lvi.Tag as DirectoryElement;
            if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
            {
                try
                {
                    GPSLatStr =
                        dirElemFileToModify.GetAttributeValueString(
                            attribute: ElementAttribute.GPSLatitude);
                    GPSLngStr =
                        dirElemFileToModify.GetAttributeValueString(
                            attribute: ElementAttribute.GPSLongitude);
                    selectionIsValid = true;
                }
                catch
                {
                    // nothing
                }
            }
        }

        if (!selectionIsValid)
        {
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_WarningTooManyFilesSelected", captionType: MessageBoxCaption.Warning,
                buttons: MessageBoxButtons.OK);
        }
        else
        {
            CultureInfo cIEnUS = new(name: "en-US");
            string SOnlyShowFCodePPL = HelperVariables.UserSettingOnlyShowFCodePPL
                ? "&fcode=PPL"
                : "";
            string openAPILink =
                $"http://api.geonames.org/findNearbyPlaceNameJSON?formatted=true&lat={GPSLatStr}&lng={GPSLngStr}&lang={HelperVariables.APILanguageToUse}{SOnlyShowFCodePPL}&style=FULL&radius={HelperVariables.ToponomyRadiusValue}&maxRows={HelperVariables.ToponyMaxRowsChoiceOfferCount}&username={HelperVariables.UserSettingGeoNamesUserName}&password=any";
            Process.Start(fileName: openAPILink);
        }
    }

    private void lvw_ExifData_KeyUp(object sender,
        KeyEventArgs e)
    {
        // unused
    }

    private void lvw_ExifData_KeyDown(object sender,
        KeyEventArgs e)
    {
        if (e.Control &&
            e.KeyCode == Keys.C)
        {
            CopySelectedValuesToClipboard();
        }

        return;

        void CopySelectedValuesToClipboard()
        {
            StringBuilder builder = new();
            foreach (ListViewItem lvi in lvw_ExifData.SelectedItems)
            {
                for (int i = 0; i < lvi.SubItems.Count; i++)
                {
                    builder.AppendLine(value: lvi.SubItems[index: i].Text);
                }
            }

            Clipboard.SetText(text: builder.ToString());
        }
    }

    private void tmi_File_DropDownOpening(object sender, EventArgs e)
    {
        tmi_File_FlatMode.Checked = FlatMode;
    }
}

#endregion

#region ControlExtensions

public static class ControlExtensions
{
    /// <summary>
    ///     Makes sure the Control in question gets doubleBufferPropertyInfo enabled/disabled.
    ///     ...Realistically we're using this to assign doubleBufferPropertyInfo = enabled to the main listView.
    ///     ...This helps stop the flickering on updating the various data points and/or rows (Items).
    ///     Also I can't seem to move this off to HelperControlAndMessageBoxHandling
    /// </summary>
    /// <param name="control">The Control that needs the value assigned</param>
    /// <param name="enable">Bool true or false (aka on or off)</param>
    public static void DoubleBuffered(this Control control,
        bool enable)
    {
        PropertyInfo doubleBufferPropertyInfo = control.GetType()
                                                       .GetProperty(
                                                            name: "DoubleBuffered",
                                                            bindingAttr: BindingFlags
                                                                            .Instance |
                                                                         BindingFlags.NonPublic);
        doubleBufferPropertyInfo.SetValue(obj: control, value: enable, index: null);
    }
}

#endregion
