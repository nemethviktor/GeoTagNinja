using GeoTagNinja.Helpers.Localisation;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GeoTagNinja.Helpers.FileSystem;

/// <summary>What running a batch rename did.</summary>
internal sealed class BatchRenameResult
{
    /// <summary>All of the batch was done. If false, nothing was: whatever had been moved was put back.</summary>
    public bool Success { get; set; }

    /// <summary>Why it failed.</summary>
    public string Error { get; set; }

    /// <summary>The file whose move or copy failed, if that could be told (null: the batch failed as a whole).</summary>
    public BatchRenameEntry FailedEntry { get; set; }

    /// <summary>The entries that were renamed, moved or copied (empty if it failed).</summary>
    public List<BatchRenameEntry> Applied { get; set; } = [];
}

/// <summary>A move or copy of the batch failed; says which one. Thrown after everything done so far has been put back.</summary>
internal sealed class BatchRenameStepException : Exception
{
    /// <summary>What was being moved or copied.</summary>
    public string From { get; }

    /// <summary>Where it was going.</summary>
    public string To { get; }

    public BatchRenameStepException(string from,
                                    string to,
                                    Exception inner) : base(message: inner.Message, innerException: inner)
    {
        From = from;
        To = to;
    }
}

/// <summary>
///     Does what <see cref="BatchRenamePlanner" /> planned, on disk: the files and their XMP sidecars.
/// </summary>
/// <remarks>
///     <para>
///         All or nothing. A batch that is half done (some files renamed, a locked one not) is worse than none, so the
///         first failure puts back everything done so far. Names that swap or chain (a becomes b while b becomes c), and
///         renames that only change case, go through temporary names, because the target of one move is the source of
///         another.
///     </para>
///     <para>
///         A sidecar (<c>name.xmp</c>, the convention the file list reads) is moved with its photo. But a RAW+JPEG pair
///         shares one: if a file that still uses it stays behind (<see cref="BatchRenameSource.SidecarShared" />), or two
///         renamed files want different names for it, the sidecar is copied for them and the original is left, so no
///         file loses its sidecar.
///     </para>
///     <para>
///         When a step fails the result says which file it was (<see cref="BatchRenameResult.FailedEntry" />) so the
///         caller can offer to go on without it: <see cref="ExcludeWithDependents" /> marks it, and whatever depended on
///         it, as not to be done, and <see cref="Execute" /> can then be run again.
///     </para>
/// </remarks>
internal static class BatchRenameExecutor
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string TemporarySuffix = ".gtn-rename";

    /// <summary>Runs the plan. Entries that are <see cref="BatchRenameEntry.Unchanged" /> are skipped.</summary>
    /// <param name="entries">A plan with no <see cref="BatchRenameEntry.Problem" />.</param>
    /// <param name="destination">Whether files are moved or copied.</param>
    internal static BatchRenameResult Execute(IReadOnlyList<BatchRenameEntry> entries,
                                              BatchRenameDestination destination)
    {
        List<BatchRenameEntry> todo = entries.Where(predicate: entry => !entry.Unchanged).ToList();
        BatchRenameResult result = new();

        if (todo.Any(predicate: entry => entry.Problem != null))
        {
            result.Error = BatchRenameText.Get(key: "mbx_BatchRename_Problem_Unfixed");
            return result;
        }

        if (todo.Count == 0)
        {
            result.Success = true;
            return result;
        }

        bool copying = destination == BatchRenameDestination.CopyToFolder;

        // moves: each file (and each sidecar nobody else needs) once. copies: sidecars that must also stay where they are
        // or that two files want under different names; they are made after the moves, from where the sidecar is by then
        List<(string from, string to)> moves = [];
        List<(string from, string to)> copies = [];
        HashSet<string> filesDone = new(comparer: StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> sidecarMovedTo = new(comparer: StringComparer.OrdinalIgnoreCase);
        HashSet<string> copyPairs = new(comparer: StringComparer.OrdinalIgnoreCase);

        foreach (BatchRenameEntry entry in todo)
        {
            if (filesDone.Add(item: entry.Source.FullPath))
            {
                (copying ? copies : moves).Add(item: (entry.Source.FullPath, entry.NewPath));
            }

            if (!entry.Source.HasSidecar)
            {
                continue;
            }

            string sidecar = BatchRenamePlanner.SidecarOf(path: entry.Source.FullPath);
            if (!File.Exists(path: sidecar))
            {
                continue;
            }

            if (copying ||
                entry.Source.SidecarShared)
            {
                if (copyPairs.Add(item: sidecar + "|" + entry.NewSidecarPath))
                {
                    copies.Add(item: (sidecar, entry.NewSidecarPath));
                }
            }
            else if (!sidecarMovedTo.TryGetValue(key: sidecar, value: out string firstTarget))
            {
                sidecarMovedTo[key: sidecar] = entry.NewSidecarPath;
                moves.Add(item: (sidecar, entry.NewSidecarPath));
            }
            else if (!string.Equals(a: firstTarget, b: entry.NewSidecarPath, comparisonType: StringComparison.OrdinalIgnoreCase) &&
                     copyPairs.Add(item: firstTarget + "|" + entry.NewSidecarPath))
            {
                // the sidecar has already gone to the first file's new name: copy it from there
                copies.Add(item: (firstTarget, entry.NewSidecarPath));
            }
        }

        try
        {
            if (destination != BatchRenameDestination.SameFolder)
            {
                foreach (string folder in moves.Concat(second: copies)
                                               .Select(selector: op => Path.GetDirectoryName(path: op.to))
                                               .Distinct(comparer: StringComparer.OrdinalIgnoreCase))
                {
                    _ = Directory.CreateDirectory(path: folder);
                }
            }

            MoveAndCopy(moves: moves, copies: copies, viaTemporaryNames: todo.Any(predicate: entry => entry.NeedsTemporaryStep));
            result.Success = true;
            result.Applied = todo;
        }
        catch (BatchRenameStepException ex)
        {
            Log.Error(exception: ex.InnerException, message: $"Batch rename failed at '{ex.From}' and was rolled back");
            result.Error = $"{Path.GetFileName(path: ex.From)}: {ex.Message}";
            result.FailedEntry = todo.FirstOrDefault(predicate: entry => Matches(entry: entry, from: ex.From, to: ex.To));
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex, message: "Batch rename failed and was rolled back");
            result.Error = ex.Message;
        }

        return result;
    }

    private static bool Matches(BatchRenameEntry entry,
                                string from,
                                string to)
    {
        StringComparison same = StringComparison.OrdinalIgnoreCase;
        return string.Equals(a: entry.Source.FullPath, b: from, comparisonType: same) ||
               string.Equals(a: entry.NewPath, b: to, comparisonType: same) ||
               (entry.Source.HasSidecar &&
                (string.Equals(a: BatchRenamePlanner.SidecarOf(path: entry.Source.FullPath), b: from, comparisonType: same) ||
                 string.Equals(a: entry.NewSidecarPath, b: to, comparisonType: same)));
    }

    /// <summary>
    ///     Takes a failed file out of the batch (marks it <see cref="BatchRenameEntry.Unchanged" />, so it is left alone)
    ///     together with every file that was going to take the name it keeps: it is still in the way, so they would fail
    ///     too, and so on down the chain.
    /// </summary>
    /// <returns>All the entries taken out, the failed one first.</returns>
    internal static List<BatchRenameEntry> ExcludeWithDependents(IReadOnlyList<BatchRenameEntry> entries,
                                                                 BatchRenameEntry failed)
    {
        List<BatchRenameEntry> excluded = [];
        HashSet<string> staysPut = new(comparer: StringComparer.OrdinalIgnoreCase);

        void Exclude(BatchRenameEntry entry)
        {
            entry.Unchanged = true;
            excluded.Add(item: entry);
            _ = staysPut.Add(item: entry.Source.FullPath);
            if (entry.Source.HasSidecar)
            {
                _ = staysPut.Add(item: BatchRenamePlanner.SidecarOf(path: entry.Source.FullPath));
            }
        }

        Exclude(entry: failed);
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (BatchRenameEntry entry in entries.Where(predicate: e => !e.Unchanged))
            {
                if (staysPut.Contains(item: entry.NewPath) ||
                    (entry.Source.HasSidecar && staysPut.Contains(item: entry.NewSidecarPath)))
                {
                    Exclude(entry: entry);
                    grew = true;
                }
            }
        }

        return excluded;
    }

    private static void MoveAndCopy(List<(string from, string to)> moves,
                                    List<(string from, string to)> copies,
                                    bool viaTemporaryNames)
    {
        // every move made, in order, so a failure can undo them exactly backwards (undoing in any other order can find
        // a name still occupied, e.g. when a goes to b and b goes to c)
        List<(string from, string to)> log = [];
        List<string> created = [];

        // what is being done now, as the file's own source and target (not the temporary name), for the failure report
        (string from, string to) current = (string.Empty, string.Empty);

        void MoveOne(string from,
                     string to,
                     string reportFrom,
                     string reportTo)
        {
            current = (reportFrom, reportTo);
            File.Move(sourceFileName: from, destFileName: to);
            log.Add(item: (from, to));
        }

        try
        {
            if (viaTemporaryNames)
            {
                string token = Guid.NewGuid().ToString(format: "N");
                string[] temporary = new string[moves.Count];
                for (int index = 0; index < moves.Count; index++)
                {
                    temporary[index] = moves[index: index].from + "." + token + index + TemporarySuffix;
                    MoveOne(from: moves[index: index].from, to: temporary[index], reportFrom: moves[index: index].from,
                        reportTo: moves[index: index].to);
                }

                for (int index = 0; index < moves.Count; index++)
                {
                    MoveOne(from: temporary[index], to: moves[index: index].to, reportFrom: moves[index: index].from,
                        reportTo: moves[index: index].to);
                }
            }
            else
            {
                foreach ((string from, string to) in moves)
                {
                    MoveOne(from: from, to: to, reportFrom: from, reportTo: to);
                }
            }

            foreach ((string from, string to) in copies)
            {
                current = (from, to);
                File.Copy(sourceFileName: from, destFileName: to, overwrite: false);
                created.Add(item: to);
            }
        }
        catch (Exception failure)
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                TryDelete(path: created[index: index]);
            }

            for (int index = log.Count - 1; index >= 0; index--)
            {
                try
                {
                    File.Move(sourceFileName: log[index: index].to, destFileName: log[index: index].from);
                }
                catch (Exception ex)
                {
                    Log.Error(exception: ex, message: $"Could not put '{log[index: index].from}' back after a failed batch rename");
                }
            }

            throw new BatchRenameStepException(from: current.from, to: current.to, inner: failure);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path: path);
        }
        catch (Exception ex)
        {
            Log.Warn(exception: ex, message: $"Could not remove the partial copy '{path}'");
        }
    }
}
