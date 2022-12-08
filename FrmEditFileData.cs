using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TimeZoneConverter;
using static GeoTagNinja.FrmMainApp;

namespace GeoTagNinja;

public partial class FrmEditFileData : Form
{
    private static bool _tzChangedByApi;
    private DateTime _origDateValCreateDate = DateTime.Now;
    private DateTime _origDateValTakenDate = DateTime.Now;

    /// <summary>
    ///     This Form provides an interface for the user to edit various bits of Exif data in images.
    /// </summary>
    public FrmEditFileData()
    {
        Logger.Debug(message: "Starting");

        InitializeComponent();
        Logger.Trace(message: "InitializeComponent OK");
    }

    /// <summary>
    ///     Fires when loading the form. Sets defaults for the listview and makes sure the app is ready to read the file data
    ///     ...w/o marking changes to textboxes (aka when a value changes the textbox formatting will generally turn to bold
    ///     but
    ///     ...when going from "nothing" to "something" that's obviously a change and we don't want that.)
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void FrmEditFileData_Load(object sender,
                                      EventArgs e)
    {
        Logger.Info(message: "Starting");
        Logger.Trace(message: "Defaults Starting");
        _frmEditFileDataNowLoadingFileData = true;
        FrmEditFileDataNowRemovingGeoData = false;

        // Ddeal with Dates
        // TakenDate
        dtp_TakenDate.Enabled = true;
        nud_TakenDateDaysShift.Enabled = false;
        nud_TakenDateHoursShift.Enabled = false;
        nud_TakenDateMinutesShift.Enabled = false;
        nud_TakenDateSecondsShift.Enabled = false;

        dtp_TakenDate.CustomFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern +
                                     " " +
                                     CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;

        // CreateDate
        dtp_CreateDate.Enabled = true;
        nud_CreateDateDaysShift.Enabled = false;
        nud_CreateDateHoursShift.Enabled = false;
        nud_CreateDateMinutesShift.Enabled = false;
        nud_CreateDateSecondsShift.Enabled = false;
        dtp_CreateDate.CustomFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern +
                                      " " +
                                      CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;

        // this just pulls the form's name -- logged inside
        HelperStatic.GenericReturnControlText(cItem: this, senderForm: this);

        // fills the countries box

        foreach (string country in AncillaryListsArrays.GetCountries())
        {
            cbx_Country.Items.Add(item: country);
        }

        // fills the country codes box
        foreach (string countryCode in AncillaryListsArrays.GetCountryCodes())
        {
            cbx_CountryCode.Items.Add(item: countryCode);
        }

        // load TZ-CBX
        foreach (string timezone in AncillaryListsArrays.GetTimeZones())
        {
            cbx_OffsetTimeList.Items.Add(item: timezone);
        }

        Logger.Trace(message: "Defaults OK");

        // this updates the listview itself
        clh_FileName.Width = -2; // auto width col
        if (lvw_FileListEditImages.Items.Count > 0)
        {
            Logger.Trace(message: "ListViewSelect Start");
            Logger.Trace(message: "Items[index: 0].Selected = true");
            lvw_FileListEditImages.Items[index: 0]
                .Selected = true;

            // actually if it's just one file i don't want this to be actively selectable
            if (lvw_FileListEditImages.Items.Count == 1)
            {
                lvw_FileListEditImages.Enabled = false;
            }

            Logger.Trace(message: "Emptying DtFileDataToWriteStage1PreQueue + DtFileDataToWriteStage2QueuePendingSave");
            // empty queue
            DtFileDataToWriteStage1PreQueue.Rows.Clear();
            // also empty the "original data" table
            DtFileDataToWriteStage2QueuePendingSave.Rows.Clear();

            // This actually gets triggered with the SelectedIndexChange above.
            // lvw_EditorFileListImagesGetData();

            // This actually gets triggered with the SelectedIndexChange above.
            //await pbx_imgPreviewPicGenerator(fileNameWithPath: fileNameWithPath);

            Logger.Trace(message: "ListViewSelect Done");
        }

        _frmEditFileDataNowLoadingFileData = false; // techinically this is redundant here
        Logger.Info(message: "Done");
    }

    /// <summary>
    ///     Gets the text values for the Controls in the Form - for labels/buttons etc this is their "language" (eg. English)
    ///     label (e.g. "Latitude")
    ///     ... for textboxes etc this is the value (e.g. "51.002")
    /// </summary>
    private void lvw_EditorFileListImagesGetData()
    {
        Logger.Debug(message: "Starting");

        _frmEditFileDataNowLoadingFileData = true;

        string fileNameWithoutPath = lvw_FileListEditImages.SelectedItems[index: 0]
            .Text;
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        string strSqlDataDT1 = null;
        string strSqlDataDT3 = null;
        string strFLData = null;

        // via https://stackoverflow.com/a/47692754/3968494

        Logger.Trace(message: "Copying Data From dtSqlDataQ");

        List<KeyValuePair<string, string>> lstSqlDataDT1 = HelperStatic.DataReadFilterDataTable(dt: DtFileDataToWriteStage1PreQueue, filePathColumnName: "fileNameWithoutPath", filePathValue: fileNameWithoutPath);

        Logger.Trace(message: "Copying Data From dtSqlDataF");
        List<KeyValuePair<string, string>> lstSqlDataDT3 = HelperStatic.DataReadFilterDataTable(dt: DtFileDataToWriteStage3ReadyToWrite, filePathColumnName: "fileNameWithoutPath", filePathValue: fileNameWithoutPath);

        Logger.Trace(message: "Data Copy Done");

        Logger.Trace(message: "Assinging Labels Start");
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            string exifTag = cItem.Name; // this is for debugging only (particularly here, that is.)
            try
            {
                Logger.Trace(message: "cItem: " + cItem.Name + " (" + cItem.GetType() + ")");
                if (
                    cItem is Label ||
                    cItem is GroupBox ||
                    cItem is Button ||
                    cItem is CheckBox ||
                    cItem is TabPage ||
                    cItem is RadioButton
                )
                {
                    // gets logged inside.
                    HelperStatic.GenericReturnControlText(cItem: cItem, senderForm: this);
                }
                else if (cItem is TextBox || cItem is ComboBox || cItem is DateTimePicker || cItem is NumericUpDown)
                {
                    // reset font to normal
                    cItem.Font = new Font(prototype: cItem.Font, newStyle: FontStyle.Regular);
                    exifTag = cItem.Name.Substring(startIndex: 4);

                    string cItemValStr = "-";

                    Logger.Trace(message: "cItem: " + cItem.Name + " - keyEqualsWhat: " + exifTag + " - Pulling from SQL");
                    strSqlDataDT1 = HelperStatic.DataGetFirstOrDefaultFromKVPList(lstIn: lstSqlDataDT1, keyEqualsWhat: exifTag);
                    strSqlDataDT3 = HelperStatic.DataGetFirstOrDefaultFromKVPList(lstIn: lstSqlDataDT3, keyEqualsWhat: exifTag);

                    // Basically not all Tags exist as CLHs.
                    List<string> lstObjectNamesIn = DtObjectTagNamesIn.Rows.OfType<DataRow>()
                        .Select(selector: dr => dr.Field<string>(columnName: "objectName"))
                        .ToList();

                    if (lstObjectNamesIn.Contains(item: exifTag))
                    {
                        strFLData = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                            .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: "clh_" + exifTag]
                                          .Index]
                            .Text;
                    }

                    if (strSqlDataDT1 != null)
                    {
                        cItemValStr = strSqlDataDT1;
                    }
                    else if (strSqlDataDT3 != null)
                    {
                        cItemValStr = strSqlDataDT3;
                    }
                    else if (strFLData != null)
                    {
                        cItemValStr = strFLData;
                    }
                    // if not in SQL...
                    else
                    {
                        if (cItem is NumericUpDown) // These are the Time-Shifts
                        {
                            Logger.Trace(message: "Not in SQL");
                            cItemValStr = "0";
                        }
                        else if (exifTag == "OffsetTimeList") // I hate you.
                        {
                            Logger.Trace(message: "Not in SQL");
                            // blank on purpose
                        }
                    }

                    // if empty...
                    if (cItemValStr == "-" || cItemValStr == "" || cItemValStr is null)
                    {
                        // okay for gbx_*Date && !NUD -> those are DateTimePickers. If they are NULL then there is either no TakenDate or no CreateDate so the actual controls will have to be disabled.
                        if (cItem.Parent.Name.StartsWith(value: "gbx_") && cItem.Parent.Name.EndsWith(value: "Date") && cItem is not NumericUpDown)
                        {
                            if (cItem.Parent.Name == "gbx_TakenDate")
                            {
                                IEnumerable<Control> cGbx_TakenDate = helperNonstatic.GetAllControls(control: gbx_TakenDate);
                                foreach (Control cItemGbx_TakenDate in cGbx_TakenDate)
                                {
                                    if (cItemGbx_TakenDate != btn_InsertTakenDate)
                                    {
                                        cItemGbx_TakenDate.Enabled = false;
                                        btn_InsertFromTakenDate.Enabled = false;
                                    }
                                }
                            }

                            else if (cItem.Parent.Name == "gbx_CreateDate")
                            {
                                IEnumerable<Control> cGbx_CreateDate = helperNonstatic.GetAllControls(control: gbx_CreateDate);
                                foreach (Control cItemGbx_CrateDate in cGbx_CreateDate)
                                {
                                    if (cItemGbx_CrateDate != btn_InsertCreateDate)
                                    {
                                        cItemGbx_CrateDate.Enabled = false;
                                        btn_InsertFromTakenDate.Enabled = false;
                                    }
                                }
                            }
                        }
                        // if it's none of the above then make the cItem be just blank.
                        else if (exifTag == "OffsetTimeList") // I hate you.
                        {
                            // blank on purpose
                        }
                        else
                        {
                            cItem.Text = "";
                        }
                    }
                    // if has value...
                    else
                    {
                        // this is related to storing the default DateTimes for TakenDate and CreateDate
                        if (cItem == dtp_TakenDate)
                        {
                            btn_InsertTakenDate.Enabled = false;
                            List<KeyValuePair<string, string>> lstSqlDataDTTakenDate =
                                HelperStatic.DataReadFilterDataTable(dt: DtOriginalTakenDate,
                                                                     filePathColumnName: "fileNameWithoutPath",
                                                                     filePathValue: fileNameWithoutPath);

                            if (lstSqlDataDTTakenDate.Count > 0)
                            {
                                _origDateValTakenDate = Convert.ToDateTime(value: HelperStatic.DataGetFirstOrDefaultFromKVPList(lstIn: lstSqlDataDTTakenDate, keyEqualsWhat: "originalTakenDate"));
                            }
                        }
                        else if (cItem == dtp_CreateDate)
                        {
                            btn_InsertCreateDate.Enabled = false;
                            List<KeyValuePair<string, string>> lstSqlDataDTCreateDate =
                                HelperStatic.DataReadFilterDataTable(dt: DtOriginalCreateDate,
                                                                     filePathColumnName: "fileNameWithoutPath",
                                                                     filePathValue: fileNameWithoutPath);

                            if (lstSqlDataDTCreateDate.Count > 0)
                            {
                                _origDateValCreateDate = Convert.ToDateTime(value: HelperStatic.DataGetFirstOrDefaultFromKVPList(lstIn: lstSqlDataDTCreateDate, keyEqualsWhat: "originalCreateDate"));
                            }
                        }

                        Logger.Trace(message: "cItem: " + cItem.Name + " - Adding to DtFileDataToWriteStage2QueuePendingSave");
                        // stick into sql ("pending save") - this is to see if the data has changed later.
                        if (cItem is not NumericUpDown cItemNumericUpDown)
                        {
                            HelperStatic.GenericUpdateAddToDataTable(
                                dt: DtFileDataToWriteStage2QueuePendingSave,
                                fileNameWithoutPath: fileNameWithoutPath,
                                settingId: cItem.Name.Substring(startIndex: 4),
                                settingValue: cItemValStr);
                            cItem.Text = cItemValStr;
                        }
                        else
                        {
                            HelperStatic.GenericUpdateAddToDataTable(
                                dt: DtFileDataToWriteStage2QueuePendingSave,
                                fileNameWithoutPath: fileNameWithoutPath,
                                settingId: cItemNumericUpDown.Name.Substring(startIndex: 4),
                                settingValue: cItemValStr);

                            cItemNumericUpDown.Value = int.Parse(cItemValStr);
                        }

                        if (cItem is TextBox txt)
                        {
                            Logger.Trace(message: "cItem: " + cItem.Name + " - Updating TextBox");
                            if (strSqlDataDT1 != null || strSqlDataDT3 != null)
                            {
                                txt.Font = new Font(prototype: txt.Font, newStyle: FontStyle.Bold);
                            }
                        }
                        else if (cItem is ComboBox cmb)
                        {
                            Logger.Trace(message: "cItem: " + cItem.Name + " - Updating ComboBox");
                            if (strSqlDataDT1 != null || strSqlDataDT3 != null)
                            {
                                cmb.Font = new Font(prototype: cmb.Font, newStyle: FontStyle.Bold);
                            }

                            if (cItem.Name == "cbx_CountryCode" || cItem.Name == "cbx_Country")
                            {
                                // okay this is derp but i don't have a particularly better idea at this point
                                // so basically countrycode and country need to be loaded first so we'll see how they are above...
                                // then have another go at them here.
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
                                else if (cItem.Name == "cbx_OffsetTime")
                                { }
                            }
                        }
                        else if (cItem is DateTimePicker dtp)
                        {
                            Logger.Trace(message: "cItem: " + cItem.Name + " - Updating DateTimePicker");
                            if (strSqlDataDT1 != null || strSqlDataDT3 != null)
                            {
                                dtp.Font = new Font(prototype: dtp.Font, newStyle: FontStyle.Bold);
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }

            Logger.Trace(message: "cItem: " + cItem.Name + " (" + cItem.GetType() + ") - Done");
        }

        // done load
        Logger.Debug(message: "Done");
        _frmEditFileDataNowLoadingFileData = false;
    }


    /// <summary>
    ///     Attempts to generate preview image for the image that was clicked on.
    /// </summary>
    /// <param name="fileNameWithPath">Name (path) of the file that was clicked on.</param>
    /// <returns></returns>
    private static async Task pbx_imgPreviewPicGenerator(string fileNameWithPath)
    {
        Logger.Debug(message: "Starting");

        string fileName = Path.GetFileName(path: fileNameWithPath);
        // via https://stackoverflow.com/a/8701748/3968494
        FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
        Image img = null;
        if (frmEditFileDataInstance != null)
        {
            try
            {
                Logger.Trace(message: "Trying Bitmap");
                using Bitmap bmpTemp = new(filename: fileNameWithPath);
                img = new Bitmap(original: bmpTemp);
                frmEditFileDataInstance.pbx_imagePreview.Image = img;
            }
            catch
            {
                Logger.Trace(message: "Bitmap failed");
            }

            if (img == null)
            {
                Logger.Trace(message: "Img doesn't exist.");
                string generatedFileName = Path.Combine(path1: UserDataFolderPath, path2: fileName + ".jpg");
                // don't run the thing again if file has already been generated
                if (!File.Exists(path: generatedFileName))
                {
                    await HelperStatic.ExifGetImagePreviews(fileNameWithoutPath: fileNameWithPath);
                }

                //sometimes the file doesn't get created. (ie exiftool may fail to extract a preview.)
                if (!File.Exists(path: generatedFileName))
                {
                    Logger.Trace(message: "Exiftool Failed to extract file");
                    try
                    {
                        Logger.Trace(message: "Trying bitmap again");
                        using Bitmap bmpTemp = new(filename: generatedFileName);
                        img = new Bitmap(original: bmpTemp);
                        frmEditFileDataInstance.pbx_imagePreview.Image = img;
                    }
                    catch
                    {
                        Logger.Trace(message: "Bitmap failed");
                    }
                }
            }
        }

        Logger.Debug(message: "Done");
    }

    #region Variables

    private static bool _frmEditFileDataNowLoadingFileData;
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
        DataTable dtToponomy = new();
        DataTable dtAltitude = new();
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        //reset this just in case.
        HelperStatic.SApiOkay = true;
        switch (((Button)sender).Name)
        {
            case "btn_getFromWeb_Toponomy":
                getFromWeb_Toponomy(fileNameWithoutPath: "");
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
                        getFromWeb_Toponomy(fileNameWithoutPath: "");
                        // no need to write back to sql because it's done automatically on textboxChange
                    }
                    else
                    {
                        getFromWeb_Toponomy(fileNameWithoutPath: lvi.Text);
                        // get lat/long from main listview
                        lvi.ForeColor = Color.Red;
                    }
                }

                break;
            case "btn_getFromWeb_Altitude":
                getFromWeb_Altitude(fileNameWithoutPath: "");

                break;
            case "btn_getAllFromWeb_Altitude":
                foreach (ListViewItem lvi in lvw_FileListEditImages.Items)
                {
                    string fileName = lvi.Text;
                    // for "this" file do the same as "normal" getfromweb
                    if (fileName ==
                        lvw_FileListEditImages.SelectedItems[index: 0]
                            .Text)
                    {
                        getFromWeb_Altitude(fileNameWithoutPath: "");

                        // no need to write back to sql because it's done automatically on textboxChange
                    }
                    else
                    {
                        getFromWeb_Altitude(fileNameWithoutPath: lvi.Text);
                        // get lat/long from main listview
                        lvi.ForeColor = Color.Red;
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
    ///     Pulls data from the various APIs and fills up the listView and fills the TextBoxes and/or SQLite.
    /// </summary>
    /// <param name="fileNameWithoutPath">Blank if used as "pull one file" otherwise the name of the file w/o Path</param>
    private void getFromWeb_Toponomy(string fileNameWithoutPath = "")
    {
        DataTable dtToponomy;
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        double parsedLat;
        double parsedLng;
        DateTime createDate = default; // can't leave it null because it's updated in various IFs and C# perceives it as uninitialised.

        string strGpsLatitude;
        string strGpsLongitude;

        dtToponomy = new DataTable();

        // this is "current file"
        if (fileNameWithoutPath == "")
        {
            strGpsLatitude = tbx_GPSLatitude.Text.ToString(provider: CultureInfo.InvariantCulture);
            strGpsLongitude = tbx_GPSLongitude.Text.ToString(provider: CultureInfo.InvariantCulture);

            if (double.TryParse(s: strGpsLatitude,
                                style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture,
                                result: out parsedLat) &&
                double.TryParse(s: strGpsLongitude,
                                style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture,
                                result: out parsedLng))
            {
                dtToponomy = HelperStatic.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
            }
        }

        // this is all the other files
        else
        {
            strGpsLatitude = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: "clh_GPSLatitude"]
                              .Index]
                .Text.ToString(provider: CultureInfo.InvariantCulture);
            strGpsLongitude = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: "clh_GPSLongitude"]
                              .Index]
                .Text.ToString(provider: CultureInfo.InvariantCulture);
            string strCreateDate = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: "clh_CreateDate"]
                              .Index]
                .Text.ToString(provider: CultureInfo.InvariantCulture);
            bool _ = DateTime.TryParse(s: strCreateDate.ToString(provider: CultureInfo.InvariantCulture), result: out createDate);

            if (double.TryParse(s: strGpsLatitude,
                                style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture,
                                result: out parsedLat) &&
                double.TryParse(s: strGpsLongitude,
                                style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture,
                                result: out parsedLng))
            {
                dtToponomy = HelperStatic.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);
            }
        }

        // Pull the data from the web regardless.
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

        string TZ = dtToponomy.Rows[index: 0][columnName: "timeZoneId"]
            .ToString();

        if (fileNameWithoutPath == "")
        {
            const int tzStartInt = 18;

            bool _ = DateTime.TryParse(s: tbx_CreateDate.Text.ToString(provider: CultureInfo.InvariantCulture), result: out createDate);

            // cbx_OffsetTimeList.FindString(TZ, 18) doesn't seem to work so....
            for (int i = 0; i <= cbx_OffsetTimeList.Items.Count; i++)
            {
                string cbxText = cbx_OffsetTimeList.Items[index: i]
                    .ToString();
                if (cbxText.Length >= tzStartInt)
                {
                    if (cbxText
                        .Substring(startIndex: tzStartInt)
                        .Contains(value: TZ))
                    {
                        // this controls the logic that the ckb_UseDST should not be re-parsed again manually on the Change event that would otherwise fire.
                        _tzChangedByApi = true;
                        cbx_OffsetTimeList.SelectedIndex = i;
                        try
                        {
                            if (TZ != null)
                            {
                                string IANATZ = TZConvert.IanaToWindows(ianaTimeZoneName: TZ);
                                string TZOffset;
                                TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(id: IANATZ);
                                ckb_UseDST.Checked = tst.IsDaylightSavingTime(dateTime: createDate);
                                TZOffset = tst.GetUtcOffset(dateTime: createDate)
                                    .ToString()
                                    .Substring(startIndex: 0, length: tst.GetUtcOffset(dateTime: createDate)
                                                                          .ToString()
                                                                          .Length -
                                                                      3);
                                if (!TZOffset.StartsWith(value: "-"))
                                {
                                    toponomyOverwrites.Add(item: ("OffsetTime", "+" + TZOffset));
                                }
                                else
                                {
                                    toponomyOverwrites.Add(item: ("OffsetTime", TZOffset));
                                }
                            }
                        }
                        catch
                        {
                            // add a zero
                            toponomyOverwrites.Add(item: ("OffsetTime", "+00:00"));
                        }

                        _tzChangedByApi = false;
                        break;
                    }
                }
            }

            // send it back to the Form + SQL
            foreach ((string toponomyOverwriteName, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
            {
                switch (toponomyDetail.toponomyOverwriteName)
                {
                    case "CountryCode":
                        cbx_CountryCode.Text = toponomyDetail.toponomyOverwriteVal;
                        break;
                    case "Country":
                        cbx_Country.Text = toponomyDetail.toponomyOverwriteVal;
                        break;
                    case "City":
                        tbx_City.Text = toponomyDetail.toponomyOverwriteVal;
                        break;
                    case "State":
                        tbx_State.Text = toponomyDetail.toponomyOverwriteVal;
                        break;
                    case "Sub_location":
                        tbx_Sub_location.Text = toponomyDetail.toponomyOverwriteVal;
                        break;
                    case "OffsetTime":
                        tbx_OffsetTime.Text = toponomyDetail.toponomyOverwriteVal;
                        break;
                }
            }
        }
        else
        {
            try
            {
                if (TZ != null)
                {
                    string IANATZ = TZConvert.IanaToWindows(ianaTimeZoneName: TZ);
                    string TZOffset;
                    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(id: IANATZ);

                    TZOffset = tst.GetUtcOffset(dateTime: createDate)
                        .ToString()
                        .Substring(startIndex: 0, length: tst.GetUtcOffset(dateTime: createDate)
                                                              .ToString()
                                                              .Length -
                                                          3);
                    if (!TZOffset.StartsWith(value: "-"))
                    {
                        toponomyOverwrites.Add(item: ("OffsetTime", "+" + TZOffset));
                    }
                    else
                    {
                        toponomyOverwrites.Add(item: ("OffsetTime", TZOffset));
                    }
                }
            }
            catch
            {
                // add a zero
                toponomyOverwrites.Add(item: ("OffsetTime", "+00:00"));
            }

            foreach ((string toponomyOverwriteName, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
            {
                HelperStatic.GenericUpdateAddToDataTable(
                    dt: DtFileDataToWriteStage1PreQueue,
                    fileNameWithoutPath: fileNameWithoutPath,
                    settingId: toponomyDetail.toponomyOverwriteName,
                    settingValue: toponomyDetail.toponomyOverwriteVal
                );
            }
        }
    }

    /// <summary>
    ///     Pulls data from the various APIs and fills up the listView and fills the TextBoxes and/or SQLite.
    /// </summary>
    /// <param name="fileNameWithoutPath">Blank if used as "pull one file" otherwise the name of the file w/o Path</param>
    private void getFromWeb_Altitude(string fileNameWithoutPath = "")
    {
        DataTable dtAltitude;
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        double parsedLat;
        double parsedLng;
        string strGpsLatitude;
        string strGpsLongitude;

        if (fileNameWithoutPath == "")
        {
            strGpsLatitude = tbx_GPSLatitude.Text.ToString(provider: CultureInfo.InvariantCulture);
            strGpsLongitude = tbx_GPSLongitude.Text.ToString(provider: CultureInfo.InvariantCulture);

            if (double.TryParse(s: strGpsLatitude,
                                style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture,
                                result: out parsedLat) &&
                double.TryParse(s: strGpsLongitude,
                                style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture,
                                result: out parsedLng))
            {
                dtAltitude = HelperStatic.DTFromAPIExifGetAltitudeFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude);

                if (dtAltitude.Rows.Count > 0)
                {
                    tbx_GPSAltitude.Text = dtAltitude.Rows[index: 0][columnName: "Altitude"]
                        .ToString();
                }
                // no need to write back to sql because it's done automatically on textboxChange
            }
            else
            {
                foreach (ListViewItem lvi in lvw_FileListEditImages.Items)
                {
                    string fileName = lvi.Text;

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
        }
    }

    /// <summary>
    ///     Allows to insert a TakenDate value if there isn't one already.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_InsertTakenDate_Click(object sender,
                                           EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> cGbx_TakenDate = helperNonstatic.GetAllControls(control: gbx_TakenDate);
        foreach (Control cItemGbx_TakenDate in cGbx_TakenDate)
        {
            if (cItemGbx_TakenDate != btn_InsertTakenDate)
            {
                cItemGbx_TakenDate.Enabled = Enabled;

                // set font to bold for these two - that will get picked up later.
                if (cItemGbx_TakenDate is DateTimePicker dtp)
                {
                    dtp.Font = new Font(prototype: dtp.Font, newStyle: FontStyle.Bold);
                    HelperStatic.GenericUpdateAddToDataTable(
                        dt: DtFileDataToWriteStage1PreQueue,
                        fileNameWithoutPath: lvw_FileListEditImages.SelectedItems[index: 0]
                            .Text,
                        settingId: dtp.Name.Substring(startIndex: 4),
                        settingValue: dtp.Text
                    );
                }
            }
        }

        btn_InsertTakenDate.Enabled = false;
    }

    /// <summary>
    ///     Handles the keyboard interactions (move up/down)
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void lvw_FileListEditImages_SelectedIndexChanged(object sender,
                                                                   EventArgs e)
    {
        Logger.Debug(message: "Starting");
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
                Logger.Debug(message: "File disappeared: " + fileNameWithPath);
                MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmEditFileData_WarningFileDisappeared"), caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
            }
        }

        Logger.Debug(message: "Done");
    }

    /// <summary>
    ///     Handles when user clicks on the OK button. Moves the data from holding table 1 to 3 and updates the main listview.
    ///     Also does some magic relating to time shifting - this is detailed further inside the comments but the TLDR is that
    ///     for files that just had their Dates explicitly updated this needs to be handled differently from those where Shift
    ///     has been actioned so that they don't become duplicate-effect.
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
            // make a list of files to actually update
            DataTable dtDistinctFileNames = DtFileDataToWriteStage1PreQueue.DefaultView.ToTable(distinct: true, "fileNameWithoutPath");
            foreach (DataRow drFileName in dtDistinctFileNames.Rows)
            {
                string fileNameWithoutPath = drFileName[columnIndex: 0]
                    .ToString();

                DataTable dtFileWriteQueue = null;
                try
                {
                    dtFileWriteQueue = DtFileDataToWriteStage1PreQueue.Select(filterExpression: "fileNameWithoutPath = '" + fileNameWithoutPath + "'")
                        .CopyToDataTable();
                }
                catch
                {
                    dtFileWriteQueue = null;
                }

                // this shouldn't ever be zero...
                if (dtFileWriteQueue != null && dtFileWriteQueue.Rows.Count > 0)
                {
                    //---
                    // TimeShifts go separately. -- Also while this could be done in one step it's easier for now if we split Taken and Create
                    // TakenDate
                    DataTable dtDateTakenDateShifted = null;
                    try
                    {
                        dtDateTakenDateShifted = dtFileWriteQueue.Select(filterExpression: "settingId LIKE 'Taken%' AND settingId LIKE '%Shift'")
                            .CopyToDataTable();
                    }
                    catch
                    {
                        dtDateTakenDateShifted = null;
                    }

                    if (dtDateTakenDateShifted != null && dtDateTakenDateShifted.Rows.Count > 0)
                    {
                        int shiftedDays = 0;
                        int shiftedHours = 0;
                        int shiftedMinutes = 0;
                        int shiftedSeconds = 0;
                        List<(string settingId, string settingValue)> lstDr3RowsToAdd = new();

                        foreach (DataRow drDateTakenDateShifted in dtDateTakenDateShifted.Rows)
                        {
                            string settingId = drDateTakenDateShifted[columnName: "settingId"]
                                .ToString();
                            string settingValue = drDateTakenDateShifted[columnName: "settingValue"]
                                .ToString();
                            switch (settingId)
                            {
                                case "TakenDateDaysShift":
                                    shiftedDays = int.Parse(s: settingValue);
                                    break;
                                case "TakenDateHoursShift":
                                    shiftedHours = int.Parse(s: settingValue);
                                    break;
                                case "TakenDateMinutesShift":
                                    shiftedMinutes = int.Parse(s: settingValue);
                                    break;
                                case "TakenDateSecondsShift":
                                    shiftedSeconds = int.Parse(s: settingValue);
                                    break;
                            }

                            if (settingValue != "0" && settingValue != null)
                            {
                                lstDr3RowsToAdd.Add(item: (settingId, settingValue));
                            }
                        }

                        foreach ((string settingId, string settingValue) lstDr3RowToAdd in lstDr3RowsToAdd)
                        {
                            HelperStatic.GenericUpdateAddToDataTable(dt: DtFileDataToWriteStage3ReadyToWrite,
                                                                     fileNameWithoutPath: fileNameWithoutPath,
                                                                     settingId: lstDr3RowToAdd.settingId,
                                                                     settingValue: lstDr3RowToAdd.settingValue);
                        }

                        int totalShiftedSeconds = shiftedSeconds +
                                                  shiftedMinutes * 60 +
                                                  shiftedHours * 60 * 60 +
                                                  shiftedDays * 60 * 60 * 24;

                        DataTable dtTakenDate = null;

                        try
                        {
                            dtTakenDate = DtOriginalTakenDate.Select(filterExpression: "fileNameWithoutPath = '" + fileNameWithoutPath + "'")
                                .CopyToDataTable();
                        }
                        catch
                        {
                            dtTakenDate = null;
                        }

                        if (dtTakenDate != null && dtTakenDate.Rows.Count > 0)
                        {
                            DateTime originalTakenDateTime = Convert.ToDateTime(value: dtTakenDate.Rows[index: 0][columnName: "settingValue"]
                                                                                    .ToString());

                            DateTime modifiedTakenDateTime = originalTakenDateTime.AddSeconds(value: totalShiftedSeconds);

                            HelperStatic.GenericUpdateAddToDataTable(dt: DtFileDataToWriteStage3ReadyToWrite,
                                                                     fileNameWithoutPath: fileNameWithoutPath,
                                                                     settingId: "TakenDate",
                                                                     settingValue: modifiedTakenDateTime
                                                                         .ToString(provider: CultureInfo
                                                                                       .CurrentUICulture));
                        }
                    }

                    // CreateDate
                    DataTable dtDateCreateDateShifted = null;
                    try
                    {
                        dtDateCreateDateShifted = dtFileWriteQueue.Select(filterExpression: "settingId LIKE 'Create%' AND settingId LIKE '%Shift'")
                            .CopyToDataTable();
                    }
                    catch
                    {
                        dtDateCreateDateShifted = null;
                    }

                    if (dtDateCreateDateShifted != null && dtDateCreateDateShifted.Rows.Count > 0)
                    {
                        int shiftedDays = 0;
                        int shiftedHours = 0;
                        int shiftedMinutes = 0;
                        int shiftedSeconds = 0;
                        List<(string settingId, string settingValue)> lstDr3RowsToAdd = new();

                        foreach (DataRow drDateCreateDateShifted in dtDateCreateDateShifted.Rows)
                        {
                            string settingId = drDateCreateDateShifted[columnName: "settingId"]
                                .ToString();
                            string settingValue = drDateCreateDateShifted[columnName: "settingValue"]
                                .ToString();
                            switch (settingId)
                            {
                                case "CreateDateDaysShift":
                                    shiftedDays = int.Parse(s: settingValue);
                                    break;
                                case "CreateDateHoursShift":
                                    shiftedHours = int.Parse(s: settingValue);
                                    break;
                                case "CreateDateMinutesShift":
                                    shiftedMinutes = int.Parse(s: settingValue);
                                    break;
                                case "CreateDateSecondsShift":
                                    shiftedSeconds = int.Parse(s: settingValue);
                                    break;
                            }

                            if (settingValue != "0" && settingValue != null)
                            {
                                lstDr3RowsToAdd.Add(item: (settingId, settingValue));
                            }
                        }

                        foreach ((string settingId, string settingValue) lstDr3RowToAdd in lstDr3RowsToAdd)
                        {
                            HelperStatic.GenericUpdateAddToDataTable(dt: DtFileDataToWriteStage3ReadyToWrite,
                                                                     fileNameWithoutPath: fileNameWithoutPath,
                                                                     settingId: lstDr3RowToAdd.settingId,
                                                                     settingValue: lstDr3RowToAdd.settingValue);
                        }

                        int totalShiftedSeconds = shiftedSeconds +
                                                  shiftedMinutes * 60 +
                                                  shiftedHours * 60 * 60 +
                                                  shiftedDays * 60 * 60 * 24;

                        DataTable dtCreateDate = null;
                        try
                        {
                            dtCreateDate = DtOriginalCreateDate.Select(filterExpression: "fileNameWithoutPath = '" + fileNameWithoutPath + "'")
                                .CopyToDataTable();
                        }
                        catch
                        {
                            dtCreateDate = null;
                        }

                        if (dtCreateDate != null && dtCreateDate.Rows.Count > 0)
                        {
                            DateTime originalCreateDateTime = Convert.ToDateTime(value: dtCreateDate.Rows[index: 0][columnName: "settingValue"]
                                                                                     .ToString());

                            DateTime modifiedCreateDateTime = originalCreateDateTime.AddSeconds(value: totalShiftedSeconds);

                            HelperStatic.GenericUpdateAddToDataTable(dt: DtFileDataToWriteStage3ReadyToWrite,
                                                                     fileNameWithoutPath: fileNameWithoutPath,
                                                                     settingId: "CreateDate",
                                                                     settingValue: modifiedCreateDateTime
                                                                         .ToString(provider: CultureInfo
                                                                                       .CurrentUICulture));
                        }
                    }

                    //---
                    // transfer data from 1 to 3
                    DataTable dtNotShifted = null;
                    try
                    {
                        dtNotShifted = dtFileWriteQueue.Select(filterExpression: "settingId NOT LIKE '%Shift'")
                            .CopyToDataTable();
                    }
                    catch
                    {
                        dtNotShifted = null;
                    }

                    if (dtNotShifted != null)
                    {
                        foreach (DataRow drS1 in dtNotShifted.Rows)
                        {
                            string settingId = drS1[columnName: "settingId"]
                                .ToString();
                            string settingValue = drS1[columnName: "settingValue"]
                                .ToString();

                            HelperStatic.GenericUpdateAddToDataTable(dt: DtFileDataToWriteStage3ReadyToWrite,
                                                                     fileNameWithoutPath: fileNameWithoutPath,
                                                                     settingId: settingId,
                                                                     settingValue: settingValue);
                        }
                    }

                    // Also logically if user changed (TakenDate || CreateDate) && ! *Shift* then stick those values into FrmMainApp.DtOriginalTakenDate && FrmMainApp.DtOriginalCreateDate
                    // Basically they just pressed OK to queue it up to write-queue. If they then amend stuff this will be their basis of reality and if they cancel (by refreshing), the Dt is lost anyway.
                    // TakenDate
                    DataRow[] drArrDateChanged = { };
                    int dateChangeCount = 0;
                    try
                    {
                        drArrDateChanged = dtFileWriteQueue.Select(filterExpression: "settingId = 'TakenDate'");
                        dateChangeCount = drArrDateChanged.Length;
                    }
                    catch
                    {
                        // nothing
                    }

                    DataRow[] drArrDateShifted;
                    int shiftedChangeCount = 0;
                    try
                    {
                        drArrDateShifted = dtFileWriteQueue.Select(filterExpression: "settingId LIKE 'TakenDate%' AND settingId LIKE '%Shift'");
                        shiftedChangeCount = drArrDateShifted.Length;
                    }
                    catch
                    {
                        // nothing
                    }

                    if (dateChangeCount == 1 && shiftedChangeCount == 0)
                    {
                        DataRow drDateChanged = drArrDateChanged[0];
                        DtOriginalTakenDate.Rows.Cast<DataRow>()
                            .Where(
                                predicate: r => r.ItemArray[0]
                                                    .ToString() ==
                                                fileNameWithoutPath)
                            .ToList()
                            .ForEach(action: r => r.Delete());

                        DtOriginalTakenDate.AcceptChanges();
                        DataRow drDateChange = DtOriginalTakenDate.NewRow();
                        drDateChange[columnName: "fileNameWithoutPath"] = fileNameWithoutPath;
                        drDateChange[columnName: "settingId"] = "originalTakenDate";
                        drDateChange[columnName: "settingValue"] = drDateChanged[columnName: "settingValue"];
                        DtOriginalTakenDate.Rows.Add(row: drDateChange);
                        DtOriginalTakenDate.AcceptChanges();

                        // Logically then clear any past-shifts too
                        DtFileDataToWriteStage3ReadyToWrite.Rows.Cast<DataRow>()
                            .Where(
                                predicate: r => r.ItemArray[0]
                                                    .ToString() ==
                                                fileNameWithoutPath &&
                                                r.ItemArray[1]
                                                    .ToString()
                                                    .StartsWith(value: "Taken") &&
                                                r.ItemArray[1]
                                                    .ToString()
                                                    .EndsWith(value: "Shift"))
                            .ToList()
                            .ForEach(action: r => r.Delete());

                        DtFileDataToWriteStage3ReadyToWrite.AcceptChanges();
                    }

                    // CreateDate
                    try
                    {
                        drArrDateChanged = dtFileWriteQueue.Select(filterExpression: "settingId = 'CreateDate'");
                        dateChangeCount = drArrDateChanged.Length;
                    }
                    catch
                    {
                        // nothing
                    }

                    try
                    {
                        drArrDateShifted = dtFileWriteQueue.Select(filterExpression: "settingId LIKE 'CreateDate%' AND settingId LIKE '%Shift'");
                        shiftedChangeCount = drArrDateShifted.Length;
                    }
                    catch
                    {
                        // nothing
                    }

                    if (dateChangeCount == 1 && shiftedChangeCount == 0)
                    {
                        DataRow drDateChanged = drArrDateChanged[0];
                        DtOriginalCreateDate.Rows.Cast<DataRow>()
                            .Where(
                                predicate: r => r.ItemArray[0]
                                                    .ToString() ==
                                                fileNameWithoutPath)
                            .ToList()
                            .ForEach(action: r => r.Delete());

                        DtOriginalCreateDate.AcceptChanges();
                        DataRow drDateChange = DtOriginalCreateDate.NewRow();
                        drDateChange[columnName: "fileNameWithoutPath"] = fileNameWithoutPath;
                        drDateChange[columnName: "settingId"] = "originalCreateDate";
                        drDateChange[columnName: "settingValue"] = drDateChanged[columnName: "settingValue"];
                        DtOriginalCreateDate.Rows.Add(row: drDateChange);
                        DtOriginalCreateDate.AcceptChanges();

                        // Logically then clear any past-shifts too
                        DtFileDataToWriteStage3ReadyToWrite.Rows.Cast<DataRow>()
                            .Where(
                                predicate: r => r.ItemArray[0]
                                                    .ToString() ==
                                                fileNameWithoutPath &&
                                                r.ItemArray[1]
                                                    .ToString()
                                                    .StartsWith(value: "Create") &&
                                                r.ItemArray[1]
                                                    .ToString()
                                                    .EndsWith(value: "Shift"))
                            .ToList()
                            .ForEach(action: r => r.Delete());

                        DtFileDataToWriteStage3ReadyToWrite.AcceptChanges();
                    }

                    ListViewItem lvi = null;
                    try
                    {
                        lvi = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath);
                    }
                    catch
                    {
                        // shouldn't happen
                    }

                    if (lvi != null)
                    {
                        // update listview w new data
                        HelperStatic.FileListBeingUpdated = true;
                        await HelperStatic.LwvUpdateRowFromDTWriteStage3ReadyToWrite(lvi: lvi);
                        HelperStatic.FileListBeingUpdated = false;
                    }
                }
            }

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
    private void tbx_cbx_dtp_nud_Any_TextChanged(object sender,
                                                 EventArgs e)
    {
        if (!_frmEditFileDataNowLoadingFileData)
        {
            string senderName;
            string parentName;
            DataTable dtPreviousText = null;
            string previousText = "";
            string newText = "";
            string exifTag = null;

            if (sender is NumericUpDown)
            {
                // this is a f...ing nightmare because NUDs have Values but everything else has Text
                NumericUpDown sndr = (NumericUpDown)sender;
                senderName = sndr.Name;
                parentName = sndr.Parent.Name;
                exifTag = sndr.Name.Substring(startIndex: 4);
                try
                {
                    dtPreviousText = DtFileDataToWriteStage2QueuePendingSave.Select(filterExpression: "fileNameWithoutPath = '" +
                                                                                                      lvw_FileListEditImages.SelectedItems[index: 0]
                                                                                                          .Text +
                                                                                                      "' AND settingId = '" +
                                                                                                      exifTag +
                                                                                                      "'")
                        .CopyToDataTable();
                }
                catch
                {
                    dtPreviousText = null;
                }

                if (dtPreviousText != null && dtPreviousText.Rows.Count > 0)
                {
                    previousText = dtPreviousText.Rows[index: 0][columnName: "settingValue"]
                        .ToString();
                }

                newText = sndr.Value.ToString(provider: CultureInfo.InvariantCulture);
            }
            else

            {
                Control sndr = (Control)sender;
                senderName = sndr.Name;
                parentName = sndr.Parent.Name;
                exifTag = sndr.Name.Substring(startIndex: 4);
                try
                {
                    dtPreviousText = DtFileDataToWriteStage2QueuePendingSave.Select(filterExpression: "fileNameWithoutPath = '" +
                                                                                                      lvw_FileListEditImages.SelectedItems[index: 0]
                                                                                                          .Text +
                                                                                                      "' AND settingId = '" +
                                                                                                      exifTag +
                                                                                                      "'")
                        .CopyToDataTable();
                }
                catch
                {
                    dtPreviousText = null;
                }

                if (dtPreviousText != null && dtPreviousText.Rows.Count > 0)
                {
                    previousText = dtPreviousText.Rows[index: 0][columnName: "settingValue"]
                        .ToString();
                }

                newText = sndr.Text;
            }

            // I'll paste it here for good measure.
            // Time Zones are left as blank on open regardless of what the stored value is. There is no Exif Tag for TZ but only "Offset", which is something like "+01:00".
            // As there is no indication for neither TZ nor DST per se I can't ascertain that  "+01:00" was in fact say BST rather than CET, one being DST the other not.
            // Either adjust manually or pull from web - the combination of coordinates + createDate would decisively inform the program of the real TZ value.
            // The value in the read-only textbox will be saved in the file.
            // ...That also means that cbx_OffsetTimeList is a "bit special" (aren't we all...) so it needs to be derailed rather than sent back to the various datatables
            if (senderName == "cbx_OffsetTimeList" && !_tzChangedByApi)
            {
                GetTimeZoneOffset();
            }
            else if (senderName == "tbx_OffsetTime")
            {
                // ignore
            }

            if (previousText != newText)
            {
                string strSndrText = newText.Replace(oldChar: ',', newChar: '.');
                if (!FrmEditFileDataNowRemovingGeoData && parentName == "gbx_GPSData" && double.TryParse(s: strSndrText, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double dbl) == false)
                {
                    // don't warn on a single "-" as that could be a lead-up to a negative number
                    if (strSndrText != "-")
                    {
                        MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmEditFileData_WarningLatLongMustBeNumbers"), caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
                        // find a valid number
                        // gpsdata has no NUDs
                        Control sndr = (Control)sender;
                        sndr.Text = "0.0";
                    }
                }
                // yayy for pointless redundancy
                else if (sender is NumericUpDown numericUpDown)
                {
                    // this seems not to be working for some reason.
                    numericUpDown.Font = new Font(prototype: numericUpDown.Font, newStyle: FontStyle.Bold);

                    HelperStatic.GenericUpdateAddToDataTable(
                        dt: DtFileDataToWriteStage1PreQueue,
                        fileNameWithoutPath: lvw_FileListEditImages.SelectedItems[index: 0]
                            .Text,
                        settingId: exifTag,
                        settingValue: numericUpDown.Value.ToString(provider: CultureInfo.InvariantCulture)
                    );

                    // adjust createDate and/or takenDate

                    if (senderName.Contains(value: "TakenDate"))
                    {
                        double shiftTakenDateSeconds =
                            (double)nud_TakenDateSecondsShift.Value +
                            (double)(nud_TakenDateMinutesShift.Value * 60) +
                            (double)(nud_TakenDateHoursShift.Value * 60 * 60) +
                            (double)(nud_TakenDateDaysShift.Value * 60 * 60 * 24);

                        dtp_TakenDate.Value = _origDateValTakenDate.AddSeconds(value: shiftTakenDateSeconds);
                    }
                    else if (senderName.Contains(value: "CreateDate"))
                    {
                        double shiftCreateDateSeconds =
                            (double)nud_CreateDateSecondsShift.Value +
                            (double)(nud_CreateDateMinutesShift.Value * 60) +
                            (double)(nud_CreateDateHoursShift.Value * 60 * 60) +
                            (double)(nud_CreateDateDaysShift.Value * 60 * 60 * 24);

                        dtp_CreateDate.Value = _origDateValCreateDate.AddSeconds(value: shiftCreateDateSeconds);
                    }
                }
                else
                {
                    Control sndr = (Control)sender;
                    sndr.Font = new Font(prototype: sndr.Font, newStyle: FontStyle.Bold);
                    // marry up countrycodes and countrynames
                    string sqliteText;
                    if (senderName == "cbx_CountryCode")
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
                    else if (senderName == "cbx_Country")
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
                        settingId: exifTag,
                        settingValue: sndr.Text
                    );
                }
            }
        }
    }

    private void GetTimeZoneOffset()
    {
        string strOffsetTime = "";
        bool useDST = ckb_UseDST.Checked;
        try
        {
            if (!useDST)
            {
                strOffsetTime = cbx_OffsetTimeList.Text.Substring(startIndex: 1, length: 6);
            }
            else
            {
                strOffsetTime = cbx_OffsetTimeList.Text.Substring(startIndex: 8, length: 6);
            }
        }
        catch
        {
            // nothing. Leave it as blank.
        }

        tbx_OffsetTime.Text = strOffsetTime;
    }

    #endregion


    /// <summary>
    ///     This kills off the AcceptButton - problem is that if user was to type in an invalid number (say 7 rather than 07
    ///     for HOUR) and press Enter then the Form would close and sooner rather than later it'd cause trouble.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void dtp_nud_Enter(object sender,
                               EventArgs e)
    {
        AcceptButton = null;
    }

    /// <summary>
    ///     Reinstates AcceptButton. By this point Validation auto-corrects the value if user was derp.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void dtb_nud_Leave(object sender,
                               EventArgs e)
    {
        AcceptButton = btn_OK;
    }

    /// <summary>
    ///     Handles rbt_TakenDateSetToFixedDate changing to on/off. Disables the other controls in the group when required.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void rbt_TakenDateSetToFixedDate_CheckedChanged(object sender,
                                                            EventArgs e)
    {
        if (rbt_TakenDateSetToFixedDate.Checked)
        {
            dtp_TakenDate.Enabled = true;
            nud_TakenDateDaysShift.Enabled = false;
            nud_TakenDateHoursShift.Enabled = false;
            nud_TakenDateMinutesShift.Enabled = false;
            nud_TakenDateSecondsShift.Enabled = false;

            nud_TakenDateDaysShift.Text = "0";
            nud_TakenDateHoursShift.Text = "0";
            nud_TakenDateMinutesShift.Text = "0";
            nud_TakenDateSecondsShift.Text = "0";
        }
        else
        {
            dtp_TakenDate.Enabled = false;
            nud_TakenDateDaysShift.Enabled = true;
            nud_TakenDateHoursShift.Enabled = true;
            nud_TakenDateMinutesShift.Enabled = true;
            nud_TakenDateSecondsShift.Enabled = true;
        }
    }

    private void rbt_TakenDateTimeShift_CheckedChanged(object sender,
                                                       EventArgs e)
    {
        if (!rbt_TakenDateTimeShift.Checked)
        {
            dtp_TakenDate.Enabled = true;
            nud_TakenDateDaysShift.Enabled = false;
            nud_TakenDateHoursShift.Enabled = false;
            nud_TakenDateMinutesShift.Enabled = false;
            nud_TakenDateSecondsShift.Enabled = false;
        }
        else
        {
            dtp_TakenDate.Enabled = false;
            nud_TakenDateDaysShift.Enabled = true;
            nud_TakenDateHoursShift.Enabled = true;
            nud_TakenDateMinutesShift.Enabled = true;
            nud_TakenDateSecondsShift.Enabled = true;
        }
    }

    private void rbt_CreateDateSetToFixedDate_CheckedChanged(object sender,
                                                             EventArgs e)
    {
        if (rbt_CreateDateSetToFixedDate.Checked)
        {
            dtp_CreateDate.Enabled = true;
            nud_CreateDateDaysShift.Enabled = false;
            nud_CreateDateHoursShift.Enabled = false;
            nud_CreateDateMinutesShift.Enabled = false;
            nud_CreateDateSecondsShift.Enabled = false;

            nud_CreateDateDaysShift.Text = "0";
            nud_CreateDateHoursShift.Text = "0";
            nud_CreateDateMinutesShift.Text = "0";
            nud_CreateDateSecondsShift.Text = "0";
        }
        else
        {
            dtp_CreateDate.Enabled = false;
            nud_CreateDateDaysShift.Enabled = true;
            nud_CreateDateHoursShift.Enabled = true;
            nud_CreateDateMinutesShift.Enabled = true;
            nud_CreateDateSecondsShift.Enabled = true;
        }
    }

    private void rbt_CreateDateTimeShift_CheckedChanged(object sender,
                                                        EventArgs e)
    {
        if (!rbt_CreateDateTimeShift.Checked)
        {
            dtp_CreateDate.Enabled = true;
            nud_CreateDateDaysShift.Enabled = false;
            nud_CreateDateHoursShift.Enabled = false;
            nud_CreateDateMinutesShift.Enabled = false;
            nud_CreateDateSecondsShift.Enabled = false;
        }
        else
        {
            dtp_CreateDate.Enabled = false;
            nud_CreateDateDaysShift.Enabled = true;
            nud_CreateDateHoursShift.Enabled = true;
            nud_CreateDateMinutesShift.Enabled = true;
            nud_CreateDateSecondsShift.Enabled = true;
        }
    }

    /// <summary>
    ///     This kills off the AcceptButton - problem is that if user was to type in an invalid number (say 7 rather than 07
    ///     for HOUR) and press Enter then the Form would close and sooner rather than later it'd cause trouble.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void dtp_CreateDate_Leave(object sender,
                                      EventArgs e)
    {
        AcceptButton = null;
    }

    /// <summary>
    ///     Reinstates AcceptButton. By this point Validation auto-corrects the value if user was derp.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void dtp_CreateDate_Enter(object sender,
                                      EventArgs e)
    {
        AcceptButton = btn_OK;
    }

    /// <summary>
    ///     Allows to insert a CreateDate value if there isn't one already.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_InsertCreateDate_Click(object sender,
                                            EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> cGbx_CreateDate = helperNonstatic.GetAllControls(control: gbx_CreateDate);
        foreach (Control cItemGbx_CreateDate in cGbx_CreateDate)
        {
            if (cItemGbx_CreateDate != btn_InsertCreateDate)
            {
                cItemGbx_CreateDate.Enabled = Enabled;

                // set font to bold for these two - that will get picked up later.
                if (cItemGbx_CreateDate is DateTimePicker dtp)
                {
                    dtp.Font = new Font(prototype: dtp.Font, newStyle: FontStyle.Bold);
                    HelperStatic.GenericUpdateAddToDataTable(
                        dt: DtFileDataToWriteStage1PreQueue,
                        fileNameWithoutPath: lvw_FileListEditImages.SelectedItems[index: 0]
                            .Text,
                        settingId: dtp.Name.Substring(startIndex: 4),
                        settingValue: dtp.Text
                    );
                }
            }
        }

        btn_InsertCreateDate.Enabled = false;
    }

    /// <summary>
    ///     Takes the value in TakenDate and pastes it to CreateDate
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_InsertFromTakenDate_Click(object sender,
                                               EventArgs e)
    {
        dtp_CreateDate.Value = dtp_TakenDate.Value;
    }

    /// <summary>
    ///     Sets the tooltip for the pbx_OffsetTimeInfo
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void pbx_OffsetTimeInfo_MouseHover(object sender,
                                               EventArgs e)
    {
        ToolTip ttp = new();

        ttp.SetToolTip(control: pbx_OffsetTimeInfo,
                       caption: HelperStatic.DataReadSQLiteObjectText(
                           languageName: AppLanguage,
                           objectType: "ToolTip",
                           objectName: "ttp_OffsetTime"
                       ));
    }

    /// <summary>
    ///     Pull the TZ (DST) offset according to the checked state of this.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void ckb_UseDST_CheckedChanged(object sender,
                                           EventArgs e)
    {
        GetTimeZoneOffset();
    }

    #endregion
}