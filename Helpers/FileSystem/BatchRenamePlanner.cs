using GeoTagNinja.Helpers.Localisation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GeoTagNinja.Helpers.FileSystem;

/// <summary>What the planner needs to know about one file. Plain data, so the planner can be tested without files.</summary>
internal sealed class BatchRenameSource
{
    /// <summary>The file's full path as it is now.</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>The name of the folder the file is in.</summary>
    public string FolderName { get; set; } = string.Empty;

    /// <summary>The file has an XMP sidecar (<c>name.xmp</c>) that has to follow it.</summary>
    public bool HasSidecar { get; set; }

    /// <summary>
    ///     Another file that is not being renamed (the JPEG of a RAW+JPEG pair, say) uses the same <c>name.xmp</c>, so
    ///     the sidecar must stay where it is and the file gets a copy.
    /// </summary>
    public bool SidecarShared { get; set; }

    /// <summary>XMP <c>PreservedFileName</c>, if the file has one.</summary>
    public string PreservedFileName { get; set; }

    /// <summary>Date taken, if known.</summary>
    public DateTime? DateTaken { get; set; }

    /// <summary>Date created (the metadata's), if known.</summary>
    public DateTime? DateCreated { get; set; }

    /// <summary>When the file was created on disk, if known.</summary>
    public DateTime? FileCreated { get; set; }

    /// <summary>When the file was last modified on disk, if known.</summary>
    public DateTime? FileModified { get; set; }

    /// <summary>Metadata as text, by attribute name (what <see cref="BatchRenameKind.Metadata" /> picks from).</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>The caller's own handle on the file (the list row, say); never looked at here.</summary>
    public object Tag { get; set; }
}

/// <summary>What will happen to one file.</summary>
internal sealed class BatchRenameEntry
{
    /// <summary>The file.</summary>
    public BatchRenameSource Source { get; set; }

    /// <summary>The new name with its extension (what the user sees); empty if it could not be worked out.</summary>
    public string NewFileName { get; set; } = string.Empty;

    /// <summary>The new full path.</summary>
    public string NewPath { get; set; } = string.Empty;

    /// <summary>The new path of the sidecar, if the file has one.</summary>
    public string NewSidecarPath { get; set; } = string.Empty;

    /// <summary>The file keeps its name and place: nothing is done to it.</summary>
    public bool Unchanged { get; set; }

    /// <summary>
    ///     What stops this file being renamed (empty name, clash, illegal name...), or null. While any entry has one the
    ///     batch should not be run.
    /// </summary>
    public string Problem { get; set; }

    /// <summary>
    ///     The rename cannot be done in one step: the new name is another file's current name, or differs only in case
    ///     (Windows sees that as the same file).
    /// </summary>
    public bool NeedsTemporaryStep { get; set; }
}

/// <summary>
///     Works out the new name of every file of a batch rename and what would go wrong. Pure: the only way it looks at the
///     disk is the <c>pathExists</c> callback.
/// </summary>
internal static class BatchRenamePlanner
{
    // ExifTool, which this app uses for everything, cannot open a path of 260 characters or more.
    private const int MaxPathLength = 259;
    private const int MaxFileNameLength = 255;

    private static readonly char[] WindowsIllegal = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    private static readonly HashSet<string> ReservedNames = new(comparer: StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(value: 2);

    /// <summary>
    ///     Plans the batch. The files are numbered in the order given.
    /// </summary>
    /// <param name="sources">The files, in the order they are listed.</param>
    /// <param name="settings">What the user asked for.</param>
    /// <param name="pathExists">Whether a file exists on disk.</param>
    /// <param name="now">The moment "Today" means (and "Yesterday" is the day before).</param>
    /// <returns>One entry per source, in the same order.</returns>
    internal static List<BatchRenameEntry> Plan(IReadOnlyList<BatchRenameSource> sources,
                                                BatchRenameSettings settings,
                                                Func<string, bool> pathExists,
                                                DateTime now)
    {
        List<BatchRenameEntry> entries = [];
        for (int index = 0; index < sources.Count; index++)
        {
            entries.Add(item: PlanOne(source: sources[index: index], index: index, settings: settings, now: now));
        }

        MarkClashes(entries: entries, settings: settings, pathExists: pathExists);
        return entries;
    }

    private static BatchRenameEntry PlanOne(BatchRenameSource source,
                                            int index,
                                            BatchRenameSettings settings,
                                            DateTime now)
    {
        BatchRenameEntry entry = new() { Source = source };
        string currentName = Path.GetFileName(path: source.FullPath);
        string baseName = Path.GetFileNameWithoutExtension(path: source.FullPath);
        string extension = Path.GetExtension(path: source.FullPath);

        string folder;
        switch (settings.Destination)
        {
            case BatchRenameDestination.SameFolder:
                folder = Path.GetDirectoryName(path: source.FullPath) ?? string.Empty;
                break;
            default:
                folder = settings.DestinationFolder?.Trim() ?? string.Empty;
                if (folder.Length == 0 ||
                    !Path.IsPathRooted(path: folder))
                {
                    entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_NoDestination");
                    entry.NewFileName = currentName;
                    return entry;
                }

                break;
        }

        StringBuilder name = new();
        foreach (BatchRenameComponent component in settings.Components)
        {
            if (component.Kind == BatchRenameKind.NewExtension)
            {
                extension = ApplyCase(text: extension, mode: component.Case);
                continue;
            }

            string piece = Piece(component: component, source: source, index: index, baseName: baseName, now: now,
                problem: out string problem);
            if (problem != null)
            {
                entry.Problem = problem;
                break;
            }

            _ = name.Append(value: piece);
        }

        string newBase = Sanitise(name: name.ToString(), settings: settings);
        if (entry.Problem == null &&
            newBase.Length == 0)
        {
            entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_EmptyName");
        }

        if (entry.Problem == null &&
            ReservedNames.Contains(item: newBase))
        {
            entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_Reserved", newBase);
        }

        entry.NewFileName = entry.Problem != null && newBase.Length == 0
            ? currentName
            : newBase + extension;

        if (entry.Problem == null &&
            entry.NewFileName.Length > MaxFileNameLength)
        {
            entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_NameTooLong");
        }

        entry.NewPath = Path.Combine(path1: folder, path2: entry.NewFileName);
        if (entry.Problem == null &&
            entry.NewPath.Length > MaxPathLength)
        {
            entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_PathTooLong");
        }

        if (source.HasSidecar)
        {
            entry.NewSidecarPath = Path.Combine(path1: folder, path2: Path.GetFileNameWithoutExtension(path: entry.NewFileName) + ".xmp");
        }

        entry.Unchanged = entry.Problem == null &&
                          settings.Destination != BatchRenameDestination.CopyToFolder &&
                          string.Equals(a: entry.NewPath, b: source.FullPath, comparisonType: StringComparison.Ordinal);
        return entry;
    }

    /// <summary>The text one component adds to the name; <paramref name="problem" /> says why it cannot.</summary>
    private static string Piece(BatchRenameComponent component,
                                BatchRenameSource source,
                                int index,
                                string baseName,
                                DateTime now,
                                out string problem)
    {
        problem = null;
        switch (component.Kind)
        {
            case BatchRenameKind.Text:
                return component.Text ?? string.Empty;

            case BatchRenameKind.CurrentFilename:
                return ApplyCase(text: FilePart(fileName: Path.GetFileName(path: source.FullPath), part: component.Part), mode: component.Case);

            case BatchRenameKind.PreservedFilename:
                return ApplyCase(text: FilePart(fileName: PreservedName(source: source) ?? Path.GetFileName(path: source.FullPath), part: component.Part),
                    mode: component.Case);

            case BatchRenameKind.FolderName:
                return ApplyCase(text: source.FolderName ?? string.Empty, mode: component.Case);

            case BatchRenameKind.SequenceNumber:
                int digits = Math.Max(val1: 1, val2: Math.Min(val1: 9, val2: component.Digits));
                // invariant: Arabic-Indic digits and the like must never end up in a file name
                return ((long)component.Start + index).ToString(format: "D" + digits.ToString(provider: CultureInfo.InvariantCulture),
                    provider: CultureInfo.InvariantCulture);

            case BatchRenameKind.SequenceLetter:
                return ApplyCase(text: LetterSequence(number: index + 1), mode: component.Case == BatchRenameCase.Upper
                    ? BatchRenameCase.Upper
                    : BatchRenameCase.Lower);

            case BatchRenameKind.DateTime:
                return DatePiece(component: component, source: source, now: now, problem: out problem);

            case BatchRenameKind.Metadata:
                if (source.Metadata != null &&
                    component.Text != null &&
                    source.Metadata.TryGetValue(key: component.Text, value: out string value) &&
                    value != null &&
                    value.Trim() != "-")
                {
                    return value.Trim();
                }

                return string.Empty;

            case BatchRenameKind.StringSubstitution:
                string subject = component.SubstituteSource == BatchRenameSubstituteSource.PreservedFilename
                    ? Path.GetFileNameWithoutExtension(path: PreservedName(source: source) ?? source.FullPath)
                    : baseName;
                return Substitute(subject: subject, component: component, problem: out problem);

            default:
                return string.Empty;
        }
    }

    private static string PreservedName(BatchRenameSource source)
    {
        return string.IsNullOrWhiteSpace(value: source.PreservedFileName)
            ? null
            : Path.GetFileName(path: source.PreservedFileName.Trim());
    }

    /// <summary>One part of a file name (see <see cref="BatchRenameFilePart" />).</summary>
    internal static string FilePart(string fileName,
                                    BatchRenameFilePart part)
    {
        string name = Path.GetFileNameWithoutExtension(path: fileName);
        switch (part)
        {
            case BatchRenameFilePart.NameAndExtension:
                return fileName;
            case BatchRenameFilePart.Extension:
                return Path.GetExtension(path: fileName).TrimStart('.');
            case BatchRenameFilePart.NumberSuffix:
                int start = name.Length;
                while (start > 0 &&
                       name[index: start - 1] is >= '0' and <= '9')
                {
                    start--;
                }

                return name.Substring(startIndex: start);
            default:
                return name;
        }
    }

    private static string DatePiece(BatchRenameComponent component,
                                    BatchRenameSource source,
                                    DateTime now,
                                    out string problem)
    {
        problem = null;
        DateTime? date = component.DateSource switch
        {
            BatchRenameDateSource.DateTaken => source.DateTaken,
            BatchRenameDateSource.DateCreated => source.DateCreated,
            BatchRenameDateSource.FileCreated => source.FileCreated,
            BatchRenameDateSource.FileModified => source.FileModified,
            BatchRenameDateSource.Yesterday => now.AddDays(value: -1),
            _ => now
        };

        if (date == null)
        {
            problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_NoDate",
                BatchRenameText.Get(key: $"lbl_BatchRename_DateSource_{component.DateSource}"));
            return string.Empty;
        }

        return FormatDate(date: date.Value,
            format: string.IsNullOrEmpty(value: component.Text)
                ? "YYYYMMDD"
                : component.Text);
    }

    /// <summary>
    ///     Writes a date using Bridge's tokens: <c>YYYY</c>, <c>YY</c>, <c>MM</c>, <c>DD</c>, <c>hh</c> (24-hour), <c>mm</c>
    ///     and <c>ss</c>; every other character is copied as it is. Done by hand rather than with .NET's date formats so
    ///     that the result is the same in every culture and calendar (Thai years, Arabic digits...) and nothing in the
    ///     format can be taken for a standard-format letter.
    /// </summary>
    internal static string FormatDate(DateTime date,
                                      string format)
    {
        (string token, int value, string digits)[] tokens =
        [
            ("YYYY", date.Year, "D4"),
            ("YY", date.Year % 100, "D2"),
            ("MM", date.Month, "D2"),
            ("DD", date.Day, "D2"),
            ("hh", date.Hour, "D2"),
            ("mm", date.Minute, "D2"),
            ("ss", date.Second, "D2")
        ];

        StringBuilder text = new();
        int i = 0;
        while (i < format.Length)
        {
            bool matched = false;
            foreach ((string token, int value, string digits) in tokens)
            {
                if (string.CompareOrdinal(strA: format, indexA: i, strB: token, indexB: 0, length: token.Length) == 0)
                {
                    _ = text.Append(value: value.ToString(format: digits, provider: CultureInfo.InvariantCulture));
                    i += token.Length;
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                _ = text.Append(value: format[index: i]);
                i++;
            }
        }

        return text.ToString();
    }

    private static string Substitute(string subject,
                                     BatchRenameComponent component,
                                     out string problem)
    {
        problem = null;
        if (string.IsNullOrEmpty(value: component.Find))
        {
            return subject;
        }

        try
        {
            RegexOptions options = RegexOptions.CultureInvariant;
            if (component.IgnoreCase)
            {
                options |= RegexOptions.IgnoreCase;
            }

            string pattern = component.UseRegex
                ? component.Find
                : Regex.Escape(str: component.Find);
            string replacement = component.UseRegex
                ? component.Replace ?? string.Empty
                : (component.Replace ?? string.Empty).Replace(oldValue: "$", newValue: "$$");

            Regex regex = new(pattern: pattern, options: options, matchTimeout: RegexTimeout);
            return component.ReplaceAll
                ? regex.Replace(input: subject, replacement: replacement)
                : regex.Replace(input: subject, replacement: replacement, count: 1);
        }
        catch (ArgumentException)
        {
            problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_BadRegex");
            return subject;
        }
        catch (RegexMatchTimeoutException)
        {
            problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_RegexTimeout");
            return subject;
        }
    }

    /// <summary>1 gives a, 26 gives z, 27 gives aa (the way spreadsheet columns count).</summary>
    internal static string LetterSequence(int number)
    {
        StringBuilder letters = new();
        while (number > 0)
        {
            number--;
            _ = letters.Insert(index: 0, value: (char)('a' + (number % 26)));
            number /= 26;
        }

        return letters.ToString();
    }

    /// <summary>Upper/lower-cases with the invariant culture (a Turkish "i" must not become "İ").</summary>
    internal static string ApplyCase(string text,
                                     BatchRenameCase mode)
    {
        return mode switch
        {
            BatchRenameCase.Lower => text.ToLowerInvariant(),
            BatchRenameCase.Upper => text.ToUpperInvariant(),
            _ => text
        };
    }

    /// <summary>
    ///     Makes a name (without extension) legal: characters Windows forbids become underscores, control characters go,
    ///     trailing spaces and full stops go, and for macOS/Unix a leading full stop (a hidden file) goes too.
    /// </summary>
    internal static string Sanitise(string name,
                                    BatchRenameSettings settings)
    {
        StringBuilder clean = new(capacity: name.Length);
        foreach (char c in name)
        {
            if (char.IsControl(c: c))
            {
                continue;
            }

            _ = clean.Append(value: Array.IndexOf(array: WindowsIllegal, value: c) >= 0
                ? '_'
                : c);
        }

        string result = clean.ToString().TrimEnd(' ', '.').TrimStart(' ');
        if (settings.MacCompatible ||
            settings.UnixCompatible)
        {
            result = result.TrimStart('.');
        }

        return result;
    }

    /// <summary>Flags the entries that would overwrite something or each other, and the ones needing a temporary step.</summary>
    private static void MarkClashes(List<BatchRenameEntry> entries,
                                    BatchRenameSettings settings,
                                    Func<string, bool> pathExists)
    {
        bool vacates = settings.Destination != BatchRenameDestination.CopyToFolder;

        // what the batch itself moves away from: a name that is free again once its owner has gone
        HashSet<string> vacatedFiles = new(comparer: StringComparer.OrdinalIgnoreCase);
        HashSet<string> vacatedSidecars = new(comparer: StringComparer.OrdinalIgnoreCase);
        if (vacates)
        {
            foreach (BatchRenameEntry entry in entries.Where(predicate: e => !e.Unchanged && e.Problem == null))
            {
                _ = vacatedFiles.Add(item: entry.Source.FullPath);
                if (entry.Source.HasSidecar &&
                    !entry.Source.SidecarShared)
                {
                    _ = vacatedSidecars.Add(item: SidecarOf(path: entry.Source.FullPath));
                }
            }
        }

        // names that stay put are taken as well
        HashSet<string> staying = new(comparer: StringComparer.OrdinalIgnoreCase);
        foreach (BatchRenameEntry entry in entries.Where(predicate: e => e.Unchanged))
        {
            _ = staying.Add(item: entry.NewPath);
        }

        HashSet<string> claimed = new(comparer: StringComparer.OrdinalIgnoreCase);
        foreach (BatchRenameEntry entry in entries.Where(predicate: e => !e.Unchanged && e.Problem == null))
        {
            if (staying.Contains(item: entry.NewPath) ||
                !claimed.Add(item: entry.NewPath))
            {
                entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_NameClaimed");
                continue;
            }

            if (pathExists(arg: entry.NewPath) &&
                !vacatedFiles.Contains(item: entry.NewPath))
            {
                entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_FileExists");
                continue;
            }

            if (entry.Source.HasSidecar &&
                pathExists(arg: entry.NewSidecarPath) &&
                !vacatedSidecars.Contains(item: entry.NewSidecarPath) &&
                !string.Equals(a: entry.NewSidecarPath, b: SidecarOf(path: entry.Source.FullPath), comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                entry.Problem = BatchRenameText.Get(key: "mbx_BatchRename_Problem_SidecarExists");
                continue;
            }

            entry.NeedsTemporaryStep = vacates &&
                                       (vacatedFiles.Contains(item: entry.NewPath) ||
                                        (entry.Source.HasSidecar && vacatedSidecars.Contains(item: entry.NewSidecarPath)));
        }
    }

    /// <summary>The sidecar of a file: the same name with an <c>.xmp</c> extension.</summary>
    internal static string SidecarOf(string path)
    {
        return Path.Combine(path1: Path.GetDirectoryName(path: path) ?? string.Empty,
            path2: Path.GetFileNameWithoutExtension(path: path) + ".xmp");
    }
}
