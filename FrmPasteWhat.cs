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

        // pre-load "All"
        btn_GPSData_All_Click(sender: null, e: null);
        btn_LocationData_All_Click(sender: null, e: null);
    }

    /// <summary>
    /// Adds text to the labels.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    private void FrmPasteWhat_Load(object sender,
                                   EventArgs e)
    {
        HelperNonStatic helperNonstatic = new();
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
    /// Updates the various relevant write-queues. For the Edit Form that's Q1, for the Main Form it's Q3.
    /// </summary>
    /// <param name="sender">Unused</param>
    /// <param name="e">Unused</param>
    /// <exception cref="NotImplementedException">Warn that I did something incorrect.</exception>
    private async void btn_OK_Click(object sender,
                                    EventArgs e)
    {
        // create a list of tags that also have a Ref version
        List<string> tagsWithRefList = new()
        {
            "GPSAltitude", "GPSDestLatitude", "GPSDestLongitude", "GPSImgDirection", "GPSLatitude", "GPSLongitude", "GPSSpeed"
        };

        if (initiatorName == "FrmEditFileData")
        {
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
            if (frmEditFileDataInstance != null)
            {
                ListView lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                foreach (ListViewItem lvi in lvw.Items)
                {
                    PasteDataToLvi(lvi.Text);
                }
            }
        }
        else if (initiatorName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                ListView lvw = frmMainAppInstance.lvw_FileList;
                HelperStatic.FileListBeingUpdated = true;
                foreach (ListViewItem lvi in lvw.SelectedItems)
                {
                    if (File.Exists(Path.Combine(FrmMainApp.FolderName, lvi.Text)))
                    {
                        // check it's not in the read-queue.
                        while (HelperStatic.GenericLockCheckLockFile(fileNameWithOutPath: lvi.Text))
                        {
                            await Task.Delay(millisecondsDelay: 10);
                        }

                        PasteDataToLvi(fileName: lvi.Text);
                    }

                    await HelperStatic.LwvUpdateRowFromDTWriteStage3ReadyToWrite(lvi: lvi);
                }

                HelperStatic.FileListBeingUpdated = false;
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        Hide();

        void PasteDataToLvi(string fileName)
        {
            HelperNonStatic helperNonstatic = new();
            IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
            foreach (Control cItem in c)
            {
                if (cItem.GetType() == typeof(CheckBox))
                {
                    CheckBox thisCheckBox = (CheckBox)cItem;
                    if (thisCheckBox.Checked)
                    {
                        string tagName = cItem.Name.Substring(startIndex: 4);
                        UpdateFileDataPaste(tagName: tagName, fileNameWithOutPathToUpdate: fileName);

                        // also do all the Refs 
                        if (tagsWithRefList.Contains(item: tagName))
                        {
                            UpdateFileDataPaste(tagName: tagName + "Ref", fileNameWithOutPathToUpdate: fileName);
                        }

                        // also do all the CountryCode 
                        if (tagName == "Country")
                        {
                            UpdateFileDataPaste(tagName: "CountryCode", fileNameWithOutPathToUpdate: fileName);
                        }
                    }
                }
            }
        }
    }

    private static void UpdateFileDataPaste(string tagName,
                                            string fileNameWithOutPathToUpdate
    )
    {
        if (initiatorName == "FrmEditFileData")
        {
            FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
            if (frmEditFileDataInstance != null)
            {
                ListView lvw = frmEditFileDataInstance.lvw_FileListEditImages;

                string fileNameWithOutPath = lvw.SelectedItems[index: 0]
                    .Text;
                string fileNameWithPath = Path.Combine(path1: FrmMainApp.FolderName, path2: fileNameWithOutPath);
                string pasteValueStr = "";

                // check it's sitting somewhere already?
                DataView dvSqlDataQ = new(table: FrmMainApp.DtFileDataToWriteStage1PreQueue);
                dvSqlDataQ.RowFilter = "fileNameWithOutPath = '" + fileNameWithOutPath + "' AND settingId ='" + tagName + "'";

                DataView dvSqlDataRTW = new(table: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite);
                dvSqlDataRTW.RowFilter = "fileNameWithOutPath = '" + fileNameWithOutPath + "' AND settingId ='" + tagName + "'";

                DataView dvSqlDataInFile = new(table: FrmMainApp.DtFileDataSeenInThisSession);
                // this holds fileNameWithPath with a Directory attached to the string 
                dvSqlDataInFile.RowFilter = "fileNameWithPath = '" + fileNameWithPath + "' AND settingId ='" + tagName + "'";

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

                    HelperStatic.GenericUpdateAddToDataTable(
                        dt: FrmMainApp.DtFileDataToWriteStage1PreQueue,
                        fileNameWithoutPath: fileNameWithOutPathToUpdate,
                        settingId: tagName,
                        settingValue: pasteValueStr);
                }
            }
        }
        else if (initiatorName == "FrmMainApp")
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (frmMainAppInstance != null)
            {
                foreach (ListViewItem lvi in frmMainAppInstance.lvw_FileList.SelectedItems)
                {
                    string fileNameWithPath = Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text);
                    string fileNameWithOutPath = lvi.Text;

                    // paste from copy-pool (no filename needed)
                    EnumerableRowCollection<DataRow> drFileCopyPoolRows = FrmMainApp.DtFileDataCopyPool.AsEnumerable()
                        .Where(r => r.Field<string>("settingId") == tagName);

                    foreach (DataRow dr in drFileCopyPoolRows)
                    {
                        string strToWrite;
                        if (dr[columnIndex: 1]
                                .ToString() ==
                            "-")
                        {
                            strToWrite = "";
                        }
                        else
                        {
                            strToWrite = dr[columnIndex: 1]
                                .ToString();
                        }

                        HelperStatic.GenericUpdateAddToDataTable(
                            dt: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite,
                            fileNameWithoutPath: fileNameWithOutPath,
                            settingId: dr[columnIndex: 0]
                                .ToString(),
                            settingValue: strToWrite
                        );
                    }
                }
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private void btn_Cancel_Click(object sender,
                                  EventArgs e)
    {
        Hide();
    }

    #region GPSData

    private void ckb_GPSLatitude_CheckedChanged(object sender,
                                                EventArgs e)
    { }

    private void ckb_GPSLongitude_CheckedChanged(object sender,
                                                 EventArgs e)
    { }

    private void ckb_GPSAltitude_CheckedChanged(object sender,
                                                EventArgs e)
    { }

    private void ckb_GPSDestLatitude_CheckedChanged(object sender,
                                                    EventArgs e)
    { }


    private void ckb_GPSDestLongitude_CheckedChanged(object sender,
                                                     EventArgs e)
    { }

    private void ckb_GPSImgDirection_CheckedChanged(object sender,
                                                    EventArgs e)
    { }

    private void ckb_GPSSpeed_CheckedChanged(object sender,
                                             EventArgs e)
    { }

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

    private void ckb_City_CheckedChanged(object sender,
                                         EventArgs e)
    { }

    private void ckb_Country_CheckedChanged(object sender,
                                            EventArgs e)
    { }

    private void ckb_State_CheckedChanged(object sender,
                                          EventArgs e)
    { }

    private void ckb_Sub_location_CheckedChanged(object sender,
                                                 EventArgs e)
    { }

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
}