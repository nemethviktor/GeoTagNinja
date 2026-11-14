using GeoTagNinja.Helpers.Exif;
using NUnit.Framework;
using SkiaSharp;
using System.IO;

namespace GeoTagNinja.Tests;

/// <summary>
///     The orientation maths behind thumbnails and previews. Wrong here means every portrait photo sideways, so it is
///     pinned pixel by pixel. Not locale-dependent, hence no culture matrix.
/// </summary>
[TestFixture]
public class ImageOpsTests
{
    private static readonly SKColor Red = new(red: 255, green: 0, blue: 0);
    private static readonly SKColor Blue = new(red: 0, green: 0, blue: 255);

    /// <summary>A 2x1 picture: red on the left, blue on the right.</summary>
    private static SKBitmap TwoPixels()
    {
        SKBitmap bitmap = new(width: 2, height: 1);
        bitmap.SetPixel(x: 0, y: 0, color: Red);
        bitmap.SetPixel(x: 1, y: 0, color: Blue);
        return bitmap;
    }

    // Where red (source's left pixel) and blue end up, as (x, y) of the oriented picture.
    [TestCase(1, 2, 1, 0, 0, 1, 0)]
    [TestCase(2, 2, 1, 1, 0, 0, 0)] // mirrored left-right
    [TestCase(3, 2, 1, 1, 0, 0, 0)] // upside down: a 1-row picture looks mirrored
    [TestCase(4, 2, 1, 0, 0, 1, 0)] // mirrored top-bottom: a 1-row picture is unchanged
    [TestCase(5, 1, 2, 0, 0, 0, 1)] // transpose
    [TestCase(6, 1, 2, 0, 0, 0, 1)] // 90 degrees clockwise: left pixel ends up on top
    [TestCase(7, 1, 2, 0, 1, 0, 0)] // transverse
    [TestCase(8, 1, 2, 0, 1, 0, 0)] // 90 degrees anticlockwise: left pixel ends up at the bottom
    public void ApplyOrientation_PutsPixelsWhereExifSays(int orientation,
                                                         int expectedWidth,
                                                         int expectedHeight,
                                                         int redX,
                                                         int redY,
                                                         int blueX,
                                                         int blueY)
    {
        using SKBitmap source = TwoPixels();
        using SKBitmap result = ImageOps.ApplyOrientation(bitmap: source, exifOrientation: orientation);

        Assert.That(actual: result.Width, expression: Is.EqualTo(expected: expectedWidth));
        Assert.That(actual: result.Height, expression: Is.EqualTo(expected: expectedHeight));
        Assert.That(actual: result.GetPixel(x: redX, y: redY), expression: Is.EqualTo(expected: Red));
        Assert.That(actual: result.GetPixel(x: blueX, y: blueY), expression: Is.EqualTo(expected: Blue));
    }

    [Test]
    public void ApplyOrientation_AsStoredReturnsTheSameBitmap()
    {
        using SKBitmap source = TwoPixels();
        Assert.That(actual: ImageOps.ApplyOrientation(bitmap: source, exifOrientation: 1),
            expression: Is.SameAs(expected: source));
        Assert.That(actual: ImageOps.ApplyOrientation(bitmap: source, exifOrientation: 0),
            expression: Is.SameAs(expected: source));
    }

    [TestCase(400, 200, 128, 128, 64)]
    [TestCase(200, 400, 128, 64, 128)]
    [TestCase(100, 50, 128, 100, 50)] // never enlarged
    public void FitWithin_KeepsAspectAndNeverEnlarges(int width,
                                                      int height,
                                                      int maxEdge,
                                                      int expectedWidth,
                                                      int expectedHeight)
    {
        using SKBitmap source = new(width: width, height: height);
        using SKBitmap result = ImageOps.FitWithin(source: source, maxEdge: maxEdge);

        Assert.That(actual: result.Width, expression: Is.EqualTo(expected: expectedWidth));
        Assert.That(actual: result.Height, expression: Is.EqualTo(expected: expectedHeight));
    }

    [Test]
    public void CreateJpeg_MakesAThumbnailOfThePlainJpegDecode()
    {
        string path = Path.Combine(path1: Path.GetTempPath(), path2: $"gtn_test_{System.Guid.NewGuid():N}.jpg");
        try
        {
            using (SKBitmap source = new(width: 400, height: 200))
            {
                File.WriteAllBytes(path: path, bytes: ImageOps.EncodeJpeg(bitmap: source, quality: 90));
            }

            byte[] jpeg = ThumbnailGenerator.CreateJpeg(fileNameWithPath: path, size: 128);

            Assert.That(actual: jpeg, expression: Is.Not.Null);
            using SKBitmap thumbnail = SKBitmap.Decode(buffer: jpeg);
            Assert.That(actual: thumbnail.Width, expression: Is.EqualTo(expected: 128));
            Assert.That(actual: thumbnail.Height, expression: Is.EqualTo(expected: 64));
        }
        finally
        {
            File.Delete(path: path);
        }
    }

    [Test]
    public void DecodeOriented_UsesTheFallbackOnlyWhenTheFileHasNoOrientation()
    {
        byte[] jpeg;
        using (SKBitmap source = new(width: 40, height: 20))
        {
            jpeg = ImageOps.EncodeJpeg(bitmap: source, quality: 90);
        }

        using SKBitmap upright = ImageOps.DecodeOriented(data: jpeg, fallbackExifOrientation: 6);
        Assert.That(actual: upright.Width, expression: Is.EqualTo(expected: 20));
        Assert.That(actual: upright.Height, expression: Is.EqualTo(expected: 40));
    }
}
