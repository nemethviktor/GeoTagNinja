using GeoTagNinja.Helpers.FileSystem;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GeoTagNinja.View.Controls;

/// <summary>
///     The folder tree shown at the left of the main window: drives and folders, expanded lazily, plus a star menu of
///     favourite folders. It only displays and reports clicks; changing folder, and storing the favourites, are the host's
///     job (<see cref="FolderChosen" />, <see cref="BookmarksChanged" />). What the tree lists and how bookmarks behave
///     lives in <see cref="FolderTreeSource" /> and <see cref="FolderBookmarks" />, which the Avalonia version reuses.
/// </summary>
/// <remarks>
///     Designer safety: nothing here needs app state; every caption is passed in by the host through
///     <see cref="Captions" />, so the control builds fine when a designer instantiates it.
/// </remarks>
internal sealed class FolderTreePanel : UserControl
{
    /// <summary>The captions the host looks up (localised) and hands over.</summary>
    internal sealed class FolderTreeCaptions
    {
        internal string Title { get; set; } = "Folders";
        internal string Favourites { get; set; } = "Favourite folders";
        internal string AddCurrent { get; set; } = "Add current folder to favourites";
        internal string RemoveCurrent { get; set; } = "Remove current folder from favourites";
        internal string NoFavourites { get; set; } = "(no favourite folders yet)";
        internal string Refresh { get; set; } = "Refresh";
        internal string Hide { get; set; } = "Hide the folder tree";
        internal string AddThis { get; set; } = "Add to favourites";
        internal string RemoveThis { get; set; } = "Remove from favourites";
    }

    /// <summary>Marks the placeholder child that makes a not-yet-loaded node expandable.</summary>
    private const string PlaceholderTag = "placeholder";

    private readonly ToolStrip _toolStrip = new();
    private readonly ToolStripLabel _titleLabel = new();
    private readonly ToolStripDropDownButton _favouritesButton = new();
    private readonly ToolStripButton _refreshButton = new();
    private readonly ToolStripButton _hideButton = new();
    private readonly TreeView _tree = new();
    private readonly ContextMenuStrip _nodeMenu = new();
    private readonly ImageList _images = new();
    private readonly Timer _keyboardDebounce = new() { Interval = 350 };

    private FolderTreeCaptions _captions = new();
    private FolderBookmarks _bookmarks = new();
    private string _currentFolder = string.Empty;
    private string _pendingKeyboardPath = string.Empty;
    private bool _selectingFromCode;

    /// <summary>Raised when the user picks a folder (a click, Enter, a favourite). The host decides whether to go there.</summary>
    internal event EventHandler<string>? FolderChosen;

    /// <summary>Raised when the user adds or removes a favourite; the host stores <see cref="Bookmarks" />.</summary>
    internal event EventHandler? BookmarksChanged;

    /// <summary>Raised when the user clicks the panel's hide button.</summary>
    internal event EventHandler? HideRequested;

    internal FolderTreePanel()
    {
        SuspendLayout();

        _titleLabel.Font = new Font(prototype: Font, newStyle: FontStyle.Bold);
        _favouritesButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _favouritesButton.Text = "\u2605"; // black star
        _favouritesButton.ShowDropDownArrow = true;
        _favouritesButton.DropDownOpening += (_,
                                              _) => BuildFavouritesMenu();
        _refreshButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _refreshButton.Text = "\u21BB"; // clockwise open circle arrow
        _refreshButton.Click += (_,
                                 _) => Reload();
        _hideButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _hideButton.Text = "\u2715"; // multiplication x
        _hideButton.Alignment = ToolStripItemAlignment.Right;
        _hideButton.Click += (_,
                              _) => HideRequested?.Invoke(sender: this, e: EventArgs.Empty);

        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.Dock = DockStyle.Top;
        _ = _toolStrip.Items.Add(value: _titleLabel);
        _ = _toolStrip.Items.Add(value: _favouritesButton);
        _ = _toolStrip.Items.Add(value: _refreshButton);
        _ = _toolStrip.Items.Add(value: _hideButton);

        _tree.Dock = DockStyle.Fill;
        _tree.HideSelection = false;
        _tree.ShowRootLines = true;
        _tree.PathSeparator = "\\";
        _tree.ImageList = _images;
        _tree.BeforeExpand += Tree_BeforeExpand;
        _tree.AfterSelect += Tree_AfterSelect;
        _tree.KeyDown += Tree_KeyDown;
        _tree.NodeMouseClick += Tree_NodeMouseClick;
        _tree.ContextMenuStrip = _nodeMenu;
        _nodeMenu.Opening += NodeMenu_Opening;

        _keyboardDebounce.Tick += (_,
                                   _) =>
        {
            _keyboardDebounce.Stop();
            if (_pendingKeyboardPath.Length > 0)
            {
                FolderChosen?.Invoke(sender: this, e: _pendingKeyboardPath);
            }
        };

        // the tree fills what the tool strip leaves; added in this order so docking resolves correctly
        Controls.Add(value: _tree);
        Controls.Add(value: _toolStrip);
        ResumeLayout(performLayout: false);

        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
        {
            LoadIcons();
            Reload();
        }

        ApplyCaptions();
    }

    /// <summary>The favourite folders. Set by the host at start-up; read back after <see cref="BookmarksChanged" />.</summary>
    internal FolderBookmarks Bookmarks
    {
        get => _bookmarks;
        set => _bookmarks = value ?? new FolderBookmarks();
    }

    /// <summary>The (localised) captions; set once by the host.</summary>
    internal FolderTreeCaptions Captions
    {
        get => _captions;
        set
        {
            _captions = value ?? new FolderTreeCaptions();
            ApplyCaptions();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keyboardDebounce.Dispose();
            _images.Dispose();
            _nodeMenu.Dispose();
        }

        base.Dispose(disposing: disposing);
    }

    private void ApplyCaptions()
    {
        _titleLabel.Text = _captions.Title;
        _favouritesButton.ToolTipText = _captions.Favourites;
        _refreshButton.ToolTipText = _captions.Refresh;
        _hideButton.ToolTipText = _captions.Hide;
    }

    private void LoadIcons()
    {
        int size = (int)Math.Round(a: 16 * DeviceDpi / 96.0);
        _images.ImageSize = new Size(width: size, height: size);
        _images.ColorDepth = ColorDepth.Depth32Bit;
        AddIcon(key: "folder", element: new DirectoryElement(itemNameWithoutPath: "x",
            type: DirectoryElement.ElementType.SubDirectory, fileNameWithPath: "x"));
        AddIcon(key: "drive", element: new DirectoryElement(itemNameWithoutPath: "C:\\",
            type: DirectoryElement.ElementType.Drive, fileNameWithPath: "C:\\"));
    }

    private void AddIcon(string key,
                         DirectoryElement element)
    {
        if (FolderIcons.For(directoryElement: element) is { } image)
        {
            _images.Images.Add(key: key, image: image);
        }
    }

    /// <summary>Rebuilds the tree from the drives, keeping the current folder selected.</summary>
    internal void Reload()
    {
        _tree.BeginUpdate();
        try
        {
            _tree.Nodes.Clear();
            foreach (FolderTreeSource.FolderEntry drive in FolderTreeSource.GetRoots())
            {
                _ = _tree.Nodes.Add(node: NewNode(entry: drive, imageKey: "drive"));
            }
        }
        finally
        {
            _tree.EndUpdate();
        }

        if (_currentFolder.Length > 0)
        {
            SelectFolder(path: _currentFolder);
        }
    }

    /// <summary>
    ///     Expands down to a folder and selects it, without raising <see cref="FolderChosen" /> (the host is the one
    ///     telling us where it is). Does nothing for a path the tree cannot show (a network share, a missing folder).
    /// </summary>
    internal void SelectFolder(string path)
    {
        _currentFolder = FolderBookmarks.Normalise(path: path);
        IReadOnlyList<string> chain = FolderTreeSource.AncestorChain(path: _currentFolder);
        if (chain.Count == 0)
        {
            return;
        }

        _selectingFromCode = true;
        try
        {
            TreeNodeCollection level = _tree.Nodes;
            TreeNode? found = null;
            foreach (string step in chain)
            {
                found = FindNode(nodes: level, path: step);
                if (found == null)
                {
                    return;
                }

                EnsureChildrenLoaded(node: found);
                level = found.Nodes;
                if (!string.Equals(a: step, b: chain[index: chain.Count - 1], comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    found.Expand();
                }
            }

            if (found != null && !ReferenceEquals(objA: _tree.SelectedNode, objB: found))
            {
                _tree.SelectedNode = found;
                found.EnsureVisible();
            }
        }
        finally
        {
            _selectingFromCode = false;
        }
    }

    private static TreeNode? FindNode(TreeNodeCollection nodes,
                                      string path)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is string nodePath &&
                string.Equals(a: FolderBookmarks.Normalise(path: nodePath), b: path, comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }

        return null;
    }

    private static TreeNode NewNode(FolderTreeSource.FolderEntry entry,
                                    string imageKey)
    {
        TreeNode node = new(text: entry.Name)
        {
            Tag = entry.FullPath,
            ImageKey = imageKey,
            SelectedImageKey = imageKey
        };
        // every node is expandable until it has been looked at; an empty folder loses its "+" on first expand
        _ = node.Nodes.Add(node: new TreeNode { Tag = PlaceholderTag });
        return node;
    }

    private static bool IsUnloaded(TreeNode node) =>
        node.Nodes.Count == 1 && node.Nodes[index: 0].Tag is PlaceholderTag;

    private static void EnsureChildrenLoaded(TreeNode node)
    {
        if (!IsUnloaded(node: node) || node.Tag is not string path)
        {
            return;
        }

        node.Nodes.Clear();
        foreach (FolderTreeSource.FolderEntry child in FolderTreeSource.GetChildren(path: path))
        {
            _ = node.Nodes.Add(node: NewNode(entry: child, imageKey: "folder"));
        }
    }

    private void Tree_BeforeExpand(object? sender,
                                   TreeViewCancelEventArgs e)
    {
        if (e.Node != null)
        {
            EnsureChildrenLoaded(node: e.Node);
        }
    }

    private void Tree_AfterSelect(object? sender,
                                  TreeViewEventArgs e)
    {
        if (_selectingFromCode || e.Node?.Tag is not string path)
        {
            return;
        }

        _currentFolder = FolderBookmarks.Normalise(path: path);
        if (e.Action == TreeViewAction.ByMouse)
        {
            FolderChosen?.Invoke(sender: this, e: path);
        }
        else if (e.Action == TreeViewAction.ByKeyboard)
        {
            // arrowing down a long list should not load every folder it passes
            _pendingKeyboardPath = path;
            _keyboardDebounce.Stop();
            _keyboardDebounce.Start();
        }
    }

    /// <summary>A click on the already-selected node raises no AfterSelect, so it is handled here (e.g. to reload a folder).</summary>
    private void Tree_NodeMouseClick(object? sender,
                                     TreeNodeMouseClickEventArgs e)
    {
        if (e.Button == MouseButtons.Left && ReferenceEquals(objA: _tree.SelectedNode, objB: e.Node) && e.Node.Tag is string path)
        {
            FolderChosen?.Invoke(sender: this, e: path);
        }
        else if (e.Button == MouseButtons.Right)
        {
            _tree.SelectedNode = e.Node; // so the context menu acts on the node under the pointer
        }
    }

    private void Tree_KeyDown(object? sender,
                              KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && _tree.SelectedNode?.Tag is string path)
        {
            _keyboardDebounce.Stop();
            FolderChosen?.Invoke(sender: this, e: path);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void BuildFavouritesMenu()
    {
        _favouritesButton.DropDownItems.Clear();
        foreach (string path in _bookmarks.Paths)
        {
            ToolStripMenuItem item = new(text: path.Replace(oldValue: "&", newValue: "&&")) { Tag = path };
            item.Click += (_,
                           _) => FolderChosen?.Invoke(sender: this, e: path);
            _ = _favouritesButton.DropDownItems.Add(value: item);
        }

        if (_bookmarks.Paths.Count == 0)
        {
            _ = _favouritesButton.DropDownItems.Add(value: new ToolStripMenuItem(text: _captions.NoFavourites) { Enabled = false });
        }

        _ = _favouritesButton.DropDownItems.Add(value: new ToolStripSeparator());
        bool haveCurrent = _currentFolder.Length > 0;
        bool isBookmarked = haveCurrent && _bookmarks.Contains(path: _currentFolder);
        ToolStripMenuItem toggle = new(text: isBookmarked ? _captions.RemoveCurrent : _captions.AddCurrent)
        {
            Enabled = haveCurrent
        };
        toggle.Click += (_,
                         _) => ToggleBookmark(path: _currentFolder);
        _ = _favouritesButton.DropDownItems.Add(value: toggle);

        Themed(menu: _favouritesButton.DropDown);
    }

    private void NodeMenu_Opening(object? sender,
                                  CancelEventArgs e)
    {
        _nodeMenu.Items.Clear();
        if (_tree.SelectedNode?.Tag is not string path)
        {
            e.Cancel = true;
            return;
        }

        ToolStripMenuItem toggle = new(text: _bookmarks.Contains(path: path) ? _captions.RemoveThis : _captions.AddThis);
        toggle.Click += (_,
                         _) => ToggleBookmark(path: path);
        _ = _nodeMenu.Items.Add(value: toggle);
        Themed(menu: _nodeMenu);
    }

    private void ToggleBookmark(string path)
    {
        if (!_bookmarks.Remove(path: path))
        {
            _ = _bookmarks.Add(path: path);
        }

        BookmarksChanged?.Invoke(sender: this, e: EventArgs.Empty);
    }

    /// <summary>Menus built after start-up miss the theme pass, so each is put through it when it is filled.</summary>
    private static void Themed(ToolStripDropDown menu)
    {
        if (menu.Items.Count > 0)
        {
            ThemeHelper.ApplyTo(control: menu);
        }
    }

    /// <summary>The folder the tree is currently pointing at (as last selected or reported by the host).</summary>
    internal string CurrentFolder => _currentFolder;
}
