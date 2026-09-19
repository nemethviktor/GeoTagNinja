using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.Model;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Threading;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Tests;

/// <summary>
///     Copying metadata from one file and pasting it onto another, across cultures.
/// </summary>
/// <remarks>
///     The paste path used to render each value to a string and re-parse it, which cannot be made culture-safe: the
///     date below is deliberately ambiguous, and 6 July rendered invariantly as "07/06/2018" comes back as 7 June in
///     any day-first culture. No exception, just a different day. This guards the typed handover that replaced it.
/// </remarks>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class CopyPasteCultureTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public CopyPasteCultureTests(string cultureName)
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

    private static DirectoryElement NewFile(string name)
    {
        return new DirectoryElement(
            itemNameWithoutPath: name,
            type: DirectoryElement.ElementType.File,
            fileNameWithPath: @"C:\photos\" + name);
    }

    /// <summary>6 July 2018 - "07/06/2018" invariantly, "06/07/2018" day-first.</summary>
    private static readonly DateTime AmbiguousDate =
        new(year: 2018, month: 7, day: 6, hour: 19, minute: 32, second: 53);

    /// <summary>
    ///     Runs the same copy-pool handover <c>FrmPasteWhat</c> performs.
    /// </summary>
    private static void Paste(DirectoryElement from,
                              DirectoryElement to,
                              ElementAttribute attribute)
    {
        object copied = from.GetAttributeValue<DateTime>(
            attribute: attribute,
            version: from.GetMaxAttributeVersion(attribute: attribute));

        CopyPasteTags.ApplyCopiedValue(
            target: to,
            attribute: attribute,
            copiedValue: copied,
            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
    }

    [Test]
    public void PastedDate_KeepsTheSameDay()
    {
        DirectoryElement source = NewFile(name: "source.jpg");
        DirectoryElement target = NewFile(name: "target.jpg");

        source.SetAttributeValue(
            attribute: ElementAttribute.TakenDate,
            value: AmbiguousDate,
            version: DirectoryElement.AttributeVersion.Original,
            isMarkedForDeletion: false);

        Paste(from: source, to: target, attribute: ElementAttribute.TakenDate);

        Assert.That(actual: target.GetAttributeValueAsString(
                        attribute: ElementAttribute.TakenDate,
                        version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                        context: ValueFormatContext.ExifTool),
            expression: Is.EqualTo(expected: "2018-07-06 19:32:53"),
            message: $"date changed while being pasted, under {_culture.Name}");
    }
}
