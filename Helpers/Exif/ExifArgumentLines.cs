using System;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     Builds the text of an ExifTool arguments file one line at a time, without adding the same line twice.
/// </summary>
/// <remarks>
///     The write path deliberately adds many arguments more than once (a tag is set early on and then again by the generic
///     step at the end; a delete is added and then added again with an empty value), so the file has to ignore repeats. It
///     used to do that by asking whether the whole text so far <i>contained</i> the new argument as a substring, which also
///     swallowed arguments that are merely similar: <c>-IPTC:City=Foo</c> was dropped if <c>-IPTC:City=Foobar</c> had been
///     added before it, and a file path was dropped if a longer path containing it had been. Only a line that is exactly the
///     same is a repeat; for two different values of one tag both lines stay, and ExifTool applies them in order, so the
///     later one wins.
/// </remarks>
internal static class ExifArgumentLines
{
    /// <summary>True if <paramref name="arguments" /> already has <paramref name="line" /> as a whole line.</summary>
    internal static bool ContainsLine(string arguments,
                                      string line)
    {
        if (string.IsNullOrEmpty(value: arguments))
        {
            return false;
        }

        // lines are separated by CRLF (Environment.NewLine on Windows); accept a bare LF too
        foreach (string existing in arguments.Split(separator: ['\n'], options: StringSplitOptions.None))
        {
            if (string.Equals(a: existing.TrimEnd('\r'), b: line, comparisonType: StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Appends <paramref name="line" /> unless it is already there as a whole line.
    /// </summary>
    /// <param name="arguments">The arguments text so far.</param>
    /// <param name="line">The argument to add (one line, without a line break).</param>
    /// <param name="addNewLine">Whether to end it with a line break (default true).</param>
    /// <returns>True if it was added, false if it was already there.</returns>
    internal static bool AddUnique(ref string arguments,
                                   string line,
                                   bool addNewLine = true)
    {
        if (ContainsLine(arguments: arguments, line: line))
        {
            return false;
        }

        arguments += line + (addNewLine
            ? Environment.NewLine
            : "");
        return true;
    }
}
