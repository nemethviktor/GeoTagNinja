using GeoTagNinja.Helpers.Data;
using Sdcb.LibRaw;
using Sdcb.LibRaw.Natives;
using System;
using System.Collections.Generic;
using SkiaSharp;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     In-memory access to the JPEG previews embedded in RAW files (and decoding of JPEG bytes with orientation
///     applied), via LibRaw. This is what lets the preview pane skip launching a separate ExifTool process.
/// </summary>
internal static class RawPreview
{
    private const int JpegMagic1 = 0xFF;
    private const int JpegMagic2 = 0xD8;

    /// <summary>Highest embedded-preview index probed when LibRaw's own list can't be read.</summary>
    private const int MaxProbeIndex = 3;

    /// <summary>
    ///     <c>RawContext.RawData</c> (the native LibRaw structures: thumbnail list, orientation) is protected in the
    ///     wrapper library, so it is read through reflection, looked up once. Callers must cope with null.
    /// </summary>
    private static readonly PropertyInfo? RawDataProperty = typeof(RawContext).GetProperty(name: "RawData",
        bindingAttr: BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private static LibRawData? GetRawData(RawContext raw)
    {
        try
        {
            return RawDataProperty?.GetValue(obj: raw) is LibRawData data
                ? data
                : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Reads the embedded JPEG that best fits a target size, together with the RAW's own orientation. RAWs embed
    ///     several images (a Nikon NEF typically: a 160 px raw bitmap, a full-size JPEG, a ~1600 px JPEG and a ~600 px
    ///     JPEG), and decoding the full-size one to make a thumbnail is what makes this slow, so the smallest JPEG whose
    ///     long edge is at least <paramref name="minLongEdge" /> is used; if none is that big, the biggest there is.
    /// </summary>
    /// <param name="fileNameWithPath">The RAW file.</param>
    /// <param name="minLongEdge">The long edge, in pixels, the result should have at least.</param>
    /// <returns>The preview, or null if LibRaw can't read the file or it has no embedded JPEG.</returns>
    internal static PreviewEntry? TryReadEmbeddedJpeg(string fileNameWithPath,
                                                      int minLongEdge)
    {
        try
        {
            using RawContext raw = RawContext.OpenFile(filePath: fileNameWithPath);
            int exifOrientation = TryGetExifOrientation(raw: raw);

            foreach (int index in CandidateIndexes(raw: raw, minLongEdge: minLongEdge))
            {
                try
                {
                    using ProcessedImage thumb = raw.ExportThumbnail(thumbnailIndex: index);
                    byte[] bytes = thumb.AsSpan<byte>().ToArray();

                    if (bytes.Length > 2 &&
                        bytes[0] == JpegMagic1 &&
                        bytes[1] == JpegMagic2)
                    {
                        return new PreviewEntry(jpeg: bytes, exifOrientation: exifOrientation);
                    }
                }
                catch
                {
                    // this index doesn't exist or isn't a JPEG; try the next best
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
    ///     The RAW container's orientation as an EXIF orientation value, or 0 if it can't be read. (LibRaw reports it as
    ///     0 none, 3 = 180, 5 = 90 CCW, 6 = 90 CW.)
    /// </summary>
    internal static int TryGetExifOrientation(RawContext raw)
    {
        try
        {
            return GetRawData(raw: raw)?.ImageSizes.Flip switch
            {
                3 => 3,
                5 => 8,
                6 => 6,
                _ => 0
            };
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    ///     Decodes an embedded JPEG and turns it upright. The orientation the JPEG itself carries wins; the RAW's own
    ///     (<paramref name="exifOrientation" />) is used when the JPEG has none.
    /// </summary>
    /// <returns>The upright bitmap (caller owns it), or null if the bytes can't be decoded.</returns>
    internal static SKBitmap DecodeOriented(byte[] jpeg,
                                            int exifOrientation)
    {
        return ImageOps.DecodeOriented(data: jpeg, fallbackExifOrientation: exifOrientation);
    }

    /// <summary>
    ///     Full demosaic with LibRaw, written out as a JPEG. Very slow: only for RAWs with no usable embedded preview.
    ///     Failures are swallowed; the caller checks whether the file appeared.
    /// </summary>
    /// <param name="originalImagePath">The RAW file.</param>
    /// <param name="generatedJpegPath">Where to write the JPEG.</param>
    internal static void GenerateFullImageJpeg(string originalImagePath,
                                               string generatedJpegPath)
    {
        try
        {
            using RawContext raw = RawContext.OpenFile(filePath: originalImagePath);
            raw.Unpack();
            raw.DcrawProcess();
            using ProcessedImage image = raw.MakeDcrawMemoryImage();

            // LibRaw hands back 8-bit RGB, three bytes a pixel, rows tightly packed.
            int width = image.Width;
            int height = image.Height;
            byte[] rgb = new byte[width * height * 3];
            System.Runtime.InteropServices.Marshal.Copy(source: image.DataPointer, destination: rgb, startIndex: 0,
                length: rgb.Length);

            using SKBitmap bitmap = new(info: new SKImageInfo(width: width, height: height,
                colorType: SKColorType.Rgba8888, alphaType: SKAlphaType.Opaque));
            byte[] rgba = new byte[width * height * 4];
            for (int pixel = 0, source = 0, target = 0; pixel < width * height; pixel++)
            {
                rgba[target++] = rgb[source++];
                rgba[target++] = rgb[source++];
                rgba[target++] = rgb[source++];
                rgba[target++] = 255;
            }

            System.Runtime.InteropServices.Marshal.Copy(source: rgba, startIndex: 0, destination: bitmap.GetPixels(),
                length: rgba.Length);

            File.WriteAllBytes(path: generatedJpegPath, bytes: ImageOps.EncodeJpeg(bitmap: bitmap, quality: 92));
        }
        catch
        {
            // ignored
        }
    }

    private static IEnumerable<int> CandidateIndexes(RawContext raw,
                                                     int minLongEdge)
    {
        try
        {
            LibRawThumbnailList? list = GetRawData(raw: raw)?.ThumbnailList;
            if (list?.ThumbList != null &&
                list.Value.ThumbCount > 0)
            {
                LibRawThumbnailItem[] items = list.Value.ThumbList;
                List<(int Index, int LongEdge)> jpegs = Enumerable
                                                        .Range(start: 0, count: Math.Min(val1: list.Value.ThumbCount, val2: items.Length))
                                                        .Where(predicate: i => items[i].Format == InternalThumbnailFormat.Jpeg)
                                                        .Select(selector: i => (Index: i, LongEdge: Math.Max(val1: (int)items[i].Width, val2: (int)items[i].Height)))
                                                        .ToList();

                return jpegs.Where(predicate: j => j.LongEdge >= minLongEdge)
                            .OrderBy(keySelector: j => j.LongEdge)
                            .Concat(second: jpegs.Where(predicate: j => j.LongEdge < minLongEdge)
                                                 .OrderByDescending(keySelector: j => j.LongEdge))
                            .Select(selector: j => j.Index)
                            .ToList();
            }
        }
        catch
        {
            // fall through to the blind range
        }

        return Enumerable.Range(start: 0, count: MaxProbeIndex + 1);
    }
}
