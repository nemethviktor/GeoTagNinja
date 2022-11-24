using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using static GeoTagNinja.FrmMainApp;

namespace GeoTagNinja;

public partial class FrmEditFileData : Form
{
    /// <summary>
    ///     This Form provides an interface for the user to edit various bits of Exif data in images.
    /// </summary>
    public FrmEditFileData()
    {
        // set basics
        CancelButton = btn_Cancel;
        AcceptButton = btn_OK;

        InitializeComponent();
    }

    /// <summary>
    ///     Fires when loading the form. Sets defaults for the listview and makes sure the app is ready to read the file data
    ///     ...w/o marking changes to textboxes (aka when a value changes the textbox formatting will generally turn to bold
    ///     but
    ///     ...when going from "nothing" to "something" that's obviously a change and we don't want that.)
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void FrmEditFileData_Load(object sender,
                                            EventArgs e)
    {
        FrmEditFileDataNowLoadingFileData = true;
        FrmEditFileDataNowRemovingGeoData = false;

        clh_FileName.Width = -2;
        if (lvw_FileListEditImages.Items.Count > 0)
        {
            lvw_FileListEditImages.Items[index: 0]
                .Selected = true;

            // actually if it's just one file i don't want this to be actively selectable
            if (lvw_FileListEditImages.Items.Count == 1)
            {
                lvw_FileListEditImages.Enabled = false;
            }

            // empty queue
            DtFileDataToWriteStage1PreQueue.Rows.Clear();
            // also empty the "original data" table
            DtFileDataToWriteStage2QueuePendingSave.Rows.Clear();

            // this below should auto-set nowLoadingFileData = false;
            lvw_EditorFileListImagesGetData();
            string fileNameWithPath = Path.Combine(path1: FolderName, path2: lvw_FileListEditImages.Items[index: 0]
                                                       .Text);

            await pbx_imgPreviewPicGenerator(fileNameWithPath: fileNameWithPath);
        }
    }

    /// <summary>
    ///     Gets the text values for the Controls in the Form - for labels/buttons etc this is their "language" (eg. English)
    ///     label (e.g. "Latitude")
    ///     ... for textboxes etc this is the value (e.g. "51.002")
    /// </summary>
    private void lvw_EditorFileListImagesGetData()
    {
        FrmEditFileDataNowLoadingFileData = true;
        string folderName = FolderName;
        string fileNameWithOutPath = lvw_FileListEditImages.SelectedItems[index: 0]
            .Text;
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(Label) || cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(Button) || cItem.GetType() == typeof(CheckBox) || cItem.GetType() == typeof(TabPage))
            {
                HelperStatic.GenericReturnControlText(cItem: cItem, senderForm: this);
            }
            else if (cItem is TextBox || cItem is ComboBox)
            {
                // reset font to normal
                cItem.Font = new Font(prototype: cItem.Font, newStyle: FontStyle.Regular);

                // if label then we want text to come from datarow [objectText]
                // else if textbox/dropdown then we want the data to come from the same spot [metaDataDirectoryData.tagName]
                string tempStr = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithOutPath)
                    .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: "clh_" + cItem.Name.Substring(startIndex: 4)]
                                  .Index]
                    .Text;
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
                HelperStatic.GenericUpdateAddToDataTable(
                    dt: DtFileDataToWriteStage2QueuePendingSave,
                    fileNameWithoutPath: fileNameWithOutPath,
                    settingId: cItem.Name.Substring(startIndex: 4),
                    settingValue: cItem.Text
                );

                // overwrite from sql-Q if available
                // if data was pulled from the map this will sit in the main table, not in Q
                DataView dvSqlDataQ = new(table: DtFileDataToWriteStage1PreQueue);
                dvSqlDataQ.RowFilter = "fileNameWithOutPath = '" + fileNameWithOutPath + "' AND settingId ='" + cItem.Name.Substring(startIndex: 4) + "'";

                DataView dvSqlDataF = new(table: DtFileDataToWriteStage3ReadyToWrite);
                dvSqlDataF.RowFilter = "fileNameWithOutPath = '" + fileNameWithOutPath + "' AND settingId ='" + cItem.Name.Substring(startIndex: 4) + "'";

                if (dvSqlDataQ.Count > 0 || dvSqlDataF.Count > 0)
                {
                    // see if data in temp-queue
                    if (dvSqlDataQ.Count > 0)
                    {
                        cItem.Text = dvSqlDataQ[recordIndex: 0][property: "settingValue"]
                            .ToString();
                    }
                    // see if data is ready to be written
                    else if (dvSqlDataF.Count > 0)
                    {
                        cItem.Text = dvSqlDataF[recordIndex: 0][property: "settingValue"]
                            .ToString();
                    }

                    HelperStatic.GenericUpdateAddToDataTable(
                        dt: DtFileDataToWriteStage1PreQueue,
                        fileNameWithoutPath: lvw_FileListEditImages.SelectedItems[index: 0]
                            .Text,
                        settingId: cItem.Name.Substring(startIndex: 4),
                        settingValue: cItem.Text
                    );

                    if (cItem is TextBox txt)
                    {
                        txt.Font = new Font(prototype: txt.Font, newStyle: FontStyle.Bold);
                    }
                    else if (cItem is ComboBox cmb)
                    {
                        cmb.Font = new Font(prototype: cmb.Font, newStyle: FontStyle.Bold);
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
                    sqliteText = HelperStatic.DataReadSQLiteCountryCodesNames(
                        queryWhat: "Country",
                        inputVal: cbx_Country.Text,
                        returnWhat: "ISO_3166_1A3"
                    );
                    if (cbx_CountryCode.Text != sqliteText)
                    {
                        cbx_CountryCode.Text = sqliteText;
                    }
                }
                else if (cItem.Name == "cbx_Country" && cItem.Text == "")
                {
                    sqliteText = HelperStatic.DataReadSQLiteCountryCodesNames(
                        queryWhat: "ISO_3166_1A3",
                        inputVal: cbx_CountryCode.Text,
                        returnWhat: "Country");
                    if (cbx_Country.Text != sqliteText)
                    {
                        cbx_Country.Text = sqliteText;
                    }
                }
            }
        }

        // done load
        FrmEditFileDataNowLoadingFileData = false;
    }

    /// <summary>
    ///     Attempts to generate preview image for the image that was clicked on.
    /// </summary>
    /// <param name="fileNameWithPath">Name (path) of the file that was clicked on.</param>
    /// <returns></returns>
    private static async Task pbx_imgPreviewPicGenerator(string fileNameWithPath)
    {
        string fileName = Path.GetFileName(path: fileNameWithPath);
        // via https://stackoverflow.com/a/8701748/3968494
        FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
        Image img = null;
        if (frmEditFileDataInstance != null)
        {
            try
            {
                using Bitmap bmpTemp = new(filename: fileNameWithPath);
                img = new Bitmap(original: bmpTemp);
                frmEditFileDataInstance.pbx_imagePreview.Image = img;
            }
            catch
            {
                // nothing.
            }

            if (img == null)
            {
                string generatedFileName = Path.Combine(path1: UserDataFolderPath, path2: fileName + ".jpg");
                // don't run the thing again if file has already been generated
                if (!File.Exists(path: generatedFileName))
                {
                    await HelperStatic.ExifGetImagePreviews(fileNameWithOutPath: fileNameWithPath);
                }

                //sometimes the file doesn't get created. (ie exiftool may fail to extract a preview.)
                if (File.Exists(path: generatedFileName))
                {
                    try
                    {
                        using Bitmap bmpTemp = new(filename: generatedFileName);
                        img = new Bitmap(original: bmpTemp);
                        frmEditFileDataInstance.pbx_imagePreview.Image = img;
                    }
                    catch
                    {
                        // nothing.
                    }
                }
            }
        }
    }

    #region Variables

    private static bool FrmEditFileDataNowLoadingFileData;
    internal static bool FrmEditFileDataNowRemovingGeoData;

    #endregion

    #region object events

    /// <summary>
    ///     Pulls data for the various "Get (All) From Web" buttons depending which actual button has been pressed.
    ///     The TLDR logic is that if it's not the "All" button then we only read the currently active file else we read all
    ///     ...but ofc the currently not visible files' data doesn't show to the user so that goes into the holding tables.
    /// </summary>
    /// <param name="sender">The object that has been interacted with</param>
    /// <param name="e">Unused</param>
    private void btn_getFromWeb_Click(object sender,
                                      EventArgs e)
    {
        double parsedLat;
        double parsedLng;

        DataTable dtToponomy = new();
        DataTable dtAltitude = new();
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        //reset this just in case.
        HelperStatic.SApiOkay = true;
        string strGpsLatitude;
        string strGpsLongitude;
        switch (((Button)sender).Name)
        {
            case "btn_getFromWeb_Toponomy":
                strGpsLatitude = tbx_GPSLatitude.Text.ToString(provider: CultureInfo.InvariantCulture);
                strGpsLongitude = tbx_GPSLongitude.Text.ToString(provider: CultureInfo.InvariantCulture);

                if (double.TryParse(s: strGpsLatitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strGpsLongitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
                {
                    dtToponomy = HelperStatic.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);

                    tbx_City.Text = dtToponomy.Rows[index: 0][columnName: "City"]
                        .ToString();
                    tbx_State.Text = dtToponomy.Rows[index: 0][columnName: "State"]
                        .ToString();
                    tbx_Sub_location.Text = dtToponomy.Rows[index: 0][columnName: "Sub_location"]
                        .ToString();
                    cbx_CountryCode.Text = dtToponomy.Rows[index: 0][columnName: "CountryCode"]
                        .ToString();
                    cbx_Country.Text = dtToponomy.Rows[index: 0][columnName: "Country"]
                        .ToString();
                    // no need to write back to sql because it's done automatically on textboxChange
                }

                break;
            case "btn_getAllFromWeb_Toponomy":
                foreach (ListViewItem lvi in lvw_FileListEditImages.Items)
                {
                    string fileName = lvi.Text;
                    // for "this" file do the same as "normal" getfromweb
                    if (fileName ==
                        lvw_FileListEditImages.SelectedItems[index: 0]
                            .Text)
                    {
                        strGpsLatitude = tbx_GPSLatitude.Text.ToString(provider: CultureInfo.InvariantCulture);
                        strGpsLongitude = tbx_GPSLongitude.Text.ToString(provider: CultureInfo.InvariantCulture);

                        if (double.TryParse(s: strGpsLatitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strGpsLongitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
                        {
                            dtToponomy = HelperStatic.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
                            if (dtToponomy.Rows.Count > 0)
                            {
                                tbx_City.Text = dtToponomy.Rows[index: 0][columnName: "City"]
                                    .ToString();
                                tbx_State.Text = dtToponomy.Rows[index: 0][columnName: "State"]
                                    .ToString();
                                tbx_Sub_location.Text = dtToponomy.Rows[index: 0][columnName: "Sub_location"]
                                    .ToString();
                                cbx_CountryCode.Text = dtToponomy.Rows[index: 0][columnName: "CountryCode"]
                                    .ToString();
                                cbx_Country.Text = dtToponomy.Rows[index: 0][columnName: "Country"]
                                    .ToString();
                            }
                            // no need to write back to sql because it's done automatically on textboxChange
                        }
                    }
                    else
                    {
                        // get lat/long from main listview
                        strGpsLatitude = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileName)
                            .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: "clh_GPSLatitude"]
                                          .Index]
                            .Text.ToString(provider: CultureInfo.InvariantCulture);
                        strGpsLongitude = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileName)
                            .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: "clh_GPSLongitude"]
                                          .Index]
                            .Text.ToString(provider: CultureInfo.InvariantCulture);
                        if (double.TryParse(s: strGpsLatitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strGpsLongitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
                        {
                            dtToponomy = HelperStatic.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
                            if (dtToponomy.Rows.Count > 0)
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

                                foreach ((string toponomyOverwriteName, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
                                {
                                    HelperStatic.GenericUpdateAddToDataTable(
                                        dt: DtFileDataToWriteStage1PreQueue,
                                        fileNameWithoutPath: lvi.Text,
                                        settingId: toponomyDetail.toponomyOverwriteName,
                                        settingValue: toponomyDetail.toponomyOverwriteVal
                                    );
                                }

                                lvi.ForeColor = Color.Red;
                            }
                        }
                    }
                }

                break;
            case "btn_getFromWeb_Altitude":
                strGpsLatitude = tbx_GPSLatitude.Text.ToString(provider: CultureInfo.InvariantCulture);
                strGpsLongitude = tbx_GPSLongitude.Text.ToString(provider: CultureInfo.InvariantCulture);

                if (double.TryParse(s: strGpsLatitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strGpsLongitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
                {
                    dtAltitude = HelperStatic.DTFromAPIExifGetAltitudeFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
                    if (dtAltitude.Rows.Count > 0)
                    {
                        tbx_GPSAltitude.Text = dtAltitude.Rows[index: 0][columnName: "Altitude"]
                            .ToString();
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
                    if (fileName ==
                        lvw_FileListEditImages.SelectedItems[index: 0]
                            .Text)
                    {
                        strGpsLatitude = tbx_GPSLatitude.Text.ToString(provider: CultureInfo.InvariantCulture);
                        strGpsLongitude = tbx_GPSLongitude.Text.ToString(provider: CultureInfo.InvariantCulture);

                        if (double.TryParse(s: strGpsLatitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strGpsLongitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
                        {
                            dtAltitude = HelperStatic.DTFromAPIExifGetAltitudeFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
                            if (dtAltitude.Rows.Count > 0)
                            {
                                tbx_GPSAltitude.Text = dtAltitude.Rows[index: 0][columnName: "Altitude"]
                                    .ToString();
                            }
                            // no need to write back to sql because it's done automatically on textboxChange
                        }
                    }
                    else
                    {
                        // get lat/long from main listview
                        strGpsLatitude = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileName)
                            .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: "clh_GPSLatitude"]
                                          .Index]
                            .Text.ToString(provider: CultureInfo.InvariantCulture);
                        strGpsLongitude = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileName)
                            .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: "clh_GPSLongitude"]
                                          .Index]
                            .Text.ToString(provider: CultureInfo.InvariantCulture);
                        if (double.TryParse(s: strGpsLatitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strGpsLongitude, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
                        {
                            dtAltitude = HelperStatic.DTFromAPIExifGetAltitudeFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
                            if (dtAltitude.Rows.Count > 0)
                            {
                                string altitude = dtAltitude.Rows[index: 0][columnName: "Altitude"]
                                    .ToString();
                                HelperStatic.GenericUpdateAddToDataTable(
                                    dt: DtFileDataToWriteStage1PreQueue,
                                    fileNameWithoutPath: lvi.Text,
                                    settingId: "GPSAltitude",
                                    settingValue: altitude
                                );
                            }
                        }
                    }
                }

                break;
            default:
                MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmEditFileData_ErrorInvalidSender") + ((Button)sender).Name, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                break;
        }

        if (HelperStatic.SApiOkay)
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmEditFileData_InfoDataUpdated"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmEditFileData_ErrorAPIError"), caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }


    /// <summary>
    ///     Handles the keyboard interactions (move up/down)
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void lvw_FileListEditImages_SelectedIndexChanged(object sender,
                                                                   EventArgs e)
    {
        if (lvw_FileListEditImages.SelectedItems.Count > 0)
        {
            string fileNameWithPath = Path.Combine(path1: FolderName, path2: lvw_FileListEditImages.SelectedItems[index: 0]
                                                       .Text);
            if (File.Exists(path: fileNameWithPath))
            {
                lvw_EditorFileListImagesGetData();

                pbx_imagePreview.Image = null;
                await pbx_imgPreviewPicGenerator(fileNameWithPath: fileNameWithPath);
            }
            else
            {
                MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmEditFileData_WarningFileDisappeared"), caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
            }
        }
    }

    /// <summary>
    ///     Handles when user clicks on the OK button. Moves the data from holding table 1 to 3 and updates the main listview
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void btn_OK_Click(object sender,
                                    EventArgs e)
    {
        // move data from temp-queue to write-queue
        if (DtFileDataToWriteStage1PreQueue.Rows.Count > 0)
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            // transfer data from 1 to 3
            foreach (DataRow drS1 in DtFileDataToWriteStage1PreQueue.Rows)
            {
                // any existing instance of this particular combination needs to be deleted...
                // eg if we type "50" as a value then w/o this we'd end up with a "5" row and a "50" row.
                for (int i = DtFileDataToWriteStage3ReadyToWrite.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow drS3 = DtFileDataToWriteStage3ReadyToWrite.Rows[index: i];
                    if (
                        drS3[columnName: "fileNameWithOutPath"]
                            .ToString() ==
                        drS1[columnName: "fileNameWithOutPath"]
                            .ToString() &&
                        drS3[columnName: "settingId"]
                            .ToString() ==
                        drS1[columnName: "settingId"]
                            .ToString()
                    )
                    {
                        drS3.Delete();
                    }
                }

                DtFileDataToWriteStage3ReadyToWrite.AcceptChanges();
                DtFileDataToWriteStage3ReadyToWrite.Rows.Add(values: drS1.ItemArray);
            }

            // update listview w new data
            HelperStatic.FileListBeingUpdated = true;
            foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.Items)
            {
                await HelperStatic.LwvUpdateRowFromDTWriteStage3ReadyToWrite(lvi: lvi);
            }

            HelperStatic.FileListBeingUpdated = false;

            // drop from q
            DtFileDataToWriteStage1PreQueue.Rows.Clear();
        }

        DtFileDataToWriteStage2QueuePendingSave.Rows.Clear();
        // re-center map on new data.

        await HelperStatic.LvwItemClickNavigate();
    }

    /// <summary>
    ///     Handles when user clicks Cancel. Clears holding tables 1 & 2.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_Cancel_Click(object sender,
                                  EventArgs e)
    {
        // clear the queues
        DtFileDataToWriteStage1PreQueue.Rows.Clear();
        DtFileDataToWriteStage2QueuePendingSave.Rows.Clear();
        Hide();
    }

    /// <summary>
    ///     This pulls up the "Paste-What" Form.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_SetCurrentValues_Click(object sender,
                                            EventArgs e)
    {
        FrmPasteWhat frmPasteWhat = new(initiator: Name);
        frmPasteWhat.ShowDialog();
    }

    /// <summary>
    ///     Handles when user requests removal of all geodata from selected img.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void btn_RemoveGeoData_Click(object sender,
                                               EventArgs e)
    {
        await HelperStatic.ExifRemoveLocationData(senderName: "FrmEditFileData");
    }

    #region object text change handlers

    /// <summary>
    ///     Handles changes in textboxes and dropdowns. Compares "old" and "new" data and if they mismatch formats Control as
    ///     bold.
    /// </summary>
    /// <param name="sender">The Control whose Text has been changed</param>
    /// <param name="e">Unused</param>
    private void tbx_cbx_Any_TextChanged(object sender,
                                         EventArgs e)
    {
        if (FrmEditFileDataNowLoadingFileData == false)
        {
            Control sndr = (Control)sender;

            DataView dvPreviousText = new(table: DtFileDataToWriteStage2QueuePendingSave);
            dvPreviousText.RowFilter = "fileNameWithOutPath = '" +
                                       lvw_FileListEditImages.SelectedItems[index: 0]
                                           .Text +
                                       "' AND settingId ='" +
                                       sndr.Name.Substring(startIndex: 4) +
                                       "'";
            if (dvPreviousText[recordIndex: 0][property: "settingValue"]
                    .ToString() !=
                sndr.Text)
            {
                string strSndrText = sndr.Text.Replace(oldChar: ',', newChar: '.');
                if (!FrmEditFileDataNowRemovingGeoData && sndr.Parent.Name == "gbx_GPSData" && double.TryParse(s: strSndrText, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double dbl) == false)
                {
                    // don't warn on a single "-" as that could be a lead-up to a negative number
                    if (strSndrText != "-")
                    {
                        MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmEditFileData_WarningLatLongMustBeNumbers"), caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
                        // find a valid number
                        sndr.Text = "0.0";
                    }
                }
                else
                {
                    sndr.Font = new Font(prototype: sndr.Font, newStyle: FontStyle.Bold);
                    // marry up countrycodes and countrynames
                    string sqliteText;
                    if (sndr.Name == "cbx_CountryCode")
                    {
                        sqliteText = HelperStatic.DataReadSQLiteCountryCodesNames(
                            queryWhat: "ISO_3166_1A3",
                            inputVal: sndr.Text,
                            returnWhat: "Country"
                        );
                        if (cbx_Country.Text != sqliteText)
                        {
                            cbx_Country.Text = sqliteText;
                        }
                    }
                    else if (sndr.Name == "cbx_Country")
                    {
                        sqliteText = HelperStatic.DataReadSQLiteCountryCodesNames(
                            queryWhat: "Country",
                            inputVal: sndr.Text,
                            returnWhat: "ISO_3166_1A3"
                        );
                        if (cbx_CountryCode.Text != sqliteText)
                        {
                            cbx_CountryCode.Text = sqliteText;
                        }
                    }

                    HelperStatic.GenericUpdateAddToDataTable(
                        dt: DtFileDataToWriteStage1PreQueue,
                        fileNameWithoutPath: lvw_FileListEditImages.SelectedItems[index: 0]
                            .Text,
                        settingId: sndr.Name.Substring(startIndex: 4),
                        settingValue: sndr.Text
                    );
                }
            }
        }
    }

    #endregion

    /// <summary>
    ///     Obsolete
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void tbx_cbx_Any_Enter(object sender,
                                   EventArgs e)
    {
        //previousText = this.Text;
    }

    #endregion
}