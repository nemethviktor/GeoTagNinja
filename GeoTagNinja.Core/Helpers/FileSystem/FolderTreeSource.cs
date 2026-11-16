using System;
using System.Collections.Generic;
using System.IO;

namespace GeoTagNinja.Helpers.FileSystem;

/// <summary>
///     What a folder tree shows: the roots (drives), the sub-folders of a folder, and the chain of folders leading to a
///     path (so a tree can expand down to it). No UI types, so the WinForms and Avalonia trees both build on it.
/// </summary>
internal static class FolderTreeSource
{
    /// <summary>One entry in the tree: the label to show and the full path it stands for.</summary>
    internal readonly struct FolderEntry
    {
        internal FolderEntry(string Name,
                             string FullPath)
        {
            this.Name = Name;
            this.FullPath = FullPath;
        }

        internal string Name { get; }
        internal string FullPath { get; }
    }

    /// <summary>The ready drives (<c>C:\</c>, <c>D:\</c>...). Disconnected network drives and empty card readers are left out.</summary>
    internal static IReadOnlyList<FolderEntry> GetRoots()
    {
        List<FolderEntry> roots = [];
        try
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady)
                    {
                        string letter = drive.Name.TrimEnd('\\');
                        string label = string.IsNullOrWhiteSpace(value: drive.VolumeLabel)
                            ? letter
                            : $"{drive.VolumeLabel} ({letter})";
                        roots.Add(item: new FolderEntry(Name: label, FullPath: drive.RootDirectory.FullName));
                    }
                }
                catch (IOException)
                {
                    // a drive that went away while we were asking
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // nothing sensible to show; the caller gets what it has
        }

        return roots;
    }

    /// <summary>
    ///     The visible sub-folders of a folder, sorted the way Explorer does (natural, case-insensitive). Hidden and system
    ///     folders (<c>$RECYCLE.BIN</c>, <c>System Volume Information</c>) are skipped, as is anything that cannot be read.
    ///     An unreadable folder simply has no children.
    /// </summary>
    internal static IReadOnlyList<FolderEntry> GetChildren(string path)
    {
        List<FolderEntry> children = [];
        try
        {
            foreach (DirectoryInfo directory in new DirectoryInfo(path: path).EnumerateDirectories())
            {
                try
                {
                    if ((directory.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    {
                        continue;
                    }

                    children.Add(item: new FolderEntry(Name: directory.Name, FullPath: directory.FullName));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // skip just this one
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return [];
        }

        children.Sort(comparison: (a,
                                   b) => NaturalCompare(a: a.Name, b: b.Name));
        return children;
    }

    /// <summary>
    ///     The folders from the drive root down to <paramref name="path" />, inclusive: for <c>E:\Pics\2025\India</c> that
    ///     is <c>E:\</c>, <c>E:\Pics</c>, <c>E:\Pics\2025</c>, <c>E:\Pics\2025\India</c>. Empty for a blank, relative or UNC
    ///     path (the tree lists drives only).
    /// </summary>
    internal static IReadOnlyList<string> AncestorChain(string path)
    {
        string normalised = FolderBookmarks.Normalise(path: path);
        if (normalised.Length < 3 || normalised[index: 1] != ':' || normalised[index: 2] != '\\')
        {
            return [];
        }

        string current = normalised.Substring(startIndex: 0, length: 3);
        List<string> chain = [current];
        foreach (string part in normalised.Substring(startIndex: 3)
                    .Split(separator: ['\\'], options: StringSplitOptions.RemoveEmptyEntries))
        {
            current = current.EndsWith(value: "\\", comparisonType: StringComparison.Ordinal)
                ? current + part
                : current + "\\" + part;
            chain.Add(item: current);
        }

        return chain;
    }

    /// <summary>
    ///     Case-insensitive comparison in which runs of digits compare by value, so <c>Trip 2</c> sorts before
    ///     <c>Trip 10</c>. Ordinal (not culture-aware) on purpose: the order must not depend on the user's locale.
    /// </summary>
    internal static int NaturalCompare(string a,
                                       string b)
    {
        int i = 0;
        int j = 0;
        while (i < a.Length && j < b.Length)
        {
            if (char.IsDigit(c: a[index: i]) && char.IsDigit(c: b[index: j]))
            {
                int startA = i;
                int startB = j;
                while (i < a.Length && char.IsDigit(c: a[index: i]))
                {
                    i++;
                }

                while (j < b.Length && char.IsDigit(c: b[index: j]))
                {
                    j++;
                }

                string numberA = a.Substring(startIndex: startA, length: i - startA).TrimStart('0');
                string numberB = b.Substring(startIndex: startB, length: j - startB).TrimStart('0');
                int byLength = numberA.Length.CompareTo(value: numberB.Length);
                if (byLength != 0)
                {
                    return byLength;
                }

                int byValue = string.CompareOrdinal(strA: numberA, strB: numberB);
                if (byValue != 0)
                {
                    return byValue;
                }
            }
            else
            {
                int byChar = char.ToUpperInvariant(c: a[index: i]).CompareTo(value: char.ToUpperInvariant(c: b[index: j]));
                if (byChar != 0)
                {
                    return byChar;
                }

                i++;
                j++;
            }
        }

        return (a.Length - i).CompareTo(value: b.Length - j);
    }
}
