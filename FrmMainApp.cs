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
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoenM.ExifToolLib;
using geoTagNinja;
using GeoTagNinja.Properties;
using Microsoft.Web.WebView2.Core;
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

    #region Variables

    internal static string ResourcesFolderPath = Path.Combine(path1: AppDomain.CurrentDomain.BaseDirectory, path2: "Resources");
    internal static string UserDataFolderPath = Path.Combine(path1: GetFolderPath(folder: SpecialFolder.ApplicationData), path2: "GeoTagNinja");
    internal const string DoubleQuote = "\"";
    private static string LatCoordinate;
    private static string LngCoordinate;
    internal static DataTable ObjectNames;
    internal static DataTable ObjectTagNamesIn;
    internal static DataTable ObjectTagNamesOut;
    internal static string FolderName;
    internal static string AppLanguage = "english"; // default to english 
    private static string ShowLocToMapDialogChoice = "default";
    private FrmSettings FrmSettings;
    private FrmEditFileData FrmEditFileData;
    private FrmImportGpx FrmImportGpx;

    internal readonly AsyncExifTool AsyncExifTool;

    // this is for copy-paste
    internal static DataTable DtFileDataCopyPool;

    // these are for queueing listOfAsyncCompatibleFileNamesWithOutPath up
    internal static DataTable DtFileDataToWriteStage1PreQueue;
    internal static DataTable DtFileDataToWriteStage2QueuePendingSave;
    internal static DataTable DtFileDataToWriteStage3ReadyToWrite;

    // this is for checking if listOfAsyncCompatibleFileNamesWithOutPath need to be re-parsed.
    internal static DataTable DtFilesSeenInThisSession;
    internal static DataTable DtFileDataSeenInThisSession;

    internal static bool RemoveGeoDataIsRunning;

    #endregion

    #region Methods

    #region Form/App Related

    /// <summary>
    ///     This is the main Form for the app. This particular section handles the initialisation of the form and loading
    ///     various defaults.
    /// </summary>
    public FrmMainApp()
    {
        // initialise exifTool - leave as-is

        #region ExifToolConfiguration

        string exifToolExe = Path.Combine(path1: ResourcesFolderPath, path2: "exiftool.exe");

        Encoding exifToolEncoding = Encoding.UTF8;

        List<string> commonArgs = new();

        string customExifToolConfigFile = @".ExifTool_config";

        AsyncExifToolConfiguration asyncExifToolConfiguration = string.IsNullOrWhiteSpace(value: customExifToolConfigFile)
            ? new AsyncExifToolConfiguration(exifToolFullFilename: exifToolExe, exifToolResultEncoding: exifToolEncoding, commonArgs: commonArgs)
            : new AsyncExifToolConfiguration(exifToolFullFilename: exifToolExe, configurationFilename: customExifToolConfigFile, exifToolResultEncoding: exifToolEncoding, commonArgs: commonArgs);

        AsyncExifTool = new AsyncExifTool(configuration: asyncExifToolConfiguration);
        AsyncExifTool.Initialize();

        #endregion

        #region Load Settings

        // load all settings
        try
        {
            Directory.CreateDirectory(path: Path.Combine(path1: GetFolderPath(folder: SpecialFolder.ApplicationData), path2: "GeoTagNinja"));
            HelperStatic.DataCreateSQLiteDB();
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantCreateSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            Application.Exit();
        }

        // write settings for combobox defaults etc
        try
        {
            HelperStatic.DataWriteSQLiteSettingsDefaultSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantWriteSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            Application.Exit();
        }

        // read language and objectnames
        try
        {
            AppLanguage = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "cbx_Language"
            );
            ObjectNames = HelperStatic.DataReadSQLiteObjectMapping(
                tableName: "objectNames",
                orderBy: "sqlOrder"
            );
            ObjectTagNamesIn = HelperStatic.DataReadSQLiteObjectMapping(tableName: "objectTagNames_In");
            ObjectTagNamesOut = HelperStatic.DataReadSQLiteObjectMapping(tableName: "objectTagNames_Out");
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantLoadSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            Application.Exit();
        }

        // get some defaults
        try
        {
            HelperStatic.SArcGisApiKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();
            HelperStatic.SGeoNamesUserName = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_GeoNames_UserName"
            );
            HelperStatic.SGeoNamesPwd = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_GeoNames_Pwd"
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantReadDefaultSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // check webView2 is available
        try
        {
            string version = "";
            version = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantLoadWebView2") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            Application.Exit();
        }

        // InitializeComponent();
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeComponent") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // get double-buffering to avoid flickering listviews
        try
        {
            lvw_FileList.DoubleBuffered(enable: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorDoubleBuffer") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // get objectnames (col names, etc.)
        try
        {
            ObjectNames.DefaultView.Sort = "sqlOrder";
            DataTable dt = ObjectNames.DefaultView.ToTable();
            foreach (DataRow dr in dt.Rows)
            {
                ColumnHeader clh = new();
                clh.Name = "clh_" + dr[columnName: "objectName"];
                lvw_FileList.Columns.Add(value: clh);
            }

            foreach (ColumnHeader clh in lvw_FileList.Columns)
            {
                clh.Text = HelperStatic.DataReadSQLiteObjectText(
                    languageName: AppLanguage,
                    objectType: "ColumnHeader",
                    objectName: clh.Name
                );
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorLanguageFileColumnHeaders") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        #endregion

        FormClosing += FrmMainApp_FormClosing;

        HelperStatic.GenericCreateDataTables();
    }

    /// <summary>
    ///     Handles the initial loading - adds various elements and ensures the app functions.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void FrmMainApp_Load(object sender,
                                       EventArgs e)
    {
        // icon
        Icon = Resources.AppIcon;

        // clear both tables, just in case + generic cleanup
        try
        {
            DtFileDataToWriteStage1PreQueue.Rows.Clear();
            DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorClearingFileDataQTables") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        try
        {
            HelperStatic.FsoCleanUpUserFolder();
        }
        catch
        {
            // ignored
        }

        // resize columns
        try
        {
            VisualReadLvw_FileList_ColWidth(frmMainApp: this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorResizingColumns") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        wbv_MapArea.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;

        #region StartupFolder

        string valStartupFolder = "";
        try
        {
            valStartupFolder = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_Startup_Folder"
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorSettingStartupFolder") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        if (valStartupFolder == null)
        {
            valStartupFolder = GetFolderPath(folder: SpecialFolder.MyPictures);
        }

        if (valStartupFolder.EndsWith(value: "\\"))
        {
            tbx_FolderName.Text = valStartupFolder;
        }
        else
        {
            tbx_FolderName.Text = valStartupFolder + "\\";
        }

        #endregion

        // initialise webview2
        await InitialiseWebView();

        //assign labels to objects

        #region AppLanguage

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (
                cItem.GetType() == typeof(MenuStrip) ||
                cItem.GetType() == typeof(ToolStrip) ||
                cItem.GetType() == typeof(Label) ||
                cItem.GetType() == typeof(Button) ||
                cItem.GetType() == typeof(CheckBox) ||
                cItem.GetType() == typeof(TabPage) ||
                cItem.GetType() == typeof(ToolStripButton)
                // cItem.GetType() == typeof(ToolTip) // tooltips are not controls.
            )
            {
                if (cItem.Name == "lbl_ParseProgress")
                {
                    cItem.Text = cItem.Text = HelperStatic.DataReadSQLiteObjectText(
                        languageName: AppLanguage,
                        objectType: cItem.GetType()
                            .ToString()
                            .Split('.')
                            .Last(),
                        actionType: "Normal",
                        objectName: cItem.Name
                    );
                }
                else if (cItem is ToolStrip)
                {
                    // https://www.codeproject.com/Messages/3329190/How-to-convert-a-Control-into-a-ToolStripButton.aspx
                    ToolStrip ts = cItem as ToolStrip;
                    foreach (ToolStripItem tsi in ts.Items)
                    {
                        ToolStripButton tsb = tsi as ToolStripButton;
                        if (tsb != null)
                        {
                            tsb.ToolTipText = HelperStatic.DataReadSQLiteObjectText(
                                languageName: AppLanguage,
                                objectType: tsb.GetType()
                                    .ToString()
                                    .Split('.')
                                    .Last(),
                                objectName: tsb.Name
                            );
                        }
                    }
                }
                else
                {
                    cItem.Text = HelperStatic.DataReadSQLiteObjectText(
                        languageName: AppLanguage,
                        objectType: cItem.GetType()
                            .ToString()
                            .Split('.')
                            .Last(),
                        objectName: cItem.Name
                    );
                }
            }
        }

        // don't think the menustrip above is working
        List<ToolStripItem> allMenuItems = new();
        foreach (ToolStripItem toolItem in mns_MenuStrip.Items)
        {
            allMenuItems.Add(item: toolItem);
            //add sub items
            allMenuItems.AddRange(collection: helperNonstatic.GetMenuItems(item: toolItem));
        }

        foreach (ToolStripItem cItem in allMenuItems)
        {
            if (cItem is ToolStripMenuItem)
            {
                ToolStripMenuItem tsmi = cItem as ToolStripMenuItem;
                cItem.Text = cItem.Text = HelperStatic.DataReadSQLiteObjectText(
                    languageName: AppLanguage,
                    objectType: cItem.GetType()
                        .ToString()
                        .Split('.')
                        .Last(),
                    objectName: cItem.Name
                );
            }
            else if (cItem is ToolStripSeparator)
            {
                ToolStripSeparator tss = cItem as ToolStripSeparator;
            }
        }

        #endregion

        VisualResizeAppElements(frmMainApp: this);

        // load lvwFileList
        lvwFileList_LoadOrUpdate();
        tbx_lng.KeyPress += (sndr,
                             ev) =>
        {
            if (ev.KeyChar.Equals(obj: (char)13))
            {
                btn_NavigateMapGo.PerformClick();
                ev.Handled = true; // suppress default handling
            }
        };
        SizeChanged += FrmMainApp_SizeChanged;
        try
        {
            tbx_lat.Text = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "lastLat"
            );
            tbx_lng.Text = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "lastLng"
            );
        }
        catch
        {
            // ignored
        }

        if (tbx_lat.Text == "" || tbx_lat.Text == "0")
        {
            // NASA HQ
            string defaultLat = "38.883056";
            string defaultLng = "-77.016389";
            tbx_lat.Text = defaultLat;
            tbx_lng.Text = defaultLng;
        }

        HelperStatic.HsMapMarkers.Clear();
        HelperStatic.HsMapMarkers.Add(item: (tbx_lat.Text.Replace(oldChar: ',', newChar: '.'), tbx_lng.Text.Replace(oldChar: ',', newChar: '.')));
        NavigateMapGo();

        await HelperStatic.GenericCheckForNewVersions();
    }

    /// <summary>
    ///     Initialises the map in the app and browses to the default or last-used location.
    /// </summary>
    /// <returns></returns>
    private async Task InitialiseWebView()
    {
        try
        {
            // silly thing dumps the folder by default right into Program Files where it can't write further due to permission issues
            // need to move it elsewhere.

            CoreWebView2Environment c2Wv = await CoreWebView2Environment.CreateAsync(browserExecutableFolder: null,
                                                                                     userDataFolder: Path.GetTempPath(),
                                                                                     options: new CoreWebView2EnvironmentOptions(additionalBrowserArguments: null, language: "en"));
            await wbv_MapArea.EnsureCoreWebView2Async(environment: c2Wv);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewEnsureCoreWebView2Async") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        try
        {
            if (wbv_MapArea.CoreWebView2 != null)
            {
                wbv_MapArea.CoreWebView2.Settings.IsWebMessageEnabled = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewIsWebMessageEnabled") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // read the "map.html" file.
        string htmlCode = "";
        try
        {
            htmlCode = File.ReadAllText(path: Path.Combine(path1: ResourcesFolderPath, path2: "map.html"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewReadHTMLFile") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        try
        {
            string strLatCoordinate = tbx_lat.Text.Replace(oldChar: ',', newChar: '.');
            string strLngCoordinate = tbx_lng.Text.Replace(oldChar: ',', newChar: '.');

            double parsedLat;
            double parsedLng;
            if (double.TryParse(s: strLatCoordinate, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strLngCoordinate, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
            {
                LatCoordinate = strLatCoordinate;
                LngCoordinate = strLngCoordinate;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewParseCoordsFromHTMLFile") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // replace hard-coded values in the html code
        try
        {
            if (HelperStatic.SArcGisApiKey == null)
            {
                HelperStatic.SArcGisApiKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();
            }

            htmlCode = htmlCode.Replace(oldValue: "yourApiKey", newValue: HelperStatic.SArcGisApiKey);
            htmlCode = htmlCode.Replace(oldValue: "replaceLat", newValue: LatCoordinate);
            htmlCode = htmlCode.Replace(oldValue: "replaceLng", newValue: LngCoordinate);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewReplaceStringInHTMLFile") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        // show the decoded location on the map
        try
        {
            wbv_MapArea.NavigateToString(htmlContent: htmlCode);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeWebViewNavigateToStringInHTMLFile") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        try
        {
            wbv_MapArea.WebMessageReceived += wbv_MapArea_WebMessageReceived;
        }
        catch (Exception ex)
        {
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

                await HelperStatic.ExifWriteExifToFile();
                // shouldn't be needed but just in case.
                HelperStatic.FilesAreBeingSaved = false;
            }
            else if (dialogResult == DialogResult.No)
            {
                DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
            }
        }

        // write column widths to db
        VisualWriteLvw_FileList_ColWidth(frmMainApp: this);

        // write lat/long for future reference to db
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
        pbx_imagePreview.Image = null; // unlocks listOfAsyncCompatibleFileNamesWithOutPath. theoretically.
        HelperStatic.FsoCleanUpUserFolder();

        // force it otherwise it keeps a lock on for a few seconds.
        await AsyncExifTool.DisposeAsync();
    }

    /// <summary>
    ///     this is to deal with the icons in listview
    ///     from https://stackoverflow.com/a/37806517/3968494
    /// </summary>
    [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming"), SuppressMessage(category: "ReSharper", checkId: "UnusedMember.Local"), SuppressMessage(category: "ReSharper", checkId: "IdentifierTypo"), SuppressMessage(category: "ReSharper", checkId: "StringLiteralTypo"), SuppressMessage(category: "ReSharper", checkId: "MemberCanBePrivate.Local"), SuppressMessage(category: "ReSharper", checkId: "FieldCanBeMadeReadOnly.Local")]
    private static class NativeMethods
    {
        public const uint LVM_FIRST = 0x1000;
        public const uint LVM_GETIMAGELIST = LVM_FIRST + 2;
        public const uint LVM_SETIMAGELIST = LVM_FIRST + 3;

        public const uint LVSIL_NORMAL = 0;
        public const uint LVSIL_SMALL = 1;
        public const uint LVSIL_STATE = 2;
        public const uint LVSIL_GROUPHEADER = 3;

        public const uint SHGFI_DISPLAYNAME = 0x200;
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;
        public const uint SHGFI_SMALLICON = 0x1;
        public const uint SHGFI_SYSICONINDEX = 0x4000;

        [DllImport(dllName: "user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd,
                                                uint msg,
                                                uint wParam,
                                                IntPtr lParam);

        [DllImport(dllName: "comctl32")]
        public static extern bool ImageList_Destroy(IntPtr hImageList);

        [DllImport(dllName: "shell32", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(string pszPath,
                                                  uint dwFileAttributes,
                                                  ref SHFILEINFOW psfi,
                                                  uint cbSizeFileInfo,
                                                  uint uFlags);

        [DllImport(dllName: "uxtheme", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd,
                                                string pszSubAppName,
                                                string pszSubIdList);


        [StructLayout(layoutKind: LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFOW
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(unmanagedType: UnmanagedType.ByValTStr, SizeConst = 260 * 2)]
            public string szDisplayName;

            [MarshalAs(unmanagedType: UnmanagedType.ByValTStr, SizeConst = 80 * 2)]
            public string szTypeName;
        }


        [StructLayout(layoutKind: LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Shfileinfow
        {
            public readonly IntPtr hIcon;
            public readonly int iIcon;
            public readonly uint dwAttributes;

            [MarshalAs(unmanagedType: UnmanagedType.ByValTStr, SizeConst = 260 * 2)]
            public readonly string szDisplayName;

            [MarshalAs(unmanagedType: UnmanagedType.ByValTStr, SizeConst = 80 * 2)]
            public readonly string szTypeName;
        }
    }

    #endregion

    #region Resizing Stuff

    /// <summary>
    ///     When the app resizes automatically this adjusts the elements to fit the view.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void FrmMainApp_SizeChanged(object sender,
                                        EventArgs e)
    {
        // when minimised some heights become 0 value which causes problems with the splitterdistances.
        if (WindowState != FormWindowState.Minimized)
        {
            VisualResizeAppElements(frmMainApp: this);
        }
    }

    /// <summary>
    ///     Identical to the above but this is the one actually executing
    /// </summary>
    /// <param name="frmMainApp">Make a guess</param>
    private void VisualResizeAppElements(FrmMainApp frmMainApp)
    {
        #region fixed stuff

        frmMainApp.tsr_MainAppToolStrip.Top = Convert.ToInt16(value: mns_MenuStrip.Bottom + 2);
        frmMainApp.tsr_MainAppToolStrip.Left = 5;

        frmMainApp.tsr_FolderControl.Top = Convert.ToInt16(value: tsr_MainAppToolStrip.Bottom + 2);
        frmMainApp.tsr_FolderControl.Left = 5;

        // tct is the map page container, currently not split into further pages
        frmMainApp.tct_Main.Width = Convert.ToInt16(value: Width * 0.4);
        frmMainApp.tct_Main.Top = Convert.ToInt16(value: tsr_FolderControl.Bottom + 2);
        frmMainApp.tct_Main.Height = Convert.ToInt16(value: Bottom - tsr_FolderControl.Bottom - 95);
        frmMainApp.tct_Main.Left = Convert.ToInt16(value: Width - frmMainApp.tct_Main.Width - 20);

        #endregion

        #region splitcontainers

        frmMainApp.splitContainerMain.Top = frmMainApp.tsr_FolderControl.Bottom;
        frmMainApp.splitContainerMain.Left = 20;
        frmMainApp.splitContainerMain.Height = Convert.ToInt16(value: frmMainApp.Height);
        frmMainApp.splitContainerMain.Width = frmMainApp.Width - 40;

        try
        {
            frmMainApp.splitContainerMain.SplitterDistance = Convert.ToInt16(value: Width * 0.5);
            frmMainApp.splitContainerMain.MaximumSize = new Size(width: Convert.ToInt16(value: frmMainApp.Width * 0.98), height: Convert.ToInt16(value: (frmMainApp.Height - frmMainApp.splitContainerMain.Top) * 0.95));

            // that's the left block
            frmMainApp.splitContainerMain.Panel1MinSize = Convert.ToInt16(value: frmMainApp.splitContainerMain.Width * 0.15);
            frmMainApp.splitContainerLeftTop.SplitterDistance = Convert.ToInt16(value: frmMainApp.splitContainerMain.Height * 0.65);

            // that's the right block
            frmMainApp.splitContainerMain.Panel2MinSize = Convert.ToInt16(value: frmMainApp.splitContainerMain.Width * 0.25);

            frmMainApp.splitContainerRight.SplitterDistance = Convert.ToInt16(value: frmMainApp.splitContainerRight.Height * 0.9);
        }
        catch
        { }

        #endregion

        // top left
        frmMainApp.lvw_FileList.Top = 0;
        frmMainApp.lvw_FileList.Left = 0;
        frmMainApp.lvw_FileList.Height = frmMainApp.splitContainerLeftTop.Panel1.Height;
        frmMainApp.lvw_FileList.Width = frmMainApp.splitContainerLeftTop.Panel1.Width;

        // top right
        frmMainApp.tct_Main.Top = 0;
        frmMainApp.tct_Main.Left = frmMainApp.splitContainerRight.Panel1.Left;
        frmMainApp.tct_Main.Height = frmMainApp.splitContainerRight.Panel1.Height;
        frmMainApp.tct_Main.Width = frmMainApp.splitContainerRight.Width;

        // bottom left
        frmMainApp.pbx_imagePreview.Top = 0;
        frmMainApp.pbx_imagePreview.Left = 0;
        frmMainApp.pbx_imagePreview.Height = frmMainApp.splitContainerLeftBottom.Panel1.Height;
        frmMainApp.pbx_imagePreview.Width = frmMainApp.splitContainerLeftBottom.Panel1.Width;

        frmMainApp.lbl_ParseProgress.Top = 0;
        frmMainApp.lbl_ParseProgress.Left = 0;
        frmMainApp.lbl_ParseProgress.Width = frmMainApp.splitContainerLeftBottom.Panel2.Width;
        frmMainApp.splitContainerLeftTop.Panel2MinSize = Convert.ToInt16(value: frmMainApp.splitContainerMain.Height * 0.25);

        // bottom right
        frmMainApp.lbl_lat.Top = 0;
        frmMainApp.tbx_lat.Top = 0;
        frmMainApp.lbl_lat.Left = frmMainApp.splitContainerRight.Left;
        frmMainApp.tbx_lat.Left = frmMainApp.lbl_lat.Right + 2;

        frmMainApp.lbl_lng.Top = 0;
        frmMainApp.tbx_lng.Top = 0;
        frmMainApp.lbl_lng.Left = frmMainApp.tbx_lat.Right + 2;
        frmMainApp.tbx_lng.Left = frmMainApp.lbl_lng.Right + 2;

        frmMainApp.btn_NavigateMapGo.Top = 0;
        frmMainApp.btn_NavigateMapGo.Left = frmMainApp.tbx_lng.Right + 2;
        frmMainApp.btn_loctToFile.Top = 0;
        frmMainApp.btn_loctToFile.Left = frmMainApp.btn_NavigateMapGo.Right + 2;

        // bit manual for now.
        try
        {
            ttp_loctToFile.SetToolTip(control: frmMainApp.btn_loctToFile,
                                      caption: HelperStatic.DataReadSQLiteObjectText(
                                          languageName: AppLanguage,
                                          objectType: "ToolTip",
                                          objectName: "ttp_loctToFile"
                                      )
            );

            ttp_NavigateMapGo.SetToolTip(control: frmMainApp.btn_NavigateMapGo,
                                         caption: HelperStatic.DataReadSQLiteObjectText(
                                             languageName: AppLanguage,
                                             objectType: "ToolTip",
                                             objectName: "ttp_NavigateMapGo"
                                         )
            );
        }
        catch
        {
            //nothing
        }
    }

    /// <summary>
    ///     Reads the widths of individual CLHs from SQL, if not found assigns them "auto" (-2)
    /// </summary>
    /// <param name="frmMainApp">Make a guess</param>
    /// <exception cref="InvalidOperationException">If it encounters a missingCLH</exception>
    private void VisualReadLvw_FileList_ColWidth(FrmMainApp frmMainApp)
    {
        string settingIdToSend;
        int colWidth = 0;
        // logic: see if it's in SQL first...if not then set to Auto
        foreach (ColumnHeader columnHeader in frmMainApp.lvw_FileList.Columns)
        {
            // columnHeader.Name doesn't get automatically recorded, i think that's a VSC bug.
            // anyway will introduce a breaking-line here for that.
            // oh and can't convert bool to str but it's not letting to deal w it otherwise anyway so going for length == 0 instead
            if (columnHeader.Name.Length == 0)
            {
                throw new InvalidOperationException(message: "columnHeader name missing");
            }

            settingIdToSend = lvw_FileList.Name + "_" + columnHeader.Name + "_width";
            colWidth = Convert.ToInt16(value: HelperStatic.DataReadSQLiteSettings(
                                           tableName: "applayout",
                                           settingTabPage: "lvw_FileList",
                                           settingId: settingIdToSend)
            );
            if (colWidth == 0) // a null value would be parsed to zero
            {
                switch (columnHeader.Name.Substring(startIndex: 4))
                {
                    case "GPSLatitude" or "GPSLatitudeRef" or "GPSLongitude" or "GPSLongitudeRef" or "GPSSpeedRef" or "GPSAltitudeRef" or "DestCoordinates" or "GPSDestLatitude" or "GPSDestLatitudeRef" or "GPSDestLongitude" or "GPSDestLongitudeRef" or "GPSImgDirection" or "GPSImgDirectionRef":
                        columnHeader.Width = 0;
                        break;
                    default:
                        columnHeader.Width = -2;
                        break;
                }
            }
            else
            {
                switch (columnHeader.Name.Substring(startIndex: 4))
                {
                    case "GPSLatitude" or "GPSLatitudeRef" or "GPSLongitude" or "GPSLongitudeRef" or "GPSSpeedRef" or "GPSAltitudeRef" or "DestCoordinates" or "GPSDestLatitude" or "GPSDestLatitudeRef" or "GPSDestLongitude" or "GPSDestLongitudeRef" or "GPSImgDirection" or "GPSImgDirectionRef":
                        columnHeader.Width = 0;
                        break;
                    default:
                        columnHeader.Width = colWidth;
                        break;
                }
            }
        }
    }

    /// <summary>
    ///     Sends the CLH width to SQL for writing.
    /// </summary>
    /// <param name="frmMainApp">Make a guess</param>
    private void VisualWriteLvw_FileList_ColWidth(FrmMainApp frmMainApp)
    {
        string settingIdToSend;
        foreach (ColumnHeader columnHeader in frmMainApp.lvw_FileList.Columns)
        {
            if (columnHeader.Width != -2) // actually this doesn't work but low-pri to fix
            {
                settingIdToSend = lvw_FileList.Name + "_" + columnHeader.Name + "_width";
                HelperStatic.DataWriteSQLiteSettings(
                    tableName: "applayout",
                    settingTabPage: "lvw_FileList",
                    settingId: settingIdToSend,
                    settingValue: columnHeader.Width.ToString()
                );
            }
        }
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

        HelperStatic.LvwCountItemsWithGeoData();
    }

    /// <summary>
    ///     Handles the navigation to a coordinate on the map. Replaces hard-coded values w/ user-provided ones
    ///     ... and executes the navigation action.
    /// </summary>
    private void NavigateMapGo()
    {
        string htmlCode = "";
        HelperStatic.HtmlAddMarker = "";
        HelperStatic.MinLat = null;
        HelperStatic.MinLng = null;
        HelperStatic.MaxLat = null;
        HelperStatic.MaxLng = null;

        // lazy
        string strLatCoordinate = "0";
        string strLngCoordinate = "0";

        try
        {
            htmlCode = File.ReadAllText(path: Path.Combine(path1: ResourcesFolderPath, path2: "map.html"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorNavigateMapGoHTMLCode") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        if (HelperStatic.SArcGisApiKey == null)
        {
            HelperStatic.SArcGisApiKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();
        }

        foreach ((string strLat, string strLng) locationCoord in HelperStatic.HsMapMarkers)
        {
            HelperStatic.HtmlAddMarker += "var marker = L.marker([" + locationCoord.strLat + ", " + locationCoord.strLng + "]).addTo(map).openPopup();" + "\n";
            strLatCoordinate = locationCoord.strLat;
            strLngCoordinate = locationCoord.strLng;

            // set scene for mix/max so map zoom can be set automatically
            {
                if (HelperStatic.MinLat == null)
                {
                    HelperStatic.MinLat = double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture);
                    HelperStatic.MaxLat = HelperStatic.MinLat;
                }

                if (double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture) < HelperStatic.MinLat)
                {
                    HelperStatic.MinLat = double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture);
                }

                if (double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture) > HelperStatic.MaxLat)
                {
                    HelperStatic.MaxLat = double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture);
                }

                if (HelperStatic.MinLng == null)
                {
                    HelperStatic.MinLng = double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture);
                    HelperStatic.MaxLng = HelperStatic.MinLng;
                }

                if (double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture) < HelperStatic.MinLng)
                {
                    HelperStatic.MinLng = double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture);
                }

                if (double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture) > HelperStatic.MaxLng)
                {
                    HelperStatic.MaxLng = double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture);
                }
            }
        }

        htmlCode = htmlCode.Replace(oldValue: "replaceLat", newValue: strLatCoordinate);
        htmlCode = htmlCode.Replace(oldValue: "replaceLng", newValue: strLngCoordinate);
        htmlCode = htmlCode.Replace(oldValue: "replaceMinLat", newValue: HelperStatic.MinLat.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));
        htmlCode = htmlCode.Replace(oldValue: "replaceMinLng", newValue: HelperStatic.MinLng.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));
        htmlCode = htmlCode.Replace(oldValue: "replaceMaxLat", newValue: HelperStatic.MaxLat.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));
        htmlCode = htmlCode.Replace(oldValue: "replaceMaxLng", newValue: HelperStatic.MaxLng.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));

        htmlCode = htmlCode.Replace(oldValue: "yourApiKey", newValue: HelperStatic.SArcGisApiKey);
        htmlCode = htmlCode.Replace(oldValue: "{ HTMLAddMarker }", newValue: HelperStatic.HtmlAddMarker);
        wbv_MapArea.NavigateToString(htmlContent: htmlCode);
    }

    #endregion

    #region Menu Stuff

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
            int fileCount = 0;
            FrmEditFileData = new FrmEditFileData();
            FrmEditFileData.lvw_FileListEditImages.Items.Clear();
            foreach (ListViewItem selectedItem in lvw_FileList.SelectedItems)
            {
                if (File.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: selectedItem.Text)))
                {
                    FolderName = tbx_FolderName.Text;
                    FrmEditFileData.lvw_FileListEditImages.Items.Add(text: selectedItem.Text);
                    fileCount++;
                }
            }

            if (fileCount > 0)
            {
                FrmEditFileData.Text = HelperStatic.DataReadSQLiteObjectText(
                    languageName: AppLanguage,
                    objectType: "Form",
                    objectName: "FrmEditFileData"
                );
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
    ///     Handles the tmi_File_ImportGPX_Click event -> Brings up the FrmImportGpx to import grack
    ///     listOfAsyncCompatibleFileNamesWithOutPath
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
                    lvw_FileList.Items.Clear();
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
    ///     Performs a pull of Toponomy & Altitude info for all of the selected listOfAsyncCompatibleFileNamesWithOutPath.
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
                    .Substring(0, tst.GetUtcOffset(dateTime: createDate)
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

            lvi.ForeColor = Color.Red;
            HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);
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
            lvi.ForeColor = Color.Red;
            HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);
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
        HelperStatic.SChangeFolderIsOkay = false;
        await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
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
        HelperStatic.SChangeFolderIsOkay = false;
        await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
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
            btn_ts_Refresh_lvwFileList_Click(sender: this, e: new EventArgs());
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
    ///     selected listOfAsyncCompatibleFileNamesWithOutPath
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
        FrmImportGpx frmImportGpx = new();
        frmImportGpx.ShowDialog();
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
    ///     ... it assigns the outstanding listOfAsyncCompatibleFileNamesWithOutPath according to compatibility and then runs
    ///     the respective exiftool commands.
    ///     Also I've introduced a "Please Wait" Form to block the Main Form from being interacted with while the folder is
    ///     refreshing. Soz but needed.
    /// </summary>
    private async void lvwFileList_LoadOrUpdate()
    {
        List<string> listOfAsyncCompatibleFileNamesWithOutPath = new();
        List<string> listOfAsyncInompatibleFileNamesWithOutPath = new();

        lvw_FileList.Items.Clear();
        Application.DoEvents();
        HelperStatic.FilesBeingProcessed.Clear();
        RemoveGeoDataIsRunning = false;

        #region PleaseWaitBox

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
        pleaseWaitBox.Show();
        Enabled = false;

        #endregion

        if (tbx_FolderName.Text != null)
        {
            // this shouldn't really happen but just in case
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
            }

            // check for drives
            if (FolderName == SpecialFolder.MyComputer.ToString())
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    lvw_FileList_addListItem(fileName: drive.Name);
                }
            }
            else
            {
                // list folders and stick them at the beginning of the listview
                // ReparsePoint means these are links. 
                List<string> dirs = new();
                try
                {
                    DirectoryInfo di = new(path: FolderName);
                    foreach (DirectoryInfo sub in di.GetDirectories())
                    {
                        if (sub.Attributes.ToString()
                                .Contains(value: "Directory") &&
                            !sub.Attributes.ToString()
                                .Contains(value: "ReparsePoint"))
                        {
                            dirs.Add(item: sub.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(text: ex.Message);
                }

                string[] allowedExtensions = new string[AncillaryListsArrays.AllCompatibleExtensions()
                    .Length];
                Array.Copy(sourceArray: allowedExtensions, destinationArray: AncillaryListsArrays.AllCompatibleExtensions(), length: 0);
                for (int i = 0; i < allowedExtensions.Length; i++)
                {
                    allowedExtensions[i] = AncillaryListsArrays.AllCompatibleExtensions()[i]
                        .Split('\t')
                        .First();
                }

                // list listOfAsyncCompatibleFileNamesWithOutPath that have whitelisted extensions
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
                    MessageBox.Show(text: ex.Message);
                }

                listFilesWithPath = listFilesWithPath.OrderBy(keySelector: o => o)
                    .ToList();

                // also add to filesBeingProcessed
                foreach (string fileNameWithPath in listFilesWithPath)
                {
                    string fileNameWithoutPath = Path.GetFileName(path: fileNameWithPath);
                    HelperStatic.GenericLockLockFile(fileNameWithoutPath: fileNameWithoutPath);
                }

                // add a parent folder. "dot dot"
                try
                {
                    string tmpStrParent = HelperStatic.FsoGetParent(path: tbx_FolderName.Text);
                    if (tmpStrParent != null && tmpStrParent != SpecialFolder.MyComputer.ToString())
                    {
                        lvw_FileList.Items.Add(text: "..");
                    }
                }
                catch
                {
                    // ignored
                }

                foreach (string currentDir in dirs)
                {
                    lvw_FileList_addListItem(fileName: Path.GetFileName(path: currentDir));
                }

                foreach (string fileNameWithPath in listFilesWithPath)
                {
                    string fileNameWithoutPath = Path.GetFileName(path: fileNameWithPath);
                    string fileNameWithPathWithXMP = Path.Combine(path1: Path.GetDirectoryName(path: fileNameWithPath), path2: Path.GetFileNameWithoutExtension(path: fileNameWithPath) + ".xmp");
                    lvw_FileList_addListItem(fileName: Path.GetFileName(path: fileNameWithoutPath));

                    // if this file appears in the DtFilesSeenInThisSession // or with xmp
                    bool timeStampNeedsUpdating = false;
                    if (DtFilesSeenInThisSession.AsEnumerable()
                        .Any(predicate: row => fileNameWithPath == row.Field<string>(columnName: "fileNameWithPath")))
                    {
                        // check if the timestamp matches
                        DataRow[] drFoundFileData = DtFilesSeenInThisSession.Select(filterExpression: "fileNameWithPath = '" + fileNameWithPath + "'");
                        DataRow[] drFoundFileDataXmp = DtFilesSeenInThisSession.Select(filterExpression: "fileNameWithPath = '" + fileNameWithPathWithXMP + "'");
                        if (drFoundFileData.Length != 0 || drFoundFileDataXmp.Length != 0)
                        {
                            string drFoundFileDataStr = drFoundFileData[0][columnName: "fileDateTime"]
                                .ToString();

                            string lastWriteTimeStr = File.GetLastWriteTime(path: fileNameWithPath)
                                .ToString(provider: CultureInfo.InvariantCulture);

                            if (drFoundFileDataStr == lastWriteTimeStr)
                            {
                                // nothing actually
                            }
                            else
                            {
                                timeStampNeedsUpdating = true;
                            }

                            // xmp
                            if (File.Exists(path: fileNameWithPathWithXMP) && drFoundFileDataXmp.Length != 0)
                            {
                                string drFoundFileDataStrXmp = drFoundFileDataXmp[0][columnName: "fileDateTime"]
                                    .ToString();

                                string lastWriteTimeStrXmp = File.GetLastWriteTime(path: fileNameWithPathWithXMP)
                                    .ToString(provider: CultureInfo.InvariantCulture);

                                if (drFoundFileDataStrXmp == lastWriteTimeStrXmp)
                                {
                                    // nothing actually
                                }
                                else
                                {
                                    timeStampNeedsUpdating = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        timeStampNeedsUpdating = true;
                    }

                    if (timeStampNeedsUpdating)
                    {
                        // delete if exists in DtFilesSeenInThisSession
                        DataRow[] drFoundFileData = DtFilesSeenInThisSession.Select(filterExpression: "fileNameWithPath = '" + fileNameWithPath + "'");
                        if (drFoundFileData.Length != 0)
                        {
                            drFoundFileData[0]
                                .Delete();
                            DtFilesSeenInThisSession.AcceptChanges();
                        }

                        // xmp
                        DataRow[] drFoundFileDataXmp = DtFilesSeenInThisSession.Select(filterExpression: "fileNameWithPath = '" + fileNameWithPathWithXMP + "'");
                        if (drFoundFileDataXmp.Length != 0)
                        {
                            drFoundFileDataXmp[0]
                                .Delete();
                            DtFilesSeenInThisSession.AcceptChanges();
                        }

                        // also drop from DtFileDataSeenInThisSession
                        drFoundFileData = DtFileDataSeenInThisSession.Select(filterExpression: "fileNameWithPath = '" + fileNameWithPath + "'");
                        if (drFoundFileData.Length != 0)
                        {
                            foreach (DataRow dr in drFoundFileData)
                            {
                                dr.Delete();
                                DtFileDataSeenInThisSession.AcceptChanges();
                            }
                        }

                        // the add-in used here can't process nonstandard characters in filenames w/o an args file, which doesn't return what we're after.
                        // so for 'standard' stuff we'll run async and for everything else we'll do it slower but more compatible
                        // lock will be removed later.
                        if (Regex.IsMatch(input: fileNameWithPath, pattern: @"^[a-zA-Z0-9.:\\_ ]*$"))
                        {
                            listOfAsyncCompatibleFileNamesWithOutPath.Add(item: Path.GetFileName(path: fileNameWithPath));
                        }
                        else
                        {
                            listOfAsyncInompatibleFileNamesWithOutPath.Add(item: Path.GetFileName(path: fileNameWithPath));
                        }
                    }
                    // basically if we've already parsed this file and it hasn't changed since then don't reparse it.
                    else
                    {
                        DataRow[] drFoundFileData = DtFileDataSeenInThisSession.Select(filterExpression: "fileNameWithPath = '" + fileNameWithPath + "'");

                        // just to be on the foolproof side
                        if (drFoundFileData.Length != 0)
                        {
                            foreach (DataRow dr in drFoundFileData)
                            {
                                ListView lvw = lvw_FileList;
                                ListViewItem lvi = lvw.FindItemWithText(text: dr[columnIndex: 0]
                                                                            .ToString()
                                                                            .Split('\\')
                                                                            .Last());
                                //lvw.BeginUpdate();
                                try
                                {
                                    lvi.SubItems[index: lvw.Columns[key: "clh_" +
                                                                         dr[columnIndex: 1]]
                                                     .Index]
                                        .Text = dr[columnIndex: 2]
                                        .ToString();
                                }
                                catch
                                {
                                    // nothing
                                }

                                // remove the lock
                                try
                                {
                                    HelperStatic.GenericLockUnLockFile(fileNameWithoutPath: fileNameWithoutPath);
                                }
                                catch
                                {
                                    // nothing, this shouldn't happen but i don't want it to stop the app anyway.
                                }

                                lvi.ForeColor = Color.Black;
                                //lvw.EndUpdate();
                            }
                        }
                    }
                }

                if (listOfAsyncCompatibleFileNamesWithOutPath.Count > 0)
                {
                    HelperStatic.FolderEnterLastEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await HelperStatic.ExifGetExifFromFilesCompatibleFileNames(listOfAsyncCompatibleFileNamesWithOutPath: listOfAsyncCompatibleFileNamesWithOutPath, folderEnterEpoch: HelperStatic.FolderEnterLastEpoch);
                }

                if (listOfAsyncInompatibleFileNamesWithOutPath.Count > 0)
                {
                    string dontShowIncompatibleFileWarningAgainInSql = HelperStatic.DataReadSQLiteSettings(
                        tableName: "settings",
                        settingTabPage: "generic",
                        settingId: "dontShowIncompatibleFileWarningAgain"
                    );
                    if (dontShowIncompatibleFileWarningAgainInSql != "true")
                    {
                        DialogResult dontShowIncompatibleFileWarningAgain = MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_QuestionDontShowIncompatibleFileWarningAgain"),
                                                                                            caption: "Nonstandard paths", buttons: MessageBoxButtons.OKCancel, icon: MessageBoxIcon.Warning);
                        if (dontShowIncompatibleFileWarningAgain == DialogResult.Cancel)
                        {
                            HelperStatic.DataWriteSQLiteSettings(
                                tableName: "settings",
                                settingTabPage: "generic",
                                settingId: "dontShowIncompatibleFileWarningAgain",
                                settingValue: "true"
                            );
                        }
                    }

                    HelperStatic.FolderEnterLastEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await HelperStatic.ExifGetExifFromFilesIncompatibleFileNames(listOfAsyncIncompatibleFileNamesWithOutPath: listOfAsyncInompatibleFileNamesWithOutPath, folderEnterEpoch: HelperStatic.FolderEnterLastEpoch);
                }
            }
        }

        HelperStatic.FileListBeingUpdated = false;
        Enabled = true;
        pleaseWaitBox.Hide();
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
        ListViewHitTestInfo info = lvw_FileList.HitTest(x: e.X, y: e.Y);
        ListViewItem item = info.Item;

        if (item != null)
        {
            bool isDrive = HelperStatic.LvwItemIsDrive(lvwFileListItem: item);
            // if .. (parent) then do a folder-up
            if (item.Text == "..")
            {
                btn_OneFolderUp_Click(sender: sender, e: EventArgs.Empty);
            }
            // if this is a folder or drive, enter
            else if (Directory.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: item.Text)) || isDrive)
            {
                // check for outstanding listOfAsyncCompatibleFileNamesWithOutPath first and save if user wants
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
                                                  .First() +
                                              @"\";
                    }

                    btn_ts_Refresh_lvwFileList_Click(sender: this, e: EventArgs.Empty);
                }
            }
            // if this is a file
            else if (File.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: item.Text)))
            {
                FrmEditFileData = new FrmEditFileData();
                FolderName = tbx_FolderName.Text;
                FrmEditFileData.lvw_FileListEditImages.Items.Add(text: item.Text);
                FrmEditFileData.Text = HelperStatic.DataReadSQLiteObjectText(
                    languageName: AppLanguage,
                    objectType: "Form",
                    objectName: "FrmEditFileData"
                );
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
            if (lvw_FileList.FocusedItem != null)
            {
                await HelperStatic.LvwItemClickNavigate();
                // it's easier to call the create-preview here than in the other one because focusedItems misbehave/I don't quite understand it/them
                if (lvw_FileList.SelectedItems.Count > 0)
                {
                    if (File.Exists(path: Path.Combine(FolderName +
                                                       lvw_FileList.SelectedItems[index: 0]
                                                           .Text)))
                    {
                        await HelperStatic.LvwItemCreatePreview(fileNameWithPath: Path.Combine(FolderName +
                                                                                               lvw_FileList.SelectedItems[index: 0]
                                                                                                   .Text));
                    }
                }

                // for folders and other non-valid items, don't do anything.
                if (HelperStatic.HsMapMarkers.Count > 0)
                {
                    NavigateMapGo();
                }
            }
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
                    if (HelperStatic.HsMapMarkers.Count > 0)
                    {
                        NavigateMapGo();
                    }
                }
            }

            // just in case...
            HelperStatic.SNowSelectingAllItems = false;
        }

        // Shift Control C -> copy details
        else if (e.Control && e.Shift && e.KeyCode == Keys.C)
        {
            HelperStatic.LwvCopyGeoData();
        }

        // Shift Control V -> paste details
        else if (e.Control && e.Shift && e.KeyCode == Keys.V)
        {
            HelperStatic.LwvPasteGeoData();
        }

        // Control Enter -> Edit File
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
                if (folderToEnter == "..")
                {
                    btn_OneFolderUp_Click(sender: sender, e: EventArgs.Empty);
                }
                // if this is a folder or drive, enter
                else if (Directory.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: item.Text)))
                {
                    // check for outstanding listOfAsyncCompatibleFileNamesWithOutPath first and save if user wants
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

        // Control S -> Save listOfAsyncCompatibleFileNamesWithOutPath
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
    ///     Adds a new listitem to lvw_FileList listview
    /// </summary>
    /// <param name="fileName">Name of file to be added</param>
    private void lvw_FileList_addListItem(string fileName)
    {
        List<string> subItemList = new();

        #region icon handlers

        //https://stackoverflow.com/a/37806517/3968494
        NativeMethods.SHFILEINFOW shfi = new();
        IntPtr hSysImgList = NativeMethods.SHGetFileInfo(pszPath: "",
                                                         dwFileAttributes: 0,
                                                         psfi: ref shfi,
                                                         cbSizeFileInfo: (uint)Marshal.SizeOf(structure: shfi),
                                                         uFlags: NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON);
        Debug.Assert(condition: hSysImgList != IntPtr.Zero); // cross our fingers and hope to succeed!

        // Set the ListView control to use that image list.
        IntPtr hOldImgList = NativeMethods.SendMessage(hWnd: lvw_FileList.Handle,
                                                       msg: NativeMethods.LVM_SETIMAGELIST,
                                                       wParam: NativeMethods.LVSIL_SMALL,
                                                       lParam: hSysImgList);

        // If the ListView control already had an image list, delete the old one.
        if (hOldImgList != IntPtr.Zero)
        {
            NativeMethods.ImageList_Destroy(hImageList: hOldImgList);
        }

        // Set up the ListView control's basic properties.
        // Set its theme so it will look like the one used by Explorer.
        NativeMethods.SetWindowTheme(hWnd: lvw_FileList.Handle, pszSubAppName: "Explorer", pszSubIdList: null);

        // Get the items from the file system, and add each of them to the ListView,
        // complete with their corresponding name and icon indices.
        IntPtr himl;
        if (tbx_FolderName.Text != SpecialFolder.MyComputer.ToString())
        {
            himl = NativeMethods.SHGetFileInfo(pszPath: Path.Combine(path1: tbx_FolderName.Text, path2: fileName),
                                               dwFileAttributes: 0,
                                               psfi: ref shfi,
                                               cbSizeFileInfo: (uint)Marshal.SizeOf(structure: shfi),
                                               uFlags: NativeMethods.SHGFI_DISPLAYNAME | NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON);
        }
        else
        {
            himl = NativeMethods.SHGetFileInfo(pszPath: fileName,
                                               dwFileAttributes: 0,
                                               psfi: ref shfi,
                                               cbSizeFileInfo: (uint)Marshal.SizeOf(structure: shfi),
                                               uFlags: NativeMethods.SHGFI_DISPLAYNAME | NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON);
        }

        //Debug.Assert(himl == hSysImgList); // should be the same imagelist as the one we set

        #endregion

        if (File.Exists(path: Path.Combine(path1: FolderName, path2: fileName)))
        {
            foreach (ColumnHeader columnHeader in lvw_FileList.Columns)
            {
                if (columnHeader.Name != "clh_FileName")
                {
                    subItemList.Add(item: "-");
                }
            }
        }

        ListViewItem lvi = new();

        // dev comment --> https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetfileinfow
        // SHGFI_DISPLAYNAME (0x000000200)
        // Retrieve the display name for the file, which is the name as it appears in Windows Explorer.
        // The name is copied to the szDisplayName member of the structure specified in psfi.
        // The returned display name uses the long file name, if there is one, rather than the 8.3 form of the file name.
        // [!!!!] Note that the display name can be affected by settings such as whether extensions are shown.

        // TLDR if Windows User has "show extensions" set to OFF in Windows Explorer, they won't show here either.
        // The repercussions of that is w/o an extension fileinfo.exists will return false and exiftool won't run/find it.

        // With that in mind if we're missing the extension then we'll force it back on.
        string fileExtension = Path.GetExtension(path: Path.Combine(path1: FolderName, path2: fileName));
        if (!string.IsNullOrEmpty(value: fileExtension))
        {
            if (shfi.szDisplayName.Contains(value: fileExtension))
            {
                lvi.Text = shfi.szDisplayName;
            }
            else
            {
                lvi.Text = shfi.szDisplayName + fileExtension;
            }
        }
        else
        {
            // this should prevent showing silly string values for special folders (like if your Pictures folder has been moved to say Digi, it'd have shown "Digi" but since that doesn't exist per se it'd have caused an error.
            // same for non-English places. E.g. "Documents and Settings" in HU would be displayed as "Felhasználók" but that folder is still actually called Documents and Settings, but the label is "fake".
            if (Directory.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: fileName)))
            {
                lvi.Text = fileName;
            }
            else
            {
                lvi.Text = shfi.szDisplayName;
            }
        }

        lvi.ImageIndex = shfi.iIcon;
        if (File.Exists(path: Path.Combine(path1: FolderName, path2: fileName)))
        {
            lvi.ForeColor = Color.Gray;
        }

        // don't add twice. this could happen if user does F5 too fast/too many times/is derp. (mostly the last one.)
        if (lvw_FileList.FindItemWithText(text: lvi.Text) == null)
        {
            lvw_FileList.Items.Add(value: lvi)
                .SubItems.AddRange(items: subItemList.ToArray());
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
        if (!HelperStatic.SNowSelectingAllItems)
        {
            await HelperStatic.LvwItemClickNavigate();
            // for folders and other non-valid items, don't do anything.
            if (HelperStatic.HsMapMarkers.Count > 0)
            {
                NavigateMapGo();
            }
        }
    }

    #endregion

    #endregion

    #region handlers

    /// <summary>
    ///     Deals with invoking the listview (from outside the thread) and updating the colour of a particular row (Item) to
    ///     the assigned colour.
    /// </summary>
    /// <param name="lvw">The listView Control that needs updating. Most likely the one in the main Form</param>
    /// <param name="itemText">The particular ListViewItem (by text) that needs updating</param>
    /// <param name="color">Parameter to assign a particular colour (prob red or black) to the whole row</param>
    internal static void HandlerUpdateItemColour(ListView lvw,
                                                 string itemText,
                                                 Color color)
    {
        // If the current thread is not the UI thread, InvokeRequired will be true
        if (lvw.InvokeRequired)
        {
            lvw.Invoke(method: (Action)(() => HandlerUpdateItemColour(lvw: lvw, itemText: itemText, color: color)));
            return;
        }

        ListViewItem itemToModify = lvw.FindItemWithText(text: itemText);
        if (itemToModify != null)
        {
            itemToModify.ForeColor = color;
        }
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
    }

    /// <summary>
    ///     Resizes items when the main Splitter is moved.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void splitContainerMain_SplitterMoved(object sender,
                                                  SplitterEventArgs e)
    {
        // top left
        lvw_FileList.Width = splitContainerMain.Panel1.Width - 2;

        // bottom left
        pbx_imagePreview.Width = splitContainerMain.Panel1.Width - 2;
        lbl_ParseProgress.Width = splitContainerMain.Panel1.Width - 2;

        // top right
        tct_Main.Top = 0;
        tct_Main.Left = splitContainerRight.Panel1.Left;
        tct_Main.Height = splitContainerRight.Panel1.Height;
        tct_Main.Width = splitContainerRight.Width;

        // bottom right
        lbl_lat.Top = 0;
        tbx_lat.Top = 0;
        lbl_lat.Left = splitContainerRight.Left;
        tbx_lat.Left = lbl_lat.Right + 2;

        lbl_lng.Top = 0;
        tbx_lng.Top = 0;
        lbl_lng.Left = tbx_lat.Right + 2;
        tbx_lng.Left = lbl_lng.Right + 2;

        btn_NavigateMapGo.Top = 0;
        btn_NavigateMapGo.Left = tbx_lng.Right + 2;
        btn_loctToFile.Top = 0;
        btn_loctToFile.Left = btn_NavigateMapGo.Right + 2;
    }

    /// <summary>
    ///     Resizes items when the left lower Splitter is moved
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void splitContainerLeftTop_SplitterMoved(object sender,
                                                     SplitterEventArgs e)
    {
        // top left
        lvw_FileList.Height = splitContainerLeftTop.Panel1.Height - 2;

        // bottom left
        splitContainerLeftBottom.Panel1.Top = 0;
        pbx_imagePreview.Top = 0;
        pbx_imagePreview.Height = splitContainerLeftBottom.Panel1.Height - 2;
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