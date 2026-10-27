using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Process-wide, in-memory cache of finished icon-view thumbnails, so revisiting a folder doesn't regenerate them.
///     See <see cref="FileKeyedCache{T}" /> for the keying and eviction rules.
/// </summary>
/// <remarks>
///     Thumbnails are held as small JPEG byte arrays rather than <see cref="Image" /> objects (a 128 px bitmap is
///     64 KB of GDI memory, a JPEG of it a few KB), and decoding one is far cheaper than regenerating it.
/// </remarks>
internal static class ThumbnailCache
{
    private const long MaxTotalBytes = 512L * 1024 * 1024;

    private static readonly FileKeyedCache<byte[]> Cache = new(maxTotalBytes: MaxTotalBytes,
        sizeOf: bytes => bytes.Length);

    /// <summary>Returns a fresh <see cref="Image" /> for the file if cached, else null. The caller owns the image.</summary>
    internal static Image? TryGet(string fileNameWithPath)
    {
        byte[]? bytes = Cache.TryGet(fileNameWithPath: fileNameWithPath);
        if (bytes == null)
        {
            return null;
        }

        try
        {
            using MemoryStream stream = new(buffer: bytes);
            using Image decoded = Image.FromStream(stream: stream);
            // copy so the returned image doesn't depend on the stream
            return new Bitmap(original: decoded);
        }
        catch
        {
            return null;
        }
    }

    internal static void Add(string fileNameWithPath,
                             Image thumbnail)
    {
        try
        {
            using MemoryStream stream = new();
            thumbnail.Save(stream: stream, format: ImageFormat.Jpeg);
            Cache.Add(fileNameWithPath: fileNameWithPath, value: stream.ToArray());
        }
        catch
        {
            // a cache miss next time is the worst outcome
        }
    }

    /// <summary>Empties the cache.</summary>
    internal static void Clear() => Cache.Clear();
}
