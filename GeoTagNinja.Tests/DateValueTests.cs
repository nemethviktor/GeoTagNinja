using GeoTagNinja.Model;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Threading;

namespace GeoTagNinja.Tests;

/// <summary>
///     Date handling, run under every culture in <see cref="Cultures.All" />.
/// </summary>
/// <remarks>
///     This fixture exists because of a specific field report: users outside the UK/US who used "insert create date"
///     on a file that had none found the wrong value written to the file. The root cause was that ExifTool output was
///     parsed with the current culture, so the answer depended on where the user lived.
/// </remarks>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class DateValueTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public DateValueTests(string cultureName)
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

    private static readonly DateTime Sample =
        new(year: 2018, month: 6, day: 22, hour: 19, minute: 32, second: 53);

    /// <summary>
    ///     What goes into a file must be identical everywhere. If this fails, two users editing the same photo write
    ///     different bytes.
    /// </summary>
    [Test]
    public void ExifToolFormat_IsIdenticalInEveryCulture()
    {
        Assert.That(actual: AttributeValueFormatter.FormatDateTime(
                        value: Sample, context: ValueFormatContext.ExifTool),
            expression: Is.EqualTo(expected: "2018-06-22 19:32:53"));
    }

    /// <summary>
    ///     ExifTool's own default output layout. GeoTagNinja ships a config that overrides it, but the parser must not
    ///     depend on that config having been found.
    /// </summary>
    [Test]
    public void NativeExifToolLayout_Parses()
    {
        Assert.That(actual: AttributeValueFormatter.TryParseDateTime(
                        value: "2018:06:22 19:32:53",
                        context: ValueFormatContext.ExifTool,
                        result: out DateTime parsed),
            expression: Is.True);
        Assert.That(actual: parsed, expression: Is.EqualTo(expected: Sample));
    }

    /// <summary>The layout the bundled .ExifTool_config actually produces.</summary>
    [Test]
    public void ConfiguredExifToolLayout_Parses()
    {
        Assert.That(actual: AttributeValueFormatter.TryParseDateTime(
                        value: "2018-06-22 19:32:53",
                        context: ValueFormatContext.ExifTool,
                        result: out DateTime parsed),
            expression: Is.True);
        Assert.That(actual: parsed, expression: Is.EqualTo(expected: Sample));
    }

    /// <summary>A value shown to the user must read back as the same instant.</summary>
    [Test]
    public void DisplayText_RoundTrips()
    {
        string shown = AttributeValueFormatter.FormatDateTime(
            value: Sample, context: ValueFormatContext.Display);

        Assert.That(actual: AttributeValueFormatter.TryParseDateTime(
                        value: shown, context: ValueFormatContext.Display,
                        result: out DateTime parsed),
            expression: Is.True, message: $"could not re-read '{shown}'");
        Assert.That(actual: parsed, expression: Is.EqualTo(expected: Sample));
    }

    /// <summary>Values handed between parts of the app must survive exactly.</summary>
    [Test]
    public void RoundTripText_RoundTrips()
    {
        string carried = AttributeValueFormatter.FormatDateTime(
            value: Sample, context: ValueFormatContext.RoundTrip);

        Assert.That(actual: AttributeValueFormatter.TryParseDateTime(
                        value: carried, context: ValueFormatContext.RoundTrip,
                        result: out DateTime parsed),
            expression: Is.True);
        Assert.That(actual: parsed, expression: Is.EqualTo(expected: Sample));
    }

    /// <summary>ExifTool writes an all-zero stamp for a tag that exists but is empty; that is not a date.</summary>
    [TestCase("0000:00:00 00:00:00")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not a date")]
    [TestCase(null)]
    public void NonDates_AreRejected(string value)
    {
        Assert.That(actual: AttributeValueFormatter.TryParseDateTime(
                        value: value, context: ValueFormatContext.ExifTool, result: out DateTime _),
            expression: Is.False);
        Assert.That(actual: AttributeValueFormatter.ParseDateTimeOrNull(
                        value: value, context: ValueFormatContext.ExifTool),
            expression: Is.Null);
    }
}
