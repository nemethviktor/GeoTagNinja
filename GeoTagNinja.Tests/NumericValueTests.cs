using GeoTagNinja.Model;
using NUnit.Framework;
using System.Globalization;
using System.Threading;

namespace GeoTagNinja.Tests;

/// <summary>
///     Numeric handling, run under every culture in <see cref="Cultures.All" />.
/// </summary>
/// <remarks>
///     The field report behind this fixture: decimals were not saved correctly because 5.4 in English is 5,4 in
///     Hungarian. Coordinates, altitudes and f-numbers all go through this path, and a coordinate that is wrong by a
///     factor of ten puts the photo in the wrong country.
/// </remarks>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class NumericValueTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public NumericValueTests(string cultureName)
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

    /// <summary>What lands in the file must not depend on where the user lives.</summary>
    [TestCase(5.4, "5.4")]
    [TestCase(-33.8688, "-33.8688")]
    [TestCase(151.2093, "151.2093")]
    [TestCase(0.0, "0")]
    [TestCase(1234.5, "1234.5")]
    public void ExifToolFormat_UsesDotAndNoGrouping(double value,
                                                    string expected)
    {
        Assert.That(actual: AttributeValueFormatter.FormatDouble(
                        value: value, context: ValueFormatContext.ExifTool),
            expression: Is.EqualTo(expected: expected));
    }

    /// <summary>A machine-readable decimal must parse identically everywhere.</summary>
    [TestCase("5.4", 5.4)]
    [TestCase("-33.8688", -33.8688)]
    [TestCase("0", 0.0)]
    public void InvariantDecimal_Parses(string text,
                                        double expected)
    {
        Assert.That(actual: AttributeValueFormatter.TryParseDouble(
                        value: text, context: ValueFormatContext.ExifTool, result: out double parsed),
            expression: Is.True);
        Assert.That(actual: parsed, expression: Is.EqualTo(expected: expected).Within(amount: 1e-9));
    }

    /// <summary>
    ///     The headline case. A user typing their own decimal separator into the edit form must get the number they
    ///     meant - not that number multiplied by ten, which is what happens when a comma is read as digit grouping.
    /// </summary>
    [Test]
    public void UserTypedDecimal_IsReadAsTheUserMeantIt()
    {
        string typed = 5.4.ToString(provider: _culture);

        Assert.That(actual: AttributeValueFormatter.TryParseDouble(
                        value: typed, context: ValueFormatContext.Display, result: out double parsed),
            expression: Is.True, message: $"could not parse '{typed}'");
        Assert.That(actual: parsed, expression: Is.EqualTo(expected: 5.4).Within(amount: 1e-9),
            message: $"'{typed}' in {_culture.Name} was read as {parsed}");
    }

    /// <summary>Anything the user is shown must be readable back without loss.</summary>
    [TestCase(5.4)]
    [TestCase(-33.8688)]
    [TestCase(1234.5)]
    public void DisplayedNumber_RoundTrips(double value)
    {
        string shown = AttributeValueFormatter.FormatDouble(
            value: value, context: ValueFormatContext.Display);

        Assert.That(actual: AttributeValueFormatter.TryParseDouble(
                        value: shown, context: ValueFormatContext.Display, result: out double parsed),
            expression: Is.True, message: $"could not re-read '{shown}'");
        Assert.That(actual: parsed, expression: Is.EqualTo(expected: value).Within(amount: 1e-9));
    }

    [TestCase("100", 100)]
    [TestCase("6400", 6400)]
    public void Integers_ParseInvariantly(string text,
                                          int expected)
    {
        Assert.That(actual: AttributeValueFormatter.TryParseInt(
                        value: text, context: ValueFormatContext.ExifTool, result: out int parsed),
            expression: Is.True);
        Assert.That(actual: parsed, expression: Is.EqualTo(expected: expected));
    }

    [Test]
    public void IntegerFormat_IsIdenticalInEveryCulture()
    {
        Assert.That(actual: AttributeValueFormatter.FormatInt(
                        value: 6400, context: ValueFormatContext.ExifTool),
            expression: Is.EqualTo(expected: "6400"));
    }
}
