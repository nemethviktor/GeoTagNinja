using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using geoTagNinja;
using static GeoTagNinja.frm_MainApp;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

namespace GeoTagNinja
{
    public partial class frm_editFileData : Form
    {
        #region Variables
        internal static bool frm_editFileDataNowLoadingFileData;
        internal static bool frm_editFileDataNowRemovingGeoData;
        #endregion
        public frm_editFileData()
        {
            // set basics
            CancelButton = btn_Cancel;
            AcceptButton = btn_OK;

            InitializeComponent();
        }
        private async void frm_editFileData_Load(object sender, EventArgs e)
        {
            frm_editFileDataNowLoadingFileData = true;
            frm_editFileDataNowRemovingGeoData = false;

            this.clh_FileName.Width = -2;
            this.lvw_FileListEditImages.Items[0].Selected = true;
            // actually if it's just one file i don't want this to be actively selectable
            if (lvw_FileListEditImages.Items.Count == 1)
            {
                lvw_FileListEditImages.Enabled = false;
            }

            // empty queue
            frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Clear();
            // also empty the "original data" table
            frm_MainApp.dt_fileDataToWriteStage2QueuePendingSave.Rows.Clear();

            // this below should auto-set nowLoadingFileData = false;
            lvw_FileListEditImagesGetData();
            await pbx_imgPreviewPicGenerator(lvw_FileListEditImages.Items[0].Text);
        }
        private void lvw_FileListEditImagesGetData()
        {
            frm_editFileDataNowLoadingFileData = true;
            string folderName = frm_MainApp.folderName;
            string fileName = lvw_FileListEditImages.SelectedItems[0].Text;
            string tempStr;
            frm_MainApp frm_MainAppInstance = (frm_MainApp)Application.OpenForms["frm_MainApp"];

            Helper_NonStatic Helper_nonstatic = new();
            IEnumerable<Control> c = Helper_nonstatic.GetAllControls(this);
            foreach (Control cItem in c)
            {
                if (cItem.GetType() == typeof(Label) || cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(Button) || cItem.GetType() == typeof(CheckBox) || cItem.GetType() == typeof(TabPage))
                {
                    // for some reason there is no .Last() being offered here
                    cItem.Text = Helper.DataReadSQLiteObjectText(
                        languageName: frm_MainApp.appLanguage,
                        objectType: (cItem.GetType().ToString().Split('.')[cItem.GetType().ToString().Split('.').Length - 1]),
                        objectName: cItem.Name
                        );
                }
                else if (cItem is TextBox || cItem is ComboBox)
                {
                    // reset font to normal
                    cItem.Font = new Font(cItem.Font, FontStyle.Regular);

                    // if label then we want text to come from datarow [objectText]
                    // else if textbox/dropdown then we want the data to come from the same spot [metaDataDirectoryData.tagName]
                    tempStr = frm_MainAppInstance.lvw_FileList.FindItemWithText(fileName).SubItems[frm_MainAppInstance.lvw_FileList.Columns["clh_" + cItem.Name.Substring(4)].Index].Text;
                    if (tempStr == "-")
                    {
                        {
                            cItem.Text = "";
                        }
                    }
                    else
                    {
                        cItem.Text = tempStr;
                    }
                    // stick into sql ("pending save") - this is to see if the data has changed later.

                    DataRow dr_FileDataRow;

                    dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage2QueuePendingSave.NewRow();
                    dr_FileDataRow["filePath"] = fileName;
                    dr_FileDataRow["settingId"] = cItem.Name.Substring(4);
                    dr_FileDataRow["settingValue"] = cItem.Text;
                    dt_fileDataToWriteStage2QueuePendingSave.Rows.Add(dr_FileDataRow);


                    // overwrite from sql-Q if available
                    // if data was pulled from the map this will sit in the main table, not in Q
                    DataView dv_sqlData_Q = new DataView(frm_MainApp.dt_fileDataToWriteStage1PreQueue);
                    dv_sqlData_Q.RowFilter = "filePath = '" + fileName + "' AND settingId ='" + cItem.Name.Substring(4) + "'";

                    DataView dv_sqlData_F = new DataView(frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite);
                    dv_sqlData_F.RowFilter = "filePath = '" + fileName + "' AND settingId ='" + cItem.Name.Substring(4) + "'";

                    if (dv_sqlData_Q.Count > 0 || dv_sqlData_F.Count > 0)
                    {
                        // see if data in temp-queue
                        if (dv_sqlData_Q.Count > 0)
                        {
                            cItem.Text = dv_sqlData_Q[0]["settingValue"].ToString();
                        }
                        // see if data is ready to be written
                        else if (dv_sqlData_F.Count > 0)
                        {
                            cItem.Text = dv_sqlData_F[0]["settingValue"].ToString();
                        }

                        dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage1PreQueue.NewRow();
                        dr_FileDataRow["filePath"] = lvw_FileListEditImages.SelectedItems[0].Text;
                        dr_FileDataRow["settingId"] = cItem.Name.Substring(4);
                        dr_FileDataRow["settingValue"] = cItem.Text;
                        frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Add(dr_FileDataRow);

                        if (cItem is TextBox txt)
                        {
                            txt.Font = new Font(txt.Font, FontStyle.Bold);
                        }
                        else if (cItem is ComboBox cmb)
                        {
                            cmb.Font = new Font(cmb.Font, FontStyle.Bold);
                        }
                    }
                }
            }
            // okay this is derp but i don't have a particularly better idea at this point
            // so basically countrycode and country need to be loaded first so we'll see how they are above...
            // then have another go at them here.
            foreach (Control cItem in c)
            {
                if (cItem.Name == "cbx_CountryCode" || cItem.Name == "cbx_Country")
                {
                    // marry up countrycodes and countrynames
                    string sqliteText;
                    if (cItem.Name == "cbx_CountryCode" && cItem.Text == "")
                    {
                        sqliteText = Helper.DataReadSQLiteCountryCodesNames(
                            queryWhat: "Country",
                            inputVal: ((ComboBox)cbx_Country).Text,
                            returnWhat: "ISO_3166_1A3"
                            );
                        if (this.cbx_CountryCode.Text != sqliteText)
                        {
                            this.cbx_CountryCode.Text = sqliteText;
                        }
                    }
                    else if (cItem.Name == "cbx_Country" && cItem.Text == "")
                    {
                        sqliteText = Helper.DataReadSQLiteCountryCodesNames(
                            queryWhat: "ISO_3166_1A3",
                            inputVal: ((ComboBox)cbx_CountryCode).Text,
                            returnWhat: "Country");
                        if (this.cbx_Country.Text != sqliteText)
                        {
                            this.cbx_Country.Text = sqliteText;
                        }
                    }
                }
            }

            // done load
            frm_editFileDataNowLoadingFileData = false;
        }
        #region object events
        private void btn_getFromWeb_Click(object sender, EventArgs e)
        {
            double parsedLat;
            double parsedLng;
            DataRow dr_FileDataRow;

            DataTable dt_Toponomy = new();
            DataTable dt_Altitude = new();
            frm_MainApp frm_MainAppInstance = (frm_MainApp)Application.OpenForms["frm_MainApp"];

            //reset this just in case.
            Helper.s_APIOkay = true;
            string strGPSLatitude;
            string strGPSLongitude;
            switch (((Button)sender).Name)
            {
                case "btn_getFromWeb_Toponomy":
                    strGPSLatitude = tbx_GPSLatitude.Text.Replace(',', '.');
                    strGPSLongitude = tbx_GPSLongitude.Text.Replace(',', '.');

                    if (double.TryParse(strGPSLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLat) && double.TryParse(strGPSLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLng))
                    {
                        dt_Toponomy = Helper.DTFromAPIExifGetToponomyFromWeb(strGPSLatitude, strGPSLongitude);

                        tbx_City.Text = dt_Toponomy.Rows[0]["City"].ToString();
                        tbx_State.Text = dt_Toponomy.Rows[0]["State"].ToString();
                        tbx_Sub_location.Text = dt_Toponomy.Rows[0]["Sub_location"].ToString();
                        cbx_CountryCode.Text = dt_Toponomy.Rows[0]["CountryCode"].ToString();
                        cbx_Country.Text = dt_Toponomy.Rows[0]["Country"].ToString();
                        // no need to write back to sql because it's done automatically on textboxChange
                    }
                    break;
                case "btn_getAllFromWeb_Toponomy":
                    foreach (ListViewItem lvi in lvw_FileListEditImages.Items)
                    {
                        string fileName = lvi.Text;
                        // for "this" file do the same as "normal" getfromweb
                        if (fileName == lvw_FileListEditImages.SelectedItems[0].Text)
                        {
                            strGPSLatitude = tbx_GPSLatitude.Text.Replace(',', '.');
                            strGPSLongitude = tbx_GPSLongitude.Text.Replace(',', '.');

                            if (double.TryParse(strGPSLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLat) && double.TryParse(strGPSLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLng))
                            {
                                dt_Toponomy = Helper.DTFromAPIExifGetToponomyFromWeb(strGPSLatitude, strGPSLongitude);
                                if (dt_Toponomy.Rows.Count > 0)
                                {
                                    tbx_City.Text = dt_Toponomy.Rows[0]["City"].ToString();
                                    tbx_State.Text = dt_Toponomy.Rows[0]["State"].ToString();
                                    tbx_Sub_location.Text = dt_Toponomy.Rows[0]["Sub_location"].ToString();
                                    cbx_CountryCode.Text = dt_Toponomy.Rows[0]["CountryCode"].ToString();
                                    cbx_Country.Text = dt_Toponomy.Rows[0]["Country"].ToString();
                                }
                                // no need to write back to sql because it's done automatically on textboxChange
                            }
                        }
                        else
                        {
                            // get lat/long from main listview
                            strGPSLatitude = frm_MainAppInstance.lvw_FileList.FindItemWithText(fileName).SubItems[frm_MainAppInstance.lvw_FileList.Columns["clh_GPSLatitude"].Index].Text.Replace(',', '.');
                            strGPSLongitude = frm_MainAppInstance.lvw_FileList.FindItemWithText(fileName).SubItems[frm_MainAppInstance.lvw_FileList.Columns["clh_GPSLongitude"].Index].Text.Replace(',', '.');
                            if (double.TryParse(strGPSLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLat) && double.TryParse(strGPSLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLng))
                            {
                                dt_Toponomy = Helper.DTFromAPIExifGetToponomyFromWeb(strGPSLatitude, strGPSLongitude);
                                if (dt_Toponomy.Rows.Count > 0)
                                {
                                    string CountryCode = dt_Toponomy.Rows[0]["CountryCode"].ToString();
                                    string Country = dt_Toponomy.Rows[0]["Country"].ToString();
                                    string City = dt_Toponomy.Rows[0]["City"].ToString();
                                    string State = dt_Toponomy.Rows[0]["State"].ToString();
                                    string Sub_location = dt_Toponomy.Rows[0]["Sub_location"].ToString();

                                    // Send off to SQL

                                    // CountryCode
                                    dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage1PreQueue.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "CountryCode";
                                    dr_FileDataRow["settingValue"] = CountryCode;
                                    frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Add(dr_FileDataRow);

                                    // Country
                                    dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage1PreQueue.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "Country";
                                    dr_FileDataRow["settingValue"] = Country;
                                    frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Add(dr_FileDataRow);

                                    // City
                                    dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage1PreQueue.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "City";
                                    dr_FileDataRow["settingValue"] = City;
                                    frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Add(dr_FileDataRow);

                                    // State
                                    dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage1PreQueue.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "State";
                                    dr_FileDataRow["settingValue"] = State;
                                    frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Add(dr_FileDataRow);

                                    // Sub_location
                                    dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage1PreQueue.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "Sub_location";
                                    dr_FileDataRow["settingValue"] = Sub_location;
                                    frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Add(dr_FileDataRow);
                                }
                            }
                        }
                    }

                    break;
                case "btn_getFromWeb_Altitude":
                    strGPSLatitude = tbx_GPSLatitude.Text.Replace(',', '.');
                    strGPSLongitude = tbx_GPSLongitude.Text.Replace(',', '.');

                    if (double.TryParse(strGPSLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLat) && double.TryParse(strGPSLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLng))
                    {
                        dt_Altitude = Helper.DTFromAPIExifGetAltitudeFromWeb(strGPSLatitude, strGPSLongitude);
                        if (dt_Altitude.Rows.Count > 0)
                        {
                            tbx_GPSAltitude.Text = dt_Altitude.Rows[0]["Altitude"].ToString();
                        }
                        // no need to write back to sql because it's done automatically on textboxChange
                    }
                    break;
                case "btn_getAllFromWeb_Altitude":
                    // same logic as toponomy
                    foreach (ListViewItem lvi in lvw_FileListEditImages.Items)
                    {
                        string fileName = lvi.Text;
                        // for "this" file do the same as "normal" getfromweb
                        if (fileName == lvw_FileListEditImages.SelectedItems[0].Text)
                        {
                            strGPSLatitude = tbx_GPSLatitude.Text.Replace(',', '.');
                            strGPSLongitude = tbx_GPSLongitude.Text.Replace(',', '.');

                            if (double.TryParse(strGPSLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLat) && double.TryParse(strGPSLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLng))
                            {
                                dt_Altitude = Helper.DTFromAPIExifGetAltitudeFromWeb(strGPSLatitude, strGPSLongitude);
                                if (dt_Altitude.Rows.Count > 0)
                                {
                                    tbx_GPSAltitude.Text = dt_Altitude.Rows[0]["Altitude"].ToString();
                                }
                                // no need to write back to sql because it's done automatically on textboxChange
                            }
                        }
                        else
                        {
                            // get lat/long from main listview
                            strGPSLatitude = frm_MainAppInstance.lvw_FileList.FindItemWithText(fileName).SubItems[frm_MainAppInstance.lvw_FileList.Columns["clh_GPSLatitude"].Index].Text.Replace(',', '.'); ;
                            strGPSLongitude = frm_MainAppInstance.lvw_FileList.FindItemWithText(fileName).SubItems[frm_MainAppInstance.lvw_FileList.Columns["clh_GPSLongitude"].Index].Text.Replace(',', '.'); ;
                            if (double.TryParse(strGPSLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLat) && double.TryParse(strGPSLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedLng))
                            {
                                dt_Altitude = Helper.DTFromAPIExifGetAltitudeFromWeb(strGPSLatitude, strGPSLongitude);
                                if (dt_Altitude.Rows.Count > 0)
                                {
                                    string Altitude = dt_Altitude.Rows[0]["Altitude"].ToString();

                                    dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage1PreQueue.NewRow();
                                    dr_FileDataRow["filePath"] = lvi.Text;
                                    dr_FileDataRow["settingId"] = "GPSAltitude";
                                    dr_FileDataRow["settingValue"] = Altitude;
                                    frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Add(dr_FileDataRow);
                                }
                            }
                        }
                    }
                    break;
                default:
                    MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_editFileData_ErrorInvalidSender") + ((Button)sender).Name, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
            if (Helper.s_APIOkay)
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_editFileData_InfoDataUpdated"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_editFileData_ErrorAPIError"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void lvw_FileListEditImages_MouseClick(object sender, MouseEventArgs e)
        {
            if (File.Exists(Path.Combine(folderName, lvw_FileListEditImages.SelectedItems[0].Text)))
            {
                lvw_FileListEditImagesGetData();

                this.pbx_imgPreview.Image = null;
                await pbx_imgPreviewPicGenerator(lvw_FileListEditImages.SelectedItems[0].Text);
            }
            else
            {
                MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_editFileData_WarningFileDisappeared"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void btn_OK_Click(object sender, EventArgs e)
        {
            // move data from temp-queue to write-queue
            if (frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Count > 0)
            {
                // transfer data from 1 to 3
                foreach (DataRow dr in dt_fileDataToWriteStage1PreQueue.Rows)
                {
                    frm_MainApp.dt_fileDataToWriteStage3ReadyToWrite.Rows.Add(dr.ItemArray);
                }

                // update listview w new data
                Helper.LwvUpdateRow();

                // drop from q
                frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Clear();
            }
            frm_MainApp.dt_fileDataToWriteStage2QueuePendingSave.Rows.Clear();
        }
        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            // clear the queues
            frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Clear();
            frm_MainApp.dt_fileDataToWriteStage2QueuePendingSave.Rows.Clear();
            this.Hide();
        }
        private void btn_RemoveGeoData_Click(object sender, EventArgs e)
        {
            Helper.ExifRemoveLocationData("frm_editFileData");
        }
        #region object text change handlers
        private void tbx_cbx_Any_TextChanged(object sender, EventArgs e)
        {
            if (frm_editFileDataNowLoadingFileData == false)
            {
                Control sndr = (Control)sender;

                DataView dv_PreviousText = new DataView(frm_MainApp.dt_fileDataToWriteStage2QueuePendingSave);
                dv_PreviousText.RowFilter = "filePath = '" + lvw_FileListEditImages.SelectedItems[0].Text + "' AND settingId ='" + sndr.Name.Substring(4) + "'";
                if (dv_PreviousText[0]["settingValue"].ToString() != sndr.Text)
                {
                    string strSndrText = sndr.Text.Replace(',', '.');
                    if (!frm_editFileDataNowRemovingGeoData && sndr.Parent.Name == "gbx_GPSData" && double.TryParse(strSndrText, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl) == false)
                    {
                        MessageBox.Show(Helper.GenericGetMessageBoxText("mbx_frm_editFileData_WarningLatLongMustBeNumbers"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        // find a valid number
                        sndr.Text = "0.0";
                    }
                    else
                    {
                        DataRow dr_FileDataRow;

                        sndr.Font = new Font(sndr.Font, FontStyle.Bold);
                        // marry up countrycodes and countrynames
                        string sqliteText;
                        if (sndr.Name == "cbx_CountryCode")
                        {
                            sqliteText = Helper.DataReadSQLiteCountryCodesNames(
                                queryWhat: "ISO_3166_1A3",
                                inputVal: sndr.Text,
                                returnWhat: "Country"
                                );
                            if (this.cbx_Country.Text != sqliteText)
                            {
                                this.cbx_Country.Text = sqliteText;
                            }
                        }
                        else if (sndr.Name == "cbx_Country")
                        {
                            sqliteText = Helper.DataReadSQLiteCountryCodesNames(
                                queryWhat: "Country",
                                inputVal: sndr.Text,
                                returnWhat: "ISO_3166_1A3"
                                );
                            if (this.cbx_CountryCode.Text != sqliteText)
                            {
                                this.cbx_CountryCode.Text = sqliteText;
                            }
                        }
                        dr_FileDataRow = frm_MainApp.dt_fileDataToWriteStage1PreQueue.NewRow();
                        dr_FileDataRow["filePath"] = lvw_FileListEditImages.SelectedItems[0].Text;
                        dr_FileDataRow["settingId"] = sndr.Name.Substring(4);
                        dr_FileDataRow["settingValue"] = sndr.Text;
                        frm_MainApp.dt_fileDataToWriteStage1PreQueue.Rows.Add(dr_FileDataRow);
                    }
                }
            }
        }
        #endregion
        private void tbx_cbx_Any_Enter(object sender, EventArgs e)
        {
            //previousText = this.Text;
        }
        #endregion
        private static async Task pbx_imgPreviewPicGenerator(string fileName)
        {

            // via https://stackoverflow.com/a/8701748/3968494
            Image img;
            string fileNameWithPath = Path.Combine(folderName, fileName);
            frm_editFileData frm_editFileDataInstance = (frm_editFileData)Application.OpenForms["frm_editFileData"];

            FileInfo fi = new(fileNameWithPath);
            if (fi.Extension == ".jpg")
            {
                using (var bmpTemp = new Bitmap(fileNameWithPath))
                {
                    img = new Bitmap(bmpTemp);
                    frm_editFileDataInstance.pbx_imgPreview.Image = img;
                }

            }
            else
            {
                string generatedFileName = Path.Combine(frm_MainApp.userDataFolderPath, fileName + ".jpg");
                // don't run the thing again if file has already been generated
                if (!File.Exists(generatedFileName))
                {
                    await Helper.ExifGetImagePreviews(fileName);
                }
                using (var bmpTemp = new Bitmap(generatedFileName))
                {
                    img = new Bitmap(bmpTemp);
                    frm_editFileDataInstance.pbx_imgPreview.Image = img;
                }
            }
        }
    }
}
