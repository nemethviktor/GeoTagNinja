using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace GeoTagNinja;

public partial class FrmPasteWhat : Form
{
    /// <summary>
    ///     This form controls what data to paste from a "current file" to "selected files"
    ///     I'm not adding further commentary because most of it is pretty self explanatory
    /// </summary>
    public FrmPasteWhat()
    {
        InitializeComponent();

        // pre-load "All"
        btn_GPSData_All_Click(sender: null, e: null);
        btn_LocationData_All_Click(sender: null, e: null);
    }

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

    private void btn_OK_Click(object sender,
                              EventArgs e)
    {
        FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
        if (frmEditFileDataInstance != null)
        {
            ListView lvw = frmEditFileDataInstance.lvw_FileListEditImages;

            // create a list of tags that also have a Ref version
            List<string> tagsWithRefList = new()
            {
                "GPSAltitude", "GPSDestLatitude", "GPSDestLongitude", "GPSImgDirection", "GPSLatitude", "GPSLongitude", "GPSSpeed"
            };

            // stuff has to go back into Q1.
            foreach (ListViewItem lvi in lvw.Items)
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
                            UpdateFileDataPaste(tagName: tagName, fileToUpdate: lvi.Text);

                            // also do all the Refs 
                            if (tagsWithRefList.Contains(item: tagName))
                            {
                                UpdateFileDataPaste(tagName: tagName + "Ref", fileToUpdate: lvi.Text);
                            }

                            // also do all the CountryCode 
                            if (tagName == "Country")
                            {
                                UpdateFileDataPaste(tagName: "CountryCode", fileToUpdate: lvi.Text);
                            }
                        }
                    }

                    Hide();
                }
            }
        }
    }

    private static void UpdateFileDataPaste(string tagName,
                                            string fileToUpdate
    )
    {
        FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
        if (frmEditFileDataInstance != null)
        {
            ListView lvw = frmEditFileDataInstance.lvw_FileListEditImages;

            string fileToPullFrom = lvw.SelectedItems[index: 0]
                .Text;
            string pasteValueStr = "";

            // check it's sitting somewhere already?
            DataView dvSqlDataQ = new(table: FrmMainApp.DtFileDataToWriteStage1PreQueue);
            dvSqlDataQ.RowFilter = "filePath = '" + fileToPullFrom + "' AND settingId ='" + tagName + "'";

            DataView dvSqlDataRTW = new(table: FrmMainApp.DtFileDataToWriteStage3ReadyToWrite);
            dvSqlDataRTW.RowFilter = "filePath = '" + fileToPullFrom + "' AND settingId ='" + tagName + "'";

            DataView dvSqlDataInFile = new(table: FrmMainApp.DtFileDataSeenInThisSession);
            // this holds filepaths with a Directory attached to the string 
            dvSqlDataInFile.RowFilter = "filePath = '" + Path.Combine(path1: FrmMainApp.FolderName, path2: fileToPullFrom) + "' AND settingId ='" + tagName + "'";

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
                    filePath: fileToUpdate,
                    settingId: tagName,
                    settingValue: pasteValueStr);
            }
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