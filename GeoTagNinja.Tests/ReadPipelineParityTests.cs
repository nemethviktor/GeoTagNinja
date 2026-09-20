using GeoTagNinja.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Tests;

/// <summary>
///     The two ways tags reach the model - a photo read off disk, and a track-file sidecar - must produce the same
///     values from the same tags.
/// </summary>
/// <remarks>
///     There used to be two readers: <c>ReadExifData.ExifGetStandardisedDataPointFromExifAsString</c> was a
///     string-based re-implementation of the transformations, kept in step with them by hand, and it had already
///     drifted - it read rationals the typed path could not, and skipped the metric-to-imperial conversion the typed
///     path applied. These tests exist so the merge cannot quietly come undone.
/// </remarks>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class ReadPipelineParityTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public ReadPipelineParityTests(string cultureName)
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

    /// <summary>The shape the track-file reader builds out of the sidecar XMP.</summary>
    private static DataTable AsSidecarTable(IDictionary<string, string> tags)
    {
        DataTable table = new();
        table.Columns.Add(columnName: "attribute");
        table.Columns.Add(columnName: "TagValue");

        foreach (KeyValuePair<string, string> tag in tags)
        {
            DataRow row = table.NewRow();
            row[columnName: "attribute"] = tag.Key;
            row[columnName: "TagValue"] = tag.Value;
            table.Rows.Add(row: row);
        }

        return table;
    }

    private static readonly Dictionary<string, string> SampleTags = new()
    {
        { "EXIF:GPSLatitude", "47.497913" },
        { "EXIF:GPSLatitudeRef", "North" },
        { "EXIF:GPSLongitude", "19.040236" },
        { "EXIF:GPSLongitudeRef", "East" },
        { "EXIF:GPSAltitude", "133.5 m" },
        { "EXIF:GPSDOP", "43/10" }
    };

    [Test]
    public void SidecarAndPhotoTags_YieldTheSameValues()
    {
        IDictionary<ElementAttribute, IConvertible> fromPhoto =
            ExifTagSetParser.ParseTagSet(dictTagsIn: SampleTags);
        IDictionary<ElementAttribute, IConvertible> fromSidecar =
            ExifTagSetParser.ParseTagSet(
                dictTagsIn: ExifTagSetParser.TagsFromExifToolDataTable(
                    dtFileExif: AsSidecarTable(tags: SampleTags)));

        Assert.That(actual: fromSidecar, expression: Is.EquivalentTo(expected: fromPhoto),
            message: $"failed in {_culture.Name}");
    }

    [TestCase(ElementAttribute.GPSLatitude, 47.497913)]
    [TestCase(ElementAttribute.GPSLongitude, 19.040236)]
    [TestCase(ElementAttribute.GPSAltitude, 133.5)]
    [TestCase(ElementAttribute.GPSDOP, 4.3)]
    public void SidecarValues_AreTypedNotText(ElementAttribute attribute,
                                              double expected)
    {
        IDictionary<ElementAttribute, IConvertible> parsed =
            ExifTagSetParser.ParseTagSet(
                dictTagsIn: ExifTagSetParser.TagsFromExifToolDataTable(
                    dtFileExif: AsSidecarTable(tags: SampleTags)));

        Assert.That(actual: parsed.ContainsKey(key: attribute), expression: Is.True,
            message: $"{attribute} missing in {_culture.Name}");
        Assert.That(actual: parsed[key: attribute], expression: Is.TypeOf<double>());
        Assert.That(actual: (double)parsed[key: attribute],
            expression: Is.EqualTo(expected: expected).Within(amount: 0.0001),
            message: $"failed in {_culture.Name}");
    }

    /// <summary>
    ///     A tag the track did not record arrives as a present-but-empty row. It must not satisfy the lookup and stop
    ///     the reader from trying the next tag that could hold the same attribute.
    /// </summary>
    [Test]
    public void EmptySidecarRows_DoNotMaskALaterTag()
    {
        DataTable table = AsSidecarTable(tags: new Dictionary<string, string>
        {
            { "XMP:GPSLatitude", "" },
            { "EXIF:GPSLatitude", "47.497913" },
            { "EXIF:GPSLatitudeRef", "North" }
        });

        IDictionary<ElementAttribute, IConvertible> parsed =
            ExifTagSetParser.ParseTagSet(
                dictTagsIn: ExifTagSetParser.TagsFromExifToolDataTable(dtFileExif: table));

        Assert.That(actual: (double)parsed[key: ElementAttribute.GPSLatitude],
            expression: Is.EqualTo(expected: 47.497913).Within(amount: 0.0001),
            message: $"failed in {_culture.Name}");
    }

    /// <summary>
    ///     The horizontal error is derived from the dilution of precision whichever reader was used; the track-file
    ///     path was the only one that did this before.
    /// </summary>
    [Test]
    public void HPositioningError_IsDerivedForBothReaders()
    {
        IDictionary<ElementAttribute, IConvertible> parsed =
            ExifTagSetParser.ParseTagSet(dictTagsIn: SampleTags);

        Assert.That(actual: double.Parse(
                s: (string)parsed[key: ElementAttribute.GPSHPositioningError],
                provider: CultureInfo.InvariantCulture),
            expression: Is.EqualTo(expected: 12.9).Within(amount: 0.0001),
            message: $"failed in {_culture.Name}");
    }
}
