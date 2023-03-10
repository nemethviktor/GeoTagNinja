using System.IO;
using System.Windows.Forms;
using GeoTagNinja.Helpers;

namespace GeoTagNinja;

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
        FrmEditFileData.lvw_FileListEditImages.Items.Clear();
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        foreach (ListViewItem selectedItem in frmMainAppInstance.lvw_FileList.SelectedItems)
        {
            // only deal with listOfAsyncCompatibleFileNamesWithOutPath, not folders
            if (File.Exists(path: Path.Combine(path1: frmMainAppInstance.tbx_FolderName.Text, path2: selectedItem.Text)))
            {
                overallCount++;
                FrmMainApp.FolderName = frmMainAppInstance.tbx_FolderName.Text;
                FrmEditFileData.lvw_FileListEditImages.Items.Add(text: selectedItem.Text);
                fileCount++;
            }
            else if (Directory.Exists(path: Path.Combine(path1: frmMainAppInstance.tbx_FolderName.Text, path2: selectedItem.Text)))
            {
                overallCount++;
                folderCount++;
            }
        }

        if (fileCount > 0)
        {
            FrmEditFileData.ShowDialog();
        }
        // basically if the user only selected folders, do nothing
        else if (overallCount == folderCount + fileCount)
        {
            //nothing.
        }
        // we appear to have lost a file or two.
        else
        {
            MessageBox.Show(text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningFileDisappeared"),
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }
    }
}