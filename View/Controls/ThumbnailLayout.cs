using System;
using System.Collections.Generic;
using System.Drawing;

namespace GeoTagNinja.View.Controls;

/// <summary>Where the arrow/Home/End/Page keys move the focus in the thumbnail grid.</summary>
public enum ThumbnailNavigation
{
    Left,
    Right,
    Up,
    Down,
    Home,
    End,
    PageUp,
    PageDown
}

/// <summary>
///     The geometry of the thumbnail grid, kept free of controls so it can be unit-tested: cells flow left to right in
///     rows, as many per row as fit. Coordinates are "content" coordinates (the scrolled-out surface), not the window's.
/// </summary>
internal static class ThumbnailLayout
{
    /// <summary>Space around the thumbnail inside its cell.</summary>
    internal const int CellPadding = 8;

    /// <summary>Smallest and largest thumbnail the user can pick.</summary>
    internal const int MinThumbnailSize = 64;

    internal const int MaxThumbnailSize = 256;

    internal static int ClampSize(int size) => Math.Min(val1: MaxThumbnailSize, val2: Math.Max(val1: MinThumbnailSize, val2: size));

    internal static int CellWidth(int thumbnailSize) => thumbnailSize + (2 * CellPadding);

    /// <summary>Thumbnail, a gap, then the caption (<paramref name="captionHeight" />), with padding top and bottom.</summary>
    internal static int CellHeight(int thumbnailSize,
                                   int captionHeight) => CellPadding + thumbnailSize + CellPadding + captionHeight + CellPadding;

    /// <summary>Cells per row for a given usable width; always at least one.</summary>
    internal static int Columns(int usableWidth,
                                int cellWidth) => Math.Max(val1: 1, val2: usableWidth / Math.Max(val1: 1, val2: cellWidth));

    internal static int Rows(int count,
                             int columns) => count <= 0
        ? 0
        : (count + columns - 1) / columns;

    internal static int ContentHeight(int count,
                                      int columns,
                                      int cellHeight) => Rows(count: count, columns: columns) * cellHeight;

    /// <summary>The cell rectangle of an item, in content coordinates.</summary>
    internal static Rectangle CellBounds(int index,
                                         int columns,
                                         int cellWidth,
                                         int cellHeight) => new(x: (index % columns) * cellWidth,
        y: (index / columns) * cellHeight,
        width: cellWidth,
        height: cellHeight);

    /// <summary>
    ///     The indexes (inclusive) of the items whose rows touch the visible band. <c>first &gt; last</c> means none.
    /// </summary>
    internal static (int First, int Last) VisibleRange(int scrollTop,
                                                       int viewHeight,
                                                       int cellHeight,
                                                       int columns,
                                                       int count)
    {
        if (count <= 0 ||
            viewHeight <= 0)
        {
            return (First: 0, Last: -1);
        }

        int firstRow = Math.Max(val1: 0, val2: scrollTop / cellHeight);
        int lastRow = Math.Max(val1: 0, val2: (scrollTop + viewHeight - 1) / cellHeight);
        int first = Math.Min(val1: count - 1, val2: firstRow * columns);
        int last = Math.Min(val1: count - 1, val2: ((lastRow + 1) * columns) - 1);
        return (First: first, Last: last);
    }

    /// <summary>The item at a content-coordinate point, or -1 (empty space, or past the last item).</summary>
    internal static int HitTest(int x,
                                int y,
                                int columns,
                                int cellWidth,
                                int cellHeight,
                                int count)
    {
        if (x < 0 ||
            y < 0 ||
            x >= columns * cellWidth)
        {
            return -1;
        }

        int index = ((y / cellHeight) * columns) + (x / cellWidth);
        return index < count
            ? index
            : -1;
    }

    /// <summary>
    ///     The items whose cells a rectangle touches (for rubber-band selection), in order. The rectangle is in content
    ///     coordinates. A rectangle with no area counts as the single pixel it points at.
    /// </summary>
    internal static List<int> IndexesIn(Rectangle rect,
                                        int columns,
                                        int cellWidth,
                                        int cellHeight,
                                        int count)
    {
        List<int> result = [];
        int right = Math.Max(val1: rect.Right, val2: rect.Left + 1);
        int bottom = Math.Max(val1: rect.Bottom, val2: rect.Top + 1);

        if (count <= 0 ||
            right <= 0 ||
            bottom <= 0 ||
            rect.Left >= columns * cellWidth)
        {
            return result;
        }

        int firstColumn = Math.Max(val1: 0, val2: rect.Left) / cellWidth;
        int lastColumn = Math.Min(val1: columns - 1, val2: (right - 1) / cellWidth);
        int firstRow = Math.Max(val1: 0, val2: rect.Top) / cellHeight;
        int lastRow = (bottom - 1) / cellHeight;

        for (int row = firstRow; row <= lastRow; row++)
        {
            for (int column = firstColumn; column <= lastColumn; column++)
            {
                int index = (row * columns) + column;
                if (index < count)
                {
                    result.Add(item: index);
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Where a navigation key takes the focus from <paramref name="current" />. Never leaves the list; Down from a
    ///     column with no cell below goes to the last item, as file managers do.
    /// </summary>
    internal static int Navigate(int current,
                                 ThumbnailNavigation direction,
                                 int columns,
                                 int count,
                                 int pageRows)
    {
        if (count <= 0)
        {
            return -1;
        }

        // no focus yet: start at the first item
        if (current < 0)
        {
            return 0;
        }

        int target = direction switch
        {
            ThumbnailNavigation.Left => current - 1,
            ThumbnailNavigation.Right => current + 1,
            ThumbnailNavigation.Up => current - columns,
            ThumbnailNavigation.Down => current + columns,
            ThumbnailNavigation.Home => 0,
            ThumbnailNavigation.End => count - 1,
            ThumbnailNavigation.PageUp => current - (Math.Max(val1: 1, val2: pageRows) * columns),
            ThumbnailNavigation.PageDown => current + (Math.Max(val1: 1, val2: pageRows) * columns),
            _ => current
        };

        if (target >= count)
        {
            // moving down/page-down past the end: land on the last item if that is a new row, else stay
            bool movesDown = direction is ThumbnailNavigation.Down or ThumbnailNavigation.PageDown;
            bool lastIsOnALowerRow = (count - 1) / columns > current / columns;
            return movesDown && lastIsOnALowerRow
                ? count - 1
                : Math.Min(val1: current, val2: count - 1);
        }

        if (target < 0)
        {
            // moving up/page-up past the start: go to the first item if that is a higher row, else stay
            bool movesUp = direction is ThumbnailNavigation.Up or ThumbnailNavigation.PageUp;
            return movesUp && current / columns > 0
                ? 0
                : current;
        }

        return target;
    }
}
