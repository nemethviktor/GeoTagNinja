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
    internal static string UserDataFolderPath = Path.Combine(path1: Environment.GetFolderPath(folder: Environment.SpecialFolder.ApplicationData), path2: "GeoTagNinja");
    internal const string DoubleQuote = "\"";
    internal static string LatCoordinate;
    internal static string LngCoordinate;
    internal static DataTable ObjectNames;
    internal static DataTable ObjectTagNamesIn;
    internal static DataTable ObjectTagNamesOut;
    internal static string FolderName;
    internal static string AppLanguage = "english"; // default to english 
    internal static string ShowLocToMapDialogChoice = "default";
    internal FrmSettings FrmSettings;
    internal FrmEditFileData FrmEditFileData;
    internal FrmImportGpx FrmImportGpx;

    /// <summary>
    ///     this one basically handles what extensions we work with.
    ///     the actual list is used for file-specific Settings as well as the general running of the app
    ///     leave the \t in!
    /// </summary>
    internal AsyncExifTool AsyncExifTool;

    internal static DataTable DtFileDataCopyPool;
    internal static DataTable DtFileDataToWriteStage1PreQueue;
    internal static DataTable DtFileDataToWriteStage2QueuePendingSave;
    internal static DataTable DtFileDataToWriteStage3ReadyToWrite;

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

        List<string> commonArgs = new List<string>();

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
            Directory.CreateDirectory(path: Path.Combine(path1: Environment.GetFolderPath(folder: Environment.SpecialFolder.ApplicationData), path2: "GeoTagNinja"));
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
            HelperStatic.s_ArcGIS_APIKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();
            HelperStatic.s_GeoNames_UserName = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_GeoNames_UserName"
            );
            HelperStatic.s_GeoNames_Pwd = HelperStatic.DataReadSQLiteSettings(
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

        #region create dataTables

        // dt_fileDataCopyPool
        DtFileDataCopyPool = new DataTable();
        DtFileDataCopyPool.Clear();
        DtFileDataCopyPool.Columns.Add(columnName: "settingId");
        DtFileDataCopyPool.Columns.Add(columnName: "settingValue");

        // dt_fileDataToWriteStage1PreQueue 
        DtFileDataToWriteStage1PreQueue = new DataTable();
        DtFileDataToWriteStage1PreQueue.Clear();
        DtFileDataToWriteStage1PreQueue.Columns.Add(columnName: "filePath");
        DtFileDataToWriteStage1PreQueue.Columns.Add(columnName: "settingId");
        DtFileDataToWriteStage1PreQueue.Columns.Add(columnName: "settingValue");

        // dt_fileDataToWriteStage2QueuePendingSave 
        DtFileDataToWriteStage2QueuePendingSave = new DataTable();
        DtFileDataToWriteStage2QueuePendingSave.Clear();
        DtFileDataToWriteStage2QueuePendingSave.Columns.Add(columnName: "filePath");
        DtFileDataToWriteStage2QueuePendingSave.Columns.Add(columnName: "settingId");
        DtFileDataToWriteStage2QueuePendingSave.Columns.Add(columnName: "settingValue");

        // dt_fileDataToWriteStage3ReadyToWrite 
        DtFileDataToWriteStage3ReadyToWrite = new DataTable();
        DtFileDataToWriteStage3ReadyToWrite.Clear();
        DtFileDataToWriteStage3ReadyToWrite.Columns.Add(columnName: "filePath");
        DtFileDataToWriteStage3ReadyToWrite.Columns.Add(columnName: "settingId");
        DtFileDataToWriteStage3ReadyToWrite.Columns.Add(columnName: "settingValue");

        #endregion
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
            valStartupFolder = Environment.GetFolderPath(folder: Environment.SpecialFolder.MyPictures);
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

        HelperStatic.hs_MapMarkers.Clear();
        HelperStatic.hs_MapMarkers.Add(item: (tbx_lat.Text.Replace(oldChar: ',', newChar: '.'), tbx_lng.Text.Replace(oldChar: ',', newChar: '.')));
        NavigateMapGo();

        HelperStatic.GenericCheckForNewVersions();
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
            wbv_MapArea.CoreWebView2.Settings.IsWebMessageEnabled = true;
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
            if (HelperStatic.s_ArcGIS_APIKey == null)
            {
                HelperStatic.s_ArcGIS_APIKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();
            }

            htmlCode = htmlCode.Replace(oldValue: "yourApiKey", newValue: HelperStatic.s_ArcGIS_APIKey);
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
                await HelperStatic.ExifWriteExifToFile();
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
        pbx_imagePreview.Image = null; // unlocks files. theoretically.
        HelperStatic.FsoCleanUpUserFolder();

        // force it otherwise it keeps a lock on for a few seconds.
        AsyncExifTool.Dispose();
    }

    /// <summary>
    ///     this is to deal with the icons in listview
    ///     from https://stackoverflow.com/a/37806517/3968494
    /// </summary>
    static class NativeMethods
    {
        public const uint LVM_FIRST = 0x1000;
        public const uint LVM_GETIMAGELIST = (LVM_FIRST + 2);
        public const uint LVM_SETIMAGELIST = (LVM_FIRST + 3);

        public const uint LVSIL_NORMAL = 0;
        public const uint LVSIL_SMALL = 1;
        public const uint LVSIL_STATE = 2;
        public const uint LVSIL_GROUPHEADER = 3;

        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd,
                                                uint msg,
                                                uint wParam,
                                                IntPtr lParam);

        [DllImport("comctl32")]
        public static extern bool ImageList_Destroy(IntPtr hImageList);

        public const uint SHGFI_DISPLAYNAME = 0x200;
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;
        public const uint SHGFI_SMALLICON = 0x1;
        public const uint SHGFI_SYSICONINDEX = 0x4000;


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFOW
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260 * 2)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80 * 2)]
            public string szTypeName;
        }

        [DllImport("shell32", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(string pszPath,
                                                  uint dwFileAttributes,
                                                  ref SHFILEINFOW psfi,
                                                  uint cbSizeFileInfo,
                                                  uint uFlags);

        [DllImport("uxtheme", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd,
                                                string pszSubAppName,
                                                string pszSubIdList);


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
        frmMainApp.splitContainerMain.Left = Convert.ToInt16(value: Left + 20);
        frmMainApp.splitContainerMain.Height = Convert.ToInt16(value: frmMainApp.Height - frmMainApp.splitContainerMain.Top);
        frmMainApp.splitContainerMain.Width = frmMainApp.Width - 20 - Convert.ToInt16(value: Left + 20);

        try
        {
            frmMainApp.splitContainerMain.SplitterDistance = Convert.ToInt16(value: frmMainApp.splitContainerMain.Width * 0.5);
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

    [SuppressMessage("ReSharper", "InconsistentNaming")]
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
            System.Text.Json.JsonSerializer.Deserialize<MapGpsCoordinates>(json: jsonString);
        string strLat = $"{mapGpsCoordinates?.lat}".ToString(CultureInfo.InvariantCulture);
        string strLng = $"{mapGpsCoordinates?.lng}".ToString(CultureInfo.InvariantCulture);
        double.TryParse(strLat, NumberStyles.Any, CultureInfo.InvariantCulture, out double dblLat); // trust me i hate this f...king culture thing as much as possible...
        double.TryParse(strLng, NumberStyles.Any, CultureInfo.InvariantCulture, out double dblLng); // trust me i hate this f...king culture thing as much as possible...
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
            .ToString(CultureInfo.InvariantCulture);
        tbx_lng.Text = Math.Round(value: dblLng, digits: 6)
            .ToString(CultureInfo.InvariantCulture);
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
        HelperStatic.hs_MapMarkers.Clear();
        HelperStatic.hs_MapMarkers.Add(item: (tbx_lat.Text.Replace(oldChar: ',', newChar: '.'), tbx_lng.Text.Replace(oldChar: ',', newChar: '.')));
        NavigateMapGo();
    }

    /// <summary>
    ///     Handles the clicking on "ToFile" button. See comments above re: why we're using strings (culture-related issue)
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_loctToFile_Click(object sender,
                                      EventArgs e)
    {
        string strParsedLat = tbx_lat.Text.Replace(oldChar: ',', newChar: '.');
        string strParsedLng = tbx_lng.Text.Replace(oldChar: ',', newChar: '.');
        double parsedLat;
        double parsedLng;
        GeoResponseToponomy readJsonToponomy = new();
        GeoResponseAltitude readJsonAltitude = new();

        // lat/long gets written regardless of update-toponomy-choice
        if (double.TryParse(s: strParsedLat, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strParsedLng, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
        {
            if (lvw_FileList.SelectedItems.Count > 0)
            {
                foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                {
                    // don't do folders...
                    if (File.Exists(path: Path.Combine(path1: FolderName, path2: lvi.Text)))
                    {
                        // Latitude
                        HelperStatic.GenericUpdateAddToDataTable(
                            dt: DtFileDataToWriteStage3ReadyToWrite,
                            filePath: lvi.Text,
                            settingId: "GPSLatitude",
                            settingValue: strParsedLat
                        );

                        // Longitude
                        HelperStatic.GenericUpdateAddToDataTable(
                            dt: DtFileDataToWriteStage3ReadyToWrite,
                            filePath: lvi.Text,
                            settingId: "GPSLongitude",
                            settingValue: strParsedLng
                        );
                    }
                }
            }
        }

        if (double.TryParse(s: strParsedLat, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strParsedLng, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
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
                    foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                    {
                        // don't do folders...
                        if (File.Exists(path: Path.Combine(path1: FolderName, path2: lvi.Text)))
                        {
                            DataTable dtToponomy = new();
                            DataTable dtAltitude = new();
                            if (ShowLocToMapDialogChoice.Contains(value: "yes"))
                            {
                                HelperStatic.s_APIOkay = true;
                                dtToponomy = HelperStatic.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strParsedLat.ToString(provider: CultureInfo.InvariantCulture), lng: strParsedLng.ToString(provider: CultureInfo.InvariantCulture));
                                dtAltitude = HelperStatic.DTFromAPIExifGetAltitudeFromWebOrSQL(lat: strParsedLat.ToString(provider: CultureInfo.InvariantCulture), lng: strParsedLng.ToString(provider: CultureInfo.InvariantCulture));
                                if (HelperStatic.s_APIOkay)
                                {
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
                                    toponomyOverwrites.Add(item: ("GPSAltitude", dtAltitude.Rows[index: 0][columnName: "Altitude"]
                                                                      .ToString()));

                                    foreach ((string toponomyOverwriteName, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
                                    {
                                        HelperStatic.GenericUpdateAddToDataTable(
                                            dt: DtFileDataToWriteStage3ReadyToWrite,
                                            filePath: lvi.Text,
                                            settingId: toponomyDetail.toponomyOverwriteName,
                                            settingValue: toponomyDetail.toponomyOverwriteVal
                                        );
                                    }

                                    HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Processing: " + lvi.Text);
                                    lvi.ForeColor = Color.Red;
                                }
                            }
                        }
                    }

                    HelperStatic.LwvUpdateRowFromDTWriteStage3ReadyToWrite();
                }
            }
        }
    }

    /// <summary>
    ///     Handles the navigation to a coordinate on the map. Replaces hard-coded values w/ user-provided ones
    ///     ... and executes the navigation action.
    /// </summary>
    internal void NavigateMapGo()
    {
        string htmlCode = "";
        HelperStatic.HTMLAddMarker = "";
        HelperStatic.minLat = null;
        HelperStatic.minLng = null;
        HelperStatic.maxLat = null;
        HelperStatic.maxLng = null;

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

        if (HelperStatic.s_ArcGIS_APIKey == null)
        {
            HelperStatic.s_ArcGIS_APIKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();
        }

        foreach ((string strLat, string strLng) locationCoord in HelperStatic.hs_MapMarkers)
        {
            HelperStatic.HTMLAddMarker += "var marker = L.marker([" + locationCoord.strLat + ", " + locationCoord.strLng + "]).addTo(map).openPopup();" + "\n";
            strLatCoordinate = locationCoord.strLat;
            strLngCoordinate = locationCoord.strLng;

            // set scene for mix/max so map zoom can be set automatically
            {
                if (HelperStatic.minLat == null)
                {
                    HelperStatic.minLat = double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture);
                    HelperStatic.maxLat = HelperStatic.minLat;
                }

                if (double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture) < HelperStatic.minLat)
                {
                    HelperStatic.minLat = double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture);
                }

                if (double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture) > HelperStatic.maxLat)
                {
                    HelperStatic.maxLat = double.Parse(s: strLatCoordinate, provider: CultureInfo.InvariantCulture);
                }

                if (HelperStatic.minLng == null)
                {
                    HelperStatic.minLng = double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture);
                    HelperStatic.maxLng = HelperStatic.minLng;
                }

                if (double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture) < HelperStatic.minLng)
                {
                    HelperStatic.minLng = double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture);
                }

                if (double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture) > HelperStatic.maxLng)
                {
                    HelperStatic.maxLng = double.Parse(s: strLngCoordinate, provider: CultureInfo.InvariantCulture);
                }
            }
        }

        htmlCode = htmlCode.Replace(oldValue: "replaceLat", newValue: strLatCoordinate);
        htmlCode = htmlCode.Replace(oldValue: "replaceLng", newValue: strLngCoordinate);
        htmlCode = htmlCode.Replace(oldValue: "replaceMinLat", newValue: HelperStatic.minLat.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));
        htmlCode = htmlCode.Replace(oldValue: "replaceMinLng", newValue: HelperStatic.minLng.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));
        htmlCode = htmlCode.Replace(oldValue: "replaceMaxLat", newValue: HelperStatic.maxLat.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));
        htmlCode = htmlCode.Replace(oldValue: "replaceMaxLng", newValue: HelperStatic.maxLng.ToString()
                                        .Replace(oldChar: ',', newChar: '.'));

        htmlCode = htmlCode.Replace(oldValue: "yourApiKey", newValue: HelperStatic.s_ArcGIS_APIKey);
        htmlCode = htmlCode.Replace(oldValue: "{ HTMLAddMarker }", newValue: HelperStatic.HTMLAddMarker);
        wbv_MapArea.NavigateToString(htmlContent: htmlCode);
    }

    #endregion

    #region Menu Stuff

    #region File

    private async void tmi_File_SaveAll_Click(object sender,
                                              EventArgs e)
    {
        // i think having an Item active can cause a lock on it
        lvw_FileList.SelectedItems.Clear();
        // also the problem here is that the exiftoolAsync can still be running and locking the file.

        await HelperStatic.ExifWriteExifToFile();
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
        FrmEditFileData = new FrmEditFileData();
        FrmEditFileData.lvw_FileListEditImages.Items.Clear();
        foreach (ListViewItem selectedItem in lvw_FileList.SelectedItems)
        {
            if (File.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: selectedItem.Text)))
            {
                FolderName = tbx_FolderName.Text;
                FrmEditFileData.lvw_FileListEditImages.Items.Add(text: selectedItem.Text);
            }
        }

        FrmEditFileData.Text = HelperStatic.DataReadSQLiteObjectText(
            languageName: AppLanguage,
            objectType: "Form",
            objectName: "FrmEditFileData"
        );
        FrmEditFileData.ShowDialog();
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
    ///     Handles the tmi_File_ImportGPX_Click event -> Brings up the FrmImportGpx to import grack files
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <summary>
    ///     Handles the tmi_File_SaveAll_Click event -> triggers the file-save process
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
        HelperStatic.s_changeFolderIsOkay = false;
        await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
        if (HelperStatic.s_changeFolderIsOkay)
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
    private void tsb_GetAllFromWeb_Click(object sender,
                                         EventArgs e)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        ListView lvw = frmMainAppInstance.lvw_FileList;
        if (lvw.SelectedItems.Count > 0)
        {
            foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.SelectedItems)
            {
                // don't do folders...
                if (File.Exists(path: Path.Combine(path1: FolderName, path2: lvi.Text)))
                {
                    string strGpsLatitude = lvi.SubItems[index: lvw.Columns[key: "clh_GPSLatitude"]
                                                             .Index]
                        .Text.ToString(provider: CultureInfo.InvariantCulture);
                    string strGpsLongitude = lvi.SubItems[index: lvw.Columns[key: "clh_GPSLongitude"]
                                                              .Index]
                        .Text.ToString(provider: CultureInfo.InvariantCulture);
                    double parsedLat = 0.0;
                    double parsedLng = 0.0;
                    if (double.TryParse(s: strGpsLatitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strGpsLongitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
                    {
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

                            foreach ((string toponomyOverwriteName, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
                            {
                                HelperStatic.GenericUpdateAddToDataTable(
                                    dt: DtFileDataToWriteStage3ReadyToWrite,
                                    filePath: lvi.Text,
                                    settingId: toponomyDetail.toponomyOverwriteName,
                                    settingValue: toponomyDetail.toponomyOverwriteVal
                                );

                                lvi.SubItems[index: lvw.Columns[key: "clh_" + toponomyDetail.toponomyOverwriteName]
                                                 .Index]
                                    .Text = toponomyDetail.toponomyOverwriteVal;
                            }

                            lvi.ForeColor = Color.Red;
                            HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Processing: " + lvi.Text);
                        }

                        DataTable dtAltitude = HelperStatic.DTFromAPIExifGetAltitudeFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
                        if (dtAltitude.Rows.Count > 0)
                        {
                            HelperStatic.GenericUpdateAddToDataTable(
                                dt: DtFileDataToWriteStage3ReadyToWrite,
                                filePath: lvi.Text,
                                settingId: "GPSAltitude",
                                settingValue: dtAltitude.Rows[index: 0][columnName: "Altitude"]
                                    .ToString()
                            );
                            lvi.ForeColor = Color.Red;
                            HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Processing: " + lvi.Text);
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
        HelperStatic.s_changeFolderIsOkay = false;
        await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
        if (HelperStatic.s_changeFolderIsOkay)
        {
            tsb_Refresh_lvwFileList_Click(sender: this, e: new EventArgs());
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
        HelperStatic.s_changeFolderIsOkay = false;
        await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
        if (HelperStatic.s_changeFolderIsOkay)
        {
            string? tmpStrParent;
            string? tmpStrRoot;
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
            else
            {
                tbx_FolderName.Text = Directory.GetParent(path: tbx_FolderName.Text)
                    .ToString();
            }

            Application.DoEvents();
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
    ///     selected files
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tsb_RemoveGeoData_Click(object sender,
                                         EventArgs e)
    {
        HelperStatic.ExifRemoveLocationData(senderName: "FrmMainApp");
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
            HelperStatic.s_changeFolderIsOkay = false;
            await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
            if (HelperStatic.s_changeFolderIsOkay)
            {
                btn_ts_Refresh_lvwFileList_Click(sender: this, e: new EventArgs());
            }
        }
    }

    /// <summary>
    /// This handles the event when the user clicks into the textbox -> current value gets selected.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
        // i think having an Item active can cause a lock on it
        lvw_FileList.SelectedItems.Clear();
        // also the problem here is that the exiftoolAsync can still be running and locking the file.

        await HelperStatic.ExifWriteExifToFile();
        DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
    }

    #endregion

    #region lvw_FileList Interaction

    /// <summary>
    ///     Responsible for updating the main listview. For each file depending on the "compatible" or "incompatible" naming
    ///     ... it assigns the outstanding files according to compatibility and then runs the respective exiftool commands
    /// </summary>
    private async void lvwFileList_LoadOrUpdate()
    {
        List<string> listOfAysncCompatibleItems = new();
        List<string> listOfAysncIncompatibleItems = new();
        List<string> listOfNonUtf8Items = new();

        // this shoudn't really happen but just in case
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

        // list folders and stick them at the beginning of the listview
        List<string> dirs = Directory
            .GetDirectories(path: FolderName)
            .ToList();

        string[] allowedExtensions = new string[AncillaryListsArrays.AllCompatibleExtensions()
            .Length];
        Array.Copy(sourceArray: allowedExtensions, destinationArray: AncillaryListsArrays.AllCompatibleExtensions(), length: 0);
        for (int i = 0; i < allowedExtensions.Length; i++)
        {
            allowedExtensions[i] = AncillaryListsArrays.AllCompatibleExtensions()[i]
                .Split('\t')
                .First();
        }

        // list files that have whitelisted extensions
        List<string> files = Directory
            .GetFiles(path: FolderName)
            .Where(predicate: file => allowedExtensions.Any(predicate: file.ToLower()
                                                                .EndsWith))
            .ToList();

        files = files.OrderBy(keySelector: o => o)
            .ToList();
        foreach (string currentDir in dirs)
        {
            lvw_FileList_addListItem(fileName: Path.GetFileName(path: currentDir));
        }

        foreach (string currentFile in files)
        {
            string fileNameToTest = Path.Combine(currentFile);
            lvw_FileList_addListItem(fileName: Path.GetFileName(path: currentFile));

            // the add-in used here can't process nonstandard characters in filenames w/o an args file, which doesn't return what we're after.
            // so for 'standard' stuff we'll run async and for everything else we'll do it slower but more compatible

            if (Regex.IsMatch(input: fileNameToTest, pattern: @"^[a-zA-Z0-9.:\\_ ]*$"))
            {
                listOfAysncCompatibleItems.Add(item: Path.GetFileName(path: currentFile));
            }
            else
            {
                listOfAysncIncompatibleItems.Add(item: Path.GetFileName(path: currentFile));
            }
        }

        if (listOfAysncCompatibleItems.Count > 0)
        {
            HelperStatic.folderEnterLastEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await HelperStatic.ExifGetExifFromFilesCompatibleFileNames(files: listOfAysncCompatibleItems, folderEnterEpoch: HelperStatic.folderEnterLastEpoch);
        }

        if (listOfAysncIncompatibleItems.Count > 0)
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

            HelperStatic.folderEnterLastEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await HelperStatic.ExifGetExifFromFilesIncompatibleFileNames(files: listOfAysncIncompatibleItems, folderEnterEpoch: HelperStatic.folderEnterLastEpoch);
        }

        int filesWithGeoData = 0;
        foreach (ListViewItem lvi in lvw_FileList.Items)
        {
            if (lvi.SubItems.Count > 1)
            {
                if (lvi.SubItems[index: 1]
                        .Text !=
                    "-")
                {
                    filesWithGeoData++;
                }
            }
        }

        HandlerUpdateLabelText(label: lbl_ParseProgress, text: "Ready. Files: Total: " + files.Count + " Geodata: " + filesWithGeoData);
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
            // if this is a folder, enter
            if (Directory.Exists(path: Path.Combine(path1: tbx_FolderName.Text, path2: item.Text)))
            {
                // check for outstanding files first and save if user wants
                HelperStatic.s_changeFolderIsOkay = false;
                await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
                if (HelperStatic.s_changeFolderIsOkay)
                {
                    tbx_FolderName.Text = Path.Combine(path1: tbx_FolderName.Text, path2: item.Text);
                    btn_ts_Refresh_lvwFileList_Click(sender: this, e: new EventArgs());
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
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
                if (File.Exists(path: Path.Combine(FolderName +
                                                   lvw_FileList.SelectedItems[index: 0]
                                                       .Text)))
                {
                    await HelperStatic.LvwItemCreatePreview(fileNameWithPath: Path.Combine(FolderName +
                                                                                           lvw_FileList.SelectedItems[index: 0]
                                                                                               .Text));
                }

                // for folders and other non-valid items, don't do anything.
                if (HelperStatic.hs_MapMarkers.Count > 0)
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
            HelperStatic.s_NowSelectingAllItems = true;

            for (int i = 0; i < lvw_FileList.Items.Count; i++)
            {
                lvw_FileList.Items[index: i]
                    .Selected = true;
                // so because there is no way to do a proper "select all" w/o looping i only want to run the "navigate" (which is triggered on select-state-change at the end)
                if (i == lvw_FileList.Items.Count - 1)
                {
                    HelperStatic.s_NowSelectingAllItems = false;
                    await HelperStatic.LvwItemClickNavigate();
                    if (HelperStatic.hs_MapMarkers.Count > 0)
                    {
                        NavigateMapGo();
                    }
                }
            }

            // just in case...
            HelperStatic.s_NowSelectingAllItems = false;
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
            btn_OneFolderUp_Click(sender: sender, e: e);
        }

        // Enter  -> enter if folder
        else if (e.KeyCode == Keys.Enter)
        {
            string folderToEnter = lvw_FileList.SelectedItems[index: 0]
                .Text;
            // enter if folder
            if (Directory.Exists(path: Path.Combine(path1: FolderName, path2: folderToEnter)))
            {
                folderToEnter = Path.Combine(path1: FolderName, path2: folderToEnter);
                HelperStatic.s_changeFolderIsOkay = false;
                await HelperStatic.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
                if (HelperStatic.s_changeFolderIsOkay)
                {
                    if (!folderToEnter.EndsWith(value: "\\"))
                    {
                        tbx_FolderName.Text = folderToEnter + "\\";
                    }
                    else
                    {
                        tbx_FolderName.Text = folderToEnter;
                    }

                    try
                    {
                        lvw_FileList.Items.Clear();
                        HelperStatic.FsoCleanUpUserFolder();
                        FolderName = tbx_FolderName.Text;
                        lvwFileList_LoadOrUpdate();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInvalidFolder"), caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                    }
                }
            }
        }

        // F5 -> Refresh folder
        else if (e.KeyCode == Keys.F5)
        {
            tsb_Refresh_lvwFileList_Click(sender: sender, e: e);
            lvw_FileList.Items.Clear();
            FolderName = tbx_FolderName.Text;
            lvwFileList_LoadOrUpdate();
            e.Handled = true;
        }

        // Control S -> Save files
        else if (e.Control && e.KeyCode == Keys.S)
        {
            // i think having an Item active can cause a lock on it
            lvw_FileList.SelectedItems.Clear();

            await HelperStatic.ExifWriteExifToFile();
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
        IntPtr hSysImgList = NativeMethods.SHGetFileInfo("",
                                                         0,
                                                         ref shfi,
                                                         (uint)Marshal.SizeOf(shfi),
                                                         NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON);
        Debug.Assert(hSysImgList != IntPtr.Zero); // cross our fingers and hope to succeed!

        // Set the ListView control to use that image list.
        IntPtr hOldImgList = NativeMethods.SendMessage(lvw_FileList.Handle,
                                                       NativeMethods.LVM_SETIMAGELIST,
                                                       NativeMethods.LVSIL_SMALL,
                                                       hSysImgList);

        // If the ListView control already had an image list, delete the old one.
        if (hOldImgList != IntPtr.Zero)
        {
            NativeMethods.ImageList_Destroy(hOldImgList);
        }

        // Set up the ListView control's basic properties.
        // Set its theme so it will look like the one used by Explorer.
        NativeMethods.SetWindowTheme(lvw_FileList.Handle, "Explorer", null);

        // Get the items from the file system, and add each of them to the ListView,
        // complete with their corresponding name and icon indices.
        IntPtr himl = NativeMethods.SHGetFileInfo(Path.Combine(tbx_FolderName.Text, fileName),
                                                  0,
                                                  ref shfi,
                                                  (uint)Marshal.SizeOf(shfi),
                                                  NativeMethods.SHGFI_DISPLAYNAME | NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON);
        Debug.Assert(himl == hSysImgList); // should be the same imagelist as the one we set

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
        if (fileExtension != null && fileExtension != "")
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
            // problem here is that assume: my "Pictures" folder is _really_ called "DigiPics". shfi.szDisplayName = "Pictures" but that doesn't _really_ exist, which would cause a break later.
            // this hopefully sorts it.
            bool isSpecialFolder = false;
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(tbx_FolderName.Text, fileName));

            foreach (Environment.SpecialFolder specialFolder in Enum.GetValues(typeof(Environment.SpecialFolder)))
            {
                if (directoryInfo.FullName.ToString()
                        .ToLower() ==
                    Environment.GetFolderPath(specialFolder)
                        .ToLower())
                {
                    isSpecialFolder = true;
                    break;
                }
            }

            if (isSpecialFolder)
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

        lvw_FileList.Items.Add(value: lvi)
            .SubItems.AddRange(items: subItemList.ToArray());
    }

    /// <summary>
    ///     Watches for the user to lift the mouse button while over the listview. This will trigger the collection of
    ///     coordinates and map them.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void lvw_FileList_MouseUp(object sender,
                                            MouseEventArgs e)
    {
        if (!HelperStatic.s_NowSelectingAllItems)
        {
            await HelperStatic.LvwItemClickNavigate();
            // for folders and other non-valid items, don't do anything.
            if (HelperStatic.hs_MapMarkers.Count > 0)
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
    /// <param name="item">The particular listViewItem that needs updating</param>
    /// <param name="color">Parameter to assign a particular colour (prob red or black) to the whole row</param>
    internal static void HandlerUpdateItemColour(ListView lvw,
                                                 string item,
                                                 Color color)
    {
        // If the current thread is not the UI thread, InvokeRequired will be true
        if (lvw.InvokeRequired)
        {
            lvw.Invoke(method: (Action)(() => HandlerUpdateItemColour(lvw: lvw, item: item, color: color)));
            return;
        }

        ListViewItem itemToModify = lvw.FindItemWithText(text: item);
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
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
        var doubleBufferPropertyInfo = control.GetType()
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        doubleBufferPropertyInfo.SetValue(control, enable, null);
    }
}