using GeoTagNinja.Helpers;
using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.View.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     The View menu: switching between the details list and the thumbnail view, and the thumbnail size. Kept apart from
///     the rest of the form so menus can grow here; see the building blocks below for adding to it.
/// </summary>
public partial class FrmMainApp
{
    #region Building blocks (reuse these when moving more settings out of the Settings dialog into menus)

    /// <summary>
    ///     A menu item named like a control, with its caption looked up (and localised) from <c>Strings.resx</c> under that
    ///     name. It carries no colours of its own: the main menu is themed by <see cref="ThemeHelper.ApplyTo" /> at
    ///     start-up, so a menu built afterwards must be put through it again (see <see cref="BuildViewMenu" />) to look like
    ///     the others. Colouring items here by hand gave the View menu a different dark from every other menu.
    /// </summary>
    /// <param name="controlName">The item's name and its key in <c>Strings.resx</c> (e.g. <c>tmi_View_Details</c>).</param>
    private static ToolStripMenuItem NewMenuItem(string controlName)
    {
        return new ToolStripMenuItem
        {
            Name = controlName,
            Text = HelperControlAndMessageBoxHandling.ReturnControlText(
                controlName: controlName,
                fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.ToolStripMenuItem)
        };
    }

    /// <summary>The tick drawn next to the active item (see <see cref="SetMenuMark" />). Created on first use.</summary>
    private static Bitmap _menuTick;

    /// <summary>
    ///     Shows or hides the "this one is on / selected" mark on a menu item, as a small tick in the item's margin, in both
    ///     themes. The stock check mark is not used: in dark mode the renderer paints its box an out-of-place light blue (the
    ///     other menus never show one, so nothing had styled it), and in light mode it draws an unscaled glyph in the top
    ///     corner of an oversized box on high-DPI displays.
    /// </summary>
    /// <remarks>
    ///     The tick is deliberately small and is not scaled: an image bigger than the text makes every row of that drop-down
    ///     taller than the rows of the other menus (a 16 px image is stretched to twice that on a 200 % display, and the
    ///     rows grew from about 33 px to about 50 px). Its size follows the menu font, so it fits at any display scaling.
    ///     (The theme can only change with a restart, so one tick, in the colour of the current theme, is enough.)
    /// </remarks>
    private static void SetMenuMark(ToolStripMenuItem item,
                                    bool on)
    {
        if (_menuTick == null)
        {
            int size = Math.Max(val1: 10, val2: (int)Math.Round(a: SystemFonts.MenuFont.Height * 0.7));
            Bitmap tick = new(width: size, height: size);
            using (Graphics graphics = Graphics.FromImage(image: tick))
            using (Pen pen = new(color: ThemeHelper.IsDark ? ThemeHelper.ForeDark : SystemColors.MenuText, width: Math.Max(val1: 1.5f, val2: size / 8f)))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.DrawLines(pen: pen, points:
                [
                    new PointF(x: size * 0.19f, y: size * 0.50f),
                    new PointF(x: size * 0.38f, y: size * 0.69f),
                    new PointF(x: size * 0.78f, y: size * 0.25f)
                ]);
            }

            _menuTick = tick;
        }

        item.Checked = false;
        item.ImageScaling = ToolStripItemImageScaling.None;
        item.Image = on ? _menuTick : null;
    }

    /// <summary>
    ///     Adds a checkable menu item that is a yes/no setting (a <c>ckb_…</c> id): the tick shows the current value, and
    ///     clicking flips it, stores it (the same row the Settings dialog's check box used, so nothing needs migrating) and
    ///     calls <paramref name="apply" />. Not used yet; it is here so the next check box that moves out of the Settings
    ///     dialog is one call. (Like the rest of the View menu it is themed by the <c>ThemeHelper.ApplyTo</c> call in
    ///     <see cref="BuildViewMenu" />, so add it before that, or call it again afterwards.)
    /// </summary>
    /// <param name="parent">The menu to add it to.</param>
    /// <param name="menuControlName">Name and <c>Strings.resx</c> key of the menu item.</param>
    /// <param name="settingId">The setting's id, e.g. <c>ckb_AlwaysRecenterMap</c>.</param>
    /// <param name="isOn">Reads the current value (normally a <see cref="HelperVariables" /> field).</param>
    /// <param name="apply">Makes the new value take effect (set the field, refresh whatever shows it).</param>
    /// <param name="settingTabPage">The tab page the setting was stored under.</param>
    private static ToolStripMenuItem AddSettingToggle(ToolStripMenuItem parent,
                                                      string menuControlName,
                                                      string settingId,
                                                      Func<bool> isOn,
                                                      Action<bool> apply,
                                                      string settingTabPage = MenuSettings.ApplicationTab)
    {
        ToolStripMenuItem item = NewMenuItem(controlName: menuControlName);
        bool current = isOn();
        SetMenuMark(item: item, on: current);
        item.Click += (_,
                       _) =>
        {
            current = !current;
            SetMenuMark(item: item, on: current);
            MenuSettings.WriteBool(settingId: settingId, value: current, settingTabPage: settingTabPage);
            apply(obj: current);
        };
        _ = parent.DropDownItems.Add(value: item);
        return item;
    }

    #endregion

    #region View modes (details list / thumbnails)

    /// <summary>The thumbnail view. Created in code (not the designer); kept in step with <see cref="lvw_FileList" />.</summary>
    private ThumbnailView thumbnailView;

    /// <summary>
    ///     The thumbnail size slider. It sits at the right-hand end of the status row under the file list (where Explorer and
    ///     most image browsers put their zoom control), not on the toolbar, which is far from the thumbnails it affects.
    /// </summary>
    private TrackBar _thumbnailSizeSlider;
    private ToolStripMenuItem tmi_View;
    private ToolStripMenuItem tmi_View_Details;
    private ToolStripMenuItem tmi_View_Thumbnails;
    private ToolStripMenuItem tmi_View_SortBy;
    private readonly List<ToolStripMenuItem> _thumbnailSizeMenuItems = [];
    private System.Windows.Forms.Timer _thumbnailSizePersistTimer;
    private bool _syncingThumbnailSize;

    /// <summary>
    ///     True once the user has changed the size in this session and it has not been stored yet. The stored size
    ///     (<c>nud_ThumbnailSize</c>) is written only then: it is never seeded as a default and never rewritten while it
    ///     hasn't changed, so a user who has not chosen a size (or who is in Details mode) simply gets the default and the
    ///     app works the same; and a size chosen earlier is kept, whatever mode the app is in.
    /// </summary>
    private bool _thumbnailSizeChangedByUser;

    /// <summary>True while the size is being set from stored settings rather than by the user (so it is not saved back).</summary>
    private bool _applyingStoredThumbnailSize;

    /// <summary>True once every file of the folder being loaded has been found (see the prioritisation timer).</summary>
    private bool _discoveryComplete;

    /// <summary>The thumbnail size used when none has been stored yet.</summary>
    private const int DefaultThumbnailSize = 128;

    /// <summary>The settings ids the view mode and size are stored under (unchanged from when they lived in the Settings dialog).</summary>
    private const string ShowThumbnailsSettingId = "ckb_ShowThumbnails";

    private const string ThumbnailSizeSettingId = "nud_ThumbnailSize";

    /// <summary>
    ///     Creates the thumbnail view beside the details list, builds the View menu and the size slider, and applies the
    ///     stored mode and size. Called once from <see cref="SetUpLvwFileList" />.
    /// </summary>
    private void SetUpThumbnailView()
    {
        int storedSize = HelperVariables.UserSettingThumbnailSize > 0
            ? HelperVariables.UserSettingThumbnailSize
            : DefaultThumbnailSize;

        thumbnailView = new ThumbnailView(source: lvw_FileList)
        {
            Name = "tvw_Thumbnails",
            Dock = DockStyle.Fill,
            Visible = false,
            ThumbnailSize = storedSize
        };
        splitContainerLeftTop.Panel1.Controls.Add(value: thumbnailView);

        thumbnailView.ItemActivated += async item => await OpenListItem(item: item, sender: thumbnailView);

        // the preview and the map follow the selection once the user has finished changing it
        thumbnailView.UserSelectionChanged += async (_,
                                                     _) => await lvw_HandleSelectionChange();

        // everything else the details list answers to (Ctrl+A, Enter, Backspace, F5, Ctrl+S, Delete, ...)
        thumbnailView.KeyDown += (sender,
                                  e) =>
        {
            if (!lvw_FileList.TryHandleExternalKey(e: e))
            {
                lvw_FileList_KeyDown(sender: sender, e: e);
            }
        };

        // right-click, the menu key and Shift+F10: the same actions menu the details list ends its right-click menu with
        thumbnailView.ContextMenuRequested += location => lvw_FileList.ShowActionsMenu(owner: thumbnailView, location: location);

        thumbnailView.ThumbnailSizeChanged += (_,
                                               _) => OnThumbnailSizeChanged();

        // The size is saved a moment after the last change rather than on every wheel notch (each save refreshes the
        // settings tables).
        _thumbnailSizePersistTimer = new System.Windows.Forms.Timer { Interval = 800 };
        _thumbnailSizePersistTimer.Tick += (_,
                                            _) =>
        {
            _thumbnailSizePersistTimer.Stop();
            PersistThumbnailSize();
        };

        ThemeHelper.ApplyTo(control: thumbnailView);
        BuildViewMenu();
        ApplyViewMode(mode: HelperVariables.UserSettingShowThumbnails
            ? ListViewDisplayMode.LargeIcons
            : ListViewDisplayMode.Details, persist: false);

        // When the app starts in thumbnail mode the form isn't visible yet, so the focus the view takes on becoming visible
        // doesn't stick and the keyboard would act on the menu bar until the user clicked. Take it once the window shows.
        void FocusThumbnailsOnceShown(object sender,
                                      EventArgs e)
        {
            Shown -= FocusThumbnailsOnceShown;
            if (thumbnailView.Visible)
            {
                _ = thumbnailView.Focus();
            }
        }

        Shown += FocusThumbnailsOnceShown;
    }

    /// <summary>Adds the View menu (between Edit and Settings) and the size slider in the status row.</summary>
    private void BuildViewMenu()
    {
        tmi_View = NewMenuItem(controlName: "tmi_View");
        tmi_View_Details = NewMenuItem(controlName: "tmi_View_Details");
        tmi_View_Thumbnails = NewMenuItem(controlName: "tmi_View_Thumbnails");
        tmi_View_Details.Click += (_,
                                   _) => ApplyViewMode(mode: ListViewDisplayMode.Details, persist: true);
        tmi_View_Thumbnails.Click += (_,
                                      _) => ApplyViewMode(mode: ListViewDisplayMode.LargeIcons, persist: true);

        ToolStripMenuItem sizeMenu = NewMenuItem(controlName: "tmi_View_ThumbnailSize");
        foreach ((string name, int size) in new[]
                 {
                     ("tmi_View_ThumbnailSize_Small", 96),
                     ("tmi_View_ThumbnailSize_Medium", 128),
                     ("tmi_View_ThumbnailSize_Large", 192),
                     ("tmi_View_ThumbnailSize_ExtraLarge", 256)
                 })
        {
            ToolStripMenuItem preset = NewMenuItem(controlName: name);
            preset.Tag = size;
            preset.Click += (sender,
                             _) => thumbnailView.ThumbnailSize = (int)((ToolStripMenuItem)sender).Tag;
            _thumbnailSizeMenuItems.Add(item: preset);
            _ = sizeMenu.DropDownItems.Add(value: preset);
        }

        // Sort By: the thumbnail view has no column headers to click, so the columns are offered here (the details list's
        // headers still work, and both change the same sort)
        tmi_View_SortBy = NewMenuItem(controlName: "tmi_View_SortBy");
        tmi_View_SortBy.DropDownOpening += (_,
                                            _) => BuildSortMenu();
        BuildSortMenu();

        ToolStripMenuItem smaller = NewMenuItem(controlName: "tmi_View_ThumbnailSize_Smaller");
        smaller.ShortcutKeys = Keys.Control | Keys.OemMinus;
        smaller.ShortcutKeyDisplayString = "Ctrl+-";
        smaller.Click += (_,
                          _) => thumbnailView.ThumbnailSize -= 16;
        ToolStripMenuItem larger = NewMenuItem(controlName: "tmi_View_ThumbnailSize_Larger");
        larger.ShortcutKeys = Keys.Control | Keys.Oemplus;
        larger.ShortcutKeyDisplayString = "Ctrl++";
        larger.Click += (_,
                         _) => thumbnailView.ThumbnailSize += 16;
        _thumbnailSizeMenuItems.Add(item: smaller);
        _thumbnailSizeMenuItems.Add(item: larger);

        tmi_View.DropDownItems.AddRange(toolStripItems:
        [
            tmi_View_Details,
            tmi_View_Thumbnails,
            new ToolStripSeparator { Name = "tss_View_1" },
            sizeMenu,
            new ToolStripSeparator { Name = "tss_View_2" },
            smaller,
            larger,
            new ToolStripSeparator { Name = "tss_View_3" },
            tmi_View_SortBy
        ]);

        int position = mns_MenuStrip.Items.IndexOf(value: tmi_Settings);
        mns_MenuStrip.Items.Insert(index: position < 0 ? mns_MenuStrip.Items.Count : position, value: tmi_View);

        // The main menu was themed before this item existed; theme it again so the View menu (and its drop-downs) get
        // exactly the same look as File, Edit and the rest. Does nothing in light mode.
        ThemeHelper.ApplyTo(control: mns_MenuStrip);

        // the size slider: a small control over the right-hand end of the status row, shown while thumbnails are showing
        _thumbnailSizeSlider = new TrackBar
        {
            Name = "trb_ThumbnailSize",
            Minimum = ThumbnailLayout.MinThumbnailSize,
            Maximum = ThumbnailLayout.MaxThumbnailSize,
            SmallChange = 8,
            LargeChange = 32,
            TickStyle = TickStyle.None,
            AutoSize = false,
            Width = 150,
            Height = 26,
            Visible = false,
            // blends with the status row in either theme
            BackColor = flp_ProcessingInfo.BackColor,
            Value = thumbnailView.ThumbnailSize
        };
        _thumbnailSizeSlider.ValueChanged += (_,
                                              _) =>
        {
            if (!_syncingThumbnailSize)
            {
                thumbnailView.ThumbnailSize = _thumbnailSizeSlider.Value;
            }
        };

        Control statusRowParent = splitContainerMain.Panel1;
        void PlaceSlider() => _thumbnailSizeSlider.Location = new Point(
            x: Math.Max(val1: 0, val2: statusRowParent.ClientSize.Width - _thumbnailSizeSlider.Width - 8),
            y: Math.Max(val1: 0, val2: statusRowParent.ClientSize.Height - _thumbnailSizeSlider.Height - 2));
        PlaceSlider();
        statusRowParent.Resize += (_,
                                   _) => PlaceSlider();
        statusRowParent.Controls.Add(value: _thumbnailSizeSlider);
        _thumbnailSizeSlider.BringToFront();

        ToolTip sliderToolTip = new();
        sliderToolTip.SetToolTip(control: _thumbnailSizeSlider, caption: HelperControlAndMessageBoxHandling.ReturnControlText(
            controlName: "ttp_ThumbnailSize",
            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.ToolTip));

        RefreshThumbnailSizeControls();
    }

    /// <summary>
    ///     (Re)fills View &gt; Sort By with the columns that are showing in the details list, the current one ticked, then
    ///     ascending / descending. Rebuilt each time it opens because the user can show and hide columns.
    /// </summary>
    private void BuildSortMenu()
    {
        ToolStripItemCollection items = tmi_View_SortBy.DropDownItems;
        items.Clear();

        int current = lvw_FileList.SortColumnIndex;
        SortOrder direction = lvw_FileList.SortDirection;

        foreach (ColumnHeader column in lvw_FileList.Columns)
        {
            // hidden columns have no width; the file name is always offered
            if (column.Width == 0 &&
                column.Index != 0)
            {
                continue;
            }

            int columnIndex = column.Index;
            ToolStripMenuItem item = new()
            {
                Name = $"tmi_View_SortBy_{column.Name}",
                Text = column.Text
            };
            SetMenuMark(item: item, on: columnIndex == current);
            item.Click += (_,
                           _) => lvw_FileList.SortBy(columnIndex: columnIndex, order: lvw_FileList.SortDirection);
            _ = items.Add(value: item);
        }

        _ = items.Add(value: new ToolStripSeparator { Name = "tss_View_SortBy" });

        ToolStripMenuItem ascending = NewMenuItem(controlName: "tmi_View_SortBy_Ascending");
        SetMenuMark(item: ascending, on: direction == SortOrder.Ascending);
        ascending.Click += (_,
                            _) => lvw_FileList.SortBy(columnIndex: lvw_FileList.SortColumnIndex, order: SortOrder.Ascending);
        ToolStripMenuItem descending = NewMenuItem(controlName: "tmi_View_SortBy_Descending");
        SetMenuMark(item: descending, on: direction == SortOrder.Descending);
        descending.Click += (_,
                             _) => lvw_FileList.SortBy(columnIndex: lvw_FileList.SortColumnIndex, order: SortOrder.Descending);
        _ = items.Add(value: ascending);
        _ = items.Add(value: descending);

        // items made after start-up have to be themed like the rest of the menu
        ThemeHelper.ApplyTo(control: tmi_View_SortBy.DropDown);
    }

    /// <summary>
    ///     Shows the list as a details list or as thumbnails, without a restart, and keeps the menu, slider and stored
    ///     setting in step.
    /// </summary>
    /// <param name="mode">The mode to show.</param>
    /// <param name="persist">Whether to store the choice (false when applying what is already stored).</param>
    internal void ApplyViewMode(ListViewDisplayMode mode,
                                bool persist)
    {
        bool thumbnails = mode == ListViewDisplayMode.LargeIcons;
        listViewDisplayMode = mode;
        HelperVariables.UserSettingShowThumbnails = thumbnails;

        SetMenuMark(item: tmi_View_Details, on: !thumbnails);
        SetMenuMark(item: tmi_View_Thumbnails, on: thumbnails);
        foreach (ToolStripMenuItem sizeItem in _thumbnailSizeMenuItems)
        {
            sizeItem.Enabled = thumbnails;
        }

        _thumbnailSizeSlider.Visible = thumbnails;

        if (thumbnails)
        {
            // the list stays alive, just hidden: it still owns the items, their order and the selection
            lvw_FileList.Visible = false;
            thumbnailView.Visible = true;
        }
        else
        {
            thumbnailView.Visible = false;
            lvw_FileList.Visible = true;
            lvw_FileList.FocusedItem?.EnsureVisible();
            _ = lvw_FileList.Focus();
        }

        if (persist)
        {
            MenuSettings.WriteBool(settingId: ShowThumbnailsSettingId, value: thumbnails);
        }
    }

    /// <summary>
    ///     Applies the stored view mode and thumbnail size to the screen (after the Settings dialog, whose settings import
    ///     may have changed them).
    /// </summary>
    private void SyncViewFromSettings()
    {
        if (thumbnailView == null)
        {
            return;
        }

        // applying what is stored (or the default) is not a user change, so it must not be written back
        _applyingStoredThumbnailSize = true;
        try
        {
            thumbnailView.ThumbnailSize = HelperVariables.UserSettingThumbnailSize > 0
                ? HelperVariables.UserSettingThumbnailSize
                : DefaultThumbnailSize;
        }
        finally
        {
            _applyingStoredThumbnailSize = false;
        }

        ApplyViewMode(mode: HelperVariables.UserSettingShowThumbnails
            ? ListViewDisplayMode.LargeIcons
            : ListViewDisplayMode.Details, persist: false);
    }

    /// <summary>Brings the slider and the menu's size ticks in line with the thumbnail view's size. Stores nothing.</summary>
    private void RefreshThumbnailSizeControls()
    {
        int size = thumbnailView.ThumbnailSize;

        _syncingThumbnailSize = true;
        try
        {
            _thumbnailSizeSlider.Value = Math.Min(val1: _thumbnailSizeSlider.Maximum, val2: Math.Max(val1: _thumbnailSizeSlider.Minimum, val2: size));
        }
        finally
        {
            _syncingThumbnailSize = false;
        }

        foreach (ToolStripMenuItem presetItem in _thumbnailSizeMenuItems)
        {
            if (presetItem.Tag is int presetSize)
            {
                SetMenuMark(item: presetItem, on: presetSize == size);
            }
        }
    }

    /// <summary>The size changed (wheel, menu or slider): update the controls, and if the user did it, schedule the save.</summary>
    private void OnThumbnailSizeChanged()
    {
        RefreshThumbnailSizeControls();

        if (_applyingStoredThumbnailSize)
        {
            return;
        }

        _thumbnailSizeChangedByUser = true;
        _thumbnailSizePersistTimer?.Stop();
        _thumbnailSizePersistTimer?.Start();
    }

    /// <summary>
    ///     Stores the thumbnail size if the user changed it and it isn't stored yet (see
    ///     <see cref="_thumbnailSizeChangedByUser" />). Does nothing otherwise, in particular it never creates the setting
    ///     for someone who hasn't chosen a size.
    /// </summary>
    private void PersistThumbnailSize()
    {
        if (!_thumbnailSizeChangedByUser ||
            thumbnailView == null)
        {
            return;
        }

        _thumbnailSizeChangedByUser = false;
        int size = thumbnailView.ThumbnailSize;
        HelperVariables.UserSettingThumbnailSize = size;
        MenuSettings.WriteInt(settingId: ThumbnailSizeSettingId, value: size);
    }

    #endregion
}
