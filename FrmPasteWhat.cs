using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja;

public partial class FrmPasteWhat : Form
{
    private static string _initiatorName;
    private static readonly List<string> _lastCheckedCheckBoxes = new();
    internal static string FileDateCopySourceFileNameWithoutPath;

    /// <summary>
    ///     This Form controls what data to paste from a "current file" to "selected file(s)".
    ///     The overall logic is different on whether the Paste takes place in the FrmMainApp or FrmEditFileData.
    ///     - If the prior then stuff will come from FrmMainApp.CopyPoolDict - logic being that users can move around folders
    ///     etc and we're pre-storing data in a "pool"
    ///     - If the latter then things are a bit different because there is no "copy" so to say, only "paste" (ie there is no
    ///     CTRL+C element taking place) and we take data directly from the file
    /// </summary>
    /// <param name="initiator">This will be either the Edit File Form (FrmEditFileData) or the Main Form(FrmMainApp)</param>
    public FrmPasteWhat(string initiator)
    {
        _initiatorName = initiator;
        InitializeComponent();

        ListView lvw;
        List<ElementAttribute> tagsToPasteAttributeList = new();

        // get the name of the file we're pasting FROM.
        // in this case this will be used to pre-fill/check the checkboxes
        if (_initiatorName == "FrmEditFileData")
        {
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
            if (frmEditFileDataInstance != null)
            {
                lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                FileDateCopySourceFileNameWithoutPath = lvw.SelectedItems[index: 0]
                    .Text;

                // No need to check in Original as the assumption is that user wants to paste things that have changed.
                // Stuff will live in Stage1EditFormIntraTabTransferQueue
                DirectoryElement dirElemFileToCopyFrom = FrmMainApp.DirectoryElements.FindElementByItemName(FileNameWithPath: Path.Combine(path1: FrmMainApp.FolderName, path2: FileDateCopySourceFileNameWithoutPath));
                foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                {
                    if (dirElemFileToCopyFrom != null &&
                        dirElemFileToCopyFrom.HasSpecificAttributeWithVersion(attribute: attribute,
                                                                              version: DirectoryElement.AttributeVersion
                                                                                  .Stage1EditFormIntraTabTransferQueue))
                    {
                        tagsToPasteAttributeList.Add(item: attribute);
                    }
                }
            }
        }
        else if (_initiatorName == "FrmMainApp")
        {
            foreach (KeyValuePair<ElementAttribute, string> keyValuePair in FrmMainApp.CopyPoolDict)
            {
                tagsToPasteAttributeList.Add(item: keyValuePair.Key);
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem is CheckBox cbx)
            {
                ElementAttribute attribute;
                if (cbx.Name.Substring(startIndex: 4) == "OffsetTime")

                {
                    attribute = ElementAttribute.OffsetTime; // fml. basically the actual tbx_OffsetTimeList is a TextBox so it would not be picked up as a change.
                }
                else
                {
                    attribute = GetAttributeFromString(attributeToFind: cbx.Name.Substring(startIndex: 4));
                }

                if (tagsToPasteAttributeList.Contains(item: attribute))
                {
                    cbx.Checked = true;
                }
            }
        }

        rbt_PasteTakenDateActual.Enabled = ckb_TakenDate.Checked;
        rbt_PasteTakenDateShift.Enabled = ckb_TakenDate.Checked;

        rbt_PasteCreateDateActual.Enabled = ckb_CreateDate.Checked;
        rbt_PasteCreateDateShift.Enabled = ckb_CreateDate.Checked;

        // enable the shift-radiobuttons if there's data
        if (tagsToPasteAttributeList is
            {
                Count: > 0
            })
        {
            foreach (ElementAttribute attribute in tagsToPasteAttributeList)
            {
                string attributeString = GetAttributeName(attribute: attribute);
                if (attributeString.StartsWith(value: "TakenDate") && attributeString.EndsWith(value: "Shift"))
                {
                    rbt_PasteTakenDateShift.Checked = true;
                }
                else if (attributeString.StartsWith(value: "CreateDate") && attributeString.EndsWith(value: "Shift"))
                {
                    rbt_PasteCreateDateShift.Checked = true;
                }
            }
        }
    }

    /// <summary>
    ///     Adds text to the labels.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void FrmPasteWhat_Load(object sender,
                                   EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        HelperStatic.GenericReturnControlText(cItem: this, senderForm: this);

        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem is Label ||
                cItem is GroupBox ||
                cItem is Button ||
                cItem is CheckBox ||
                cItem is TabPage ||
                cItem is RadioButton
               )

            {
                HelperStatic.GenericReturnControlText(cItem: cItem, senderForm: this);
            }
        }

        btn_PullMostRecentPasteSettings.Enabled = _lastCheckedCheckBoxes.Count > 0;
    }

    /// <summary>
    ///     Updates the various relevant write-queues. For the Edit Form that's Stage1EditFormIntraTabTransferQueue, for the
    ///     Main Form it's
    ///     Stage3ReadyToWrite.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    /// <exception cref="NotImplementedException">Warn that I did something incorrect.</exception>
    private async void btn_OK_Click(object sender,
                                    EventArgs e)
    {
        // this can't be 'string' because some of the types aren't strings
        Dictionary<ElementAttribute, object> copyPasteDict = new();

        // create a list of tags that also have a Ref version
        List<string> tagsWithRefList = new()
        {
            "GPSAltitude",
            "GPSDestLatitude",
            "GPSDestLongitude",
            "GPSImgDirection",
            "GPSLatitude",
            "GPSLongitude",
            "GPSSpeed"
        };

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);

        ListView lvw;

        // get the name of the file we're pasting FROM.
        if (_initiatorName == "FrmEditFileData")
        {
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
            if (frmEditFileDataInstance != null)
            {
                lvw = frmEditFileDataInstance.lvw_FileListEditImages;
            }
        }
        else if (_initiatorName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                lvw = frmMainAppInstance.lvw_FileList;
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        // get a list of tag names to paste based on what is checked on the checkboxes in the Form
        List<ElementAttribute> tagsToPaste = new();
        _lastCheckedCheckBoxes.Clear();

        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(CheckBox))
            {
                CheckBox thisCheckBox = (CheckBox)cItem;
                if (thisCheckBox.Checked)
                {
                    string attributeString = cItem.Name.Substring(startIndex: 4);
                    ElementAttribute attribute = GetAttributeFromString(attributeToFind: attributeString);
                    _lastCheckedCheckBoxes.Add(item: cItem.Name);

                    // "EndsWith" doesn't work here because the CheckBox.Name never ends with "Shift".
                    if (attributeString == "TakenDate" || attributeString == "CreateDate")
                    {
                        string pasteWhichDate = attributeString.Replace(oldValue: "Date", newValue: "");
                        string[] timeUnitArr = { "Days", "Hours", "Minutes", "Seconds" };
                        switch (pasteWhichDate)
                        {
                            case "Taken":
                                if (rbt_PasteTakenDateActual.Checked)
                                {
                                    tagsToPaste.Add(item: attribute);
                                }
                                else if (rbt_PasteTakenDateShift.Checked)
                                {
                                    // want: "TakenDateDaysShift"
                                    foreach (string timeUnit in timeUnitArr)
                                    {
                                        tagsToPaste.Add(item: GetAttributeFromString(attributeToFind: attributeString + timeUnit + "Shift"));
                                    }
                                }

                                break;
                            case "Create":
                                if (rbt_PasteCreateDateActual.Checked)
                                {
                                    tagsToPaste.Add(item: attribute);
                                }
                                else if (rbt_PasteCreateDateShift.Checked)
                                {
                                    foreach (string timeUnit in timeUnitArr)
                                    {
                                        tagsToPaste.Add(item: GetAttributeFromString(attributeToFind: attributeString + timeUnit + "Shift"));
                                    }
                                }

                                break;
                        }
                    }
                    // also do all the CountryCode 
                    else if (attributeString == "Country")
                    {
                        tagsToPaste.Add(item: attribute);
                        tagsToPaste.Add(item: ElementAttribute.CountryCode);
                    }
                    else
                    {
                        tagsToPaste.Add(item: attribute);
                    }

                    // any in the Ref lot
                    if (tagsWithRefList.Contains(item: attributeString))
                    {
                        tagsToPaste.Add(item: GetAttributeFromString(attributeToFind: attributeString + "Ref"));
                    }
                }
            }
        }

        if (_initiatorName == "FrmEditFileData")
        {
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];

            if (frmEditFileDataInstance != null)
            {
                lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                // do paste into the tables + grid as req'd
                DirectoryElement dirElemFileToCopyFrom = FrmMainApp.DirectoryElements.FindElementByItemName(
                    FileNameWithPath: Path.Combine(path1: FrmMainApp.FolderName,
                                                   path2: lvw.SelectedItems[index: 0]
                                                       .Text));

                foreach (ElementAttribute attribute in tagsToPaste)
                {
                    string attributeStr = GetAttributeName(attribute: attribute);

                    // there must be a better way around this
                    Type typeOfAttribute = GetAttributeType(attribute: attribute);
                    string pasteValueStr = null;
                    double? pasteValueDbl = null;
                    int? pasteValueInt = null;

                    // by this point we know that _there is_ something to paste.
                    btn_OK.Enabled = false;
                    btn_Cancel.Enabled = false;

                    // check it's sitting somewhere already?

                    bool dataInStage1QueuedInEditForm = false;
                    bool dataInStage3ReadyToWrite = false;
                    bool dataInFile = false;

                    if (dirElemFileToCopyFrom != null)
                    {
                        dataInStage1QueuedInEditForm = dirElemFileToCopyFrom.HasSpecificAttributeWithVersion(
                            attribute: attribute,
                            version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue);

                        dataInStage3ReadyToWrite = dirElemFileToCopyFrom.HasSpecificAttributeWithVersion(
                            attribute: attribute,
                            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);

                        dataInFile = dirElemFileToCopyFrom.HasSpecificAttributeWithVersion(attribute: attribute,
                                                                                           version: DirectoryElement.AttributeVersion.Original);

                        // see if data in temp-queue
                        if (dataInStage1QueuedInEditForm)
                        {
                            pasteValueStr = dirElemFileToCopyFrom.GetAttributeValueString(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue);

                            if (typeOfAttribute == typeof(double))
                            {
                                pasteValueDbl = dirElemFileToCopyFrom.GetAttributeValue<double>(
                                    attribute: attribute,
                                    version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue);
                            }
                            else if (typeOfAttribute == typeof(int))
                            {
                                pasteValueInt = dirElemFileToCopyFrom.GetAttributeValue<int>(
                                    attribute: attribute,
                                    version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue);
                            }
                        }
                        // see if data is ready to be written
                        else if (dataInStage3ReadyToWrite)
                        {
                            pasteValueStr = dirElemFileToCopyFrom.GetAttributeValueString(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);

                            if (typeOfAttribute == typeof(double))
                            {
                                pasteValueDbl = dirElemFileToCopyFrom.GetAttributeValue<double>(
                                    attribute: attribute,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                            }
                            else if (typeOfAttribute == typeof(int))
                            {
                                pasteValueInt = dirElemFileToCopyFrom.GetAttributeValue<int>(
                                    attribute: attribute,
                                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                            }
                        }
                        // take it from the file then
                        else if (dataInFile)
                        {
                            pasteValueStr = dirElemFileToCopyFrom.GetAttributeValueString(attribute: attribute, version: DirectoryElement.AttributeVersion.Original);
                            if (typeOfAttribute == typeof(double))
                            {
                                pasteValueDbl = dirElemFileToCopyFrom.GetAttributeValue<double>(
                                    attribute: attribute,
                                    version: DirectoryElement.AttributeVersion.Original);
                            }
                            else if (typeOfAttribute == typeof(int))
                            {
                                pasteValueInt = dirElemFileToCopyFrom.GetAttributeValue<int>(
                                    attribute: attribute,
                                    version: DirectoryElement.AttributeVersion.Original);
                            }
                        }

                        else
                        {
                            pasteValueStr = FrmMainApp.NullStringEquivalentGeneric;
                        }
                    }

                    if (pasteValueStr == FrmMainApp.NullStringEquivalentGeneric || pasteValueStr is null)
                    {
                        if (typeOfAttribute == typeof(double))
                        {
                            pasteValueStr = FrmMainApp.NullStringEquivalentZero;
                            pasteValueDbl = FrmMainApp.NullDoubleEquivalent;
                        }
                        else if (typeOfAttribute == typeof(int))
                        {
                            pasteValueStr = FrmMainApp.NullStringEquivalentZero;
                            pasteValueInt = FrmMainApp.NullIntEquivalent;
                        }
                        else if (typeOfAttribute == typeof(string))
                        {
                            pasteValueStr = FrmMainApp.NullStringEquivalentBlank;
                        }
                        else
                        {
                            throw new ArgumentException(message: "Trying to get attribute type of unknown attribute with value " + attributeStr);
                        }
                    }

                    if (typeOfAttribute == typeof(string))
                    {
                        copyPasteDict.Add(key: attribute, value: pasteValueStr);
                    }
                    else if (typeOfAttribute == typeof(int))
                    {
                        copyPasteDict.Add(key: attribute, value: pasteValueInt);
                    }
                    else if (typeOfAttribute == typeof(double))
                    {
                        copyPasteDict.Add(key: attribute, value: pasteValueDbl);
                    }
                    else
                    {
                        throw new ArgumentException(message: "Trying to get attribute type of unknown attribute with value " + attributeStr);
                    }
                }

                string fileNameWithoutPathToUpdate = null;

                if (frmEditFileDataInstance != null)
                {
                    lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                    // for each file
                    foreach (ListViewItem lvi in lvw.Items)
                    {
                        fileNameWithoutPathToUpdate = lvi.Text;
                        DirectoryElement dirElemFileToModify = FrmMainApp.DirectoryElements.FindElementByItemName(
                            FileNameWithPath: Path.Combine(path1: FrmMainApp.FolderName,
                                                           path2: fileNameWithoutPathToUpdate));
                        if (dirElemFileToModify != null)
                        {
                            bool takenShiftCopyPasteRequired = false;
                            bool createShiftCopyPasteRequired = false;
                            bool takenAlreadyShifted = false;
                            bool createAlreadyShifted = false;

                            // update each tag --> frmEditFileDataInstance
                            foreach (KeyValuePair<ElementAttribute, object> keyValuePair in copyPasteDict)
                            {
                                Type typeofPaste = GetAttributeType(attribute: keyValuePair.Key);
                                string attributeStr = GetAttributeName(attribute: keyValuePair.Key);

                                if (attributeStr.Contains(value: "Taken") && attributeStr.Contains(value: "Shift"))
                                {
                                    takenShiftCopyPasteRequired = true;
                                }

                                if (attributeStr.Contains(value: "Create") && attributeStr.Contains(value: "Shift"))
                                {
                                    createShiftCopyPasteRequired = true;
                                }

                                if (typeofPaste == typeof(string))
                                {
                                    // remove value if blank
                                    dirElemFileToModify.SetAttributeValueAnyType(attribute: keyValuePair.Key,
                                                                                 value: Convert.ToString(value: keyValuePair.Value, provider: CultureInfo.InvariantCulture),
                                                                                 version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                                                                 isMarkedForDeletion: keyValuePair.Value.ToString() == "");
                                }
                                else
                                {
                                    dirElemFileToModify.SetAttributeValueAnyType(attribute: keyValuePair.Key,
                                                                                 value: Convert.ToString(value: keyValuePair.Value, provider: CultureInfo.InvariantCulture),
                                                                                 version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                                                                                 isMarkedForDeletion: false);
                                }
                            }

                            // this little bit of cluster f.k is needed because when pasting data the "Shift" values get correctly pasted into wherever they need to go but the "Actual"
                            // changes do not, which is technically correct. The problem is then that they don't get written to the file either.

                            if (takenShiftCopyPasteRequired && !takenAlreadyShifted)
                            {
                                takenAlreadyShifted = CheckAdjustTakenTimeShiftActual(
                                    dirElemFileToModify: dirElemFileToModify,
                                    dirElemVersion: DirectoryElement.AttributeVersion
                                        .Stage1EditFormIntraTabTransferQueue);
                            }
                            else if (createShiftCopyPasteRequired && !createAlreadyShifted)
                            {
                                createAlreadyShifted = CheckAdjustCreateTimeShiftActual(
                                    dirElemFileToModify: dirElemFileToModify,
                                    dirElemVersion: DirectoryElement.AttributeVersion
                                        .Stage1EditFormIntraTabTransferQueue);
                            }
                        }
                    }
                }
            }
        }
        else if (_initiatorName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                // by this point we know that _there is_ something to paste.
                btn_OK.Enabled = false;
                btn_Cancel.Enabled = false;

                foreach (ElementAttribute attribute in tagsToPaste)
                {
                    string pasteValueStr = null;

                    bool dataInCopyDict = FrmMainApp.CopyPoolDict.Any(predicate: l => l.Key == attribute); // https://stackoverflow.com/a/57437756/3968494
                    // get the dataaaaa
                    if (dataInCopyDict)
                    {
                        pasteValueStr = FrmMainApp.CopyPoolDict.First(predicate: c => c.Key == attribute)
                            .Value; // https://stackoverflow.com/a/25298643/3968494
                        copyPasteDict.Add(key: attribute, value: pasteValueStr);
                    }
                    else // this will be marked as "remove" later
                    {
                        copyPasteDict.Add(key: attribute, value: FrmMainApp.NullStringEquivalentGeneric);
                    }
                }

                // for each file
                foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.SelectedItems)
                {
                    string fileNameWithoutPathToUpdate = lvi.Text;
                    DirectoryElement dirElemFileToModify = FrmMainApp.DirectoryElements.FindElementByItemName(FileNameWithPath: Path.Combine(path1: FrmMainApp.FolderName, path2: fileNameWithoutPathToUpdate));

                    if (dirElemFileToModify != null)
                    {
                        bool takenShiftCopyPasteRequired = false;
                        bool createShiftCopyPasteRequired = false;
                        bool takenAlreadyShifted = false;
                        bool createAlreadyShifted = false;

                        foreach (KeyValuePair<ElementAttribute, object> keyValuePair in copyPasteDict)
                        {
                            string attributeStr = GetAttributeName(attribute: keyValuePair.Key);

                            if (attributeStr.Contains(value: "Taken") && attributeStr.Contains(value: "Shift"))
                            {
                                takenShiftCopyPasteRequired = true;
                            }

                            if (attributeStr.Contains(value: "Create") && attributeStr.Contains(value: "Shift"))
                            {
                                createShiftCopyPasteRequired = true;
                            }

                            // remove value if blank
                            bool markForRemoval = keyValuePair.Value.ToString() == FrmMainApp.NullStringEquivalentGeneric || string.IsNullOrEmpty(value: keyValuePair.Value.ToString());
                            dirElemFileToModify.SetAttributeValueAnyType(attribute: keyValuePair.Key,
                                                                         value: keyValuePair.Value.ToString(),
                                                                         version: DirectoryElement.AttributeVersion
                                                                             .Stage3ReadyToWrite,
                                                                         isMarkedForDeletion: markForRemoval);
                        }

                        // this little bit of cluster f.k is needed because when pasting data the "Shift" values get correctly pasted into wherever they need to go but the "Actual"
                        // changes do not, which is technically correct. The problem is then that they don't get written to the file either.

                        if (takenShiftCopyPasteRequired && !takenAlreadyShifted)
                        {
                            takenAlreadyShifted = CheckAdjustTakenTimeShiftActual(
                                dirElemFileToModify: dirElemFileToModify,
                                dirElemVersion: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                        }
                        else if (createShiftCopyPasteRequired && !createAlreadyShifted)
                        {
                            createAlreadyShifted = CheckAdjustCreateTimeShiftActual(
                                dirElemFileToModify: dirElemFileToModify,
                                dirElemVersion: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                        }
                    }

                    // update listview
                    if (File.Exists(path: Path.Combine(path1: FrmMainApp.FolderName, path2: fileNameWithoutPathToUpdate)))
                    {
                        // check it's not in the read-queue.
                        while (HelperStatic.GenericLockCheckLockFile(fileNameWithoutPath: fileNameWithoutPathToUpdate))
                        {
                            await Task.Delay(millisecondsDelay: 10);
                        }

                        HelperStatic.FileListBeingUpdated = true;
                        await HelperStatic.LwvUpdateRowFromDEStage3ReadyToWrite(lvi: lvi);
                        FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Processing: " + fileNameWithoutPathToUpdate);
                        HelperStatic.FileListBeingUpdated = false;
                    }
                }

                // just for good measure
                HelperStatic.FileListBeingUpdated = false;
            }
        }

        Hide();

        bool CheckAdjustTakenTimeShiftActual(DirectoryElement dirElemFileToModify,
                                             DirectoryElement.AttributeVersion dirElemVersion)
        {
            int shiftedDays = 0;
            int shiftedHours = 0;
            int shiftedMinutes = 0;
            int shiftedSeconds = 0;

            shiftedDays = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: ElementAttribute.TakenDateDaysShift,
                version: dirElemVersion,
                notFoundValue: 0);

            shiftedHours = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: ElementAttribute.TakenDateHoursShift,
                version: dirElemVersion,
                notFoundValue: 0);

            shiftedMinutes = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: ElementAttribute.TakenDateMinutesShift,
                version: dirElemVersion,
                notFoundValue: 0);

            shiftedSeconds = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: ElementAttribute.TakenDateSecondsShift,
                version: dirElemVersion,
                notFoundValue: 0);

            int totalShiftedSeconds = shiftedSeconds +
                                      shiftedMinutes * 60 +
                                      shiftedHours * 60 * 60 +
                                      shiftedDays * 60 * 60 * 24;

            DateTime originalTakenDateTime = DateTime.Parse(
                s: FrmMainApp.OriginalTakenDateDict[key: dirElemFileToModify.ItemNameWithoutPath],
                provider: CultureInfo.CurrentUICulture);

            DateTime modifiedTakenDateTime = originalTakenDateTime.AddSeconds(value: totalShiftedSeconds);

            dirElemFileToModify.SetAttributeValueAnyType(attribute: ElementAttribute.TakenDate,
                                                         value: Convert.ToString(value: modifiedTakenDateTime, provider: CultureInfo.CurrentUICulture),
                                                         version: dirElemVersion,
                                                         isMarkedForDeletion: false);
            return true;
        }

        bool CheckAdjustCreateTimeShiftActual(DirectoryElement dirElemFileToModify,
                                              DirectoryElement.AttributeVersion dirElemVersion)
        {
            int shiftedDays = 0;
            int shiftedHours = 0;
            int shiftedMinutes = 0;
            int shiftedSeconds = 0;

            shiftedDays = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: ElementAttribute.CreateDateDaysShift,
                version: dirElemVersion,
                notFoundValue: 0);

            shiftedHours = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: ElementAttribute.CreateDateHoursShift,
                version: dirElemVersion,
                notFoundValue: 0);

            shiftedMinutes = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: ElementAttribute.CreateDateMinutesShift,
                version: dirElemVersion,
                notFoundValue: 0);

            shiftedSeconds = (int)dirElemFileToModify.GetAttributeValue<int>(
                attribute: ElementAttribute.CreateDateSecondsShift,
                version: dirElemVersion,
                notFoundValue: 0);

            int totalShiftedSeconds = shiftedSeconds +
                                      shiftedMinutes * 60 +
                                      shiftedHours * 60 * 60 +
                                      shiftedDays * 60 * 60 * 24;

            DateTime originalCreateDateTime = DateTime.Parse(
                s: FrmMainApp.OriginalCreateDateDict[key: dirElemFileToModify.ItemNameWithoutPath],
                provider: CultureInfo.CurrentUICulture);

            DateTime modifiedCreateDateTime = originalCreateDateTime.AddSeconds(value: totalShiftedSeconds);

            dirElemFileToModify.SetAttributeValueAnyType(attribute: ElementAttribute.CreateDate,
                                                         value: Convert.ToString(value: modifiedCreateDateTime, provider: CultureInfo.CurrentUICulture),
                                                         version: dirElemVersion,
                                                         isMarkedForDeletion: false);
            return true;
        }
    }


    private void btn_Cancel_Click(object sender,
                                  EventArgs e)
    {
        Hide();
    }

    private void btn_AllData_All_Click(object sender,
                                       EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this, type: typeof(CheckBox));
        foreach (Control cItem in c)
        {
            CheckBox thisCheckBox = (CheckBox)cItem;
            thisCheckBox.Checked = true;
        }
    }

    private void btn_AllData_None_Click(object sender,
                                        EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this, type: typeof(CheckBox));
        foreach (Control cItem in c)
        {
            CheckBox thisCheckBox = (CheckBox)cItem;
            thisCheckBox.Checked = false;
        }
    }


    private void btn_PullMostRecentPasteSettings_Click(object sender,
                                                       EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this, type: typeof(CheckBox));
        foreach (Control cItem in c)
        {
            CheckBox thisCheckBox = (CheckBox)cItem;
            thisCheckBox.Checked = _lastCheckedCheckBoxes.Contains(item: cItem.Name);
        }
    }

    #region GPSData

    private void btn_GPSData_All_Click(object sender,
                                       EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(CheckBox))
            {
                if (cItem.Parent.Name == "gbx_GPSData" && cItem.GetType() == typeof(CheckBox))
                {
                    CheckBox thisCheckBox = (CheckBox)cItem;
                    thisCheckBox.Checked = true;
                }
            }
        }
    }

    private void btn_GPSData_None_Click(object sender,
                                        EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(CheckBox))
            {
                if (cItem.Parent.Name == "gbx_GPSData" && cItem.GetType() == typeof(CheckBox))
                {
                    CheckBox thisCheckBox = (CheckBox)cItem;
                    thisCheckBox.Checked = false;
                }
            }
        }
    }

    #endregion

    #region LocationData

    private void btn_LocationData_All_Click(object sender,
                                            EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(CheckBox))
            {
                if (cItem.Parent.Name == "gbx_LocationData" && cItem.GetType() == typeof(CheckBox))
                {
                    CheckBox thisCheckBox = (CheckBox)cItem;
                    thisCheckBox.Checked = true;
                }
            }
        }
    }


    private void btn_LocationData_None_Click(object sender,
                                             EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(CheckBox))
            {
                if (cItem.Parent.Name == "gbx_LocationData" && cItem.GetType() == typeof(CheckBox))
                {
                    CheckBox thisCheckBox = (CheckBox)cItem;
                    thisCheckBox.Checked = false;
                }
            }
        }
    }

    #endregion

    #region DateTimes

    private void btn_Dates_All_Click(object sender,
                                     EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(CheckBox))
            {
                if (cItem.Parent.Name.Contains(value: "Date") && cItem.GetType() == typeof(CheckBox))
                {
                    CheckBox thisCheckBox = (CheckBox)cItem;
                    thisCheckBox.Checked = true;
                }
            }
        }
    }

    private void btn_Dates_None_Click(object sender,
                                      EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(CheckBox))
            {
                if (cItem.Parent.Name.Contains(value: "Date") && cItem.GetType() == typeof(CheckBox))
                {
                    CheckBox thisCheckBox = (CheckBox)cItem;
                    thisCheckBox.Checked = false;
                }
            }
        }
    }


    private void ckb_TakenDate_CheckedChanged(object sender,
                                              EventArgs e)
    {
        rbt_PasteTakenDateActual.Enabled = ckb_TakenDate.Checked;
        if (ckb_TakenDate.Checked)
        {
            rbt_PasteTakenDateActual.Checked = true;
        }

        rbt_PasteTakenDateShift.Enabled = ckb_TakenDate.Checked;
    }

    private void ckb_CreateDate_CheckedChanged(object sender,
                                               EventArgs e)
    {
        rbt_PasteCreateDateActual.Enabled = ckb_CreateDate.Checked;
        if (ckb_CreateDate.Checked)
        {
            rbt_PasteCreateDateActual.Checked = true;
        }

        rbt_PasteCreateDateShift.Enabled = ckb_CreateDate.Checked;
    }


    private void rbt_PasteTakenDateShift_CheckedChanged(object sender,
                                                        EventArgs e)
    {
        if (rbt_PasteTakenDateShift.Checked)
        {
            bool takenDateShiftDataExists = false;

            List<ElementAttribute> listOfTagsToCopyTimeShifts = new()
            {
                ElementAttribute.TakenDateSecondsShift,
                ElementAttribute.TakenDateMinutesShift,
                ElementAttribute.TakenDateHoursShift,
                ElementAttribute.TakenDateDaysShift
            };

            if (_initiatorName == "FrmEditFileData")
            {
                FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];

                if (frmEditFileDataInstance != null)
                {
                    ListView lvw = frmEditFileDataInstance.lvw_FileListEditImages;
                    ListViewItem lvi = lvw.SelectedItems[index: 0];
                    DirectoryElement dirElemFileSource = FrmMainApp.DirectoryElements.FindElementByItemName(FileNameWithPath: Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text));

                    // when USING EDIT we use the local data, therefore timeshifts can only possibly live in DtFileDataToWriteStage1PreQueue
                    foreach (ElementAttribute attribute in listOfTagsToCopyTimeShifts)
                    {
                        if (dirElemFileSource.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue))
                        {
                            takenDateShiftDataExists = true;
                            break;
                        }
                    }
                }
            }
            else if (_initiatorName == "FrmMainApp")
            {
                // see if there is actually any paste-able data for the SOURCE file
                foreach (ElementAttribute attribute in listOfTagsToCopyTimeShifts)
                {
                    bool dataInCopyKVP = FrmMainApp.CopyPoolDict.Any(predicate: l => l.Key == attribute); // https://stackoverflow.com/a/57437756/3968494
                    if (dataInCopyKVP)
                    {
                        takenDateShiftDataExists = true;
                        break;
                    }
                }
            }

            if (!takenDateShiftDataExists)
            {
                rbt_PasteTakenDateActual.Checked = true;
                MessageBox.Show(
                    text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmPasteWhat_NoDateShiftToPaste"),
                    caption: HelperStatic.GenericGetMessageBoxCaption(captionType: "Info"));
            }
        }
    }

    private void rbt_PasteCreateDateShift_CheckedChanged(object sender,
                                                         EventArgs e)
    {
        bool createDateShiftDataExists = false;

        List<ElementAttribute> listOfTagsToCopyTimeShifts = new()
        {
            ElementAttribute.CreateDateSecondsShift,
            ElementAttribute.CreateDateMinutesShift,
            ElementAttribute.CreateDateHoursShift,
            ElementAttribute.CreateDateDaysShift
        };

        FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];

        if (frmEditFileDataInstance != null)
        {
            ListView lvw = frmEditFileDataInstance.lvw_FileListEditImages;
            ListViewItem lvi = lvw.SelectedItems[index: 0];
            DirectoryElement dirElemFileSource = FrmMainApp.DirectoryElements.FindElementByItemName(FileNameWithPath: Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text));

            // when USING EDIT we use the local data, therefore timeshifts can only possibly live in DtFileDataToWriteStage1PreQueue
            foreach (ElementAttribute attribute in listOfTagsToCopyTimeShifts)
            {
                if (dirElemFileSource.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue))
                {
                    createDateShiftDataExists = true;
                    break;
                }
            }
        }
        else if (_initiatorName == "FrmMainApp")
        {
            // see if there is actually any paste-able data for the SOURCE file
            foreach (ElementAttribute attribute in listOfTagsToCopyTimeShifts)
            {
                bool dataInCopyKVP = FrmMainApp.CopyPoolDict.Any(predicate: l => l.Key == attribute); // https://stackoverflow.com/a/57437756/3968494
                if (dataInCopyKVP)
                {
                    createDateShiftDataExists = true;
                    break;
                }
            }
        }

        if (!createDateShiftDataExists)
        {
            rbt_PasteCreateDateActual.Checked = true;
            MessageBox.Show(
                text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmPasteWhat_NoDateShiftToPaste"),
                caption: HelperStatic.GenericGetMessageBoxCaption(captionType: "Info"));
        }
    }

    #endregion
}