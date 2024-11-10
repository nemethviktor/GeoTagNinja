using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using GeoTagNinja.View.DialogAndMessageBoxes;
using TimeZoneConverter;
using static GeoTagNinja.Helpers.HelperGenericAncillaryListsArrays;

namespace GeoTagNinja;

public partial class FrmImportExportGpx : Form
{
    internal const string DoubleQuote = "\"";
    private static readonly Timer FormTimer = new();
    private static string LocalIanatZname;
    private static string SelectedIanatzName;
    private static string SelectedTzAdjustment;
    private static string ISO639Lang;
    private static int _lastShiftSecond;
    private static int _lastShiftMinute;
    private static int _lastShiftHour;
    private static int _lastShiftDay;
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

        HelperControlThemeManager.SetThemeColour(
            themeColour: HelperVariables.UserSettingUseDarkMode
                ? ThemeColour.Dark
                : ThemeColour.Light, parentControl: this);


        // set defaults
        rbt_importOneFile.Checked = true;
        pbx_importFromAnotherFolder.Enabled = false;
        lbl_importOneFile.Enabled = true;
        lbl_importFromAnotherFolder.Enabled = false;
        ckb_OverlayGPXForSelectedDatesOnly.Enabled = false;

        rbt_importFromCurrentFolder.Enabled = !Program.CollectionModeEnabled;

        HelperControlAndMessageBoxHandling.ReturnControlText(cItem: this, senderForm: this);

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
                    extension) => current + "*." + extension + ";");

        ofd_importOneFile.Filter = gpxExtensionsFilter;

        // set label texts and combobox items
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            HelperControlAndMessageBoxHandling.ReturnControlText(cItem: cItem, senderForm: this);

            if (cItem.Name == "cbx_ImportTimeAgainst")
            {
                foreach (object importTimeAgainstItem in Enum.GetValues(enumType: typeof(ImportTimeAgainst)))
                {
                    cbx_ImportTimeAgainst.Items.Add(item: importTimeAgainstItem.ToString());
                }

                cbx_ImportTimeAgainst.SelectedIndex = 0;
            }

            else if (cItem.Name == "cbx_ExportTrackOrderBy")
            {
                foreach (object exportFileOrderItem in Enum.GetValues(enumType: typeof(ExportFileOrder)))
                {
                    cbx_ExportTrackOrderBy.Items.Add(item: exportFileOrderItem.ToString());
                }

                cbx_ExportTrackOrderBy.SelectedIndex = 0;
            }
            else if (cItem.Name == "cbx_ExportTrackTimeStampType")
            {
                foreach (object exportFileFMTTimeBasisItem in Enum.GetValues(enumType: typeof(ExportFileFMTTimeBasis)))
                {
                    cbx_ExportTrackTimeStampType.Items.Add(item: exportFileFMTTimeBasisItem.ToString());
                }

                cbx_ExportTrackTimeStampType.SelectedIndex = 0;
            }
        }

        // trigger timer for datetime-update
        FormTimer.Enabled = true;
        FormTimer.Interval = 1000;
        FormTimer.Start();

        FormTimer.Tick += TimerEventProcessor;
    }

    private void FillTZComboBox(ComboBox cbxToFill)
    {
        foreach (string timezone in GetTimeZones())
        {
            cbxToFill.Items.Add(item: timezone);
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
                             .StartsWith(value: "(" +
                                                plusMinusChar +
                                                TimeZoneInfo.Local.BaseUtcOffset.ToString()
                                                            .Substring(startIndex: 0, length: 5)))
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
        List<DateTime> retList = new();
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
                    btn_Generic_OK.Text = HelperControlAndMessageBoxHandling.ReturnControlText(
                        controlName: "Generic_PleaseWait",
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.Generic);
                    btn_Generic_OK.AutoSize = true;
                    btn_Generic_OK.Enabled = false;
                    btn_Generic_Cancel.Enabled = false;
                    List<DateTime> overlayDateList = new();

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
                    Hide();
                }
                else
                {
                    CustomMessageBox customMessageBox = new(
                        text: HelperControlAndMessageBoxHandling.ReturnControlText(
                            controlName: "mbx_FrmImportExportGpx_FileOrFolderDoesntExist",
                            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                        caption: HelperControlAndMessageBoxHandling.ReturnControlText(
                            controlName: HelperControlAndMessageBoxHandling.MessageBoxCaption
                                                                           .Error.ToString(),
                            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBoxCaption),
                        buttons: MessageBoxButtons.OK,
                        icon: MessageBoxIcon.Error);
                    customMessageBox.ShowDialog();
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


    private void ckb_LoadTrackOntoMap_CheckedChanged(object sender, EventArgs e)
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
            CustomMessageBox customMessageBox = new(
                text: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_FrmImportNoStoredShiftValues",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                caption: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: HelperControlAndMessageBoxHandling.MessageBoxCaption
                                                                   .Error.ToString(),
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBoxCaption),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            customMessageBox.ShowDialog();
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

    private void pbx_Browse_SaveTo_Folder_Click(object sender, EventArgs e)
    {
        if (ofd_SaveTrackTo.ShowDialog() == DialogResult.OK)
        {
            tbx_SaveTrackTo.Text = ofd_SaveTrackTo.FileName;
        }
    }
}

#endregion

#endregion