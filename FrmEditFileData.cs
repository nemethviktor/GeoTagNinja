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
        InitializeComponent();

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

        // load TZ-CBX
        foreach (string timezone in AncillaryListsArrays.GetTimeZones())
        {
            cbx_OffsetTimeList.Items.Add(item: timezone);
        }
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
        _frmEditFileDataNowLoadingFileData = true;
        FrmEditFileDataNowRemovingGeoData = false;

        // this just pulls the form's name
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

        // this updates the listview itself
        clh_FileName.Width = -2; // auto width col
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

            // This actually gets triggered with the SelectedIndexChange above.
            // lvw_EditorFileListImagesGetData();

            // This actually gets triggered with the SelectedIndexChange above.
            //await pbx_imgPreviewPicGenerator(fileNameWithPath: fileNameWithPath);
        }

        _frmEditFileDataNowLoadingFileData = false; // techinically this is redundant here
    }

    /// <summary>
    ///     Gets the text values for the Controls in the Form - for labels/buttons etc this is their "language" (eg. English)
    ///     label (e.g. "Latitude")
    ///     ... for textboxes etc this is the value (e.g. "51.002")
    /// </summary>
    private void lvw_EditorFileListImagesGetData()
    {
        _frmEditFileDataNowLoadingFileData = true;

        string fileNameWithoutPath = lvw_FileListEditImages.SelectedItems[index: 0]
            .Text;
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            try
            {
                if (
                    cItem is Label ||
                    cItem is GroupBox ||
                    cItem is Button ||
                    cItem is CheckBox ||
                    cItem is TabPage ||
                    cItem is RadioButton
                )
                {
                    HelperStatic.GenericReturnControlText(cItem: cItem, senderForm: this);
                }
                else if (cItem is TextBox || cItem is ComboBox || cItem is DateTimePicker || cItem is NumericUpDown)
                {
                    // reset font to normal
                    cItem.Font = new Font(prototype: cItem.Font, newStyle: FontStyle.Regular);
                    string exifTag = cItem.Name.Substring(startIndex: 4);

                    // if label then we want text to come from datarow [objectText]
                    // else if textbox/dropdown then we want the data to come from the same spot [metaDataDirectoryData.tagName]
                    string tempStr = "-";
                    if (exifTag == "OffsetTimeList") // I hate you.
                    {
                        // blank on purpose
                    }
                    else if (exifTag.EndsWith(value: "Shift")) // I very passionately hate you too.
                    {
                        // this will never sit in the main listview data because it's not a tag.
                        tempStr = lvw_EditorFile_GetDataReadFromSQLQueues(fileNameWithoutPath: fileNameWithoutPath, cItem: cItem);
                        if (tempStr == null)
                        {
                            if (cItem is not NumericUpDown)
                            {
                                tempStr = "-";
                            }
                            else
                            {
                                tempStr = "0";
                            }
                        }
                    }
                    else
                    {
                        tempStr = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                            .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: "clh_" + exifTag]
                                          .Index]
                            .Text;
                    }

                    // if empty...
                    if (tempStr == "-")
                    {
                        switch (cItem.Parent.Name)
                        {
                            case "gbx_TakenDate":
                                // NUDs are likely to be blank for almost all files so this shouldn't prevent the user from operating with the data
                                if (cItem is not NumericUpDown)
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

                                break;
                            case "gbx_CreateDate":
                                if (cItem is not NumericUpDown)
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

                                break;

                            default:
                                cItem.Text = "";
                                break;
                        }
                    }
                    // still if empty
                    else
                    {
                        cItem.Text = tempStr;
                        if (cItem == dtp_TakenDate)
                        {
                            btn_InsertTakenDate.Enabled = false;
                            DataRow[] drTakenDate = FrmMainApp.DtOriginalTakenDate.Select(filterExpression: "fileNameWithoutPath = '" + fileNameWithoutPath + "'");
                            if (drTakenDate.Length > 0)
                            {
                                _origDateValTakenDate = Convert.ToDateTime(value: drTakenDate[0][columnName: "originalTakenDate"]
                                                                               .ToString());
                            }
                        }
                        else if (cItem == dtp_CreateDate)
                        {
                            btn_InsertCreateDate.Enabled = false;
                            DataRow[] drCreateDate = FrmMainApp.DtOriginalCreateDate.Select(filterExpression: "fileNameWithoutPath = '" + fileNameWithoutPath + "'");
                            if (drCreateDate.Length > 0)
                            {
                                _origDateValCreateDate = Convert.ToDateTime(value: drCreateDate[0][columnName: "originalCreateDate"]
                                                                                .ToString());
                            }
                        }
                        else if (cItem == cbx_OffsetTimeList)
                        {
                            // leave blank on purpose.
                        }
                    }

                    // stick into sql ("pending save") - this is to see if the data has changed later.
                    if (cItem is not NumericUpDown cItemNumericUpDown)
                    {
                        HelperStatic.GenericUpdateAddToDataTable(
                            dt: DtFileDataToWriteStage2QueuePendingSave,
                            fileNameWithoutPath: fileNameWithoutPath,
                            settingId: cItem.Name.Substring(startIndex: 4),
                            settingValue: cItem.Text);
                    }
                    else
                    {
                        HelperStatic.GenericUpdateAddToDataTable(
                            dt: DtFileDataToWriteStage2QueuePendingSave,
                            fileNameWithoutPath: fileNameWithoutPath,
                            settingId: cItemNumericUpDown.Name.Substring(startIndex: 4),
                            settingValue: cItemNumericUpDown.Value.ToString(provider: CultureInfo.InvariantCulture));
                    }

                    string strValueInSQL = lvw_EditorFile_GetDataReadFromSQLQueues(fileNameWithoutPath: fileNameWithoutPath, cItem: cItem);

                    if (strValueInSQL != null)
                    {
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
                            dtp.Font = new Font(prototype: dtp.Font, newStyle: FontStyle.Bold);
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        // done load
        _frmEditFileDataNowLoadingFileData = false;
    }

    private static string lvw_EditorFile_GetDataReadFromSQLQueues(string fileNameWithoutPath,
                                                                  Control cItem)
    {
        string strValueInSQL = null;

        // overwrite from sql-Q if available
        // if data was pulled from the map this will sit in the main table, not in Q
        DataView dvSqlDataQ = new(table: DtFileDataToWriteStage1PreQueue);
        dvSqlDataQ.RowFilter = "fileNameWithoutPath = '" + fileNameWithoutPath + "' AND settingId ='" + cItem.Name.Substring(startIndex: 4) + "'";

        DataView dvSqlDataF = new(table: DtFileDataToWriteStage3ReadyToWrite);
        dvSqlDataF.RowFilter = "fileNameWithoutPath = '" + fileNameWithoutPath + "' AND settingId ='" + cItem.Name.Substring(startIndex: 4) + "'";

        if (dvSqlDataQ.Count > 0 || dvSqlDataF.Count > 0)
        {
            // see if data in temp-queue
            if (dvSqlDataQ.Count > 0)
            {
                strValueInSQL = dvSqlDataQ[recordIndex: 0][property: "settingValue"]
                    .ToString();
            }
            // see if data is ready to be written
            else if (dvSqlDataF.Count > 0)
            {
                strValueInSQL = dvSqlDataF[recordIndex: 0][property: "settingValue"]
                    .ToString();
            }
        }

        return strValueInSQL;
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
                    await HelperStatic.ExifGetImagePreviews(fileNameWithoutPath: fileNameWithPath);
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
            DataView dvFileNames = new(table: DtFileDataToWriteStage1PreQueue);
            DataTable dtDistinctFileNames = dvFileNames.ToTable(distinct: true, "fileNameWithoutPath");
            foreach (DataRow drFileName in dtDistinctFileNames.Rows)
            {
                string fileNameWithoutPath = drFileName[columnIndex: 0]
                    .ToString();

                DataView dvFileWriteQueue = new(table: DtFileDataToWriteStage1PreQueue);
                dvFileWriteQueue.RowFilter = "fileNameWithoutPath = '" + fileNameWithoutPath + "'";

                // this shouldn't ever be zero...
                if (dvFileWriteQueue.Count > 0)
                {
                    // transfer data from 1 to 3
                    DataTable dtFileWriteQueue = dvFileWriteQueue.ToTable();
                    foreach (DataRow drS1 in dtFileWriteQueue.Rows)
                    {
                        string drS1FileNameWithoutPath = drS1[columnName: "fileNameWithoutPath"]
                            .ToString();
                        string drS1SettingId = drS1[columnName: "settingId"]
                            .ToString();

                        // any existing instance of this particular combination needs to be deleted...
                        // eg if we type "50" as a value then w/o this we'd end up with a "5" row and a "50" row.

                        DtFileDataToWriteStage3ReadyToWrite.Rows.Cast<DataRow>()
                            .Where(
                                predicate: r => r.ItemArray[0]
                                                    .ToString() ==
                                                drS1FileNameWithoutPath &&
                                                r.ItemArray[1]
                                                    .ToString() ==
                                                drS1SettingId)
                            .ToList()
                            .ForEach(action: r => r.Delete());

                        DtFileDataToWriteStage3ReadyToWrite.AcceptChanges();
                        DtFileDataToWriteStage3ReadyToWrite.Rows.Add(values: drS1.ItemArray);
                        DtFileDataToWriteStage3ReadyToWrite.AcceptChanges();
                    }

                    // Also logically if user changed (TakenDate || CreateDate) && ! *Shift* then stick those values into FrmMainApp.DtOriginalTakenDate && FrmMainApp.DtOriginalCreateDate
                    // Basically they just pressed OK to queue it up to write-queue. If they then amend stuff this will be their basis of reality and if they cancel (by refreshing), the Dt is lost anyway.
                    // TakenDate
                    DataRow[] dtDateChanged = dtFileWriteQueue.Select(filterExpression: "settingId = 'TakenDate'");
                    int dateChangeCount = dtDateChanged.Length;

                    DataRow[] dtDateShifted = dtFileWriteQueue.Select(filterExpression: "settingId LIKE 'TakenDate%' AND settingId LIKE '%Shift'");
                    int shiftedChangeCount = dtDateShifted.Length;

                    if (dateChangeCount == 1 && shiftedChangeCount == 0)
                    {
                        DataRow drDateChanged = dtDateChanged[0];
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
                        drDateChange[columnName: "originalTakenDate"] = drDateChanged[columnName: "settingValue"];
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
                    dtDateChanged = dtFileWriteQueue.Select(filterExpression: "settingId = 'CreateDate'");
                    dateChangeCount = dtDateChanged.Length;

                    dtDateShifted = dtFileWriteQueue.Select(filterExpression: "settingId LIKE 'CreateDate%' AND settingId LIKE '%Shift'");
                    shiftedChangeCount = dtDateShifted.Length;

                    if (dateChangeCount == 1 && shiftedChangeCount == 0)
                    {
                        DataRow drDateChanged = dtDateChanged[0];
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
                        drDateChange[columnName: "originalCreateDate"] = drDateChanged[columnName: "settingValue"];
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
            DataView dvPreviousText = new(table: DtFileDataToWriteStage2QueuePendingSave);
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
                dvPreviousText.RowFilter = "fileNameWithoutPath = '" +
                                           lvw_FileListEditImages.SelectedItems[index: 0]
                                               .Text +
                                           "' AND settingId = '" +
                                           exifTag +
                                           "'";
                if (dvPreviousText.Count > 0)
                {
                    previousText = dvPreviousText[recordIndex: 0][property: "settingValue"]
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
                dvPreviousText.RowFilter = "fileNameWithoutPath = '" +
                                           lvw_FileListEditImages.SelectedItems[index: 0]
                                               .Text +
                                           "' AND settingId = '" +
                                           exifTag +
                                           "'";
                if (dvPreviousText.Count > 0)
                {
                    previousText = dvPreviousText[recordIndex: 0][property: "settingValue"]
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