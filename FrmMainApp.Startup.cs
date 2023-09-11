using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using GeoTagNinja.Helpers;

namespace GeoTagNinja;

public partial class FrmMainApp
{
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
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorInitializeComponent") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
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
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorDoubleBuffer") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
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

        GetUOMAbbreviated();

        foreach (Control cItem in c)
        {
            if (
                cItem.GetType() == typeof(MenuStrip) ||
                cItem.GetType() == typeof(ToolStrip) ||
                cItem.GetType() == typeof(Label) ||
                cItem.GetType() == typeof(Button) ||
                cItem.GetType() == typeof(CheckBox) ||
                cItem.GetType() == typeof(TabPage) ||
                cItem.GetType() == typeof(ToolStripButton) ||
                (cItem.GetType() == typeof(ListView) && cItem.Name != "lvw_FileView")
                // cItem.GetType() == typeof(ToolTip) // tooltips are not controls.
            )
            {
                if (cItem.Name == "lbl_ParseProgress")
                {
                    objectName = cItem.Name;
                    objectText = HelperDataLanguageTZ.DataReadDTObjectText(
                        objectType: cItem.GetType()
                                         .Name +
                                    "_Normal",
                        objectName: objectName
                    );
                    cItem.Text = objectText;
                    Logger.Trace(message: "" + objectName + ": " + objectText);
                }
                else if (cItem is ToolStrip ts)
                {
                    // https://www.codeproject.com/Messages/3329190/How-to-convert-a-Control-into-a-ToolStripButton.aspx
                    foreach (ToolStripItem tsi in ts.Items)
                    {
                        ToolStripButton tsb = tsi as ToolStripButton;
                        if (tsb != null)
                        {
                            objectName = tsb.Name;
                            objectText = HelperDataLanguageTZ.DataReadDTObjectText(
                                objectType: tsb.GetType()
                                               .Name,
                                objectName: tsb.Name
                            );
                            tsb.ToolTipText = objectText;
                            Logger.Trace(message: "" + objectName + ": " + objectText);
                        }
                    }
                }
                else if (cItem is ListView lvw)
                {
                    foreach (ColumnHeader columnHeader in lvw.Columns)
                    {
                        // this is entirely stupid but .Name in this case returns nothing of use even though it's hard-coded in the Designer.
                        // alas .Text works -- fml.
                        objectName = columnHeader.Text;
                        objectText = HelperDataLanguageTZ.DataReadDTObjectText(
                            objectType: columnHeader.GetType()
                                                    .Name,
                            objectName: objectName
                        );
                        columnHeader.Text = objectText;
                        columnHeader.Width = 120; // arbitrary
                        Logger.Trace(message: "" + objectName + ": " + objectText);
                    }
                }
                else
                {
                    objectName = cItem.Name;
                    objectText = HelperDataLanguageTZ.DataReadDTObjectText(
                        objectType: cItem.GetType()
                                         .Name,
                        objectName: cItem.Name
                    );
                    cItem.Text = objectText;
                    Logger.Trace(message: "" + objectName + ": " + objectText);
                }
            }
        }

        // Text for ImagePreview
        pbx_imagePreview.EmptyText = HelperDataLanguageTZ.DataReadDTObjectText(
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
                objectText = HelperDataLanguageTZ.DataReadDTObjectText(
                    objectType: cItem.GetType()
                        .Name,
                    objectName: cItem.Name
                );
                cItem.Text = objectText;
                Logger.Trace(message: objectName + ": " + objectText);
            }
        }

        pbx_imagePreview.EmptyText = HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: "PictureBox",
            objectName: "pbx_imagePreviewEmptyText"
        );

        Logger.Trace(message: "Setting Tooltips");
        ttp_loctToFile.SetToolTip(control: btn_loctToFile,
                                  caption: HelperDataLanguageTZ.DataReadDTObjectText(
                                      objectType: "ToolTip",
                                      objectName: "ttp_loctToFile"
                                  )
        );

        ttp_loctToFileDestination.SetToolTip(control: btn_loctToFileDestination,
                                             caption: HelperDataLanguageTZ.DataReadDTObjectText(
                                                 objectType: "ToolTip",
                                                 objectName: "ttp_loctToFileDestination"
                                             )
        );
        ttp_NavigateMapGo.SetToolTip(control: btn_NavigateMapGo,
                                     caption: HelperDataLanguageTZ.DataReadDTObjectText(
                                         objectType: "ToolTip",
                                         objectName: "ttp_NavigateMapGo"
                                     )
        );
        ttp_SaveFavourite.SetToolTip(control: btn_SaveLocation,
                                     caption: HelperDataLanguageTZ.DataReadDTObjectText(
                                         objectType: "ToolTip",
                                         objectName: "ttp_SaveFavourite"
                                     )
        );
        ttp_LoadFavourite.SetToolTip(control: btn_LoadFavourite,
                                     caption: HelperDataLanguageTZ.DataReadDTObjectText(
                                         objectType: "ToolTip",
                                         objectName: "ttp_LoadFavourite"
                                     )
        );
        ttp_ManageFavourites.SetToolTip(control: btn_ManageFavourites,
                                        caption: HelperDataLanguageTZ.DataReadDTObjectText(
                                            objectType: "ToolTip",
                                            objectName: "ttp_ManageFavourites"
                                        )
        );
    }

    internal static string GetUOMAbbreviated()
    {
        return HelperVariables.UOMAbbreviated = HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: "Label", objectName: HelperVariables.UseImperial
                ? "lbl_Feet_Abbr"
                : "lbl_Metres_Abbr"
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
            nud_lat.Text = HelperDataApplicationSettings.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "lastLat"
            );
            if (nud_lat.Text != null)
            {
                nud_lat.Value = Convert.ToDecimal(value: nud_lat.Text, provider: CultureInfo.InvariantCulture);
            }

            nud_lng.Text = HelperDataApplicationSettings.DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "lastLng"
            );
            if (nud_lng.Text != null)
            {
                nud_lng.Value = Convert.ToDecimal(value: nud_lng.Text, provider: CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            // ignored
        }

        if (nud_lat.Text == "" || nud_lat.Text == "0")
        {
            // NASA HQ
            string defaultLat = "38.883056";
            string defaultLng = "-77.016389";
            nud_lat.Text = defaultLat;
            nud_lng.Text = defaultLng;

            nud_lat.Value = Convert.ToDecimal(value: defaultLat, CultureInfo.InvariantCulture);
            nud_lng.Value = Convert.ToDecimal(value: defaultLng, CultureInfo.InvariantCulture);
        }

        HelperVariables.HsMapMarkers.Clear();
        HelperVariables.HsMapMarkers.Add(item: (nud_lat.Text.Replace(oldChar: ',', newChar: '.'), nud_lng.Text.Replace(oldChar: ',', newChar: '.')));
        HelperVariables.LastLat = double.Parse(s: nud_lat.Text.Replace(oldChar: ',', newChar: '.'), provider: CultureInfo.InvariantCulture);
        HelperVariables.LastLng = double.Parse(s: nud_lng.Text.Replace(oldChar: ',', newChar: '.'), provider: CultureInfo.InvariantCulture);
    }
}