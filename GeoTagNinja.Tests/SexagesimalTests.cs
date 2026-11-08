using GeoTagNinja.Helpers.Geo;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Threading;

namespace GeoTagNinja.Tests;

/// <summary>Degrees-minutes-seconds conversion: the same text and numbers in every culture.</summary>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class SexagesimalTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public SexagesimalTests(string cultureName)
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

    [TestCase(47.91943, true, "N47°55'9.95\"")]
    [TestCase(106.91757200, false, "E106°55'3.26\"")]
    [TestCase(-33.5, true, "S33°30'0\"")]
    [TestCase(-0.0001, false, "W0°0'0.36\"")]
    [TestCase(0.0, true, "N0°0'0\"")]
    public void Format_UsesFullStopAndHemisphereLetter(double degrees,
                                                       bool isLatitude,
                                                       string expected)
    {
        Assert.AreEqual(expected: expected, actual: Sexagesimal.Format(degrees: degrees, isLatitude: isLatitude));
    }

    [Test]
    public void Format_NeverShowsSixtySeconds()
    {
        // 59.999999 seconds rounds up into the next minute rather than printing 60"
        Assert.AreEqual(expected: "N10°31'0\"",
            actual: Sexagesimal.Format(degrees: 10.0 + 30.0 / 60.0 + 59.999999 / 3600.0, isLatitude: true));
    }

    [TestCase("N47°55'9.95\"", true, 47.9194306)]
    [TestCase("47°55'9.95\"N", true, 47.9194306)]
    [TestCase("S33 30 0", true, -33.5)]
    [TestCase("E106°55'3.26\"", false, 106.917572)]
    [TestCase("-12.5", true, -12.5)]
    [TestCase("12.5 W", false, -12.5)]
    [TestCase("N47°30.5'", true, 47.508333)]
    public void TryParse_ReadsCommonForms(string text,
                                          bool isLatitude,
                                          double expected)
    {
        Assert.IsTrue(condition: Sexagesimal.TryParse(text: text, isLatitude: isLatitude, degrees: out double degrees));
        Assert.AreEqual(expected: expected, actual: degrees, delta: 0.000001);
    }

    [TestCase("", true)]
    [TestCase("hello", true)]
    [TestCase("E47°55'9.95\"", true)] // a longitude letter on a latitude
    [TestCase("N95", true)]
    [TestCase("E190", false)]
    [TestCase("N47°61'", true)]
    [TestCase("N47°55'61\"", true)]
    [TestCase("N47,5", true)] // a decimal comma is not read: numbers are invariant
    public void TryParse_RefusesNonsense(string text,
                                         bool isLatitude)
    {
        Assert.IsFalse(condition: Sexagesimal.TryParse(text: text, isLatitude: isLatitude, degrees: out _));
    }

    [Test]
    public void FormatThenParse_AgreesToTheHundredthOfASecond()
    {
        foreach (double value in new[] { 47.91943, -33.865143, 0.000278, 179.999, -179.5 })
        {
            string text = Sexagesimal.Format(degrees: value, isLatitude: false);
            Assert.IsTrue(condition: Sexagesimal.TryParse(text: text, isLatitude: false, degrees: out double back));
            Assert.AreEqual(expected: value, actual: back, delta: 0.01 / 3600.0, message: text);
        }
    }
}
