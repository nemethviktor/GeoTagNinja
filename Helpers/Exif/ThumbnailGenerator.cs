using ImageMagick;
using ImageMagick.Formats;
using Sdcb.LibRaw;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     Produces the square icon-view thumbnails, entirely in memory. The idea (and the reason this is quick) is the
///     same as in other photo browsers: avoid decoding the full image whenever the file already carries a small JPEG
///     (EXIF IFD1 thumbnail, RAW embedded preview), and when it doesn't, ask the decoder for a pre-scaled decode instead
///     of a full one. Nothing is written to disk.
/// </summary>
internal static class ThumbnailGenerator
{
    /// <summary>EXIF "ThumbnailData" GDI+ property (the IFD1 JPEG).</summary>
    private const int ExifThumbnailDataId = 0x501B;

    /// <summary>Highest LibRaw embedded-thumbnail index worth probing. Some bodies have index 0, others 1 and beyond.</summary>
    private const int MaxLibRawThumbnailIndex = 3;

    /// <summary>How far the embedded thumbnail's aspect ratio may drift from the real image before we distrust it.</summary>
    private const double AspectTolerance = 0.02;

    /// <summary>Formats GDI+ reads natively; everything else is RAW-ish or goes to Magick.</summary>
    private static readonly HashSet<string> GdiPlusExtensions =
    [
        "jpg", "jpeg", "jpe", "tif", "tiff", "png", "bmp", "gif"
    ];

    private static readonly HashSet<string> JpegExtensions = ["jpg", "jpeg", "jpe"];

    private static readonly HashSet<string> MagickOnlyExtensions = ["heic", "heif", "webp"];

    /// <summary>True for formats GDI+ reads natively (lower-case extension without the dot).</summary>
    internal static bool IsGdiPlusFormat(string extension) => GdiPlusExtensions.Contains(item: extension);

    /// <summary>True for formats only Magick reads (HEIC, WebP, ...).</summary>
    internal static bool IsMagickOnlyFormat(string extension) => MagickOnlyExtensions.Contains(item: extension);

    /// <summary>
    ///     Builds a <paramref name="size" /> x <paramref name="size" /> thumbnail (letterboxed, orientation applied).
    /// </summary>
    /// <param name="fileNameWithPath">The image to thumbnail.</param>
    /// <param name="size">Edge length in pixels.</param>
    /// <returns>The thumbnail, or null if no method could read the file.</returns>
    internal static Image? Create(string fileNameWithPath,
                                  int size)
    {
        string extension = Path.GetExtension(path: fileNameWithPath).TrimStart('.').ToLowerInvariant();

        Image? result = null;

        if (IsGdiPlusFormat(extension: extension))
        {
            result = TryExifThumbnail(fileNameWithPath: fileNameWithPath, size: size);

            if (result == null &&
                JpegExtensions.Contains(item: extension))
            {
                result = TryMagick(fileNameWithPath: fileNameWithPath, size: size, jpegScaledDecode: true);
            }

            result ??= TryGdiPlusFullDecode(fileNameWithPath: fileNameWithPath, size: size);
        }
        else if (IsMagickOnlyFormat(extension: extension))
        {
            result = TryMagick(fileNameWithPath: fileNameWithPath, size: size, jpegScaledDecode: false);
        }
        else
        {
            result = TryLibRawEmbeddedPreview(fileNameWithPath: fileNameWithPath, size: size);
            result ??= TryLibRawFullImage(fileNameWithPath: fileNameWithPath, size: size);
            result ??= TryMagick(fileNameWithPath: fileNameWithPath, size: size, jpegScaledDecode: false);
        }

        return result;
    }

    /// <summary>
    ///     Reads the EXIF IFD1 thumbnail out of a GDI+-readable file. With validateImageData off GDI+ only parses the
    ///     header, so the megapixels of the main image are never decoded.
    /// </summary>
    private static Image? TryExifThumbnail(string fileNameWithPath,
                                           int size)
    {
        try
        {
            using FileStream stream = new(path: fileNameWithPath, mode: FileMode.Open, access: FileAccess.Read,
                share: FileShare.ReadWrite);
            using Image main = Image.FromStream(stream: stream, useEmbeddedColorManagement: false,
                validateImageData: false);

            if (!main.PropertyIdList.Contains(value: ExifThumbnailDataId))
            {
                return null;
            }

            byte[] thumbBytes = main.GetPropertyItem(propid: ExifThumbnailDataId).Value;
            if (thumbBytes == null ||
                thumbBytes.Length == 0)
            {
                return null;
            }

            using MemoryStream thumbStream = new(buffer: thumbBytes);
            using Image thumb = Image.FromStream(stream: thumbStream);

            // Too small to look decent at the target size, or edited by something that left a stale thumbnail.
            if (Math.Max(val1: thumb.Width, val2: thumb.Height) < size)
            {
                return null;
            }

            double thumbAspect = (double)thumb.Width / thumb.Height;
            double mainAspect = (double)main.Width / main.Height;
            if (Math.Abs(value: thumbAspect / mainAspect - 1) > AspectTolerance)
            {
                return null;
            }

            // The IFD1 thumbnail carries no orientation of its own; the main image's tag applies to it.
            if (main.PropertyIdList.Contains(value: HelperVariables.exifOrientationID))
            {
                thumb.SetPropertyItem(propitem: main.GetPropertyItem(propid: HelperVariables.exifOrientationID));
            }

            thumb.ExifRotate();
            return FitToSquare(source: thumb, size: size);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Magick with a scaled decode: for JPEG the <c>jpeg:size</c> define lets libjpeg decode at 1/2, 1/4 or 1/8
    ///     scale, which is a fraction of the cost of a full decode.
    /// </summary>
    private static Image? TryMagick(string fileNameWithPath,
                                    int size,
                                    bool jpegScaledDecode)
    {
        try
        {
            MagickReadSettings settings = new();
            if (jpegScaledDecode)
            {
                settings.SetDefines(defines: new JpegReadDefines
                {
                    Size = new MagickGeometry(width: (uint)size, height: (uint)size)
                });
            }

            using MagickImage image = new(fileName: fileNameWithPath, readSettings: settings);
            image.AutoOrient();
            image.Thumbnail(geometry: new MagickGeometry(width: (uint)size, height: (uint)size));

            return BitmapFromMagick(image: image, size: size);
        }
        catch
        {
            return null;
        }
    }

    private static Image BitmapFromMagick(MagickImage image,
                                          int size)
    {
        using MemoryStream stream = new(buffer: image.ToByteArray(format: MagickFormat.Bmp));
        using Image decoded = Image.FromStream(stream: stream);
        return FitToSquare(source: decoded, size: size);
    }

    /// <summary>
    ///     Last resort for GDI+ formats: full decode. Slow, but only reached for files with no usable embedded thumbnail
    ///     that Magick also couldn't read.
    /// </summary>
    private static Image? TryGdiPlusFullDecode(string fileNameWithPath,
                                               int size)
    {
        try
        {
            using FileStream stream = new(path: fileNameWithPath, mode: FileMode.Open, access: FileAccess.Read,
                share: FileShare.ReadWrite);
            using Image full = Image.FromStream(stream: stream);
            full.ExifRotate();
            return FitToSquare(source: full, size: size);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Pulls the JPEG preview embedded in a RAW. The file is opened once and the thumbnail indexes are probed on that
    ///     one context (previously the file was re-opened for each index).
    /// </summary>
    private static Image? TryLibRawEmbeddedPreview(string fileNameWithPath,
                                                   int size)
    {
        try
        {
            using RawContext raw = RawContext.OpenFile(filePath: fileNameWithPath);
            int exifOrientation = RawPreview.TryGetExifOrientation(raw: raw);

            for (int index = 0; index <= MaxLibRawThumbnailIndex; index++)
            {
                try
                {
                    using ProcessedImage thumb = raw.ExportThumbnail(thumbnailIndex: index);
                    using Image decoded = RawPreview.DecodeOriented(jpeg: thumb.AsSpan<byte>().ToArray(), exifOrientation: exifOrientation);
                    return FitToSquare(source: decoded, size: size);
                }
                catch
                {
                    // this index doesn't exist or isn't a JPEG; try the next.
                }
            }
        }
        catch
        {
            // not a RAW LibRaw understands
        }

        return null;
    }

    /// <summary>
    ///     Full demosaic. Very slow, so strictly a fallback for RAWs with no embedded preview.
    /// </summary>
    private static Image? TryLibRawFullImage(string fileNameWithPath,
                                             int size)
    {
        string tempFile = Path.Combine(path1: Path.GetTempPath(), path2: $"gtn_{Guid.NewGuid():N}.jpg");
        try
        {
            ReadGetImagePreviews.UseLibRawToGenerateFullImage(originalImagePath: fileNameWithPath,
                generatedJpegPath: tempFile);

            if (!File.Exists(path: tempFile))
            {
                return null;
            }

            using FileStream stream = new(path: tempFile, mode: FileMode.Open, access: FileAccess.Read);
            using Image decoded = Image.FromStream(stream: stream);
            return FitToSquare(source: decoded, size: size);
        }
        catch
        {
            return null;
        }
        finally
        {
            try
            {
                File.Delete(path: tempFile);
            }
            catch
            {
                // temp file; not worth failing over
            }
        }
    }

    /// <summary>
    ///     Scales <paramref name="source" /> to fit a <paramref name="size" /> square, centred. The source is not
    ///     disposed.
    /// </summary>
    private static Bitmap FitToSquare(Image source,
                                      int size)
    {
        float scale = Math.Min(val1: size / (float)source.Width, val2: size / (float)source.Height);
        int destWidth = Math.Max(val1: 1, val2: (int)(source.Width * scale));
        int destHeight = Math.Max(val1: 1, val2: (int)(source.Height * scale));
        int destX = (size - destWidth) / 2;
        int destY = (size - destHeight) / 2;

        Bitmap bitmap = new(width: size, height: size, format: PixelFormat.Format24bppRgb);
        using Graphics graphics = Graphics.FromImage(image: bitmap);
        graphics.Clear(color: Color.Transparent);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(image: source,
            destRect: new Rectangle(x: destX, y: destY, width: destWidth, height: destHeight),
            srcRect: new Rectangle(x: 0, y: 0, width: source.Width, height: source.Height),
            srcUnit: GraphicsUnit.Pixel);

        return bitmap;
    }
}
