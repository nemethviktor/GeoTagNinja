using GeoTagNinja.Helpers.Localisation;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GeoTagNinja.Helpers.FileSystem;

/// <summary>
///     The checks that need the disk, done before a batch rename starts so that what can be known to fail is stopped
///     up front rather than found halfway through. (The planner has already dealt with everything that can be worked out
///     from the names alone.)
/// </summary>
/// <remarks>
///     These are estimates: a file can still be locked a moment later. That case is handled by the executor and the
///     question the user is then asked (put everything back, or go on without the file that failed).
/// </remarks>
internal static class BatchRenamePreflight
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Checks every file that would be touched. Sets <see cref="BatchRenameEntry.Problem" /> on the entries that would
    ///     fail.
    /// </summary>
    /// <returns>Problems that belong to the whole batch rather than one file (not enough disk space); empty if none.</returns>
    internal static List<string> Check(IReadOnlyList<BatchRenameEntry> entries,
                                       BatchRenameDestination destination)
    {
        List<string> batchProblems = [];
        List<BatchRenameEntry> todo = entries.Where(predicate: entry => !entry.Unchanged && entry.Problem == null).ToList();

        foreach (BatchRenameEntry entry in todo)
        {
            string path = entry.Source.FullPath;
            if (!File.Exists(path: path))
            {
                entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_Gone");
                continue;
            }

            if (FileLocking.FilesAreBeingSaved ||
                FileLocking.GenericLockCheckLockFile(fileNameWithoutPath: Path.GetFileName(path: path)))
            {
                entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_BeingSaved");
                continue;
            }

            // opening a cloud-only placeholder would download it, so those are not probed
            if (!CloudFileHelpers.IsCloudOnlyPlaceholder(filePath: path) &&
                IsInUse(path: path))
            {
                entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_InUse");
            }
        }

        // can each folder that is written to be written to? One probe per folder, not per file.
        Dictionary<string, string> folderProblems = new(comparer: StringComparer.OrdinalIgnoreCase);
        foreach (BatchRenameEntry entry in todo.Where(predicate: e => e.Problem == null))
        {
            List<string> folders = [Path.GetDirectoryName(path: entry.NewPath) ?? string.Empty];
            if (destination != BatchRenameDestination.CopyToFolder)
            {
                // a rename also takes the file out of its old folder
                folders.Add(item: Path.GetDirectoryName(path: entry.Source.FullPath) ?? string.Empty);
            }

            foreach (string folder in folders.Where(predicate: f => f.Length > 0))
            {
                if (!folderProblems.TryGetValue(key: folder, value: out string problem))
                {
                    problem = FolderProblem(folder: folder);
                    folderProblems[key: folder] = problem;
                }

                if (problem != null)
                {
                    entry.Problem = problem;
                    break;
                }
            }
        }

        if (destination != BatchRenameDestination.SameFolder)
        {
            string space = DiskSpaceProblem(entries: todo.Where(predicate: e => e.Problem == null).ToList(), destination: destination);
            if (space != null)
            {
                batchProblems.Add(item: space);
            }
        }

        return batchProblems;
    }

    /// <summary>True if another program has the file open in a way that stops it being renamed or moved.</summary>
    private static bool IsInUse(string path)
    {
        try
        {
            // no sharing at all: succeeds only if nobody else has it open
            using FileStream stream = new(path: path, mode: FileMode.Open, access: FileAccess.Read, share: FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // no permission to read it is a different problem, and one a rename may not care about
            return false;
        }
    }

    /// <summary>Why files cannot be put in the folder, or null if they can (it is created if need be, and left if it was).</summary>
    private static string FolderProblem(string folder)
    {
        string probe = null;
        try
        {
            if (!Directory.Exists(path: folder))
            {
                _ = Directory.CreateDirectory(path: folder);
            }

            probe = Path.Combine(path1: folder, path2: ".gtn-write-test-" + Guid.NewGuid().ToString(format: "N"));
            using (File.Create(path: probe))
            {
            }

            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Log.Warn(exception: ex, message: $"Batch rename pre-flight: cannot write to '{folder}'");
            return BatchRenameText.Get(key: "mbx_BatchRename_Problem_FolderNotWritable", folder);
        }
        finally
        {
            if (probe != null)
            {
                try
                {
                    File.Delete(path: probe);
                }
                catch (Exception ex)
                {
                    Log.Warn(exception: ex, message: $"Could not remove the write-test file '{probe}'");
                }
            }
        }
    }

    /// <summary>
    ///     For copies and moves to another drive: whether the destination has room for everything. Null if it does (or if
    ///     that cannot be told, which is not a reason to stop).
    /// </summary>
    private static string DiskSpaceProblem(List<BatchRenameEntry> entries,
                                           BatchRenameDestination destination)
    {
        try
        {
            foreach (IGrouping<string, BatchRenameEntry> drive in entries.GroupBy(
                         keySelector: e => Path.GetPathRoot(path: e.NewPath) ?? string.Empty,
                         comparer: StringComparer.OrdinalIgnoreCase))
            {
                if (drive.Key.Length == 0 ||
                    drive.Key.StartsWith(value: @"\\", comparisonType: StringComparison.Ordinal))
                {
                    continue; // network shares do not report free space reliably
                }

                // a move within one drive takes no space; across drives (and every copy) it does
                long needed = drive.Where(predicate: e => destination == BatchRenameDestination.CopyToFolder ||
                                                          !string.Equals(a: Path.GetPathRoot(path: e.Source.FullPath), b: drive.Key,
                                                              comparisonType: StringComparison.OrdinalIgnoreCase))
                                   .Sum(selector: e => new FileInfo(fileName: e.Source.FullPath).Length +
                                                       (e.Source.HasSidecar && File.Exists(path: BatchRenamePlanner.SidecarOf(path: e.Source.FullPath))
                                                           ? new FileInfo(fileName: BatchRenamePlanner.SidecarOf(path: e.Source.FullPath)).Length
                                                           : 0));
                long free = new DriveInfo(driveName: drive.Key).AvailableFreeSpace;
                if (needed > free)
                {
                    return BatchRenameText.Get(key: "mbx_BatchRename_Problem_NoSpace", drive.Key, needed / (1024 * 1024), free / (1024 * 1024));
                }
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            Log.Warn(exception: ex, message: "Batch rename pre-flight: could not work out the free space");
        }

        return null;
    }
}
