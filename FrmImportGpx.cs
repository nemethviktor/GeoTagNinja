using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using TimeZoneConverter;

namespace GeoTagNinja;

public partial class FrmImportGpx : Form
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
    public FrmImportGpx()
    {
        InitializeComponent();
        // set defaults
        rbt_importOneFile.Checked = true;
        pbx_importFromAnotherFolder.Enabled = false;
        lbl_importOneFile.Enabled = true;
        lbl_importFromAnotherFolder.Enabled = false;

        rbt_importFromCurrentFolder.Enabled = !Program.collectionModeEnabled;

        HelperControlAndMessageBoxHandling.ReturnControlText(cItem: this, senderForm: this);

        // load TZ-CBX
        foreach (string timezone in HelperGenericAncillaryListsArrays.GetTimeZones())
        {
            cbx_UseTimeZone.Items.Add(item: timezone);
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
        for (int i = 0; i < cbx_UseTimeZone.Items.Count; i++)
        {
            if (cbx_UseTimeZone.GetItemText(item: cbx_UseTimeZone.Items[index: i])
                .Contains(value: LocalIanatZname))
            {
                cbx_UseTimeZone.SelectedIndex = i;
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
            for (int i = 0; i < cbx_UseTimeZone.Items.Count; i++)
            {
                if (cbx_UseTimeZone.GetItemText(item: cbx_UseTimeZone.Items[index: i])
                    .StartsWith(value: "(" +
                                       plusMinusChar +
                                       TimeZoneInfo.Local.BaseUtcOffset.ToString()
                                           .Substring(startIndex: 0, length: 5)))
                {
                    cbx_UseTimeZone.SelectedIndex = i;
                    TZFound = true;
                    break;
                }
            }

            // if still fail then just pick the first and sod it.
            if (!TZFound)
            {
                cbx_UseTimeZone.SelectedIndex = 0;
            }
        }

        // this has to come here (rather than further up)
        ckb_UseTimeZone.Checked = false;
        ckb_UseDST.Checked = TimeZoneInfo.Local.IsDaylightSavingTime(dateTime: DateTime.Now);
        cbx_UseTimeZone.Enabled = false;
        ckb_UseDST.Enabled = false;

        // set filter for ofd
        string gpxExtensionsFilter = "Track Files|";
        foreach (string gpxExtension in HelperGenericAncillaryListsArrays.GpxExtensions())
        {
            gpxExtensionsFilter += "*." + gpxExtension + ";";
        }

        ofd_importOneFile.Filter = gpxExtensionsFilter;

        // set label texts
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            HelperControlAndMessageBoxHandling.ReturnControlText(cItem: cItem, senderForm: this);

            if (cItem.Name == "cbx_ImportTimeAgainst")
            {
                cbx_ImportTimeAgainst.Items.Add(item: "DateTimeOriginal");
                cbx_ImportTimeAgainst.Items.Add(item: "CreateDate");
                cbx_ImportTimeAgainst.SelectedIndex = 0;
            }
        }

        // trigger timer for datetime-update
        FormTimer.Enabled = true;
        FormTimer.Interval = 1000;
        FormTimer.Start();

        FormTimer.Tick += TimerEventProcessor;
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
        if (ckb_UseDST.Checked == false)
        {
            SelectedTzAdjustment = cbx_UseTimeZone.Text.Split('#')[0]
                .TrimStart(' ')
                .TrimEnd(' ')
                .Substring(startIndex: 1, length: 6);
        }
        else
        {
            SelectedTzAdjustment = cbx_UseTimeZone.Text.Split('#')[0]
                .TrimStart(' ')
                .TrimEnd(' ')
                .Substring(startIndex: 8, length: 6);
        }

        return SelectedTzAdjustment;
    }

    /// <summary>
    ///     Handles the loading of "most recent" time-shift settings
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btn_PullMostRecentTrackSyncShift_Click(object sender,
                                                        EventArgs e)
    {
        if (_lastShiftSecond == 0 && _lastShiftMinute == 0 && _lastShiftHour == 0 && _lastShiftDay == 0)
        {
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmImportNoStoredShiftValues"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
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


    #region Events

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
        if (rbt_importOneFile.Checked)
        {
            lbl_importFromAnotherFolder.Enabled = false;
        }
        else
        {
            lbl_importFromAnotherFolder.Enabled = true;
        }
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

    /// <summary>
    ///     Closes (hides) the Form
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_Cancel_Click(object sender,
                                  EventArgs e)
    {
        Hide();
    }

    /// <summary>
    ///     Collects the settings for track-parse and sends to the data collector; then closes the Form.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_OK_Click(object sender,
                                    EventArgs e)
    {
        string trackFileLocationType = "";
        string trackFileLocationVal = "";
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

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
            if (rbt_importFromCurrentFolder.Checked)
            {
                trackFileLocationVal = _frmMainAppInstance.tbx_FolderName.Text;
            }
            else
            {
                trackFileLocationVal = lbl_importFromAnotherFolder.Text;
            }
        }

        int timeShiftSeconds = 0;
        // adjust time as needed
        if (nud_Days.Value != 0 || nud_Hours.Value != 0 || nud_Minutes.Value != 0 || nud_Seconds.Value != 0)
        {
            timeShiftSeconds += (int)nud_Days.Value * 60 * 60 * 24;
            timeShiftSeconds += (int)nud_Hours.Value * 60 * 60;
            timeShiftSeconds += (int)nud_Minutes.Value * 60;
            timeShiftSeconds += (int)nud_Seconds.Value;
        }

        if ((trackFileLocationType == "file" && File.Exists(path: trackFileLocationVal)) || (trackFileLocationType == "folder" && Directory.Exists(path: trackFileLocationVal)))
        {
            // indicate that something is going on
            btn_OK.Text = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: sender.GetType()
                    .Name,
                objectName: "btn_OK_Working"
            );
            btn_OK.AutoSize = true;
            btn_OK.Enabled = false;
            btn_Cancel.Enabled = false;

            HelperExifReadTrackData.ExifGetTrackSyncData(
                trackFileLocationType: trackFileLocationType,
                trackFileLocationVal: trackFileLocationVal,
                useTZAdjust: ckb_UseTimeZone.Checked,
                compareTZAgainst: cbx_ImportTimeAgainst.Text,
                TZVal: lbl_TZValue.Text,
                GeoMaxIntSecs: (int)nud_GeoMaxIntSecs.Value,
                GeoMaxExtSecs: (int)nud_GeoMaxExtSecs.Value,
                doNotReverseGeoCode: ckb_DoNotQueryAPI.Checked,
                language: ISO639Lang,
                timeShiftSeconds: timeShiftSeconds
            );
            Hide();
        }
        else
        {
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmImportGpx_FileOrFolderDoesntExist"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
        }

        _lastShiftSecond = decimal.ToInt16(value: nud_Seconds.Value);
        _lastShiftMinute = decimal.ToInt16(value: nud_Minutes.Value);
        _lastShiftHour = decimal.ToInt16(value: nud_Hours.Value);
        _lastShiftDay = decimal.ToInt16(value: nud_Days.Value);
    }

    private void ckb_UseTimeZone_CheckedChanged(object sender,
                                                EventArgs e)
    {
        ckb_UseDST.Enabled = ckb_UseTimeZone.Checked;
        cbx_UseTimeZone.Enabled = ckb_UseTimeZone.Checked;
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
        SelectedIanatzName = cbx_UseTimeZone.Text.Split('#')[1]
            .TrimStart(' ')
            .TrimEnd(' ');
        lbl_TZValue.Text = updatelbl_TZValue();
    }

    #endregion
}