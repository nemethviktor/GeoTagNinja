using GeoTagNinja.View.Controls;
using NUnit.Framework;
using System.Drawing;

namespace GeoTagNinja.Tests;

/// <summary>
///     The thumbnail grid's geometry and keyboard movement (<see cref="ThumbnailLayout" />). Off-by-one errors here would
///     show up as the wrong picture being clicked or the focus jumping to a neighbouring row, so the edges are covered.
/// </summary>
[TestFixture]
public class ThumbnailLayoutTests
{
    // 100 wide x 50 high cells, 4 per row, 10 items => rows of 4, 4, 2
    private const int CellW = 100;
    private const int CellH = 50;
    private const int Cols = 4;

    [Test]
    public void Columns_FitAsManyAsTheWidthAllows_ButAlwaysAtLeastOne()
    {
        Assert.That(ThumbnailLayout.Columns(usableWidth: 400, cellWidth: 100), Is.EqualTo(expected: 4));
        Assert.That(ThumbnailLayout.Columns(usableWidth: 399, cellWidth: 100), Is.EqualTo(expected: 3));
        Assert.That(ThumbnailLayout.Columns(usableWidth: 10, cellWidth: 100), Is.EqualTo(expected: 1));
        Assert.That(ThumbnailLayout.Columns(usableWidth: 0, cellWidth: 100), Is.EqualTo(expected: 1));
    }

    [TestCase(0, 4, 0)]
    [TestCase(1, 4, 1)]
    [TestCase(4, 4, 1)]
    [TestCase(5, 4, 2)]
    [TestCase(10, 4, 3)]
    public void Rows_RoundUp(int count,
                             int columns,
                             int expected)
    {
        Assert.That(ThumbnailLayout.Rows(count: count, columns: columns), Is.EqualTo(expected: expected));
    }

    [Test]
    public void CellBounds_FlowLeftToRightThenDown()
    {
        Assert.That(ThumbnailLayout.CellBounds(index: 0, columns: Cols, cellWidth: CellW, cellHeight: CellH),
            Is.EqualTo(new Rectangle(x: 0, y: 0, width: CellW, height: CellH)));
        Assert.That(ThumbnailLayout.CellBounds(index: 3, columns: Cols, cellWidth: CellW, cellHeight: CellH),
            Is.EqualTo(new Rectangle(x: 300, y: 0, width: CellW, height: CellH)));
        Assert.That(ThumbnailLayout.CellBounds(index: 4, columns: Cols, cellWidth: CellW, cellHeight: CellH),
            Is.EqualTo(new Rectangle(x: 0, y: 50, width: CellW, height: CellH)));
        Assert.That(ThumbnailLayout.CellBounds(index: 9, columns: Cols, cellWidth: CellW, cellHeight: CellH),
            Is.EqualTo(new Rectangle(x: 100, y: 100, width: CellW, height: CellH)));
    }

    [Test]
    public void VisibleRange_CoversEveryRowTouchingTheBand_AndIsClampedToTheList()
    {
        // top of the list, a band two rows tall => rows 0 and 1 => items 0..7
        Assert.That(ThumbnailLayout.VisibleRange(scrollTop: 0, viewHeight: 100, cellHeight: CellH, columns: Cols, count: 10),
            Is.EqualTo((First: 0, Last: 7)));

        // scrolled half a row down: rows 0..2 all touch the band
        Assert.That(ThumbnailLayout.VisibleRange(scrollTop: 25, viewHeight: 100, cellHeight: CellH, columns: Cols, count: 10),
            Is.EqualTo((First: 0, Last: 9)));

        // exactly one row down, band exactly one row: only row 1
        Assert.That(ThumbnailLayout.VisibleRange(scrollTop: 50, viewHeight: 50, cellHeight: CellH, columns: Cols, count: 10),
            Is.EqualTo((First: 4, Last: 7)));

        // last (partial) row: last index is clamped to the list
        Assert.That(ThumbnailLayout.VisibleRange(scrollTop: 100, viewHeight: 50, cellHeight: CellH, columns: Cols, count: 10),
            Is.EqualTo((First: 8, Last: 9)));
    }

    [Test]
    public void VisibleRange_IsEmptyForNoItemsOrNoHeight()
    {
        (int first, int last) = ThumbnailLayout.VisibleRange(scrollTop: 0, viewHeight: 100, cellHeight: CellH, columns: Cols, count: 0);
        Assert.That(first > last, Is.True);

        (first, last) = ThumbnailLayout.VisibleRange(scrollTop: 0, viewHeight: 0, cellHeight: CellH, columns: Cols, count: 10);
        Assert.That(first > last, Is.True);
    }

    [Test]
    public void HitTest_FindsTheCellUnderThePoint_AndNothingInEmptySpace()
    {
        Assert.That(ThumbnailLayout.HitTest(x: 5, y: 5, columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10), Is.EqualTo(expected: 0));
        Assert.That(ThumbnailLayout.HitTest(x: 399, y: 49, columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10), Is.EqualTo(expected: 3));
        Assert.That(ThumbnailLayout.HitTest(x: 100, y: 50, columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10), Is.EqualTo(expected: 5));
        Assert.That(ThumbnailLayout.HitTest(x: 150, y: 120, columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10), Is.EqualTo(expected: 9));

        // right of the last column, left/above the origin, and the empty tail of the last row
        Assert.That(ThumbnailLayout.HitTest(x: 400, y: 5, columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10), Is.EqualTo(expected: -1));
        Assert.That(ThumbnailLayout.HitTest(x: -1, y: 5, columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10), Is.EqualTo(expected: -1));
        Assert.That(ThumbnailLayout.HitTest(x: 5, y: -1, columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10), Is.EqualTo(expected: -1));
        Assert.That(ThumbnailLayout.HitTest(x: 250, y: 120, columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10), Is.EqualTo(expected: -1));
    }

    [TestCase(5, ThumbnailNavigation.Left, 4)]
    [TestCase(5, ThumbnailNavigation.Right, 6)]
    [TestCase(5, ThumbnailNavigation.Up, 1)]
    [TestCase(5, ThumbnailNavigation.Down, 9)]
    [TestCase(5, ThumbnailNavigation.Home, 0)]
    [TestCase(5, ThumbnailNavigation.End, 9)]
    public void Navigate_MovesOneStepInTheGrid(int current,
                                               ThumbnailNavigation direction,
                                               int expected)
    {
        Assert.That(ThumbnailLayout.Navigate(current: current, direction: direction, columns: Cols, count: 10, pageRows: 2),
            Is.EqualTo(expected: expected));
    }

    [Test]
    public void Navigate_StopsAtTheEdges()
    {
        // left at the start, right at the end
        Assert.That(ThumbnailLayout.Navigate(current: 0, direction: ThumbnailNavigation.Left, columns: Cols, count: 10, pageRows: 2), Is.EqualTo(expected: 0));
        Assert.That(ThumbnailLayout.Navigate(current: 9, direction: ThumbnailNavigation.Right, columns: Cols, count: 10, pageRows: 2), Is.EqualTo(expected: 9));

        // up from the first row stays put
        Assert.That(ThumbnailLayout.Navigate(current: 2, direction: ThumbnailNavigation.Up, columns: Cols, count: 10, pageRows: 2), Is.EqualTo(expected: 2));

        // down from the last row stays put
        Assert.That(ThumbnailLayout.Navigate(current: 9, direction: ThumbnailNavigation.Down, columns: Cols, count: 10, pageRows: 2), Is.EqualTo(expected: 9));
    }

    [Test]
    public void Navigate_DownFromAColumnWithNoCellBelow_GoesToTheLastItem()
    {
        // item 3 is in the last column; row 1 (items 4-7) exists but then row 2 has only items 8 and 9.
        Assert.That(ThumbnailLayout.Navigate(current: 6, direction: ThumbnailNavigation.Down, columns: Cols, count: 10, pageRows: 2), Is.EqualTo(expected: 9));
    }

    [Test]
    public void Navigate_PageKeysMoveWholeRows_AndClamp()
    {
        Assert.That(ThumbnailLayout.Navigate(current: 1, direction: ThumbnailNavigation.PageDown, columns: Cols, count: 10, pageRows: 1), Is.EqualTo(expected: 5));
        Assert.That(ThumbnailLayout.Navigate(current: 5, direction: ThumbnailNavigation.PageUp, columns: Cols, count: 10, pageRows: 1), Is.EqualTo(expected: 1));

        // a page further than the list: down lands on the last item, up on the first
        Assert.That(ThumbnailLayout.Navigate(current: 1, direction: ThumbnailNavigation.PageDown, columns: Cols, count: 10, pageRows: 50), Is.EqualTo(expected: 9));
        Assert.That(ThumbnailLayout.Navigate(current: 9, direction: ThumbnailNavigation.PageUp, columns: Cols, count: 10, pageRows: 50), Is.EqualTo(expected: 0));
    }

    [Test]
    public void Navigate_WithNoFocusStartsAtTheFirstItem_AndNothingInAnEmptyList()
    {
        Assert.That(ThumbnailLayout.Navigate(current: -1, direction: ThumbnailNavigation.Down, columns: Cols, count: 10, pageRows: 2), Is.EqualTo(expected: 0));
        Assert.That(ThumbnailLayout.Navigate(current: -1, direction: ThumbnailNavigation.Down, columns: Cols, count: 0, pageRows: 2), Is.EqualTo(expected: -1));
    }

    [Test]
    public void IndexesIn_ReturnsEveryCellTheRectangleTouches()
    {
        // x 50..250 touches columns 0-2, y 25..85 touches rows 0-1
        Assert.That(ThumbnailLayout.IndexesIn(rect: new Rectangle(x: 50, y: 25, width: 200, height: 60), columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10),
            Is.EqualTo(new[] { 0, 1, 2, 4, 5, 6 }));

        // a rectangle starting before the origin is clipped to it
        Assert.That(ThumbnailLayout.IndexesIn(rect: new Rectangle(x: -30, y: -30, width: 80, height: 80), columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10),
            Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void IndexesIn_IgnoresCellsPastTheEndOfTheList_AndSpaceOutsideTheGrid()
    {
        // row 2 only has items 8 and 9; columns 2 and 3 of that row do not exist
        Assert.That(ThumbnailLayout.IndexesIn(rect: new Rectangle(x: 200, y: 100, width: 150, height: 40), columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10),
            Is.Empty);

        // wholly to the right of the last column, and wholly above the origin
        Assert.That(ThumbnailLayout.IndexesIn(rect: new Rectangle(x: 400, y: 0, width: 50, height: 50), columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10), Is.Empty);
        Assert.That(ThumbnailLayout.IndexesIn(rect: new Rectangle(x: 0, y: -60, width: 50, height: 50), columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10), Is.Empty);
        Assert.That(ThumbnailLayout.IndexesIn(rect: new Rectangle(x: 0, y: 0, width: 50, height: 50), columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 0), Is.Empty);
    }

    [Test]
    public void IndexesIn_ARectangleWithNoAreaIsThePixelItPointsAt()
    {
        Assert.That(ThumbnailLayout.IndexesIn(rect: new Rectangle(x: 100, y: 50, width: 0, height: 0), columns: Cols, cellWidth: CellW, cellHeight: CellH, count: 10),
            Is.EqualTo(new[] { 5 }));
    }

    [TestCase(10, ThumbnailLayout.MinThumbnailSize)]
    [TestCase(64, 64)]
    [TestCase(128, 128)]
    [TestCase(256, 256)]
    [TestCase(1000, ThumbnailLayout.MaxThumbnailSize)]
    public void ClampSize_KeepsTheSizeInRange(int size,
                                              int expected)
    {
        Assert.That(ThumbnailLayout.ClampSize(size: size), Is.EqualTo(expected: expected));
    }
}
