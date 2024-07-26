using System;
using System.IO;
using System.Security.Cryptography;

namespace GeoTagNinja.Helpers;

internal static class HelperFileSystemGetChecksum
{
    /// <summary>
    ///     This is via
    ///     https://stackoverflow.com/questions/1177607/what-is-the-fastest-way-to-create-a-checksum-for-large-files-in-c-sharp/1177744#1177744
    ///     Basically the idea is that we can checksum files relatively quickly and then if matching whatever is stored in the
    ///     memory then don't reload every bit of data again.
    /// </summary>
    /// <returns>The checksum of the file or string.Empty if the file doesn't exist.</returns>
    internal static string GetChecksum(string fileNameWithPath)
    {
        if (File.Exists(path: fileNameWithPath))
        {
            using BufferedStream stream = new(stream: File.OpenRead(path: fileNameWithPath), bufferSize: 1200000);
            SHA256Managed sha = new();
            byte[] checksum = sha.ComputeHash(inputStream: stream);
            return BitConverter.ToString(value: checksum).Replace(oldValue: "-", newValue: string.Empty);
        }

        return string.Empty;
    }
}