using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Tests;

/// <summary>What a template or "Load from Image" carries, under every culture (dates and numbers travel as invariant text).</summary>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class EditTemplateGroupTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public EditTemplateGroupTests(string cultureName)
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

    [Test]
    public void EveryAttributeBelongsToExactlyOneGroup()
    {
        List<ElementAttribute> all = EditTemplateGroups.All.SelectMany(selector: group => group.Attributes)
                                                       .ToList();
        Assert.AreEqual(expected: all.Count, actual: all.Distinct()
                                                        .Count());
    }

    [Test]
    public void NamedValues_RoundTrip()
    {
        Dictionary<ElementAttribute, string> values = new()
        {
            [ElementAttribute.IPTCKeywords] = "a\nb, c",
            [ElementAttribute.GPSLatitude] = "47.91943",
            [ElementAttribute.Copyright] = "\u00a9 \u0130stanbul"
        };

        Dictionary<ElementAttribute, string> back =
            EditTemplateGroups.FromNamed(named: EditTemplateGroups.ToNamed(values: values));

        CollectionAssert.AreEquivalent(expected: values, actual: back);
    }

    [Test]
    public void Capture_CarriesTypedValuesAsInvariantText_AndSkipsBlankDates()
    {
        Dictionary<ElementAttribute, IConvertible> parsed = new()
        {
            [ElementAttribute.GPSLatitude] = 47.91943,
            [ElementAttribute.TakenDate] = new DateTime(year: 2026, month: 2, day: 8, hour: 12, minute: 30, second: 47),
            [ElementAttribute.CreateDate] = NullEquivalents.DateTime,
            [ElementAttribute.IPTCKeywords] = TagList.From(items: ["x", "y"])
        };

        Dictionary<ElementAttribute, string> values = EditTemplateGroups.Capture(parsed: parsed);

        Assert.AreEqual(expected: "47.91943", actual: values[ElementAttribute.GPSLatitude]);
        Assert.IsFalse(condition: values.ContainsKey(key: ElementAttribute.CreateDate));
        Assert.AreEqual(expected: "x\ny", actual: values[ElementAttribute.IPTCKeywords]);

        // the date must parse back the same under any culture
        Assert.IsTrue(condition: AttributeValueFormatter.TryParse(value: values[ElementAttribute.TakenDate],
            targetType: typeof(DateTime),
            context: ValueFormatContext.RoundTrip,
            result: out IConvertible back));
        Assert.AreEqual(expected: parsed[ElementAttribute.TakenDate], actual: back);
    }
}
