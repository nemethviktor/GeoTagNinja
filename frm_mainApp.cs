using CoenM.ExifToolLib;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja
{
    public partial class frm_MainApp : Form
    {
        /// <summary>
        /// These two make the elements of the main listview accessible to other classes.
        /// </summary>
        public ListView.ListViewItemCollection ListViewItems
        {
            get { return lvw_FileList.Items; }
        }
        public ListView.ColumnHeaderCollection ListViewColumnHeaders
        {
            get { return lvw_FileList.Columns; }
        }

        #region Variables
        internal static string resourcesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        internal static string userDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GeoTagNinja");
        internal const string doubleQuote = "\"";
        internal static string latCoordinate;
        internal static string lngCoordinate;
        internal static DataTable objectNames;
        internal static DataTable objectTagNames_In;
        internal static DataTable objectTagNames_Out;
        internal static string folderName;
        internal static string appLanguage = "english"; // default to english 
        internal static bool dontShowLocToMapDialog;
        internal frm_Settings FrmSettings;
        internal frm_editFileData FrmEditFileData;

        /// <summary>
        /// this one basically handles what extensions we work with.
        /// the actual list is used for file-specific Settings as well as the general running of the app
        /// leave the \t in!
        /// </summary>
        internal static string[] allExtensions = new[]
            {
            "arq	Sony Alpha Pixel-Shift RAW (TIFF-based)"
            ,"arw	Sony Alpha RAW (TIFF-based)"
            ,"cr2	Canon RAW 2 (TIFF-based) (CR2 spec)"
            ,"cr3	Canon RAW 3 (QuickTime-based) (CR3 spec)"
            ,"dcp	DNG Camera Profile (DNG-like)"
            ,"dng	Digital Negative (TIFF-based)"
            ,"erf	Epson RAW Format (TIFF-based)"
            ,"exv	Exiv2 metadata file (JPEG-based)"
            ,"fff	Hasselblad Flexible File Format (TIFF-based)"
            ,"gpr	GoPro RAW (DNG-based)"
            ,"hdp	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)"
            ,"heic	High Efficiency Image Format (QuickTime-based)"
            ,"heif	High Efficiency Image Format (QuickTime-based)"
            ,"hif	High Efficiency Image Format (QuickTime-based)"
            ,"iiq	Phase One Intelligent Image Quality RAW (TIFF-based)"
            ,"insp	Insta360 Picture (JPEG-based)"
            ,"jp2	JPEG 2000 image [Compound/Extended]"
            ,"jpe	Joint Photographic Experts Group image"
            ,"jpeg	Joint Photographic Experts Group image"
            ,"jpf	JPEG 2000 image [Compound/Extended]"
            ,"jpg	Joint Photographic Experts Group image"
            ,"jpm	JPEG 2000 image [Compound/Extended]"
            ,"jpx	JPEG 2000 image [Compound/Extended]"
            ,"jxl	JPEG XL (codestream and ISO BMFF)"
            ,"jxr	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)"
            ,"mef	Mamiya (RAW) Electronic Format (TIFF-based)"
            ,"mie	Meta Information Encapsulation (MIE specification)"
            ,"mos	Creo Leaf Mosaic (TIFF-based)"
            ,"mpo	Extended Multi-Picture format (JPEG with MPF extensions)"
            ,"mrw	Minolta RAW"
            ,"nef	Nikon (RAW) Electronic Format (TIFF-based)"
            ,"nrw	Nikon RAW (2) (TIFF-based)"
            ,"orf	Olympus RAW Format (TIFF-based)"
            ,"ori	Olympus RAW Format (TIFF-based)"
            ,"pef	Pentax (RAW) Electronic Format (TIFF-based)"
            ,"raf	FujiFilm RAW Format"
            ,"raw	Kyocera Contax N Digital RAW"
            ,"rw2	Panasonic RAW 2 (TIFF-based)"
            ,"rwl	Leica RAW (TIFF-based)"
            ,"sr2	Sony RAW 2 (TIFF-based)"
            ,"srw	Samsung RAW format (TIFF-based)"
            ,"thm	Thumbnail image (JPEG)"
            ,"tif	QuickTime Image File"
            ,"tiff	Tagged Image File Format"
            ,"wdp 	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)"
            ,"x3f	Sigma/Foveon RAW"

            };
        internal AsyncExifTool asyncExifTool;

        internal static DataTable dt_fileDataCopyPool;
        internal static DataTable dt_fileDataToWriteStage1PreQueue;
        internal static DataTable dt_fileDataToWriteStage2QueuePendingSave;
        internal static DataTable dt_fileDataToWriteStage3ReadyToWrite;

        #endregion
        #region Methods
        #region Form/App Related
        /// <summary>
        /// This is the main Form for the app. This particular section handles the initialisation of the form and loading various defaults.
        /// </summary>
        public frm_MainApp()
        {
            // initialise exifTool - leave as-is
            #region ExifToolConfiguration
            var exifToolExe = Path.Combine(frm_MainApp.resourcesFolderPath, "exiftool.exe");

            Encoding exifToolEncoding = Encoding.UTF8;

            var commonArgs = new List<string> { };

            var customExifToolConfigFile = @".ExifTool_config";

            AsyncExifToolConfiguration asyncExifToolConfiguration = string.IsNullOrWhiteSpace(customExifToolConfigFile)
                ? new AsyncExifToolConfiguration(exifToolExe, exifToolEncoding, commonArgs)
                : new AsyncExifToolConfiguration(exifToolExe, customExifToolConfigFile, exifToolEncoding, commonArgs);

            asyncExifTool = new AsyncExifTool(asyncExifToolConfiguration);
            asyncExifTool.Initialize();
            #endregion
            #region Load Settings
            // load all settings
            try
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GeoTagNinja"));
                Helper.DataCreateSQLiteDB();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorCantCreateSQLiteDB") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            // write settings for combobox defaults etc
            try
            {
                Helper.DataWriteSQLiteSettingsDefaultSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorCantWriteSQLiteDB") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            // read language and objectnames
            try
            {
                appLanguage = Helper.DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "tpg_Application",
                    settingId: "cbx_Language"
                    );
                objectNames = Helper.DataReadSQLiteObjectMapping(
                    tableName: "objectNames",
                    orderBy: "sqlOrder"
                    );
                objectTagNames_In = Helper.DataReadSQLiteObjectMapping(tableName: "objectTagNames_In");
                objectTagNames_Out = Helper.DataReadSQLiteObjectMapping(tableName: "objectTagNames_Out");
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorCantLoadSQLiteDB") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            // get some defaults
            try
            {
                Helper.s_ArcGIS_APIKey = Helper.DataSelectTbxARCGIS_APIKey_FromSQLite();
                Helper.s_GeoNames_UserName = Helper.DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "tpg_Application",
                    settingId: "tbx_GeoNames_UserName"
                    );
                Helper.s_GeoNames_Pwd = Helper.DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "tpg_Application",
                    settingId: "tbx_GeoNames_Pwd"
                    );
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorCantReadDefaultSQLiteDB") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            // check webView2 is available
            try
            {
                var version = "";
                version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorCantLoadWebView2") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            // InitializeComponent();
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInitializeComponent") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // get double-buffering to avoid flickering listviews
            try
            {
                ControlExtensions.DoubleBuffered(lvw_FileList, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorDoubleBuffer") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // get objectnames (col names, etc.)
            try
            {
                objectNames.DefaultView.Sort = "sqlOrder";
                DataTable dt = objectNames.DefaultView.ToTable();
                foreach (DataRow dr in dt.Rows)
                {
                    ColumnHeader clh = new();
                    clh.Name = "clh_" + dr["objectName"].ToString();
                    lvw_FileList.Columns.Add(clh);
                }
                foreach (ColumnHeader clh in lvw_FileList.Columns)
                {

                    clh.Text = Helper.DataReadSQLiteObjectText(
                        languageName: appLanguage,
                        objectType: "ColumnHeader",
                        objectName: clh.Name
                        );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorLanguageFileColumnHeaders") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            #endregion
            this.FormClosing += new FormClosingEventHandler(frm_MainApp_FormClosing);
            #region create dataTables

            // dt_fileDataCopyPool
            dt_fileDataCopyPool = new DataTable();
            dt_fileDataCopyPool.Clear();
            dt_fileDataCopyPool.Columns.Add("settingId");
            dt_fileDataCopyPool.Columns.Add("settingValue");

            // dt_fileDataToWriteStage1PreQueue 
            dt_fileDataToWriteStage1PreQueue = new DataTable();
            dt_fileDataToWriteStage1PreQueue.Clear();
            dt_fileDataToWriteStage1PreQueue.Columns.Add("filePath");
            dt_fileDataToWriteStage1PreQueue.Columns.Add("settingId");
            dt_fileDataToWriteStage1PreQueue.Columns.Add("settingValue");

            // dt_fileDataToWriteStage2QueuePendingSave 
            dt_fileDataToWriteStage2QueuePendingSave = new DataTable();
            dt_fileDataToWriteStage2QueuePendingSave.Clear();
            dt_fileDataToWriteStage2QueuePendingSave.Columns.Add("filePath");
            dt_fileDataToWriteStage2QueuePendingSave.Columns.Add("settingId");
            dt_fileDataToWriteStage2QueuePendingSave.Columns.Add("settingValue");

            // dt_fileDataToWriteStage3ReadyToWrite 
            dt_fileDataToWriteStage3ReadyToWrite = new DataTable();
            dt_fileDataToWriteStage3ReadyToWrite.Clear();
            dt_fileDataToWriteStage3ReadyToWrite.Columns.Add("filePath");
            dt_fileDataToWriteStage3ReadyToWrite.Columns.Add("settingId");
            dt_fileDataToWriteStage3ReadyToWrite.Columns.Add("settingValue");
            #endregion
        }
        /// <summary>
        /// Handles the initial loading - adds various elements and ensures the app functions.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private async void frm_MainApp_Load(object sender, EventArgs e)
        {

            // icon
            this.Icon = Properties.Resources.AppIcon;

            // clear both tables, just in case + generic cleanup
            try
            {
                frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Clear();
                frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorClearingFileDataQTables") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            try { Helper.FsoCleanUpUserFolder(); } catch { }

            // resize columns
            try
            {
                VisualReadLvw_FileList_ColWidth(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorResizingColumns") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            wbv_MapArea.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;

            #region StartupFolder
            string val_StartupFolder = "";
            try
            {
                val_StartupFolder = Helper.DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "tpg_Application",
                    settingId: "tbx_Startup_Folder"
                    );

            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorSettingStartupFolder") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (val_StartupFolder == null)
            {
                val_StartupFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }

            if (val_StartupFolder.EndsWith("\\"))
            {
                tbx_FolderName.Text = val_StartupFolder;
            }
            else
            {
                tbx_FolderName.Text = val_StartupFolder + "\\";
            }

            #endregion

            // initialise webview2
            await InitialiseWebView();

            //assign labels to objects
            #region AppLanguage
            Helper_NonStatic Helper_nonstatic = new();
            IEnumerable<Control> c = Helper_nonstatic.GetAllControls(this);
            foreach (Control cItem in c)
            {
                if (cItem.GetType() == typeof(MenuStrip) || cItem.GetType() == typeof(ToolStripMenuItem) || cItem.GetType() == typeof(Label) || cItem.GetType() == typeof(Button) || cItem.GetType() == typeof(CheckBox) || cItem.GetType() == typeof(TabPage))
                {
                    if (cItem.Name == "lbl_ParseProgress")
                    {
                        cItem.Text = cItem.Text = Helper.DataReadSQLiteObjectText(
                            languageName: appLanguage,
                            objectType: cItem.GetType().ToString().Split('.').Last(),
                            actionType: "Normal",
                            objectName: cItem.Name
                            );
                    }
                    else
                    {
                        cItem.Text = Helper.DataReadSQLiteObjectText(
                            languageName: appLanguage,
                            objectType: cItem.GetType().ToString().Split('.').Last(),
                            objectName: cItem.Name
                            );
                    }
                }
            }

            // don't think the menustrip above is working
            List<ToolStripItem> allMenuItems = new();
            foreach (ToolStripItem toolItem in mns_MenuStrip.Items)
            {
                allMenuItems.Add(toolItem);
                //add sub items
                allMenuItems.AddRange(Helper_nonstatic.GetMenuItems(toolItem));
            }
            foreach (ToolStripItem cItem in allMenuItems)
            {
                if (cItem is ToolStripMenuItem)
                {
                    ToolStripMenuItem tsmi = (cItem as ToolStripMenuItem);
                    cItem.Text = cItem.Text = Helper.DataReadSQLiteObjectText(
                        languageName: appLanguage,
                        objectType: cItem.GetType().ToString().Split('.').Last(),
                        objectName: cItem.Name
                        );
                }
                else if (cItem is ToolStripSeparator)
                {
                    ToolStripSeparator tss = (cItem as ToolStripSeparator);
                }
            }

            #endregion

            VisualResizeAppElements(this);

            // load lvwFileList
            lvwFileList_LoadOrUpdate();
            tbx_lng.KeyPress += (sndr, ev) =>
            {
                if (ev.KeyChar.Equals((char)13))
                {
                    btn_NavigateMapGo.PerformClick();
                    ev.Handled = true; // suppress default handling
                }
            };
            this.SizeChanged += frm_MainApp_SizeChanged;
            try
            {
                this.tbx_lat.Text = Helper.DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "generic",
                    settingId: "lastLat"
                    );
                this.tbx_lng.Text = Helper.DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "generic",
                    settingId: "lastLng"
                    );
            }
            catch
            {
            }
            if (tbx_lat.Text == "" || tbx_lat.Text == "0")
            {
                // NASA HQ
                string defaultLat = "38.883056";
                string defaultLng = "-77.016389";
                this.tbx_lat.Text = defaultLat;
                this.tbx_lng.Text = defaultLng;
            }
            NavigateMapGo(tbx_lat.Text, tbx_lng.Text);

            #region query current & newest exifTool version
            // get current & newest exiftool version -- do this here at the end so it doesn't hold up the process
            // exiftool doesn't get released (proper) to github...technically we could query https://api.github.com/repos/exiftool/exiftool/tags
            // but i'm not sure that's particularly safe in case he ever changes the tags (unlikely tho')
            Helper.ExifGetExifToolVersion();
            string currentExifToolVersion = Helper.DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "generic",
                    settingId: "exifToolVer"
                    );


            Helper.s_APIOkay = true;
            DataTable dt_APIExifToolVersion = Helper.DTFromAPI_GetExifToolVersion();
            string newestExifToolVersion = dt_APIExifToolVersion.Rows[0]["version"].ToString();
            if (currentExifToolVersion != null && currentExifToolVersion != "" && currentExifToolVersion != newestExifToolVersion)
            {
                if (MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_InfoNewExifToolVersionExists") + newestExifToolVersion, "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("https://exiftool.org/exiftool-" + newestExifToolVersion + ".zip");
                }
            }
            #endregion
            #region query current & newest GTN version
            // current version may be something like "0.5.8251.40825"
            string currentGTNVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // don't run this in debug mode. currentGTNVersion is dependent on the # of days since 1/1/2000 basically.
            // so each time i make a new build this updates and the query triggers a messagebox...which for me is a bit useless.
#if !DEBUG
            Helper.s_APIOkay = true;
            DataTable dt_APIGTNVersion = Helper.DTFromAPI_GetGTNVersion();
            // newest may be something like "v0.5.8251"
            string newestGTNVersion = dt_APIGTNVersion.Rows[0]["version"].ToString().Replace("v", "");
            if (!currentGTNVersion.Contains(newestGTNVersion))
            {
                if (MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_InfoNewGTNVersionExists") + newestGTNVersion, "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("https://github.com/nemethviktor/GeoTagNinja/releases/download/" + dt_APIGTNVersion.Rows[0]["version"].ToString() + "/GeoTagNinja_Setup.msi");
                }
            }
#endif
            #endregion
        }
        /// <summary>
        /// Initialises the map in the app and browses to the default or last-used location.
        /// </summary>
        /// <returns></returns>
        private async Task InitialiseWebView()
        {
            try
            {
                // silly thing dumps the folder by default right into Program Files where it can't write further due to permission issues
                // need to move it elsewhere.

                CoreWebView2Environment c2wv = await CoreWebView2Environment.CreateAsync(null,
                                                            Path.GetTempPath(),
                                                            new CoreWebView2EnvironmentOptions(null, "en", null));
                await wbv_MapArea.EnsureCoreWebView2Async(c2wv);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInitializeWebViewEnsureCoreWebView2Async") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                wbv_MapArea.CoreWebView2.Settings.IsWebMessageEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInitializeWebViewIsWebMessageEnabled") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // read the "map.html" file.
            string HTMLCode = "";
            try
            {
                HTMLCode = File.ReadAllText(@Path.Combine(frm_MainApp.resourcesFolderPath, "map.html"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInitializeWebViewReadHTMLFile") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                string strLatCoordinate = this.tbx_lat.Text.Replace(',', '.');
                string strLngCoordinate = this.tbx_lng.Text.Replace(',', '.');

                double parsedLat;
                double parsedLng;
                if (double.TryParse(strLatCoordinate, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLat) && double.TryParse(strLngCoordinate, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLng))
                {
                    latCoordinate = strLatCoordinate;
                    lngCoordinate = strLngCoordinate;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInitializeWebViewParseCoordsFromHTMLFile") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // replace hard-coded values in the html code
            try
            {
                if (Helper.s_ArcGIS_APIKey == null)
                {
                    Helper.s_ArcGIS_APIKey = Helper.DataSelectTbxARCGIS_APIKey_FromSQLite();

                }
                HTMLCode = HTMLCode.Replace("yourApiKey", Helper.s_ArcGIS_APIKey);
                HTMLCode = HTMLCode.Replace("replaceLat", latCoordinate.ToString());
                HTMLCode = HTMLCode.Replace("replaceLng", lngCoordinate.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInitializeWebViewReplaceStringInHTMLFile") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // show the decoded location on the map
            try
            {
                wbv_MapArea.NavigateToString(HTMLCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInitializeWebViewNavigateToStringInHTMLFile") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                wbv_MapArea.WebMessageReceived += wbv_MapArea_WebMessageReceived;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInitializeWebViewWebMessageReceived") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        /// <summary>
        /// When the app closes we want to make sure there's nothing in the write-queue.
        /// ...once that's dealt with we write the details of the app layout (e.g. column widths) to sqlite.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private async void frm_MainApp_FormClosing(System.Object sender, System.Windows.Forms.FormClosingEventArgs e)
        {

            // check if there is any data in the write-Q
            if (dt_fileDataToWriteStage3ReadyToWrite.Rows.Count > 0)
            {
                DialogResult dialogResult = MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_QuestionFileQIsNotEmpty"), "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    await Helper.ExifWriteExifToFile();
                }
                else if (dialogResult == DialogResult.No)
                {
                    frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Clear();
                }
            }
            // write column widths to db
            VisualWriteLvw_FileList_ColWidth(this);

            // write lat/long for future reference to db
            Helper.DataWriteSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "lastLat",
                settingValue: this.tbx_lat.Text
                );
            Helper.DataWriteSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "lastLng",
                settingValue: this.tbx_lng.Text
                );

            // clean up
            this.pbx_imagePreview.Image = null; // unlocks files. theoretically.
            Helper.FsoCleanUpUserFolder();

            // force it otherwise it keeps a lock on for a few seconds.
            asyncExifTool.Dispose();
        }
        /// <summary>
        /// this is to deal with the icons in listview
        /// from https://stackoverflow.com/a/37806517/3968494
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
        }
        #endregion
        #region Resizing Stuff
        /// <summary>
        /// When the app resizes automatically this adjusts the elements to fit the view.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void frm_MainApp_SizeChanged(object sender, EventArgs e)
        {
            VisualResizeAppElements(this);
        }
        /// <summary>
        /// Identical to the above but this is the one actually executing
        /// </summary>
        /// <param name="frm_mainApp">Make a guess</param>
        private void VisualResizeAppElements(frm_MainApp frm_mainApp)
        {
            frm_mainApp.tsr_MainAppToolStrip.Top = Convert.ToInt16(mns_MenuStrip.Bottom + 2);
            frm_mainApp.tsr_MainAppToolStrip.Left = 5;

            frm_mainApp.tsr_FolderControl.Top = Convert.ToInt16(tsr_MainAppToolStrip.Bottom + 2);
            frm_mainApp.tsr_FolderControl.Left = 5;

            frm_mainApp.tct_Main.Width = Convert.ToInt16(Width * 0.4);
            frm_mainApp.tct_Main.Top = Convert.ToInt16(tsr_FolderControl.Bottom + 2);
            frm_mainApp.tct_Main.Height = Convert.ToInt16(Bottom - tsr_FolderControl.Bottom - 95);
            frm_mainApp.tct_Main.Left = Convert.ToInt16(Width - frm_mainApp.tct_Main.Width - 20);

            // label & textbox arrangements
            frm_mainApp.lbl_lat.Top = frm_mainApp.tct_Main.Bottom + 2;
            frm_mainApp.tbx_lat.Top = frm_mainApp.lbl_lat.Top;
            frm_mainApp.lbl_lat.Left = frm_mainApp.tct_Main.Left;
            frm_mainApp.tbx_lat.Left = frm_mainApp.lbl_lat.Right + 2;

            frm_mainApp.lbl_lng.Top = frm_mainApp.tct_Main.Bottom + 2;
            frm_mainApp.tbx_lng.Top = frm_mainApp.lbl_lat.Top;
            frm_mainApp.lbl_lng.Left = frm_mainApp.tbx_lat.Right + 2;
            frm_mainApp.tbx_lng.Left = frm_mainApp.lbl_lng.Right + 2;

            // map buttons
            frm_mainApp.btn_NavigateMapGo.Left = frm_mainApp.tbx_lng.Right + 2;
            frm_mainApp.btn_NavigateMapGo.Top = frm_mainApp.tbx_lng.Top;
            frm_mainApp.btn_loctToFile.Left = frm_mainApp.btn_NavigateMapGo.Right + 2;
            frm_mainApp.btn_loctToFile.Top = frm_mainApp.tbx_lng.Top;

            // fileList
            frm_mainApp.lvw_FileList.Top = frm_mainApp.tct_Main.Top;
            frm_mainApp.lvw_FileList.Left = Convert.ToInt16(Left + 20);
            frm_mainApp.lvw_FileList.Height = Convert.ToInt16((frm_mainApp.tct_Main.Height) * 0.75);
            frm_mainApp.lvw_FileList.Width = frm_mainApp.tct_Main.Left - 20;

            // pbx_imagePreview
            frm_mainApp.pbx_imagePreview.Top = frm_mainApp.lvw_FileList.Bottom + 5;
            frm_mainApp.pbx_imagePreview.Left = frm_mainApp.lvw_FileList.Left;
            frm_mainApp.pbx_imagePreview.Height = Convert.ToInt16((frm_mainApp.tct_Main.Height) * 0.24);
            frm_mainApp.pbx_imagePreview.Width = Convert.ToInt16((frm_mainApp.pbx_imagePreview.Height) * 1.5); //frm_mainApp.lvw_FileList.Width;

            //frm_mainApp.pnl_PictureBox.Controls.Add(pbx_imagePreview);
            //frm_mainApp.pnl_PictureBox.Top = frm_mainApp.lvw_FileList.Bottom + 5;
            //frm_mainApp.pnl_PictureBox.Left = frm_mainApp.lvw_FileList.Left;
            //frm_mainApp.pnl_PictureBox.Height = Convert.ToInt16((frm_mainApp.tct_Main.Height) * 0.24);
            //frm_mainApp.pnl_PictureBox.Width = frm_mainApp.lvw_FileList.Width;

            // lbl_ParseProgress
            frm_mainApp.lbl_ParseProgress.Top = frm_mainApp.btn_NavigateMapGo.Top;
            frm_mainApp.lbl_ParseProgress.Left = frm_mainApp.lvw_FileList.Left;
            frm_mainApp.lbl_ParseProgress.Width = frm_mainApp.lvw_FileList.Width;
        }
        /// <summary>
        /// Reads the widths of individual CLHs from SQL, if not found assigns them "auto" (-2)
        /// </summary>
        /// <param name="frm_mainApp">Make a guess</param>
        /// <exception cref="InvalidOperationException">If it encounters a missingCLH</exception>
        private void VisualReadLvw_FileList_ColWidth(frm_MainApp frm_mainApp)
        {
            string settingIdToSend;
            int colWidth = 0;
            // logic: see if it's in SQL first...if not then set to Auto
            foreach (ColumnHeader columnHeader in frm_mainApp.lvw_FileList.Columns)
            {
                // columnHeader.Name doesn't get automatically recorded, i think that's a VSC bug.
                // anyway will introduce a breaking-line here for that.
                // oh and can't convert bool to str but it's not letting to deal w it otherwise anyway so going for length == 0 instead
                if (columnHeader.Name.Length == 0)
                {
                    throw new InvalidOperationException("columnHeader name missing");
                }
                settingIdToSend = lvw_FileList.Name + "_" + columnHeader.Name + "_width";
                colWidth = Convert.ToInt16(Helper.DataReadSQLiteSettings(
                    tableName: "applayout",
                    settingTabPage: "lvw_FileList",
                    settingId: settingIdToSend)
                    );
                if (colWidth == 0) // a null value would be parsed to zero
                {
                    switch (columnHeader.Name.Substring(4))
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
                    switch (columnHeader.Name.Substring(4))
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
        /// Sends the CLH width to SQL for writing.
        /// </summary>
        /// <param name="frm_mainApp">Make a guess</param>
        private void VisualWriteLvw_FileList_ColWidth(frm_MainApp frm_mainApp)
        {

            string settingIdToSend;
            foreach (ColumnHeader columnHeader in frm_mainApp.lvw_FileList.Columns)
            {
                if (columnHeader.Width != -2) // actually this doesn't work but low-pri to fix
                {
                    settingIdToSend = lvw_FileList.Name + "_" + columnHeader.Name + "_width";
                    Helper.DataWriteSQLiteSettings(
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
        public class MapGPSCoordinates
        {
            public double lat { get; set; }
            public double lng { get; set; }
        }
        /// <summary>
        /// Provides an interaction layer between the map and the app. The reason why we're using string instead of proper numbers
        /// ... is that the API only deals with English-formatted numbers whereas we can't force that necessarily on the user if they have
        /// ... other Culture setting. 
        /// ... Also if the user zooms out too much they can click on a map-area (coordinate) that's not "real" so we are dealing with that in this code.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void wbv_MapArea_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var JsonString = e.WebMessageAsJson;

            MapGPSCoordinates mapGPSCoordinates =
                System.Text.Json.JsonSerializer.Deserialize<MapGPSCoordinates>(JsonString);
            string strLat = $"{mapGPSCoordinates?.lat}".ToString().Replace(',', '.');
            string strLng = $"{mapGPSCoordinates?.lng}".ToString().Replace(',', '.');
            double dblLat;
            double dblLng;
            double.TryParse(strLat, NumberStyles.Any, CultureInfo.InvariantCulture, out dblLat); // trust me i hate this f...king culture thing as much as possible...
            double.TryParse(strLng, NumberStyles.Any, CultureInfo.InvariantCulture, out dblLng); // trust me i hate this f...king culture thing as much as possible...
            // if the user zooms out too much they can encounter an "unreal" coordinate.
            if (dblLng < -180)
            {
                dblLng = 180 - (Math.Abs(dblLng) % 180);
            }
            else if (dblLng > 180)
            {
                dblLng = (Math.Abs(dblLng) % 180);
            }

            this.tbx_lat.Text = Math.Round(dblLat, 6).ToString().Replace(',', '.');
            this.tbx_lng.Text = Math.Round(dblLng, 6).ToString().Replace(',', '.');

        }
        /// <summary>
        /// Needed for the proper functioning of webview2
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void webView_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {

        }
        /// <summary>
        /// Handles the clicking on Go button
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void btn_NavigateMapGo_Click(object sender, EventArgs e)
        {
            NavigateMapGo(this.tbx_lat.Text.ToString(), this.tbx_lng.Text.ToString());
        }
        /// <summary>
        /// Handles the clicking on "ToFile" button. See comments above re: why we're using strings (culture-related issue)
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void btn_loctToFile_Click(object sender, EventArgs e)
        {
            string strParsedLat = tbx_lat.Text.Replace(',', '.');
            string strParsedLng = tbx_lng.Text.Replace(',', '.');
            double parsedLat;
            double parsedLng;
            geoTagNinja.GeoResponseToponomy ReadJson_Toponomy = new();
            geoTagNinja.GeoResponseAltitude ReadJson_Altitude = new();
            DataRow dr_FileDataRow;

            // lat/long gets written regardless of update-toponomy-choice
            if (double.TryParse(strParsedLat, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLat) && double.TryParse(strParsedLng, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLng))
            {
                if (lvw_FileList.SelectedItems.Count > 0)
                {
                    foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                    {
                        // don't do folders...
                        if (File.Exists(Path.Combine(folderName, lvi.Text)))
                        {
                            // Latitude
                            dr_FileDataRow = dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "GPSLatitude";
                            dr_FileDataRow["settingValue"] = strParsedLat;
                            dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                            // Longitude
                            dr_FileDataRow = dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                            dr_FileDataRow["filePath"] = lvi.Text;
                            dr_FileDataRow["settingId"] = "GPSLongitude";
                            dr_FileDataRow["settingValue"] = strParsedLng;
                            dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);
                        }
                    }
                }
            }

            if (double.TryParse(strParsedLat, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLat) && double.TryParse(strParsedLng, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLng))
            {
                if (lvw_FileList.SelectedItems.Count > 0)
                {
                    DialogResult dialogResult = DialogResult.Yes; // can't leave it as null
                    if (dontShowLocToMapDialog != true)
                    {
                        MessageBoxManager.Cancel = "Stop Asking"; // for the time being this will be hard-coded English
                        MessageBoxManager.Register();
                        dialogResult = MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_QuestionAddToponomy"), "Info", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        MessageBoxManager.Unregister();
                    }

                    foreach (ListViewItem lvi in lvw_FileList.SelectedItems)
                    {
                        // don't do folders...
                        if (File.Exists(Path.Combine(folderName, lvi.Text)))
                        {
                            DataTable dt_Toponomy = new();
                            DataTable dt_Altitude = new();
                            if (dialogResult == DialogResult.Yes || dialogResult == DialogResult.Cancel || dontShowLocToMapDialog == true)
                            {
                                if (dialogResult == DialogResult.Cancel)
                                {
                                    dontShowLocToMapDialog = true;
                                }
                                Helper.s_APIOkay = true;
                                dt_Toponomy = Helper.DTFromAPIExifGetToponomyFromWebOrSQL(strParsedLat, strParsedLng);
                                dt_Altitude = Helper.DTFromAPIExifGetAltitudeFromWebOrSQL(strParsedLat, strParsedLng);
                                if (Helper.s_APIOkay)
                                {
                                    string CountryCode = dt_Toponomy.Rows[0]["CountryCode"].ToString();
                                    string Country = dt_Toponomy.Rows[0]["Country"].ToString();
                                    string City = dt_Toponomy.Rows[0]["City"].ToString();
                                    string State = dt_Toponomy.Rows[0]["State"].ToString();
                                    string Sub_location = dt_Toponomy.Rows[0]["Sub_location"].ToString();
                                    string Altitude = dt_Altitude.Rows[0]["Altitude"].ToString();

                                    // CountryCode
                                    dr_FileDataRow = dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "CountryCode";
                                    dr_FileDataRow["settingValue"] = CountryCode;
                                    dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                                    // Country
                                    dr_FileDataRow = dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "Country";
                                    dr_FileDataRow["settingValue"] = Country;
                                    dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                                    // City
                                    dr_FileDataRow = dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "City";
                                    dr_FileDataRow["settingValue"] = City;
                                    dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                                    // State
                                    dr_FileDataRow = dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "State";
                                    dr_FileDataRow["settingValue"] = State;
                                    dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                                    // Sub_location
                                    dr_FileDataRow = dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "Sub_location";
                                    dr_FileDataRow["settingValue"] = Sub_location;
                                    dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);

                                    // Altitude
                                    dr_FileDataRow = dt_fileDataToWriteStage3ReadyToWrite.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "GPSAltitude";
                                    dr_FileDataRow["settingValue"] = Altitude;
                                    dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr_FileDataRow);
                                }
                            }
                        }
                    }
                    Helper.LwvUpdateRowFromDTWriteStage3ReadyToWrite();
                }
            }
        }
        /// <summary>
        /// Handles the navigation to a coordinate on the map. Replaces hard-coded values w/ user-provided ones
        /// ... and executes the navigation action.
        /// </summary>
        internal void NavigateMapGo(string strLatCoordinate, string strLngCoordinate)
        {
            string HTMLCode = "";
            try
            {
                HTMLCode = File.ReadAllText(Path.Combine(resourcesFolderPath, "map.html"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorNavigateMapGoHTMLCode") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            strLatCoordinate = strLatCoordinate.Replace(',', '.');
            strLngCoordinate = strLngCoordinate.Replace(',', '.');

            if (Helper.s_ArcGIS_APIKey == null)
            {
                Helper.s_ArcGIS_APIKey = Helper.DataSelectTbxARCGIS_APIKey_FromSQLite();

            }
            HTMLCode = HTMLCode.Replace("yourApiKey", Helper.s_ArcGIS_APIKey);
            HTMLCode = HTMLCode.Replace("replaceLat", strLatCoordinate);
            HTMLCode = HTMLCode.Replace("replaceLng", strLngCoordinate);

            wbv_MapArea.NavigateToString(HTMLCode);
        }
        #endregion
        #region Menu Stuff
        /// <summary>
        /// Handles the tmi_Settings_Settings_Click event -> brings up the Settings Form
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void tmi_Settings_Settings_Click(object sender, EventArgs e)
        {
            FrmSettings = new frm_Settings();
            FrmSettings.Text = Helper.DataReadSQLiteObjectText(
                languageName: appLanguage,
                objectType: "Form",
                objectName: "frm_Settings"
                );
            FrmSettings.ShowDialog();
        }
        /// <summary>
        /// Handles the tmi_Help_About_Click event -> brings up the About Form
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void tmi_Help_About_Click(object sender, EventArgs e)
        {
            frm_aboutBox frm_aboutBox = new();
            frm_aboutBox.ShowDialog();
        }
        /// <summary>
        /// Handles the tmi_File_Quit_Click event -> cleans the user-folder then quits the app
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void tmi_File_Quit_Click(object sender, EventArgs e)
        {
            Helper.FsoCleanUpUserFolder();
            Application.Exit();
        }
        /// <summary>
        /// Handles the tmi_File_SaveAll_Click event -> triggers the file-save process
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private async void tmi_File_SaveAll_Click(object sender, EventArgs e)
        {
            // i think having an Item active can cause a lock on it
            lvw_FileList.SelectedItems.Clear();
            // also the problem here is that the exiftoolAsync can still be running and locking the file.

            await Helper.ExifWriteExifToFile();
            dt_fileDataToWriteStage3ReadyToWrite.Rows.Clear();
        }
        /// <summary>
        /// Handles the tmi_File_EditFiles_Click event -> opens the File Edit Form
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void tmi_File_EditFiles_Click(object sender, EventArgs e)
        {
            FrmEditFileData = new frm_editFileData();
            FrmEditFileData.lvw_FileListEditImages.Items.Clear();
            foreach (ListViewItem selectedItem in lvw_FileList.SelectedItems)
            {
                if (System.IO.File.Exists(Path.Combine(tbx_FolderName.Text, selectedItem.Text)))
                {
                    folderName = tbx_FolderName.Text;
                    FrmEditFileData.lvw_FileListEditImages.Items.Add(selectedItem.Text);
                }
            }
            FrmEditFileData.Text = Helper.DataReadSQLiteObjectText(
                languageName: appLanguage,
                objectType: "Form",
                objectName: "frm_editFileData"
                );
            FrmEditFileData.ShowDialog();
        }
        /// <summary>
        /// Handles the tmi_File_CopyGeoData_Click event -> triggers LwvCopyGeoData 
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void tmi_File_CopyGeoData_Click(object sender, EventArgs e)
        {
            Helper.LwvCopyGeoData();
        }
        /// <summary>
        /// Handles the tmi_File_PasteGeoData_Click event -> triggers LwvPasteGeoData
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void tmi_File_PasteGeoData_Click(object sender, EventArgs e)
        {
            Helper.LwvPasteGeoData();
        }
        #endregion
        #region TaskBar Stuff
        /// <summary>
        /// Handles the btn_Refresh_lvwFileList_Click event -> checks if there is anything in the write-Q
        /// ... then cleans up the user-folder and triggers lvwFileList_LoadOrUpdate
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private async void btn_Refresh_lvwFileList_Click(object sender, EventArgs e)
        {
            Helper.s_changeFolderIsOkay = false;
            await Helper.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
            if (Helper.s_changeFolderIsOkay)
            {
                if (System.IO.Directory.Exists(tbx_FolderName.Text))
                {
                    if (!tbx_FolderName.Text.EndsWith("\\"))
                    {
                        tbx_FolderName.Text += "\\";
                    }
                    try
                    {
                        this.lvw_FileList.Items.Clear();
                        Helper.FsoCleanUpUserFolder();
                        folderName = tbx_FolderName.Text;
                        lvwFileList_LoadOrUpdate();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInvalidFolder") + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInvalidFolder"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        /// <summary>
        /// Generally similar to the above.(btn_Refresh_lvwFileList_Click)
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private async void btn_ts_Refresh_lvwFileList_Click(object sender, EventArgs e)
        {
            Helper.s_changeFolderIsOkay = false;
            await Helper.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
            if (Helper.s_changeFolderIsOkay)
            {
                btn_Refresh_lvwFileList_Click(this, new EventArgs());
            }
        }
        /// <summary>
        /// Handles the btn_OneFolderUp_Click event -> Ensures the write-Q is emtpy, that the parent folder exists and 
        /// ... if all's well then moves folder. On error moves to C:\
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private async void btn_OneFolderUp_Click(object sender, EventArgs e)
        {
            Helper.s_changeFolderIsOkay = false;
            await Helper.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
            if (Helper.s_changeFolderIsOkay)
            {
                string? tmpStrParent;
                string? tmpStrRoot;
                // this is a bit derp but alas
                if (tbx_FolderName.Text.EndsWith("\\"))
                {
                    try
                    {
                        tmpStrParent = Helper.FsoGetParent(tbx_FolderName.Text);
                    }
                    catch
                    {
                        tmpStrParent = Helper.GenericCoalesce(
                            System.IO.Directory.GetDirectoryRoot(tbx_FolderName.Text).ToString()
                            , "C:"
                            );
                    }
                    tmpStrRoot = Helper.GenericCoalesce(
                        System.IO.Directory.GetDirectoryRoot(tbx_FolderName.Text).ToString(),
                        "C:"
                        );
                    tbx_FolderName.Text = Helper.GenericCoalesce(tmpStrParent, tmpStrRoot);
                }
                else
                {
                    tbx_FolderName.Text = System.IO.Directory.GetParent(tbx_FolderName.Text).ToString();
                }
                Application.DoEvents();
                btn_ts_Refresh_lvwFileList_Click(this, new EventArgs());
            }
        }
        /// <summary>
        /// Handles the btn_EditFile_Click event -> shows the Edit File Form
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void btn_EditFile_Click(object sender, EventArgs e)
        {
            Helper.ExifShowEditFrm();
        }
        /// <summary>
        /// Handles the btn_RemoveGeoData_Click event -> calls Helper.ExifRemoveLocationData to remove GeoData from all selected files
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void btn_RemoveGeoData_Click(object sender, EventArgs e)
        {
            Helper.ExifRemoveLocationData("frm_mainApp");
        }
        /// <summary>
        /// Handles various keypress events. -> currently for tbx_FolderName when pressing Enter it will move to the folder
        /// ... if value is a folder subject to the usual "move folder" requirements.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private async void tbx_FolderName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Helper.s_changeFolderIsOkay = false;
                await Helper.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
                if (Helper.s_changeFolderIsOkay)
                {
                    btn_ts_Refresh_lvwFileList_Click(this, new EventArgs());
                }
            }
        }
        /// <summary>
        /// Handles the btn_SaveFiles_Click event -> triggers ExifWriteExifToFile
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private async void btn_SaveFiles_Click(object sender, EventArgs e)
        {

            // i think having an Item active can cause a lock on it
            lvw_FileList.SelectedItems.Clear();
            // also the problem here is that the exiftoolAsync can still be running and locking the file.

            await Helper.ExifWriteExifToFile();
            dt_fileDataToWriteStage3ReadyToWrite.Rows.Clear();
        }
        #endregion
        #region lvw_FileList Interaction
        /// <summary>
        /// Responsible for updating the main listview. For each file depending on the "compatible" or "incompatible" naming
        /// ... it assigns the outstanding files according to compatibility and then runs the respective exiftool commands
        /// </summary>
        private async void lvwFileList_LoadOrUpdate()
        {
            List<string> listOfAysncCompatibleItems = new List<string>();
            List<string> listOfAysncIncompatibleItems = new List<string>();
            List<string> listOfNonUTF8Items = new List<string>();

            // this shoudn't really happen but just in case
            if (folderName is null)
            {
                if (Directory.Exists(tbx_FolderName.Text))
                {
                    // nothing
                }
                else
                {
                    tbx_FolderName.Text = @"C:\";
                }
                folderName = tbx_FolderName.Text;
            }

            // list folders and stick them at the beginning of the listview
            var dirs = System.IO.Directory
                .GetDirectories(folderName)
                .ToList();

            string[] allowedExtensions = new string[allExtensions.Length];
            Array.Copy(allowedExtensions, allExtensions, 0);
            for (int i = 0; i < allowedExtensions.Length; i++)
            {
                allowedExtensions[i] = allExtensions[i].Split('\t').First();
            }

            // list files that have whitelisted extensions
            var files = System.IO.Directory
                .GetFiles(folderName)
                .Where(file => allowedExtensions.Any(file.ToLower().EndsWith))
                .ToList();

            files = files.OrderBy(o => o).ToList();
            foreach (string currentDir in dirs)
            {
                lvw_FileList_addListItem(Path.GetFileName(currentDir));
            }

            foreach (string currentFile in files)
            {
                string fileNameToTest = Path.Combine(currentFile);
                lvw_FileList_addListItem(Path.GetFileName(currentFile));

                // the add-in used here can't process nonstandard characters in filenames w/o an args file, which doesn't return what we're after.
                // so for 'standard' stuff we'll run async and for everything else we'll do it slower but more compatible

                if ((Regex.IsMatch(fileNameToTest, @"^[a-zA-Z0-9.:\\_ ]*$")))
                {
                    listOfAysncCompatibleItems.Add(Path.GetFileName(currentFile));
                }
                else
                {
                    listOfAysncIncompatibleItems.Add(Path.GetFileName(currentFile));
                }
            }

            if (listOfAysncCompatibleItems.Count > 0)
            {
                await Helper.ExifGetExifFromFilesCompatibleFileNames(listOfAysncCompatibleItems);
            }
            if (listOfAysncIncompatibleItems.Count > 0)
            {
                string dontShowIncompatibleFileWarningAgainInSQL = Helper.DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "generic",
                    settingId: "dontShowIncompatibleFileWarningAgain"
                    );
                if (dontShowIncompatibleFileWarningAgainInSQL != "true")
                {
                    DialogResult dontShowIncompatibleFileWarningAgain = MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_QuestionDontShowIncompatibleFileWarningAgain"),
                                "Nonstandard paths", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                    if (dontShowIncompatibleFileWarningAgain == DialogResult.Cancel)
                    {
                        Helper.DataWriteSQLiteSettings(
                            tableName: "settings",
                            settingTabPage: "generic",
                            settingId: "dontShowIncompatibleFileWarningAgain",
                            settingValue: "true"
                            );
                    }
                }

                await Helper.ExifGetExifFromFilesIncompatibleFileNames(listOfAysncIncompatibleItems);
            }
            int filesWithGeoData = 0;
            foreach (ListViewItem lvi in lvw_FileList.Items)
            {
                if (lvi.SubItems.Count > 1)
                {
                    if (lvi.SubItems[1].Text != "-")
                    {
                        filesWithGeoData++;
                    }
                }
            }
            frm_MainApp.HandlerUpdateLabelText(lbl_ParseProgress, "Ready. Files: Total: " + files.Count.ToString() + " Geodata: " + filesWithGeoData.ToString());

        }
        /// <summary>
        /// Handles the lvw_FileList_Click event -> if the file that has been clicked on has geodata, it shows its location on the map
        /// ... also generates a preview for the file if there is one.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private async void lvw_FileList_Click(object sender, EventArgs e)
        {
            await Helper.LvwItemClickNavigate();
        }
        /// <summary>
        /// Handles the lvw_FileList_MouseDoubleClick event -> if user clicked on a folder then enter, if a file then edit
        /// ... else warn and don't do anything.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private async void lvw_FileList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = lvw_FileList.HitTest(e.X, e.Y);
            ListViewItem item = info.Item;

            if (item != null)
            {
                // if this is a folder, enter
                if (Directory.Exists(Path.Combine(tbx_FolderName.Text, item.Text)))
                {
                    // check for outstanding files first and save if user wants
                    Helper.s_changeFolderIsOkay = false;
                    await Helper.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
                    if (Helper.s_changeFolderIsOkay)
                    {
                        tbx_FolderName.Text = Path.Combine(tbx_FolderName.Text, item.Text);
                        btn_ts_Refresh_lvwFileList_Click(this, new EventArgs());
                    }
                }
                // if this is a file
                else if (File.Exists(Path.Combine(tbx_FolderName.Text, item.Text)))
                {
                    FrmEditFileData = new frm_editFileData();
                    folderName = tbx_FolderName.Text;
                    FrmEditFileData.lvw_FileListEditImages.Items.Add(item.Text);
                    FrmEditFileData.Text = Helper.DataReadSQLiteObjectText(
                        languageName: appLanguage,
                        objectType: "Form",
                        objectName: "frm_editFileData"
                        );
                    FrmEditFileData.ShowDialog();
                }
            }
            else
            {
                this.lvw_FileList.SelectedItems.Clear();
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_WarningNoItemSelected"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        /// <summary>
        /// Handles the various keypress combinations. See inline comments for details.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">The key pressed</param>
        private async void lvw_FileList_KeyDown(object sender, KeyEventArgs e)
        {
            // control A -> select all
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
            {
                foreach (ListViewItem item in lvw_FileList.Items)
                {
                    item.Selected = true;
                }
            }

            // Shift Control C -> copy details
            else if (e.Control && e.Shift && e.KeyCode == Keys.C)
            {
                Helper.LwvCopyGeoData();
            }

            // Shift Control V -> paste details
            else if (e.Control && e.Shift && e.KeyCode == Keys.V)
            {
                Helper.LwvPasteGeoData();
            }

            // Control Enter -> Edit File
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Enter)
            {
                Helper.ExifShowEditFrm();
            }

            // Backspace -> Up one folder
            else if (e.KeyCode == Keys.Back)
            {
                btn_OneFolderUp_Click(sender, e);
            }

            // Enter  -> enter if folder
            else if (e.KeyCode == Keys.Enter)
            {
                string folderToEnter = lvw_FileList.SelectedItems[0].Text;
                // enter if folder
                if (Directory.Exists(Path.Combine(folderName, folderToEnter)))
                {
                    folderToEnter = (Path.Combine(folderName, folderToEnter));
                    Helper.s_changeFolderIsOkay = false;
                    await Helper.FsoCheckOutstandingFiledataOkayToChangeFolderAsync();
                    if (Helper.s_changeFolderIsOkay)
                    {
                        if (!folderToEnter.EndsWith("\\"))
                        {
                            tbx_FolderName.Text = folderToEnter + "\\";
                        }
                        else
                        {
                            tbx_FolderName.Text = folderToEnter;
                        }
                        try
                        {
                            this.lvw_FileList.Items.Clear();
                            Helper.FsoCleanUpUserFolder();
                            folderName = tbx_FolderName.Text;
                            lvwFileList_LoadOrUpdate();
                        }
                        catch (Exception)
                        {
                            MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_mainApp_ErrorInvalidFolder"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            // F5 -> Refresh folder
            else if (e.KeyCode == Keys.F5)
            {
                btn_Refresh_lvwFileList_Click(sender, e);
                this.lvw_FileList.Items.Clear();
                folderName = tbx_FolderName.Text;
                lvwFileList_LoadOrUpdate();
                e.Handled = true;

            }

            // Control S -> Save files
            else if (e.Control && e.KeyCode == Keys.S)
            {
                // i think having an Item active can cause a lock on it
                lvw_FileList.SelectedItems.Clear();

                await Helper.ExifWriteExifToFile();
                dt_fileDataToWriteStage3ReadyToWrite.Rows.Clear();
            }
        }
        /// <summary>
        /// Adds a new listitem to lvw_FileList listview
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
                                                             NativeMethods.SHGFI_SYSICONINDEX
                                                              | NativeMethods.SHGFI_SMALLICON);
            Debug.Assert(hSysImgList != IntPtr.Zero);  // cross our fingers and hope to succeed!

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
                                                        NativeMethods.SHGFI_DISPLAYNAME
                                                        | NativeMethods.SHGFI_SYSICONINDEX
                                                        | NativeMethods.SHGFI_SMALLICON);
            Debug.Assert(himl == hSysImgList); // should be the same imagelist as the one we set
            #endregion

            if (File.Exists(Path.Combine(folderName, fileName)))
            {
                foreach (ColumnHeader columnHeader in lvw_FileList.Columns)
                {
                    if (columnHeader.Name != "clh_FileName")
                    {
                        subItemList.Add("-");
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
            string fileExtension = Path.GetExtension(Path.Combine(folderName, fileName));
            if (fileExtension != null && fileExtension != "")
            {
                if (shfi.szDisplayName.Contains(fileExtension))
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
                lvi.Text = shfi.szDisplayName;
            }

            lvi.ImageIndex = shfi.iIcon;
            if (File.Exists(Path.Combine(folderName, fileName)))
            {
                lvi.ForeColor = Color.Gray;
            }
            lvw_FileList.Items.Add(lvi).SubItems.AddRange(subItemList.ToArray());
        }
        /// <summary>
        /// Handles the lvw_FileList_SelectedIndexChanged event -> Triggers lvw_FileList_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvw_FileList_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvw_FileList_Click(sender, e);
        }
        #endregion
        #endregion
        #region handlers
        /// <summary>
        /// Deals with invoking the listview (from outside the thread) and updating the colour of a particular row (Item) to the assigned colour.
        /// </summary>
        /// <param name="lvw">The listView Control that needs updating. Most likely the one in the main Form</param>
        /// <param name="item">The particular listViewItem that needs updating</param>
        /// <param name="color">Parameter to assign a particular colour (prob red or black) to the whole row</param>
        internal static void HandlerUpdateItemColour(ListView lvw, string item, Color color)
        {
            // If the current thread is not the UI thread, InvokeRequired will be true
            if (lvw.InvokeRequired)
            {
                lvw.Invoke((Action)(() => HandlerUpdateItemColour(lvw, item, color)));
                return;
            }
            ListViewItem itemToModify = lvw.FindItemWithText(item);
            if (itemToModify != null)
            {
                itemToModify.ForeColor = color;
            }
        }
        /// <summary>
        /// Updates the Text of any Label from outside the thread.
        /// </summary>
        /// <param name="label">The Label Control that needs updating</param>
        /// <param name="text">The Text that will be assigned</param>
        internal static void HandlerUpdateLabelText(Label label, string text)
        {
            // If the current thread is not the UI thread, InvokeRequired will be true
            if (label.InvokeRequired)
            {
                // If so, call Invoke, passing it a lambda expression which calls
                // UpdateText with the same label and text, but on the UI thread instead.
                label.Invoke((Action)(() => HandlerUpdateLabelText(label, text)));
                return;
            }
            // If we're running on the UI thread, we'll get here, and can safely update 
            // the label's text.
            label.Text = text;
        }
        #endregion
    }
    public static class ControlExtensions
    {
        /// <summary>
        /// Makes sure the Control in question gets doubleBufferPropertyInfo enabled/disabled. 
        /// ...Realistically we're using this to assign doubleBufferPropertyInfo = enabled to the main listView.
        /// ...This helps stop the flickering on updating the various data points and/or rows (Items).
        /// </summary>
        /// <param name="control">The Control that needs the value assigned</param>
        /// <param name="enable">Bool true or false (aka on or off)</param>
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo.SetValue(control, enable, null);
        }
    }
}