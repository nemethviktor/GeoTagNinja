using System.Collections.Generic;
using System.IO;

namespace GeoTagNinja.Helpers;

internal partial class HelperNonStatic
{
    // Nested FileInfoEqualityComparer class
    // Courtesy ChatGPT
    private class FileInfoEqualityComparer : IEqualityComparer<FileInfo>
    {
        public bool Equals(FileInfo? x, FileInfo? y)
        {
            return x?.FullName == y?.FullName;
        }

        public int GetHashCode(FileInfo obj)
        {
            return obj.FullName.GetHashCode();
        }
    }

    /// <summary>
    ///     Creates a HashSet for FileInfo HashSets so they can be actually unique
    /// </summary>
    /// <returns></returns>
    public HashSet<FileInfo> CreateHashSetWithComparer()
    {
        return new HashSet<FileInfo>(new FileInfoEqualityComparer());
    }
}