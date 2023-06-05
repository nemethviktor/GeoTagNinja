﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExifToolWrapper;
using geoTagNinja;
using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using GeoTagNinja.Properties;
using Microsoft.Web.WebView2.Core;
using NLog;
using NLog.Config;
using NLog.Targets;
using TimeZoneConverter;
using static System.Environment;
using static GeoTagNinja.Model.SourcesAndAttributes;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace GeoTagNinja;

public partial class FrmMainApp : Form
{
    /// <summary>
    ///     The EXIFTool used in this application.
    ///     Note that it must be disposed of (done by Form_Closing)!
    /// </summary>
    private readonly ExifTool _ExifTool = new();

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
    public static DirectoryElementCollection DirectoryElements { get; } = new();

    /// <summary>
    /// The server that receives messages from clients via our named pipe.
    /// </summary>
    private SingleInstance_PipeServer NamedPipeServer = null;

    #region Variables

    internal const string DoubleQuote = "\"";

    public const string ParentFolder = "..";
    public const string NullStringEquivalentGeneric = "-";
    public const string NullStringEquivalentBlank = ""; // fml.
    public const string NullStringEquivalentZero = "0"; // fml.
    public const int NullIntEquivalent = 0;
    public const double NullDoubleEquivalent = 0.0;
    public static readonly DateTime NullDateTimeEquivalent = new DateTime(1, 1, 1, 0, 0, 0);

    internal static DataTable DtLanguageLabels;
    internal static DataTable DtFavourites;

    // CustomCityLogic

    internal static string FolderName;
    internal static string _AppLanguage = "English"; // default to english
    internal static List<string> LstFavourites = new();

    internal static string ShowLocToMapDialogChoice = "default";
    internal FrmSettings FrmSettings;
    internal FrmEditFileData FrmEditFileData;
    internal FrmImportGpx FrmImportGpx;

    internal string _mapHtmlTemplateCode = "";

    internal static bool RemoveGeoDataIsRunning;
    private static bool _StopProcessingRows;

    // this is for copy-paste
    // the elements are: EA, Value, Changed?
    internal static Dictionary<ElementAttribute, Tuple<string, bool>> CopyPoolDict = new();

    // this is for checking if files need to be re-parsed.
    internal static DataTable DtFileDataSeenInThisSession;
    internal static DataTable DtToponomySessionData;

    // these are for storing the inital values of TakenDate and CreateDate. Needed for TimeShift.
    internal static Dictionary<string, string> OriginalTakenDateDict = new();
    internal static Dictionary<string, string> OriginalCreateDateDict = new();

    internal static List<string> filesToEditGUIDStringList = new();

    #endregion


    #region Form/App Related

    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     This is the main Form for the app. This particular section handles the initialisation of the form and loading
    ///     various defaults.
    /// </summary>
    public FrmMainApp()
    {
        #region Define Logging Config

        HelperVariables.UserDataFolderPath = Path.Combine(path1: GetFolderPath(folder: SpecialFolder.ApplicationData), path2: "GeoTagNinja");
        HelperVariables.ResourcesFolderPath = Path.Combine(path1: AppDomain.CurrentDomain.BaseDirectory, path2: "Resources");
        HelperVariables.SSettingsDataBasePath = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "database.sqlite");

        if (!Directory.Exists(path: HelperVariables.UserDataFolderPath))
        {
            Directory.CreateDirectory(path: HelperVariables.UserDataFolderPath);
        }

        // Set up logging
        LoggingConfiguration config = new();

        string logFileLocation = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "logfile.txt");
        if (File.Exists(path: logFileLocation))
        {
            File.Delete(path: logFileLocation);
        }

        FileTarget logfile = new(name: "logfile") { FileName = logFileLocation };
        #if (DEBUG)
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

        int procID = Process.GetCurrentProcess().Id;
        Logger.Info(message: "Constructor: Starting GTN with process ID " + procID.ToString() );
        Logger.Info(message: "Collection mode: " + Program.collectionModeEnabled.ToString());
        if (Program.collectionModeEnabled) Logger.Info(message: "Collection source: " + Program.collectionFileLocation);

        if (Program.singleInstance_Highlander) NamedPipeServer = new SingleInstance_PipeServer(PipeCmd_ShowMessage);

        DirectoryElements.ExifTool = _ExifTool;
        HelperDataOtherDataRelated.GenericCreateDataTables();

        HelperGenericAppStartup.AppStartupCreateDataBaseFile();
        HelperGenericAppStartup.AppStartupWriteDefaultSettings();
        HelperGenericAppStartup.AppStartupReadObjectNamesAndLanguage();
        HelperGenericAppStartup.AppStartupReadCustomCityLogic();
        HelperGenericAppStartup.AppStartupReadAPILanguage();
        HelperGenericAppStartup.AppStartupApplyDefaults();
        HelperDataLanguageTZ.DataReadLanguageDataFromCSV();
        HelperDataLanguageTZ.DataReadCountryCodeDataFromCSV();
        HelperGenericAppStartup.AppStartupCheckWebView2();
        AppStartupInitializeComponentFrmMainApp();
        AppStartupEnableDoubleBuffering();

        FormClosing += FrmMainApp_FormClosing;

        Logger.Info(message: "Constructor: Done");
    }


    /// <summary>
    ///     Handles the initial loading - adds various elements and ensures the app functions.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void FrmMainApp_Load(object sender,
                                       EventArgs e)
    {
        Logger.Info(message: "OnLoad: Starting");
        // icon

        Logger.Trace(message: "Setting Icon");
        Icon = Resources.AppIcon;

        // clear both tables, just in case + generic cleanup
        try
        {
            Logger.Debug(message: "Clear DtFileDataToWriteStage1PreQueue");

            foreach (DirectoryElement dirElemFileToModify in DirectoryElements)
            {
                {
                    foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                    {
                        dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue);
                    }
                }
            }

            Logger.Debug(message: "Clear DtFileDataToWriteStage3ReadyToWrite");
            foreach (DirectoryElement dirElemFileToModify in DirectoryElements)
            {
                {
                    foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                    {
                        dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_FrmMainApp_ErrorClearingFileDataQTables") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
        }

        try
        {
            HelperFileSystemOperators.FsoCleanUpUserFolder();
        }
        catch (Exception ex)
        {
            // not really fatal
            Logger.Error(message: "Error: " + ex.Message);
        }

        // Setup the List View
        try
        {
            lvw_FileList.ReadAndApplySetting(appLanguage: AppLanguage, objectNames: HelperVariables.DtObjectNames);
        }
        catch (Exception ex)
        {
            Logger.Error(message: "Error: " + ex.Message);
            MessageBox.Show(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorResizingColumns") +
                          ex.Message,
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error)
                ;
        }

        // can't log inside.
        Logger.Debug(message: "Run CoreWebView2InitializationCompleted");
        wbv_MapArea.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;

        if (!Program.collectionModeEnabled)
        {
            HelperGenericAppStartup.AppSetupInitialiseStartupFolder(toolStripTextBox: tbx_FolderName);
        }

        // initialise webView2
        await InitialiseWebView();

        // assign labels to objects
        AppStartupAssignLabelsToObjects();

        // load lvwFileList
        lvw_FileList_LoadOrUpdate();

        Logger.Trace(message: "Assign 'Enter' Key behaviour to tbx_lng");
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
        AppStartupPullLastLatLngFromSettings();
        HelperGenericAppStartup.AppStartupPullOverWriteBlankToponomy();
        HelperGenericAppStartup.AppStartupPullToponomyRadiusAndMaxRows();
        NavigateMapGo();

        await HelperAPIVersionCheckers.CheckForNewVersions();

        Logger.Info(message: "OnLoad: Done.");
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
        Logger.Debug(message: "OnClose: Starting");

        NamedPipeServer.stopServing();

        bool dataWriteQueueIsNotEmpty = true;
        foreach (DirectoryElement dirElemFileToModify in DirectoryElements)
        {
            foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
            {
                if (dirElemFileToModify.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite))
                {
                    dataWriteQueueIsNotEmpty = false;
                    break;
                }
            }
        }

        // check if there is any data in the write-Q
        if (!dataWriteQueueIsNotEmpty)
        {
            DialogResult dialogResult = MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_QuestionFileQIsNotEmpty"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Question"),
                buttons: MessageBoxButtons.YesNo,
                icon: MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                while (HelperGenericFileLocking.FileListBeingUpdated || HelperGenericFileLocking.FilesAreBeingSaved)
                {
                    await Task.Delay(millisecondsDelay: 10);
                }

                Logger.Debug(message: "Starting ExifWriteExifToFile");
                await HelperExifWriteSaveToFile.ExifWriteExifToFile();
                HelperGenericFileLocking.FilesAreBeingSaved = false;

                Logger.Debug(message: "Finished ExifWriteExifToFile");
            }
            else if (dialogResult == DialogResult.No)
            {
                Logger.Debug(message: "User Chose not to Save.");
                foreach (DirectoryElement dirElemFileToModify in DirectoryElements)
                {
                    foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                    {
                        dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                    }
                }
            }
        }

        // Write column widths to db
        Logger.Trace(message: "Write column widths to db");
        lvw_FileList.PersistSettings();

        // Write lat/long for future reference to db
        Logger.Trace(message: "Write lat/long for future reference to db [lat/lng]: " + nud_lat.Text + "/" + nud_lng.Text);
        HelperDataApplicationSettings.DataWriteSQLiteSettings(
            tableName: "settings",
            settingTabPage: "generic",
            settingId: "lastLat",
            settingValue: nud_lat.Text
        );
        HelperDataApplicationSettings.DataWriteSQLiteSettings(
            tableName: "settings",
            settingTabPage: "generic",
            settingId: "lastLng",
            settingValue: nud_lng.Text
        );

        // clean up
        Logger.Trace(message: "Set pbx_imagePreview.Image = null");
        pbx_imagePreview.Image = null; // unlocks files. theoretically.

        // Shutdown Exif Tool
        Logger.Debug(message: "OnClose: Dispose EXIF-Tool");
        _ExifTool.Dispose();
        HelperFileSystemOperators.FsoCleanUpUserFolder();
        Logger.Debug(message: "OnClose: Done.");
    }



    private void PipeCmd_ShowMessage(string text)
    {
        MessageBox.Show(
            text: $"Pipe Server has this message:\n{text}",
            caption: "Pipe Server",
            buttons: MessageBoxButtons.OK,
            icon: MessageBoxIcon.Information);
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

        double correctedDblLat = HelperExifDataPointInteractions.GenericCorrectInvalidCoordinate(coordHalfPair: dblLat);
        double correctedDblLng = HelperExifDataPointInteractions.GenericCorrectInvalidCoordinate(coordHalfPair: dblLng);
        nud_lat.Text = correctedDblLat.ToString(provider: CultureInfo.InvariantCulture);
        nud_lng.Text = correctedDblLng.ToString(provider: CultureInfo.InvariantCulture);

        nud_lat.Value = Convert.ToDecimal(value: correctedDblLat, provider: CultureInfo.InvariantCulture);
        nud_lng.Value = Convert.ToDecimal(value: correctedDblLng, provider: CultureInfo.InvariantCulture);
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
        HelperVariables.HsMapMarkers.Clear();
        HelperVariables.HsMapMarkers.Add(item: parseLatLngTextBox());
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
        string strGPSLatitude = nud_lat.Text.Replace(oldChar: ',', newChar: '.');
        string strGPSLongitude = nud_lng.Text.Replace(oldChar: ',', newChar: '.');
        double parsedLat;
        double parsedLng;
        _StopProcessingRows = false;
        GeoResponseToponomy readJsonToponomy = new();

        // lat/long gets written regardless of update-toponomy-choice
        if (double.TryParse(s: strGPSLatitude,
                            style: NumberStyles.Any,
                            provider: CultureInfo.InvariantCulture,
                            result: out parsedLat) &&
            double.TryParse(s: strGPSLongitude,
                            style: NumberStyles.Any,
                            provider: CultureInfo.InvariantCulture,
                            result: out parsedLng))
        {
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                {
                    DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemUniqueID(UniqueID: lvi.SubItems[index: lvw_FileList.Columns[key: "clh_GUID"]
                                                                                                                                  .Index]
                                                                                                           .Text);
                    // don't do folders...
                    if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                    {
                        string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;

                        // check it's not in the read-queue.
                        while (HelperGenericFileLocking.GenericLockCheckLockFile(fileNameWithoutPath: fileNameWithoutPath))
                        {
                            await Task.Delay(millisecondsDelay: 10);
                        }

                        // Latitude
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: ElementAttribute.GPSLatitude,
                                                                     value: strGPSLatitude,
                                                                     version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite, isMarkedForDeletion: false);

                        // Longitude
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: ElementAttribute.GPSLongitude,
                                                                     value: strGPSLongitude,
                                                                     version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite, isMarkedForDeletion: false);
                    }
                }
            }
        }

        if (double.TryParse(s: strGPSLatitude,
                            style: NumberStyles.Any,
                            provider: CultureInfo.InvariantCulture,
                            result: out parsedLat) &&
            double.TryParse(s: strGPSLongitude,
                            style: NumberStyles.Any,
                            provider: CultureInfo.InvariantCulture,
                            result: out parsedLng))
        {
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                if (!ShowLocToMapDialogChoice.Contains(value: "_remember"))
                {
                    // via https://stackoverflow.com/a/17385937/3968494
                    ShowLocToMapDialogChoice = HelperControlAndMessageBoxHandling.ShowDialogWithCheckBox(
                        labelText: HelperDataLanguageTZ.DataReadDTObjectText(
                            objectType: "Label",
                            objectName: "lbl_QuestionAddToponomy"
                        ),
                        caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                        checkboxText: HelperDataLanguageTZ.DataReadDTObjectText(
                            objectType: "CheckBox",
                            objectName: "ckb_QuestionAddToponomyDontAskAgain"
                        ),
                        returnCheckboxText: "_remember",
                        button1Text: HelperDataLanguageTZ.DataReadDTObjectText(
                            objectType: "Button",
                            objectName: "btn_Yes"
                        ),
                        returnButton1Text: "yes",
                        button2Text: HelperDataLanguageTZ.DataReadDTObjectText(
                            objectType: "Button",
                            objectName: "btn_No"
                        ),
                        returnButton2Text: "no"
                    );
                }

                if (ShowLocToMapDialogChoice != "default") // basically user can alt+f4 from the box, which is dumb but nonetheless would break the code.
                {
                    HelperGenericFileLocking.FileListBeingUpdated = true;
                    foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                    {
                        DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemUniqueID(UniqueID: lvi.SubItems[index: lvw_FileList.Columns[key: "clh_GUID"]
                                                                                                                                      .Index]
                                                                                                               .Text);
                        // don't do folders...
                        if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                        {
                            string fileNameWithPath = dirElemFileToModify.FileNameWithPath;
                            string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;

                            while (HelperGenericFileLocking.GenericLockCheckLockFile(fileNameWithoutPath: fileNameWithoutPath))
                            {
                                await Task.Delay(millisecondsDelay: 10);
                            }

                            DataTable dtToponomy = new();
                            DataTable dtAltitude = new();
                            if (ShowLocToMapDialogChoice.Contains(value: "yes"))
                            {
                                lvw_FileList_UpdateTagsFromWeb(strGpsLatitude: strGPSLatitude, strGpsLongitude: strGPSLongitude, lvi: lvi);
                            }
                        }

                        await FileListViewReadWrite.ListViewUpdateRowFromDEStage3ReadyToWrite(lvi: lvi);
                    }

                    HelperGenericFileLocking.FileListBeingUpdated = false;
                }
            }
        }

        // Not logging this.
        FileListViewReadWrite.ListViewCountItemsWithGeoData();
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
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_FrmMainApp_ErrorNavigateMapGoHTMLCode") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
        }

        return (LatCoordinate, LngCoordinate);
    }


    private void updateWebView(IDictionary<string, string> replacements)
    {
        string htmlCode = _mapHtmlTemplateCode;

        // If set, replace arcgis key
        if (HelperVariables.SArcGisApiKey != null)
        {
            htmlCode = htmlCode.Replace(oldValue: "yourApiKey", newValue: HelperVariables.SArcGisApiKey);
        }

        Logger.Trace(message: "HelperStatic.SArcGisApiKey == null: " + (HelperVariables.SArcGisApiKey == null));

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
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewNavigateToStringInHTMLFile") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
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

        HelperVariables.HtmlAddMarker = "";
        double dblMinLat = 180;
        double dblMinLng = 180;
        double dblMaxLat = -180;
        double dblMaxLng = -180;

        // Add markers on map for every marker-item and
        // find viewing rect. for map (min / max of all markers to enclose all of them)
        if (HelperVariables.HsMapMarkers.Count > 0)
        {
            double dLat = 0;
            double dLng = 0;
            foreach ((string strLat, string strLng) locationCoord in HelperVariables.HsMapMarkers)
            {
                // Add marker location
                HelperVariables.HtmlAddMarker += "var marker = L.marker([" + locationCoord.strLat + ", " + locationCoord.strLng + "]).addTo(map).openPopup();" + "\n";

                // Update viewing rectangle if neede
                dLat = double.Parse(s: locationCoord.strLat, provider: CultureInfo.InvariantCulture);
                dLng = double.Parse(s: locationCoord.strLng, provider: CultureInfo.InvariantCulture);
                dblMinLat = Math.Min(val1: dblMinLat, val2: dLat);
                dblMaxLat = Math.Max(val1: dblMaxLat, val2: dLat);
                dblMinLng = Math.Min(val1: dblMinLng, val2: dLng);
                dblMaxLng = Math.Max(val1: dblMaxLng, val2: dLng);

                Logger.Trace(message: "Added marker: strLatCoordinate: " + locationCoord.strLat + " / strLngCoordinate:" + locationCoord.strLng);
            }

            HelperVariables.LastLat = dLat;
            HelperVariables.LastLng = dLng;

            HelperVariables.MinLat = dblMinLat;
            HelperVariables.MinLng = dblMinLng;
            HelperVariables.MaxLat = dblMaxLat;
            HelperVariables.MaxLng = dblMaxLng;
            htmlReplacements.Add(key: "{ HTMLAddMarker }", value: HelperVariables.HtmlAddMarker);
        }
        else
        {
            // No markers added
            htmlReplacements.Add(key: "{ HTMLAddMarker }", value: "");
        }

        Logger.Trace(message: "Added " + HelperVariables.HsMapMarkers.Count + " map markers.");

        htmlReplacements.Add(key: "replaceLat", value: HelperVariables.LastLat.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));
        htmlReplacements.Add(key: "replaceLng", value: HelperVariables.LastLng.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));
        htmlReplacements.Add(key: "replaceMinLat", value: HelperVariables.MinLat.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));
        htmlReplacements.Add(key: "replaceMinLng", value: HelperVariables.MinLng.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));
        htmlReplacements.Add(key: "replaceMaxLat", value: HelperVariables.MaxLat.ToString()
                                 .Replace(oldChar: ',', newChar: '.'));
        htmlReplacements.Add(key: "replaceMaxLng", value: HelperVariables.MaxLng.ToString()
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
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewEnsureCoreWebView2Async") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
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
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewIsWebMessageEnabled") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
        }

        // read the "map.html" file.
        try
        {
            Logger.Trace(message: "Read map.html file");
            _mapHtmlTemplateCode = File.ReadAllText(path: Path.Combine(path1: HelperVariables.ResourcesFolderPath, path2: "map.html"));
            Logger.Trace(message: "Read map.html file OK");
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Read map.html file - Error: " + ex.Message);
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewReadHTMLFile") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
        }

        // Get the ArcGis API Key
        if (HelperVariables.SArcGisApiKey == null)
        {
            Logger.Trace(message: "Replace hard-coded values in the html code - SArcGisApiKey is null");
            HelperVariables.SArcGisApiKey = HelperDataApplicationSettings.DataSelectTbxARCGIS_APIKey_FromSQLite();
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
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewWebMessageReceived") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
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
        while (HelperGenericFileLocking.FileListBeingUpdated || HelperGenericFileLocking.FilesAreBeingSaved)
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
        if (lvw_FileList.SelectedItems.Count > 0)
        {
            Logger.Debug(message: "Starting");

            int fileCount = 0;

            Logger.Trace(message: "Trigger FrmEditFileData");
            FrmEditFileData = new FrmEditFileData();
            FrmEditFileData.lvw_FileListEditImages.Items.Clear();

            Logger.Trace(message: "Add Files To lvw_FileListEditImages");
            ListView lvw = lvw_FileList;
            foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
            {
                DirectoryElement fileDirectoryElement = DirectoryElements.FindElementByItemUniqueID(UniqueID: lvi.SubItems[index: lvw.Columns[key: "clh_GUID"]
                                                                                                                               .Index]
                                                                                                        .Text);
                if (fileDirectoryElement != null)
                {
                    if (File.Exists(path: fileDirectoryElement.FileNameWithPath))
                    {
                        ListViewItem lviFE = new()
                        {
                            Text = fileDirectoryElement.ItemNameWithoutPath
                        };
                        ListViewItem.ListViewSubItem lsu = new()
                        {
                            Name = "clh_GUID",
                            Text = fileDirectoryElement.UniqueID.ToString()
                        };
                        lviFE.SubItems.Add(item: lsu);

                        FrmEditFileData.lvw_FileListEditImages.Items.Add(value: lviFE);
                        fileCount++;
                        Logger.Trace(message: "Added " + lviFE.Text);
                    }
                }
            }

            if (fileCount > 0)
            {
                Logger.Trace(message: "FrmEditFileData Get objectTexts");
                FrmEditFileData.Text = HelperDataLanguageTZ.DataReadDTObjectText(
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
    ///     Handles the tmi_File_ImportGPX_Click event -> Brings up the FrmImportGpx to import track
    ///     files
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tmi_File_ImportGPX_Click(object sender,
                                          EventArgs e)
    {
        FrmImportGpx = new FrmImportGpx();
        FrmImportGpx.Text = HelperDataLanguageTZ.DataReadDTObjectText(
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
        HelperFileSystemOperators.FsoCleanUpUserFolder();
        Application.Exit();
    }

    #endregion


    #region TaskBar Stuff

    /// <summary>
    ///     Handles the tsb_Refresh_lvwFileList_Click event -> checks if there is anything in the write-Q
    ///     ... then cleans up the user-folder and triggers lvw_FileList_LoadOrUpdate
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void tsb_Refresh_lvwFileList_Click(object sender,
                                                     EventArgs e)
    {
        Logger.Debug(message: "Starting");

        HelperVariables.SChangeFolderIsOkay = false;
        await HelperFileSystemOperators.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
        if (HelperVariables.SChangeFolderIsOkay)
        {
            if (!Program.collectionModeEnabled)
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
                        HelperFileSystemOperators.FsoCleanUpUserFolder();
                        FolderName = tbx_FolderName.Text;
                        lvw_FileList_LoadOrUpdate();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                                      messageBoxName: "mbx_FrmMainApp_ErrorInvalidFolder") +
                                  ex.Message,
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Error);
                    }
                }
                else if (tbx_FolderName.Text == SpecialFolder.MyComputer.ToString())
                {
                    lvw_FileList_LoadOrUpdate();
                }

                else
                {
                    MessageBox.Show(
                        text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInvalidFolder"),
                        caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                        buttons: MessageBoxButtons.OK,
                        icon: MessageBoxIcon.Error);
                }
            }
            else
            {
                lvw_FileList_LoadOrUpdate();
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
        HelperVariables.SApiOkay = true;
        _StopProcessingRows = false;
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (frmMainAppInstance != null)
        {
            ListView lvw = frmMainAppInstance.lvw_FileList;
            if (lvw.SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.SelectedItems)
                {
                    DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemUniqueID(UniqueID: lvi.SubItems[index: lvw.Columns[key: "clh_GUID"]
                                                                                                                                  .Index]
                                                                                                           .Text);
                    // don't do folders...
                    if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                    {
                        string fileNameWithPath = dirElemFileToModify.FileNameWithPath;
                        string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;

                        // check it's not in the read-queue.
                        while (HelperGenericFileLocking.GenericLockCheckLockFile(fileNameWithoutPath: fileNameWithoutPath))
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
                            lvw_FileList_UpdateTagsFromWeb(strGpsLatitude: strGpsLatitude, strGpsLongitude: strGpsLongitude, lvi: lvi);
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
        Logger.Debug(message: "Starting");

        HelperVariables.SChangeFolderIsOkay = false;
        await HelperFileSystemOperators.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
        Logger.Trace(message: "SChangeFolderIsOkay: " + HelperVariables.SChangeFolderIsOkay);

        if (HelperVariables.SChangeFolderIsOkay)
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

        HelperVariables.SChangeFolderIsOkay = false;
        await HelperFileSystemOperators.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
        Logger.Trace(message: "SChangeFolderIsOkay: " + HelperVariables.SChangeFolderIsOkay);

        if (HelperVariables.SChangeFolderIsOkay)
        {
            string? tmpStrParent = null;
            string? tmpStrRoot = null;
            // this is a bit derp but alas
            if (tbx_FolderName.Text.EndsWith(value: "\\"))
            {
                try
                {
                    tmpStrParent = HelperFileSystemOperators.FsoGetParent(path: tbx_FolderName.Text);
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
                tbx_FolderName.Text = HelperGenericTypeOperations.Coalesce(tmpStrParent, tmpStrRoot);
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
        filesToEditGUIDStringList.Clear();

        ListView lvw = lvw_FileList;
        foreach (ListViewItem lvi in lvw.SelectedItems)
        {
            filesToEditGUIDStringList.Add(item: lvi.SubItems[index: lvw.Columns[key: "clh_GUID"]
                                                                 .Index]
                                              .Text);
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
        // if user is impatient and hammer-spams the button it could create a very long queue of nothing-useful.
        if (!RemoveGeoDataIsRunning)
        {
            RemoveGeoDataIsRunning = true;
            await HelperExifDataPointInteractions.ExifRemoveLocationData(senderName: "FrmMainApp");
            RemoveGeoDataIsRunning = false;
            FileListViewReadWrite.ListViewCountItemsWithGeoData();
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
            DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemUniqueID(UniqueID: lvi.SubItems[index: lvw_FileList.Columns[key: "clh_GUID"]
                                                                                                                          .Index]
                                                                                                   .Text);
            if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
            {
                validFilesToImport = true;
                break;
            }
        }

        if (validFilesToImport)
        {
            FrmImportGpx frmImportGpx = new();
            frmImportGpx.StartPosition = FormStartPosition.CenterScreen;
            frmImportGpx.ShowDialog();
        }
        else
        {
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmImportGpx_NoFileSelected"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
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
            HelperVariables.SChangeFolderIsOkay = false;
            await HelperFileSystemOperators.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
            if (HelperVariables.SChangeFolderIsOkay)
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
        while (HelperGenericFileLocking.FileListBeingUpdated || HelperGenericFileLocking.FilesAreBeingSaved)
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
            DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemUniqueID(UniqueID: lvi.SubItems[index: lvw_FileList.Columns[key: "clh_GUID"]
                                                                                                                          .Index]
                                                                                                   .Text);
            string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;

            HelperVariables.CurrentAltitude = null;
            HelperVariables.CurrentAltitude = lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                .SubItems[index: lvw_FileList.Columns[key: "clh_GPSAltitude"]
                              .Index]
                .Text.ToString(provider: CultureInfo.InvariantCulture);

            DataTable dtToponomy = HelperExifReadExifData.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strGpsLatitude,
                                                                                               lng: strGpsLongitude,
                                                                                               fileNameWithoutPath: fileNameWithoutPath);
            if (dtToponomy.Rows.Count > 0)
            {
                // Send off to SQL
                List<(ElementAttribute attribute, string toponomyOverwriteVal)> toponomyOverwrites = new()
                {
                    (ElementAttribute.CountryCode, dtToponomy.Rows[index: 0][columnName: "CountryCode"]
                         .ToString()),
                    (ElementAttribute.Country, dtToponomy.Rows[index: 0][columnName: "Country"]
                         .ToString())
                };

                foreach (ElementAttribute attribute in HelperGenericAncillaryListsArrays.ToponomyReplaces())
                {
                    string colName = GetAttributeName(attribute: attribute);
                    string settingVal = HelperExifReadExifData.ReplaceBlankToponomy(settingId: attribute, settingValue: dtToponomy.Rows[index: 0][columnName: colName]
                                                                                        .ToString());
                    toponomyOverwrites.Add(item: (attribute, settingVal));
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
                    if (!TZOffset.StartsWith(value: NullStringEquivalentGeneric))
                    {
                        toponomyOverwrites.Add(item: (ElementAttribute.OffsetTime, "+" + TZOffset));
                    }
                    else
                    {
                        toponomyOverwrites.Add(item: (ElementAttribute.OffsetTime, TZOffset));
                    }
                }
                catch
                {
                    // don't do anything.
                }

                foreach ((ElementAttribute attribute, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
                {
                    dirElemFileToModify.SetAttributeValueAnyType(attribute: toponomyDetail.attribute,
                                                                 value: toponomyDetail.toponomyOverwriteVal,
                                                                 version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite, isMarkedForDeletion: false);

                    string colName = GetAttributeName(attribute: toponomyDetail.attribute);

                    lvi.SubItems[index: lvw_FileList.Columns[key: "clh_" + colName]
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
            else
            {
                string APIHandlingChoice = HelperControlAndMessageBoxHandling.ShowDialogWithCheckBox(
                    labelText: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_QuestionNoRowsFromAPI"),
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Question"),
                    checkboxText: HelperDataLanguageTZ.DataReadDTObjectText(
                        objectType: "CheckBox",
                        objectName: "ckb_QuestionStopProcessingRows"
                    ),
                    returnCheckboxText: "_stopprocessing",
                    button1Text: HelperDataLanguageTZ.DataReadDTObjectText(
                        objectType: "Button",
                        objectName: "btn_Yes"
                    ),
                    returnButton1Text: "yes",
                    button2Text: HelperDataLanguageTZ.DataReadDTObjectText(
                        objectType: "Button",
                        objectName: "btn_No"
                    ),
                    returnButton2Text: "no"
                );

                if (APIHandlingChoice.Contains(value: "yes"))
                {
                    List<(ElementAttribute attribute, string toponomyOverwriteVal)> toponomyOverwrites = new();
                    toponomyOverwrites.Add(item: (ElementAttribute.CountryCode, null));
                    toponomyOverwrites.Add(item: (ElementAttribute.Country, null));

                    foreach (ElementAttribute attribute in HelperGenericAncillaryListsArrays.ToponomyReplaces())
                    {
                        toponomyOverwrites.Add(item: (attribute, null));
                    }

                    foreach ((ElementAttribute attribute, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
                    {
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: toponomyDetail.attribute,
                                                                     value: toponomyDetail.toponomyOverwriteVal,
                                                                     version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite, isMarkedForDeletion: false);

                        string colName = GetAttributeName(attribute: toponomyDetail.attribute);
                        lvi.SubItems[index: lvw_FileList.Columns[key: "clh_" + colName]
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
    ///     Responsible for updating the main listview. For each file depending on the "compatible" or "incompatible" naming
    ///     ... it assigns the outstanding files according to compatibility and then runs
    ///     the respective exiftool commands.
    ///     Also I've introduced a "Please Wait" Form to block the Main Form from being interacted with while the folder is
    ///     refreshing. Soz but needed.
    /// </summary>
    private void lvw_FileList_LoadOrUpdate()
    {
        Logger.Debug(message: "Starting");

        Logger.Trace(message: "Clear lvw_FileList");
        lvw_FileList.ClearData();
        DirectoryElements.Clear();
        Application.DoEvents();
        HelperGenericFileLocking.FilesBeingProcessed.Clear();
        RemoveGeoDataIsRunning = false;

        #region FrmPleaseWaitBox

        Logger.Trace(message: "Create FrmPleaseWaitBox");

        Form FrmPleaseWaitBox = new()
        {
            ControlBox = false,
            ShowInTaskbar = false,
            Size = new Size(width: 300, height: 40),
            Padding = new Padding(left: 4, top: 2, right: 2, bottom: 4),
            Text = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: "Form",
                objectName: "FrmPleaseWaitBox"
            ),
            StartPosition = FormStartPosition.CenterScreen
        };

        Logger.Trace(message: "Show FrmPleaseWaitBox");
        FrmPleaseWaitBox.Show();
        Logger.Trace(message: "Disable FrmMainApp");
        Enabled = false;

        #endregion

        // Clear Tables that keep track of the current folder...
        Logger.Trace(message: "Clear OriginalTakenDateDict, OriginalCreateDateDict and DtFileDataSeenInThisSession");
        OriginalTakenDateDict.Clear();
        OriginalCreateDateDict.Clear();
        DtFileDataSeenInThisSession.Clear();

        tbx_FolderName.Enabled = !Program.collectionModeEnabled;

        if (Program.collectionModeEnabled)
        {
            Logger.Trace(message: "FolderName: disabled - using collectionModeEnabled");
            tbx_FolderName.Text = @"** collectionMode enabled **"; // point here is that this doesn't exist and as such will block certain operations (like "go up one level"), which is what we want.

            // Load data (and add to DEs)
            DirectoryElements.ParseFolderOrFileListToDEs(folderOrCollectionFileName: Program.collectionFileLocation,
                                                         statusMethod: delegate(string statusText)
                                                         {
                                                             HandlerUpdateLabelText(label: lbl_ParseProgress,
                                                                                    text: statusText);
                                                         },
                                                         collectionModeEnabled: Program.collectionModeEnabled);
        }
        // not collectionModeEnabled
        else
        {
            tbx_FolderName.Enabled = true;

            Logger.Trace(message: "tbx_FolderName.Text: " + tbx_FolderName.Text);
            if (tbx_FolderName.Text != null)
            {
                // this shouldn't really happen but just in case
                Logger.Trace(message: "FolderName: " + FolderName);
                if (FolderName is null)
                {
                    if (!Directory.Exists(path: tbx_FolderName.Text))
                    {
                        tbx_FolderName.Text = @"C:\";
                    }

                    FolderName = tbx_FolderName.Text;
                    Logger.Trace(message: "FolderName [was null, now updated]: " + FolderName);
                }

                // Load data (and add to DEs)
                DirectoryElements.ParseFolderOrFileListToDEs(folderOrCollectionFileName: FolderName,
                                                             statusMethod: delegate(string statusText)
                                                             {
                                                                 HandlerUpdateLabelText(label: lbl_ParseProgress,
                                                                                        text: statusText);
                                                             },
                                                             collectionModeEnabled: Program.collectionModeEnabled);
            }
        }

        // Show Form
        lvw_FileList.ReloadFromDEs(directoryElements: DirectoryElements);

        HelperGenericFileLocking.FileListBeingUpdated = false;
        HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Ready.");
        Logger.Trace(message: "Enable FrmMainApp");
        Enabled = true;
        Logger.Trace(message: "Hide PleaseWaitBox");
        FrmPleaseWaitBox.Hide();

        // Not logging this.
        FileListViewReadWrite.ListViewCountItemsWithGeoData();
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
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_WarningNoItemSelected"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
            return;
        }

        DirectoryElement item_de = (DirectoryElement)item.Tag;
        Logger.Trace(message: "item: " + item.Text);

        switch (item_de.Type)
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
                HelperVariables.SChangeFolderIsOkay = false;
                await HelperFileSystemOperators.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
                if (HelperVariables.SChangeFolderIsOkay)
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
                filesToEditGUIDStringList.Clear();

                ListView lvw = lvw_FileList;
                ListViewItem lvi = lvw.SelectedItems[index: 0];
                filesToEditGUIDStringList.Add(item: lvi.SubItems[index: lvw.Columns[key: "clh_GUID"]
                                                                     .Index]
                                                  .Text);

                Logger.Trace(message: "Add File To lvw_FileListEditImages");
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
            // it's easier to call the create-preview here than in the other one because focusedItems misbehave/I don't quite understand it/them
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                ListViewItem lvi = lvw_FileList.SelectedItems[index: 0];
                DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemUniqueID(UniqueID: lvi.SubItems[index: lvw_FileList.Columns[key: "clh_GUID"]
                                                                                                                              .Index]
                                                                                                       .Text);
                string fileNameWithPath = dirElemFileToModify.FileNameWithPath;

                if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                {
                    await HelperExifReadGetImagePreviews.GenericCreateImagePreview(
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
            HelperVariables.SNowSelectingAllItems = true;

            for (int i = 0; i < lvw_FileList.Items.Count; i++)
            {
                lvw_FileList.Items[index: i]
                    .Selected = true;
                // so because there is no way to do a proper "select all" w/o looping i only want to run the "navigate" (which is triggered on select-state-change at the end)
                if (i == lvw_FileList.Items.Count - 1)
                {
                    HelperVariables.SNowSelectingAllItems = false;
                    FileListViewMapNavigation.ListViewItemClickNavigate();
                    NavigateMapGo();
                }
            }

            // just in case...
            HelperVariables.SNowSelectingAllItems = false;
        }

        // Shift Ctrl C -> copy details
        else if (e.Control && e.Shift && e.KeyCode == Keys.C)
        {
            FileListViewCopyPaste.ListViewCopyGeoData();
        }

        // Shift Ctrl V -> paste details
        else if (e.Control && e.Shift && e.KeyCode == Keys.V)
        {
            FileListViewCopyPaste.ListViewPasteGeoData();
        }

        // Ctrl Enter -> Edit File
        else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Enter)
        {
            filesToEditGUIDStringList.Clear();

            ListView lvw = lvw_FileList;
            foreach (ListViewItem lvi in lvw.SelectedItems)
            {
                filesToEditGUIDStringList.Add(item: lvi.SubItems[index: lvw.Columns[key: "clh_GUID"]
                                                                     .Index]
                                                  .Text);
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
                DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemUniqueID(UniqueID: lvi.SubItems[index: lvw_FileList.Columns[key: "clh_GUID"]
                                                                                                                              .Index]
                                                                                                       .Text);

                // if .. (parent) then do a folder-up
                if (dirElemFileToModify.Type == DirectoryElement.ElementType.ParentDirectory)
                {
                    btn_OneFolderUp_Click(sender: sender, e: EventArgs.Empty);
                }
                // if this is a folder or drive, enter
                else if (dirElemFileToModify.Type == DirectoryElement.ElementType.SubDirectory)
                {
                    // check for outstanding files first and save if user wants
                    HelperVariables.SChangeFolderIsOkay = false;
                    await HelperFileSystemOperators.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
                    if (HelperVariables.SChangeFolderIsOkay)
                    {
                        if (Directory.Exists(path: dirElemFileToModify.FileNameWithPath))
                        {
                            tbx_FolderName.Text = dirElemFileToModify.FileNameWithPath;
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
            while (HelperGenericFileLocking.FileListBeingUpdated || HelperGenericFileLocking.FilesAreBeingSaved)
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
        FrmSettings.Text = HelperDataLanguageTZ.DataReadDTObjectText(
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
        DtFavourites = HelperGenericAppStartup.AppStartupLoadFavourites();
        if (DtFavourites.Rows.Count > 0)
        {
            FrmManageFavourites frmManageFavouritesInstance = new();
            frmManageFavouritesInstance.ShowDialog();
        }
        else
        {
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_NoFavouritesDefined"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Information);
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

                DataTable dtFavourite = HelperDataFavourites.DataReadSQLiteFavourites(structureOnly: true);
                dtFavourite.Clear();
                DataRow drFavourite = dtFavourite.NewRow();
                drFavourite[columnName: "favouriteName"] = favouriteName;

                foreach (ElementAttribute attribute in HelperGenericAncillaryListsArrays.GetFavouriteTags())
                {
                    string colName = GetAttributeName(attribute: attribute);
                    string addStr = lvi.SubItems[index: lvw.Columns[key: "clh_" + colName]
                                                     .Index]
                        .Text.ToString(provider: CultureInfo.InvariantCulture);

                    if (addStr == NullStringEquivalentGeneric)
                    {
                        addStr = "";
                    }

                    drFavourite[columnName: colName] = addStr;
                }

                HelperDataFavourites.DataDeleteSQLiteFavourite(favouriteName: favouriteName);
                HelperDataFavourites.DataWriteSQLiteAddNewFavourite(drFavourite: drFavourite);

                DtFavourites = HelperGenericAppStartup.AppStartupLoadFavourites();
                MessageBox.Show(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoFavouriteSaved"),
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Information);
            }

            else
            {
                MessageBox.Show(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_WarningNoItemSelected"),
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                    messageBoxName: "mbx_FrmMainApp_InfoFavouriteNameCannotBeBlank"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
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
            EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in DtFavourites.AsEnumerable()
                                                               where dataRow.Field<string>(columnName: "favouriteName") == favouriteToLoad
                                                               select dataRow;

            DataRow drFavouriteData = drDataTableData.FirstOrDefault();

            bool filesAreSelected = false;
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                {
                    DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemUniqueID(UniqueID: lvi.SubItems[index: lvw_FileList.Columns[key: "clh_GUID"]
                                                                                                                                  .Index]
                                                                                                           .Text);
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
                    DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemUniqueID(UniqueID: lvi.SubItems[index: lvw_FileList.Columns[key: "clh_GUID"]
                                                                                                                                  .Index]
                                                                                                           .Text);
                    if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                    {
                        foreach (ElementAttribute attribute in HelperGenericAncillaryListsArrays.GetFavouriteTags())
                        {
                            string colName = GetAttributeName(attribute: attribute);
                            string settingValue = drFavouriteData[columnName: colName]
                                .ToString();

                            dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                                                                         value: settingValue,
                                                                         version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite, isMarkedForDeletion: false);
                        }

                        await FileListViewReadWrite.ListViewUpdateRowFromDEStage3ReadyToWrite(lvi: lvi);
                    }
                }
            }

            else
            {
                MessageBox.Show(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_WarningNoItemSelected"),
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoFavouriteNotValid"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Information);
        }
    }

    /// <summary>
    ///     Clears cached data for any selected items if there is any to clear
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
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoCahcedDataRemoved"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoCahcedDataNotRemoved"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Information);
        }
    }

    private void cbx_Favourites_SelectedValueChanged(object sender,
                                                     EventArgs e)
    {
        string favouriteToLoad = cbx_Favourites.Text;

        // pull favs (this needs doing each time as user may have changed it)
        DtFavourites = HelperGenericAppStartup.AppStartupLoadFavourites(clearDropDown: false);
        cbx_Favourites.Text = favouriteToLoad;
        if (LstFavourites.Contains(item: favouriteToLoad))
        {
            EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in DtFavourites.AsEnumerable()
                                                               where dataRow.Field<string>(columnName: "favouriteName") == favouriteToLoad
                                                               select dataRow;

            DataRow drFavouriteData = drDataTableData.FirstOrDefault();

            if (drFavouriteData != null)
            {
                double favLat = double.Parse(s: drFavouriteData[columnName: "GPSLatitude"]
                                                 .ToString(), provider: CultureInfo.InvariantCulture);
                double favLng = double.Parse(s: drFavouriteData[columnName: "GPSLongitude"]
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

                nud_lat.Value = Convert.ToDecimal(value: favLat, provider: CultureInfo.InvariantCulture);
                nud_lng.Value = Convert.ToDecimal(value: favLng, provider: CultureInfo.InvariantCulture);

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
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_NoFavouritesDefined"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Information);
        }
    }


    private void tmi_OpenCoordsInAPI_Click(object sender,
                                           EventArgs e)
    {
        bool selectionIsValid = false;
        bool latParseSuccess = false;
        double dblLat = 0;
        bool lngParseSuccess = false;
        double dblLng = 0;
        ListView lvw = lvw_FileList;

        if (lvw.SelectedItems.Count == 1)
        {
            ListViewItem lvi = lvw_FileList.SelectedItems[index: 0];
            DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemUniqueID(lvi.SubItems[index: lvw.Columns[key: "clh_GUID"]
                                                                                                                .Index]
                                                                                                   .Text);
            if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
            {
                latParseSuccess = double.TryParse(s: lvi.SubItems[index: lvw.Columns[key: "clh_GPSLatitude"]
                                                                      .Index]
                                                      .Text, result: out dblLat);
                lngParseSuccess = double.TryParse(s: lvi.SubItems[index: lvw.Columns[key: "clh_GPSLongitude"]
                                                                      .Index]
                                                      .Text, result: out dblLng);
                if (latParseSuccess && lngParseSuccess)
                {
                    selectionIsValid = true;
                }
            }
        }

        if (!selectionIsValid)
        {
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                    messageBoxName: "mbx_FrmMainApp_WarningTooManyFilesSelected"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
        }
        else
        {
            CultureInfo cIEnUS = new(name: "en-US");
            string SOnlyShowFCodePPL = HelperVariables.SOnlyShowFCodePPL
                ? "&fcode=PPL"
                : "";
            string openAPILink = "http://api.geonames.org/findNearbyPlaceNameJSON?formatted=true&lat=" +
                                 dblLat.ToString(provider: cIEnUS) +
                                 "&lng=" +
                                 dblLng.ToString(provider: cIEnUS) +
                                 "&lang=" +
                                 HelperVariables.APILanguageToUse +
                                 SOnlyShowFCodePPL +
                                 "&style=FULL" +
                                 "&radius=" +
                                 HelperVariables.ToponomyRadiusValue +
                                 "&maxRows=" +
                                 HelperVariables.ToponomyMaxRows +
                                 "&username=" +
                                 HelperVariables.SGeoNamesUserName +
                                 "&password=any";
            Process.Start(fileName: openAPILink);
        }
    }
}

#endregion

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
            .GetProperty(name: "DoubleBuffered", bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic);
        doubleBufferPropertyInfo.SetValue(obj: control, value: enable, index: null);
    }
}