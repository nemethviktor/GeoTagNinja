using GeoTagNinja.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;
using HelperControlAndMessageBoxCustomMessageBoxManager =
    GeoTagNinja.Helpers.HelperControlAndMessageBoxCustomMessageBoxManager;

namespace GeoTagNinja;

public partial class FrmMainApp
{
    /// <summary>
    ///     Calls the InitializeComponent
    /// </summary>
    private Task AppStartupInitializeComponentFrmMainApp()
    {
        // InitializeComponent();
        Log.Info(message: "Starting");

        try
        {
            InitializeComponent();


            Text = $"{((AssemblyTitleAttribute)Assembly.GetExecutingAssembly()
                                                       .GetCustomAttributes(attributeType: typeof(AssemblyTitleAttribute),
                                                            inherit: false)[0]).Title} [b{Assembly.GetExecutingAssembly()
                                                           .GetName()
                                                           .Version.Build.ToString(provider: CultureInfo.InvariantCulture)}]";
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorInitializeComponent", captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK, extraMessage: ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Enables double-buffering so that the listview doesn't flicker
    /// </summary>
    private Task AppStartupEnableDoubleBuffering()
    {
        Log.Info(message: "Starting");

        try
        {
            lvw_FileList.DoubleBuffered(enable: true);
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorDoubleBuffer", captionType: MessageBoxCaption.Error,
                buttons: MessageBoxButtons.OK, extraMessage: ex.Message);
        }

        return Task.CompletedTask;
    }


    /// <summary>
    ///     Assigns labels to various objects in the application during startup. This includes buttons, labels, checkboxes, and
    ///     other UI elements.
    ///     It also sets up tooltips for specific controls. The labels and tooltips are fetched from a data source using the
    ///     HelperControlAndMessageBoxHandling.ReturnControlText method.
    /// </summary>
    private void AppStartupAssignLabelsToObjects()
    {
        Log.Info(message: "Starting");

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
                cItem.GetType() == typeof(ToolStripButton) ||
                (cItem.GetType() == typeof(ListView) && cItem.Name != "lvw_FileView")
            // cItem.GetType() == typeof(ToolTip) // tooltips are not controls.
            )
            {
                if (cItem.Name == "lbl_ParseProgress")
                {
                    objectName = cItem.Name;
                    objectText = ReturnControlText(controlName: "lbl_ParseProgress_Normal",
                            fakeControlType: FakeControlTypes.Label)
                        ;
                    cItem.Text = objectText;
                    Log.Trace(message: $"{objectName}: {objectText}");
                }
                else if (cItem is ToolStrip ts)
                {
                    // https://www.codeproject.com/Messages/3329190/How-to-convert-a-Control-into-a-ToolStripButton.aspx
                    foreach (ToolStripItem tsi in ts.Items)
                    {
                        if (tsi is ToolStripButton tsb)
                        {
                            objectName = tsb.Name;
                            objectText = ReturnControlText(controlName: objectName,
                                fakeControlType: FakeControlTypes.ToolStripButton);

                            tsb.ToolTipText = objectText;
                            Log.Trace(message: $"{objectName}: {objectText}");
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
                        objectText = ReturnControlText(controlName: objectName,
                            fakeControlType: FakeControlTypes.ColumnHeader);
                        columnHeader.Text = objectText;
                        columnHeader.Width = 120; // arbitrary
                        Log.Trace(message: $"{objectName}: {objectText}");
                    }
                }
                else
                {
                    objectName = cItem.Name;
                    ReturnControlText(cItem: cItem, senderForm: this);

                    Log.Trace(message: $"{objectName}: {cItem.Text}");
                }
            }
        }

        // Text for ImagePreview
        pbx_imagePreview.EmptyText = ReturnControlText(controlName: "pbx_imagePreviewEmptyText",
            fakeControlType: FakeControlTypes.PictureBox);

        // The MenuStrip logic above is not working
        List<ToolStripItem> allMenuItems = [];
        foreach (ToolStripItem toolItem in mns_MenuStrip.Items)
        {
            allMenuItems.Add(item: toolItem);
            Log.Trace(message: $"Menu: {toolItem.Name}");
            //add sub items - not logging this.
            allMenuItems.AddRange(collection: helperNonstatic.GetMenuItems(item: toolItem));
        }

        foreach (ToolStripItem toolItem in cms_FileListView.Items)
        {
            allMenuItems.Add(item: toolItem);
            //add sub items
            allMenuItems.AddRange(collection: helperNonstatic.GetMenuItems(item: toolItem));
        }

        foreach (ToolStripMenuItem cItem in allMenuItems.OfType<ToolStripMenuItem>())
        {
            objectName = cItem.Name;
            objectText = ReturnControlText(controlName: objectName,
                fakeControlType: FakeControlTypes.ToolStripMenuItem);

            cItem.Text = objectText;
            Log.Trace(message: $"{objectName}: {objectText}");
        }


        Log.Trace(message: "Setting Tooltips");
        List<(ToolTip, Control, string)> ttpLabelsList =
        [
            (ttp_loctToFile, btn_loctToFile, "ttp_loctToFile"),
            (ttp_loctToFileDestination, btn_loctToFileDestination, "ttp_loctToFileDestination"),
            (ttp_NavigateMapGo, btn_NavigateMapGo, "ttp_NavigateMapGo"),
            (ttp_SaveFavourite, btn_SaveFavourite, "ttp_SaveFavourite"),
            (ttp_LoadFavourite, btn_LoadFavourite, "ttp_LoadFavourite"),
            (ttp_ManageFavourites, btn_ManageFavourites, "ttp_ManageFavourites")
        ];
        foreach ((ToolTip, Control, string) valueTuple in ttpLabelsList)
        {
            ToolTip ttp = valueTuple.Item1;
            ttp.SetToolTip(control: valueTuple.Item2,
                caption: ReturnControlText(controlName: valueTuple.Item3,
                    fakeControlType: FakeControlTypes.ToolTip));
        }
    }


    /// <summary>
    ///     Pulls the last lat/lng combo from Settings if available, otherwise points to NASA's HQ
    /// </summary>
    private void AppStartupGetLastLatLngFromSettings()
    {
        Log.Info(message: "Starting");

        try
        {
            nud_lat.Text = HelperDataApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings,
                settingTabPage: "generic",
                settingId: "lastLat"
            );
            if (nud_lat.Text != null)
            {
                nud_lat.Value = Convert.ToDecimal(value: nud_lat.Text, provider: CultureInfo.CurrentCulture);
            }

            nud_lng.Text = HelperDataApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings,
                settingTabPage: "generic",
                settingId: "lastLng"
            );
            if (nud_lng.Text != null)
            {
                nud_lng.Value = Convert.ToDecimal(value: nud_lng.Text, provider: CultureInfo.CurrentCulture);
            }
        }
        catch
        {
            // ignored
        }

        if (nud_lat.Text is "" or "0")
        {
            // NASA HQ
            string defaultLat = "38.883056";
            string defaultLng = "-77.016389";
            nud_lat.Text = defaultLat;
            nud_lng.Text = defaultLng;

            nud_lat.Value = Convert.ToDecimal(value: defaultLat, provider: CultureInfo.InvariantCulture);
            nud_lng.Value = Convert.ToDecimal(value: defaultLng, provider: CultureInfo.InvariantCulture);
        }

        HelperVariables.HsMapMarkers.Clear();
        _ = HelperVariables.HsMapMarkers.Add(item: (nud_lat.Text.Replace(oldChar: ',', newChar: '.'),
                                                nud_lng.Text.Replace(oldChar: ',', newChar: '.')));
        HelperVariables.LastLat = double.Parse(s: nud_lat.Text.Replace(oldChar: ',', newChar: '.'),
            provider: CultureInfo.InvariantCulture);
        HelperVariables.LastLng = double.Parse(s: nud_lng.Text.Replace(oldChar: ',', newChar: '.'),
            provider: CultureInfo.InvariantCulture);
    }


    /// <summary>
    ///     Sets the application theme at startup based on the user's settings.
    /// </summary>
    /// <remarks>
    ///     If the user has chosen to use dark mode, the method sets the theme color to dark and applies a custom renderer to
    ///     the menu strip.
    ///     If the user has not chosen to use dark mode, the method sets the theme color to light and uses the default
    ///     rendering for the controls.
    /// </remarks>
    private Task AppStartupSetAppTheme()
    {
        // the custom logic is ugly af so no need to be pushy about it in light mode.
        if (!HelperVariables.UserSettingUseDarkMode)
        {
            tcr_Main.DrawMode = TabDrawMode.Normal;
            lvw_FileList.OwnerDraw = false;
            lvw_ExifData.OwnerDraw = false;
        }
        else
        {
            mns_MenuStrip.Renderer = new DarkMenuStripRenderer();
        }

        // adds colour/theme

        HelperControlThemeManager.SetThemeColour(
            themeColour: HelperVariables.UserSettingUseDarkMode
                ? ThemeColour.Dark
                : ThemeColour.Light, parentControl: this);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Reads the data in SQLite for panel widths/heights/sizes and applies them if available.
    /// </summary>
    private void AppStartupApplyVisualStyleDefaults()
    {
        Log.Info(message: "Starting");
        // there should be a better way of doing this. 
        // reflections could do it and i asked GPT on the how-part but it only gave options for storing and retrieving _all_ the controls and _all_ their details, which isn't something i'd like.

        Dictionary<string, int> settingsApplicationDesignValuesDict = new()
        {
            { "splitContainerMainSplitterDistance", 0 },
            { "splitContainerLeftTopSplitterDistance", 0 }
        };

        // need to make it into a list else the foreach complains that the collection has been modfied.
        List<string> settingsApplicationDesignValuesKeysList = settingsApplicationDesignValuesDict.Keys.ToList();

        foreach (string settingsApplicationDesignValue
                 in settingsApplicationDesignValuesKeysList)
        {
            string dataInSQL =
                HelperDataApplicationSettings.DataReadSQLiteSettings(
                    dataTable: HelperVariables.DtHelperDataApplicationSettings,
                    settingTabPage: "generic",
                    settingId: settingsApplicationDesignValue, returnBlankIfNull: true);

            Log.Debug(
                message:
                $"Reading settingsApplicationDesignValue {settingsApplicationDesignValue}, dataInSQL {dataInSQL}.");


            bool parsedDataInSQLSuccessfully = int.TryParse(s: dataInSQL,
                style: NumberStyles.Any,
                provider: CultureInfo.InvariantCulture,
                result: out int parsedSQLValueInt);

            if (!string.IsNullOrWhiteSpace(value: dataInSQL) && parsedDataInSQLSuccessfully)
            {
                settingsApplicationDesignValuesDict[key: settingsApplicationDesignValue] = parsedSQLValueInt;
            }
        }

        foreach (KeyValuePair<string, int> settingsApplicationDesignValue in settingsApplicationDesignValuesDict)
        {
            checkAssignSingleValues(dictValueKey: settingsApplicationDesignValue.Key);
        }

        void checkAssignSingleValues(string dictValueKey)
        {
            int valToAssign = settingsApplicationDesignValuesDict[key: dictValueKey];
            Log.Debug(
                message:
                $"Assinging value {valToAssign} to {dictValueKey}.");
            if (valToAssign > 0)
            {
                switch (dictValueKey)
                {
                    case "splitContainerMainSplitterDistance":
                        splitContainerMain.SplitterDistance = valToAssign;
                        break;
                    case "splitContainerLeftTopSplitterDistance":
                        splitContainerLeftTop.SplitterDistance = valToAssign;
                        break;
                }
            }
        }

        string TrimEnd(string source, string value)
        {
            return !source.EndsWith(value: value)
                ? source
                : source.Remove(startIndex: source.LastIndexOf(value: value));
        }

        Log.Debug(message: "Done");
    }

    private void splitContainerControl_Paint(object sender, PaintEventArgs e)
    {
        // https://stackoverflow.com/a/16006968
        splitContainerMain.Paint -= splitContainerControl_Paint;
        // Handle restoration here
        AppStartupApplyVisualStyleDefaults();
    }
}