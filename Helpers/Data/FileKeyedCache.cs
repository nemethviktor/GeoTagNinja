using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Thread-safe, size-capped, in-memory cache of per-file values, keyed by full path + last-write time + length.
///     An edited or replaced file therefore misses automatically, and two folders holding a file of the same name can
///     never share an entry. When the cap is exceeded the oldest entries are dropped first. Nothing is persisted.
/// </summary>
/// <typeparam name="T">The cached value.</typeparam>
internal sealed class FileKeyedCache<T>(long maxTotalBytes,
                                        Func<T, long> sizeOf)
    where T : class
{
    private readonly ConcurrentDictionary<string, T> _entries = new();
    private readonly ConcurrentQueue<string> _insertionOrder = new();
    private long _totalBytes;

    internal T? TryGet(string fileNameWithPath)
    {
        string? key = KeyFor(fileNameWithPath: fileNameWithPath);
        return key != null && _entries.TryGetValue(key: key, value: out T? value)
            ? value
            : null;
    }

    internal void Add(string fileNameWithPath,
                      T value)
    {
        string? key = KeyFor(fileNameWithPath: fileNameWithPath);
        if (key == null ||
            !_entries.TryAdd(key: key, value: value))
        {
            return;
        }

        _insertionOrder.Enqueue(item: key);
        _ = Interlocked.Add(location1: ref _totalBytes, value: sizeOf(arg: value));

        while (Interlocked.Read(location: ref _totalBytes) > maxTotalBytes &&
               _insertionOrder.TryDequeue(result: out string? oldest))
        {
            if (_entries.TryRemove(key: oldest, value: out T? removed))
            {
                _ = Interlocked.Add(location1: ref _totalBytes, value: -sizeOf(arg: removed));
            }
        }
    }

    internal void Clear()
    {
        _entries.Clear();
        while (_insertionOrder.TryDequeue(result: out string? _))
        {
        }

        _ = Interlocked.Exchange(location1: ref _totalBytes, value: 0);
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
