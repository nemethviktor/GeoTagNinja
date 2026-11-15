using SkiaSharp;
using System;
using System.IO;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     The small set of picture operations Core needs, done with SkiaSharp so that neither Core nor Avalonia depend on
///     System.Drawing. Orientation values are the EXIF ones (1 = as stored, 3 = upside down, 6 = needs 90° clockwise,
///     8 = needs 90° anticlockwise, and the mirrored 2, 4, 5, 7).
/// </summary>
internal static class ImageOps
{
    /// <summary>
    ///     JPEG quality for thumbnails kept in the cache: small, and plenty for a 128 or 256 px picture.
    /// </summary>
    internal const int ThumbnailJpegQuality = 85;

    /// <summary>
    ///     Scales <paramref name="source" /> down so its long edge is <paramref name="maxEdge" />, keeping the aspect
    ///     ratio (an image already smaller is not enlarged). No square canvas or letterboxing: the thumbnail view
    ///     centres each image in its cell. The source is not disposed.
    /// </summary>
    internal static SKBitmap FitWithin(SKBitmap source,
                                       int maxEdge)
    {
        float scale = Math.Min(val1: 1f,
            val2: Math.Min(val1: maxEdge / (float)source.Width, val2: maxEdge / (float)source.Height));
        int width = Math.Max(val1: 1, val2: (int)Math.Round(a: source.Width * scale));
        int height = Math.Max(val1: 1, val2: (int)Math.Round(a: source.Height * scale));

        SKImageInfo info = new(width: width, height: height, colorType: SKColorType.Bgra8888,
            alphaType: SKAlphaType.Premul);
        SKBitmap resized = new(info: info);
        using SKPixmap sourcePixels = source.PeekPixels();
        using SKPixmap targetPixels = resized.PeekPixels();
        if (!sourcePixels.ScalePixels(destination: targetPixels,
                sampling: new SKSamplingOptions(resampler: SKCubicResampler.Mitchell)))
        {
            resized.Dispose();
            throw new InvalidOperationException(message: "Could not scale the image.");
        }

        return resized;
    }

    /// <summary>
    ///     Turns a picture the way its EXIF orientation says it should be shown. Returns <paramref name="bitmap" />
    ///     itself when no change is needed; otherwise a new bitmap (the source is left for the caller to dispose).
    /// </summary>
    internal static SKBitmap ApplyOrientation(SKBitmap bitmap,
                                              int exifOrientation)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // Where each source pixel (x, y) lands: x' = a*x + b*y + tx, y' = c*x + d*y + ty.
        (bool swap, SKMatrix matrix) = exifOrientation switch
        {
            2 => (false, new SKMatrix(scaleX: -1, skewX: 0, transX: width, skewY: 0, scaleY: 1, transY: 0, persp0: 0, persp1: 0, persp2: 1)),
            3 => (false, new SKMatrix(scaleX: -1, skewX: 0, transX: width, skewY: 0, scaleY: -1, transY: height, persp0: 0, persp1: 0, persp2: 1)),
            4 => (false, new SKMatrix(scaleX: 1, skewX: 0, transX: 0, skewY: 0, scaleY: -1, transY: height, persp0: 0, persp1: 0, persp2: 1)),
            5 => (true, new SKMatrix(scaleX: 0, skewX: 1, transX: 0, skewY: 1, scaleY: 0, transY: 0, persp0: 0, persp1: 0, persp2: 1)),
            6 => (true, new SKMatrix(scaleX: 0, skewX: -1, transX: height, skewY: 1, scaleY: 0, transY: 0, persp0: 0, persp1: 0, persp2: 1)),
            7 => (true, new SKMatrix(scaleX: 0, skewX: -1, transX: height, skewY: -1, scaleY: 0, transY: width, persp0: 0, persp1: 0, persp2: 1)),
            8 => (true, new SKMatrix(scaleX: 0, skewX: 1, transX: 0, skewY: -1, scaleY: 0, transY: width, persp0: 0, persp1: 0, persp2: 1)),
            _ => (false, SKMatrix.Identity)
        };

        if (matrix.IsIdentity)
        {
            return bitmap;
        }

        SKBitmap oriented = new(info: new SKImageInfo(width: swap ? height : width, height: swap ? width : height,
            colorType: SKColorType.Bgra8888, alphaType: SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap: oriented);
        canvas.SetMatrix(matrix: matrix);
        canvas.DrawBitmap(bitmap: bitmap, x: 0, y: 0);
        return oriented;
    }

    /// <summary>
    ///     Decodes picture bytes (JPEG, PNG, BMP, GIF, WebP...) and turns the result upright. The orientation the file
    ///     itself carries wins; <paramref name="fallbackExifOrientation" /> is used only when the file has none (a JPEG
    ///     cut out of a RAW usually has none, and the RAW knows it).
    /// </summary>
    /// <returns>The upright bitmap (caller owns it), or null when the bytes can't be decoded.</returns>
    internal static SKBitmap DecodeOriented(byte[] data,
                                            int fallbackExifOrientation = 1)
    {
        using MemoryStream stream = new(buffer: data);
        return DecodeOriented(stream: stream, fallbackExifOrientation: fallbackExifOrientation);
    }

    /// <inheritdoc cref="DecodeOriented(byte[], int)" />
    internal static SKBitmap DecodeOriented(Stream stream,
                                            int fallbackExifOrientation = 1)
    {
        using SKCodec codec = SKCodec.Create(stream: stream);
        if (codec == null)
        {
            return null;
        }

        int orientation = (int)codec.EncodedOrigin;
        if (orientation <= 1)
        {
            orientation = fallbackExifOrientation;
        }

        SKBitmap decoded = SKBitmap.Decode(codec: codec);
        if (decoded == null)
        {
            return null;
        }

        SKBitmap oriented = ApplyOrientation(bitmap: decoded, exifOrientation: orientation);
        if (!ReferenceEquals(objA: oriented, objB: decoded))
        {
            decoded.Dispose();
        }

        return oriented;
    }

    /// <summary>
    ///     Encodes a bitmap as JPEG.
    /// </summary>
    internal static byte[] EncodeJpeg(SKBitmap bitmap,
                                      int quality = ThumbnailJpegQuality)
    {
        using SKImage image = SKImage.FromBitmap(bitmap: bitmap);
        using SKData data = image.Encode(format: SKEncodedImageFormat.Jpeg, quality: quality);
        return data.ToArray();
    }
}
