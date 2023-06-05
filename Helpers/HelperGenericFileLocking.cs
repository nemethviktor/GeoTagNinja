using System.Collections.Generic;

namespace GeoTagNinja.Helpers;

internal static class HelperGenericFileLocking
{
    internal static readonly object TableLock = new();
    internal static HashSet<string> FilesBeingProcessed = new();
    internal static bool FileListBeingUpdated;
    internal static bool FilesAreBeingSaved;

    /// <summary>
    ///     Checks if a file is currently locked by any other running operation - checks if the ItemNameWithoutPath is
    ///     currently in FilesBeingProcessed
    /// </summary>
    /// <param name="fileNameWithoutPath">The file name without the path.</param>
    /// <returns>A true/false</returns>
    internal static bool GenericLockCheckLockFile(string fileNameWithoutPath)
    {
        return FilesBeingProcessed.Contains(item: fileNameWithoutPath);
    }

    /// <summary>
    ///     Adds ItemNameWithoutPath to FilesBeingProcessed
    /// </summary>
    /// <param name="fileNameWithoutPath">The file name without the path.</param>
    internal static void GenericLockLockFile(string fileNameWithoutPath)
    {
        FilesBeingProcessed.Add(item: fileNameWithoutPath);
    }

    /// <summary>
    ///     Removes ItemNameWithoutPath from FilesBeingProcessed
    /// </summary>
    /// <param name="fileNameWithoutPath">The file name without the path.</param>
    internal static void GenericLockUnLockFile(string fileNameWithoutPath)
    {
        FilesBeingProcessed.Remove(item: fileNameWithoutPath);
    }
}