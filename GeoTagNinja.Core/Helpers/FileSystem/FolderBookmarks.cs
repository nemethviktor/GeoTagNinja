using System;
using System.Collections.Generic;

namespace GeoTagNinja.Helpers.FileSystem;

/// <summary>
///     The user's favourite folders (bookmarks): an ordered list of folder paths, with no duplicates. Pure logic with no
///     file system, database or UI, so the WinForms folder tree and the Avalonia one share it. Storage is the caller's
///     business: <see cref="Serialise" /> and <see cref="Parse" /> turn the list into one string and back.
/// </summary>
internal sealed class FolderBookmarks
{
    private readonly List<string> _paths = [];

    /// <summary>The bookmarked folders, in the order the user added them.</summary>
    internal IReadOnlyList<string> Paths => _paths;

    /// <summary>
    ///     A path in the one form used for comparing and storing: no trailing separator, except on a drive root
    ///     (<c>C:\</c>), where it is what makes the path a folder rather than "the current directory of drive C".
    ///     Returns an empty string for a blank path.
    /// </summary>
    internal static string Normalise(string path)
    {
        if (string.IsNullOrWhiteSpace(value: path))
        {
            return string.Empty;
        }

        string trimmed = path.Trim().TrimEnd('\\', '/');
        return trimmed.Length switch
        {
            0 => path.Trim().Substring(startIndex: 0, length: 1), // "\" or "/" alone
            2 when trimmed[index: 1] == ':' => trimmed + "\\",   // "C:" -> "C:\"
            _ => trimmed
        };
    }

    /// <summary>True if the folder is bookmarked (case-insensitive, as Windows paths are).</summary>
    internal bool Contains(string path) => IndexOf(path: path) >= 0;

    /// <summary>Adds a folder at the end. Returns false (and changes nothing) if it is blank or already there.</summary>
    internal bool Add(string path)
    {
        string normalised = Normalise(path: path);
        if (normalised.Length == 0 || Contains(path: normalised))
        {
            return false;
        }

        _paths.Add(item: normalised);
        return true;
    }

    /// <summary>Removes a folder. Returns false if it was not bookmarked.</summary>
    internal bool Remove(string path)
    {
        int index = IndexOf(path: path);
        if (index < 0)
        {
            return false;
        }

        _paths.RemoveAt(index: index);
        return true;
    }

    /// <summary>One path per line (a path cannot contain a line break), for storing in a single setting.</summary>
    internal string Serialise() => string.Join(separator: "\n", values: _paths);

    /// <summary>The inverse of <see cref="Serialise" />; blank lines and repeats are dropped, so a hand-edited value is safe.</summary>
    internal static FolderBookmarks Parse(string text)
    {
        FolderBookmarks bookmarks = new();
        foreach (string line in (text ?? string.Empty).Split(separator: ['\n', '\r'],
                     options: StringSplitOptions.RemoveEmptyEntries))
        {
            _ = bookmarks.Add(path: line);
        }

        return bookmarks;
    }

    private int IndexOf(string path)
    {
        string normalised = Normalise(path: path);
        return normalised.Length == 0
            ? -1
            : _paths.FindIndex(match: p => string.Equals(a: p, b: normalised, comparisonType: StringComparison.OrdinalIgnoreCase));
    }
}
