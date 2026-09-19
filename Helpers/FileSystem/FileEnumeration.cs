using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.FileSystem;

/// <summary>
///     Enumerates files on disk for the folder-scanning pass.
/// </summary>
internal static class FileEnumeration
{
    /// <summary>
    ///     Compares files by full path.
    /// </summary>
    /// <remarks>
    ///     <see cref="FileInfo" /> does not override equality, so a plain HashSet of FileInfo deduplicates by reference
    ///     and therefore never actually deduplicates. Anything collecting FileInfo values should be built through
    ///     <see cref="CreateHashSetWithComparer" /> instead.
    /// </remarks>
    private sealed class FileInfoEqualityComparer : IEqualityComparer<FileInfo>
    {
        public bool Equals(FileInfo x, FileInfo y)
        {
            return x?.FullName == y?.FullName;
        }

        public int GetHashCode(FileInfo obj)
        {
            return obj.FullName.GetHashCode();
        }
    }

    /// <summary>
    ///     Creates a <see cref="HashSet{T}" /> of files that deduplicates by path rather than by reference.
    /// </summary>
    internal static HashSet<FileInfo> CreateHashSetWithComparer()
    {
        return [with(new FileInfoEqualityComparer())];
    }

    /// <summary>
    ///     Enumerates files under a folder, skipping anything that cannot be read.
    /// </summary>
    /// <remarks>
    ///     Folders the user cannot access are common enough (system folders, disconnected network shares, cloud
    ///     placeholders) that failing the whole scan over one of them is not acceptable; they are logged and skipped.
    ///     via https://stackoverflow.com/a/33172145/3968494
    /// </remarks>
    /// <param name="folder">The root folder to parse.</param>
    /// <param name="filter">Lower-case extensions to accept, including the leading dot.</param>
    /// <param name="recursive">Whether to descend into subfolders.</param>
    /// <param name="updateProgressHandler">Progress callback. Currently unused by the caller.</param>
    /// <param name="cancellationToken">Cancels the walk between files.</param>
    internal static IEnumerable<FileInfo> GetFilesFromAFolder(string folder,
                                                              string[] filter,
                                                              bool recursive,
                                                              Action<string> updateProgressHandler,
                                                              CancellationToken cancellationToken)
    {
        IEnumerable<string> found = [];
        try
        {
            found = Directory.GetFiles(path: folder)
                             .Where(predicate: file =>
                                  filter.Any(predicate: ext => file.ToLower().EndsWith(value: ext)));
        }
        catch (Exception ex)
        {
            Console.WriteLine(value: $"Error accessing folder {folder}: {ex.Message}");
        }

        int counter = 0;
        foreach (string file in found)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            // Keep the UI breathing during a long scan without pumping messages for every single file.
            if (counter % 10 == 0)
            {
                Application.DoEvents();
            }

            yield return new FileInfo(fileName: file);
            counter++;
        }

        if (!recursive)
        {
            yield break;
        }

        IEnumerable<string> directories = [];
        try
        {
            directories = Directory.GetDirectories(path: folder);
        }
        catch (Exception ex)
        {
            Console.WriteLine(value: $"Error accessing subdirectories in {folder}: {ex.Message}");
        }

        foreach (string dir in directories)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            foreach (FileInfo subFile in GetFilesFromAFolder(
                         folder: dir,
                         filter: filter,
                         recursive: true,
                         updateProgressHandler: updateProgressHandler,
                         cancellationToken: cancellationToken))
            {
                yield return subFile;
            }
        }
    }
}
