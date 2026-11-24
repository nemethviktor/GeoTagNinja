using GeoTagNinja.Helpers;
using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.FileSystem;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.View.Controls;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     The folder tree at the left of the main window (hide-able, View menu, Ctrl+B) and the favourite folders in its star
///     menu. Built in code, like the other recent additions, so the designer files stay untouched. What the tree lists and
///     how favourites behave is in <see cref="FolderTreeSource" /> and <see cref="FolderBookmarks" /> (shared with the
///     Avalonia version); <see cref="FolderTreePanel" /> is the WinForms view; this file is the wiring: where it sits, how
///     it changes folder, and what is stored.
/// </summary>
public partial class FrmMainApp
{
    private const string FolderTreeSettingsTab = "generic";
    private const string FolderTreeVisibleSettingId = "folderTreeVisible";
    private const string FolderTreeWidthSettingId = "folderTreeWidth";
    private const string FolderBookmarksSettingId = "folderBookmarks";
    private const int DefaultFolderTreeWidth = 260;

    private SplitContainer _folderTreeSplit;
    private FolderTreePanel _folderTreePanel;
    private ToolStripMenuItem tmi_View_FolderTree;
    private System.Windows.Forms.Timer _folderTreeSyncTimer;
    private bool _applyingFolderTreeLayout;

    /// <summary>
    ///     Puts the tree to the left of everything (the existing main split container moves into the right-hand panel
    ///     unchanged), adds the View menu item and restores the stored state. Called once from
    ///     <see cref="SetUpThumbnailView" />, after the View menu exists.
    /// </summary>
    private void AddFolderTree()
    {
        _folderTreePanel = new FolderTreePanel
        {
            Dock = DockStyle.Fill,
            Captions = BuildFolderTreeCaptions(),
            Bookmarks = FolderBookmarks.Parse(text: ReadFolderTreeSetting(settingId: FolderBookmarksSettingId))
        };
        _folderTreePanel.FolderChosen += async (_,
                                                path) => await NavigateToFolderFromTree(path: path);
        _folderTreePanel.BookmarksChanged += (_,
                                              _) => MenuSettings.Write(settingId: FolderBookmarksSettingId,
            value: _folderTreePanel.Bookmarks.Serialise(), settingTabPage: FolderTreeSettingsTab);
        _folderTreePanel.HideRequested += (_,
                                           _) => SetFolderTreeVisible(visible: false);

        // Wrap the existing main split container: the tree goes in a new left panel, the old layout in the right one.
        // The wrapper takes the old container's place in the z-order, which is what makes docking resolve as before.
        _folderTreeSplit = new SplitContainer
        {
            Name = "splitContainerFolderTree",
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            Orientation = Orientation.Vertical,
            SplitterWidth = splitContainerMain.SplitterWidth
        };
        int zIndex = Controls.GetChildIndex(child: splitContainerMain);
        SuspendLayout();
        Controls.Remove(value: splitContainerMain);
        _folderTreeSplit.Panel2.Controls.Add(value: splitContainerMain);
        _folderTreeSplit.Panel1.Controls.Add(value: _folderTreePanel);
        Controls.Add(value: _folderTreeSplit);
        Controls.SetChildIndex(child: _folderTreeSplit, newIndex: zIndex);
        ResumeLayout(performLayout: true);
        ThemeHelper.ApplyTo(control: _folderTreePanel);

        tmi_View_FolderTree = NewMenuItem(controlName: "tmi_View_FolderTree");
        tmi_View_FolderTree.ShortcutKeys = Keys.Control | Keys.B;
        tmi_View_FolderTree.Click += (_,
                                      _) => SetFolderTreeVisible(visible: _folderTreeSplit.Panel1Collapsed);
        tmi_View.DropDownItems.Insert(index: 0, value: tmi_View_FolderTree);
        tmi_View.DropDownItems.Insert(index: 1, value: new ToolStripSeparator { Name = "tss_View_0" });
        ThemeHelper.ApplyTo(control: mns_MenuStrip);

        // Restore the stored state now: this runs from inside the form's Load handler (see SetUpThumbnailView), so
        // subscribing to Load here would never fire. FixedPanel keeps the tree's width when the window is resized later.
        // Shown by default (it is how GeoSetter, which people come from, opens), then whatever the user last chose.
        _applyingFolderTreeLayout = true;
        try
        {
            int width = int.TryParse(s: ReadFolderTreeSetting(settingId: FolderTreeWidthSettingId),
                style: NumberStyles.Integer, provider: CultureInfo.InvariantCulture, result: out int stored)
                ? stored
                : DefaultFolderTreeWidth;
            _folderTreeSplit.SplitterDistance = Math.Max(val1: 120,
                val2: Math.Min(val1: width, val2: Math.Max(val1: 120, val2: _folderTreeSplit.Width / 2)));
        }
        catch (ArgumentException)
        {
            // the window was too narrow for the stored width; the default split stands
        }
        finally
        {
            _applyingFolderTreeLayout = false;
        }

        SetFolderTreeVisible(visible: ReadFolderTreeSetting(settingId: FolderTreeVisibleSettingId) != "false",
            persist: false);
        _folderTreeSplit.SplitterMoved += (_,
                                           _) =>
        {
            if (!_applyingFolderTreeLayout && !_folderTreeSplit.Panel1Collapsed)
            {
                MenuSettings.WriteInt(settingId: FolderTreeWidthSettingId, value: _folderTreeSplit.SplitterDistance,
                    settingTabPage: FolderTreeSettingsTab);
            }
        };

        // keep the tree on whatever folder the list is showing (typed, Backspace, up button, start-up), after a short pause
        // so typing a path does not walk the tree on every keystroke
        _folderTreeSyncTimer = new System.Windows.Forms.Timer { Interval = 400 };
        _folderTreeSyncTimer.Tick += (_,
                                      _) =>
        {
            _folderTreeSyncTimer.Stop();
            SyncFolderTreeToFolderBox();
        };
        tbx_FolderName.TextChanged += (_,
                                       _) =>
        {
            _folderTreeSyncTimer.Stop();
            _folderTreeSyncTimer.Start();
        };
    }

    /// <summary>
    ///     The folder tree's captions in the current app language (also used to re-translate it after a language change).
    /// </summary>
    private static FolderTreePanel.FolderTreeCaptions BuildFolderTreeCaptions() =>
        new()
        {
            Title = FolderTreeText(controlName: "lbl_FolderTree_Title"),
            Favourites = FolderTreeText(controlName: "tsb_FolderTree_Favourites"),
            AddCurrent = FolderTreeText(controlName: "tmi_FolderTree_AddCurrent"),
            RemoveCurrent = FolderTreeText(controlName: "tmi_FolderTree_RemoveCurrent"),
            NoFavourites = FolderTreeText(controlName: "tmi_FolderTree_NoFavourites"),
            Refresh = FolderTreeText(controlName: "tsb_FolderTree_Refresh"),
            Hide = FolderTreeText(controlName: "tsb_FolderTree_Hide"),
            AddThis = FolderTreeText(controlName: "cmi_FolderTree_AddThis"),
            RemoveThis = FolderTreeText(controlName: "cmi_FolderTree_RemoveThis")
        };

    private static string FolderTreeText(string controlName) =>
        HelperControlAndMessageBoxHandling.ReturnControlText(
            controlName: controlName,
            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.ToolStripMenuItem);

    private static string ReadFolderTreeSetting(string settingId) =>
        ApplicationSettings.DataReadSQLiteSettings(
            dataTable: HelperVariables.DtHelperDataApplicationSettings,
            settingTabPage: FolderTreeSettingsTab,
            settingId: settingId) ?? string.Empty;

    private void SetFolderTreeVisible(bool visible,
                                      bool persist = true)
    {
        _folderTreeSplit.Panel1Collapsed = !visible;
        SetMenuMark(item: tmi_View_FolderTree, on: visible);
        if (visible)
        {
            SyncFolderTreeToFolderBox();
        }

        if (persist)
        {
            MenuSettings.WriteBool(settingId: FolderTreeVisibleSettingId, value: visible,
                settingTabPage: FolderTreeSettingsTab);
        }
    }

    private void SyncFolderTreeToFolderBox()
    {
        if (_folderTreeSplit == null || _folderTreeSplit.Panel1Collapsed || tbx_FolderName.Text.Length == 0)
        {
            return;
        }

        string path = tbx_FolderName.Text;
        if (Directory.Exists(path: path))
        {
            _folderTreePanel.SelectFolder(path: path);
        }
    }

    /// <summary>
    ///     A folder picked in the tree or the favourites menu. The same guard as typing a path and pressing Enter: if photos
    ///     have unsaved edits the user is asked first, and if they cancel we stay where we were (and the tree goes back to
    ///     the folder the list is still showing).
    /// </summary>
    private async System.Threading.Tasks.Task NavigateToFolderFromTree(string path)
    {
        if (!Directory.Exists(path: path))
        {
            _folderTreePanel.Reload(); // the folder is gone (renamed, drive removed): show what is there now
            return;
        }

        if (string.Equals(a: FolderBookmarks.Normalise(path: path),
                b: FolderBookmarks.Normalise(path: tbx_FolderName.Text), comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return; // already there; a refresh is what F5 is for
        }

        HelperVariables.OperationChangeFolderIsOkay = false;
        await Operators.FsoCheckOutstandingFileDataOkayToChangeFolderAsync(isTheAppClosing: false);
        if (!HelperVariables.OperationChangeFolderIsOkay)
        {
            SyncFolderTreeToFolderBox();
            return;
        }

        tbx_FolderName.Text = path;
        tsb_Refresh_lvwFileList_Click(sender: this, e: EventArgs.Empty);
    }
}
