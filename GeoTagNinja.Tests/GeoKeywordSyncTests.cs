using GeoTagNinja.Helpers.Geo;
using GeoTagNinja.Model;
using NUnit.Framework;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace GeoTagNinja.Tests;

/// <summary>Keeping keywords in step with a location edit: only the photo's own old place names and the geo: pair move.</summary>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class GeoKeywordSyncTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public GeoKeywordSyncTests(string cultureName)
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

    private static GeoKeywordLocation Stratford => new()
    {
        Country = "United Kingdom", CountryCode = "GBR", State = "England", City = "Stratford",
        Latitude = 51.541315, Longitude = -0.01036167
    };

    private static string[] Run(string[] keywords,
                                GeoKeywordLocation before,
                                GeoKeywordLocation after)
    {
        return GeoKeywordSync.Apply(keywords: TagList.From(items: keywords), before: before, after: after)
                             .ToArray();
    }

    [Test]
    public void NoLocationChange_LeavesKeywordsAlone()
    {
        string[] keywords = ["Stratford", "mum"];
        Assert.AreEqual(expected: keywords, actual: Run(keywords: keywords, before: Stratford, after: Stratford));
    }

    [Test]
    public void MovedPhoto_SwapsOldNamesForNew_AndKeepsEverythingElse()
    {
        GeoKeywordLocation paris = new()
            { Country = "France", CountryCode = "FRA", State = "Ile-de-France", City = "Paris", Latitude = 48.8566, Longitude = 2.3522 };

        string[] result = Run(
            keywords: ["England", "GBR", "geo:lat=51.54131500", "geo:lon=-0.01036167", "geotagged", "Stratford", "mum",
                "United Kingdom", "Bugyi"],
            before: Stratford,
            after: paris);

        CollectionAssert.AreEquivalent(
            expected: new[]
            {
                "geotagged", "mum", "Bugyi", "France", "FRA", "Ile-de-France", "Paris", "geo:lat=48.85660000",
                "geo:lon=2.35220000"
            },
            actual: result);
    }

    [Test]
    public void OldNameThatIsAlsoTheNewName_StaysWhereItWas()
    {
        GeoKeywordLocation sameCityNewStreet = new()
            { Country = "United Kingdom", CountryCode = "GBR", State = "England", City = "Stratford", Sublocation = "Olympic Park" };

        string[] result = Run(keywords: ["Stratford", "mum", "GBR"], before: Stratford, after: sameCityNewStreet);

        Assert.AreEqual(expected: "Stratford", actual: result[0]);
        CollectionAssert.Contains(collection: result, actual: "Olympic Park");
    }

    [Test]
    public void RemovedLocation_RemovesItsKeywordsAndGeoPair_ButNothingElse()
    {
        string[] result = Run(
            keywords: ["Stratford", "geo:lat=51.54131500", "geo:lon=-0.01036167", "mum", "Hungary"],
            before: Stratford,
            after: new GeoKeywordLocation());

        CollectionAssert.AreEqual(expected: new[] { "mum", "Hungary" }, actual: result);
    }

    [Test]
    public void NameMatch_IsExactAndOrdinal_SoSimilarWordsSurvive()
    {
        // "Stratford and New Town Ward" is not the city name, and Turkish dotted/dotless I never fold together
        GeoKeywordLocation istanbul = new() { City = "\u0130stanbul" };
        string[] result = Run(
            keywords: ["Stratford and New Town Ward", "istanbul", "\u0131stanbul"],
            before: new GeoKeywordLocation { City = "Stratford" },
            after: istanbul);

        CollectionAssert.Contains(collection: result, actual: "Stratford and New Town Ward");
        CollectionAssert.Contains(collection: result, actual: "\u0130stanbul");
    }

    [Test]
    public void CoordinatesAreWrittenWithAFullStop_InEveryCulture()
    {
        string[] result = Run(keywords: [], before: new GeoKeywordLocation(),
            after: new GeoKeywordLocation { Latitude = 1.5, Longitude = -2.25 });

        CollectionAssert.AreEqual(expected: new[] { "geo:lat=1.50000000", "geo:lon=-2.25000000" }, actual: result);
    }

    [Test]
    public void OldCountry_IsRecognisedByItsOtherSpellings_ButNeverAddedBack()
    {
        // the photo's fields say "United Kingdom"/"GB"; the keywords came from a tool that wrote "GBR" and "UK" is not one of them
        GeoKeywordLocation before = new() { Country = "United Kingdom", CountryCode = "GB" };
        GeoKeywordLocation after = new() { Country = "France", CountryCode = "FR" };
        string[] result = Run(keywords: ["GBR", "gb", "United Kingdom", "mum"], before: before, after: after);

        CollectionAssert.AreEqual(expected: new[] { "mum", "France", "FR" }, actual: result);
    }

    [Test]
    public void CountryAliases_CanBeFoundFromEitherTheNameOrTheCode_AndIgnoreUnknownOnes()
    {
        CollectionAssert.Contains(collection: GeoKeywordSync.CountryAliases(country: "", code: "HUN").ToArray(),
            actual: "HU");
        CollectionAssert.Contains(collection: GeoKeywordSync.CountryAliases(country: "Hungary", code: "").ToArray(),
            actual: "HUN");
        CollectionAssert.IsEmpty(collection: GeoKeywordSync.CountryAliases(country: "Narnia", code: "").ToArray());
    }

    [Test]
    public void UnchangedCountry_KeepsItsAliasKeywords()
    {
        string[] keywords = ["GBR", "Stratford"];
        string[] result = Run(keywords: keywords,
            before: new GeoKeywordLocation { Country = "United Kingdom", CountryCode = "GB", City = "Stratford" },
            after: new GeoKeywordLocation { Country = "United Kingdom", CountryCode = "GB", City = "Leyton" });

        CollectionAssert.Contains(collection: result, actual: "GBR");
        CollectionAssert.DoesNotContain(collection: result, actual: "Stratford");
    }
}
