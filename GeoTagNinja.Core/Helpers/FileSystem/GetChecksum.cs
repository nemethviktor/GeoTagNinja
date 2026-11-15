using System;
using System.IO;
using System.IO.Hashing;

namespace GeoTagNinja.Helpers.FileSystem;

internal static class GetChecksum
{
    /// <summary>
    ///     Used purely for change-detection (has this file's content changed since the last folder scan?), not as a
    ///     security boundary, so a fast non-cryptographic hash (xxHash) is used instead of SHA-256 - still a
    ///     full-content hash, so single-byte changes are still caught.
    /// </summary>
    /// <returns>The checksum of the file or string. Empty if the file doesn't exist.</returns>
    internal static string GetFileChecksum(string fileNameWithPath)
    {
        if (File.Exists(path: fileNameWithPath))
        {
            using FileStream stream = new(path: fileNameWithPath, mode: FileMode.Open, access: FileAccess.Read, share: FileShare.Read, bufferSize: 1200000);
            XxHash3 hasher = new();
            hasher.Append(stream: stream);
            byte[] checksum = hasher.GetCurrentHash();
            return BitConverter.ToString(value: checksum).Replace(oldValue: "-", newValue: string.Empty);
        }

        return string.Empty;
    }
}