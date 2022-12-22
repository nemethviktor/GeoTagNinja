using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using static System.Environment;

namespace GeoTagNinja;

public partial class FrmMainApp
{
    /// <summary>
    ///     Creates the database sqlite file
    /// </summary>
    private static void AppStartupCreateDataBaseFile()
    {
        Logger.Info(message: "Starting");
        // load all settings
        try
        {
            Logger.Debug(message: "applicationDataGeoTagNinjaFolder");
            string applicationDataGeoTagNinjaFolder = Path.Combine(path1: GetFolderPath(folder: SpecialFolder.ApplicationData), path2: "GeoTagNinja");
            Directory.CreateDirectory(path: applicationDataGeoTagNinjaFolder);

            HelperStatic.DataCreateSQLiteDB();
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(
                text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantCreateSQLiteDB") +
                      ex.Message,
                caption: "Error",
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    /// <summary>
    ///     Writes defaults to sqlite if they don't exist
    /// </summary>
    private static void AppStartupWriteDefaultSettings()
    {
        Logger.Debug(message: "Starting");

        // write settings for combobox defaults etc
        try
        {
            HelperStatic.DataWriteSQLiteSettingsDefaultSettings();
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(
                text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantWriteSQLiteDB") +
                      ex.Message,
                caption: "Error",
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    /// <summary>
    ///     Reads object names from SQLite
    /// </summary>
    private static void AppStartupReadObjectNamesAndLanguage()
    {
        Logger.Debug(message: "Starting");

        try
        {
            _AppLanguage = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "cbx_Language"
            );

            Logger.Trace(message: "AppLanguage is" + _AppLanguage);

            Logger.Debug(message: "Reading dtObjectNames");
            DtObjectNames = HelperStatic.DataReadSQLiteObjectMapping(
                tableName: "objectNames",
                orderBy: "sqlOrder"
            );
            DtObjectTagNamesIn = HelperStatic.DataReadSQLiteObjectMapping(tableName: "objectTagNames_In");
            Logger.Trace(message: "objectTagNames_In OK");
            DtObjectTagNamesOut = HelperStatic.DataReadSQLiteObjectMapping(tableName: "objectTagNames_Out");
            Logger.Trace(message: "objectTagNames_Out OK");
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantLoadSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            Application.Exit();
        }

        // read language and objectnames
        HelperStatic.DataReadObjectTextFromFiles();
    }

    /// <summary>
    ///     Applies default settings
    /// </summary>
    private static void AppStartupApplyDefaults()
    {
        // get some defaults
        Logger.Debug(message: "Starting");

        try
        {
            Logger.Debug(message: "ARCGis Key");
            HelperStatic.SArcGisApiKey = HelperStatic.DataSelectTbxARCGIS_APIKey_FromSQLite();

            Logger.Debug(message: "ARCGis GeoNamesUserName");
            HelperStatic.SGeoNamesUserName = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_GeoNames_UserName"
            );
            Logger.Debug(message: "ARCGis GeoNamesPassword");
            HelperStatic.SGeoNamesPwd = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_GeoNames_Pwd"
            );
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantReadDefaultSQLiteDB") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    /// <summary>
    ///     Makes sure there is webview2 installed and working
    /// </summary>
    private static void AppStartupCheckWebView2()
    {
        // Check webView2 availability
        Logger.Debug(message: "Starting");

        try
        {
            string webView2Version = "";
            webView2Version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            Logger.Trace(message: "Check webView2 version is: " + webView2Version);
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorCantLoadWebView2") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    /// <summary>
    ///     Calls the InitializeComponent
    /// </summary>
    private void AppStartupInitializeComponentFrmMainApp()
    {
        // InitializeComponent();
        Logger.Debug(message: "Starting");

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeComponent") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    /// <summary>
    ///     Enables double-buffering so that the listview doesn't flicker
    /// </summary>
    private void AppStartupEnableDoubleBuffering()
    {
        Logger.Debug(message: "Starting");

        try
        {
            lvw_FileList.DoubleBuffered(enable: true);
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorDoubleBuffer") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    /// <summary>
    ///     Sets the startup folder. Defaults to "MyPictures" if null.
    /// </summary>
    private void AppSetupInitialiseStartupFolder()
    {
        Logger.Debug(message: "Starting");

        string startupFolder = "";
        try
        {
            startupFolder = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_Startup_Folder"
            );
            Logger.Trace(message: "Startup Folder is: " + startupFolder);
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorSettingStartupFolder") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }

        if (startupFolder == null)
        {
            startupFolder = GetFolderPath(folder: SpecialFolder.MyPictures);
            Logger.Trace(message: "Startup Folder is null, defaulting to SpecialFolder.MyPictures: " + startupFolder);
        }

        if (startupFolder.EndsWith(value: "\\"))
        {
            tbx_FolderName.Text = startupFolder;
        }
        else
        {
            tbx_FolderName.Text = startupFolder + "\\";
        }
    }

    /// <summary>
    ///     Assigns the various labels to objects (such as buttons, labels etc.)
    /// </summary>
    private void AppStartupAssignLabelsToObjects()
    {
        Logger.Debug(message: "Starting");

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        string objectName;
        string objectText;
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
                    objectName = cItem.Name;
                    objectText = HelperStatic.DataReadDTObjectText(
                        objectType: cItem.GetType()
                                        .ToString()
                                        .Split('.')
                                        .Last() +
                                    "_Normal",
                        objectName: objectName
                    );
                    cItem.Text = objectText;
                    Logger.Trace(message: "" + objectName + ": " + objectText);
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
                            objectName = tsb.Name;
                            objectText = HelperStatic.DataReadDTObjectText(
                                objectType: tsb.GetType()
                                    .ToString()
                                    .Split('.')
                                    .Last(),
                                objectName: tsb.Name
                            );
                            tsb.ToolTipText = objectText;
                            Logger.Trace(message: "" + objectName + ": " + objectText);
                        }
                    }
                }
                else
                {
                    objectName = cItem.Name;
                    objectText = HelperStatic.DataReadDTObjectText(
                        objectType: cItem.GetType()
                            .ToString()
                            .Split('.')
                            .Last(),
                        objectName: cItem.Name
                    );
                    cItem.Text = objectText;
                    Logger.Trace(message: "" + objectName + ": " + objectText);
                }
            }
        }

        // Text for ImagePreview
        pbx_imagePreview.EmptyText = HelperStatic.DataReadDTObjectText(
            objectType: "PictureBox",
            objectName: "pbx_imagePreviewEmptyText"
        );

        // don't think the menustrip above is working
        List<ToolStripItem> allMenuItems = new();
        foreach (ToolStripItem toolItem in mns_MenuStrip.Items)
        {
            allMenuItems.Add(item: toolItem);
            Logger.Trace(message: "Menu: " + toolItem.Name);
            //add sub items - not logging this.
            allMenuItems.AddRange(collection: helperNonstatic.GetMenuItems(item: toolItem));
        }

        foreach (ToolStripItem toolItem in cms_FileListView.Items)
        {
            allMenuItems.Add(item: toolItem);
            //add sub items
            allMenuItems.AddRange(collection: helperNonstatic.GetMenuItems(item: toolItem));
        }

        foreach (ToolStripItem cItem in allMenuItems)
        {
            if (cItem is ToolStripMenuItem)
            {
                objectName = cItem.Name;
                objectText = HelperStatic.DataReadDTObjectText(
                    objectType: cItem.GetType()
                        .ToString()
                        .Split('.')
                        .Last(),
                    objectName: cItem.Name
                );
                cItem.Text = objectText;
                Logger.Trace(message: objectName + ": " + objectText);
            }
        }

        pbx_imagePreview.EmptyText = HelperStatic.DataReadDTObjectText(
            objectType: "PictureBox",
            objectName: "pbx_imagePreviewEmptyText"
        );

        Logger.Trace(message: "Setting Tooltips");
        ttp_loctToFile.SetToolTip(control: btn_loctToFile,
                                  caption: HelperStatic.DataReadDTObjectText(
                                      objectType: "ToolTip",
                                      objectName: "ttp_loctToFile"
                                  )
        );

        ttp_NavigateMapGo.SetToolTip(control: btn_NavigateMapGo,
                                     caption: HelperStatic.DataReadDTObjectText(
                                         objectType: "ToolTip",
                                         objectName: "ttp_NavigateMapGo"
                                     )
        );
        ttp_SaveFavourite.SetToolTip(control: btn_SaveLocation,
                                     caption: HelperStatic.DataReadDTObjectText(
                                         objectType: "ToolTip",
                                         objectName: "ttp_SaveFavourite"
                                     )
        );
        ttp_LoadFavourite.SetToolTip(control: btn_LoadFavourite,
                                     caption: HelperStatic.DataReadDTObjectText(
                                         objectType: "ToolTip",
                                         objectName: "ttp_LoadFavourite"
                                     )
        );
    }

    /// <summary>
    ///     Pulls the last lat/lng combo from Settings if available, otherwise points to NASA's HQ
    /// </summary>
    private void AppStartupPullLastLatLngFromSettings()
    {
        Logger.Debug(message: "Starting");

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
        HelperStatic.LastLat = double.Parse(s: tbx_lat.Text.Replace(oldChar: ',', newChar: '.'), provider: CultureInfo.InvariantCulture);
        HelperStatic.LastLng = double.Parse(s: tbx_lng.Text.Replace(oldChar: ',', newChar: '.'), provider: CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Pulls the settings for overwriting empty toponomy details if req'd
    /// </summary>
    internal static void AppStartupPullOverWriteBlankToponomy()
    {
        bool.TryParse(value: HelperStatic.DataReadSQLiteSettings(
                          tableName: "settings",
                          settingTabPage: "tpg_Application",
                          settingId: "ckb_ReplaceBlankToponyms"), result: out HelperStatic.ToponomyReplace);

        if (HelperStatic.ToponomyReplace)
        {
            string replaceEmpty = HelperStatic.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "tpg_Application",
                settingId: "tbx_ReplaceBlankToponyms");
            ;
            if (!string.IsNullOrEmpty(value: replaceEmpty))
            {
                HelperStatic.ToponomyReplaceWithWhat = replaceEmpty;
            }
        }
    }

    /// <summary>
    ///     Pulls data related to user's Settings re how many choices an API pull should offer and what should be the default
    ///     radius
    /// </summary>
    internal static void AppStartupPullToponomyRadiusAndMaxRows()
    {
        string choiceCountValue = HelperStatic.DataReadSQLiteSettings(
                                      tableName: "settings",
                                      settingTabPage: "tpg_Application",
                                      settingId: "nud_ChoiceOfferCount"
                                  ) ??
                                  "1";

        HelperStatic.ToponomyMaxRows = choiceCountValue;

        string radiusValue = HelperStatic.DataReadSQLiteSettings(
                                 tableName: "settings",
                                 settingTabPage: "tpg_Application",
                                 settingId: "nud_ChoiceRadius"
                             ) ??
                             "10";
        HelperStatic.ToponomyRadiusValue = radiusValue;
    }

    private static DataTable AppStartupLoadFavourites()
    {
        Logger.Info(message: "Starting");
        DataTable dtFavourites = HelperStatic.DataReadSQLiteFavourites();
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        LstFavourites.Clear();
        AutoCompleteStringCollection autoCompleteCustomSource = new();
        frmMainAppInstance.cbx_Favourites.Items.Clear();
        foreach (DataRow drFavorite in dtFavourites.Rows)
        {
            string locationName = drFavorite[columnName: "locationName"]
                .ToString();
            LstFavourites.Add(item: locationName);
            autoCompleteCustomSource.Add(value: locationName);
            if (frmMainAppInstance != null)
            {
                frmMainAppInstance.cbx_Favourites.Items.Add(item: locationName);
            }
        }

        if (frmMainAppInstance != null)
        {
            frmMainAppInstance.cbx_Favourites.AutoCompleteSource = AutoCompleteSource.CustomSource;
            frmMainAppInstance.cbx_Favourites.AutoCompleteCustomSource = autoCompleteCustomSource;
        }

        return dtFavourites;
    }
}