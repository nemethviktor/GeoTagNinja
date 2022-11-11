using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TimeZoneConverter;
using Control = System.Windows.Forms.Control;

namespace GeoTagNinja
{
    public partial class frm_importGPX : Form
    {
        static System.Windows.Forms.Timer formTimer = new System.Windows.Forms.Timer();
        internal const string doubleQuote = "\"";
        internal static string localIANATZname;
        internal static string selectedIANATZName;
        internal static string selectedTZAdjustment;

        frm_MainApp frm_mainAppInstance = (frm_MainApp)Application.OpenForms["frm_mainApp"];

        /// <summary>
        /// This form helps import various Track files. 
        /// For the list of currently supported file types & formats check: https://exiftool.org/geotag.html
        /// </summary>
        public frm_importGPX()
        {
            InitializeComponent();
            // set defaults
            this.rbt_importOneFile.Checked = true;
            this.pbx_importFromAnotherFolder.Enabled = false;
            this.lbl_importOneFile.Enabled = true;
            this.lbl_importFromAnotherFolder.Enabled = false;

            // load TZ-CBX
            foreach (string timezone in ancillary_ListsArrays.getTimeZones())
            {
                cbx_UseTimeZone.Items.Add(timezone);
            }

            try
            {
                localIANATZname = TZConvert.WindowsToIana(TimeZoneInfo.Local.Id);
            }
            catch
            {
                localIANATZname = "Europe/London";
            }

            // this is a little lame but works. -> try to default to local TZ
            for (int i = 0; i < cbx_UseTimeZone.Items.Count; i++)
            {
                if (cbx_UseTimeZone.GetItemText(cbx_UseTimeZone.Items[i]).Contains(localIANATZname))
                {
                    cbx_UseTimeZone.SelectedIndex = i;
                    break;
                }
            }
            // this has to come here (rather than further up)
            this.ckb_UseTimeZone.Checked = false;
            this.ckb_UseDST.Checked = TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now);
            this.cbx_UseTimeZone.Enabled = false;
            this.ckb_UseDST.Enabled = false;


            // set filter for ofd
            string gpxExtensionsFilter = "Track Files|";
            foreach (string gpxExtension in ancillary_ListsArrays.gpxExtensions())
            {
                gpxExtensionsFilter += "*." + gpxExtension + ";";
            }
            ofd_importOneFile.Filter = gpxExtensionsFilter;

            // set label texts
            Helper_NonStatic Helper_nonstatic = new Helper_NonStatic();
            IEnumerable<Control> c = Helper_nonstatic.GetAllControls(this);
            foreach (Control cItem in c)
            {
                Helper.GenericReturnControlText(cItem: cItem, senderForm: this);

                if (cItem.Name == "cbx_ImportTimeAgainst")
                {
                    this.cbx_ImportTimeAgainst.Items.Add("DateTimeOriginal");
                    this.cbx_ImportTimeAgainst.Items.Add("CreateDate");
                    this.cbx_ImportTimeAgainst.SelectedIndex = 0;
                }
            }

            // trigger timer for datetime-update
            formTimer.Enabled = true;
            formTimer.Interval = 1000;
            formTimer.Start();

            formTimer.Tick += new EventHandler(TimerEventProcessor);
        }

        /// <summary>
        /// This updates the "now" value of lbl_CameraTimeData with any user adjustments there may be.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerEventProcessor(object sender, EventArgs e)
        {
            lbl_CameraTimeData.Text = DateTime.Now.AddDays((int)this.nud_Days.Value).AddHours((int)this.nud_Hours.Value).AddMinutes((int)this.nud_Minutes.Value).AddSeconds((int)this.nud_Seconds.Value).ToString("yyyy MMMM dd HH:mm:ss");
        }
        #region Events
        /// <summary>
        /// Opens a file browser for track files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbx_importOneFile_Click(object sender, EventArgs e)
        {
            if (ofd_importOneFile.ShowDialog() == DialogResult.OK)
            {
                lbl_importOneFile.Text = ofd_importOneFile.FileName;
            }
        }
        /// <summary>
        /// Opens a folder browser for track files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbx_importFromAnotherFolder_Click(object sender, EventArgs e)
        {
            if (fbd_importFromAnotherFolder.ShowDialog() == DialogResult.OK)
            {
                lbl_importFromAnotherFolder.Text = fbd_importFromAnotherFolder.SelectedPath;
            }
        }

        private void rbt_importOneFile_CheckedChanged(object sender, EventArgs e)
        {
            this.pbx_importOneFile.Enabled = this.rbt_importOneFile.Checked;
            this.lbl_importOneFile.Enabled = this.rbt_importOneFile.Checked;
            if (this.rbt_importOneFile.Checked == true)
            {
                this.lbl_importFromAnotherFolder.Enabled = false;
            }
            else
            {
                this.lbl_importFromAnotherFolder.Enabled = true;
            }
        }

        private void rbt_importFromCurrentFolder_CheckedChanged(object sender, EventArgs e)
        {
            this.pbx_importOneFile.Enabled = false;
            this.pbx_importFromAnotherFolder.Enabled = false;
            this.lbl_importOneFile.Enabled = false;
            this.lbl_importFromAnotherFolder.Enabled = false;
        }

        private void rbt_importFromAnotherFolder_CheckedChanged(object sender, EventArgs e)
        {
            this.pbx_importFromAnotherFolder.Enabled = this.rbt_importFromAnotherFolder.Checked == true;
            this.lbl_importFromAnotherFolder.Enabled = this.rbt_importFromAnotherFolder.Checked == true;
            if (this.rbt_importFromAnotherFolder.Checked == true)
            {
                this.pbx_importOneFile.Enabled = false;
                this.lbl_importOneFile.Enabled = false;
            }
            else
            {
                this.pbx_importOneFile.Enabled = true;
                this.lbl_importOneFile.Enabled = true;
            }
        }

        /// <summary>
        /// Closes (hides) the Form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
        /// <summary>
        /// Collects the settings for track-parse and sends to the data collector; then closes the Form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btn_OK_Click(object sender, EventArgs e)
        {
            string trackFileLocationType = "";
            string trackFileLocationVal = "";
            
            // one source:
            // exiftool -geotag "c:\gps logs\track.log" x.jpg
            if (this.rbt_importOneFile.Checked == true)
            {
                trackFileLocationType = "file";
                trackFileLocationVal = this.lbl_importOneFile.Text;
            }
            else
            {
                trackFileLocationType = "folder";
                if (this.rbt_importFromCurrentFolder.Checked == true)
                {
                    trackFileLocationVal = frm_mainAppInstance.tbx_FolderName.Text;
                }
                else
                {
                    trackFileLocationVal = lbl_importFromAnotherFolder.Text;
                }
            };

            int timeShiftSeconds = 0;
            // adjust time as needed
            if (this.nud_Days.Value != 0 || this.nud_Hours.Value != 0 || this.nud_Minutes.Value != 0 || this.nud_Seconds.Value != 0)
            {
                timeShiftSeconds += (int)this.nud_Days.Value * 60 * 60 * 24;
                timeShiftSeconds += (int)this.nud_Hours.Value * 60 * 60;
                timeShiftSeconds += (int)this.nud_Minutes.Value * 60;
                timeShiftSeconds += (int)this.nud_Seconds.Value;
            }

            if ((trackFileLocationType == "file" && File.Exists(trackFileLocationVal)) || (trackFileLocationType == "folder" && Directory.Exists(trackFileLocationVal)))
            {
                // indicate that something is going on
                btn_OK.Text = Helper.DataReadSQLiteObjectText(
                                languageName: frm_MainApp.appLanguage,
                                objectType: sender.GetType().ToString().Split('.').Last(),
                                actionType: "Working",
                                objectName: "btn_OK"
                                );
                btn_OK.AutoSize = true;
                btn_OK.Enabled = false;

                await Helper.ExifGetTrackSyncData(
                    trackFileLocationType: trackFileLocationType,
                    trackFileLocationVal: trackFileLocationVal,
                    useTZAdjust: ckb_UseTimeZone.Checked,
                    compareTZAgainst: cbx_ImportTimeAgainst.Text,
                    TZVal: lbl_TZValue.Text,
                    GeoMaxIntSecs: (int)nud_GeoMaxIntSecs.Value,
                    GeoMaxExtSecs: (int)nud_GeoMaxExtSecs.Value,
                    timeShiftSeconds: timeShiftSeconds
                    );
                this.Hide();
            }
            else
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_importGPX_FileOrFolderDoesntExist"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ckb_UseTimeZone_CheckedChanged(object sender, EventArgs e)
        {
            this.ckb_UseDST.Enabled = this.ckb_UseTimeZone.Checked;
            this.cbx_UseTimeZone.Enabled = this.ckb_UseTimeZone.Checked;
            lbl_TZValue.Text = updatelbl_TZValue();
        }

        private void ckb_UseDST_CheckedChanged(object sender, EventArgs e)
        {
            lbl_TZValue.Text = updatelbl_TZValue();
        }

        private void cbx_UseTimeZone_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedIANATZName = cbx_UseTimeZone.Text.Split('#')[1].TrimStart(' ').TrimEnd(' ');
            lbl_TZValue.Text = updatelbl_TZValue();
        }
        #endregion
        private string updatelbl_TZValue()
        {
            if (ckb_UseDST.Checked == false)
            {
                selectedTZAdjustment = cbx_UseTimeZone.Text.Split('#')[0].TrimStart(' ').TrimEnd(' ').Substring(1, 6);
            }
            else
            {
                selectedTZAdjustment = cbx_UseTimeZone.Text.Split('#')[0].TrimStart(' ').TrimEnd(' ').Substring(8, 6);
            }
            return selectedTZAdjustment;
        }
    }
}
