using System;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using GeoTagNinja.View.DialogAndMessageBoxes;
using static GeoTagNinja.View.ListView.FileListView;

namespace GeoTagNinja.View.EditFileForm;

internal static class EditFileFormGeneric
{
    /// <summary>
    ///     Loads up the Edit (file exif data) Form.
    /// </summary>
    internal static void ShowFrmEditFileData()
    {
        int overallCount = 0;
        int fileCount = 0;
        int folderCount = 0;
        FrmEditFileData FrmEditFileData = new();

        System.Windows.Forms.ListView lvw = FrmEditFileData.lvw_FileListEditImages;
        lvw.Columns.Clear();
        lvw.Items.Clear();

        ColumnHeader clh_fileName = new();
        clh_fileName.Name = COL_NAME_PREFIX + FileListColumns.FILENAME;
        clh_fileName.Width = lvw.Width;
        lvw.Columns.Add(value: clh_fileName);

        //ColumnHeader clh_GUID = new();
        //clh_GUID.Name = COL_NAME_PREFIX + FileListColumns.GUID;
        //clh_GUID.Width = 0;
        //lvw.Columns.Add(value: clh_GUID);

        foreach (string fileToEditGUID in FrmMainApp.filesToEditGUIDStringList)
        {
            DirectoryElement dirElemFileToModify =
                FrmMainApp.DirectoryElements.FindElementByItemGUID(GUID: fileToEditGUID);

            if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
            {
                overallCount++;
                ListViewItem lvi = new()
                {
                    Text = dirElemFileToModify.ItemNameWithoutPath,
                    Tag = dirElemFileToModify
                };
                //lvi.SubItems.Add(text: fileToEditGUID);
                FrmEditFileData.lvw_FileListEditImages.Items.Add(value: lvi);
                fileCount++;
            }
            else if (dirElemFileToModify.Type ==
                     DirectoryElement.ElementType.SubDirectory)
            {
                overallCount++;
                folderCount++;
            }
        }

        if (fileCount > 0)
        {
            FrmEditFileData.StartPosition = FormStartPosition.CenterScreen;
            try
            {
                FrmEditFileData.ShowDialog();
            }
            catch (AccessViolationException)
            {
                // Ignore the exception
            }
        }
        // basically if the user only selected folders, do nothing
        else if (overallCount == folderCount + fileCount)
        {
            //nothing.
        }
        // we appear to have lost a file or two.
        else
        {
            CustomMessageBox customMessageBox = new(
                text: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_Helper_WarningFileDisappeared",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                caption: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: HelperControlAndMessageBoxHandling.MessageBoxCaption
                                                                   .Warning.ToString(),
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBoxCaption),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
            customMessageBox.ShowDialog();
        }
    }
}