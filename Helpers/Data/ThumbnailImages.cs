using GeoTagNinja.Helpers.Exif;
using System.Drawing;
using System.IO;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     The WinForms view of <see cref="ThumbnailCache" />: turns the cached JPEG bytes into a System.Drawing image.
/// </summary>
internal static class ThumbnailImages
{
    /// <summary>
    ///     The cached thumbnail as an image the caller owns, or null when it isn't in memory.
    /// </summary>
    internal static Image? TryGetMemory(string fileNameWithPath,
                                        int size)
    {
        byte[]? bytes = ThumbnailCache.TryGetBytes(fileNameWithPath: fileNameWithPath, size: size);
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
}
