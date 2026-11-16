using GeoTagNinja.Helpers.FileSystem;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace GeoTagNinja.Tests;

/// <summary>
///     The folder tree's logic: favourite folders, the chain of folders down to a path, and folder ordering. Only the
///     ordering can depend on the locale, so only that test switches culture.
/// </summary>
public class FolderTreeTests
{
    [TestCase(@"E:\Pics\2025\", @"E:\Pics\2025")]
    [TestCase(@"E:\Pics\2025", @"E:\Pics\2025")]
    [TestCase(@"  E:\Pics  ", @"E:\Pics")]
    [TestCase(@"C:", @"C:\")]
    [TestCase(@"C:\", @"C:\")]
    [TestCase(@"", @"")]
    [TestCase(@"   ", @"")]
    public void Normalise_GivesOneFormForComparingAndStoring(string input,
                                                            string expected) =>
        Assert.That(actual: FolderBookmarks.Normalise(path: input), expression: Is.EqualTo(expected: expected));

    [Test]
    public void Add_IgnoresDuplicatesDifferingOnlyByCaseOrTrailingSeparator()
    {
        FolderBookmarks bookmarks = new();
        Assert.That(actual: bookmarks.Add(path: @"E:\Pics\India"), expression: Is.True);
        Assert.That(actual: bookmarks.Add(path: @"e:\pics\INDIA\"), expression: Is.False);
        Assert.That(actual: bookmarks.Paths, expression: Is.EqualTo(expected: new[] { @"E:\Pics\India" }));
    }

    [Test]
    public void Remove_FindsTheFolderWhateverTheCase()
    {
        FolderBookmarks bookmarks = new();
        _ = bookmarks.Add(path: @"E:\A");
        _ = bookmarks.Add(path: @"E:\B");
        Assert.That(actual: bookmarks.Remove(path: @"e:\a\"), expression: Is.True);
        Assert.That(actual: bookmarks.Remove(path: @"E:\A"), expression: Is.False);
        Assert.That(actual: bookmarks.Paths, expression: Is.EqualTo(expected: new[] { @"E:\B" }));
    }

    [Test]
    public void SerialiseThenParse_KeepsOrderAndNonAsciiNames()
    {
        FolderBookmarks bookmarks = new();
        _ = bookmarks.Add(path: @"E:\Képek\Budapest");
        _ = bookmarks.Add(path: @"D:\写真\東京");
        _ = bookmarks.Add(path: @"C:\");

        Assert.That(actual: FolderBookmarks.Parse(text: bookmarks.Serialise()).Paths,
            expression: Is.EqualTo(expected: bookmarks.Paths));
    }

    [Test]
    public void Parse_DropsBlankLinesAndRepeats_AndToleratesNull()
    {
        FolderBookmarks messy = FolderBookmarks.Parse(text: "E:\\A\r\n\r\nE:\\a\\\n  \nE:\\B");
        Assert.That(actual: messy.Paths, expression: Is.EqualTo(expected: new[] { @"E:\A", @"E:\B" }));
        Assert.That(actual: FolderBookmarks.Parse(text: null).Paths, expression: Is.Empty);
    }

    [Test]
    public void AncestorChain_ListsEveryFolderFromTheDriveDown()
    {
        Assert.That(actual: FolderTreeSource.AncestorChain(path: @"E:\Pics\2025\India\"),
            expression: Is.EqualTo(expected: new[] { @"E:\", @"E:\Pics", @"E:\Pics\2025", @"E:\Pics\2025\India" }));
        Assert.That(actual: FolderTreeSource.AncestorChain(path: @"C:"), expression: Is.EqualTo(expected: new[] { @"C:\" }));
    }

    [TestCase(@"")]
    [TestCase(@"Pics\2025")]
    [TestCase(@"\\server\share\Pics")]
    public void AncestorChain_IsEmptyForWhatTheTreeCannotShow(string path) =>
        Assert.That(actual: FolderTreeSource.AncestorChain(path: path), expression: Is.Empty);

    [TestCase("en-US")]
    [TestCase("tr-TR")] // dotted/dotless i
    [TestCase("ar-SA")] // Arabic digits
    public void NaturalCompare_OrdersNumbersByValue_AndIgnoresCase_InAnyLocale(string cultureName)
    {
        CultureInfo original = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new CultureInfo(name: cultureName);
        try
        {
            List<string> sorted =
                new[]
                    {
                        "Trip 10", "trip 2", "Trip 1", "Zoo", "alpha", "20080503_Bath", "20071012_Lake_District",
                        "Iceland", "iceland"
                    }
                   .OrderBy(keySelector: n => n,
                        comparer: Comparer<string>.Create(comparison: FolderTreeSource.NaturalCompare))
                   .ToList();

            Assert.That(actual: sorted, expression: Is.EqualTo(expected: new[]
            {
                "20071012_Lake_District", "20080503_Bath", "alpha", "Iceland", "iceland", "Trip 1", "trip 2", "Trip 10",
                "Zoo"
            }));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }
}
