using GeoTagNinja.Properties;
using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja;

public partial class FrmPasteWhat : Form
{
    private static string initiatorName;

    /// <summary>
    ///     This form controls what data to paste from a "current file" to "selected files".
    /// </summary>
    /// <param name="initiator">This will be either the Edit File Form (FrmEditFileData) or the Main Form(FrmMainApp)</param>
    public FrmPasteWhat(string initiator)
    {
        initiatorName = initiator;
        InitializeComponent();

        //// pre-load "All"
        //btn_GPSData_All_Click(sender: null, e: null);
        //btn_LocationData_All_Click(sender: null, e: null);
        //btn_Dates_None_Click(sender: null, e: null);
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
            if (cItem.GetType() == typeof(Label) || cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(Button) || cItem.GetType() == typeof(CheckBox) || cItem.GetType() == typeof(TabPage))
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
        if (initiatorName == "FrmEditFileData")
        {
            FrmMainApp.DtFileDataPastePool.Clear();
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
            if (frmEditFileDataInstance != null)
            {
                lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                fileNameSourceWithoutPath = lvw.SelectedItems[index: 0]
                    .Text;
                fileNameSourceWithPath = Path.Combine(path1: FrmMainApp.FolderName, path2: fileNameSourceWithoutPath);
            }
        }
        else if (initiatorName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms["FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                lvw = frmMainAppInstance.lvw_FileList;

                fileNameSourceWithoutPath = Path.GetFileName(FrmMainApp.FileDateCopySourceFileNameWithPath);
                fileNameSourceWithPath = FrmMainApp.FileDateCopySourceFileNameWithPath;
                ;
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        // get a list of tag names to paste
        List<string> tagsToPaste = new List<string>();
        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(CheckBox))
            {
                CheckBox thisCheckBox = (CheckBox)cItem;
                if (thisCheckBox.Checked)
                {
                    string tagName = cItem.Name.Substring(startIndex: 4);
                    tagsToPaste.Add(tagName);

                    // any in the Ref lot
                    if (tagsWithRefList.Contains(item: tagName))
                    {
                        tagsToPaste.Add(tagName + "Ref");
                    }

                    // also do all the CountryCode 
                    if (tagName == "Country")
                    {
                        tagsToPaste.Add("CountryCode");
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
            DataView dvSqlDataQ = new(table: FrmMainApp.DtFileDataToWriteStage1PreQueue);
            dvSqlDataQ.RowFilter = "fileNameWithoutPath = '" + fileNameSourceWithoutPath + "' AND settingId ='" + tagName + "'";

            DataView dvSqlDataRTW = new(table: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite);
            dvSqlDataRTW.RowFilter = "fileNameWithoutPath = '" + fileNameSourceWithoutPath + "' AND settingId ='" + tagName + "'";

            DataView dvSqlDataInFile = new(table: FrmMainApp.DtFileDataSeenInThisSession);
            // this holds fileNameWithPath with a Directory attached to the string 
            dvSqlDataInFile.RowFilter = "fileNameWithPath = '" + fileNameSourceWithPath + "' AND settingId ='" + tagName + "'";

            if (dvSqlDataQ.Count > 0 || dvSqlDataRTW.Count > 0 || dvSqlDataInFile.Count > 0)
            {
                // see if data in temp-queue
                if (dvSqlDataQ.Count > 0)
                {
                    pasteValueStr = dvSqlDataQ[recordIndex: 0][property: "settingValue"]
                        .ToString();
                }
                // see if data is ready to be written
                else if (dvSqlDataRTW.Count > 0)
                {
                    pasteValueStr = dvSqlDataRTW[recordIndex: 0][property: "settingValue"]
                        .ToString();
                }
                // take it from the file then
                else if (dvSqlDataInFile.Count > 0)
                {
                    pasteValueStr = dvSqlDataInFile[recordIndex: 0][property: "settingValue"]
                        .ToString();
                }

                if (pasteValueStr == "-")
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
        if (initiatorName == "FrmEditFileData")
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
                        string settingId = drPRow[0]
                            .ToString();
                        string settingValue = drPRow[1]
                            .ToString();

                        // this is for FrmEditData
                        HelperStatic.GenericUpdateAddToDataTable(
                            dt: FrmMainApp.DtFileDataToWriteStage1PreQueue,
                            fileNameWithoutPath: fileNameWithoutPathToUpdate,
                            settingId: settingId,
                            settingValue: settingValue);
                    }
                }
            }
        }

        else if (initiatorName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                // for each file
                foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.SelectedItems)
                {
                    string fileNameWithoutPathToUpdate = lvi.Text;

                    // paste from copy-pool (no filename needed)
                    // update each tag
                    foreach (DataRow drPRow in FrmMainApp.DtFileDataPastePool.Rows)
                    {
                        string settingId = drPRow[0]
                            .ToString();
                        string settingValue = drPRow[1]
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

    #region Dates

    private void btn_Dates_All_Click(object sender,
                                     EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem.GetType() == typeof(GroupBox) || cItem.GetType() == typeof(CheckBox))
            {
                if (cItem.Parent.Name == "gbx_Dates" && cItem.GetType() == typeof(CheckBox))
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
                if (cItem.Parent.Name == "gbx_Dates" && cItem.GetType() == typeof(CheckBox))
                {
                    CheckBox thisCheckBox = (CheckBox)cItem;
                    thisCheckBox.Checked = false;
                }
            }
        }
    }

    #endregion

    #endregion
}