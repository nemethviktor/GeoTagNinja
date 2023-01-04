using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja;

public partial class FrmPasteWhat : Form
{
    private static string _initiatorName;

    /// <summary>
    ///     This form controls what data to paste from a "current file" to "selected files".
    /// </summary>
    /// <param name="initiator">This will be either the Edit File Form (FrmEditFileData) or the Main Form(FrmMainApp)</param>
    public FrmPasteWhat(string initiator)
    {
        _initiatorName = initiator;
        InitializeComponent();

        ListView lvw;
        string fileNameSourceWithoutPath;
        List<string> tagsToPasteList = null;

        // get the name of the file we're pasting FROM.
        // in this case this will be used to pre-fill/check the checkboxes
        if (_initiatorName == "FrmEditFileData")
        {
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
            if (frmEditFileDataInstance != null)
            {
                lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                fileNameSourceWithoutPath = lvw.SelectedItems[index: 0]
                    .Text;

                // stuff will live in DT1
                tagsToPasteList = GetTagsToPaste(dt: FrmMainApp.DtFileDataToWriteStage1PreQueue);
            }
        }
        else if (_initiatorName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                lvw = frmMainAppInstance.lvw_FileList;

                fileNameSourceWithoutPath = Path.GetFileName(path: FrmMainApp.FileDateCopySourceFileNameWithPath);

                // stuff will live in DT3
                tagsToPasteList = GetTagsToPaste(dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite);
                // Basically there is no requirement per se that DT3 is in fact filled in for this option.
                // It is entirely reasonable that user wants to copypaste not just edited bits.
                // In that case however there won't be data in the list and so no defaults, which I think is sensible.
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
            if (cItem is CheckBox)
            {
                string tagName;
                if (cItem.Name.Substring(startIndex: 4) == "OffsetTime")

                {
                    tagName = "OffsetTimeList"; // fml. basically the actual tbx_OffsetTimeList is a TextBox so it would not be picked up as a change.
                }
                else
                {
                    tagName = cItem.Name.Substring(startIndex: 4);
                }

                if (tagsToPasteList.Contains(item: tagName))
                {
                    CheckBox sndr = (CheckBox)cItem;
                    sndr.Checked = true;
                }
            }
        }

        rbt_PasteTakenDateActual.Enabled = ckb_TakenDate.Checked;
        rbt_PasteTakenDateShift.Enabled = ckb_TakenDate.Checked;

        rbt_PasteCreateDateActual.Enabled = ckb_CreateDate.Checked;
        rbt_PasteCreateDateShift.Enabled = ckb_CreateDate.Checked;

        // enable the shift-radiobuttons if there's data
        if (tagsToPasteList != null)
        {
            foreach (string tagName in tagsToPasteList)
            {
                if (tagName.StartsWith(value: "TakenDate") && tagName.EndsWith(value: "Shift"))
                {
                    rbt_PasteTakenDateShift.Checked = true;
                }
                else if (tagName.StartsWith(value: "CreateDate") && tagName.EndsWith(value: "Shift"))
                {
                    rbt_PasteCreateDateShift.Checked = true;
                }
            }
        }

        List<string> GetTagsToPaste(DataTable dt)
        {
            List<string> tagsList = new();
            EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in dt.AsEnumerable()
                                                               where dataRow.Field<string>(columnName: "fileNameWithoutPath") == fileNameSourceWithoutPath
                                                               select dataRow;

            Parallel.ForEach(source: drDataTableData, body: dataRow =>
                {
                    string settingId = dataRow[columnName: "settingId"]
                        .ToString();
                    tagsList.Add(item: settingId);
                })
                ;
            return tagsList;
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
    }

    /// <summary>
    ///     Updates the various relevant write-queues. For the Edit Form that's Q1, for the Main Form it's Q3.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    /// <exception cref="NotImplementedException">Warn that I did something incorrect.</exception>
    private async void btn_OK_Click(object sender,
                                    EventArgs e)
    {
        FrmMainApp.DtFileDataPastePool.Clear();

        // create a list of tags that also have a Ref version
        List<string> tagsWithRefList = new()
        {
            "GPSAltitude", "GPSDestLatitude", "GPSDestLongitude", "GPSImgDirection", "GPSLatitude", "GPSLongitude", "GPSSpeed"
        };

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);

        ListView lvw;
        string fileNameSourceWithoutPath = null;
        string fileNameSourceWithPath = null;

        string pasteValueStr = null;

        // get the name of the file we're pasting FROM.
        if (_initiatorName == "FrmEditFileData")
        {
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
            if (frmEditFileDataInstance != null)
            {
                lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                fileNameSourceWithoutPath = lvw.SelectedItems[index: 0]
                    .Text;
                fileNameSourceWithPath = Path.Combine(path1: FrmMainApp.FolderName, path2: fileNameSourceWithoutPath);
            }
        }
        else if (_initiatorName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                lvw = frmMainAppInstance.lvw_FileList;

                fileNameSourceWithoutPath = Path.GetFileName(path: FrmMainApp.FileDateCopySourceFileNameWithPath);
                fileNameSourceWithPath = FrmMainApp.FileDateCopySourceFileNameWithPath;
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        // get a list of tag names to paste
        List<string> tagsToPaste = new();
        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(CheckBox))
            {
                CheckBox thisCheckBox = (CheckBox)cItem;
                if (thisCheckBox.Checked)
                {
                    string tagName = cItem.Name.Substring(startIndex: 4);

                    // "EndsWith" doesn't work here because the CheckBox.Name never ends with "Shift".
                    if (tagName == "TakenDate" || tagName == "CreateDate")
                    {
                        string pasteWhichDate = tagName.Replace(oldValue: "Date", newValue: "");
                        string[] timeUnitArr = { "Days", "Hours", "Minutes", "Seconds" };
                        switch (pasteWhichDate)
                        {
                            case "Taken":
                                if (rbt_PasteTakenDateActual.Checked)
                                {
                                    tagsToPaste.Add(item: tagName);
                                }
                                else if (rbt_PasteTakenDateShift.Checked)
                                {
                                    // want: TakenDateDaysShift
                                    foreach (string timeUnit in timeUnitArr)
                                    {
                                        tagsToPaste.Add(item: tagName + timeUnit + "Shift");
                                    }
                                }

                                break;
                            case "Create":
                                if (rbt_PasteCreateDateActual.Checked)
                                {
                                    tagsToPaste.Add(item: tagName);
                                }
                                else if (rbt_PasteCreateDateShift.Checked)
                                {
                                    foreach (string timeUnit in timeUnitArr)
                                    {
                                        tagsToPaste.Add(item: tagName + timeUnit + "Shift");
                                    }
                                }

                                break;
                        }
                    }
                    // also do all the CountryCode 
                    else if (tagName == "Country")
                    {
                        tagsToPaste.Add(item: tagName);
                        tagsToPaste.Add(item: "CountryCode");
                    }
                    else
                    {
                        tagsToPaste.Add(item: tagName);
                    }

                    // any in the Ref lot
                    if (tagsWithRefList.Contains(item: tagName))
                    {
                        tagsToPaste.Add(item: tagName + "Ref");
                    }
                }
            }
        }

        // stick the tagNames + values into DtFileDataPastePool
        foreach (string tagName in tagsToPaste)
        {
            // by this point we know there is something to paste.
            btn_OK.Enabled = false;
            btn_Cancel.Enabled = false;

            // check it's sitting somewhere already?
            DataTable dtSqlDataQ;
            try
            {
                dtSqlDataQ = FrmMainApp.DtFileDataToWriteStage1PreQueue.Select(filterExpression: "fileNameWithoutPath = '" + fileNameSourceWithoutPath + "' AND settingId ='" + tagName + "'")
                    .CopyToDataTable();
            }
            catch
            {
                dtSqlDataQ = null;
            }

            DataTable dtSqlDataReadyToWrite;
            try
            {
                dtSqlDataReadyToWrite = FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Select(filterExpression: "fileNameWithoutPath = '" + fileNameSourceWithoutPath + "' AND settingId ='" + tagName + "'")
                    .CopyToDataTable();
            }
            catch
            {
                dtSqlDataReadyToWrite = null;
            }

            DataTable dtSqlDataInFile;
            try
            {
                dtSqlDataInFile = FrmMainApp.DtFileDataSeenInThisSession.Select(filterExpression: "fileNameWithPath = '" + fileNameSourceWithPath + "' AND settingId ='" + tagName + "'")
                    .CopyToDataTable();
            }
            catch
            {
                dtSqlDataInFile = null;
            }

            // see if data in temp-queue
            if (dtSqlDataQ != null && dtSqlDataQ.Rows.Count > 0)
            {
                pasteValueStr = dtSqlDataQ.Rows[index: 0][columnName: "settingValue"]
                    .ToString();
            }
            // see if data is ready to be written
            else if (dtSqlDataReadyToWrite != null && dtSqlDataReadyToWrite.Rows.Count > 0)
            {
                pasteValueStr = dtSqlDataReadyToWrite.Rows[index: 0][columnName: "settingValue"]
                    .ToString();
            }
            // take it from the file then
            else if (dtSqlDataInFile != null && dtSqlDataInFile.Rows.Count > 0)
            {
                pasteValueStr = dtSqlDataInFile.Rows[index: 0][columnName: "settingValue"]
                    .ToString();
            }

            else
            {
                pasteValueStr = "-";
            }

            if (pasteValueStr == "-" || pasteValueStr is null)
            {
                if (tagName.EndsWith(value: "Shift"))
                {
                    pasteValueStr = "0";
                }
                else
                {
                    pasteValueStr = "";
                }
            }

            DataRow newDr = FrmMainApp.DtFileDataPastePool.NewRow();
            newDr[columnName: "settingId"] = tagName;
            newDr[columnName: "settingValue"] = pasteValueStr;
            FrmMainApp.DtFileDataPastePool.Rows.Add(row: newDr);
            FrmMainApp.DtFileDataPastePool.AcceptChanges();
        }

        // do paste into the tables + grid as req'd
        if (_initiatorName == "FrmEditFileData")
        {
            string fileNameWithoutPathToUpdate = null;
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
            if (frmEditFileDataInstance != null)
            {
                lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                // for each file
                foreach (ListViewItem lvi in lvw.Items)
                {
                    fileNameWithoutPathToUpdate = lvi.Text;
                    // update each tag
                    foreach (DataRow drPRow in FrmMainApp.DtFileDataPastePool.Rows)
                    {
                        string settingId = drPRow[columnIndex: 0]
                            .ToString();
                        string settingValue = drPRow[columnIndex: 1]
                            .ToString();

                        // this is for FrmEditData -> no need to separate the "Shifts".
                        HelperStatic.GenericUpdateAddToDataTable(
                            dt: FrmMainApp.DtFileDataToWriteStage1PreQueue,
                            fileNameWithoutPath: fileNameWithoutPathToUpdate,
                            settingId: settingId,
                            settingValue: settingValue);
                    }
                }
            }
        }

        else if (_initiatorName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                // for each file
                foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.SelectedItems)
                {
                    string fileNameWithoutPathToUpdate = lvi.Text;

                    // TimeShifts go separately. -- Also while this could be done in one step it's easier for now if we split Taken and Create
                    // TakenDate
                    DataRow[] dtDateTakenDateShifted = FrmMainApp.DtFileDataPastePool.Select(filterExpression: "settingId LIKE 'Taken%' AND settingId LIKE '%Shift'");
                    if (dtDateTakenDateShifted.Length > 0)
                    {
                        int shiftedDays = 0;
                        int shiftedHours = 0;
                        int shiftedMinutes = 0;
                        int shiftedSeconds = 0;
                        foreach (DataRow drPRow in dtDateTakenDateShifted)
                        {
                            switch (drPRow[columnIndex: 0])
                            {
                                case "TakenDateDaysShift":
                                    shiftedDays = int.Parse(s: drPRow[columnIndex: 1]
                                                                .ToString());
                                    break;
                                case "TakenDateHoursShift":
                                    shiftedHours = int.Parse(s: drPRow[columnIndex: 1]
                                                                 .ToString());
                                    break;
                                case "TakenDateMinutesShift":
                                    shiftedMinutes = int.Parse(s: drPRow[columnIndex: 1]
                                                                   .ToString());
                                    break;
                                case "TakenDateSecondsShift":
                                    shiftedSeconds = int.Parse(s: drPRow[columnIndex: 1]
                                                                   .ToString());
                                    break;
                            }
                        }

                        int totalShiftedSeconds = shiftedSeconds +
                                                  shiftedMinutes * 60 +
                                                  shiftedHours * 60 * 60 +
                                                  shiftedDays * 60 * 60 * 24;

                        DataRow[] drTakenDate = FrmMainApp.DtOriginalTakenDate.Select(filterExpression: "fileNameWithoutPath = '" + fileNameWithoutPathToUpdate + "'");
                        if (drTakenDate.Length > 0)
                        {
                            DateTime originalTakenDateTime = Convert.ToDateTime(value: drTakenDate[0][columnName: "settingValue"]
                                                                                    .ToString());

                            DateTime modifiedTakenDateTime = originalTakenDateTime.AddSeconds(value: totalShiftedSeconds);
                            HelperStatic.GenericUpdateAddToDataTable(
                                dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                                fileNameWithoutPath: fileNameWithoutPathToUpdate,
                                settingId: "TakenDate",
                                settingValue: modifiedTakenDateTime.ToString(provider: CultureInfo.CurrentUICulture));
                        }
                    }

                    // CreateDate
                    DataRow[] dtDateCreateDateShifted = FrmMainApp.DtFileDataPastePool.Select(filterExpression: "settingId LIKE 'Create%' AND settingId LIKE '%Shift'");
                    if (dtDateCreateDateShifted.Length > 0)
                    {
                        int shiftedDays = 0;
                        int shiftedHours = 0;
                        int shiftedMinutes = 0;
                        int shiftedSeconds = 0;
                        foreach (DataRow drPRow in dtDateCreateDateShifted)
                        {
                            switch (drPRow[columnIndex: 0])
                            {
                                case "CreateDateDaysShift":
                                    shiftedDays = int.Parse(s: drPRow[columnIndex: 1]
                                                                .ToString());
                                    break;
                                case "CreateDateHoursShift":
                                    shiftedHours = int.Parse(s: drPRow[columnIndex: 1]
                                                                 .ToString());
                                    break;
                                case "CreateDateMinutesShift":
                                    shiftedMinutes = int.Parse(s: drPRow[columnIndex: 1]
                                                                   .ToString());
                                    break;
                                case "CreateDateSecondsShift":
                                    shiftedSeconds = int.Parse(s: drPRow[columnIndex: 1]
                                                                   .ToString());
                                    break;
                            }
                        }

                        int totalShiftedSeconds = shiftedSeconds +
                                                  shiftedMinutes * 60 +
                                                  shiftedHours * 60 * 60 +
                                                  shiftedDays * 60 * 60 * 24;

                        DataRow[] drCreateDate = FrmMainApp.DtOriginalCreateDate.Select(filterExpression: "fileNameWithoutPath = '" + fileNameWithoutPathToUpdate + "'");
                        if (drCreateDate.Length > 0)
                        {
                            DateTime originalCreateDateTime = Convert.ToDateTime(value: drCreateDate[0][columnName: "settingValue"]
                                                                                     .ToString());

                            DateTime modifiedCreateDateTime = originalCreateDateTime.AddSeconds(value: totalShiftedSeconds);
                            HelperStatic.GenericUpdateAddToDataTable(
                                dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                                fileNameWithoutPath: fileNameWithoutPathToUpdate,
                                settingId: "CreateDate",
                                settingValue: modifiedCreateDateTime.ToString(provider: CultureInfo.CurrentUICulture));
                        }
                    }

                    // update each non-TimeShift Tag
                    DataRow[] dtDateNotShifted = FrmMainApp.DtFileDataPastePool.Select(filterExpression: "settingId NOT LIKE '%Shift'");
                    if (dtDateNotShifted.Length > 0)
                    {
                        foreach (DataRow drPRow in dtDateNotShifted)
                        {
                            string settingId = drPRow[columnIndex: 0]
                                .ToString();
                            string settingValue = drPRow[columnIndex: 1]
                                .ToString();

                            if (settingValue == "-")
                            {
                                settingValue = "";
                            }

                            HelperStatic.GenericUpdateAddToDataTable(
                                dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                                fileNameWithoutPath: fileNameWithoutPathToUpdate,
                                settingId: settingId,
                                settingValue: settingValue);
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
                        await HelperStatic.LwvUpdateRowFromDTWriteStage3ReadyToWrite(lvi: lvi);
                        FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Processing: " + fileNameWithoutPathToUpdate);
                        HelperStatic.FileListBeingUpdated = false;
                    }
                }

                // just for good measure
                HelperStatic.FileListBeingUpdated = false;
            }
        }

        Hide();
    }


    private void btn_Cancel_Click(object sender,
                                  EventArgs e)
    {
        Hide();
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

            if (_initiatorName == "FrmEditFileData")
            {
                FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
                if (frmEditFileDataInstance != null)
                {
                    ListView lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                    string fileNameSourceWithoutPath = lvw.SelectedItems[index: 0]
                        .Text;

                    List<string> listOfTagsToCopyTimeShifts = new()
                    {
                        "TakenDateSecondsShift",
                        "TakenDateMinutesShift",
                        "TakenDateHoursShift ",
                        "TakenDateDaysShift"
                    };
                    // when USING EDIT we use the local data, therefore timeshifts can only possibly live in DtFileDataToWriteStage1PreQueue
                    foreach (string settingId in listOfTagsToCopyTimeShifts)
                    {
                        DataRow[] dtDateShifted = FrmMainApp.DtFileDataToWriteStage1PreQueue.Select(filterExpression: "fileNameWithoutPath = '" + fileNameSourceWithoutPath + "' AND settingId = '" + settingId + "'");
                        if (dtDateShifted.Length > 0)
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
                string sourceFileNameWithoutPath = FrmMainApp.DtFileDataCopyPool.Rows[index: 0][columnName: "fileNameWithoutPath"]
                    .ToString();
                DataRow[] dtDateShifted = FrmMainApp.DtFileDataCopyPool.Select(filterExpression: "fileNameWithoutPath = '" + sourceFileNameWithoutPath + "' AND settingId LIKE 'TakenDate%' AND settingId LIKE '%Shift'");

                if (dtDateShifted.Length > 0)
                {
                    takenDateShiftDataExists = true;
                }
            }

            if (!takenDateShiftDataExists)
            {
                rbt_PasteTakenDateActual.Checked = true;
                MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmPasteWhat_NoDateShiftToPaste"), caption: "Info");
            }
        }
    }

    private void rbt_PasteCreateDateShift_CheckedChanged(object sender,
                                                         EventArgs e)
    {
        bool CreateDateShiftDataExists = false;

        if (_initiatorName == "FrmEditFileData")
        {
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
            if (frmEditFileDataInstance != null)
            {
                ListView lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                string fileNameSourceWithoutPath = lvw.SelectedItems[index: 0]
                    .Text;

                List<string> listOfTagsToCopyTimeShifts = new()
                {
                    "CreateDateSecondsShift",
                    "CreateDateMinutesShift",
                    "CreateDateHoursShift ",
                    "CreateDateDaysShift"
                };
                // when USING EDIT we use the local data, therefore timeshifts can only possibly live in DtFileDataToWriteStage1PreQueue
                foreach (string settingId in listOfTagsToCopyTimeShifts)
                {
                    DataRow[] dtDateShifted = FrmMainApp.DtFileDataToWriteStage1PreQueue.Select(filterExpression: "fileNameWithoutPath = '" + fileNameSourceWithoutPath + "' AND settingId = '" + settingId + "'");
                    if (dtDateShifted.Length > 0)
                    {
                        CreateDateShiftDataExists = true;
                        break;
                    }
                }
            }
        }
        else if (_initiatorName == "FrmMainApp")
        {
            // see if there is actually any paste-able data for the SOURCE file
            string sourceFileNameWithoutPath = FrmMainApp.DtFileDataCopyPool.Rows[index: 0][columnName: "fileNameWithoutPath"]
                .ToString();
            DataRow[] dtDateShifted = FrmMainApp.DtFileDataCopyPool.Select(filterExpression: "fileNameWithoutPath = '" + sourceFileNameWithoutPath + "' AND settingId LIKE 'CreateDate%' AND settingId LIKE '%Shift'");

            if (dtDateShifted.Length > 0)
            {
                CreateDateShiftDataExists = true;
            }
        }

        if (!CreateDateShiftDataExists)
        {
            rbt_PasteCreateDateActual.Checked = true;
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmPasteWhat_NoDateShiftToPaste"), caption: "Info");
        }
    }

    #endregion
}