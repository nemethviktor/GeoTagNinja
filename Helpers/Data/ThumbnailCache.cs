using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Process-wide, in-memory cache of finished icon-view thumbnails, so revisiting a folder doesn't regenerate them.
///     Entries are keyed by full path + last-write time + length, so an edited or replaced file misses automatically
///     and two folders holding a file of the same name can never share an entry.
/// </summary>
/// <remarks>
///     Thumbnails are held as small JPEG byte arrays rather than <see cref="Image" /> objects (a 128 px bitmap is
///     64 KB of GDI memory, a JPEG of it a few KB), and decoding one is far cheaper than regenerating it. Total size is
///     capped; when full, the oldest entries go first. Nothing is persisted between sessions.
/// </remarks>
internal static class ThumbnailCache
{
    private const long MaxTotalBytes = 512L * 1024 * 1024;

    private static readonly ConcurrentDictionary<string, byte[]> Entries = new();
    private static readonly ConcurrentQueue<string> InsertionOrder = new();
    private static long _totalBytes;

    /// <summary>Returns a fresh <see cref="Image" /> for the file if cached, else null. The caller owns the image.</summary>
    internal static Image? TryGet(string fileNameWithPath)
    {
        string? key = KeyFor(fileNameWithPath: fileNameWithPath);
        if (key == null ||
            !Entries.TryGetValue(key: key, value: out byte[]? bytes))
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
        string? key = KeyFor(fileNameWithPath: fileNameWithPath);
        if (key == null)
        {
            return;
        }

        try
        {
            using MemoryStream stream = new();
            thumbnail.Save(stream: stream, format: ImageFormat.Jpeg);
            byte[] bytes = stream.ToArray();

            if (Entries.TryAdd(key: key, value: bytes))
            {
                InsertionOrder.Enqueue(item: key);
                _ = Interlocked.Add(location1: ref _totalBytes, value: bytes.Length);
                Trim();
            }
        }
        catch
        {
            // a cache miss next time is the worst outcome
        }
    }

    /// <summary>Empties the cache.</summary>
    internal static void Clear()
    {
        Entries.Clear();
        while (InsertionOrder.TryDequeue(result: out string? _))
        {
        }

        _ = Interlocked.Exchange(location1: ref _totalBytes, value: 0);
    }

    private static void Trim()
    {
        while (Interlocked.Read(location: ref _totalBytes) > MaxTotalBytes &&
               InsertionOrder.TryDequeue(result: out string? oldest))
        {
            if (Entries.TryRemove(key: oldest, value: out byte[]? removed))
            {
                _ = Interlocked.Add(location1: ref _totalBytes, value: -removed.Length);
            }
        }
    }

    private static string? KeyFor(string fileNameWithPath)
    {
        try
        {
            FileInfo info = new(fileName: fileNameWithPath);
            return info.Exists
                ? $"{fileNameWithPath}|{info.LastWriteTimeUtc.Ticks}|{info.Length}"
                : null;
        }
        catch
        {
            return null;
        }
    }
}
