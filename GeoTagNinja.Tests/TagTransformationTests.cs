using GeoTagNinja.Model;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Threading;

namespace GeoTagNinja.Tests;

/// <summary>
///     The per-tag clean-up applied to raw ExifTool output, run under every culture.
/// </summary>
/// <remarks>
///     These are the strings ExifTool actually emits for jpg and raw files - rationals, values with units appended,
///     and the "35 mm equivalent" phrasing some bodies use. None of it should be read differently depending on the
///     operator's locale.
/// </remarks>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class TagTransformationTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public TagTransformationTests(string cultureName)
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

    /// <summary>Altitude arrives either as a plain number, with a unit, or as a rational.</summary>
    [TestCase("133.5", 133.5)]
    [TestCase("133.5 m", 133.5)]
    [TestCase("1335/10", 133.5)]
    public void Altitude_IsExtracted(string raw,
                                     double expected)
    {
        double? actual = TagsToModelValueTransformations.T2M_GPSAltitude(parseResult: raw);
        Assert.That(actual: actual, expression: Is.Not.Null);
        Assert.That(actual: actual.Value, expression: Is.EqualTo(expected: expected).Within(amount: 0.01));
    }

    /// <summary>GPSDOP is documented as numeric but is frequently written as a rational.</summary>
    [TestCase("4.3", 4.3)]
    [TestCase("2", 2.0)]
    public void GpsDop_IsExtracted(string raw,
                                   double expected)
    {
        double? actual = TagsToModelValueTransformations.T2M_GPSDOP(parseResult: raw);
        Assert.That(actual: actual, expression: Is.Not.Null);
        Assert.That(actual: actual.Value, expression: Is.EqualTo(expected: expected).Within(amount: 0.01));
    }

    /// <summary>The Canon 40D phrasing that prompted the 35mm-equivalent special case.</summary>
    [TestCase(SourcesAndAttributes.ElementAttribute.FocalLength, "51.0 mm", 51.0)]
    [TestCase(SourcesAndAttributes.ElementAttribute.Fnumber, "f/2.8", 2.8)]
    [TestCase(SourcesAndAttributes.ElementAttribute.FocalLengthIn35mmFormat,
        "51.0 mm (35 mm equivalent: 81.7 mm)", 81.7)]
    public void FocalLengthAndAperture_AreExtracted(SourcesAndAttributes.ElementAttribute attribute,
                                                    string raw,
                                                    double expected)
    {
        Assert.That(actual: TagsToModelValueTransformations.T2M_F_FocalLength(
                        attribute: attribute, parseResult: raw),
            expression: Is.EqualTo(expected: expected).Within(amount: 0.05));
    }

    [TestCase("1/250 sec", "1/250")]
    [TestCase("  2.5 sec  ", "2.5")]
    public void ExposureTime_LosesItsUnit(string raw,
                                          string expected)
    {
        Assert.That(actual: TagsToModelValueTransformations.T2M_ExposureTime(parseResult: raw),
            expression: Is.EqualTo(expected: expected));
    }

    [TestCase("Above Sea Level", "Above Sea Level")]
    [TestCase("Below Sea Level", "Below Sea Level")]
    [TestCase("0", "Above Sea Level")]
    [TestCase("1", "Below Sea Level")]
    public void AltitudeRef_IsNormalised(string raw,
                                         string expected)
    {
        Assert.That(actual: TagsToModelValueTransformations.T2M_AltitudeRef(parseResult: raw),
            expression: Is.EqualTo(expected: expected));
    }

    /// <summary>Both ExifTool date layouts, via the transformation the read pass actually calls.</summary>
    [TestCase("2018:06:22 19:32:53")]
    [TestCase("2018-06-22 19:32:53")]
    public void TakenOrCreateDate_IsParsed(string raw)
    {
        DateTime? actual = TagsToModelValueTransformations.T2M_TakenCreatedDate(parseResult: raw);
        Assert.That(actual: actual, expression: Is.Not.Null, message: $"failed in {_culture.Name}");
        Assert.That(actual: actual.Value,
            expression: Is.EqualTo(expected: new DateTime(
                year: 2018, month: 6, day: 22, hour: 19, minute: 32, second: 53)));
    }

    [TestCase("0000:00:00 00:00:00")]
    [TestCase(null)]
    public void EmptyDateTags_YieldNoDate(string raw)
    {
        Assert.That(actual: TagsToModelValueTransformations.T2M_TakenCreatedDate(parseResult: raw),
            expression: Is.Null);
    }
}
