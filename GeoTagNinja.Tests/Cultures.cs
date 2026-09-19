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
///         <item><c>en-AU</c> - day first, dot decimal, different short-date punctuation.</item>
///         <item><c>en-IN</c> - day first with dashes, and lakh/crore digit grouping rather than thousands.</item>
///         <item><c>es-AR</c> - day first, <b>comma decimal</b> and dot grouping: the exact inverse of invariant.</item>
///         <item><c>hu-HU</c> - year first with dots, comma decimal, and a space as the group separator.</item>
///         <item><c>fi-FI</c> - comma decimal, non-breaking space thousand separator (\u00A0), trailing dots in short dates (D.M.YYYY).</item>
///         <item><c>ar-SA</c> - Arabic digits & Hijri Calendar: Uses Eastern Arabic numerals (٠١٢٣٤٥٦٧٨٩) and non-Gregorian dates by default. Tests right-to-left (RTL) string handling.١٤٤٨/٠٤/٠٨</item>
///         <item><c>ar-EG</c> - RTL with Western Arabic digits: Tests right-to-left text placement with standard 0-9 numerals and custom decimal separators (٫ U+066B).١٬٢٣٤٫٥٦</item>
///         <item><c>de-CH</c> - Apostrophe Grouping: Uses an apostrophe (') as a thousand separator. Frequently breaks regex string cleansers or SQL escaping logic.1'234.56</item>
///         <item><c>fr-FR</c> - Leading/Trailing Negative Signs & Currency Position: Negative values or currencies often put symbols/signs after the number (1 234,56 - or 1 234,56 €).-1 234,56 €</item>
///         <item><c>ja-JP</c> - ISO-like Dates & Myriad Grouping (Kanji/Yen): Standard numeric representation uses thousands, but kanji representations switch to myriads ($10^4$ / Man). Dates use strict YYYY/MM/DD.2026/09/19</item>
///         <item><c>th-TH</c> - Buddhist Era (BE) Year Offsets: Uses the Buddhist calendar year (2026 + 543 = 2569). Will fail any date validation checking for reasonable 19xx/20xx year ranges.</item>
///     </list>
///     <para>
///         If a value survives all in both directions, it will survive the rest.
///     </para>
/// </remarks>
internal static class Cultures
{
    /// <summary>Culture names used with NUnit's <c>[TestCaseSource]</c>.</summary>
    internal static readonly string[] All =
    [
        "en-US",
        "en-GB",
        "en-AU",
        "en-IN",
        "es-AR",
        "hu-HU",
        "fi-FI",
        "ar-SA",
        "ar-EG",
        "de-CH",
        "fr-FR",
        "ja-JP",
        "th-TH",
    ];
}
