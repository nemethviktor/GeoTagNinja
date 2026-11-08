using GeoTagNinja.Helpers.Geo;
using GeoTagNinja.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace GeoTagNinja.Tests;

/// <summary>
///     Deciding whether a photo lies inside a favourite, under every culture in <see cref="Cultures.All" />.
/// </summary>
/// <remarks>
///     A wrong answer here relabels a photo with the wrong place, and with Snap on moves it there. The favourite's
///     position is text ("47.4979", "North"), so a comma-decimal locale reading it wrongly (47,4979 read as 474979)
///     would silently match nothing - or the wrong thing.
/// </remarks>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class FavouriteMatcherTests
{
    // Budapest, Parliament; about 1.1 km from the Chain Bridge.
    private const string ParliamentLat = "47.5071";
    private const string ParliamentLon = "19.0456";

    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public FavouriteMatcherTests(string cultureName)
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

    private static Favourite Fav(string name,
                                 string lat,
                                 string latRef,
                                 string lon,
                                 string lonRef,
                                 int radius,
                                 bool autoAssign = true)
    {
        return new Favourite
        {
            FavouriteName = name,
            GPSLatitude = lat,
            GPSLatitudeRef = latRef,
            GPSLongitude = lon,
            GPSLongitudeRef = lonRef,
            Radius = radius,
            AutoAssign = autoAssign
        };
    }

    [Test]
    public void Distance_IsKnownValue()
    {
        // one degree of latitude is about 111.2 km
        double metres = FavouriteMatcher.DistanceMetres(latitude1: 10, longitude1: 20, latitude2: 11,
            longitude2: 20);
        Assert.That(actual: metres, expression: Is.EqualTo(expected: 111195).Within(amount: 100));
    }

    [Test]
    public void Distance_IsZeroForTheSamePlace_AndSymmetric()
    {
        Assert.That(
            actual: FavouriteMatcher.DistanceMetres(latitude1: 47.5, longitude1: 19, latitude2: 47.5,
                longitude2: 19), expression: Is.Zero);

        double ab = FavouriteMatcher.DistanceMetres(latitude1: 47.5, longitude1: 19, latitude2: -33.9,
            longitude2: 151.2);
        double ba = FavouriteMatcher.DistanceMetres(latitude1: -33.9, longitude1: 151.2, latitude2: 47.5,
            longitude2: 19);
        Assert.That(actual: ab, expression: Is.EqualTo(expected: ba).Within(amount: 0.001));
    }

    [Test]
    public void Distance_AcrossTheAntimeridian_IsShort()
    {
        double metres = FavouriteMatcher.DistanceMetres(latitude1: 0, longitude1: 179.9, latitude2: 0,
            longitude2: -179.9);
        Assert.That(actual: metres, expression: Is.LessThan(expected: 30000));
    }

    [Test]
    public void ParsePosition_ReadsInvariantText_WhateverTheCulture()
    {
        bool parsed = FavouriteMatcher.TryParsePosition(latitude: "47.5071", latitudeRef: "North",
            longitude: "19.0456", longitudeRef: "East", latitudeSigned: out double lat,
            longitudeSigned: out double lon);

        Assert.That(actual: parsed, expression: Is.True);
        Assert.That(actual: lat, expression: Is.EqualTo(expected: 47.5071));
        Assert.That(actual: lon, expression: Is.EqualTo(expected: 19.0456));
    }

    [TestCase("South", "West", -1, -1)]
    [TestCase("S", "W", -1, -1)]
    [TestCase("s", "w", -1, -1)]
    [TestCase("North", "East", 1, 1)]
    [TestCase("", "", 1, 1)]
    public void ParsePosition_SouthAndWestAreNegative(string latRef,
                                                      string lonRef,
                                                      int latSign,
                                                      int lonSign)
    {
        _ = FavouriteMatcher.TryParsePosition(latitude: "33.9", latitudeRef: latRef, longitude: "151.2",
            longitudeRef: lonRef, latitudeSigned: out double lat, longitudeSigned: out double lon);

        Assert.That(actual: lat, expression: Is.EqualTo(expected: 33.9 * latSign));
        Assert.That(actual: lon, expression: Is.EqualTo(expected: 151.2 * lonSign));
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("abc")]
    [TestCase("91")]
    [TestCase("NaN")]
    [TestCase("47,5071")] // a comma-decimal culture's spelling: the app stores invariant text, so this is not a number
    public void ParsePosition_RejectsUnusableLatitude(string latitude)
    {
        // "47,5071" must not be read as 47.5071 (nor as 475071 with the comma taken for a thousands separator).
        Assert.That(
            actual: FavouriteMatcher.TryParsePosition(latitude: latitude, latitudeRef: "North",
                longitude: "19", longitudeRef: "East", latitudeSigned: out _, longitudeSigned: out _),
            expression: Is.False);
    }

    [Test]
    public void Match_FindsFavouriteWhoseRadiusContainsThePhoto()
    {
        List<Favourite> favourites =
        [
            Fav(name: "Parliament", lat: ParliamentLat, latRef: "North", lon: ParliamentLon, lonRef: "East",
                radius: 300)
        ];

        // about 100 m away
        Favourite match = FavouriteMatcher.FindAutoAssignMatch(favourites: favourites, latitude: 47.5080,
            longitude: 19.0456);

        Assert.That(actual: match?.FavouriteName, expression: Is.EqualTo(expected: "Parliament"));
    }

    [Test]
    public void Match_IgnoresPhotosOutsideTheRadius()
    {
        List<Favourite> favourites =
        [
            Fav(name: "Parliament", lat: ParliamentLat, latRef: "North", lon: ParliamentLon, lonRef: "East",
                radius: 300)
        ];

        // about 1.1 km away
        Assert.That(
            actual: FavouriteMatcher.FindAutoAssignMatch(favourites: favourites, latitude: 47.5171,
                longitude: 19.0456), expression: Is.Null);
    }

    [Test]
    public void Match_SkipsFavouritesThatAreNotAutoAssignOrHaveNoRadius()
    {
        List<Favourite> favourites =
        [
            Fav(name: "Off", lat: ParliamentLat, latRef: "North", lon: ParliamentLon, lonRef: "East",
                radius: 500, autoAssign: false),
            Fav(name: "NoArea", lat: ParliamentLat, latRef: "North", lon: ParliamentLon, lonRef: "East",
                radius: 0)
        ];

        Assert.That(
            actual: FavouriteMatcher.FindAutoAssignMatch(favourites: favourites, latitude: 47.5071,
                longitude: 19.0456), expression: Is.Null);
    }

    [Test]
    public void Match_PrefersTheNearestCentre_AndBreaksTiesByName()
    {
        List<Favourite> favourites =
        [
            Fav(name: "Wide", lat: "47.5071", latRef: "North", lon: "19.0456", lonRef: "East", radius: 5000),
            Fav(name: "Close", lat: "47.5075", latRef: "North", lon: "19.0456", lonRef: "East", radius: 5000)
        ];

        Assert.That(
            actual: FavouriteMatcher.FindAutoAssignMatch(favourites: favourites, latitude: 47.5076,
                longitude: 19.0456)?.FavouriteName, expression: Is.EqualTo(expected: "Close"));

        // the same centre: the name that sorts first wins, whichever order they are listed in
        List<Favourite> tied =
        [
            Fav(name: "b", lat: "47.5", latRef: "North", lon: "19", lonRef: "East", radius: 100),
            Fav(name: "A", lat: "47.5", latRef: "North", lon: "19", lonRef: "East", radius: 100)
        ];
        Assert.That(
            actual: FavouriteMatcher.FindAutoAssignMatch(favourites: tied, latitude: 47.5, longitude: 19)
                                    ?.FavouriteName, expression: Is.EqualTo(expected: "A"));
        Assert.That(
            actual: FavouriteMatcher.FindAutoAssignMatch(favourites: tied.AsEnumerable()
                                                                         .Reverse(), latitude: 47.5, longitude: 19)
                                    ?.FavouriteName, expression: Is.EqualTo(expected: "A"));
    }

    [Test]
    public void Match_HandlesSouthernAndWesternFavourites()
    {
        List<Favourite> favourites =
        [
            Fav(name: "Sydney", lat: "33.8568", latRef: "South", lon: "151.2153", lonRef: "East", radius: 500)
        ];

        Assert.That(
            actual: FavouriteMatcher.FindAutoAssignMatch(favourites: favourites, latitude: -33.8568,
                longitude: 151.2153)?.FavouriteName, expression: Is.EqualTo(expected: "Sydney"));
        // the mirror image across the equator is a long way off
        Assert.That(
            actual: FavouriteMatcher.FindAutoAssignMatch(favourites: favourites, latitude: 33.8568,
                longitude: 151.2153), expression: Is.Null);
    }

    [Test]
    public void Match_IgnoresAFavouriteWithAnUnreadablePosition()
    {
        List<Favourite> favourites =
        [
            Fav(name: "Broken", lat: "", latRef: "", lon: "", lonRef: "", radius: 1000000)
        ];

        Assert.That(
            actual: FavouriteMatcher.FindAutoAssignMatch(favourites: favourites, latitude: 0, longitude: 0),
            expression: Is.Null);
    }
}

/// <summary>
///     The tree grouping, the stored keyword text and name de-duplication behind the Manage Favourites dialog.
/// </summary>
[TestFixture]
public class FavouriteGroupingTests
{
    private static Favourite Place(string country = null,
                                   string state = null,
                                   string city = null)
    {
        return new Favourite
        {
            FavouriteName = "x", CountryCode = "HUN", Country = country, State = state, City = city,
            Coordinates = "47.5;19.0"
        };
    }

    [Test]
    public void Path_NestsInFieldOrder_WhateverOrderTheKeysWereTickedIn()
    {
        List<string> path = FavouriteGrouping.PathFor(favourite: Place(country: "Hungary", state: "Pest", city: "Budapest"),
            keys: [FavouriteGroupKey.City, FavouriteGroupKey.Country, FavouriteGroupKey.State], blankLabel: "(none)");

        Assert.That(actual: path, expression: Is.EqualTo(expected: new[] { "Hungary", "Pest", "Budapest" }));
    }

    [Test]
    public void Path_PutsAMissingValueUnderTheBlankLabel_SoNothingVanishesFromTheTree()
    {
        List<string> path = FavouriteGrouping.PathFor(favourite: Place(country: "Hungary", city: "  "),
            keys: FavouriteGrouping.DefaultKeys, blankLabel: "(none)");

        Assert.That(actual: path, expression: Is.EqualTo(expected: new[] { "Hungary", "(none)", "(none)" }));
    }

    [Test]
    public void Path_IsEmptyWithNoGroupings()
    {
        Assert.That(
            actual: FavouriteGrouping.PathFor(favourite: Place(country: "Hungary"), keys: [], blankLabel: "(none)"),
            expression: Is.Empty);
    }

    [Test]
    public void UniqueName_KeepsAFreeName_AndNumbersATakenOne()
    {
        Assert.That(actual: FavouriteGrouping.UniqueName(baseName: "Home", existingNames: ["Work"]),
            expression: Is.EqualTo(expected: "Home"));
        Assert.That(actual: FavouriteGrouping.UniqueName(baseName: "Home", existingNames: ["home", "Work"]),
            expression: Is.EqualTo(expected: "Home (2)"));
        Assert.That(
            actual: FavouriteGrouping.UniqueName(baseName: "Home", existingNames: ["Home", "Home (2)"]),
            expression: Is.EqualTo(expected: "Home (3)"));
    }

    [Test]
    public void Keywords_RoundTripThroughTheStoredText_DroppingBlanksAndRepeats()
    {
        string stored = FavouriteKeywords.Join(keywords: ["holiday", " beach, sun ", "", "Holiday", null]);

        Assert.That(actual: stored, expression: Is.EqualTo(expected: "holiday\nbeach, sun"));
        Assert.That(actual: FavouriteKeywords.Split(stored: stored),
            expression: Is.EqualTo(expected: new[] { "holiday", "beach, sun" }));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  \r\n ")]
    public void Keywords_BlankStoredTextIsNoKeywords(string stored)
    {
        Assert.That(actual: FavouriteKeywords.Split(stored: stored), expression: Is.Empty);
    }
}
