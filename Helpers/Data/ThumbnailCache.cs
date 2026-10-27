using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Cache of finished icon-view thumbnails, so revisiting a folder (this session or a later one) doesn't regenerate
///     them. Two tiers: an in-memory one (<see cref="FileKeyedCache{T}" />, fast, lost on exit) in front of the
///     persistent <see cref="ThumbnailStore" />. Entries are specific to a thumbnail size.
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

    /// <summary>Returns a fresh <see cref="Image" /> for the file at this size if cached, else null. The caller owns the image.</summary>
    internal static Image? TryGet(string fileNameWithPath,
                                  int size)
    {
        string? key = FileKeyedCache<byte[]>.KeyFor(fileNameWithPath: fileNameWithPath, variant: size.ToString());
        if (key == null)
        {
            return null;
        }

        byte[]? bytes = Cache.TryGetByKey(key: key);
        if (bytes == null)
        {
            bytes = ThumbnailStore.TryGet(key: key);
            if (bytes == null)
            {
                return null;
            }

            Cache.AddByKey(key: key, value: bytes);
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
                             int size,
                             Image thumbnail)
    {
        string? key = FileKeyedCache<byte[]>.KeyFor(fileNameWithPath: fileNameWithPath, variant: size.ToString());
        if (key == null)
        {
            return;
        }

        try
        {
            using MemoryStream stream = new();
            thumbnail.Save(stream: stream, format: ImageFormat.Jpeg);
            byte[] bytes = stream.ToArray();
            Cache.AddByKey(key: key, value: bytes);
            ThumbnailStore.Add(key: key, jpeg: bytes);
        }
        catch
        {
            // a cache miss next time is the worst outcome
        }
    }

    /// <summary>Empties the in-memory tier (the persistent one is left alone).</summary>
    internal static void Clear() => Cache.Clear();
}
