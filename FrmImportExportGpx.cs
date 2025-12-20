using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TimeZoneConverter;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;
using static GeoTagNinja.Helpers.HelperGenericAncillaryListsArrays;
using Themer = WinFormsDarkThemerNinja.Themer;


namespace GeoTagNinja;

public partial class FrmImportExportGpx : Form
{
    internal const string DoubleQuote = "\"";
    private static readonly Timer FormTimer = new();
    private static string LocalIanatZname;
    private static string SelectedIanatzName;
    private static string SelectedTzAdjustment;
    private static int _lastShiftSecond;
    private static int _lastShiftMinute;
    private static int _lastShiftHour;
    private static int _lastShiftDay;
    private static string _lastTimeZoneChoice;
    private static string _lastCompareAgainstChoice;
    private readonly FrmMainApp _frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

    /// <summary>
    ///     This form helps import various Track files.
    ///     For the list of currently supported file types & formats check: https://exiftool.org/geotag.html
    /// </summary>
    public FrmImportExportGpx()
    {
        InitializeComponent();
        if (!HelperVariables.UserSettingUseDarkMode)
        {
            tcr_ImportExport.DrawMode = TabDrawMode.Normal;
        }

        ReturnControlText(cItem: this, senderForm: this);

        // load TZ-CBX
        FillTZComboBox(cbxToFill: cbx_ImportUseTimeZone);

        // this has to come here (rather than further up)
        ckb_UseTimeZone.Checked = false;
        ckb_UseDST.Checked = TimeZoneInfo.Local.IsDaylightSavingTime(dateTime: DateTime.Now);
        cbx_ImportUseTimeZone.Enabled = false;
        ckb_UseDST.Enabled = false;

        // set filter for ofd
        string gpxExtensionsFilter = GpxExtensions()
           .Aggregate(seed: "Track Files|",
                func: (current,
                       extension) => $"{current}*.{extension};");

        ofd_importOneFile.Filter = gpxExtensionsFilter;

        // set label texts and combobox items
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            ReturnControlText(cItem: cItem, senderForm: this);

            switch (cItem.Name)
            {
                case "cbx_ImportTimeAgainst":
                    {
                        foreach (object importTimeAgainstItem in Enum.GetValues(enumType: typeof(ImportTimeAgainst)))
                        {
                            _ = cbx_ImportTimeAgainst.Items.Add(item: importTimeAgainstItem.ToString());
                        }

                        if (!string.IsNullOrWhiteSpace(value: _lastCompareAgainstChoice))
                        {
                            cbx_ImportTimeAgainst.Text = _lastCompareAgainstChoice;
                        }
                        else
                        {
                            cbx_ImportTimeAgainst.SelectedIndex = 0;
                        }

                        break;
                    }
                case "cbx_ImportUseTimeZone":
                    if (!string.IsNullOrWhiteSpace(value: _lastTimeZoneChoice))
                    {
                        ckb_UseTimeZone.Checked = true;
                        cbx_ImportUseTimeZone.Enabled = true;
                        cbx_ImportUseTimeZone.Text = _lastTimeZoneChoice;
                    }

                    break;
                case "cbx_ExportTrackOrderBy":
                    {
                        foreach (object exportFileOrderItem in Enum.GetValues(enumType: typeof(ExportFileOrder)))
                        {
                            _ = cbx_ExportTrackOrderBy.Items.Add(item: exportFileOrderItem.ToString());
                        }

                        cbx_ExportTrackOrderBy.SelectedIndex = 0;
                        break;
                    }
                case "cbx_ExportTrackTimeStampType":
                    {
                        foreach (object exportFileFMTTimeBasisItem in Enum.GetValues(
                                     enumType: typeof(ExportFileFMTTimeBasis)))
                        {
                            _ = cbx_ExportTrackTimeStampType.Items.Add(item: exportFileFMTTimeBasisItem.ToString());
                        }

                        cbx_ExportTrackTimeStampType.SelectedIndex = 0;
                        break;
                    }
            }
        }

        GetDefaultUserSettings();

        // trigger timer for datetime-update
        FormTimer.Enabled = true;
        FormTimer.Interval = 1000;
        FormTimer.Start();

        FormTimer.Tick += TimerEventProcessor;
    }

    /// <summary>
    ///     Reads the track source, TZ value and DST, and max inter/extrapolation values from SQLite
    /// </summary>
    private async void GetDefaultUserSettings()
    {
        await HelperGenericAppStartup.AppStartupApplyDefaults(settingTabPage: "tpg_ImportExport_Import",
            actuallyRunningAtStartup: false);
        nud_GeoMaxIntSecs.Value = HelperVariables.UserSettingImportGPXMaxInterpolation;
        nud_GeoMaxExtSecs.Value = HelperVariables.UserSettingImportGPXMaxExtrapolation;

        ckb_UseDST.Checked = HelperVariables.UserSettingImportGPXUseDST;
        ckb_UseTimeZone.Checked = HelperVariables.UserSettingImportGPXUseParticularTimeZone;
        if (ckb_UseTimeZone.Checked)
        {
            cbx_ImportUseTimeZone.Text =
                HelperVariables.UserSettingImportGPXTimeZoneToUse.Replace(oldValue: ") # ", newValue: "\r) # ");
        }

        switch (HelperVariables.UserSettingImportGPXImportSource)
        {
            case "rbt_importOneFile":
                rbt_importOneFile.Checked = true;
                lbl_importOneFile.Text = GetStoredUserSettingValue(settingID: "lbl_importOneFile");
                break;
            case "rbt_importFromCurrentFolder":
                rbt_importFromCurrentFolder.Checked = true;
                break;
            case "rbt_importFromAnotherFolder":
                rbt_importFromAnotherFolder.Checked = true;
                lbl_importFromAnotherFolder.Text = GetStoredUserSettingValue(settingID: "lbl_importFromAnotherFolder");
                break;
            default:
                rbt_importOneFile.Checked = true;
                break;
        }

        pbx_importOneFile.Enabled = rbt_importOneFile.Checked;
        lbl_importOneFile.Enabled = rbt_importOneFile.Checked;

        pbx_importFromAnotherFolder.Enabled = rbt_importFromAnotherFolder.Checked;
        lbl_importFromAnotherFolder.Enabled = rbt_importFromAnotherFolder.Checked;

        ckb_OverlayGPXForSelectedDatesOnly.Enabled = false;

        rbt_importFromCurrentFolder.Enabled = !Program.CollectionModeEnabled;

        static string GetStoredUserSettingValue(string settingID)
        {
            return HelperDataApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings,
                settingTabPage: "tpg_ImportExport_Import",
                settingId: settingID,
                returnBlankIfNull: true
            );
        }
    }

    private void FillTZComboBox(ComboBox cbxToFill)
    {
        foreach (string timezone in GetTimeZones())
        {
            _ = cbxToFill.Items.Add(item: timezone);
        }

        try
        {
            LocalIanatZname = TZConvert.WindowsToIana(windowsTimeZoneId: TimeZoneInfo.Local.Id);
        }
        catch
        {
            LocalIanatZname = "Europe/London";
        }

        // this is a little lame but works. -> try to default to local TZ
        bool TZFound = false;
        for (int i = 0; i < cbxToFill.Items.Count; i++)
        {
            if (cbxToFill.GetItemText(item: cbxToFill.Items[index: i])
                         .Contains(value: LocalIanatZname))
            {
                cbxToFill.SelectedIndex = i;
                TZFound = true;
                break;
            }
        }

        // it's entirely possible that a TZ doesn't get found for whatever reason (largely that I'm a derp and somehow excluded it from the list. In this case we try to find something similar.)
        if (!TZFound)
        {
            bool localTZBaseIsNegative = TimeZoneInfo.Local.BaseUtcOffset.Hours < 0;
            char plusMinusChar;
            plusMinusChar = localTZBaseIsNegative
                ? '-'
                : '+';

            // loop again. duh.
            for (int i = 0; i < cbxToFill.Items.Count; i++)
            {
                if (cbxToFill.GetItemText(item: cbxToFill.Items[index: i])
                             .StartsWith(value: $"({plusMinusChar}{TimeZoneInfo.Local.BaseUtcOffset.ToString()
                                .Substring(startIndex: 0, length: 5)}"))
                {
                    cbxToFill.SelectedIndex = i;
                    TZFound = true;
                    break;
                }
            }

            // if still fail then just pick the first and sod it.
            if (!TZFound)
            {
                cbxToFill.SelectedIndex = 0;
            }
        }
    }

    /// <summary>
    ///     This updates the "now" value of lbl_CameraTimeData with any user adjustments there may be.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void TimerEventProcessor(object sender,
                                     EventArgs e)
    {
        lbl_CameraTimeData.Text = DateTime.Now.AddDays(value: (int)nud_Days.Value)
                                          .AddHours(value: (int)nud_Hours.Value)
                                          .AddMinutes(value: (int)nud_Minutes.Value)
                                          .AddSeconds(value: (int)nud_Seconds.Value)
                                          .ToString(format: "yyyy MMMM dd HH:mm:ss");
    }

    private string updatelbl_TZValue()
    {
        SelectedTzAdjustment = cbx_ImportUseTimeZone.Text.Split('#')[0]
                                                    .TrimStart(' ')
                                                    .TrimEnd(' ')
                                                    .Substring(
                                                         startIndex: ckb_UseDST.Checked == false
                                                             ? 1
                                                             : 8, length: 6);

        return SelectedTzAdjustment;
    }

    /// <summary>
    ///     Gets the min and max 'takenDate' values for the items selected in frmMain's lvw.
    /// </summary>
    /// <returns></returns>
    private List<DateTime> GetMinMaxDateTimesFromFrmMainListView()
    {
        DateTime minTakenDateTime = DateTime.MaxValue; // yes these are backwards on purpose
        DateTime maxTakenDateTime = DateTime.MinValue; // yes these are backwards on purpose
        List<DateTime> retList = [];
        ListView lvw = _frmMainAppInstance.lvw_FileList;
        foreach (ListViewItem lvi in lvw.SelectedItems)
        {
            DirectoryElement directoryElement =
                lvi.Tag as DirectoryElement;
            DateTime? takenDateTime = directoryElement.GetAttributeValue<DateTime>(
                attribute: SourcesAndAttributes.ElementAttribute.TakenDate,
                version: directoryElement.GetMaxAttributeVersion(
                    attribute: SourcesAndAttributes.ElementAttribute.TakenDate),
                notFoundValue: null);
            if (takenDateTime.HasValue)
            {
                // i'm sure there are better ways around this but i'm lazy
                retList.Clear();
                if (takenDateTime < minTakenDateTime)
                {
                    minTakenDateTime = ((DateTime)takenDateTime).Date;
                }

                if (takenDateTime > maxTakenDateTime)
                {
                    maxTakenDateTime = (DateTime)takenDateTime;
                }

                // the idea is that i'll use indexes 0 and 1 specifically to get the values we need.
                retList.Add(item: minTakenDateTime);
                retList.Add(item: maxTakenDateTime);
            }
        }

        return retList;
    }

    #region Events

    private void FrmImportExportGpx_Load(object sender, EventArgs e)
    {

        Themer.ApplyThemeToControl(
            control: this,
            themeStyle: HelperVariables.UserSettingUseDarkMode ?
            Themer.ThemeStyle.Custom :
            Themer.ThemeStyle.Default
            );
    }

    /// <summary>
    ///     Collects the settings for track-parse and sends to the data collector; then closes the Form.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void btn_Generic_OK_Click(object sender,
                                            EventArgs e)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        switch (tcr_ImportExport.SelectedTab.Name)
        {
            case "tpg_ImportExport_Import":
                string trackFileLocationType = "";
                string trackFileLocationVal = "";

                // one source:
                // exiftool -geotag "c:\gps logs\track.log" x.jpg
                if (rbt_importOneFile.Checked)
                {
                    trackFileLocationType = "file";
                    trackFileLocationVal = lbl_importOneFile.Text;
                }
                else
                {
                    trackFileLocationType = "folder";
                    // this wouldn't exist in collectionMode
                    trackFileLocationVal = rbt_importFromCurrentFolder.Checked
                        ? _frmMainAppInstance.tbx_FolderName.Text
                        : lbl_importFromAnotherFolder.Text;
                }

                int timeShiftSeconds = 0;
                // adjust time as needed
                if (nud_Days.Value != 0 ||
                    nud_Hours.Value != 0 ||
                    nud_Minutes.Value != 0 ||
                    nud_Seconds.Value != 0)
                {
                    timeShiftSeconds += (int)nud_Days.Value * 60 * 60 * 24;
                    timeShiftSeconds += (int)nud_Hours.Value * 60 * 60;
                    timeShiftSeconds += (int)nud_Minutes.Value * 60;
                    timeShiftSeconds += (int)nud_Seconds.Value;
                }

                if ((trackFileLocationType == "file" && File.Exists(path: trackFileLocationVal)) ||
                    (trackFileLocationType == "folder" && Directory.Exists(path: trackFileLocationVal)))
                {
                    // indicate that something is going on
                    btn_Generic_OK.Text = ReturnControlText(
                        controlName: "Generic_PleaseWait",
                        fakeControlType: FakeControlTypes.Generic);
                    btn_Generic_OK.AutoSize = true;
                    btn_Generic_OK.Enabled = false;
                    btn_Generic_Cancel.Enabled = false;
                    List<DateTime> overlayDateList = [];

                    TrackOverlaySetting trackOverlaySetting = new();
                    if (!ckb_LoadTrackOntoMap.Checked)
                    {
                        trackOverlaySetting = TrackOverlaySetting.DoNotOverlay;
                    }
                    else if (!ckb_OverlayGPXForSelectedDatesOnly.Checked)
                    {
                        trackOverlaySetting = TrackOverlaySetting.OverlayForAllDates;
                    }
                    else
                    {
                        trackOverlaySetting = TrackOverlaySetting.OverlayForOverlappingDates;
                        overlayDateList = GetMinMaxDateTimesFromFrmMainListView();
                    }

                    _lastCompareAgainstChoice = cbx_ImportTimeAgainst.Text;
                    _lastTimeZoneChoice = ckb_UseTimeZone.Checked ? cbx_ImportUseTimeZone.Text : null;

                    await HelperExifReadTrackFile.ExifGetTrackSyncData(
                        trackFileLocationType: trackFileLocationType,
                        trackFileLocationVal: trackFileLocationVal,
                        compareTZAgainst: cbx_ImportTimeAgainst.Text,
                        TZVal: lbl_TZValue.Text,
                        GeoMaxIntSecs: (int)nud_GeoMaxIntSecs.Value,
                        GeoMaxExtSecs: (int)nud_GeoMaxExtSecs.Value,
                        doNotReverseGeoCode: ckb_DoNotQueryAPI.Checked,
                        getTrackDataOverlay: trackOverlaySetting,
                        overlayDateList: overlayDateList,
                        timeShiftSeconds: timeShiftSeconds);
                    frmMainAppInstance?.SelectAllListViewItems();
                    Hide();
                }
                else
                {
                    Themer.ShowMessageBox(
                        message: HelperControlAndMessageBoxHandling.ReturnControlText(
                            controlName: "mbx_FrmImportExportGpx_FileOrFolderDoesntExist",
                            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                        icon: MessageBoxIcon.Error,
                        buttons: MessageBoxButtons.OK);
                }

                _lastShiftSecond = decimal.ToInt16(value: nud_Seconds.Value);
                _lastShiftMinute = decimal.ToInt16(value: nud_Minutes.Value);
                _lastShiftHour = decimal.ToInt16(value: nud_Hours.Value);
                _lastShiftDay = decimal.ToInt16(value: nud_Days.Value);
                break;
            case "tpg_ImportExport_Export":

                ListView lvw = _frmMainAppInstance.lvw_FileList;
                List<string> exportFileList = (from ListViewItem lvi in lvw.SelectedItems
                                               select lvi.Tag as DirectoryElement
                                               into directoryElement
                                               select directoryElement.FileNameWithPath).ToList();

                GenerateFMTFile(includeAltitude: ckb_ExportTrackIncludeAltitude.Checked,
                    exportFileFMTTimeBasis: cbx_ExportTrackTimeStampType.Text);
                await HelperExifWriteTrackDataToTrackFile.ExifWriteTrackDataToTrackFile(fileList: exportFileList,
                    outFilePath: tbx_SaveTrackTo.Text);
                Hide();
                break;
        }
    }

    /// <summary>
    ///     Closes (hides) the Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_Generic_Cancel_Click(object sender,
                                          EventArgs e)
    {
        Hide();
    }

    private void btn_SaveDefaults_Click(object sender,
                                        EventArgs e)
    {
        List<AppSettingContainer> settingsToWrite = [];
        string tableName = "settings";
        string tabName = tcr_ImportExport.SelectedTab.Name;

        // Define radio button group
        var importSourceRadios = new[]
        {
            new
            {
                Radio = rbt_importOneFile, Id = "rbt_importOneFile",
                Label = lbl_importOneFile
            },
            new
            {
                Radio = rbt_importFromCurrentFolder, Id = "rbt_importFromCurrentFolder",
                Label = (Label)null
            },
            new
            {
                Radio = rbt_importFromAnotherFolder, Id = "rbt_importFromAnotherFolder",
                Label = lbl_importFromAnotherFolder
            }
        };

        // Add all radio button states (true for checked, false for unchecked)
        foreach (var rbt in importSourceRadios)
        {
            settingsToWrite.Add(item: new AppSettingContainer
            {
                TableName = tableName,
                SettingTabPage = tabName,
                SettingId = rbt.Id,
                SettingValue = rbt.Radio.Checked.ToString().ToLower()
            });

            // If it has an associated label and it's selected, store the label text too
            if (rbt.Radio.Checked &&
                rbt.Label != null)
            {
                settingsToWrite.Add(item: new AppSettingContainer
                {
                    TableName = tableName,
                    SettingTabPage = tabName,
                    SettingId = rbt.Label.Name,
                    SettingValue = rbt.Label.Text
                });
            }
        }

        // Numeric values
        settingsToWrite.AddRange(collection: new[]
        {
            new AppSettingContainer
            {
                TableName = tableName,
                SettingTabPage = tabName,
                SettingId = "nud_GeoMaxIntSecs",
                SettingValue = nud_GeoMaxIntSecs.Text
            },
            new AppSettingContainer
            {
                TableName = tableName,
                SettingTabPage = tabName,
                SettingId = "nud_GeoMaxExtSecs",
                SettingValue = nud_GeoMaxExtSecs.Text
            }
        });

        // Strings
        if (ckb_UseTimeZone.Checked)
        {
            settingsToWrite.AddRange(collection: new[]
            {
                new AppSettingContainer
                {
                    TableName = tableName,
                    SettingTabPage = tabName,
                    SettingId = "cbx_ImportUseTimeZone",
                    SettingValue = cbx_ImportUseTimeZone.Text.Replace(oldValue: "\r", newValue: "")
                }
            });
        }

        // Booleans
        settingsToWrite.AddRange(collection: new[]
        {
            new AppSettingContainer
            {
                TableName = tableName,
                SettingTabPage = tabName,
                SettingId = "ckb_UseTimeZone",
                SettingValue = ckb_UseTimeZone.Checked.ToString().ToLower()
            },
            new AppSettingContainer
            {
                TableName = tableName,
                SettingTabPage = tabName,
                SettingId = "ckb_UseDST",
                SettingValue = ckb_UseDST.Checked.ToString().ToLower()
            }
        });

        if (settingsToWrite.Count > 0)
        {
            HelperDataApplicationSettings.DataWriteSQLiteSettings(settingsToWrite: settingsToWrite);
        }

        Themer.ShowMessageBox(message:
            HelperControlAndMessageBoxHandling.ReturnControlText(
                controlName: "mbx_GenericDone",
                fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
            icon: MessageBoxIcon.Information,
            buttons: MessageBoxButtons.OK);
    }

    #region Import

    /// <summary>
    ///     Opens a file browser for track files
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void pbx_importOneFile_Click(object sender,
                                         EventArgs e)
    {
        if (ofd_importOneFile.ShowDialog() == DialogResult.OK)
        {
            lbl_importOneFile.Text = ofd_importOneFile.FileName;
        }
    }

    /// <summary>
    ///     Opens a folder browser for track files
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void pbx_importFromAnotherFolder_Click(object sender,
                                                   EventArgs e)
    {
        if (fbd_importFromAnotherFolder.ShowDialog() == DialogResult.OK)
        {
            lbl_importFromAnotherFolder.Text = fbd_importFromAnotherFolder.SelectedPath;
        }
    }

    private void rbt_importOneFile_CheckedChanged(object sender,
                                                  EventArgs e)
    {
        pbx_importOneFile.Enabled = rbt_importOneFile.Checked;
        lbl_importOneFile.Enabled = rbt_importOneFile.Checked;
        lbl_importFromAnotherFolder.Enabled = !rbt_importOneFile.Checked;
    }

    private void rbt_importFromCurrentFolder_CheckedChanged(object sender,
                                                            EventArgs e)
    {
        pbx_importOneFile.Enabled = false;
        pbx_importFromAnotherFolder.Enabled = false;
        lbl_importOneFile.Enabled = false;
        lbl_importFromAnotherFolder.Enabled = false;
    }

    private void rbt_importFromAnotherFolder_CheckedChanged(object sender,
                                                            EventArgs e)
    {
        pbx_importFromAnotherFolder.Enabled = rbt_importFromAnotherFolder.Checked;
        lbl_importFromAnotherFolder.Enabled = rbt_importFromAnotherFolder.Checked;
        if (rbt_importFromAnotherFolder.Checked)
        {
            pbx_importOneFile.Enabled = false;
            lbl_importOneFile.Enabled = false;
        }
        else
        {
            pbx_importOneFile.Enabled = true;
            lbl_importOneFile.Enabled = true;
        }
    }

    private void ckb_UseTimeZone_CheckedChanged(object sender,
                                                EventArgs e)
    {
        ckb_UseDST.Enabled = ckb_UseTimeZone.Checked;
        cbx_ImportUseTimeZone.Enabled = ckb_UseTimeZone.Checked;
        lbl_TZValue.Text = updatelbl_TZValue();
    }

    private void ckb_UseDST_CheckedChanged(object sender,
                                           EventArgs e)
    {
        lbl_TZValue.Text = updatelbl_TZValue();
    }

    private void cbx_UseTimeZone_SelectedIndexChanged(object sender,
                                                      EventArgs e)
    {
        SelectedIanatzName = cbx_ImportUseTimeZone.Text.Split('#')[1]
                                                  .TrimStart(' ')
                                                  .TrimEnd(' ');
        lbl_TZValue.Text = updatelbl_TZValue();
    }

    private void ckb_LoadTrackOntoMap_CheckedChanged(object sender,
                                                     EventArgs e)
    {
        ckb_OverlayGPXForSelectedDatesOnly.Enabled = ckb_LoadTrackOntoMap.Checked;
    }

    /// <summary>
    ///     Handles the loading of "most recent" time-shift settings
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btn_PullMostRecentTrackSyncShift_Click(object sender,
                                                        EventArgs e)
    {
        if (_lastShiftSecond == 0 &&
            _lastShiftMinute == 0 &&
            _lastShiftHour == 0 &&
            _lastShiftDay == 0)
        {
            Themer.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_FrmImportNoStoredShiftValues",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                icon: MessageBoxIcon.Error,
                buttons: MessageBoxButtons.OK);

        }

        if (_lastShiftSecond != 0)
        {
            nud_Seconds.Value = _lastShiftSecond;
            nud_Seconds.Text = _lastShiftSecond.ToString(provider: CultureInfo.InvariantCulture);
        }

        if (_lastShiftMinute != 0)
        {
            nud_Minutes.Value = _lastShiftMinute;
            nud_Minutes.Text = _lastShiftMinute.ToString(provider: CultureInfo.InvariantCulture);
        }

        if (_lastShiftHour != 0)
        {
            nud_Hours.Value = _lastShiftHour;
            nud_Hours.Text = _lastShiftHour.ToString(provider: CultureInfo.InvariantCulture);
        }

        if (_lastShiftDay != 0)
        {
            nud_Days.Value = _lastShiftDay;
            nud_Days.Text = _lastShiftDay.ToString(provider: CultureInfo.InvariantCulture);
        }
    }

    #endregion

    #region Export

    private void pbx_Browse_SaveTo_Folder_Click(object sender,
                                                EventArgs e)
    {
        if (ofd_SaveTrackTo.ShowDialog() == DialogResult.OK)
        {
            tbx_SaveTrackTo.Text = ofd_SaveTrackTo.FileName;
        }
    }

    #endregion

    #region Unspecified

    private void tcr_ImportExport_SelectedIndexChanged(object sender,
                                                       EventArgs e)
    {
        btn_SaveDefaults.Enabled = tcr_ImportExport.SelectedTab.Name == "tpg_ImportExport_Import";
    }

    private void pbx_SaveDefaults_MouseHover(object sender,
                                             EventArgs e)
    {
        ToolTip ttp = new();
        ttp.SetToolTip(control: pbx_SaveDefaults,
            caption: ReturnControlText(
                fakeControlType: FakeControlTypes.ToolTip,
                controlName: "ttp_FrmImportExport_SaveDefaults"
            ));
    }

    #endregion

}

    #endregion

