
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

    /// <summary>
    ///     Makes sure the thumbnail is in the in-memory tier, fetching it from the persistent store if that is where it
    ///     is. Nothing is decoded. Meant for background threads.
    /// </summary>
    /// <returns>True if a thumbnail for this file and size exists (now in memory).</returns>
    internal static bool EnsureLoaded(string fileNameWithPath,
                                      int size)
    {
        string? key = FileKeyedCache<byte[]>.KeyFor(fileNameWithPath: fileNameWithPath, variant: size.ToString());
        if (key == null)
        {
            return false;
        }

        if (Cache.TryGetByKey(key: key) != null)
        {
            return true;
        }

        byte[]? bytes = ThumbnailStore.TryGet(key: key);
        if (bytes == null)
        {
            return false;
        }

        Cache.AddByKey(key: key, value: bytes);
        return true;
    }

    /// <summary>
    ///     The cached thumbnail's JPEG bytes, or null when it isn't in memory.
    /// </summary>
    internal static byte[]? TryGetBytes(string fileNameWithPath,
                                        int size)
    {
        string? key = FileKeyedCache<byte[]>.KeyFor(fileNameWithPath: fileNameWithPath, variant: size.ToString());
        return key == null
            ? null
            : Cache.TryGetByKey(key: key);
    }

    /// <summary>
    ///     Puts a freshly made thumbnail (JPEG bytes) into the memory cache and the database.
    /// </summary>
    internal static void Add(string fileNameWithPath,
                             int size,
                             byte[] jpeg)
    {
        string? key = FileKeyedCache<byte[]>.KeyFor(fileNameWithPath: fileNameWithPath, variant: size.ToString());
        if (key == null)
        {
            return;
        }

        try
        {
            Cache.AddByKey(key: key, value: jpeg);
            ThumbnailStore.Add(key: key, jpeg: jpeg);
        }
        catch
        {
            // a cache miss next time is the worst outcome
        }
    }

    /// <summary>Empties the in-memory tier (the persistent one is left alone).</summary>
    internal static void Clear() => Cache.Clear();
}
