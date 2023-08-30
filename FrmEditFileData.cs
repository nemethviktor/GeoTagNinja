using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using NLog;
using TimeZoneConverter;
using static GeoTagNinja.FrmMainApp;
using static GeoTagNinja.Model.SourcesAndAttributes;
using static GeoTagNinja.View.ListView.FileListView;

namespace GeoTagNinja;

public partial class FrmEditFileData : Form
{
    private static bool _tzChangedByApi;


#region Variables

    private static bool _frmEditFileDataNowLoadingFileData;

#endregion

    private DateTime _origDateValCreateDate = DateTime.Now;
    private DateTime _origDateValTakenDate = DateTime.Now;

    /// <summary>
    ///     This Form provides an interface for the user to edit various bits of Exif data in images.
    /// </summary>
    public FrmEditFileData()
    {
        Logger logger = FrmMainApp.Logger;
        logger.Debug(message: "Starting");

        InitializeComponent();
        logger.Trace(message: "InitializeComponent OK");
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
        Logger logger = FrmMainApp.Logger;
        logger.Info(message: "Starting");
        logger.Trace(message: "Defaults Starting");
        _frmEditFileDataNowLoadingFileData = true;

        logger.Trace(message: "Emptying FrmMainApp.Stage1EditFormIntraTabTransferQueue + Stage2EditFormReadyToSaveAndMoveToWriteQueue");
        foreach (DirectoryElement dirElemFileToModify in DirectoryElements)
        {
            {
                foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                {
                    // empty queue

                    dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue);

                    // also empty the "original data" table

                    dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue);
                }
            }
        }

        logger.Trace(message: "Emptying FrmMainApp.Stage1EditFormIntraTabTransferQueue + Stage2EditFormReadyToSaveAndMoveToWriteQueue - Done");

        logger.Trace(message: "Setting Dropdown defaults");
        // Deal with Dates
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
        HelperControlAndMessageBoxHandling.ReturnControlText(cItem: this, senderForm: this);

        // fills the countries box

        foreach (string country in HelperGenericAncillaryListsArrays.GetCountries())
        {
            cbx_Country.Items.Add(item: country);
        }

        // fills the country codes box
        foreach (string countryCode in HelperGenericAncillaryListsArrays.GetCountryCodes())
        {
            cbx_CountryCode.Items.Add(item: countryCode);
        }

        // load TZ-CBX
        foreach (string timezone in HelperGenericAncillaryListsArrays.GetTimeZones())
        {
            cbx_OffsetTimeList.Items.Add(item: timezone);
        }

        logger.Trace(message: "Setting Dropdown defaults - Done");

        // this updates the listview itself

        if (lvw_FileListEditImages.Items.Count > 0)
        {
            logger.Trace(message: "ListViewSelect Start");

            lvw_FileListEditImages.Items[index: 0]
                                  .Selected = true;

            // actually if it's just one file i don't want this to be actively selectable
            if (lvw_FileListEditImages.Items.Count == 1)
            {
                lvw_FileListEditImages.Enabled = false;
            }

            logger.Trace(message: "ListViewSelect Done");
        }

        _frmEditFileDataNowLoadingFileData = false; // techinically this is redundant here
        logger.Info(message: "Done");
    }

    /// <summary>
    ///     Gets the text values for the Controls in the Form - for labels/buttons etc this is their "language" (eg. English)
    ///     label (e.g. "Latitude")
    ///     ... for textboxes etc this is the value (e.g. "51.002")
    /// </summary>
    private void lvw_EditorFileListImagesGetData()
    {
        Logger logger = FrmMainApp.Logger;
        logger.Debug(message: "Starting");

        _frmEditFileDataNowLoadingFileData = true;

        ListView lvw = lvw_FileListEditImages;
        ListViewItem lvi = lvw.SelectedItems[index: 0];
        lvw.Columns[index: 0]
           .Width = lvw.Width;

        string fileNameWithoutPath = lvi.Text;
        DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemGUID(GUID: lvi.SubItems[index: lvw.Columns[key: COL_NAME_PREFIX + FileListColumns.GUID]
                                                                                                                    .Index]
                                                                                                .Text);
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        btn_InsertFromTakenDate.Enabled = false;

        logger.Trace(message: "Assinging Labels Start");
        HelperNonStatic helperNonstatic = new();
        List<Type> lstControlTypesNoDEValue = new()
        {
            typeof(Label),
            typeof(GroupBox),
            typeof(Button),
            typeof(CheckBox),
            typeof(TabPage),
            typeof(RadioButton)
        };
        List<Type> lstControlTypesWithDEValue = new()
        {
            typeof(TextBox),
            typeof(ComboBox),
            typeof(DateTimePicker),
            typeof(NumericUpDown)
        };

        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (lstControlTypesNoDEValue.Contains(item: cItem.GetType()) || lstControlTypesWithDEValue.Contains(item: cItem.GetType()))
            {
                string strExifTag = cItem.Name; // this is for debugging only (particularly here, that is.)

                try
                {
                    logger.Trace(message: "cItem: " +
                                          cItem.Name +
                                          " (Type: " +
                                          cItem.GetType()
                                               .Name +
                                          ") - Starting.");

                    if (lstControlTypesNoDEValue.Contains(item: cItem.GetType()))
                    {
                        // gets logged inside.
                        cItem.Text = HelperDataLanguageTZ.DataReadDTObjectText(objectType: cItem.GetType()
                                                                                                .Name,
                                                                               objectName: cItem.Name);
                    }
                    else if (lstControlTypesWithDEValue.Contains(item: cItem.GetType()))
                    {
                        strExifTag = cItem.Name.Substring(startIndex: 4);

                        //
                        ElementAttribute attribute = GetAttributeFromString(attributeToFind: strExifTag);

                        Type typeOfAttribute = GetAttributeType(attribute: attribute);
                        DirectoryElement.AttributeVersion maxAttributeVersion = DirectoryElement.AttributeVersion.Original;
                        bool dataExistsSomewhere = false;
                        bool dataIsNull = false;
                        List<DirectoryElement.AttributeVersion> relevantAttributeVersions = new()
                        {
                            // DO NOT reorder!
                            DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                            DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                            DirectoryElement.AttributeVersion.Original
                        };

                        foreach (DirectoryElement.AttributeVersion relevantAttributeVersion in relevantAttributeVersions)
                        {
                            if (dirElemFileToModify.HasSpecificAttributeWithVersion(
                                    attribute: attribute,
                                    version: relevantAttributeVersion))
                            {
                                maxAttributeVersion = relevantAttributeVersion;
                                dataExistsSomewhere = true;
                                break;
                            }
                        }

                        IConvertible dataInDE = null;
                        string cItemValStr = FrmMainApp.NullStringEquivalentBlank;
                        if (dataExistsSomewhere)
                        {
                            if (typeOfAttribute == typeof(string))
                            {
                                dataInDE = dirElemFileToModify.GetAttributeValueString(attribute: attribute,
                                                                                       version: maxAttributeVersion);

                                if (string.IsNullOrEmpty(dataInDE.ToString()))
                                    dataIsNull = true;
                            }
                            else if (typeOfAttribute == typeof(int))
                            {
                                dataInDE = dirElemFileToModify.GetAttributeValue<int>(
                                    attribute: attribute,
                                    version: maxAttributeVersion);
                                if ((int)dataInDE == FrmMainApp.NullIntEquivalent)
                                    dataIsNull = true;
                            }
                            else if (typeOfAttribute == typeof(double))
                            {
                                dataInDE = dirElemFileToModify.GetAttributeValue<double>(
                                    attribute: attribute,
                                    version: maxAttributeVersion);
                                if ((double)dataInDE == FrmMainApp.NullDoubleEquivalent)
                                    dataIsNull = true;
                            }
                            else if (typeOfAttribute == typeof(DateTime))
                            {
                                dataInDE = dirElemFileToModify.GetAttributeValue<DateTime>(
                                    attribute: attribute,
                                    version: maxAttributeVersion);
                                if ((DateTime)dataInDE == FrmMainApp.NullDateTimeEquivalent)
                                    dataIsNull = true;
                            }

                            if (!dataIsNull)
                                cItemValStr = dataInDE.ToString();

                            // reset font to normal
                            cItem.Font = new Font(prototype: cItem.Font, newStyle: FontStyle.Regular);
                        }
                        else
                        {
                            dataIsNull = true;
                        }

                        // if dataIsNull...
                        if (dataIsNull)
                        {
                            // okay for gbx_*Date && !NUD -> those are DateTimePickers. If they are NULL then there is either no TakenDate or no CreateDate so the actual controls will have to be disabled.
                            if (cItem.Parent.Name.StartsWith(value: "gbx_") && cItem.Parent.Name.EndsWith(value: "Date"))
                            {
                                if (cItem is not NumericUpDown nud)
                                {
                                    if (cItem.Parent.Name == "gbx_TakenDate")
                                    {
                                        IEnumerable<Control> cGbx_TakenDate = helperNonstatic.GetAllControls(control: gbx_TakenDate);
                                        foreach (Control cItemGbx_TakenDate in cGbx_TakenDate)
                                        {
                                            if (cItemGbx_TakenDate != btn_InsertTakenDate)
                                            {
                                                cItemGbx_TakenDate.Enabled = false;
                                                btn_InsertTakenDate.Enabled = true;
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
                                                btn_InsertCreateDate.Enabled = true;
                                                btn_InsertFromTakenDate.Enabled = false;
                                            }
                                        }
                                    }
                                }
                                else // cItem is nud
                                {
                                    nud.Value = FrmMainApp.NullIntEquivalent;
                                    nud.Text = FrmMainApp.NullStringEquivalentZero;
                                }
                            }
                            // if it's none of the above then make the cItem be just blank.
                            else if (strExifTag == "OffsetTimeList") // I hate you.
                            {
                                // blank on purpose
                            }
                            else
                            {
                                cItem.Text = FrmMainApp.NullStringEquivalentBlank;
                            }
                        }
                        // if has value...
                        else
                        {
                            // if this is a TakenDate or CreateDate -related
                            if (cItem.Parent.Name.StartsWith(value: "gbx_") && cItem.Parent.Name.EndsWith(value: "Date") && cItem is not NumericUpDown)
                            {
                                if (cItem.Parent.Name == "gbx_TakenDate")
                                {
                                    IEnumerable<Control> cGbx_TakenDate = helperNonstatic.GetAllControls(control: gbx_TakenDate);
                                    foreach (Control cItemGbx_TakenDate in cGbx_TakenDate)
                                    {
                                        if (cItemGbx_TakenDate != btn_InsertTakenDate)
                                        {
                                            cItemGbx_TakenDate.Enabled = true;
                                            btn_InsertTakenDate.Enabled = false;
                                            btn_InsertFromTakenDate.Enabled = true;
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
                                            cItemGbx_CrateDate.Enabled = true;
                                            btn_InsertCreateDate.Enabled = false;
                                        }
                                    }
                                }
                            }

                            // this is related to storing the default DateTimes for TakenDate and CreateDate
                            // what we also need to do here is account for any copy-paste shifts.
                            if (cItem is DateTimePicker dtp)
                            {
                                int shiftedDays = 0;
                                int shiftedHours = 0;
                                int shiftedMinutes = 0;
                                int shiftedSeconds = 0;
                                int totalShiftedSeconds = 0;

                                if (dtp == dtp_TakenDate && OriginalTakenDateDict.ContainsKey(key: fileNameWithoutPath))
                                {
                                    _origDateValTakenDate = Convert.ToDateTime(value: OriginalTakenDateDict[key: fileNameWithoutPath]);

                                    shiftedDays = (int)dirElemFileToModify.GetAttributeValue<int>(
                                        attribute: ElementAttribute.TakenDateDaysShift,
                                        version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                        notFoundValue: 0);

                                    shiftedHours = (int)dirElemFileToModify.GetAttributeValue<int>(
                                        attribute: ElementAttribute.TakenDateHoursShift,
                                        version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                        notFoundValue: 0);

                                    shiftedMinutes = (int)dirElemFileToModify.GetAttributeValue<int>(
                                        attribute: ElementAttribute.TakenDateMinutesShift,
                                        version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                        notFoundValue: 0);

                                    shiftedSeconds = (int)dirElemFileToModify.GetAttributeValue<int>(
                                        attribute: ElementAttribute.TakenDateSecondsShift,
                                        version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                        notFoundValue: 0);

                                    totalShiftedSeconds = shiftedSeconds +
                                                          shiftedMinutes * 60 +
                                                          shiftedHours * 60 * 60 +
                                                          shiftedDays * 60 * 60 * 24;

                                    DateTime modifiedTakenDateTime = _origDateValTakenDate.AddSeconds(value: totalShiftedSeconds);
                                    cItemValStr = modifiedTakenDateTime.ToString(provider: CultureInfo.CurrentUICulture);
                                }

                                else if (dtp == dtp_CreateDate && OriginalCreateDateDict.ContainsKey(key: fileNameWithoutPath))
                                {
                                    _origDateValCreateDate = Convert.ToDateTime(value: OriginalCreateDateDict[key: fileNameWithoutPath]);

                                    shiftedDays = (int)dirElemFileToModify.GetAttributeValue<int>(
                                        attribute: ElementAttribute.CreateDateDaysShift,
                                        version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                        notFoundValue: 0);

                                    shiftedHours = (int)dirElemFileToModify.GetAttributeValue<int>(
                                        attribute: ElementAttribute.CreateDateHoursShift,
                                        version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                        notFoundValue: 0);

                                    shiftedMinutes = (int)dirElemFileToModify.GetAttributeValue<int>(
                                        attribute: ElementAttribute.CreateDateMinutesShift,
                                        version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                        notFoundValue: 0);

                                    shiftedSeconds = (int)dirElemFileToModify.GetAttributeValue<int>(
                                        attribute: ElementAttribute.CreateDateSecondsShift,
                                        version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                        notFoundValue: 0);

                                    totalShiftedSeconds = shiftedSeconds +
                                                          shiftedMinutes * 60 +
                                                          shiftedHours * 60 * 60 +
                                                          shiftedDays * 60 * 60 * 24;

                                    DateTime modifiedCreateDateTime = _origDateValCreateDate.AddSeconds(value: totalShiftedSeconds);
                                    cItemValStr = modifiedCreateDateTime.ToString(provider: CultureInfo.CurrentUICulture);
                                }

                                logger.Trace(message: "cItem: " + cItem.Name + " - Updating DateTimePicker");
                                if (maxAttributeVersion != DirectoryElement.AttributeVersion.Original || totalShiftedSeconds != 0)
                                {
                                    dtp.Font = new Font(prototype: dtp.Font, newStyle: FontStyle.Bold);
                                }
                            }

                            logger.Trace(message: "cItem: " + cItem.Name + " - Adding to Stage2EditFormReadyToSaveAndMoveToWriteQueue");
                            // stick into DE2 ("pending save") - this is to see if the data has changed later.
                            if (cItem is not NumericUpDown nud)
                            {
                                attribute = GetAttributeFromString(attributeToFind: cItem.Name.Substring(startIndex: 4));

                                dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                                                                             value: cItemValStr,
                                                                             version: DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue,
                                                                             isMarkedForDeletion: false);

                                cItem.Text = cItemValStr;
                            }
                            else // if (cItem is NumericUpDown nud)
                            {
                                nud.Value = Convert.ToDecimal(value: cItemValStr, provider: CultureInfo.InvariantCulture);
                                cItemValStr = nud.Value.ToString();

                                if (nud.Name.EndsWith(value: "Shift") && nud.Value != 0)
                                {
                                    if (nud.Name.Substring(startIndex: 4)
                                           .StartsWith(value: "Taken"))
                                    {
                                        rbt_TakenDateTimeShift.Checked = true;
                                    }
                                    else if (nud.Name.Substring(startIndex: 4)
                                                .StartsWith(value: "Create"))
                                    {
                                        rbt_CreateDateTimeShift.Checked = true;
                                    }
                                }

                                attribute = GetAttributeFromString(attributeToFind: nud.Name.Substring(startIndex: 4));

                                dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                                                                             value: cItemValStr,
                                                                             version: DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue,
                                                                             isMarkedForDeletion: false);

                                nud.Text = nud.Value.ToString();

                                if (maxAttributeVersion != DirectoryElement.AttributeVersion.Original)
                                {
                                    nud.Font = new Font(prototype: nud.Font, newStyle: FontStyle.Bold);
                                }
                            }

                            if (cItem is TextBox txt)
                            {
                                logger.Trace(message: "cItem: " + cItem.Name + " - Updating TextBox");
                                if (maxAttributeVersion != DirectoryElement.AttributeVersion.Original)
                                {
                                    txt.Font = new Font(prototype: txt.Font, newStyle: FontStyle.Bold);
                                }
                            }
                            else if (cItem is ComboBox cmb)
                            {
                                logger.Trace(message: "cItem: " + cItem.Name + " - Updating ComboBox");
                                if (maxAttributeVersion != DirectoryElement.AttributeVersion.Original)
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
                                        sqliteText = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
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
                                        sqliteText = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
                                            queryWhat: "ISO_3166_1A3",
                                            inputVal: cbx_CountryCode.Text,
                                            returnWhat: "Country");
                                        if (cbx_Country.Text != sqliteText)
                                        {
                                            cbx_Country.Text = sqliteText;
                                        }
                                    }
                                    else if (cItem.Name == "cbx_OffsetTime")
                                    {
                                        // ignore
                                    }
                                }
                            }
                        }
                    }
                    //
                }
                catch
                {
                    // ignored
                }

                logger.Trace(message: "cItem: " +
                                      cItem.Name +
                                      " (Type: " +
                                      cItem.GetType()
                                           .Name +
                                      ") - Done.");
            }
        }

        logger.Trace(message: "Assinging Labels Done");

        // done load
        logger.Debug(message: "Done");
        _frmEditFileDataNowLoadingFileData = false;
    }

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
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        //reset this just in case.
        HelperVariables.SApiOkay = true;
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
            default:
                MessageBox.Show(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                              messageBoxName: "mbx_FrmEditFileData_ErrorInvalidSender") +
                          ((Button)sender).Name,
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error);
                break;
        }

        if (HelperVariables.SApiOkay)
        {
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmEditFileData_InfoDataUpdated"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Info"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_FrmEditFileData_ErrorAPIError"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
        }
    }

    /// <summary>
    ///     Pulls data from the various APIs and fills up the listView and fills the TextBoxes and/or SQLite.
    /// </summary>
    /// <param name="fileNameWithoutPath">Blank if used as "pull one file" otherwise the name of the file w/o Path</param>
    private void getFromWeb_Toponomy(string fileNameWithoutPath = "")
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        double parsedLat;
        double parsedLng;
        DateTime createDate = FrmMainApp.NullDateTimeEquivalent; // can't leave it null because it's updated in various IFs and C# perceives it as uninitialised.

        string strGpsLatitude = null;
        string strGpsLongitude = null;

        DataTable dtToponomy = new();

        // this is "current file"
        if (fileNameWithoutPath == "")
        {
            if (nud_GPSLatitude.Text != "" && nud_GPSLongitude.Text != "")
            {
                strGpsLatitude = nud_GPSLatitude.Value.ToString(provider: CultureInfo.InvariantCulture);
                strGpsLongitude = nud_GPSLongitude.Value.ToString(provider: CultureInfo.InvariantCulture);

                HelperVariables.CurrentAltitude = null;
                HelperVariables.CurrentAltitude = nud_GPSAltitude.Text.ToString(provider: CultureInfo.InvariantCulture);

                dtToponomy = HelperExifReadExifData.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strGpsLatitude,
                                                                                         lng: strGpsLongitude,
                                                                                         fileNameWithoutPath: fileNameWithoutPath);
            }
        }

        // this is all the other files
        else
        {
            if (frmMainAppInstance != null)
            {
                HelperVariables.CurrentAltitude = null;
                HelperVariables.CurrentAltitude = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                                                                    .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: COL_NAME_PREFIX + FileListColumns.GPS_ALTITUDE]
                                                                                                       .Index]
                                                                    .Text.ToString(provider: CultureInfo.InvariantCulture);

                strGpsLatitude = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                                                   .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: COL_NAME_PREFIX + FileListColumns.GPS_LATITUDE]
                                                                                      .Index]
                                                   .Text.ToString(provider: CultureInfo.InvariantCulture);
                strGpsLongitude = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                                                    .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: COL_NAME_PREFIX + FileListColumns.GPS_LONGITUDE]
                                                                                       .Index]
                                                    .Text.ToString(provider: CultureInfo.InvariantCulture);
                string strCreateDate = frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                                                         .SubItems[index: frmMainAppInstance.lvw_FileList.Columns[key: COL_NAME_PREFIX + FileListColumns.CREATE_DATE]
                                                                                            .Index]
                                                         .Text.ToString(provider: CultureInfo.InvariantCulture);
                bool _ = DateTime.TryParse(s: strCreateDate.ToString(provider: CultureInfo.InvariantCulture), result: out createDate);
            }

            if (double.TryParse(s: strGpsLatitude,
                                style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture,
                                result: out parsedLat) &&
                double.TryParse(s: strGpsLongitude,
                                style: NumberStyles.Any,
                                provider: CultureInfo.InvariantCulture,
                                result: out parsedLng))
            {
                dtToponomy = HelperExifReadExifData.DTFromAPIExifGetToponomyFromWebOrSQL(lat: strGpsLatitude, lng: strGpsLongitude, fileNameWithoutPath: fileNameWithoutPath);
            }
        }

        // Pull the data from the web regardless.
        List<(ElementAttribute attribute, string toponomyOverwriteVal)> toponomyOverwrites = new();
        if (dtToponomy != null && dtToponomy.Rows.Count > 0)
        {
            toponomyOverwrites.Add(item: (ElementAttribute.CountryCode, dtToponomy.Rows[index: 0][columnName: "CountryCode"]
                                                                                  .ToString()));
            toponomyOverwrites.Add(item: (ElementAttribute.Country, dtToponomy.Rows[index: 0][columnName: "Country"]
                                                                              .ToString()));
            toponomyOverwrites.Add(item: (ElementAttribute.City, dtToponomy.Rows[index: 0][columnName: "City"]
                                                                           .ToString()));
            toponomyOverwrites.Add(item: (ElementAttribute.State, dtToponomy.Rows[index: 0][columnName: "State"]
                                                                            .ToString()));
            toponomyOverwrites.Add(item: (ElementAttribute.Sub_location, dtToponomy.Rows[index: 0][columnName: "Sub_location"]
                                                                                   .ToString()));
            toponomyOverwrites.Add(item: (ElementAttribute.GPSAltitude, dtToponomy.Rows[index: 0][columnName: "GPSAltitude"]
                                                                                  .ToString()));

            string TZ = dtToponomy.Rows[index: 0][columnName: "timeZoneId"]
                                  .ToString();

            // multiple files...i think. fml.
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
                                    if (!TZOffset.StartsWith(value: NullStringEquivalentGeneric))
                                    {
                                        toponomyOverwrites.Add(item: (ElementAttribute.OffsetTime, "+" + TZOffset));
                                    }
                                    else
                                    {
                                        toponomyOverwrites.Add(item: (ElementAttribute.OffsetTime, TZOffset));
                                    }
                                }
                            }
                            catch
                            {
                                // add a zero
                                toponomyOverwrites.Add(item: (ElementAttribute.OffsetTime, "+00:00"));
                            }

                            _tzChangedByApi = false;
                            break;
                        }
                    }
                }

                // send it back to the Form + SQL
                foreach ((ElementAttribute attribute, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
                {
                    switch (toponomyDetail.attribute)
                    {
                        case ElementAttribute.CountryCode:
                            cbx_CountryCode.Text = toponomyDetail.toponomyOverwriteVal;
                            break;
                        case ElementAttribute.Country:
                            cbx_Country.Text = toponomyDetail.toponomyOverwriteVal;
                            break;
                        case ElementAttribute.City:
                            tbx_City.Text = toponomyDetail.toponomyOverwriteVal;
                            break;
                        case ElementAttribute.State:
                            tbx_State.Text = toponomyDetail.toponomyOverwriteVal;
                            break;
                        case ElementAttribute.Sub_location:
                            tbx_Sub_location.Text = toponomyDetail.toponomyOverwriteVal;
                            break;
                        case ElementAttribute.GPSAltitude:
                            nud_GPSAltitude.Text = toponomyDetail.toponomyOverwriteVal;
                            nud_GPSAltitude.Value = Convert.ToDecimal(value: toponomyDetail.toponomyOverwriteVal, provider: CultureInfo.InvariantCulture);
                            break;
                        case ElementAttribute.OffsetTime:
                            tbx_OffsetTime.Text = toponomyDetail.toponomyOverwriteVal;
                            break;
                    }
                }
            }
            // one file
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
                        if (!TZOffset.StartsWith(value: NullStringEquivalentGeneric))
                        {
                            toponomyOverwrites.Add(item: (ElementAttribute.OffsetTime, "+" + TZOffset));
                        }
                        else
                        {
                            toponomyOverwrites.Add(item: (ElementAttribute.OffsetTime, TZOffset));
                        }
                    }
                }
                catch
                {
                    // add a zero
                    toponomyOverwrites.Add(item: (ElementAttribute.OffsetTime, "+00:00"));
                }

                ListView lvw = lvw_FileListEditImages;
                ListViewItem lvi = lvw.SelectedItems[index: 0];

                DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemGUID(GUID: lvi.SubItems[index: lvw.Columns[key: COL_NAME_PREFIX + FileListColumns.GUID]
                                                                                                                            .Index]
                                                                                                        .Text);
                foreach ((ElementAttribute attribute, string toponomyOverwriteVal) toponomyDetail in toponomyOverwrites)
                {
                    if (dirElemFileToModify != null)
                    {
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: toponomyDetail.attribute,
                                                                     value: toponomyDetail.toponomyOverwriteVal,
                                                                     version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue, isMarkedForDeletion: false);
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
        string fileNameWithoutPath = lvw_FileListEditImages.SelectedItems[index: 0]
                                                           .Text;
        foreach (Control cItemGbx_TakenDate in cGbx_TakenDate)
        {
            if (cItemGbx_TakenDate != btn_InsertTakenDate)
            {
                cItemGbx_TakenDate.Enabled = Enabled;

                // set font to bold for these two - that will get picked up later.
                if (cItemGbx_TakenDate is DateTimePicker dtp)
                {
                    dtp.Font = new Font(prototype: dtp.Font, newStyle: FontStyle.Bold);
                    ListView lvw = lvw_FileListEditImages;
                    ListViewItem lvi = lvw.SelectedItems[index: 0];

                    DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemGUID(GUID: lvi.SubItems[index: lvw.Columns[key: COL_NAME_PREFIX + FileListColumns.GUID]
                                                                                                                                .Index]
                                                                                                            .Text);

                    ElementAttribute attribute = GetAttributeFromString(attributeToFind: dtp.Name.Substring(startIndex: 4));

                    if (dirElemFileToModify != null)
                    {
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                                                                     value: dtp.Text,
                                                                     version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue, isMarkedForDeletion: false);
                    }
                }
            }
        }

        btn_InsertTakenDate.Enabled = false;
    }

    /// <summary>
    ///     Handles change in the listview - generates preview or warns user that the file is gone if deleted
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void lvw_FileListEditImages_SelectedIndexChanged(object sender,
                                                                   EventArgs e)
    {
        Logger logger = FrmMainApp.Logger;
        logger.Debug(message: "Starting");
        if (lvw_FileListEditImages.SelectedItems.Count > 0)
        {
            ListView lvw = lvw_FileListEditImages;
            ListViewItem lvi = lvw.SelectedItems[index: 0];

            DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemGUID(GUID: lvi.SubItems[index: lvw.Columns[key: COL_NAME_PREFIX + FileListColumns.GUID]
                                                                                                                        .Index]
                                                                                                    .Text);
            string fileNameWithPath = dirElemFileToModify.FileNameWithPath;

            if (File.Exists(path: fileNameWithPath))
            {
                lvw_EditorFileListImagesGetData();

                pbx_imagePreview.Image = null;
                await HelperExifReadGetImagePreviews.GenericCreateImagePreview(fileNameWithPath: fileNameWithPath,
                                                                               initiator: "FrmEditFileData");
            }
            else
            {
                logger.Debug(message: "File disappeared: " + fileNameWithPath);
                MessageBox.Show(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                        messageBoxName: "mbx_FrmEditFileData_WarningFileDisappeared"),
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Warning);
            }
        }

        logger.Debug(message: "Done");
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
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (frmMainAppInstance != null)
        {
            int directoryElementsCount = DirectoryElements.Count;
            // move data from temp-queue to write-queue
            for (int fileCounter = 0; fileCounter < directoryElementsCount; fileCounter++)
            {
                DirectoryElement dirElemFileToModify = DirectoryElements[fileCounter];
                // this is to prevent code from looping through _all_ the files in a folder pointlessly.
                if (dirElemFileToModify.HasDirtyAttributes(DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue))
                {
                    bool takenAlreadyShifted = false;
                    bool createAlreadyShifted = false;

                    foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                    {
                        if (dirElemFileToModify.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue))
                        {
                            dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                                                                         value: dirElemFileToModify.GetAttributeValueString(attribute: attribute,
                                                                                                                            version: DirectoryElement.AttributeVersion
                                                                                                                                                     .Stage1EditFormIntraTabTransferQueue
                                                                         ),
                                                                         version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                                                                         isMarkedForDeletion: dirElemFileToModify
                                                                            .IsMarkedForDeletion(attribute: attribute,
                                                                                                 version: DirectoryElement.AttributeVersion
                                                                                                                          .Stage1EditFormIntraTabTransferQueue));

                            // remove from Stage1EditFormIntraTabTransferQueue
                            dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue);
                        }

                        // remove from Stage2EditFormReadyToSaveAndMoveToWriteQueue

                        dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue);
                    }

                    ListViewItem lvi = null;
                    try
                    {
                        lvi = frmMainAppInstance.lvw_FileList.FindItemWithText(text: dirElemFileToModify.ItemNameWithoutPath);
                    }
                    catch
                    {
                        // shouldn't happen
                    }

                    if (lvi != null)
                    {
                        HelperGenericFileLocking.FileListBeingUpdated = true;
                        await FileListViewReadWrite.ListViewUpdateRowFromDEStage3ReadyToWrite(lvi: lvi);
                        TaskbarManagerInstance.SetProgressValue(fileCounter + 1, directoryElementsCount);
                        Thread.Sleep(1);
                        HelperGenericFileLocking.FileListBeingUpdated = false;
                    }
                }
            }

            TaskbarManagerInstance.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.NoProgress);
            // re-center map on new data.
            FileListViewMapNavigation.ListViewItemClickNavigate();
        }
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
        foreach (DirectoryElement dirElemFileToModify in DirectoryElements)
        {
            {
                foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                {
                    dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue);

                    dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue);
                }
            }
        }

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
        await HelperExifDataPointInteractions.ExifRemoveLocationData(senderName: "FrmEditFileData");
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
            Control sndr = (Control)sender;
            string senderName = sndr.Name;
            string parentName = sndr.Parent.Name;

            string previousText = "herp-derp";
            string newText = "";

            string fileNameWithoutPath = lvw_FileListEditImages.SelectedItems[index: 0]
                                                               .Text;

            string exifTagStr = sndr.Name.Substring(startIndex: 4);
            ElementAttribute attribute = GetAttributeFromString(attributeToFind: exifTagStr);
            ListView lvw = lvw_FileListEditImages;
            ListViewItem lvi = lvw.SelectedItems[index: 0];

            DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemGUID(GUID: lvi.SubItems[index: lvw.Columns[key: COL_NAME_PREFIX + FileListColumns.GUID]
                                                                                                                        .Index]
                                                                                                    .Text);
            if (dirElemFileToModify != null && dirElemFileToModify.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue))
            {
                previousText = dirElemFileToModify.GetAttributeValueString(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue);
            }

            if (sndr is NumericUpDown nudTextControl)
            {
                if (nudTextControl.Text != NullStringEquivalentBlank)
                {
                    nudTextControl.Text = nudTextControl.Value.ToString();
                }
            }
            else
            {
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
                if (sndr is NumericUpDown nud)
                {
                    string nudTextStr = nud.Text == NullStringEquivalentBlank
                        ? NullStringEquivalentBlank
                        : nud.Value.ToString(provider: CultureInfo.InvariantCulture);

                    if (dirElemFileToModify != null)
                    {
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                                                                     value: nudTextStr,
                                                                     version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                                                     isMarkedForDeletion: dirElemFileToModify
                                                                        .IsMarkedForDeletion(attribute: attribute,
                                                                                             version: DirectoryElement.AttributeVersion
                                                                                                                      .Stage1EditFormIntraTabTransferQueue));
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                                                                     value: nudTextStr,
                                                                     version: DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue,
                                                                     isMarkedForDeletion: dirElemFileToModify
                                                                        .IsMarkedForDeletion(attribute: attribute,
                                                                                             version: DirectoryElement.AttributeVersion
                                                                                                                      .Stage2EditFormReadyToSaveAndMoveToWriteQueue));
                    }

                    // adjust createDate and/or takenDate

                    if (senderName.Contains(value: "TakenDate"))
                    {
                        double shiftTakenDateSeconds =
                            (double)nud_TakenDateSecondsShift.Value +
                            (double)(nud_TakenDateMinutesShift.Value * 60) +
                            (double)(nud_TakenDateHoursShift.Value * 60 * 60) +
                            (double)(nud_TakenDateDaysShift.Value * 60 * 60 * 24);

                        dtp_TakenDate.Value = _origDateValTakenDate.AddSeconds(value: shiftTakenDateSeconds);
                        rbt_TakenDateTimeShift.Checked = shiftTakenDateSeconds != 0;
                    }
                    else if (senderName.Contains(value: "CreateDate"))
                    {
                        double shiftCreateDateSeconds =
                            (double)nud_CreateDateSecondsShift.Value +
                            (double)(nud_CreateDateMinutesShift.Value * 60) +
                            (double)(nud_CreateDateHoursShift.Value * 60 * 60) +
                            (double)(nud_CreateDateDaysShift.Value * 60 * 60 * 24);

                        dtp_CreateDate.Value = _origDateValCreateDate.AddSeconds(value: shiftCreateDateSeconds);
                        rbt_CreateDateTimeShift.Checked = shiftCreateDateSeconds != 0;
                    }
                }
                else
                {
                    // marry up countrycodes and countrynames
                    string sqliteText;
                    if (senderName == "cbx_CountryCode")
                    {
                        sqliteText = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
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
                        sqliteText = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
                            queryWhat: "Country",
                            inputVal: sndr.Text,
                            returnWhat: "ISO_3166_1A3"
                        );
                        if (cbx_CountryCode.Text != sqliteText)
                        {
                            cbx_CountryCode.Text = sqliteText;
                        }
                    }

                    if (dirElemFileToModify != null)
                    {
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                                                                     value: sndr.Text,
                                                                     version: DirectoryElement.AttributeVersion
                                                                                              .Stage1EditFormIntraTabTransferQueue,
                                                                     isMarkedForDeletion: dirElemFileToModify
                                                                        .IsMarkedForDeletion(attribute: attribute,
                                                                                             version: DirectoryElement.AttributeVersion
                                                                                                                      .Stage1EditFormIntraTabTransferQueue));
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                                                                     value: sndr.Text,
                                                                     version: DirectoryElement.AttributeVersion
                                                                                              .Stage2EditFormReadyToSaveAndMoveToWriteQueue,
                                                                     isMarkedForDeletion: dirElemFileToModify
                                                                        .IsMarkedForDeletion(attribute: attribute,
                                                                                             version: DirectoryElement.AttributeVersion
                                                                                                                      .Stage2EditFormReadyToSaveAndMoveToWriteQueue));
                    }
                }

                sndr.Font = new Font(prototype: sndr.Font, newStyle: FontStyle.Bold);
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
    private void any_nud_Enter(object sender,
                               EventArgs e)
    {
        AcceptButton = null;
    }

    /// <summary>
    ///     Reinstates AcceptButton. By this point Validation auto-corrects the value if user was derp.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void any_nud_Leave(object sender,
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

            nud_TakenDateDaysShift.Text = NullStringEquivalentZero;
            nud_TakenDateHoursShift.Text = NullStringEquivalentZero;
            nud_TakenDateMinutesShift.Text = NullStringEquivalentZero;
            nud_TakenDateSecondsShift.Text = NullStringEquivalentZero;
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

            nud_CreateDateDaysShift.Text = NullStringEquivalentZero;
            nud_CreateDateHoursShift.Text = NullStringEquivalentZero;
            nud_CreateDateMinutesShift.Text = NullStringEquivalentZero;
            nud_CreateDateSecondsShift.Text = NullStringEquivalentZero;
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

        ListView lvw = lvw_FileListEditImages;
        ListViewItem lvi = lvw.SelectedItems[index: 0];

        DirectoryElement dirElemFileToModify = DirectoryElements.FindElementByItemGUID(GUID: lvi.SubItems[index: lvw.Columns[key: COL_NAME_PREFIX + FileListColumns.GUID]
                                                                                                                    .Index]
                                                                                                .Text);

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
                    if (dirElemFileToModify != null)
                    {
                        ElementAttribute attribute = GetAttributeFromString(attributeToFind: dtp.Name.Substring(startIndex: 4));
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                                                                     value: dtp.Text,
                                                                     version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                                                     isMarkedForDeletion: dirElemFileToModify
                                                                        .IsMarkedForDeletion(attribute: attribute,
                                                                                             version: DirectoryElement.AttributeVersion
                                                                                                                      .Stage1EditFormIntraTabTransferQueue));
                    }
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
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        ttp.SetToolTip(control: pbx_OffsetTimeInfo,
                       caption: HelperDataLanguageTZ.DataReadDTObjectText(
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