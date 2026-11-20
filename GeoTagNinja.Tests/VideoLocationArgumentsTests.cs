using GeoTagNinja.Helpers;
using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.Model;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using static GeoTagNinja.Model.DirectoryElement;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Tests;

/// <summary>
///     The ExifTool arguments that put a location into a video. The tag names and the "lat, lon[, alt]" layout were
///     checked against real ExifTool output on a Samsung S25 clip and synthetic MP4/MOV/M4V/3GP clips: both
///     <c>Keys:GPSCoordinates</c> and <c>UserData:GPSCoordinates</c> are written, because players differ in which they
///     read. The text is read by players, not people, so it is invariant whatever the culture.
/// </summary>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class VideoLocationArgumentsTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public VideoLocationArgumentsTests(string cultureName)
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

    private static DirectoryElement NewVideo()
    {
        return new DirectoryElement(
            itemNameWithoutPath: "clip.mp4",
            type: ElementType.File,
            fileNameWithPath: @"C:\videos\clip.mp4");
    }

    private static void Set(DirectoryElement element,
                            ElementAttribute attribute,
                            IConvertible value,
                            AttributeVersion version,
                            bool deleted = false)
    {
        element.SetAttributeValue(attribute: attribute, value: value, version: version, isMarkedForDeletion: deleted);
    }

    [TestCase(47.4979, 19.0402, 120.0, "47.4979, 19.0402, 120")]
    [TestCase(-33.8688, 151.2093, 58.5, "-33.8688, 151.2093, 58.5")]
    [TestCase(51.5131, -0.0006, null, "51.5131, -0.0006")]
    [TestCase(47.49791234567, 19.04, null, "47.497912, 19.04")]
    [TestCase(0.0, 0.0, 0.0, "0, 0, 0")]
    public void Coordinates_AreInvariantWithADotWhateverTheCulture(double latitude,
                                                                   double longitude,
                                                                   double? altitude,
                                                                   string expected)
    {
        Assert.That(actual: VideoLocationArguments.Coordinates(latitude: latitude, longitude: longitude, altitudeMetres: altitude),
            expression: Is.EqualTo(expected: expected), message: $"in {_culture.Name}");
    }

    [Test]
    public void SettingALocation_WritesBothAtomsWithTheSameText()
    {
        var lines = VideoLocationArguments.Lines(removeAll: false, latitude: 47.4979, longitude: 19.0402, altitudeMetres: 120);

        Assert.That(actual: lines, expression: Is.EqualTo(expected: new[]
        {
            "-Keys:GPSCoordinates=47.4979, 19.0402, 120",
            "-UserData:GPSCoordinates=47.4979, 19.0402, 120"
        }), message: $"in {_culture.Name}");
    }

    [Test]
    public void ALocationWithoutBothCoordinates_WritesNothing()
    {
        Assert.That(actual: VideoLocationArguments.Lines(removeAll: false, latitude: 47.5, longitude: null, altitudeMetres: null), expression: Is.Empty);
        Assert.That(actual: VideoLocationArguments.Lines(removeAll: false, latitude: null, longitude: 19.0, altitudeMetres: 10), expression: Is.Empty);
    }

    [Test]
    public void Removal_ClearsEveryPlaceTheLocationCanLive()
    {
        var lines = VideoLocationArguments.Lines(removeAll: true, latitude: 47.5, longitude: 19.0, altitudeMetres: 10);

        Assert.That(actual: lines, expression: Is.EquivalentTo(expected: new[]
        {
            "-gps*=", "-xmp:gps*=", "-Keys:GPSCoordinates=", "-UserData:GPSCoordinates="
        }));
    }

    [Test]
    public void StagedLatLon_IsPlanned()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.GPSLatitude, value: 47.4979, version: AttributeVersion.Stage3ReadyToWrite);
        Set(element: video, attribute: ElementAttribute.GPSLongitude, value: 19.0402, version: AttributeVersion.Stage3ReadyToWrite);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: false);

        Assert.That(actual: plan.HasLocationEdit, expression: Is.True);
        Assert.That(actual: plan.HasOtherEdits, expression: Is.False);
        Assert.That(actual: plan.Lines.First(), expression: Is.EqualTo(expected: "-Keys:GPSCoordinates=47.4979, 19.0402"), message: $"in {_culture.Name}");
    }

    /// <summary>Editing only the altitude must not lose the position the file already has.</summary>
    [Test]
    public void StagingOnlyTheAltitude_KeepsThePositionFromTheFile()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.GPSLatitude, value: 51.5131, version: AttributeVersion.Original);
        Set(element: video, attribute: ElementAttribute.GPSLongitude, value: -0.0006, version: AttributeVersion.Original);
        Set(element: video, attribute: ElementAttribute.GPSAltitude, value: 35.5, version: AttributeVersion.Stage3ReadyToWrite);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: false);

        Assert.That(actual: plan.Lines.First(), expression: Is.EqualTo(expected: "-Keys:GPSCoordinates=51.5131, -0.0006, 35.5"), message: $"in {_culture.Name}");
    }

    /// <summary>The model holds the altitude in the user's unit; the file wants metres.</summary>
    [Test]
    public void ImperialAltitude_IsWrittenInMetres()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.GPSLatitude, value: 47.5, version: AttributeVersion.Stage3ReadyToWrite);
        Set(element: video, attribute: ElementAttribute.GPSLongitude, value: 19.0, version: AttributeVersion.Stage3ReadyToWrite);
        Set(element: video, attribute: ElementAttribute.GPSAltitude, value: 100.0, version: AttributeVersion.Stage3ReadyToWrite);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: true);

        double expectedMetres = Math.Round(value: 100.0 / HelperVariables.MetreToFeet, digits: 2);
        Assert.That(actual: plan.Lines.First(),
            expression: Is.EqualTo(expected: $"-Keys:GPSCoordinates=47.5, 19, {expectedMetres.ToString(provider: CultureInfo.InvariantCulture)}"),
            message: $"in {_culture.Name}");
    }

    /// <summary>A removed altitude must not come back from the file's older value.</summary>
    [Test]
    public void RemovedAltitude_IsLeftOut()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.GPSLatitude, value: 47.5, version: AttributeVersion.Original);
        Set(element: video, attribute: ElementAttribute.GPSLongitude, value: 19.0, version: AttributeVersion.Original);
        Set(element: video, attribute: ElementAttribute.GPSAltitude, value: 120.0, version: AttributeVersion.Original);
        Set(element: video, attribute: ElementAttribute.GPSAltitude, value: 0.0, version: AttributeVersion.Stage3ReadyToWrite, deleted: true);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: false);

        Assert.That(actual: plan.Lines.First(), expression: Is.EqualTo(expected: "-Keys:GPSCoordinates=47.5, 19"), message: $"in {_culture.Name}");
    }

    [Test]
    public void RemovingTheLatitude_RemovesTheWholeLocation()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.GPSLatitude, value: 47.5, version: AttributeVersion.Original);
        Set(element: video, attribute: ElementAttribute.GPSLatitude, value: 0.0, version: AttributeVersion.Stage3ReadyToWrite, deleted: true);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: false);

        Assert.That(actual: plan.Lines, expression: Does.Contain(expected: "-UserData:GPSCoordinates="));
        Assert.That(actual: plan.Lines.Any(predicate: line => line.Contains(value: "=47.")), expression: Is.False);
    }

    [Test]
    public void RemoveAllGps_RemovesTheWholeLocation()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.RemoveAllGPS, value: "", version: AttributeVersion.Stage3ReadyToWrite, deleted: true);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: false);

        Assert.That(actual: plan.Lines, expression: Does.Contain(expected: "-Keys:GPSCoordinates="));
        Assert.That(actual: plan.Lines, expression: Does.Contain(expected: "-UserData:GPSCoordinates="));
    }

    /// <summary>Only the location is written; a date (or anything else) staged on a video is reported, not written.</summary>
    [Test]
    public void OtherStagedEdits_AreFlaggedNotWritten()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.TakenDate, value: new DateTime(year: 2024, month: 6, day: 22, hour: 19, minute: 32, second: 53), version: AttributeVersion.Stage3ReadyToWrite);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: false);

        Assert.That(actual: plan.HasLocationEdit, expression: Is.False);
        Assert.That(actual: plan.HasOtherEdits, expression: Is.True);
    }

    [Test]
    public void LocationAndOtherEdits_WriteTheLocationAndFlagTheRest()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.GPSLatitude, value: 47.5, version: AttributeVersion.Stage3ReadyToWrite);
        Set(element: video, attribute: ElementAttribute.GPSLongitude, value: 19.0, version: AttributeVersion.Stage3ReadyToWrite);
        Set(element: video, attribute: ElementAttribute.TakenDate, value: new DateTime(year: 2024, month: 6, day: 22), version: AttributeVersion.Stage3ReadyToWrite);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: false);

        Assert.That(actual: plan.HasLocationEdit, expression: Is.True);
        Assert.That(actual: plan.HasOtherEdits, expression: Is.True);
    }

    [Test]
    public void NothingStaged_PlansNothing()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.GPSLatitude, value: 47.5, version: AttributeVersion.Original);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: false);

        Assert.That(actual: plan.HasLocationEdit, expression: Is.False);
        Assert.That(actual: plan.HasOtherEdits, expression: Is.False);
    }

    /// <summary>These carry an XMP tag, so they can be written to a video (checked with a real MP4).</summary>
    [TestCase(ElementAttribute.Caption)]
    [TestCase(ElementAttribute.ObjectName)]
    [TestCase(ElementAttribute.Headline)]
    [TestCase(ElementAttribute.Copyright)]
    [TestCase(ElementAttribute.Byline)]
    [TestCase(ElementAttribute.City)]
    [TestCase(ElementAttribute.State)]
    [TestCase(ElementAttribute.Country)]
    [TestCase(ElementAttribute.CountryCode)]
    [TestCase(ElementAttribute.Sublocation)]
    [TestCase(ElementAttribute.Rating)]
    [TestCase(ElementAttribute.IPTCKeywords)]
    [TestCase(ElementAttribute.XMLSubjects)]
    [TestCase(ElementAttribute.CreatorEmail)]
    public void AttributesWithAnXmpTag_CanBeWrittenToAVideo(ElementAttribute attribute)
    {
        Assert.That(actual: VideoLocationArguments.IsXmpWritable(attribute: attribute), expression: Is.True);
    }

    /// <summary>
    ///     The location has its own writer, the dates are left for later, and the rest have no XMP tag at all (a
    ///     video would silently drop what they write).
    /// </summary>
    [TestCase(ElementAttribute.GPSLatitude)]
    [TestCase(ElementAttribute.GPSLongitude)]
    [TestCase(ElementAttribute.GPSAltitude)]
    [TestCase(ElementAttribute.RemoveAllGPS)]
    [TestCase(ElementAttribute.TakenDate)]
    [TestCase(ElementAttribute.CreateDate)]
    [TestCase(ElementAttribute.GPSSpeed)]
    [TestCase(ElementAttribute.Make)]
    public void LocationDatesAndAttributesWithoutAnXmpTag_AreNotWrittenAsXmp(ElementAttribute attribute)
    {
        Assert.That(actual: VideoLocationArguments.IsXmpWritable(attribute: attribute), expression: Is.False);
    }

    [Test]
    public void XmpTagsOnly_DropsTheTagsAVideoCannotHold()
    {
        var kept = VideoLocationArguments.XmpTagsOnly(tags: ["IPTC:Keywords", "XMP-dc:Subject", "EXIF:Copyright", "xmp-dc:Rights", "City"]);

        Assert.That(actual: kept, expression: Is.EqualTo(expected: new[] { "XMP-dc:Subject", "xmp-dc:Rights" }));
        Assert.That(actual: VideoLocationArguments.XmpTagsOnly(tags: null), expression: Is.Empty);
    }

    [Test]
    public void StagedCaption_IsPlannedAsAnXmpEdit()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.Caption, value: "A walk by the river", version: AttributeVersion.Stage3ReadyToWrite);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: false);

        Assert.That(actual: plan.HasXmpEdit, expression: Is.True);
        Assert.That(actual: plan.HasWritableEdit, expression: Is.True);
        Assert.That(actual: plan.HasLocationEdit, expression: Is.False);
        Assert.That(actual: plan.HasOtherEdits, expression: Is.False);
    }

    [Test]
    public void StagedSpeed_HasNowhereToGoOnAVideo()
    {
        DirectoryElement video = NewVideo();
        Set(element: video, attribute: ElementAttribute.GPSSpeed, value: 12.5, version: AttributeVersion.Stage3ReadyToWrite);

        VideoLocationArguments.Plan plan = VideoLocationArguments.PlanFor(element: video, useImperial: false);

        Assert.That(actual: plan.HasWritableEdit, expression: Is.False);
        Assert.That(actual: plan.HasOtherEdits, expression: Is.True);
    }
}