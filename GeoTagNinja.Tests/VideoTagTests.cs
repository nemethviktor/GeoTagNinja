using GeoTagNinja.Helpers;
using GeoTagNinja.Helpers.FileSystem;
using GeoTagNinja.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Tests;

/// <summary>
///     Video files (MP4, MOV, ...) keep their location and dates in QuickTime atoms, not in EXIF. ExifTool exposes the
///     location as <c>Composite:GPSLatitude</c> and friends, and the dates as <c>QuickTime:CreateDate</c> (UTC) and
///     <c>QuickTime:CreationDate</c> (the local wall-clock time, from the <c>Keys</c> atom). The tag lists and values
///     here come from real ExifTool output for a Samsung S25 clip and synthetic clips (the application runs ExifTool
///     with <c>-G</c>, so the group is the family-0 name, "QuickTime", not "Keys").
/// </summary>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class VideoTagTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;
    private bool _originalVideoSetting;

    public VideoTagTests(string cultureName)
    {
        _culture = new CultureInfo(name: cultureName);
    }

    [SetUp]
    public void SetUp()
    {
        _originalCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = _culture;
        _originalVideoSetting = HelperVariables.UserSettingEnableVideoFiles;
    }

    [TearDown]
    public void TearDown()
    {
        Thread.CurrentThread.CurrentCulture = _originalCulture;
        HelperVariables.UserSettingEnableVideoFiles = _originalVideoSetting;
    }

    /// <summary>A Samsung Galaxy S25 clip: a location with no altitude, a UTC create date, no creation date.</summary>
    [Test]
    public void SamsungClip_ReadsLocationAndCreateDate()
    {
        IDictionary<ElementAttribute, IConvertible> parsed = ExifTagSetParser.ParseTagSet(
            dictTagsIn: new Dictionary<string, string>
            {
                { "Composite:GPSLatitude", "+51.513100" },
                { "Composite:GPSLongitude", "-0.000600" },
                { "QuickTime:CreateDate", "2026-11-20 12:39:36" }
            });

        Assert.That(actual: (double)parsed[key: ElementAttribute.GPSLatitude],
            expression: Is.EqualTo(expected: 51.5131).Within(amount: 0.000001), message: $"in {_culture.Name}");
        Assert.That(actual: (double)parsed[key: ElementAttribute.GPSLongitude],
            expression: Is.EqualTo(expected: -0.0006).Within(amount: 0.000001), message: $"in {_culture.Name}");
        Assert.That(actual: parsed[key: ElementAttribute.CreateDate],
            expression: Is.EqualTo(expected: new DateTime(year: 2026, month: 11, day: 20, hour: 12, minute: 39, second: 36)),
            message: $"in {_culture.Name}");
        Assert.That(actual: parsed.ContainsKey(key: ElementAttribute.GPSAltitude), expression: Is.False);
        Assert.That(actual: parsed.ContainsKey(key: ElementAttribute.TakenDate), expression: Is.False);
    }

    /// <summary>
    ///     An iPhone-style clip: a location with altitude, and a creation date that is the local wall-clock time (the
    ///     offset is dropped by ExifTool's output format, and by the parser if it were present).
    /// </summary>
    [Test]
    public void IPhoneStyleClip_ReadsAltitudeAndLocalCreationDate()
    {
        IDictionary<ElementAttribute, IConvertible> parsed = ExifTagSetParser.ParseTagSet(
            dictTagsIn: new Dictionary<string, string>
            {
                { "Composite:GPSLatitude", "+47.497900" },
                { "Composite:GPSLongitude", "+19.040200" },
                { "Composite:GPSAltitude", "120 m" },
                { "Composite:GPSAltitudeRef", "Above Sea Level" },
                { "QuickTime:CreationDate", "2024-06-22 19:32:53" },
                { "QuickTime:CreateDate", "2024-06-22 17:32:53" }
            });

        Assert.That(actual: (double)parsed[key: ElementAttribute.GPSAltitude],
            expression: Is.EqualTo(expected: 120d).Within(amount: 0.0001), message: $"in {_culture.Name}");
        Assert.That(actual: parsed[key: ElementAttribute.TakenDate],
            expression: Is.EqualTo(expected: new DateTime(year: 2024, month: 6, day: 22, hour: 19, minute: 32, second: 53)),
            message: $"in {_culture.Name}");
        Assert.That(actual: parsed[key: ElementAttribute.CreateDate],
            expression: Is.EqualTo(expected: new DateTime(year: 2024, month: 6, day: 22, hour: 17, minute: 32, second: 53)),
            message: $"in {_culture.Name}");
    }

    /// <summary>A clip with no creation time is read as <c>0000:00:00 00:00:00</c> - that is "blank", not a date.</summary>
    [Test]
    public void ZeroCreateDate_IsBlankNotYearOne()
    {
        IDictionary<ElementAttribute, IConvertible> parsed = ExifTagSetParser.ParseTagSet(
            dictTagsIn: new Dictionary<string, string>
            {
                { "QuickTime:CreateDate", "0000:00:00 00:00:00" }
            });

        Assert.That(actual: parsed.ContainsKey(key: ElementAttribute.CreateDate), expression: Is.False,
            message: $"in {_culture.Name}");
    }

    /// <summary>A photo's EXIF tags still win over the video fallbacks added after them.</summary>
    [Test]
    public void PhotoExifTags_TakePrecedenceOverCompositeFallback()
    {
        IDictionary<ElementAttribute, IConvertible> parsed = ExifTagSetParser.ParseTagSet(
            dictTagsIn: new Dictionary<string, string>
            {
                { "EXIF:GPSLatitude", "47.497913" },
                { "EXIF:GPSLatitudeRef", "North" },
                { "Composite:GPSLatitude", "10.000000" },
                { "EXIF:CreateDate", "2018-06-22 19:32:53" },
                { "QuickTime:CreateDate", "2001-01-01 00:00:00" }
            });

        Assert.That(actual: (double)parsed[key: ElementAttribute.GPSLatitude],
            expression: Is.EqualTo(expected: 47.497913).Within(amount: 0.0001), message: $"in {_culture.Name}");
        Assert.That(actual: ((DateTime)parsed[key: ElementAttribute.CreateDate]).Year, expression: Is.EqualTo(expected: 2018));
    }

    /// <summary>The ExifTool tag arguments must request the QuickTime tags, or video would never report them.</summary>
    [TestCase("Composite:GPSLatitude")]
    [TestCase("Composite:GPSLongitude")]
    [TestCase("Composite:GPSAltitude")]
    [TestCase("QuickTime:CreateDate")]
    [TestCase("QuickTime:CreationDate")]
    public void RequiredInTags_IncludeTheVideoTags(string tag)
    {
        Assert.That(actual: GetAllRequiredInAttributes(), expression: Does.Contain(expected: tag));
    }

    [TestCase("clip.mp4", true)]
    [TestCase(@"C:\Videos\IMG_0001.MOV", true)]
    [TestCase("mp4", true)]
    [TestCase(".3gp", true)]
    [TestCase("GOPR0001.LRV", true)]
    [TestCase("holiday.jpg", false)]
    [TestCase("shot.heic", false)]
    [TestCase("raw.dng", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void IsVideoFile_RecognisesVideoExtensions(string name,
                                                      bool expected)
    {
        Assert.That(actual: SupportedFileExtensions.IsVideoFile(fileNameOrExtension: name), expression: Is.EqualTo(expected: expected));
    }

    /// <summary>
    ///     The per-extension defaults (sidecar, "process original") are derived from the words "raw" and "tiff" in the
    ///     description. A video description that happened to contain either would silently get RAW defaults.
    /// </summary>
    [Test]
    public void VideoDescriptions_DoNotLookLikeRawOrTiff()
    {
        foreach (string entry in SupportedFileExtensions.AllCompatibleExtensions()
                                                        .Where(predicate: e => SupportedFileExtensions.IsVideoFile(fileNameOrExtension: e.Split('\t')[0])))
        {
            string description = entry.Split('\t').Last().ToLowerInvariant();
            Assert.That(actual: description, expression: Does.Not.Contain(expected: "raw").And.Not.Contain(expected: "tiff"),
                message: entry);
        }
    }

    [Test]
    public void FolderScan_SeesVideoOnlyWhenTheSettingIsOn()
    {
        HelperVariables.UserSettingEnableVideoFiles = false;
        Assert.That(actual: SupportedFileExtensions.AllCompatibleExtensionsExt(), expression: Does.Not.Contain(expected: "mp4"));
        Assert.That(actual: SupportedFileExtensions.AllCompatibleExtensionsExt(), expression: Does.Contain(expected: "jpg"));

        HelperVariables.UserSettingEnableVideoFiles = true;
        Assert.That(actual: SupportedFileExtensions.AllCompatibleExtensionsExt(), expression: Does.Contain(expected: "mp4"));
        Assert.That(actual: SupportedFileExtensions.AllCompatibleExtensionsExt(), expression: Does.Contain(expected: "mov"));
    }
}
