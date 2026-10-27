namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     A preview-pane image kept as JPEG bytes. <see cref="ExifOrientation" /> (EXIF values 1-8, 0 = none) is the
///     orientation still to be applied if the JPEG carries no orientation tag of its own; it comes from the source file,
///     not the embedded JPEG.
/// </summary>
internal sealed class PreviewEntry(byte[] jpeg,
                                   int exifOrientation)
{
    internal byte[] Jpeg { get; } = jpeg;
    internal int ExifOrientation { get; } = exifOrientation;
}

/// <summary>
///     Small, process-wide, in-memory cache of recently shown previews for the slow-to-produce formats (RAW embedded
///     previews, HEIC/WebP conversions), so going back to a file is instant. See <see cref="FileKeyedCache{T}" />.
/// </summary>
internal static class PreviewCache
{
    private const long MaxTotalBytes = 256L * 1024 * 1024;

    private static readonly FileKeyedCache<PreviewEntry> Cache = new(maxTotalBytes: MaxTotalBytes,
        sizeOf: entry => entry.Jpeg.Length);

    internal static PreviewEntry? TryGet(string fileNameWithPath) => Cache.TryGet(fileNameWithPath: fileNameWithPath);

    internal static void Add(string fileNameWithPath,
                             PreviewEntry entry) => Cache.Add(fileNameWithPath: fileNameWithPath, value: entry);

    internal static void Clear() => Cache.Clear();
}
