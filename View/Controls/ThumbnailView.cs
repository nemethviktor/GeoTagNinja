using NLog;
using GeoTagNinja.Helpers;
using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using GeoTagNinja.View.FileList;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace GeoTagNinja.View.Controls;

/// <summary>
///     The thumbnail ("icon") view: a purpose-built control that paints the files of a <see cref="FileListView" /> as a
///     grid of pictures with captions.
/// </summary>
/// <remarks>
///     <para>
///         It is a view, not a second list. The <see cref="FileListView" /> (kept alive, hidden, while this is shown)
///         still owns the items, their order (sorting, filtering) and the selection, so the map, the preview, the
///         shortcuts and the edit flows all keep working unchanged; this control reads the items, draws them, and writes
///         the user's clicks and key presses back as selection on that list.
///     </para>
///     <para>
///         Nothing is held per file. Only the cells on screen are drawn, from a small recycled pool of decoded bitmaps
///         (<see cref="ThumbnailPool" />); the pictures themselves live as small JPEGs in <see cref="ThumbnailCache" />. A
///         cell whose picture isn't ready shows a placeholder while its own worker (visible rows first) makes it, then
///         repaints just that cell. The size is a property, so it can change at any time (Ctrl + wheel, the menu or the
///         slider): the stored thumbnails come in two sizes and are scaled to fit, so resizing doesn't regenerate them.
///     </para>
///     <para>Scrolling uses the native scroll bar (<see cref="ScrollableControl" />), so it themes like the rest of the app.</para>
/// </remarks>
internal sealed class ThumbnailView : ScrollableControl
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>How far one Ctrl + wheel notch changes the size.</summary>
    private const int WheelSizeStep = 16;

    private const int SyncIntervalMilliseconds = 100;
    private const int WorkerThreads = 3;

    private readonly FileListView _source;
    private readonly System.Windows.Forms.Timer _syncTimer = new() { Interval = SyncIntervalMilliseconds };
    private readonly ToolTip _toolTip = new();
    private readonly ThumbnailPool _pool = new();

    /// <summary>Where each element currently sits in the list, so a finished thumbnail can find its cell.</summary>
    private readonly Dictionary<DirectoryElement, int> _indexOf = [];

    private readonly ConcurrentQueue<DirectoryElement> _ready = new();

    /// <summary>Elements already asked for because their picture was missing at paint time (UI thread only).</summary>
    private readonly HashSet<DirectoryElement> _requested = [];

    private int _readyPosted;
    private ThumbnailWorker? _worker;
    private CancellationTokenSource? _workerCts;
    private int _workerBucket;

    private int _thumbnailSize = 128;
    private int _count;
    private int _columns = 1;
    private int _cellWidth;
    private int _cellHeight;
    private int _captionHeight;
    private int _leftMargin;
    private int _lastVersion = -1;
    private int _lastContentVersion = -1;
    private int _focusIndex = -1;
    private int _anchorIndex = -1;
    private int _hoverIndex = -1;

    // rubber-band selection: dragging from empty space selects the cells the rectangle touches
    private const int RubberBandThreshold = 4;
    private bool _rubberPending;
    private bool _rubberActive;
    private Point _rubberStart; // content coordinates (grid origin, unscrolled)
    private Rectangle _rubberRect; // content coordinates
    private HashSet<int> _rubberBase = [];
    private HashSet<int> _rubberLast = [];

    // in-place rename (F2)
    private TextBox? _renameBox;
    private ListViewItem? _renameItem;
    private bool _endingRename;
    private DirectoryElement? _lastPrioritisedFirst;
    private int _lastPrioritisedCount = -1;

    internal ThumbnailView(FileListView source)
    {
        _source = source;

        SetStyle(flag: ControlStyles.AllPaintingInWmPaint |
                       ControlStyles.UserPaint |
                       ControlStyles.OptimizedDoubleBuffer |
                       ControlStyles.ResizeRedraw |
                       ControlStyles.Selectable |
                       ControlStyles.StandardClick |
                       ControlStyles.StandardDoubleClick,
            value: true);
        DoubleBuffered = true;
        AutoScroll = true;
        TabStop = true;

        _syncTimer.Tick += (_,
                            _) => SyncWithSource();
        _source.SelectedIndexChanged += (_,
                                         _) => Invalidate();
    }

    /// <summary>Raised when the user double-clicks a cell.</summary>
    internal event Action<ListViewItem>? ItemActivated;

    /// <summary>
    ///     Raised once the user has finished changing the selection (mouse button released, or a navigation key released),
    ///     which is when the app updates the preview and the map.
    /// </summary>
    internal event EventHandler? UserSelectionChanged;

    /// <summary>
    ///     Raised when the user asks for the context menu (right-click, the menu key or Shift+F10), with the position in this
    ///     control's client coordinates. The selection has already been updated.
    /// </summary>
    internal event Action<Point>? ContextMenuRequested;

    /// <summary>Raised after <see cref="ThumbnailSize" /> changed.</summary>
    internal event EventHandler? ThumbnailSizeChanged;

    /// <summary>The on-screen edge length of a thumbnail, clamped to the supported range.</summary>
    internal int ThumbnailSize
    {
        get => _thumbnailSize;
        set
        {
            int size = ThumbnailLayout.ClampSize(size: value);
            if (size == _thumbnailSize)
            {
                return;
            }

            // keep roughly the same pictures on screen: remember the top-left one and scroll back to it afterwards
            int topIndex = TopVisibleIndex();
            bool bucketChanged = ThumbnailProvider.BucketFor(displaySize: size) != ThumbnailProvider.BucketFor(displaySize: _thumbnailSize);

            _thumbnailSize = size;
            ComputeLayout();
            ScrollToIndexRow(index: topIndex);

            if (bucketChanged)
            {
                // the stored thumbnails of the other size are needed now; what is decoded no longer fits
                _pool.Clear();
                _requested.Clear();
                if (Visible)
                {
                    RestartGeneration();
                    QueueAllItems();
                }
            }

            Invalidate();
            ThumbnailSizeChanged?.Invoke(sender: this, e: EventArgs.Empty);
        }
    }

    #region Generation

    /// <summary>
    ///     Throws away any queued thumbnail work and starts a fresh worker (for a new folder, or a new stored size). Does
    ///     nothing while the view is hidden: the worker only runs while thumbnails are on screen.
    /// </summary>
    internal void RestartGeneration()
    {
        StopGeneration();
        if (!Visible)
        {
            return;
        }

        _workerBucket = ThumbnailProvider.BucketFor(displaySize: _thumbnailSize);
        int bucket = _workerBucket;
        _workerCts = new CancellationTokenSource();
        _worker = new ThumbnailWorker(
            process: element =>
            {
                if (ThumbnailProvider.Ensure(directoryElement: element, bucket: bucket))
                {
                    NotifyThumbnailReady(directoryElement: element);
                }
            },
            ct: _workerCts.Token,
            workerCount: WorkerThreads);
        _lastPrioritisedFirst = null;
        _lastPrioritisedCount = -1;
    }

    private void StopGeneration()
    {
        _workerCts?.Cancel();
        _workerCts = null;
        _worker = null;
    }

    /// <summary>Queues a file for thumbnailing (as it is discovered). Ignored while hidden or for non-files.</summary>
    internal void QueueForGeneration(DirectoryElement directoryElement)
    {
        if (directoryElement.Type == DirectoryElement.ElementType.File)
        {
            _worker?.Enqueue(directoryElement: directoryElement);
        }
    }

    private void QueueAllItems()
    {
        foreach (ListViewItem item in _source.Items)
        {
            if (item.Tag is DirectoryElement element)
            {
                QueueForGeneration(directoryElement: element);
            }
        }
    }

    /// <summary>Called from worker threads when an element's thumbnail is ready in the cache.</summary>
    private void NotifyThumbnailReady(DirectoryElement directoryElement)
    {
        _ready.Enqueue(item: directoryElement);

        // one UI hop per burst rather than per thumbnail
        if (Interlocked.Exchange(location1: ref _readyPosted, value: 1) == 0 &&
            IsHandleCreated &&
            !IsDisposed)
        {
            try
            {
                _ = BeginInvoke(method: ProcessReady);
            }
            catch (InvalidOperationException)
            {
                // handle went away between the check and the call
                _ = Interlocked.Exchange(location1: ref _readyPosted, value: 0);
            }
        }
    }

    private void ProcessReady()
    {
        _ = Interlocked.Exchange(location1: ref _readyPosted, value: 0);

        int scrollTop = -AutoScrollPosition.Y;
        while (_ready.TryDequeue(result: out DirectoryElement? element))
        {
            _ = _requested.Remove(item: element);
            if (_indexOf.TryGetValue(key: element, value: out int index))
            {
                Rectangle cell = CellRectangle(index: index, scrollTop: scrollTop);
                if (cell.IntersectsWith(rect: ClientRectangle))
                {
                    Invalidate(rc: cell);
                }
            }
        }
    }

    #endregion

    #region Model sync

    /// <summary>
    ///     Brings the layout up to date with the list (new, removed, re-sorted or changed items), and keeps the thumbnail
    ///     worker aimed at what is on screen. Runs on a timer while visible; cheap when nothing changed.
    /// </summary>
    private void SyncWithSource()
    {
        int version = _source.ItemsVersion;
        int count = _source.Items.Count;
        if (version != _lastVersion ||
            count != _count)
        {
            _lastVersion = version;
            _count = count;
            RebuildIndex();
            ComputeLayout();

            if (_focusIndex >= _count)
            {
                _focusIndex = -1;
            }

            if (_anchorIndex >= _count)
            {
                _anchorIndex = -1;
            }

            if (_hoverIndex >= _count)
            {
                _hoverIndex = -1;
            }

            Invalidate();
        }

        // metadata arriving or a row being marked as changed alters captions and colours, not the layout
        if (_source.ContentVersion != _lastContentVersion)
        {
            _lastContentVersion = _source.ContentVersion;
            Invalidate();
        }

        // follow the list's focus if something else moved it (restoring the last position, the "find this file" flow)
        ListViewItem? focused = _source.FocusedItem;
        int focusedIndex = focused?.Index ?? -1;
        if (focusedIndex != _focusIndex)
        {
            _focusIndex = focusedIndex;
            if (_focusIndex >= 0)
            {
                EnsureIndexVisible(index: _focusIndex);
            }

            Invalidate();
        }

        PrioritiseVisible();
    }

    private void RebuildIndex()
    {
        _indexOf.Clear();
        for (int i = 0; i < _count; i++)
        {
            if (_source.Items[index: i].Tag is DirectoryElement element)
            {
                _indexOf[key: element] = i;
            }
        }
    }

    /// <summary>Points the worker at the visible cells first when they changed.</summary>
    private void PrioritiseVisible()
    {
        if (_worker == null)
        {
            return;
        }

        List<DirectoryElement> wanted = GetVisibleAndNearbyElements();
        DirectoryElement? first = wanted.FirstOrDefault();
        if (first == _lastPrioritisedFirst &&
            wanted.Count == _lastPrioritisedCount)
        {
            return;
        }

        _lastPrioritisedFirst = first;
        _lastPrioritisedCount = wanted.Count;
        _worker.Prioritise(elements: wanted);
    }

    /// <summary>
    ///     The files on screen, then the next screenful below and the one above, most urgent first. Used to prioritise both
    ///     thumbnails and the metadata read.
    /// </summary>
    internal List<DirectoryElement> GetVisibleAndNearbyElements()
    {
        List<DirectoryElement> result = [];
        if (_count == 0)
        {
            return result;
        }

        (int first, int last) = ThumbnailLayout.VisibleRange(scrollTop: -AutoScrollPosition.Y, viewHeight: ClientSize.Height,
            cellHeight: _cellHeight, columns: _columns, count: _count);
        if (first > last)
        {
            return result;
        }

        int span = last - first + 1;

        void Add(int from,
                 int to,
                 bool ascending)
        {
            from = Math.Max(val1: 0, val2: from);
            to = Math.Min(val1: Math.Min(val1: _count, val2: _source.Items.Count) - 1, val2: to);
            for (int i = 0; i <= to - from; i++)
            {
                int index = ascending ? from + i : to - i;
                if (_source.Items[index: index].Tag is DirectoryElement { Type: DirectoryElement.ElementType.File } element)
                {
                    result.Add(item: element);
                }
            }
        }

        Add(from: first, to: last, ascending: true);
        Add(from: last + 1, to: last + span, ascending: true);
        Add(from: first - span, to: first - 1, ascending: false);
        return result;
    }

    #endregion

    #region Layout and scrolling

    private void ComputeLayout()
    {
        _captionHeight = (Font.Height * 2) + 2;
        _cellWidth = ThumbnailLayout.CellWidth(thumbnailSize: _thumbnailSize);
        _cellHeight = ThumbnailLayout.CellHeight(thumbnailSize: _thumbnailSize, captionHeight: _captionHeight);

        // Always reserve the scroll bar's width: otherwise the bar appearing narrows the view, which changes the column
        // count, which changes the height, which can make the bar disappear again.
        int usable = ClientRectangle.Width - (VScroll ? 0 : SystemInformation.VerticalScrollBarWidth);
        _columns = ThumbnailLayout.Columns(usableWidth: usable, cellWidth: _cellWidth);
        _leftMargin = Math.Max(val1: 0, val2: (usable - (_columns * _cellWidth)) / 2);

        AutoScrollMinSize = new Size(width: 0,
            height: ThumbnailLayout.ContentHeight(count: _count, columns: _columns, cellHeight: _cellHeight));
    }

    protected override void OnResize(EventArgs e)
    {
        EndRename(commit: true);
        base.OnResize(e: e);
        ComputeLayout();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e: e);

        if (Visible)
        {
            SyncWithSource();
            _syncTimer.Start();
            RestartGeneration();
            QueueAllItems();
            Focus();
        }
        else
        {
            _syncTimer.Stop();
            StopGeneration();
            _pool.Clear();
            _requested.Clear();
        }
    }

    private Rectangle CellRectangle(int index,
                                    int scrollTop)
    {
        Rectangle cell = ThumbnailLayout.CellBounds(index: index, columns: _columns, cellWidth: _cellWidth, cellHeight: _cellHeight);
        cell.Offset(x: _leftMargin, y: -scrollTop);
        return cell;
    }

    private int TopVisibleIndex()
    {
        if (_count == 0)
        {
            return 0;
        }

        (int first, int last) = ThumbnailLayout.VisibleRange(scrollTop: -AutoScrollPosition.Y, viewHeight: ClientSize.Height,
            cellHeight: Math.Max(val1: 1, val2: _cellHeight), columns: _columns, count: _count);
        return first <= last ? first : 0;
    }

    private void ScrollToIndexRow(int index)
    {
        if (_count == 0)
        {
            return;
        }

        int row = Math.Min(val1: index, val2: _count - 1) / _columns;
        AutoScrollPosition = new Point(x: 0, y: row * _cellHeight);
    }

    /// <summary>Scrolls just enough to bring an item fully into view.</summary>
    internal void EnsureIndexVisible(int index)
    {
        if (index < 0 ||
            index >= _count)
        {
            return;
        }

        int scrollTop = -AutoScrollPosition.Y;
        Rectangle cell = ThumbnailLayout.CellBounds(index: index, columns: _columns, cellWidth: _cellWidth, cellHeight: _cellHeight);
        if (cell.Top < scrollTop)
        {
            AutoScrollPosition = new Point(x: 0, y: cell.Top);
        }
        else if (cell.Bottom > scrollTop + ClientSize.Height)
        {
            AutoScrollPosition = new Point(x: 0, y: cell.Bottom - ClientSize.Height);
        }
    }

    /// <summary>Scrolls so the given list item is on screen.</summary>
    internal void EnsureVisible(ListViewItem item)
    {
        // the list may have changed since the last tick
        SyncWithSource();
        EnsureIndexVisible(index: item.Index);
    }

    #endregion

    #region Painting

    /// <summary>
    ///     True while the list has changed (a folder reloading, items added, a re-sort) and this view's picture of it, taken
    ///     by the timer, is out of date. Anything that indexes into the list must not trust <c>_count</c> then.
    /// </summary>
    private bool IsModelStale => _source.Items.Count != _count || _source.ItemsVersion != _lastVersion;

    private static Color BackColourFor() => ThemeHelper.IsDark ? ThemeHelper.BackDark : SystemColors.Window;

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(color: BackColourFor());

        // The list can change (a folder reloading clears it) between two ticks of the sync timer. Painting from the old
        // picture would index past the end; so catch up first, on the next message, and draw then.
        if (IsModelStale)
        {
            if (IsHandleCreated)
            {
                _ = BeginInvoke(method: SyncWithSource);
            }

            return;
        }

        if (_count == 0 ||
            _cellHeight <= 0)
        {
            return;
        }

        int scrollTop = -AutoScrollPosition.Y;
        (int first, int last) = ThumbnailLayout.VisibleRange(scrollTop: scrollTop, viewHeight: ClientSize.Height,
            cellHeight: _cellHeight, columns: _columns, count: _count);

        for (int index = first; index <= last; index++)
        {
            Rectangle cell = CellRectangle(index: index, scrollTop: scrollTop);
            if (!cell.IntersectsWith(rect: e.ClipRectangle))
            {
                continue;
            }

            try
            {
                DrawCell(g: g, index: index, cell: cell);
            }
            catch (Exception ex)
            {
                // An exception escaping OnPaint makes WinForms paint a red cross over the whole control and stop painting
                // it, which is far worse than one blank cell.
                Log.Debug(ex, message: $"Thumbnail cell {index} could not be drawn");
            }
        }

        if (_rubberActive)
        {
            DrawRubberBand(g: g, scrollTop: scrollTop);
        }
    }

    private void DrawRubberBand(Graphics g,
                                int scrollTop)
    {
        Rectangle r = new(x: _rubberRect.X + _leftMargin, y: _rubberRect.Y - scrollTop, width: _rubberRect.Width, height: _rubberRect.Height);
        Color edge = ThemeHelper.IsDark ? Color.FromArgb(red: 90, green: 140, blue: 220) : Color.FromArgb(red: 120, green: 170, blue: 240);
        using SolidBrush fill = new(color: Color.FromArgb(alpha: 60, baseColor: edge));
        using Pen border = new(color: edge);
        g.FillRectangle(brush: fill, rect: r);
        g.DrawRectangle(pen: border, x: r.X, y: r.Y, width: Math.Max(val1: 0, val2: r.Width - 1), height: Math.Max(val1: 0, val2: r.Height - 1));
    }

    private void DrawCell(Graphics g,
                          int index,
                          Rectangle cell)
    {
        ListViewItem item = _source.Items[index: index];
        if (item.Tag is not DirectoryElement element)
        {
            return;
        }

        bool dark = ThemeHelper.IsDark;
        bool selected = item.Selected;

        // selection / hover background
        if (selected ||
            index == _hoverIndex)
        {
            Color fill = selected
                ? (dark ? Color.FromArgb(red: 58, green: 90, blue: 140) : Color.FromArgb(red: 204, green: 224, blue: 255))
                : (dark ? ThemeHelper.HighlightDark : Color.FromArgb(red: 232, green: 241, blue: 252));
            using SolidBrush brush = new(color: fill);
            g.FillRectangle(brush: brush, rect: Rectangle.Inflate(rect: cell, x: -2, y: -2));

            if (selected)
            {
                using Pen border = new(color: dark ? Color.FromArgb(red: 90, green: 140, blue: 220) : Color.FromArgb(red: 120, green: 170, blue: 240));
                g.DrawRectangle(pen: border, x: cell.X + 2, y: cell.Y + 2, width: cell.Width - 5, height: cell.Height - 5);
            }
        }

        // picture
        Rectangle imageArea = new(x: cell.X + ThumbnailLayout.CellPadding, y: cell.Y + ThumbnailLayout.CellPadding,
            width: _thumbnailSize, height: _thumbnailSize);
        Image? picture = element.Type == DirectoryElement.ElementType.File
            ? PictureFor(element: element)
            : FolderIcons.For(directoryElement: element);

        if (picture != null)
        {
            DrawFitted(g: g, picture: picture, area: imageArea, border: element.Type == DirectoryElement.ElementType.File);
        }
        else
        {
            DrawPlaceholder(g: g, area: imageArea, element: element);
        }

        // caption: two lines, ellipsis
        Rectangle captionArea = new(x: cell.X + ThumbnailLayout.CellPadding, y: imageArea.Bottom + ThumbnailLayout.CellPadding,
            width: _thumbnailSize, height: _captionHeight);
        Color text = item.ForeColor.ToArgb() == ThemeHelper.ErrorColour.ToArgb() || item.ForeColor.ToArgb() == Color.Red.ToArgb()
            ? ThemeHelper.ErrorColour
            : ThemeHelper.DefaultItemColour;
        if (!dark && !selected)
        {
            text = text == ThemeHelper.ErrorColour ? text : SystemColors.WindowText;
        }

        TextRenderer.DrawText(dc: g, text: element.ItemNameWithoutPath, font: Font, bounds: captionArea, foreColor: text,
            flags: TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis |
                   TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

        if (index == _focusIndex &&
            ContainsFocus)
        {
            ControlPaint.DrawFocusRectangle(graphics: g, rectangle: Rectangle.Inflate(rect: cell, x: -1, y: -1));
        }
    }

    /// <summary>The decoded thumbnail for an element, from the pool or the in-memory cache; null (and a request) if not ready.</summary>
    private Image? PictureFor(DirectoryElement element)
    {
        Bitmap? pooled = _pool.TryGet(element: element);
        if (pooled != null)
        {
            return pooled;
        }

        int bucket = ThumbnailProvider.BucketFor(displaySize: _thumbnailSize);
        Image? loaded = ThumbnailCache.TryGetMemory(fileNameWithPath: element.FileNameWithPath, size: bucket);
        if (loaded != null)
        {
            Bitmap bitmap = (Bitmap)loaded;
            _pool.Put(element: element, bitmap: bitmap, bucket: bucket);
            return bitmap;
        }

        // not in memory: make sure someone is producing it (the first time per element)
        if (_worker != null &&
            _requested.Add(item: element))
        {
            _worker.Enqueue(directoryElement: element, allowRepeat: true);
        }

        return null;
    }

    private void DrawFitted(Graphics g,
                            Image picture,
                            Rectangle area,
                            bool border)
    {
        float scale = Math.Min(val1: area.Width / (float)picture.Width, val2: area.Height / (float)picture.Height);
        int width = Math.Max(val1: 1, val2: (int)Math.Round(a: picture.Width * scale));
        int height = Math.Max(val1: 1, val2: (int)Math.Round(a: picture.Height * scale));
        Rectangle target = new(x: area.X + ((area.Width - width) / 2), y: area.Y + ((area.Height - height) / 2), width: width, height: height);

        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(image: picture, destRect: target, srcX: 0, srcY: 0, srcWidth: picture.Width, srcHeight: picture.Height,
            srcUnit: GraphicsUnit.Pixel);

        if (border)
        {
            using Pen pen = new(color: ThemeHelper.IsDark ? Color.FromArgb(red: 70, green: 72, blue: 84) : Color.FromArgb(red: 200, green: 200, blue: 200));
            g.DrawRectangle(pen: pen, x: target.X, y: target.Y, width: target.Width - 1, height: target.Height - 1);
        }
    }

    private void DrawPlaceholder(Graphics g,
                                 Rectangle area,
                                 DirectoryElement element)
    {
        // a quiet 4:3 box with the file type, so the grid keeps its shape while thumbnails arrive
        int height = (area.Width * 3) / 4;
        Rectangle box = new(x: area.X, y: area.Y + ((area.Height - height) / 2), width: area.Width, height: height);
        using SolidBrush brush = new(color: ThemeHelper.IsDark ? Color.FromArgb(red: 44, green: 46, blue: 56) : Color.FromArgb(red: 240, green: 240, blue: 240));
        g.FillRectangle(brush: brush, rect: box);

        string label = element.Extension?.ToUpperInvariant() ?? string.Empty;
        TextRenderer.DrawText(dc: g, text: label, font: Font, bounds: box,
            foreColor: ThemeHelper.IsDark ? Color.Gray : Color.DimGray,
            flags: TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    #endregion

    #region Mouse

    private int IndexAt(Point clientPoint)
    {
        // (the live count, in case the list has changed since the last timer tick)
        return ThumbnailLayout.HitTest(x: clientPoint.X - _leftMargin, y: clientPoint.Y - AutoScrollPosition.Y,
            columns: _columns, cellWidth: _cellWidth, cellHeight: _cellHeight, count: Math.Min(val1: _count, val2: _source.Items.Count));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e: e);
        _ = Focus();

        if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
        {
            return;
        }

        EndRename(commit: true);

        int index = IndexAt(clientPoint: e.Location);
        if (index < 0)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Empty space: a drag from here is a rubber band (Ctrl adds to the selection). A plain click without a
                // drag just clears the selection, as before.
                _rubberPending = true;
                _rubberStart = new Point(x: e.X - _leftMargin, y: e.Y - AutoScrollPosition.Y);
                bool keepSelection = (ModifierKeys & (Keys.Control | Keys.Shift)) != 0;
                _rubberBase = keepSelection
                    ? [.. _source.SelectedIndices.Cast<int>()]
                    : [];
                _rubberLast = [.. _rubberBase];

                if (!keepSelection)
                {
                    SetSelection(indexes: [], focus: -1);
                }
            }

            return;
        }

        bool alreadySelected = _source.Items[index: index].Selected;

        if ((ModifierKeys & Keys.Control) != 0)
        {
            ToggleSelection(index: index);
            _anchorIndex = index;
        }
        else if ((ModifierKeys & Keys.Shift) != 0 &&
                 _anchorIndex >= 0)
        {
            SelectRange(from: _anchorIndex, to: index);
        }
        else if (e.Button == MouseButtons.Right &&
                 alreadySelected)
        {
            // right-clicking inside the selection keeps it
            SetFocusOnly(index: index);
        }
        else
        {
            SetSelection(indexes: [index], focus: index);
            _anchorIndex = index;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        bool wasRubberBanding = _rubberActive;
        EndRubberBand();

        base.OnMouseUp(e: e);
        UserSelectionChanged?.Invoke(sender: this, e: EventArgs.Empty);

        // the selection was updated on mouse down, so the menu acts on what was clicked
        if (e.Button == MouseButtons.Right &&
            !wasRubberBanding)
        {
            ContextMenuRequested?.Invoke(obj: e.Location);
        }
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e: e);
        if (!Capture)
        {
            EndRubberBand();
        }
    }

    private void EndRubberBand()
    {
        if (!_rubberPending &&
            !_rubberActive)
        {
            return;
        }

        _rubberPending = false;
        _rubberActive = false;
        Invalidate();
    }

    /// <summary>Grows the rubber band to the pointer and selects what it touches. Dragging past an edge scrolls.</summary>
    private void UpdateRubberBand(Point client)
    {
        if ((MouseButtons & MouseButtons.Left) == 0)
        {
            EndRubberBand();
            return;
        }

        Point current = new(x: client.X - _leftMargin, y: client.Y - AutoScrollPosition.Y);
        if (!_rubberActive)
        {
            if (Math.Abs(value: current.X - _rubberStart.X) < RubberBandThreshold &&
                Math.Abs(value: current.Y - _rubberStart.Y) < RubberBandThreshold)
            {
                return;
            }

            _rubberActive = true;
        }

        int scrollTop = -AutoScrollPosition.Y;
        if (client.Y < 0)
        {
            AutoScrollPosition = new Point(x: 0, y: Math.Max(val1: 0, val2: scrollTop - 30));
        }
        else if (client.Y > ClientSize.Height)
        {
            AutoScrollPosition = new Point(x: 0, y: scrollTop + 30);
        }

        current = new Point(x: client.X - _leftMargin, y: client.Y - AutoScrollPosition.Y);
        _rubberRect = Rectangle.FromLTRB(
            left: Math.Min(val1: _rubberStart.X, val2: current.X),
            top: Math.Min(val1: _rubberStart.Y, val2: current.Y),
            right: Math.Max(val1: _rubberStart.X, val2: current.X),
            bottom: Math.Max(val1: _rubberStart.Y, val2: current.Y));

        List<int> touched = ThumbnailLayout.IndexesIn(rect: _rubberRect, columns: _columns, cellWidth: _cellWidth,
            cellHeight: _cellHeight, count: Math.Min(val1: _count, val2: _source.Items.Count));
        HashSet<int> wanted = [.. _rubberBase, .. touched];

        // only touch the list when the set actually changed
        if (!wanted.SetEquals(other: _rubberLast))
        {
            _rubberLast = wanted;
            SetSelection(indexes: wanted, focus: touched.Count > 0 ? touched.Max() : -1);
        }

        Invalidate();
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e: e);
        int index = IndexAt(clientPoint: e.Location);
        if (index >= 0)
        {
            ItemActivated?.Invoke(obj: _source.Items[index: index]);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e: e);

        if (_rubberPending ||
            _rubberActive)
        {
            UpdateRubberBand(client: e.Location);
            return;
        }

        int index = IndexAt(clientPoint: e.Location);
        if (index == _hoverIndex)
        {
            return;
        }

        int scrollTop = -AutoScrollPosition.Y;
        if (_hoverIndex >= 0 &&
            _hoverIndex < _count)
        {
            Invalidate(rc: CellRectangle(index: _hoverIndex, scrollTop: scrollTop));
        }

        _hoverIndex = index;

        // a tip that is already showing keeps the old text, so hide it before the caption changes
        _toolTip.Hide(win: this);
        if (index >= 0)
        {
            Invalidate(rc: CellRectangle(index: index, scrollTop: scrollTop));
            _toolTip.SetToolTip(control: this, caption: (_source.Items[index: index].Tag as DirectoryElement)?.ItemNameWithoutPath);
        }
        else
        {
            _toolTip.SetToolTip(control: this, caption: string.Empty);
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e: e);
        if (_hoverIndex >= 0 &&
            _hoverIndex < _count)
        {
            Invalidate(rc: CellRectangle(index: _hoverIndex, scrollTop: -AutoScrollPosition.Y));
        }

        _hoverIndex = -1;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        // an editor over a caption would be left behind by the scrolling
        EndRename(commit: true);

        // Ctrl + wheel resizes the thumbnails, as in other image browsers; plain wheel scrolls
        if ((ModifierKeys & Keys.Control) != 0)
        {
            ThumbnailSize += e.Delta > 0 ? WheelSizeStep : -WheelSizeStep;
            if (e is HandledMouseEventArgs handled)
            {
                handled.Handled = true;
            }

            return;
        }

        base.OnMouseWheel(e: e);
    }

    #endregion

    #region Keyboard

    protected override bool IsInputKey(Keys keyData)
    {
        switch (keyData & Keys.KeyCode)
        {
            case Keys.Left:
            case Keys.Right:
            case Keys.Up:
            case Keys.Down:
            case Keys.Home:
            case Keys.End:
            case Keys.PageUp:
            case Keys.PageDown:
                return true;
        }

        return base.IsInputKey(keyData: keyData);
    }

    private static ThumbnailNavigation? NavigationFor(Keys key) => key switch
    {
        Keys.Left => ThumbnailNavigation.Left,
        Keys.Right => ThumbnailNavigation.Right,
        Keys.Up => ThumbnailNavigation.Up,
        Keys.Down => ThumbnailNavigation.Down,
        Keys.Home => ThumbnailNavigation.Home,
        Keys.End => ThumbnailNavigation.End,
        Keys.PageUp => ThumbnailNavigation.PageUp,
        Keys.PageDown => ThumbnailNavigation.PageDown,
        _ => null
    };

    protected override void OnKeyDown(KeyEventArgs e)
    {
        ThumbnailNavigation? direction = NavigationFor(key: e.KeyCode);
        if (direction != null)
        {
            e.Handled = true;

            int pageRows = Math.Max(val1: 1, val2: ClientSize.Height / Math.Max(val1: 1, val2: _cellHeight));
            int target = ThumbnailLayout.Navigate(current: _focusIndex, direction: direction.Value, columns: _columns,
                count: _count, pageRows: pageRows);

            if (target >= 0)
            {
                if (e.Shift)
                {
                    SelectRange(from: _anchorIndex >= 0 ? _anchorIndex : _focusIndex, to: target);
                }
                else if (e.Control)
                {
                    // Ctrl + arrows move the focus without touching the selection
                    SetFocusOnly(index: target);
                }
                else
                {
                    SetSelection(indexes: [target], focus: target);
                    _anchorIndex = target;
                }

                EnsureIndexVisible(index: target);
            }
        }
        else if (e.KeyCode == Keys.Space &&
                 e.Control &&
                 _focusIndex >= 0)
        {
            e.Handled = true;
            ToggleSelection(index: _focusIndex);
        }
        else if (e.KeyCode == Keys.F2)
        {
            e.Handled = true;
            BeginRename();
        }
        else if (e.KeyCode == Keys.Apps ||
                 (e.KeyCode == Keys.F10 && e.Shift))
        {
            e.Handled = true;
            Point at = new(x: 10, y: 10);
            if (_focusIndex >= 0 &&
                _focusIndex < _count)
            {
                Rectangle cell = CellRectangle(index: _focusIndex, scrollTop: -AutoScrollPosition.Y);
                at = new Point(x: cell.Left + (cell.Width / 2), y: cell.Top + (cell.Height / 2));
            }

            ContextMenuRequested?.Invoke(obj: at);
        }

        // lets the app handle everything else (Ctrl+A, Enter, Delete, F5, Ctrl+S, ...)
        base.OnKeyDown(e: e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e: e);
        if (NavigationFor(key: e.KeyCode) != null)
        {
            UserSelectionChanged?.Invoke(sender: this, e: EventArgs.Empty);
        }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e: e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e: e);
        Invalidate();
    }

    #endregion

    #region In-place rename (F2)

    /// <summary>
    ///     Opens an editor over the caption of the selected file (a single file only). Enter or clicking away renames it
    ///     through <see cref="FileListView.TryRenameItem" />, the same code the details list uses (so the extension is kept
    ///     and the XMP sidecar goes with it); Escape cancels.
    /// </summary>
    private void BeginRename()
    {
        if (_renameBox != null ||
            _source.SelectedItems.Count != 1 ||
            _source.SelectedItems[index: 0].Tag is not DirectoryElement { Type: DirectoryElement.ElementType.File } element)
        {
            return;
        }

        ListViewItem item = _source.SelectedItems[index: 0];
        EnsureIndexVisible(index: item.Index);
        Rectangle cell = CellRectangle(index: item.Index, scrollTop: -AutoScrollPosition.Y);
        bool dark = ThemeHelper.IsDark;

        _renameItem = item;
        _renameBox = new TextBox
        {
            Text = Path.GetFileNameWithoutExtension(path: element.FileNameWithPath),
            Font = Font,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Center,
            BackColor = dark ? ThemeHelper.HighlightDark : SystemColors.Window,
            ForeColor = dark ? ThemeHelper.ForeDark : SystemColors.WindowText,
            Bounds = new Rectangle(x: cell.X + 2,
                y: cell.Y + ThumbnailLayout.CellPadding + _thumbnailSize + ThumbnailLayout.CellPadding,
                width: cell.Width - 4,
                height: Font.Height + 6)
        };

        _renameBox.KeyDown += (_,
                               e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                EndRename(commit: true);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                EndRename(commit: false);
            }
        };
        _renameBox.LostFocus += (_,
                                 _) => EndRename(commit: true);

        Controls.Add(value: _renameBox);
        _renameBox.BringToFront();
        _ = _renameBox.Focus();
        _renameBox.SelectAll();
    }

    private void EndRename(bool commit)
    {
        if (_renameBox == null ||
            _endingRename)
        {
            return;
        }

        _endingRename = true;
        try
        {
            string text = _renameBox.Text;
            ListViewItem? item = _renameItem;

            Controls.Remove(value: _renameBox);
            _renameBox.Dispose();
            _renameBox = null;
            _renameItem = null;
            _ = Focus();

            if (commit &&
                item != null &&
                _source.TryRenameItem(lvi: item, newBaseName: text))
            {
                // the caption changed
                Invalidate();
            }
        }
        finally
        {
            _endingRename = false;
        }
    }

    #endregion

    #region Selection (written through to the list, which stays the source of truth)

    /// <summary>Makes exactly these items the selection (touching only those that change) and focuses one.</summary>
    private void SetSelection(IEnumerable<int> indexes,
                              int focus)
    {
        HashSet<int> wanted = [.. indexes];

        _source.BeginUpdate();
        try
        {
            foreach (int selected in _source.SelectedIndices.Cast<int>().ToList())
            {
                if (!wanted.Contains(item: selected))
                {
                    _source.Items[index: selected].Selected = false;
                }
            }

            foreach (int index in wanted)
            {
                if (index >= 0 &&
                    index < _source.Items.Count &&
                    !_source.Items[index: index].Selected)
                {
                    _source.Items[index: index].Selected = true;
                }
            }

            SetFocusOnly(index: focus);
        }
        finally
        {
            _source.EndUpdate();
        }

        Invalidate();
    }

    private void SetFocusOnly(int index)
    {
        _focusIndex = index;
        if (index >= 0 &&
            index < _source.Items.Count)
        {
            _source.Items[index: index].Focused = true;
        }

        Invalidate();
    }

    private void ToggleSelection(int index)
    {
        if (index < 0 ||
            index >= _source.Items.Count)
        {
            return;
        }

        ListViewItem item = _source.Items[index: index];
        item.Selected = !item.Selected;
        SetFocusOnly(index: index);
    }

    private void SelectRange(int from,
                             int to)
    {
        if (from < 0)
        {
            from = to;
        }

        int low = Math.Min(val1: from, val2: to);
        int high = Math.Max(val1: from, val2: to);
        SetSelection(indexes: Enumerable.Range(start: low, count: high - low + 1), focus: to);
        _anchorIndex = from;
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _syncTimer.Stop();
            _syncTimer.Dispose();
            StopGeneration();
            _toolTip.Dispose();
            _pool.Clear();
        }

        base.Dispose(disposing: disposing);
    }

    /// <summary>
    ///     A small least-recently-used pool of decoded thumbnails for the cells being drawn, so a repaint doesn't decode
    ///     JPEGs again. Bounded by memory, not by the size of the folder. UI thread only.
    /// </summary>
    private sealed class ThumbnailPool
    {
        private const long MaxBytes = 96L * 1024 * 1024;
        private readonly Dictionary<DirectoryElement, LinkedListNode<(DirectoryElement Element, Bitmap Bitmap)>> _nodes = [];
        private readonly LinkedList<(DirectoryElement Element, Bitmap Bitmap)> _byRecency = new();
        private int _capacity = 400;

        internal Bitmap? TryGet(DirectoryElement element)
        {
            if (!_nodes.TryGetValue(key: element, value: out LinkedListNode<(DirectoryElement Element, Bitmap Bitmap)>? node))
            {
                return null;
            }

            _byRecency.Remove(node: node);
            _byRecency.AddFirst(node: node);
            return node.Value.Bitmap;
        }

        internal void Put(DirectoryElement element,
                          Bitmap bitmap,
                          int bucket)
        {
            // as many decoded bitmaps as fit the budget at this stored size (32 bits per pixel)
            _capacity = (int)Math.Max(val1: 100, val2: MaxBytes / ((long)bucket * bucket * 4));

            if (_nodes.TryGetValue(key: element, value: out LinkedListNode<(DirectoryElement Element, Bitmap Bitmap)>? existing))
            {
                existing.Value.Bitmap.Dispose();
                _byRecency.Remove(node: existing);
            }

            _nodes[key: element] = _byRecency.AddFirst(value: (element, bitmap));

            while (_byRecency.Count > _capacity)
            {
                LinkedListNode<(DirectoryElement Element, Bitmap Bitmap)> oldest = _byRecency.Last!;
                _byRecency.RemoveLast();
                _ = _nodes.Remove(key: oldest.Value.Element);
                oldest.Value.Bitmap.Dispose();
            }
        }

        internal void Clear()
        {
            foreach ((DirectoryElement _, Bitmap bitmap) in _byRecency)
            {
                bitmap.Dispose();
            }

            _byRecency.Clear();
            _nodes.Clear();
        }
    }
}
