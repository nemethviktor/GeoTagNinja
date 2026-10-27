using NLog;
using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using System;
using System.Drawing;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     Makes sure a file's thumbnail is in <see cref="ThumbnailCache" /> (generating it if neither tier has it). The view
///     then paints from the cache; nothing holds a thumbnail per file.
/// </summary>
/// <remarks>
///     Thumbnails are generated at one of two stored sizes ("buckets") rather than at the exact size on screen, so
///     resizing the view doesn't invalidate the cache every time. Up to 128 px on screen uses the 128 bucket (cheap:
///     embedded thumbnails are enough), larger uses the 256 bucket; the view scales the stored image to fit.
/// </remarks>
internal static class ThumbnailProvider
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    internal const int SmallBucket = 128;
    internal const int LargeBucket = 256;

    /// <summary>The stored size to use for a thumbnail shown at <paramref name="displaySize" /> pixels.</summary>
    internal static int BucketFor(int displaySize) => displaySize <= SmallBucket
        ? SmallBucket
        : LargeBucket;

    /// <summary>
    ///     Ensures the thumbnail exists in the cache. Slow when it has to be generated, so call from a worker thread.
    /// </summary>
    /// <returns>True if a thumbnail is available afterwards.</returns>
    internal static bool Ensure(DirectoryElement directoryElement,
                                int bucket)
    {
        if (directoryElement.Type != DirectoryElement.ElementType.File ||
            directoryElement.IsCloudOffline)
        {
            return false;
        }

        string path = directoryElement.FileNameWithPath;
        if (ThumbnailCache.EnsureLoaded(fileNameWithPath: path, size: bucket))
        {
            return true;
        }

        Log.Info(message: $"Thumbnail generation started for {path}");
        try
        {
            using Image? thumbnail = ThumbnailGenerator.Create(fileNameWithPath: path, size: bucket);
            if (thumbnail == null)
            {
                return false;
            }

            ThumbnailCache.Add(fileNameWithPath: path, size: bucket, thumbnail: thumbnail);
            Log.Info(message: $"Thumbnail generation finished for {path}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, message: $"Thumbnail generation failed for {path}");
            return false;
        }
    }
}
