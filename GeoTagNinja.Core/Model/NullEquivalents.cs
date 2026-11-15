using System;

namespace GeoTagNinja.Model;

/// <summary>
///     The values the app uses to stand for "nothing" in each type, so that a blank cell, an unset attribute and a value that
///     was never read can be told apart from real data. They used to live on the main form, which every layer then had to
///     reach into.
/// </summary>
/// <remarks>
///     The sentinel for strings is <see cref="StringGeneric" /> (<c>"-"</c>). <c>WriteSaveToFile</c> maps it to empty so it is
///     never written to a file.
/// </remarks>
internal static class NullEquivalents
{
    /// <summary>The "no value" string shown in the list and held in the model ("-").</summary>
    internal const string StringGeneric = "-";

    internal const string StringBlank = "";
    internal const string StringZero = "0";
    internal const int Int = 0;
    internal const double Double = 0.0;

    internal static readonly DateTime DateTime = new(year: 1, month: 1, day: 1, hour: 0, minute: 0, second: 0);
}
