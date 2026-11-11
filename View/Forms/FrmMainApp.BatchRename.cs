using GeoTagNinja.Helpers.UI;
using System.Windows.Forms;

namespace GeoTagNinja.View.Forms;

/// <summary>Batch Rename's entries in the File menu and the file list's right-click menu.</summary>
public partial class FrmMainApp
{
    /// <summary>
    ///     Adds <c>Batch Rename...</c> (Ctrl+Shift+R, as in Adobe Bridge) under <c>Edit Files...</c>, and to the file list's
    ///     right-click menu. Created in code so the designer files stay untouched; called once while the form is set up.
    /// </summary>
    private void AddBatchRenameMenus()
    {
        ToolStripMenuItem fileItem = NewMenuItem(controlName: "tmi_File_BatchRename");
        fileItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.R;
        fileItem.Click += (_,
                           _) => lvw_FileList.ShowBatchRenameDialog();
        int at = tmi_File.DropDownItems.IndexOf(value: tmi_File_EditFiles);
        tmi_File.DropDownItems.Insert(index: at < 0
                ? tmi_File.DropDownItems.Count
                : at + 1,
            value: fileItem);

        ToolStripMenuItem contextItem = NewMenuItem(controlName: "cmi_BatchRename");
        contextItem.Click += (_,
                              _) => lvw_FileList.ShowBatchRenameDialog();
        int before = cms_FileListView.Items.IndexOf(value: tss_Cms_2);
        cms_FileListView.Items.Insert(index: before < 0
                ? cms_FileListView.Items.Count
                : before,
            value: contextItem);
    }
}
