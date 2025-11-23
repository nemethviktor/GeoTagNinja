using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using GeoTagNinja.View.ListView;
using Microsoft.WindowsAPICodePack.Taskbar;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using TimeZoneConverter;
using static GeoTagNinja.FrmMainApp;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;
using static GeoTagNinja.Helpers.HelperGenericAncillaryListsArrays;
using static GeoTagNinja.Model.SourcesAndAttributes;
using HelperControlAndMessageBoxCustomMessageBoxManager =
    GeoTagNinja.Helpers.HelperControlAndMessageBoxCustomMessageBoxManager;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

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
        HelperControlThemeManager.SetThemeColour(
            themeColour: HelperVariables.UserSettingUseDarkMode
                ? ThemeColour.Dark
                : ThemeColour.Light, parentControl: this);
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

        Log.Trace(
            message:
            "Emptying FrmMainApp.Stage1EditFormIntraTabTransferQueue + Stage2EditFormReadyToSaveAndMoveToWriteQueue");
        foreach (DirectoryElement dirElemFileToModify in DirectoryElements)
        {
            {
                foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(
                             enumType: typeof(ElementAttribute)))
                {
                    // empty queue

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
            }
        }

        Log.Trace(
            message:
            "Emptying FrmMainApp.Stage1EditFormIntraTabTransferQueue + Stage2EditFormReadyToSaveAndMoveToWriteQueue - Done");

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
            cItem: this, senderForm: this);

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

        // this updates the listview itself

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

        _frmEditFileDataNowLoadingFileData = false; // techinically this is redundant here
        Log.Info(message: "Done");
    }

    /// <summary>
    ///     This method is responsible for retrieving the text values for the Controls in the Form.
    ///     For labels, buttons, etc., this is their "language" label (e.g., "Latitude").
    ///     For textboxes and similar controls, this is the value (e.g., "51.002").
    ///     The method also handles the assignment of these values to the corresponding controls.
    /// </summary>
    private void lvw_EditorFileListImagesGetData()
    {
        Logger log = Log;
        Log.Info(message: "Starting");

        _frmEditFileDataNowLoadingFileData = true;

        ListView lvw = lvw_FileListEditImages;
        ListViewItem lvi = lvw.SelectedItems[index: 0];
        lvw.Columns[index: 0]
           .Width = lvw.Width;

        string fileNameWithoutPath = lvi.Text;
        DirectoryElement dirElemFileToModify =
            lvi.Tag as DirectoryElement;
        FrmMainApp frmMainAppInstance =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        btn_InsertFromTakenDate.Enabled = false;

        Log.Trace(message: "Assinging Labels Start");
        HelperNonStatic helperNonstatic = new();
        List<Type> lstControlTypesNoDEValue =
        [
            typeof(Label),
            typeof(GroupBox),
            typeof(Button),
            typeof(CheckBox),
            typeof(TabPage),
            typeof(RadioButton)
        ];
        List<Type> lstControlTypesWithDEValue =
        [
            typeof(TextBox),
            typeof(ComboBox),
            typeof(DateTimePicker),
            typeof(NumericUpDown)
        ];

        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (lstControlTypesNoDEValue.Contains(item: cItem.GetType()) ||
                lstControlTypesWithDEValue.Contains(item: cItem.GetType()))
            {
                string
                    debugItemName =
                        cItem.Name; // this is for debugging only (particularly here, that is.)
                string controlNameWithoutTypeIdentifier;

                try
                {
                    Log.Trace(message: $"cItem: {debugItemName} (Type: {cItem.GetType().Name}) - Starting.");

                    if (lstControlTypesNoDEValue.Contains(item: cItem.GetType()))
                    {
                        // gets logged inside.
                        ReturnControlText(cItem: cItem, senderForm: this);
                    }

                    else if (lstControlTypesWithDEValue.Contains(item: cItem.GetType()))
                    {
                        // this gets the name of the Control without the type (e.g. tbx_SomethingAttrib becomes SomethingAttrib)
                        controlNameWithoutTypeIdentifier =
                            cItem.Name.Substring(startIndex: 4);

                        // get the ElementAttribute
                        ElementAttribute attribute =
                            GetElementAttributesElementAttribute(
                                attributeToFind: controlNameWithoutTypeIdentifier);

                        // get the AttributeVersion maxAttributeVersion
                        string stringValueOfCItem = NullStringEquivalentBlank;
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
                            stringValueOfCItem =
                                dataInDirectoryElement.ToString(
                                    provider: CultureInfo.InvariantCulture);
                            cItem.Font = new Font(prototype: cItem.Font,
                                newStyle: FontStyle.Regular);

                            // technically this doesn't belong here as an overall-enabler but the code inside checks for details
                            EnableDateTimeItems(cItem: cItem);
                        }
                        else
                        {
                            // if it's none of the further below then make the cItem be just blank.
                            cItem.Text = NullStringEquivalentBlank;

                            // okay for gbx_*Date && !NUD -> those are DateTimePickers.
                            // If they are NULL then there is either no TakenDate or no CreateDate so the actual controls will have to be disabled.
                            // technically this doesn't belong here as an overall-enabler but the code inside checks for details
                            DisableDateTimeItems(cItem: cItem);
                        }

                        // wrangle the data
                        if (dataInDirectoryElement != null)
                        {
                            // stick into DE2 ("pending save") - this is to see if the data has changed later.

                            if (cItem is not NumericUpDown nud)
                            {
                                // this is related to storing the default DateTimes for TakenDate and CreateDate
                                // what we also need to do here is account for any copy-paste shifts.
                                if (cItem is DateTimePicker dtp)
                                {
                                    HandleDateTimePickerTimeShift(dtp: dtp,
                                        directoryElement:
                                        dirElemFileToModify,
                                        cItem: cItem,
                                        maxAttributeVersion:
                                        maxAttributeVersion);
                                }
                                else
                                {
                                    cItem.Text = stringValueOfCItem;
                                }

                                Log.Trace(message:
                                    $"cItem: {cItem.Name} - Adding to Stage2EditFormReadyToSaveAndMoveToWriteQueue");

                                dirElemFileToModify.SetAttributeValueAnyType(
                                    attribute: attribute,
                                    value: stringValueOfCItem,
                                    version: DirectoryElement.AttributeVersion
                                                             .Stage2EditFormReadyToSaveAndMoveToWriteQueue,
                                    isMarkedForDeletion: false);
                            }
                            else // if (cItem is NumericUpDown nud)
                            {
                                nud.Value = Convert.ToDecimal(
                                    value: stringValueOfCItem,
                                    provider: CultureInfo.InvariantCulture);

                                if (nud.Name.EndsWith(value: "Shift") &&
                                    nud.Value != 0)
                                {
                                    if (nud.Name.Substring(startIndex: 4)
                                           .StartsWith(
                                                value: TakenOrCreated.Taken.ToString()))
                                    {
                                        rbt_TakenDateTimeShift.Checked = true;
                                    }
                                    else if (nud.Name.Substring(startIndex: 4)
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
                            if (cItem.Name ==
                                "cbx_CountryCode") // this will also fill in Country
                            {
                                string countryCodeInDirectoryElement =
                                    stringValueOfCItem;
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

                            // Leaving this commented out on purpose...
                            // While the code works fine it's a source of confusion as Uses-DST isn't currently stored...
                            // anywhere and so upon reopening the Form this could lead to undesired results. ...
                            // I'll ponder on some reasonable ways to handle it if there's interest.
                            //else if (cItem.Name == "cbx_OffsetTime")
                            //{
                            //    // attempt to convert offset to a member of the list
                            //    string offsetTimeInDirectoryElement =
                            //        dirElemFileToModify.GetAttributeValueString(
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

                        if (maxAttributeVersion !=
                            DirectoryElement.AttributeVersion.Original)
                        {
                            cItem.Font =
                                new Font(prototype: cItem.Font,
                                    newStyle: FontStyle.Bold);
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                Log.Trace(message: $"cItem: {cItem.Name} (Type: {cItem.GetType()
                                                                      .Name}) - Done.");
            }
        }

        Log.Trace(message: "Assinging Labels Done");

        // done load
        Log.Debug(message: "Done");
        _frmEditFileDataNowLoadingFileData = false;
        return;

        void DisableDateTimeItems(Control cItem)
        {
            if (cItem.Parent.Name.StartsWith(value: "gbx_") &&
                cItem.Parent.Name.EndsWith(value: "Date"))
            {
                if (cItem is not NumericUpDown nud)
                {
                    if (cItem.Parent.Name == "gbx_TakenDate")
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
                    else if (cItem.Parent.Name == "gbx_CreateDate")
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

                else // cItem is nud
                {
                    nud.Value = NullIntEquivalent;
                    nud.Text = NullStringEquivalentZero;
                }
            }
        }

        void EnableDateTimeItems(Control cItem)
        {
            // if this is a TakenDate or CreateDate -related
            if (cItem.Parent.Name.StartsWith(value: "gbx_") &&
                cItem.Parent.Name.EndsWith(value: "Date") &&
                cItem is not NumericUpDown)
            {
                // this code block deals with enabling and disabling the Controls on whether there is data behind.
                if (cItem.Parent.Name == "gbx_TakenDate")
                {
                    IEnumerable<Control> cGbx_TakenDate =
                        helperNonstatic.GetAllControls(
                            control: gbx_TakenDate);
                    List<Control> controlsToEnable = [];
                    List<Control> controlsToDisable = [btn_InsertTakenDate];
                    foreach (Control cItemGbx_TakenDate in cGbx_TakenDate)
                    {
                        if (cItemGbx_TakenDate != btn_InsertTakenDate)
                        {
                            controlsToEnable.Add(
                                item: cItemGbx_TakenDate);
                        }
                    }

                    EnableSpecificControlAndDisableOthers(
                        parentControl: gbx_TakenDate,
                        controlsToEnable: controlsToEnable,
                        controlsToDisable: controlsToDisable);
                }
                else if (cItem.Parent.Name == "gbx_CreateDate")
                {
                    IEnumerable<Control> cGbx_CreateDate =
                        helperNonstatic.GetAllControls(
                            control: gbx_CreateDate);
                    List<Control> controlsToEnable = [];
                    List<Control> controlsToDisable = [btn_InsertCreateDate];
                    foreach (Control cItemGbx_CreateDate in
                             cGbx_CreateDate)
                    {
                        if (cItemGbx_CreateDate != btn_InsertCreateDate)
                        {
                            controlsToEnable.Add(
                                item: cItemGbx_CreateDate);
                        }
                    }

                    EnableSpecificControlAndDisableOthers(
                        parentControl: gbx_CreateDate,
                        controlsToEnable: controlsToEnable,
                        controlsToDisable: controlsToDisable);
                }
            }
        }

        [SuppressMessage(category: "ReSharper", checkId: "PossibleInvalidOperationException")]
        void HandleDateTimePickerTimeShift(DateTimePicker dtp,
                                           DirectoryElement directoryElement,
                                           Control cItem,
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

            Log.Trace(message: $"cItem: {cItem.Name} - Updating DateTimePicker");
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
            returnDataInDirectoryElement = directoryElement.GetAttributeValueString(
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

    #region Themeing

    // this is entirely the same as in FrmMainApp.

    // via https://stackoverflow.com/a/75716080/3968494
    private void ListView_DrawColumnHeader(object sender,
        DrawListViewColumnHeaderEventArgs e)
    {
        Color foreColor = HelperVariables.UserSettingUseDarkMode
            ? Color.FromArgb(red: 241, green: 241, blue: 241)
            : Color.Black;

        Color backColor = HelperVariables.UserSettingUseDarkMode
            ? Color.FromArgb(red: 101, green: 151, blue: 151)
            : SystemColors.Control;

        //Fills one solid background for each cell.
        using (SolidBrush backColorkBrush = new(color: backColor))
        {
            e.Graphics.FillRectangle(brush: backColorkBrush, rect: e.Bounds);
        }

        //Draw the borders for the header around each cell.
        using (Pen foreColorPen = new(color: foreColor))
        {
            e.Graphics.DrawRectangle(pen: foreColorPen, rect: e.Bounds);
        }

        using SolidBrush foreColorBrush = new(color: foreColor);
        StringFormat stringFormat = GetStringFormat();

        //Do some padding, since these draws right up next to the border for Left/Near.  Will need to change this if you use Right/Far
        Rectangle rect = e.Bounds;
        rect.X += 2;
        e.Graphics.DrawString(s: e.Header.Text, font: e.Font, brush: foreColorBrush,
            layoutRectangle: rect, format: stringFormat);
    }

    private StringFormat GetStringFormat()
    {
        return new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
    }

    private void ListView_DrawItem(object sender,
        DrawListViewItemEventArgs e)
    {
        e.DrawDefault = true;
    }

    private void ListView_DrawSubItem(object sender,
        DrawListViewSubItemEventArgs e)
    {
        e.DrawDefault = true;
    }

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
        _ = new DataTable();
        _ =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        //reset this just in case.
        HelperVariables.OperationAPIReturnedOKResponse = true;

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
                // took me a while to understand my own code. what we are doing here is that we are trying to tell the user (and by proxy, the developer) that something other than the two buttons defined above have been pressed.
                HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                    controlName: "mbx_FrmEditFileData_ErrorInvalidSender",
                    captionType: MessageBoxCaption.Error,
                    buttons: MessageBoxButtons.OK,
                    extraMessage: ((Button)sender).Name);
                break;
        }

        string messageBoxName = HelperVariables.OperationAPIReturnedOKResponse
            ? "mbx_FrmEditFileData_InfoDataUpdated"
            : "mbx_FrmEditFileData_ErrorAPIError";
        MessageBoxCaption messageBoxCaption = HelperVariables.OperationAPIReturnedOKResponse
            ? MessageBoxCaption.Information
            : MessageBoxCaption.Error;

        HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(controlName: messageBoxName,
            captionType: messageBoxCaption,
            buttons: MessageBoxButtons.OK);
    }

    /// <summary>
    ///     Pulls data from the various APIs and fills up the listView and fills the TextBoxes and/or SQLite.
    /// </summary>
    /// <param name="fileNameWithoutPath">Blank if used as "pull one file" otherwise the name of the file w/o Path</param>
    private void getFromWeb_Toponomy(string fileNameWithoutPath = "")
    {
        FrmMainApp frmMainAppInstance =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        DateTime
            createDate =
                NullDateTimeEquivalent; // can't leave it null because it's updated in various IFs and C# perceives it as uninitialised.

        string strGpsLatitude = null;
        string strGpsLongitude = null;

        DataTable dtToponomy = new();

        // this is "current file"
        if (fileNameWithoutPath == "")
        {
            if (nud_GPSLatitude.Text != "" &&
                nud_GPSLongitude.Text != "")
            {
                strGpsLatitude =
                    nud_GPSLatitude.Value.ToString(
                        provider: CultureInfo.InvariantCulture);
                strGpsLongitude =
                    nud_GPSLongitude.Value.ToString(
                        provider: CultureInfo.InvariantCulture);

                HelperVariables.CurrentAltitude = null;
                HelperVariables.CurrentAltitude =
                    nud_GPSAltitude.Text.ToString(provider: CultureInfo.InvariantCulture);

                dtToponomy = HelperExifReadExifData.DTFromAPIExifGetToponomyFromWebOrSQL(
                    lat: strGpsLatitude,
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
                HelperVariables.CurrentAltitude = frmMainAppInstance.lvw_FileList
                                                                    .FindItemWithText(text: fileNameWithoutPath)
                                                                    .SubItems[index: frmMainAppInstance
                                                                        .lvw_FileList
                                                                        .Columns[
                                                                             key: FileListView.COL_NAME_PREFIX +
                                                                             FileListView.FileListColumns.GPS_ALTITUDE]
                                                                        .Index]
                                                                    .Text.ToString(
                                                                         provider: CultureInfo.InvariantCulture);

                strGpsLatitude = frmMainAppInstance
                                .lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                                .SubItems[index: frmMainAppInstance.lvw_FileList
                                                                   .Columns[
                                                                        key: FileListView.COL_NAME_PREFIX +
                                                                             FileListView.FileListColumns.GPS_LATITUDE]
                                                                   .Index]
                                .Text.ToString(provider: CultureInfo.InvariantCulture);
                strGpsLongitude = frmMainAppInstance
                                 .lvw_FileList.FindItemWithText(text: fileNameWithoutPath)
                                 .SubItems[index: frmMainAppInstance.lvw_FileList
                                                                    .Columns[
                                                                         key: FileListView.COL_NAME_PREFIX +
                                                                              FileListView.FileListColumns
                                                                                 .GPS_LONGITUDE]
                                                                    .Index]
                                 .Text.ToString(provider: CultureInfo.InvariantCulture);
                string strCreateDate = frmMainAppInstance
                                      .lvw_FileList
                                      .FindItemWithText(text: fileNameWithoutPath)
                                      .SubItems[
                                           index: frmMainAppInstance.lvw_FileList
                                                                    .Columns[
                                                                         key: FileListView.COL_NAME_PREFIX +
                                                                              FileListView.FileListColumns.CREATE_DATE]
                                                                    .Index]
                                      .Text.ToString(
                                           provider: CultureInfo.InvariantCulture);
                _ = DateTime.TryParse(
                   s: strCreateDate.ToString(provider: CultureInfo.InvariantCulture),
                   result: out createDate);
            }

            if (double.TryParse(s: strGpsLatitude,
                    style: NumberStyles.Any,
                    provider: CultureInfo.InvariantCulture,
                    result: out _) &&
                double.TryParse(s: strGpsLongitude,
                    style: NumberStyles.Any,
                    provider: CultureInfo.InvariantCulture,
                    result: out _))
            {
                dtToponomy = HelperExifReadExifData.DTFromAPIExifGetToponomyFromWebOrSQL(
                    lat: strGpsLatitude, lng: strGpsLongitude,
                    fileNameWithoutPath: fileNameWithoutPath);
            }
        }

        // Pull the data from the web regardless.
        List<(ElementAttribute attribute, string toponomyOverwriteVal)>
            toponomyOverwrites = [];
        if (dtToponomy != null &&
            dtToponomy.Rows.Count > 0)
        {
            toponomyOverwrites.Add(item: (ElementAttribute.CountryCode,
                                          dtToponomy.Rows[index: 0][
                                                         columnName: "CountryCode"]
                                                    .ToString()));
            toponomyOverwrites.Add(item: (ElementAttribute.Country,
                                          dtToponomy.Rows[index: 0][columnName: "Country"]
                                                    .ToString()));
            toponomyOverwrites.Add(item: (ElementAttribute.City,
                                          dtToponomy.Rows[index: 0][columnName: "City"]
                                                    .ToString()));
            toponomyOverwrites.Add(item: (ElementAttribute.State,
                                          dtToponomy.Rows[index: 0][columnName: "State"]
                                                    .ToString()));
            toponomyOverwrites.Add(item: (ElementAttribute.Sublocation,
                                          dtToponomy.Rows[index: 0][
                                                         columnName: "Sublocation"]
                                                    .ToString()));
            toponomyOverwrites.Add(item: (ElementAttribute.GPSAltitude,
                                          dtToponomy.Rows[index: 0][
                                                         columnName: "GPSAltitude"]
                                                    .ToString()));

            string TZ = dtToponomy.Rows[index: 0][columnName: "timeZoneId"]
                                  .ToString();

            // multiple files...i think. fml.
            if (fileNameWithoutPath == "")
            {
                const int tzStartInt = 18;

                _ = DateTime.TryParse(
                   s: tbx_CreateDate.Text.ToString(
                       provider: CultureInfo.InvariantCulture), result: out createDate);

                // cbx_OffsetTime.FindString(TZ, 18) doesn't seem to work so....
                for (int i = 0; i <= cbx_OffsetTime.Items.Count; i++)
                {
                    string cbxText = cbx_OffsetTime.Items[index: i]
                                                   .ToString();
                    if (cbxText.Length >= tzStartInt)
                    {
                        if (cbxText
                           .Substring(startIndex: tzStartInt)
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
                                    TimeZoneInfo tst =
                                        TimeZoneInfo.FindSystemTimeZoneById(id: IANATZ);
                                    ckb_UseDST.Checked =
                                        tst.IsDaylightSavingTime(dateTime: createDate);
                                    TZOffset = tst.GetUtcOffset(dateTime: createDate)
                                                  .ToString()
                                                  .Substring(
                                                       startIndex: 0, length: tst
                                                                             .GetUtcOffset(
                                                                                  dateTime: createDate)
                                                                             .ToString()
                                                                             .Length -
                                                                              3);
                                    if (!TZOffset.StartsWith(
                                            value: NullStringEquivalentGeneric))
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
                                    item: (ElementAttribute.OffsetTime, "+00:00"));
                            }

                            _tzChangedByApi = false;
                            break;
                        }
                    }
                }

                // send it back to the Form + store
                foreach ((ElementAttribute attribute, string toponomyOverwriteVal)
in toponomyOverwrites)
                {
                    if (attribute is ElementAttribute.CountryCode)
                    {
                        cbx_CountryCode.Text = toponomyOverwriteVal;
                    }
                    else if (attribute is ElementAttribute.Country)
                    {
                        cbx_Country.Text = toponomyOverwriteVal;
                    }
                    else if (attribute is ElementAttribute.City)
                    {
                        tbx_City.Text = toponomyOverwriteVal;
                    }
                    else if (attribute is ElementAttribute.State)
                    {
                        tbx_State.Text = toponomyOverwriteVal;
                    }
                    else if (attribute == ElementAttribute.Sublocation)
                    {
                        tbx_Sublocation.Text = toponomyOverwriteVal;
                    }
                    else if (attribute is ElementAttribute.GPSAltitude)
                    {
                        nud_GPSAltitude.Text = toponomyOverwriteVal;
                        nud_GPSAltitude.Value = Convert.ToDecimal(
                            value: toponomyOverwriteVal,
                            provider: CultureInfo.InvariantCulture);
                    }
                    else if (attribute is ElementAttribute.OffsetTime)
                    {
                        tbx_OffsetTime.Text = toponomyOverwriteVal;
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
                        TimeZoneInfo tst =
                            TimeZoneInfo.FindSystemTimeZoneById(id: IANATZ);

                        TZOffset = tst.GetUtcOffset(dateTime: createDate)
                                      .ToString()
                                      .Substring(startIndex: 0, length: tst
                                                                       .GetUtcOffset(dateTime: createDate)
                                                                       .ToString()
                                                                       .Length -
                                                                        3);
                        toponomyOverwrites.Add(
                            item: !TZOffset.StartsWith(value: NullStringEquivalentGeneric)
                                ? (ElementAttribute.OffsetTime, $"+{TZOffset}")
                                : (ElementAttribute.OffsetTime, TZOffset));
                    }
                }
                catch
                {
                    // add a zero
                    toponomyOverwrites.Add(item: (ElementAttribute.OffsetTime, "+00:00"));
                }

                ListView lvw = lvw_FileListEditImages;
                ListViewItem lvi = lvw.SelectedItems[index: 0];

                DirectoryElement dirElemFileToModify =
                    lvi.Tag as DirectoryElement;
                foreach ((ElementAttribute attribute, string toponomyOverwriteVal)
in toponomyOverwrites)
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
        IEnumerable<Control> cGbx_TakenDate =
            helperNonstatic.GetAllControls(control: gbx_TakenDate);
        _ = lvw_FileListEditImages.SelectedItems[index: 0]
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

                    DirectoryElement dirElemFileToModify =
                        lvi.Tag as DirectoryElement;

                    ElementAttribute attribute =
                        GetElementAttributesElementAttribute(
                            attributeToFind: dtp.Name.Substring(startIndex: 4));

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
        if (lvw_FileListEditImages.SelectedItems.Count > 0)
        {
            ListView lvwEditImages = lvw_FileListEditImages;
            ListViewItem lvi = lvwEditImages.SelectedItems[index: 0];

            DirectoryElement dirElemFileToModify =
                lvi.Tag as DirectoryElement;
            string fileNameWithPath = dirElemFileToModify.FileNameWithPath;

            if (File.Exists(path: fileNameWithPath))
            {
                lvw_EditorFileListImagesGetData();

                pbx_imagePreview.Image = null;
                await HelperExifReadGetImagePreviews.GenericCreateImagePreview(
                    directoryElement: dirElemFileToModify,
                    initiator: HelperExifReadGetImagePreviews.Initiator.FrmEditFileData);
            }
            else
            {
                Log.Debug(message: $"File disappeared: {fileNameWithPath}");
                HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                    controlName: "mbx_FrmEditFileData_WarningFileDisappeared", captionType: MessageBoxCaption.Error,
                    buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
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
        FrmMainApp frmMainAppInstance =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (frmMainAppInstance != null)
        {
            int directoryElementsCount = DirectoryElements.Count;
            // move data from temp-queue to write-queue
            for (int fileCounter = 0; fileCounter < directoryElementsCount; fileCounter++)
            {
                DirectoryElement dirElemFileToModify =
                    DirectoryElements[index: fileCounter];
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
                                value: dirElemFileToModify.GetAttributeValueString(
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

                    ListViewItem lvi = null;
                    try
                    {
                        lvi = frmMainAppInstance.lvw_FileList.FindItemWithText(
                            text: dirElemFileToModify.ItemNameWithoutPath);
                    }
                    catch
                    {
                        // shouldn't happen
                    }

                    if (lvi != null)
                    {
                        HelperGenericFileLocking.FileListBeingUpdated = true;
                        await FileListViewReadWrite
                           .ListViewUpdateRowFromDEStage3ReadyToWrite(lvi: lvi);
                        TaskbarManagerInstance.SetProgressValue(
                            currentValue: fileCounter + 1,
                            maximumValue: directoryElementsCount);
                        Thread.Sleep(millisecondsTimeout: 1);
                        HelperGenericFileLocking.FileListBeingUpdated = false;
                    }
                }
            }

            TaskbarManagerInstance.SetProgressState(
                state: TaskbarProgressBarState.NoProgress);
            // re-center map on new data.
            FileListViewMapNavigation.ListViewItemClickNavigate();
        }
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

            string exifTagStr = sndr.Name.Substring(startIndex: 4);
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
                previousText = dirElemFileToModify.GetAttributeValueString(
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

    private void GetTimeZoneOffset()
    {
        string strOffsetTime = "";
        
        bool useDST = ckb_UseDST.Checked;
        try
        {
            strOffsetTime = cbx_OffsetTime.Text.Substring(startIndex: !useDST
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

        IEnumerable<Control> cGbx_CreateDate =
            helperNonstatic.GetAllControls(control: gbx_CreateDate);
        foreach (Control cItemGbx_CreateDate in cGbx_CreateDate)
        {
            if (cItemGbx_CreateDate != btn_InsertCreateDate)
            {
                cItemGbx_CreateDate.Enabled = Enabled;

                // set font to bold for these two - that will get picked up later.
                if (cItemGbx_CreateDate is DateTimePicker dtp)
                {
                    dtp.Font = new Font(prototype: dtp.Font, newStyle: FontStyle.Bold);
                    if (lvi.Tag is DirectoryElement dirElemFileToModify)
                    {
                        ElementAttribute attribute =
                            GetElementAttributesElementAttribute(
                                attributeToFind: dtp.Name.Substring(startIndex: 4));
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