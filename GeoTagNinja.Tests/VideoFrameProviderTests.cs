using GeoTagNinja.Helpers.Exif;
using NUnit.Framework;
using System;
using System.IO;

namespace GeoTagNinja.Tests;

/// <summary>
///     Which ffmpeg the Settings path points at. Only the path logic is covered: no ffmpeg is run, so a real one does
///     not need to be installed to run the tests.
/// </summary>
[TestFixture]
public class VideoFrameProviderTests
{
    private string _folder;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(path1: Path.GetTempPath(), path2: $"gtn_ffmpeg_{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(path: _folder);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(path: _folder, recursive: true);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void BlankPath_ResolvesToNothing(string configured)
    {
        Assert.That(actual: VideoFrameProvider.ResolveFfmpegPath(configured: configured), expression: Is.Null);
    }

    [Test]
    public void MissingFileOrFolder_ResolvesToNothing()
    {
        Assert.That(actual: VideoFrameProvider.ResolveFfmpegPath(configured: Path.Combine(path1: _folder, path2: "nothing.exe")), expression: Is.Null);
        Assert.That(actual: VideoFrameProvider.ResolveFfmpegPath(configured: Path.Combine(path1: _folder, path2: "nofolder")), expression: Is.Null);
    }

    [Test]
    public void ExistingFile_ResolvesToItself_EvenWhenQuotedOrPadded()
    {
        string file = Path.Combine(path1: _folder, path2: "ffmpeg.exe");
        File.WriteAllText(path: file, contents: "");

        Assert.That(actual: VideoFrameProvider.ResolveFfmpegPath(configured: file), expression: Is.EqualTo(expected: file));
        Assert.That(actual: VideoFrameProvider.ResolveFfmpegPath(configured: $"  \"{file}\"  "), expression: Is.EqualTo(expected: file));
    }

    /// <summary>People tend to paste the <c>bin</c> folder rather than the executable.</summary>
    [Test]
    public void FolderHoldingFfmpeg_ResolvesToTheExecutable()
    {
        string file = Path.Combine(path1: _folder, path2: "ffmpeg.exe");
        File.WriteAllText(path: file, contents: "");

        Assert.That(actual: VideoFrameProvider.ResolveFfmpegPath(configured: _folder), expression: Is.EqualTo(expected: file));
    }

    [Test]
    public void FolderWithoutFfmpeg_ResolvesToNothing()
    {
        File.WriteAllText(path: Path.Combine(path1: _folder, path2: "other.exe"), contents: "");

        Assert.That(actual: VideoFrameProvider.ResolveFfmpegPath(configured: _folder), expression: Is.Null);
    }

    [Test]
    public void IllegalCharacters_ResolveToNothingInsteadOfThrowing()
    {
        Assert.That(actual: VideoFrameProvider.ResolveFfmpegPath(configured: "C:\\bad|path<>\\ffmpeg.exe"), expression: Is.Null);
    }

    /// <summary>A file that exists but is not ffmpeg must not be taken for one.</summary>
    [Test]
    public void FileThatIsNotFfmpeg_IsNotValid()
    {
        string file = Path.Combine(path1: _folder, path2: "ffmpeg.exe");
        File.WriteAllText(path: file, contents: "not a program");

        Assert.That(actual: VideoFrameProvider.IsValidFfmpeg(configured: file), expression: Is.False);
        Assert.That(actual: VideoFrameProvider.IsValidFfmpeg(configured: ""), expression: Is.False);
        Assert.That(actual: VideoFrameProvider.IsValidFfmpeg(configured: null), expression: Is.False);
    }

    /// <summary>With no usable ffmpeg and no platform thumbnail, no frame is made (the caller shows its own symbol).</summary>
    [Test]
    public void NoFfmpegAndNoPlatformProvider_GivesNoFrame()
    {
        Func<string, int, SkiaSharp.SKBitmap> original = VideoFrameProvider.PlatformFrameProvider;
        string originalPath = GeoTagNinja.Helpers.HelperVariables.UserSettingFfmpegPath;
        try
        {
            VideoFrameProvider.PlatformFrameProvider = null;
            GeoTagNinja.Helpers.HelperVariables.UserSettingFfmpegPath = "";

            Assert.That(actual: VideoFrameProvider.TryCreate(videoPath: Path.Combine(path1: _folder, path2: "clip.mp4"), size: 128), expression: Is.Null);
        }
        finally
        {
            VideoFrameProvider.PlatformFrameProvider = original;
            GeoTagNinja.Helpers.HelperVariables.UserSettingFfmpegPath = originalPath;
        }
    }

    /// <summary>An invalid ffmpeg path falls through to the platform's thumbnail.</summary>
    [Test]
    public void InvalidFfmpeg_FallsBackToThePlatformProvider()
    {
        Func<string, int, SkiaSharp.SKBitmap> original = VideoFrameProvider.PlatformFrameProvider;
        string originalPath = GeoTagNinja.Helpers.HelperVariables.UserSettingFfmpegPath;
        int calls = 0;
        try
        {
            VideoFrameProvider.PlatformFrameProvider = (path,
                                                        size) =>
            {
                calls++;
                return new SkiaSharp.SKBitmap(width: 4, height: 4);
            };
            GeoTagNinja.Helpers.HelperVariables.UserSettingFfmpegPath = Path.Combine(path1: _folder, path2: "no_ffmpeg_here.exe");

            using SkiaSharp.SKBitmap frame = VideoFrameProvider.TryCreate(videoPath: "clip.mp4", size: 128);

            Assert.That(actual: frame, expression: Is.Not.Null);
            Assert.That(actual: calls, expression: Is.EqualTo(expected: 1));
        }
        finally
        {
            VideoFrameProvider.PlatformFrameProvider = original;
            GeoTagNinja.Helpers.HelperVariables.UserSettingFfmpegPath = originalPath;
        }
    }
}
