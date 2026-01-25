using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using GeoTagNinja.View.ListView;
using Microsoft.WindowsAPICodePack.Taskbar;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using TimeZoneConverter;
using static GeoTagNinja.FrmMainApp;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;
using static GeoTagNinja.Helpers.HelperExifReadExifData;
using static GeoTagNinja.Helpers.HelperGenericAncillaryListsArrays;
using static GeoTagNinja.Model.SourcesAndAttributes;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using Themer = WinFormsDarkThemerNinja.Themer;

namespace GeoTagNinja;

public partial class FrmEditFileData : Form
{
    private static bool _tzChangedByApi;

    #region Variables

    private static bool _frmEditFileDataNowLoadingFileData;

    #endregion

    /// <summary>
    ///     This Form provides an interface for the user to edit various bits of Exif data in images.
    /// </summary>
    public FrmEditFileData()
    {
        Log.Info(message: "Starting");
        KeyPreview = true; // send keypress to the Form first

        InitializeComponent();
        // the custom logic is ugly af so no need to be pushy about it in light mode.
        if (!HelperVariables.UserSettingUseDarkMode)
        {
            tcr_EditData.DrawMode = TabDrawMode.Normal;
            lvw_FileListEditImages.OwnerDraw = false;
        }

        Log.Trace(message: "InitializeComponent OK");

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
        Log.Info(message: "Starting");
        Log.Trace(message: "Defaults Starting");
        _frmEditFileDataNowLoadingFileData = true;

        ClearDirectoryElementsTemporaryData();
        EnableDateControlsAndSetDropdownDefaultValues();

        // we select the first item in the listview (while ensuring there is _something_ in the lvw.)
        if (lvw_FileListEditImages.Items.Count > 0)
        {
            Log.Trace(message: "ListViewSelect Start");

            lvw_FileListEditImages.Items[index: 0]
                                  .Selected = true;

            // actually if it's just one file i don't want this to be actively selectable
            // also don't enable the "next" button.
            if (lvw_FileListEditImages.Items.Count == 1)
            {
                lvw_FileListEditImages.Enabled = false;
                btn_ApplyAndNext.Enabled = false;
            }

            Log.Trace(message: "ListViewSelect Done");
        }

        Themer.ApplyThemeToControl(
            control: this,
            themeStyle: HelperVariables.UserSettingUseDarkMode ?
            Themer.ThemeStyle.Custom :
            Themer.ThemeStyle.Default
            );

        _frmEditFileDataNowLoadingFileData = false; // techinically this is redundant here
        Log.Info(message: "Done");
    }

    /// <summary>
    /// We set various controls to enabled/disabled upon loading this Form
    /// </summary>
    private void EnableDateControlsAndSetDropdownDefaultValues()
    {
        Log.Trace(message: "Setting Dropdown defaults");
        // Deal with Dates
        // TakenDate
        dtp_TakenDate.Enabled = true;
        nud_TakenDateDaysShift.Enabled = false;
        nud_TakenDateHoursShift.Enabled = false;
        nud_TakenDateMinutesShift.Enabled = false;
        nud_TakenDateSecondsShift.Enabled = false;

        dtp_TakenDate.CustomFormat =
            $"{CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}";

        // CreateDate
        dtp_CreateDate.Enabled = true;
        nud_CreateDateDaysShift.Enabled = false;
        nud_CreateDateHoursShift.Enabled = false;
        nud_CreateDateMinutesShift.Enabled = false;
        nud_CreateDateSecondsShift.Enabled = false;
        dtp_CreateDate.CustomFormat =
            $"{CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}";
        ReturnControlText(
            control: this, senderForm: this);

        // fills the countries box

        foreach (string country in GetCountries())
        {
            _ = cbx_Country.Items.Add(item: country);
        }

        // fills the country codes box
        foreach (string countryCode in
                 GetCountryCodes())
        {
            _ = cbx_CountryCode.Items.Add(item: countryCode);
        }

        // load TZ-CBX
        foreach (string timezone in GetTimeZones())
        {
            _ = cbx_OffsetTime.Items.Add(item: timezone);
        }

        Log.Trace(message: "Setting Dropdown defaults - Done");
    }

    /// <summary>
    /// Clears all the temporary ("stage 1" and "stage 2" data) for all the DEs
    /// </summary>
    private static void ClearDirectoryElementsTemporaryData()
    {
        Log.Trace(
            message:
            "Emptying FrmMainApp.Stage1EditFormIntraTabTransferQueue + Stage2EditFormReadyToSaveAndMoveToWriteQueue");

        foreach ((DirectoryElement dirElemFileToModify, ElementAttribute attribute) in from DirectoryElement dirElemFileToModify in DirectoryElements
                                                                                       from ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(
                                                                   enumType: typeof(ElementAttribute))
                                                                                       select (dirElemFileToModify, attribute))
        {
            dirElemFileToModify.RemoveAttributeValue(
                attribute: attribute,
                version: DirectoryElement.AttributeVersion
                                         .Stage1EditFormIntraTabTransferQueue);

            // also empty the "original data" table

            dirElemFileToModify.RemoveAttributeValue(
                attribute: attribute,
                version: DirectoryElement.AttributeVersion
                                         .Stage2EditFormReadyToSaveAndMoveToWriteQueue);
        }

        Log.Trace(
           message:
           "Emptying FrmMainApp.Stage1EditFormIntraTabTransferQueue + Stage2EditFormReadyToSaveAndMoveToWriteQueue - Done");

    }

    /// <summary>
    ///     This method is responsible for retrieving the text values for the Controls in the Form.
    ///     For labels, buttons, etc., this is their "language" label (e.g., "Latitude").
    ///     For textboxes and similar controls, this is the value (e.g., "51.002").
    ///     The method also handles the assignment of these values to the corresponding controls.
    /// </summary>
    private void ShowDEDataInRelevantControls(DirectoryElement dirElemFileToModify)
    {
        Logger log = Log;
        Log.Info(message: "Starting");

        _frmEditFileDataNowLoadingFileData = true;

        btn_InsertFromTakenDate.Enabled = false;

        Log.Trace(message: "Assinging Labels Start");
        HelperNonStatic helperNonstatic = new();

        /// The types of Controls for which values _do not_ come from the selected DE
        List<Type> lstControlTypesNoDEValue =
        [
            typeof(Label),
            typeof(GroupBox),
            typeof(Button),
            typeof(CheckBox),
            typeof(TabPage),
            typeof(RadioButton)
        ];

        /// The types of Controls for which values _do_ come from the selected DE
        List<Type> lstControlTypesWithDEValue =
        [
            typeof(TextBox),
            typeof(ComboBox),
            typeof(DateTimePicker),
            typeof(NumericUpDown)
        ];

        IEnumerable<Control> controls = helperNonstatic.GetAllControls(control: this);

        /// For each control we attempt to get their values
        foreach (Control control in controls)
        {
            if (lstControlTypesNoDEValue.Contains(item: control.GetType()) ||
                lstControlTypesWithDEValue.Contains(item: control.GetType()))
            {
                // this is for debugging only (particularly here, that is.)
                string debugItemName = control.Name;
                string controlNameWithoutTypeIdentifier;

                try
                {
                    Log.Trace(message: $"control: {debugItemName} (Type: {control.GetType().Name}) - Starting.");

                    if (lstControlTypesNoDEValue.Contains(item: control.GetType()))
                    {
                        // gets logged inside.
                        ReturnControlText(control: control, senderForm: this);
                    }

                    else if (lstControlTypesWithDEValue.Contains(item: control.GetType()))
                    {
                        // this gets the name of the Control without the type
                        // (e.g. tbx_SomethingAttrib becomes SomethingAttrib)
                        controlNameWithoutTypeIdentifier = control.Name.Substring(HelperVariables.COL_NAME_PREFIX.Length);

                        // get the ElementAttribute
                        ElementAttribute attribute =
                            GetElementAttributesElementAttribute(
                                attributeToFind: controlNameWithoutTypeIdentifier);

                        // get the AttributeVersion maxAttributeVersion
                        string stringValueOfControl = NullStringEquivalentBlank;
                        DirectoryElement.AttributeVersion maxAttributeVersion =
                            GetDEAttributeMaxAttributeVersion(
                                directoryElement: dirElemFileToModify,
                                attribute: attribute);

                        // get the data in orig-type format
                        IConvertible dataInDirectoryElement = GetDataInDEForAttribute(
                            directoryElement: dirElemFileToModify,
                            attribute: attribute,
                            maxAttributeVersion: maxAttributeVersion);

                        // if we have data for the Attribute we convert it to string
                        if (dataInDirectoryElement != null &&
                            !string.IsNullOrWhiteSpace(
                                value: dataInDirectoryElement.ToString()))
                        {
                            stringValueOfControl =
                                dataInDirectoryElement.ToString(
                                    provider: CultureInfo.InvariantCulture);
                            control.Font = new Font(prototype: control.Font,
                                newStyle: FontStyle.Regular);

                            // technically this doesn't belong here as an overall-enabler but the code inside checks for details
                            EnableDateTimeItems(control: control);
                        }
                        else
                        {
                            // if it's none of the further below then make the control be just blank.
                            control.Text = NullStringEquivalentBlank;

                            // okay for gbx_*Date && !NUD -> those are DateTimePickers.
                            // If they are NULL then there is either no TakenDate or no CreateDate so the actual controls will have to be disabled.
                            // technically this doesn't belong here as an overall-enabler but the code inside checks for details
                            DisableDateTimeItems(control: control);
                        }

                        // wrangle the data
                        if (dataInDirectoryElement != null)
                        {
                            // stick into DE2 ("pending save") - this is to see if the data has changed later.

                            if (control is not NumericUpDown nud)
                            {
                                // this is related to storing the default DateTimes for TakenDate and CreateDate
                                // what we also need to do here is account for any copy-paste shifts.
                                if (control is DateTimePicker dtp)
                                {
                                    HandleDateTimePickerTimeShift(dtp: dtp,
                                        directoryElement:
                                        dirElemFileToModify,
                                        control: control,
                                        maxAttributeVersion:
                                        maxAttributeVersion);
                                }
                                else
                                {
                                    control.Text = stringValueOfControl;
                                }

                                Log.Trace(message:
                                    $"control: {control.Name} - Adding to Stage2EditFormReadyToSaveAndMoveToWriteQueue");

                                dirElemFileToModify.SetAttributeValueAnyType(
                                    attribute: attribute,
                                    value: stringValueOfControl,
                                    version: DirectoryElement.AttributeVersion
                                                             .Stage2EditFormReadyToSaveAndMoveToWriteQueue,
                                    isMarkedForDeletion: false);
                            }
                            else // if (control is NumericUpDown nud)
                            {
                                nud.Value = Convert.ToDecimal(
                                    value: stringValueOfControl,
                                    provider: CultureInfo.InvariantCulture);

                                if (nud.Name.EndsWith(value: "Shift") &&
                                    nud.Value != 0)
                                {
                                    if (nud.Name.Substring(HelperVariables.COL_NAME_PREFIX.Length)
                                           .StartsWith(
                                                value: TakenOrCreated.Taken.ToString()))
                                    {
                                        rbt_TakenDateTimeShift.Checked = true;
                                    }
                                    else if (nud.Name.Substring(HelperVariables.COL_NAME_PREFIX.Length)
                                                .StartsWith(
                                                     value: TakenOrCreated.Created
                                                                          .ToString()))
                                    {
                                        rbt_CreateDateTimeShift.Checked = true;
                                    }
                                }

                                dirElemFileToModify.SetAttributeValueAnyType(
                                    attribute: attribute,
                                    value: nud.Value.ToString(
                                        provider: CultureInfo.InvariantCulture),
                                    version: DirectoryElement.AttributeVersion
                                                             .Stage2EditFormReadyToSaveAndMoveToWriteQueue,
                                    isMarkedForDeletion: false);

                                nud.Text = nud.Value.ToString();
                            }

                            // these don't have a "simple" text solution
                            if (control.Name == "cbx_CountryCode") // this will also fill in Country
                            {
                                string countryCodeInDirectoryElement =
                                    stringValueOfControl;
                                if (countryCodeInDirectoryElement != null &&
                                    !string.IsNullOrEmpty(
                                        value: countryCodeInDirectoryElement.ToString(
                                            provider: CultureInfo.InvariantCulture)))
                                {
                                    cbx_CountryCode.Text =
                                        countryCodeInDirectoryElement.ToString(
                                            provider: CultureInfo
                                               .InvariantCulture);

                                    string sqliteText = HelperDataLanguageTZ
                                       .DataReadDTCountryCodesNames(
                                            queryWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A3,
                                            inputVal: cbx_CountryCode.Text,
                                            returnWhat: LanguageMappingQueryOrReturnWhat.Country);
                                    cbx_Country.Text = sqliteText;
                                }
                            }
                            else if (control.Name == "cbx_OffsetTime")
                            {
                                // don't select anything in this case. see longer comment below.
                                ComboBox cbx = control as ComboBox;
                                cbx.SelectedIndex = -1;
                            }

                            // Leaving this commented out on purpose...
                            // While the code works fine it's a source of confusion as Uses-DST isn't currently stored...
                            // anywhere and so upon reopening the Form this could lead to undesired results. ...
                            // I'll ponder on some reasonable ways to handle it if there's interest.
                            //else if (control.Name == "cbx_OffsetTime")
                            //{
                            //    // attempt to convert offset to a member of the list
                            //    string offsetTimeInDirectoryElement =
                            //        dirElemFileToModify.GetAttributeValueAsString(
                            //            attribute: ElementAttribute.OffsetTime,
                            //            version: maxAttributeVersion);
                            //    if (offsetTimeInDirectoryElement != null &&
                            //        !string.IsNullOrEmpty(
                            //            value: offsetTimeInDirectoryElement.ToString(
                            //                provider: CultureInfo.InvariantCulture)))
                            //    {
                            //        cbx_OffsetTime.Text =
                            //            GetFirstMatchingTzData(
                            //                offsetTimeToMatch:
                            //                offsetTimeInDirectoryElement);
                            //    }
                            //}
                        }

                        if (maxAttributeVersion != DirectoryElement.AttributeVersion.Original)
                        {
                            control.Font =
                                new Font(prototype: control.Font,
                                    newStyle: FontStyle.Bold);
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                Log.Trace(message: $"control: {control.Name} (Type: {control.GetType()
                                                                      .Name}) - Done.");
            }
        }

        Log.Trace(message: "Assinging Labels Done");

        // done load
        Log.Debug(message: "Done");
        _frmEditFileDataNowLoadingFileData = false;
        return;

        void DisableDateTimeItems(Control control)
        {
            if (control.Parent.Name.StartsWith(value: "gbx_") &&
                control.Parent.Name.EndsWith(value: "Date"))
            {
                if (control is not NumericUpDown nud)
                {
                    if (control.Parent.Name == "gbx_TakenDate")
                    {
                        EnableSpecificControlAndDisableOthers(
                            parentControl: gbx_TakenDate,
                            controlsToEnable: [btn_InsertTakenDate],
                            controlsToDisable: helperNonstatic
                                              .GetAllControls(control: gbx_TakenDate)
                                              .Where(predicate: c =>
                                                   c != btn_InsertTakenDate)
                                              .ToList());
                    }
                    else if (control.Parent.Name == "gbx_CreateDate")
                    {
                        EnableSpecificControlAndDisableOthers(
                            parentControl: gbx_CreateDate,
                            controlsToEnable: [btn_InsertCreateDate],
                            controlsToDisable: helperNonstatic
                                              .GetAllControls(control: gbx_CreateDate)
                                              .Where(predicate: c =>
                                                   c != btn_InsertCreateDate)
                                              .ToList());
                    }
                }

                else // control is nud
                {
                    nud.Value = NullIntEquivalent;
                    nud.Text = NullStringEquivalentZero;
                }
            }
        }

        void EnableDateTimeItems(Control control)
        {
            // if this is a TakenDate or CreateDate -related
            if (control.Parent.Name.StartsWith(value: "gbx_") &&
                control.Parent.Name.EndsWith(value: "Date") &&
                control is not NumericUpDown)
            {
                // this code block deals with enabling and disabling the Controls on whether there is data behind.
                if (control.Parent.Name == "gbx_TakenDate")
                {
                    IEnumerable<Control> controlsInsideGbx_TakenDate =
                        helperNonstatic.GetAllControls(
                            control: gbx_TakenDate);
                    List<Control> controlsToEnable = [];
                    List<Control> controlsToDisable = [btn_InsertTakenDate];
                    foreach (Control controlInsideGbx_TakenDate in controlsInsideGbx_TakenDate)
                    {
                        if (controlInsideGbx_TakenDate != btn_InsertTakenDate)
                        {
                            controlsToEnable.Add(
                                item: controlInsideGbx_TakenDate);
                        }
                    }

                    EnableSpecificControlAndDisableOthers(
                        parentControl: gbx_TakenDate,
                        controlsToEnable: controlsToEnable,
                        controlsToDisable: controlsToDisable);
                }
                else if (control.Parent.Name == "gbx_CreateDate")
                {
                    IEnumerable<Control> controlsInsideGbx_CreateDate =
                        helperNonstatic.GetAllControls(
                            control: gbx_CreateDate);
                    List<Control> controlsToEnable = [];
                    List<Control> controlsToDisable = [btn_InsertCreateDate];
                    foreach (Control controlInsideGbx_CreateDate in
                             controlsInsideGbx_CreateDate)
                    {
                        if (controlInsideGbx_CreateDate != btn_InsertCreateDate)
                        {
                            controlsToEnable.Add(
                                item: controlInsideGbx_CreateDate);
                        }
                    }

                    EnableSpecificControlAndDisableOthers(
                        parentControl: gbx_CreateDate,
                        controlsToEnable: controlsToEnable,
                        controlsToDisable: controlsToDisable);
                }
            }
        }

        void HandleDateTimePickerTimeShift(DateTimePicker dtp,
                                           DirectoryElement directoryElement,
                                           Control control,
                                           DirectoryElement.AttributeVersion
                                               maxAttributeVersion)
        {
            DateTime directoryElementDateTimeOriginal = default;
            DateTime directoryElementDateTimeMaxVersion = default;

            // Update ref ticket #147:
            // The logic here was a bit flawed. What this does/should do is that we (should) take the Original value,
            // ... also take the amount of time already shifted and add the two together, if and only if the time shifted !=0.
            // ... otherwise we'd always return to the Original value even when it's already been modified. 

            int totalShiftedSeconds = 0;

            Dictionary<DateTimePicker, ElementAttribute> dtpMapping = new()
            {
                { dtp_TakenDate, ElementAttribute.TakenDate },
                { dtp_CreateDate, ElementAttribute.CreateDate }
            };
            if (directoryElement.GetAttributeValue<DateTime>(
                    attribute: dtpMapping[key: dtp],
                    version: DirectoryElement.AttributeVersion.Original,
                    notFoundValue: null) != null)
            {
                directoryElementDateTimeOriginal = (DateTime)directoryElement.GetAttributeValue<DateTime>(
                    attribute: dtpMapping[key: dtp],
                    version: DirectoryElement.AttributeVersion.Original,
                    notFoundValue: null);

                directoryElementDateTimeMaxVersion = (DateTime)directoryElement.GetAttributeValue<DateTime>(
                    attribute: dtpMapping[key: dtp],
                    version: maxAttributeVersion,
                    notFoundValue: null);

                totalShiftedSeconds =
                    ShiftTimeForDateTimePicker(
                        whatToShift: TimeShiftTypes.TakenDate,
                        dirElemFileToModify: directoryElement);

                dtp.Value = totalShiftedSeconds != 0
                    ? directoryElementDateTimeOriginal.AddSeconds(value: totalShiftedSeconds)
                    : directoryElementDateTimeMaxVersion;
            }

            Log.Trace(message: $"control: {control.Name} - Updating DateTimePicker");
            if (maxAttributeVersion !=
                DirectoryElement.AttributeVersion.Original ||
                totalShiftedSeconds != 0)
            {
                dtp.Font =
                    new Font(prototype: dtp.Font,
                        newStyle: FontStyle.Bold);
            }
        }
    }

    /// <summary>
    ///     Retrieves the value of a specified attribute from a given directory element.
    /// </summary>
    /// <param name="directoryElement">The directory element from which to retrieve the attribute value.</param>
    /// <param name="attribute">The attribute whose value is to be retrieved.</param>
    /// <param name="maxAttributeVersion">The maximum version of the attribute to consider when retrieving the value.</param>
    /// <returns>
    ///     The value of the specified attribute from the given directory element as an IConvertible, or null if the
    ///     attribute value is equivalent to the null equivalent for its type.
    /// </returns>
    private static IConvertible GetDataInDEForAttribute(DirectoryElement directoryElement,
        ElementAttribute attribute,
        DirectoryElement.AttributeVersion
            maxAttributeVersion)
    {
        IConvertible returnDataInDirectoryElement = null;

        Type typeOfAttribute = GetElementAttributesType(attributeToFind: attribute);
        if (typeOfAttribute == typeof(string))
        {
            returnDataInDirectoryElement = directoryElement.GetAttributeValueAsString(
                attribute: attribute,
                version: maxAttributeVersion, nowSavingExif: false);

            if (returnDataInDirectoryElement != null &&
                string.IsNullOrEmpty(value: returnDataInDirectoryElement.ToString()))
            {
                returnDataInDirectoryElement = null;
            }
        }
        else if (typeOfAttribute == typeof(int))
        {
            returnDataInDirectoryElement = directoryElement.GetAttributeValue<int>(
                attribute: attribute,
                version: maxAttributeVersion);

            try
            {
                if ((int)returnDataInDirectoryElement == NullIntEquivalent)
                {
                    returnDataInDirectoryElement = null;
                }
            }
            catch
            {
                returnDataInDirectoryElement = null;
            }
        }
        else if (typeOfAttribute == typeof(double))
        {
            returnDataInDirectoryElement = directoryElement.GetAttributeValue<double>(
                attribute: attribute,
                version: maxAttributeVersion);

            try
            {
                if (returnDataInDirectoryElement != null &&
                    (double)returnDataInDirectoryElement == NullDoubleEquivalent)
                {
                    returnDataInDirectoryElement = null;
                }
            }
            catch
            {
                returnDataInDirectoryElement = null;
            }
        }
        else if (typeOfAttribute == typeof(DateTime))
        {
            returnDataInDirectoryElement = directoryElement.GetAttributeValue<DateTime>(
                attribute: attribute,
                version: maxAttributeVersion);

            try
            {
                if (returnDataInDirectoryElement != null &&
                    (DateTime)returnDataInDirectoryElement == NullDateTimeEquivalent)
                {
                    returnDataInDirectoryElement = null;
                }
            }
            catch
            {
                returnDataInDirectoryElement = null;
            }
        }

        return returnDataInDirectoryElement;
    }

    /// <summary>
    ///     Enables specific controls and disables others within a given parent control.
    /// </summary>
    /// <param name="parentControl">The parent control that contains the controls to be enabled or disabled.</param>
    /// <param name="controlsToEnable">A list of controls to be enabled.</param>
    /// <param name="controlsToDisable">A list of controls to be disabled.</param>
    /// <remarks>
    ///     This method iterates over all controls within the given parent control. If a control is in the list of controls to
    ///     be disabled, it is disabled. If a control is in the list of controls to be enabled, it is enabled.
    /// </remarks>
    private void EnableSpecificControlAndDisableOthers(Control parentControl,
        List<Control> controlsToEnable,
        List<Control> controlsToDisable)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> controls =
            helperNonstatic.GetAllControls(control: parentControl);
        foreach (Control item in controls)
        {
            if (controlsToDisable.Contains(item: item))
            {
                item.Enabled = false;
            }

            if (controlsToEnable.Contains(item: item))
            {
                item.Enabled = true;
            }
        }
    }

    /// <summary>
    ///     Calculates the total time shift in seconds for a DateTimePicker control.
    /// </summary>
    /// <param name="whatToShift">Specifies whether the CreateDate or TakenDate should be shifted.</param>
    /// <param name="dirElemFileToModify">The DirectoryElement object that contains the attribute values for the time shift.</param>
    /// <returns>Returns the total time shift in seconds.</returns>
    private static int ShiftTimeForDateTimePicker(TimeShiftTypes whatToShift,
        DirectoryElement dirElemFileToModify)
    {
        DirectoryElement.AttributeVersion attributeVersion =
            DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue;

        int shiftedDays = (int)dirElemFileToModify.GetAttributeValue<int>(
            attribute: whatToShift == TimeShiftTypes.CreateDate
                ? ElementAttribute.CreateDateDaysShift
                : ElementAttribute.TakenDateDaysShift,
            version: attributeVersion,
            notFoundValue: 0);
        int shiftedHours = (int)dirElemFileToModify.GetAttributeValue<int>(
            attribute: whatToShift == TimeShiftTypes.CreateDate
                ? ElementAttribute.CreateDateHoursShift
                : ElementAttribute.TakenDateHoursShift,
            version: attributeVersion,
            notFoundValue: 0);
        int shiftedMinutes = (int)dirElemFileToModify.GetAttributeValue<int>(
            attribute: whatToShift == TimeShiftTypes.CreateDate
                ? ElementAttribute.CreateDateMinutesShift
                : ElementAttribute.TakenDateMinutesShift,
            version: attributeVersion,
            notFoundValue: 0);
        int shiftedSeconds = (int)dirElemFileToModify.GetAttributeValue<int>(
            attribute: whatToShift == TimeShiftTypes.CreateDate
                ? ElementAttribute.CreateDateSecondsShift
                : ElementAttribute.TakenDateSecondsShift,
            version: attributeVersion,
            notFoundValue: 0);
        int totalShiftedSeconds = shiftedSeconds +
                                  (shiftedMinutes * 60) +
                                  (shiftedHours * 60 * 60) +
                                  (shiftedDays * 60 * 60 * 24);
        return totalShiftedSeconds;
    }

    /// <summary>
    ///     Retrieves the highest version of a specific attribute in a given directory element.
    /// </summary>
    /// <param name="directoryElement">The directory element to inspect.</param>
    /// <param name="attribute">The attribute to check for versions.</param>
    /// <returns>
    ///     The highest version of the specified attribute in the directory element. Returns null if no version of the
    ///     attribute is found.
    /// </returns>
    private static DirectoryElement.AttributeVersion GetDEAttributeMaxAttributeVersion(
        DirectoryElement directoryElement,
        ElementAttribute attribute)
    {
        List<DirectoryElement.AttributeVersion> relevantAttributeVersions =
        [
            // DO NOT reorder!
            DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
            DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
            DirectoryElement.AttributeVersion.Original
        ];

        DirectoryElement.AttributeVersion maxAttributeVersion =
            relevantAttributeVersions.FirstOrDefault(
                predicate: attributeVersion =>
                    directoryElement.HasSpecificAttributeWithVersion(attribute: attribute,
                        version: attributeVersion));
        return maxAttributeVersion;
    }


    #region Object events

    /// <summary>
    ///     Pulls data for the various "Get (All) From Web" buttons depending which actual button has been pressed.
    ///     The TLDR logic is that if it's not the "All" button then we only read the currently active file else we read all
    ///     ...but ofc the currently not visible files' data doesn't show to the user so that goes into the holding tables.
    /// </summary>
    /// <param name="sender">The object that has been interacted with</param>
    /// <param name="e">Unused</param>
    private void btn_getFromWeb_Click(object sender, EventArgs e)
    {
        // reset OperationAPIReturnedOKResponse just in case.
        HelperVariables.OperationAPIReturnedOKResponse = true;
        switch (((Button)sender).Name)
        {
            case "btn_getFromWeb_Toponomy":
                GetToponomyDataFromLocalStorage(
                    dirElemFileToModify: lvw_FileListEditImages.SelectedItems[0].Tag as DirectoryElement,
                    dirElementIsTheCurrentlySelectedDE: true
                    );
                break;
            case "btn_getAllFromWeb_Toponomy":
                foreach (ListViewItem lvi in from ListViewItem lvi in lvw_FileListEditImages.Items
                                             let directoryElement = lvi.Tag as DirectoryElement
                                             select lvi)
                {
                    GetToponomyDataFromLocalStorage(
                        dirElemFileToModify: lvi.Tag as DirectoryElement,
                        dirElementIsTheCurrentlySelectedDE: lvi == lvw_FileListEditImages.SelectedItems[0]
                        );

                    lvi.ForeColor = Color.Red;
                }

                break;
            default:
                // took me a while to understand my own code.
                // what we are doing here is that we are trying to tell the user (and by proxy, the developer)
                // that something other than the two buttons defined above have been pressed.
                Themer.ShowMessageBox(
                    message: HelperControlAndMessageBoxHandling.ReturnControlText(
                        controlName: "mbx_FrmEditFileData_ErrorInvalidSender",
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox) +
                        Environment.NewLine + $"{((Button)sender).Name}",
                    icon: MessageBoxIcon.Error,
                    buttons: MessageBoxButtons.OK);
                break;
        }

        // show a messagebox to inform if things have been okay
        string messageBoxName = HelperVariables.OperationAPIReturnedOKResponse
               ? "mbx_FrmEditFileData_InfoDataUpdated"
               : "mbx_FrmEditFileData_ErrorAPIError";

        MessageBoxIcon messageBoxIcon = HelperVariables.OperationAPIReturnedOKResponse
               ? MessageBoxIcon.Information
               : MessageBoxIcon.Error;

        Themer.ShowMessageBox(
            message: HelperControlAndMessageBoxHandling.ReturnControlText(
                controlName: messageBoxName,
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                    icon: messageBoxIcon,
                    buttons: MessageBoxButtons.OK);
    }


    /// <summary>
    ///     Pulls data from local storage and fills up the listView and fills the Controls and DataTable.
    /// </summary>
    /// <param name="dirElemFileToModify">The DirectoryElement for which we want to pull the data</param>
    /// <param name="dirElementIsTheCurrentlySelectedDE">Whether this is the currently selected ListViewItem</param>
    /// <remarks>dirElementIsTheCurrentlySelectedDE matters because we don't want to update the various controls for items not
    /// curently selected so we keep a tally of that</remarks>
    private void GetToponomyDataFromLocalStorage(
        DirectoryElement dirElemFileToModify,
        bool dirElementIsTheCurrentlySelectedDE)
    {
        // Can't leave it null because it's updated in various IFs and C# perceives it as uninitialised.
        DateTime createDate = NullDateTimeEquivalent;

        // This is a datatable to store all existing toponomy data either pulled from sql or
        // the web for the session
        DataTable dtLocallyStoredToponomyData = new();

        string strGPSLatitude = NullStringEquivalentBlank;
        string strGPSLongitude = NullStringEquivalentBlank;
        HelperVariables.CurrentAltitudeAsString = NullStringEquivalentBlank;

        // we attempt to ensure there is lat/long value (+ altitude)
        if (dirElementIsTheCurrentlySelectedDE)
        {
            if (nud_GPSLatitude.Text != "" && nud_GPSLongitude.Text != "")
            {
                strGPSLatitude = nud_GPSLatitude.Value.ToString(provider: CultureInfo.InvariantCulture);
                strGPSLongitude = nud_GPSLongitude.Value.ToString(provider: CultureInfo.InvariantCulture);

                HelperVariables.CurrentAltitudeAsString = nud_GPSAltitude.Text.ToString(provider: CultureInfo.InvariantCulture);
            }
        }
        else
        {
            strGPSLatitude = dirElemFileToModify.GetAttributeValueAsString(
                attribute: ElementAttribute.GPSLatitude,
                version: dirElemFileToModify.GetMaxAttributeVersion(
                    attribute: ElementAttribute.GPSLatitude),
                notFoundValue: null);

            strGPSLongitude = dirElemFileToModify.GetAttributeValueAsString(
                attribute: ElementAttribute.GPSLongitude,
                version: dirElemFileToModify.GetMaxAttributeVersion(
                    attribute: ElementAttribute.GPSLongitude),
                notFoundValue: null);

            HelperVariables.CurrentAltitudeAsString = dirElemFileToModify.GetAttributeValueAsString(
                attribute: ElementAttribute.GPSAltitude,
                version: dirElemFileToModify.GetMaxAttributeVersion(
                    attribute: ElementAttribute.GPSAltitude),
                notFoundValue: null);

            string strCreateDate = dirElemFileToModify.GetAttributeValueAsString(
                attribute: ElementAttribute.CreateDate,
                version: dirElemFileToModify.GetMaxAttributeVersion(
                    attribute: ElementAttribute.CreateDate),
                notFoundValue: null);

            _ = DateTime.TryParse(
               s: strCreateDate.ToString(provider: CultureInfo.InvariantCulture),
               result: out createDate);
        }

        // if there is a lat + long we try to read the toponomy data belonging to it from sql and web
        if (!string.IsNullOrEmpty(strGPSLatitude) && !string.IsNullOrEmpty(strGPSLongitude))
        {
            dtLocallyStoredToponomyData =
                HelperExifReadExifData.DTFromAPIExifGetToponomyFromWebOrSQL(
                    lat: strGPSLatitude,
                    lng: strGPSLongitude,
                    fileNameWithoutPath: dirElemFileToModify.ItemNameWithoutPath);
        }
        // for debugging's sake, the "all other files" ends here/above.

        /// A list (should be dict) of values that belong to the selected lat/lng combination (ie country code, country etc.)
        List<(ElementAttribute attribute, string toponomyOverwriteVal)>
            locallyStoredToponomyDataForLatLng = [];

        /// Value of the TimeZone as string. 
        string TZ = string.Empty;

        // Exit if no valid data.
        if (dtLocallyStoredToponomyData == null || dtLocallyStoredToponomyData.Rows.Count <= 0)
        {
            return;
        }


        try
        {
            locallyStoredToponomyDataForLatLng.Add(item: (
                ElementAttribute.CountryCode,
                $"{dtLocallyStoredToponomyData.Rows[index: 0][columnName: DefaultEnglishNamesToColumnHeaders[HelperExifReadExifData.GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode, true)]]}"));
            locallyStoredToponomyDataForLatLng.Add(item: (
                ElementAttribute.Country,
                $"{dtLocallyStoredToponomyData.Rows[index: 0][columnName: DefaultEnglishNamesToColumnHeaders[HelperExifReadExifData.GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Country, true)]]}"));
            locallyStoredToponomyDataForLatLng.Add(item: (
                ElementAttribute.City,
                $"{dtLocallyStoredToponomyData.Rows[index: 0][columnName: DefaultEnglishNamesToColumnHeaders[HelperExifReadExifData.GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.City, true)]]}"));
            locallyStoredToponomyDataForLatLng.Add(item: (
                ElementAttribute.State,
                $"{dtLocallyStoredToponomyData.Rows[index: 0][columnName: DefaultEnglishNamesToColumnHeaders[HelperExifReadExifData.GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.State, true)]]}"));
            locallyStoredToponomyDataForLatLng.Add(item: (
                ElementAttribute.Sublocation,
                $"{dtLocallyStoredToponomyData.Rows[index: 0][columnName: DefaultEnglishNamesToColumnHeaders[HelperExifReadExifData.GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation, true)]]}"));
            locallyStoredToponomyDataForLatLng.Add(item: (
                ElementAttribute.GPSAltitude,
                $"{dtLocallyStoredToponomyData.Rows[index: 0][columnName: DefaultEnglishNamesToColumnHeaders[HelperExifReadExifData.GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude, true)]]}"));

            TZ = $"{dtLocallyStoredToponomyData.Rows[index: 0][columnName: DefaultEnglishNamesToColumnHeaders[HelperExifReadExifData.GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId, true)]]}";
        }

        catch (Exception ex)
        {
            // If we get here tbh I f..d up the naming of the table cols but the app shouldn't crash
            // Ref ticket #197
            Debug.Print(ex.Message);
        }

        // If this is dirElementIsTheCurrentlySelectedDE we update the relevant Controls
        if (dirElementIsTheCurrentlySelectedDE)
        {
            _ = DateTime.TryParse(
               s: tbx_CreateDate.Text.ToString(
                   provider: CultureInfo.InvariantCulture), result: out createDate);

            SetTimeZoneComboBoxForCurrentlySelectedDE(
                createDate: createDate,
                toponomyOverwrites: locallyStoredToponomyDataForLatLng,
                TZ: TZ,
                tzStartInt: 18);

            Dictionary<ElementAttribute, Control> controlPairs = new() {
                    { ElementAttribute.CountryCode, cbx_CountryCode },
                    { ElementAttribute.Country, cbx_Country},
                    { ElementAttribute.City, tbx_City },
                    { ElementAttribute.State, tbx_State },
                    { ElementAttribute.Sublocation, tbx_Sublocation },
                    { ElementAttribute.GPSAltitude, nud_GPSAltitude },
                    { ElementAttribute.OffsetTime, tbx_OffsetTime }
                };

            foreach ((ElementAttribute attribute, string toponomyOverwriteVal) in locallyStoredToponomyDataForLatLng)
            {
                Control control = controlPairs[attribute];
                control.Text = toponomyOverwriteVal;
                if (control is NumericUpDown nud)
                {
                    nud.Value = Convert.ToDecimal(
                                value: toponomyOverwriteVal,
                                provider: CultureInfo.InvariantCulture);
                }
            }
        }

        // Another file; we don't care about Controls
        else
        {
            // this is about TZ only. scroll down for the rest
            try
            {
                if (!string.IsNullOrEmpty(TZ))
                {
                    string IANATZ = TZConvert.IanaToWindows(ianaTimeZoneName: TZ);
                    string TZOffset;
                    TimeZoneInfo timeZoneInfo =
                        TimeZoneInfo.FindSystemTimeZoneById(id: IANATZ);

                    TZOffset = timeZoneInfo.GetUtcOffset(dateTime: createDate)
                                  .ToString()
                                  .Substring(0, length: timeZoneInfo
                                                                   .GetUtcOffset(dateTime: createDate)
                                                                   .ToString()
                                                                   .Length -
                                                                    3);
                    locallyStoredToponomyDataForLatLng.Add(
                        item: !TZOffset.StartsWith(value: NullStringEquivalentGeneric)
                            ? (ElementAttribute.OffsetTime, $"+{TZOffset}")
                            : (ElementAttribute.OffsetTime, TZOffset));
                }
            }
            catch
            {
                // add a "+00:00" value to locallyStoredToponomyDataForLatLng
                locallyStoredToponomyDataForLatLng.Add(item: (ElementAttribute.OffsetTime, "+00:00"));
            }

            foreach ((ElementAttribute attribute, string toponomyOverwriteVal)
                in locallyStoredToponomyDataForLatLng)
            {
                dirElemFileToModify?.SetAttributeValueAnyType(
                        attribute: attribute,
                        value: toponomyOverwriteVal,
                        version: DirectoryElement.AttributeVersion
                                                 .Stage1EditFormIntraTabTransferQueue,
                        isMarkedForDeletion: false);
            }
        }
    }


    /// <summary>
    /// Sets the selected time zone in the combo box based on the specified time zone identifier and date, and updates
    /// the provided list with the corresponding UTC offset information.
    /// </summary>
    /// <remarks>If the specified time zone cannot be found or an error occurs while retrieving time zone
    /// information, the method adds a default UTC offset of "+00:00" to the list. The method also manages the state of
    /// the combo box and related controls to prevent unintended event handling during the update.</remarks>
    /// <param name="createDate">The date and time used to determine the daylight saving time status and UTC offset for the selected time zone.</param>
    /// <param name="toponomyOverwrites">A list that is updated with the offset time information for the selected time zone. The method adds a tuple
    /// containing the offset attribute and its value.</param>
    /// <param name="TZ">The time zone identifier used to locate and select the corresponding time zone in the combo box.</param>
    /// <param name="tzStartInt">The starting index within the combo box item text from which to search for the specified time zone identifier.</param>
    private void SetTimeZoneComboBoxForCurrentlySelectedDE(
        DateTime createDate,
        List<(ElementAttribute attribute, string toponomyOverwriteVal)> toponomyOverwrites,
        string TZ,
        int tzStartInt)
    {
        // cbx_OffsetTime.FindString(TZ, 18) doesn't seem to work so....
        for (int i = 0; i <= cbx_OffsetTime.Items.Count; i++)
        {
            string cbxText = cbx_OffsetTime.Items[index: i]
                                           .ToString();
            if (cbxText.Length >= tzStartInt)
            {
                if (cbxText
                   .Substring(tzStartInt)
                   .Contains(value: TZ))
                {
                    // this controls the logic that the ckb_UseDST should not be re-parsed again manually on the Change event that would otherwise fire.
                    _tzChangedByApi = true;
                    cbx_OffsetTime.SelectedIndex = i;
                    try
                    {
                        if (TZ != null)
                        {
                            string IANATZ =
                                TZConvert.IanaToWindows(ianaTimeZoneName: TZ);
                            string TZOffset;
                            TimeZoneInfo timeZoneInfo =
                                TimeZoneInfo.FindSystemTimeZoneById(id: IANATZ);
                            ckb_UseDST.Checked =
                                timeZoneInfo.IsDaylightSavingTime(dateTime: createDate);
                            TZOffset = timeZoneInfo.GetUtcOffset(dateTime: createDate)
                                          .ToString()
                                          .Substring(
                                               startIndex: 0, length: timeZoneInfo
                                                                     .GetUtcOffset(
                                                                          dateTime: createDate)
                                                                     .ToString()
                                                                     .Length -
                                                                      3);
                            if (!TZOffset.StartsWith(value: NullStringEquivalentGeneric))
                            {
                                toponomyOverwrites.Add(
                                    item: (ElementAttribute.OffsetTime,
                                           $"+{TZOffset}"));
                            }
                            else
                            {
                                toponomyOverwrites.Add(
                                    item: (ElementAttribute.OffsetTime,
                                           TZOffset));
                            }
                        }
                    }
                    catch
                    {
                        // add a zero
                        toponomyOverwrites.Add(
                            item: (ElementAttribute.OffsetTime, " +00:00"));
                    }

                    _tzChangedByApi = false;
                    break;
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
        IEnumerable<Control> controlsInsideGbx_TakenDate =
            helperNonstatic.GetAllControls(control: gbx_TakenDate);
        _ = lvw_FileListEditImages.SelectedItems[index: 0]
                                                           .Text;
        foreach (Control controlInsideGbx_TakenDate in controlsInsideGbx_TakenDate)
        {
            if (controlInsideGbx_TakenDate != btn_InsertTakenDate)
            {
                controlInsideGbx_TakenDate.Enabled = Enabled;

                // set font to bold for these two - that will get picked up later.
                if (controlInsideGbx_TakenDate is DateTimePicker dtp)
                {
                    dtp.Font = new Font(prototype: dtp.Font, newStyle: FontStyle.Bold);
                    ListView lvw = lvw_FileListEditImages;
                    ListViewItem lvi = lvw.SelectedItems[index: 0];

                    DirectoryElement dirElemFileToModify =
                        lvi.Tag as DirectoryElement;

                    ElementAttribute attribute =
                        GetElementAttributesElementAttribute(
                            attributeToFind: dtp.Name.Substring(HelperVariables.COL_NAME_PREFIX.Length));

                    dirElemFileToModify?.SetAttributeValueAnyType(attribute: attribute,
                            value: dtp.Text,
                            version: DirectoryElement.AttributeVersion
                                                     .Stage1EditFormIntraTabTransferQueue,
                            isMarkedForDeletion: false);
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
        Log.Info(message: "Starting");
        // Check/enforce there is _some_ data...
        if (lvw_FileListEditImages.SelectedItems.Count > 0)
        {
            ListView lvwEditImages = lvw_FileListEditImages;
            ListViewItem lvi = lvwEditImages.SelectedItems[index: 0];

            DirectoryElement dirElemFileToModify = lvi.Tag as DirectoryElement;
            string fileNameWithPath = dirElemFileToModify.FileNameWithPath;

            // ensure file still exists (not deleted or gone AWOL etc.)
            if (File.Exists(path: fileNameWithPath))
            {
                ShowDEDataInRelevantControls(dirElemFileToModify: dirElemFileToModify);

                pbx_imagePreview.Image = null;
                await HelperExifReadGetImagePreviews.GenericCreateImagePreview(
                    directoryElement: dirElemFileToModify,
                    initiator: HelperExifReadGetImagePreviews.Initiator.FrmEditFileData);
            }
            else
            {
                Log.Debug(message: $"File disappeared: {fileNameWithPath}");
                Themer.ShowMessageBox(
                    message: HelperControlAndMessageBoxHandling.ReturnControlText(
                        controlName: "mbx_FrmEditFileData_WarningFileDisappeared",
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                    icon: MessageBoxIcon.Warning,
                    buttons: MessageBoxButtons.OK);
            }
        }

        Log.Debug(message: "Done");
    }

    /// <summary>
    ///     This one basically executes a "step to next" image on the listview.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btn_ApplyAndNext_Click(object sender,
        EventArgs e)
    {
        int index = 0;
        if (lvw_FileListEditImages.SelectedItems.Count == 1)
        {
            index = lvw_FileListEditImages.SelectedItems[index: 0].Index;
        }

        try
        {
            ListView lvwEditImages = lvw_FileListEditImages;
            _ = lvwEditImages.Focus();

            if (index < lvwEditImages.Items.Count)
            {
                ListViewItem lvi = lvwEditImages.Items[index: index + 1];

                lvi.Focused = true;
                lvi.Selected = true;
            }
        }
        catch (Exception ex)
        {
            Debug.Print(ex.Message);
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
    private async void btn_Generic_OK_Click(object sender,
        EventArgs e)
    {
        // This is to show what % of writes we're at in the taskbar. Gimmick.
        int fileCounter = 0;
        foreach (ListViewItem lvi in lvw_FileListEditImages.Items)
        {
            DirectoryElement dirElemFileToModify = lvi.Tag as DirectoryElement;


            // this is to prevent code from looping through _all_ the files in a folder pointlessly.
            if (dirElemFileToModify.HasDirtyAttributes(
                    whichAttributeVersion: DirectoryElement.AttributeVersion
                                                           .Stage1EditFormIntraTabTransferQueue))
            {
                foreach (ElementAttribute attribute in (ElementAttribute[])
                         Enum.GetValues(enumType: typeof(ElementAttribute)))
                {
                    if (dirElemFileToModify.HasSpecificAttributeWithVersion(
                            attribute: attribute,
                            version: DirectoryElement.AttributeVersion
                                                     .Stage1EditFormIntraTabTransferQueue))
                    {
                        dirElemFileToModify.SetAttributeValueAnyType(
                            attribute: attribute,
                            value: dirElemFileToModify.GetAttributeValueAsString(
                                attribute: attribute,
                                version: DirectoryElement.AttributeVersion
                                                         .Stage1EditFormIntraTabTransferQueue,
                                nowSavingExif: false),
                            version: DirectoryElement.AttributeVersion
                                                     .Stage3ReadyToWrite,
                            isMarkedForDeletion: dirElemFileToModify
                               .IsMarkedForDeletion(attribute: attribute,
                                    version: DirectoryElement.AttributeVersion
                                                             .Stage1EditFormIntraTabTransferQueue));

                        // remove from Stage1EditFormIntraTabTransferQueue
                        dirElemFileToModify.RemoveAttributeValue(
                            attribute: attribute,
                            version: DirectoryElement.AttributeVersion
                                                     .Stage1EditFormIntraTabTransferQueue);
                    }

                    // remove from Stage2EditFormReadyToSaveAndMoveToWriteQueue

                    dirElemFileToModify.RemoveAttributeValue(
                        attribute: attribute,
                        version: DirectoryElement.AttributeVersion
                                                 .Stage2EditFormReadyToSaveAndMoveToWriteQueue);
                }


                HelperGenericFileLocking.FileListBeingUpdated = true;
                await FileListViewReadWrite.ListViewUpdateRowFromDEStage3ReadyToWrite(dirElemFileToModify: dirElemFileToModify);
                TaskbarManagerInstance.SetProgressValue(
                    currentValue: fileCounter + 1,
                    maximumValue: lvw_FileListEditImages.Items.Count);
                Thread.Sleep(millisecondsTimeout: 1);
                HelperGenericFileLocking.FileListBeingUpdated = false;

            }
        }

        TaskbarManagerInstance.SetProgressState(
            state: TaskbarProgressBarState.NoProgress);
        // re-center map on new data.
        FileListViewMapNavigation.ListViewItemClickNavigate();

    }

    /// <summary>
    ///     Handles when user clicks Cancel. Clears holding tables 1 & 2.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void btn_Generic_Cancel_Click(object sender,
        EventArgs e)
    {
        // clear the queues
        foreach (DirectoryElement dirElemFileToModify in DirectoryElements)
        {
            {
                foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(
                             enumType: typeof(ElementAttribute)))
                {
                    dirElemFileToModify.RemoveAttributeValue(
                        attribute: attribute,
                        version: DirectoryElement.AttributeVersion
                                                 .Stage1EditFormIntraTabTransferQueue);

                    dirElemFileToModify.RemoveAttributeValue(
                        attribute: attribute,
                        version: DirectoryElement.AttributeVersion
                                                 .Stage2EditFormReadyToSaveAndMoveToWriteQueue);
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
        _ = frmPasteWhat.ShowDialog();
    }

    /// <summary>
    ///     Handles when user requests removal of all geodata from selected img.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private async void btn_RemoveGeoData_Click(object sender,
        EventArgs e)
    {
        await HelperExifDataPointInteractions.ExifRemoveLocationData(
            senderName: "FrmEditFileData");
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

            string previousText = "herp-derp";
            string newText = "";
            _ = lvw_FileListEditImages.SelectedItems[index: 0]
                                                               .Text;

            string exifTagStr = sndr.Name.Substring(HelperVariables.COL_NAME_PREFIX.Length);
            ElementAttribute attribute =
                GetElementAttributesElementAttribute(attributeToFind: exifTagStr);
            ListView lvw = lvw_FileListEditImages;
            ListViewItem lvi = lvw.SelectedItems[index: 0];

            DirectoryElement dirElemFileToModify =
                lvi.Tag as DirectoryElement;
            if (dirElemFileToModify != null &&
                dirElemFileToModify.HasSpecificAttributeWithVersion(
                    attribute: attribute,
                    version: DirectoryElement.AttributeVersion
                                             .Stage2EditFormReadyToSaveAndMoveToWriteQueue))
            {
                previousText = dirElemFileToModify.GetAttributeValueAsString(
                    attribute: attribute,
                    version: DirectoryElement.AttributeVersion
                                             .Stage2EditFormReadyToSaveAndMoveToWriteQueue,
                    nowSavingExif: false);
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
            // ...That also means that cbx_OffsetTime is a "bit special" (aren't we all...) so it needs to be derailed rather than sent back to the various datatables
            if (senderName == "cbx_OffsetTime" &&
                !_tzChangedByApi)
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
                            version: DirectoryElement.AttributeVersion
                                                     .Stage1EditFormIntraTabTransferQueue,
                            isMarkedForDeletion: dirElemFileToModify
                               .IsMarkedForDeletion(attribute: attribute,
                                    version: DirectoryElement.AttributeVersion
                                                             .Stage1EditFormIntraTabTransferQueue));
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                            value: nudTextStr,
                            version: DirectoryElement.AttributeVersion
                                                     .Stage2EditFormReadyToSaveAndMoveToWriteQueue,
                            isMarkedForDeletion: dirElemFileToModify
                               .IsMarkedForDeletion(attribute: attribute,
                                    version: DirectoryElement.AttributeVersion
                                                             .Stage2EditFormReadyToSaveAndMoveToWriteQueue));
                    }

                    // adjust createDate and/or takenDate

                    if (senderName.Contains(value: "TakenDate"))
                    {
                        DateTime origDateValTakenDate =
                            (DateTime)dirElemFileToModify.GetAttributeValue<DateTime>(
                                attribute: ElementAttribute.TakenDate,
                                version: DirectoryElement.AttributeVersion
                                                         .Original,
                                notFoundValue: DateTime.Now);

                        double shiftTakenDateSeconds =
                            (double)nud_TakenDateSecondsShift.Value +
                            (double)(nud_TakenDateMinutesShift.Value * 60) +
                            (double)(nud_TakenDateHoursShift.Value * 60 * 60) +
                            (double)(nud_TakenDateDaysShift.Value * 60 * 60 * 24);

                        dtp_TakenDate.Value =
                            origDateValTakenDate.AddSeconds(
                                value: shiftTakenDateSeconds);
                        rbt_TakenDateTimeShift.Checked = shiftTakenDateSeconds != 0;
                    }
                    else if (senderName.Contains(value: "CreateDate"))
                    {
                        DateTime origDateValCreateDate =
                            (DateTime)dirElemFileToModify.GetAttributeValue<DateTime>(
                                attribute: ElementAttribute.CreateDate,
                                version: DirectoryElement.AttributeVersion
                                                         .Original,
                                notFoundValue: DateTime.Now);

                        double shiftCreateDateSeconds =
                            (double)nud_CreateDateSecondsShift.Value +
                            (double)(nud_CreateDateMinutesShift.Value * 60) +
                            (double)(nud_CreateDateHoursShift.Value * 60 * 60) +
                            (double)(nud_CreateDateDaysShift.Value * 60 * 60 * 24);

                        dtp_CreateDate.Value =
                            origDateValCreateDate.AddSeconds(
                                value: shiftCreateDateSeconds);
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
                            queryWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A3,
                            inputVal: sndr.Text,
                            returnWhat: LanguageMappingQueryOrReturnWhat.Country
                        );
                        if (cbx_Country.Text != sqliteText)
                        {
                            cbx_Country.Text = sqliteText;
                        }
                    }
                    else if (senderName == "cbx_Country")
                    {
                        sqliteText = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
                            queryWhat: LanguageMappingQueryOrReturnWhat.Country,
                            inputVal: sndr.Text,
                            returnWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A3
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

    /// <summary>
    /// Retrieves the time zone offset based on the user's selection and updates the corresponding text box.
    /// </summary>
    /// <remarks>This method checks whether Daylight Saving Time (DST) is in use and extracts the appropriate
    /// offset from the combo box. If an error occurs during the extraction, the offset is left blank.</remarks>
    private void GetTimeZoneOffset()
    {
        string strOffsetTime = "";

        bool useDST = ckb_UseDST.Checked;
        try
        {
            strOffsetTime = cbx_OffsetTime.Text.Substring(!useDST
                ? 1
                : 8, length: 6);
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
        AcceptButton = btn_Generic_OK;
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
        AcceptButton = btn_Generic_OK;
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

        IEnumerable<Control> controlsInsideGbx_CreateDate =
            helperNonstatic.GetAllControls(control: gbx_CreateDate);
        foreach (Control controlInsideGbx_CreateDate in controlsInsideGbx_CreateDate)
        {
            if (controlInsideGbx_CreateDate != btn_InsertCreateDate)
            {
                controlInsideGbx_CreateDate.Enabled = Enabled;

                // set font to bold for these two - that will get picked up later.
                if (controlInsideGbx_CreateDate is DateTimePicker dtp)
                {
                    dtp.Font = new Font(prototype: dtp.Font, newStyle: FontStyle.Bold);
                    if (lvi.Tag is DirectoryElement dirElemFileToModify)
                    {
                        ElementAttribute attribute =
                            GetElementAttributesElementAttribute(
                                attributeToFind: dtp.Name.Substring(HelperVariables.COL_NAME_PREFIX.Length));
                        dirElemFileToModify.SetAttributeValueAnyType(attribute: attribute,
                            value: dtp.Text,
                            version: DirectoryElement.AttributeVersion
                                                     .Stage1EditFormIntraTabTransferQueue,
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
        ttp.SetToolTip(control: pbx_OffsetTimeInfo,
            caption: ReturnControlText(
                controlName: "ttp_OffsetTime",
                fakeControlType: FakeControlTypes.ToolTip
            ));
    }

    /// <summary>
    ///     Sets the tooltip for the pbx_GPSDataPaste
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void pbx_GPSDataPaste_MouseHover(object sender,
        EventArgs e)
    {
        ToolTip ttp = new();
        ttp.SetToolTip(control: pbx_GPSDataPaste,
            caption: ReturnControlText(
                fakeControlType: FakeControlTypes.ToolTip,
                controlName: "ttp_GPSDataPaste"
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

    /// <summary>
    ///     Handles the keyDown events for the Form
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FrmEditFileData_KeyDown(object sender,
        KeyEventArgs e)
    {
        if (e.Control &&
            e.KeyCode == Keys.V)
        {
            // This attempts to paste a valid pair of number-looking items into the two GPS Data NUDs
            // The logic is that if there's a string that's separated by the listSeparator as per Windows Culture...OR English/Invariant
            // and the two halves can be transformed into a number then we paste them in...
            // but only if they are in the range of -180 to +180
            try
            {
                string clipboardText = Clipboard.GetText();

                List<string> listSeparators =
                [
                    CultureInfo.CurrentCulture.TextInfo.ListSeparator,
                    CultureInfo.InvariantCulture.TextInfo.ListSeparator
                ];

                List<CultureInfo> cultureInfos =
                [
                    CultureInfo.CurrentCulture,
                    CultureInfo.InvariantCulture
                ];

                decimal parsedLat = 0;
                decimal parsedLng = 0;
                bool parseSuccess = false;

                foreach (string listSeparator in listSeparators)
                {
                    foreach (CultureInfo cultureInfo in cultureInfos)
                    {
                        if (clipboardText.Contains(value: listSeparator) &&
                            !parseSuccess)
                        {
                            bool parseSuccessLat = decimal.TryParse(
                                s: clipboardText.Split(listSeparator[index: 0])[0],
                                style: NumberStyles.Any,
                                provider: cultureInfo,
                                result: out parsedLat);
                            bool parseSuccessLng = decimal.TryParse(
                                s: clipboardText.Split(listSeparator[index: 0])[1],
                                style: NumberStyles.Any,
                                provider: cultureInfo,
                                result: out parsedLng);

                            parseSuccess = parseSuccessLat && parseSuccessLng;
                        }
                    }
                }

                if (parseSuccess)
                {
                    if (
                        parsedLat >= -180 &&
                        parsedLat <= 180 &&
                        parsedLng >= -180 &&
                        parsedLng <= 180)
                    {
                        nud_GPSLatitude.Text =
                            parsedLat.ToString(provider: CultureInfo.CurrentCulture);
                        nud_GPSLongitude.Text =
                            parsedLng.ToString(provider: CultureInfo.CurrentCulture);
                        nud_GPSLatitude.Value = parsedLat;
                        nud_GPSLongitude.Value = parsedLng;
                    }
                }
            }
            catch
            {
                // nothing
            }
        }
    }

    #endregion
}