using GeoTagNinja.Helpers.FileSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace GeoTagNinja.Tests;

/// <summary>
///     Batch Rename's planner: what each file is called, and what stops a batch (clashes, missing dates). File names must
///     not depend on the user's culture, so everything runs under every culture, plus a Turkish one for letter case.
/// </summary>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class BatchRenamePlannerTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public BatchRenamePlannerTests(string cultureName)
    {
        _culture = new CultureInfo(name: cultureName);
    }

    [SetUp]
    public void SetUp()
    {
        _originalCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = _culture;
    }

    [TearDown]
    public void TearDown()
    {
        Thread.CurrentThread.CurrentCulture = _originalCulture;
    }

    private static BatchRenameSource Source(string path,
                                            bool sidecar = false)
    {
        return new BatchRenameSource
        {
            FullPath = path,
            FolderName = "Mongolia",
            HasSidecar = sidecar,
            DateTaken = new DateTime(year: 2026, month: 7, day: 18, hour: 9, minute: 5, second: 3),
            Metadata = new Dictionary<string, string> { { "City", "Ulaanbaatar" } }
        };
    }

    private static BatchRenameSettings Settings(params BatchRenameComponent[] components)
    {
        return new BatchRenameSettings { Components = components.ToList() };
    }

    private static BatchRenameComponent Text(string text) => new() { Kind = BatchRenameKind.Text, Text = text };

    private static List<BatchRenameEntry> Plan(BatchRenameSettings settings,
                                               Func<string, bool> exists = null,
                                               params BatchRenameSource[] sources)
    {
        return BatchRenamePlanner.Plan(sources: sources, settings: settings, pathExists: exists ?? (_ => false),
            now: new DateTime(year: 2026, month: 11, day: 11));
    }

    [Test]
    public void SequenceNumber_IsPlainInvariantDigits_InOrderGiven()
    {
        BatchRenameSettings settings = Settings(
            new BatchRenameComponent { Kind = BatchRenameKind.SequenceNumber, Start = 9, Digits = 3 });
        List<BatchRenameEntry> plan = Plan(settings: settings, exists: null,
            Source(path: @"C:\p\a.NEF"), Source(path: @"C:\p\b.NEF"), Source(path: @"C:\p\c.NEF"));

        Assert.That(actual: plan.Select(selector: e => e.NewFileName), expression: Is.EqualTo(expected: new[] { "009.NEF", "010.NEF", "011.NEF" }));
    }

    [Test]
    public void DateTime_IsGregorianInvariant_WhateverTheCulture()
    {
        BatchRenameSettings settings = Settings(
            new BatchRenameComponent { Kind = BatchRenameKind.DateTime, DateSource = BatchRenameDateSource.DateTaken, Text = "YYYY-MM-DD_hhmmss" });
        List<BatchRenameEntry> plan = Plan(settings: settings, exists: null, Source(path: @"C:\p\a.jpg"));

        Assert.That(actual: plan[index: 0].NewFileName, expression: Is.EqualTo(expected: "2026-07-18_090503.jpg"));
    }

    [TestCase(1, "a")]
    [TestCase(26, "z")]
    [TestCase(27, "aa")]
    [TestCase(52, "az")]
    [TestCase(53, "ba")]
    public void LetterSequence_CountsLikeSpreadsheetColumns(int number,
                                                            string expected)
    {
        Assert.That(actual: BatchRenamePlanner.LetterSequence(number: number), expression: Is.EqualTo(expected: expected));
    }

    [Test]
    public void Case_UsesInvariantCasing_EvenUnderTurkish()
    {
        CultureInfo before = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(name: "tr-TR");
            Assert.That(actual: BatchRenamePlanner.ApplyCase(text: "title", mode: BatchRenameCase.Upper), expression: Is.EqualTo(expected: "TITLE"));
            Assert.That(actual: BatchRenamePlanner.ApplyCase(text: "TITLE", mode: BatchRenameCase.Lower), expression: Is.EqualTo(expected: "title"));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = before;
        }
    }

    [Test]
    public void FilePart_PicksNameExtensionAndNumberSuffix()
    {
        Assert.That(actual: BatchRenamePlanner.FilePart(fileName: "DSC0806.NEF", part: BatchRenameFilePart.Name), expression: Is.EqualTo(expected: "DSC0806"));
        Assert.That(actual: BatchRenamePlanner.FilePart(fileName: "DSC0806.NEF", part: BatchRenameFilePart.NameAndExtension), expression: Is.EqualTo(expected: "DSC0806.NEF"));
        Assert.That(actual: BatchRenamePlanner.FilePart(fileName: "DSC0806.NEF", part: BatchRenameFilePart.Extension), expression: Is.EqualTo(expected: "NEF"));
        Assert.That(actual: BatchRenamePlanner.FilePart(fileName: "DSC0806.NEF", part: BatchRenameFilePart.NumberSuffix), expression: Is.EqualTo(expected: "0806"));
        Assert.That(actual: BatchRenamePlanner.FilePart(fileName: "holiday.jpg", part: BatchRenameFilePart.NumberSuffix), expression: Is.EqualTo(expected: ""));
    }

    [Test]
    public void Sanitise_ReplacesIllegalCharacters_AndTrimsTrailingDotsAndSpaces()
    {
        Assert.That(actual: BatchRenamePlanner.Sanitise(name: "a/b:c*d. ", settings: new BatchRenameSettings()), expression: Is.EqualTo(expected: "a_b_c_d"));
        Assert.That(actual: BatchRenamePlanner.Sanitise(name: ".hidden", settings: new BatchRenameSettings()), expression: Is.EqualTo(expected: ".hidden"));
        Assert.That(actual: BatchRenamePlanner.Sanitise(name: ".hidden", settings: new BatchRenameSettings { MacCompatible = true }), expression: Is.EqualTo(expected: "hidden"));
    }

    [Test]
    public void Substitution_PlainTextIsLiteral_RegexIsNot_AndBadRegexIsAProblem()
    {
        BatchRenameComponent plain = new() { Kind = BatchRenameKind.StringSubstitution, Find = "a.c", Replace = "$1", ReplaceAll = true };
        Assert.That(actual: Plan(settings: Settings(plain), exists: null, Source(path: @"C:\p\a.c_abc.jpg"))[index: 0].NewFileName, expression: Is.EqualTo(expected: "$1_abc.jpg"));

        BatchRenameComponent regex = new() { Kind = BatchRenameKind.StringSubstitution, Find = "a.c", Replace = "X", UseRegex = true };
        Assert.That(actual: Plan(settings: Settings(regex), exists: null, Source(path: @"C:\p\a.c_abc.jpg"))[index: 0].NewFileName, expression: Is.EqualTo(expected: "X_X.jpg"));

        BatchRenameComponent bad = new() { Kind = BatchRenameKind.StringSubstitution, Find = "(", UseRegex = true };
        Assert.That(actual: Plan(settings: Settings(bad), exists: null, Source(path: @"C:\p\a.jpg"))[index: 0].Problem, expression: Is.Not.Null);
    }

    [Test]
    public void MissingDate_IsAProblem_NotAGuess()
    {
        BatchRenameSource noDate = Source(path: @"C:\p\a.jpg");
        noDate.DateTaken = null;
        BatchRenameSettings settings = Settings(
            new BatchRenameComponent { Kind = BatchRenameKind.DateTime, DateSource = BatchRenameDateSource.DateTaken, Text = "YYYYMMDD" });

        Assert.That(actual: Plan(settings: settings, exists: null, noDate)[index: 0].Problem, expression: Is.Not.Null);
    }

    [Test]
    public void SameNewNameForTwoFiles_IsAProblemForBoth()
    {
        List<BatchRenameEntry> plan = Plan(settings: Settings(Text(text: "same")), exists: null,
            Source(path: @"C:\p\a.jpg"), Source(path: @"C:\p\b.jpg"));

        Assert.That(actual: plan.Count(predicate: e => e.Problem != null), expression: Is.EqualTo(expected: 1));
        Assert.That(actual: plan[index: 0].Problem, expression: Is.Null, message: "the first file to claim a name keeps it");
    }

    [Test]
    public void ExistingFileOutsideTheBatch_IsAProblem()
    {
        List<BatchRenameEntry> plan = Plan(settings: Settings(Text(text: "taken")),
            exists: path => path == @"C:\p\taken.jpg", Source(path: @"C:\p\a.jpg"));

        Assert.That(actual: plan[index: 0].Problem, expression: Is.Not.Null);
    }

    [Test]
    public void SwappingNames_NeedsTemporaryStep_ButIsNotAProblem()
    {
        // a takes b's name, b takes c's name: b's name is free once b has moved
        BatchRenameSettings settings = Settings(
            new BatchRenameComponent { Kind = BatchRenameKind.SequenceLetter, Case = BatchRenameCase.Lower });
        HashSet<string> disk = ["C:\\p\\a.jpg", "C:\\p\\b.jpg"];
        List<BatchRenameEntry> plan = Plan(settings: settings, exists: disk.Contains,
            Source(path: @"C:\p\b.jpg"), Source(path: @"C:\p\a.jpg"));

        // letters a, b in order given: b.jpg -> a.jpg, a.jpg -> b.jpg
        Assert.That(actual: plan.All(predicate: e => e.Problem == null), expression: Is.True);
        Assert.That(actual: plan.All(predicate: e => e.NeedsTemporaryStep), expression: Is.True);
    }

    [Test]
    public void CaseOnlyChange_IsARename_ThroughATemporaryStep()
    {
        BatchRenameSettings settings = Settings(
            new BatchRenameComponent { Kind = BatchRenameKind.CurrentFilename, Part = BatchRenameFilePart.Name, Case = BatchRenameCase.Lower });
        List<BatchRenameEntry> plan = Plan(settings: settings, exists: path => path.Equals(value: @"C:\p\a.jpg", comparisonType: StringComparison.OrdinalIgnoreCase),
            Source(path: @"C:\p\A.jpg"));

        Assert.That(actual: plan[index: 0].Problem, expression: Is.Null);
        Assert.That(actual: plan[index: 0].Unchanged, expression: Is.False);
        Assert.That(actual: plan[index: 0].NeedsTemporaryStep, expression: Is.True);
    }

    [Test]
    public void SameName_IsUnchanged()
    {
        List<BatchRenameEntry> plan = Plan(settings: Settings(
            new BatchRenameComponent { Kind = BatchRenameKind.CurrentFilename, Part = BatchRenameFilePart.Name }),
            exists: path => path == @"C:\p\a.jpg", Source(path: @"C:\p\a.jpg"));

        Assert.That(actual: plan[index: 0].Unchanged, expression: Is.True);
        Assert.That(actual: plan[index: 0].Problem, expression: Is.Null);
    }

    [Test]
    public void Sidecar_FollowsTheName_AndAClashingSidecarIsAProblem()
    {
        BatchRenameSettings settings = Settings(Text(text: "new"));
        List<BatchRenameEntry> plan = Plan(settings: settings, exists: path => path == @"C:\p\new.xmp",
            Source(path: @"C:\p\a.jpg", sidecar: true));

        Assert.That(actual: plan[index: 0].NewSidecarPath, expression: Is.EqualTo(expected: @"C:\p\new.xmp"));
        Assert.That(actual: plan[index: 0].Problem, expression: Is.Not.Null);
    }

    [Test]
    public void CopyOrMove_NeedsAFolder_AndNeverOverwrites()
    {
        BatchRenameSettings settings = Settings(Text(text: "x"));
        settings.Destination = BatchRenameDestination.CopyToFolder;
        Assert.That(actual: Plan(settings: settings, exists: null, Source(path: @"C:\p\a.jpg"))[index: 0].Problem, expression: Is.Not.Null);

        settings.DestinationFolder = @"D:\out";
        Assert.That(actual: Plan(settings: settings, exists: null, Source(path: @"C:\p\a.jpg"))[index: 0].Problem, expression: Is.Null);
        Assert.That(actual: Plan(settings: settings, exists: path => path == @"D:\out\x.jpg", Source(path: @"C:\p\a.jpg"))[index: 0].Problem, expression: Is.Not.Null);
    }

    [Test]
    public void ExcludeWithDependents_DropsTheFailedFileAndWhateverWasToTakeItsName()
    {
        static BatchRenameEntry Entry(string from,
                                      string to)
        {
            return new BatchRenameEntry { Source = new BatchRenameSource { FullPath = from }, NewPath = to, NewFileName = to };
        }

        // a->b, b->c, c->d is a chain; z->y has nothing to do with it
        BatchRenameEntry ab = Entry(from: @"C:\p\a.jpg", to: @"C:\p\b.jpg");
        BatchRenameEntry bc = Entry(from: @"C:\p\b.jpg", to: @"C:\p\c.jpg");
        BatchRenameEntry cd = Entry(from: @"C:\p\c.jpg", to: @"C:\p\d.jpg");
        BatchRenameEntry zy = Entry(from: @"C:\p\z.jpg", to: @"C:\p\y.jpg");

        List<BatchRenameEntry> excluded = BatchRenameExecutor.ExcludeWithDependents(entries: [ab, bc, cd, zy], failed: cd);

        Assert.That(actual: excluded, expression: Is.EquivalentTo(expected: new[] { cd, bc, ab }));
        Assert.That(actual: excluded[index: 0], expression: Is.SameAs(expected: cd));
        Assert.That(actual: new[] { ab, bc, cd }.All(predicate: e => e.Unchanged), expression: Is.True);
        Assert.That(actual: zy.Unchanged, expression: Is.False);
    }

    [Test]
    public void Settings_RoundTripThroughJson()
    {
        BatchRenameSettings original = Settings(Text(text: "_"),
            new BatchRenameComponent { Kind = BatchRenameKind.DateTime, DateSource = BatchRenameDateSource.Yesterday, Text = "YYMMDD" });
        original.Destination = BatchRenameDestination.MoveToFolder;
        original.DestinationFolder = @"D:\out";

        BatchRenameSettings back = BatchRenameSettings.FromJson(json: original.ToJson());

        Assert.That(actual: back.Components.Count, expression: Is.EqualTo(expected: 2));
        Assert.That(actual: back.Components[index: 1].DateSource, expression: Is.EqualTo(expected: BatchRenameDateSource.Yesterday));
        Assert.That(actual: back.Destination, expression: Is.EqualTo(expected: BatchRenameDestination.MoveToFolder));
        Assert.That(actual: BatchRenameSettings.FromJson(json: "not json"), expression: Is.Null);
    }
}
