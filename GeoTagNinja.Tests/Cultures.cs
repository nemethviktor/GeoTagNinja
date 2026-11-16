namespace GeoTagNinja.Tests;

/// <summary>
///     The cultures every value-handling test is run under.
/// </summary>
/// <remarks>
///     <para>
///         These are not arbitrary. They are chosen so that between them they break every naive assumption this
///         application has historically made about how a number or a date looks:
///     </para>
///     <list type="bullet">
///         <item><c>en-US</c> - month first, dot decimal.</item>
///         <item><c>en-GB</c> - day first, dot decimal. The assumption the code was written under.</item>
///         <item><c>es-AR</c> - day first, <b>comma decimal</b> and dot grouping: the exact inverse of invariant.</item>
///         <item><c>fi-FI</c> - comma decimal, non-breaking space thousand separator ( ), trailing dots in short dates (D.M.YYYY).</item>
///         <item><c>ja-JP</c> - year first (YYYY/MM/DD).</item>
///         <item><c>ar-SA</c> - Eastern Arabic digits and the Hijri calendar by default; right-to-left text.</item>
///         <item><c>th-TH</c> - Buddhist Era year (2026 + 543 = 2569); fails any check for a "reasonable" 19xx/20xx year.</item>
///     </list>
///     <para>
///         If a value survives all of these in both directions, it will survive the rest. (An earlier, longer list added en-AU, en-IN, hu-HU, ar-EG, de-CH and fr-FR; none of them broke anything the seven below did not, so they were dropped to keep the suite small.)
///     </para>
/// </remarks>
internal static class Cultures
{
    /// <summary>Culture names used with NUnit's <c>[TestCaseSource]</c>.</summary>
    internal static readonly string[] All =
    [
        "en-US",
        "en-GB",
        "es-AR",
        "fi-FI",
        "ja-JP",
        "ar-SA",
        "th-TH",
    ];
}
