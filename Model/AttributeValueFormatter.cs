#nullable enable
using System;
using System.Globalization;

namespace GeoTagNinja.Model;

/// <summary>
///     Identifies <i>why</i> an attribute value is being converted between its typed (in-memory) representation and a
///     string, and therefore which formatting rules apply.
/// </summary>
/// <remarks>
///     <para>
///         Historically GeoTagNinja formatted values ad-hoc at each call site, with the result that the very same
///         attribute could be rendered with the invariant culture in one place, the current culture in another, and an
///         ExifTool-specific layout in a third. For dates in particular that meant a value written to a file, a value
///         shown on screen and a value copied between two files were not guaranteed to survive a round trip on a
///         non-English machine.
///     </para>
///     <para>
///         Every conversion now states its intent through this enum, and <see cref="AttributeValueFormatter" /> owns
///         the actual rules. There is exactly one place to look when a value comes out wrong.
///     </para>
/// </remarks>
public enum ValueFormatContext
{
    /// <summary>
    ///     The text is going to be shown to (or was typed by) a human: list view cells, text boxes, labels, tool tips.
    ///     Rendered with <see cref="CultureInfo.CurrentCulture" /> so that dates look native to the user.
    ///     <b>Never</b> persist a value formatted this way - it is lossy in several cultures.
    /// </summary>
    Display,

    /// <summary>
    ///     The text is an intermediate, machine-readable carrier that this application will parse again itself:
    ///     copy/paste pools, moving a value from one <see cref="DirectoryElement.AttributeVersion" /> to another,
    ///     caching, or stuffing a value into a <see cref="System.Data.DataTable" />.
    ///     Rendered culture-invariantly so the round trip is exact regardless of the machine's locale.
    /// </summary>
    RoundTrip,

    /// <summary>
    ///     The text is going to be handed to ExifTool as a tag value (an <c>-Tag=Value</c> argument).
    ///     Rendered culture-invariantly, using the canonical layouts ExifTool accepts.
    /// </summary>
    ExifTool
}

/// <summary>
///     The single authority for converting GeoTagNinja attribute values between their typed in-memory representation
///     (<see cref="string" />, <see cref="int" />, <see cref="double" /> and <see cref="DateTime" />) and text.
/// </summary>
/// <remarks>
///     <para>
///         <b>The rule of the house:</b> values live in memory as the type declared by
///         <see cref="SourcesAndAttributes.GetElementAttributesType" />. They only become strings at the edges - the
///         UI, the ExifTool argument file, the clipboard - and when they do, the caller must say which edge it is via
///         <see cref="ValueFormatContext" />.
///     </para>
///     <para>
///         <b>Dates.</b> ExifTool's native output is <c>YYYY:MM:DD hh:mm:ss</c>. GeoTagNinja ships a
///         <c>.ExifTool_config</c> that overrides this to <c>%Y-%m-%d %H:%M:%S</c>, so in practice the application
///         sees <c>YYYY-MM-DD hh:mm:ss</c> - but only for as long as ExifTool actually finds that config file. The
///         parser below therefore accepts both layouts (plus ISO-8601, optional sub-seconds and optional UTC offsets),
///         so date handling no longer silently depends on a Perl config file being picked up.
///     </para>
///     <para>
///         <b>Numbers.</b> Doubles and ints are deliberately formatted culture-invariantly in <i>all</i> contexts,
///         including <see cref="ValueFormatContext.Display" />. Coordinates, altitudes and f-numbers are round-tripped
///         through the map's JavaScript bridge, <see cref="System.Windows.Forms.NumericUpDown" /> controls and the
///         SQLite settings store, all of which expect a dot as the decimal separator. Localising them is a deliberate
///         product decision rather than a formatting one, and is not made here.
///     </para>
/// </remarks>
public static class AttributeValueFormatter
{
    /// <summary>
    ///     The canonical, culture-independent date/time layout used whenever a <see cref="DateTime" /> has to survive a
    ///     round trip: ExifTool arguments, the copy/paste pool and inter-version transfers.
    /// </summary>
    /// <remarks>
    ///     ExifTool accepts this layout on write and - thanks to the bundled <c>.ExifTool_config</c> - also emits it on
    ///     read, which is why the same constant serves both directions.
    /// </remarks>
    public const string CanonicalDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    ///     Every date/time layout GeoTagNinja is prepared to read, in descending order of likelihood.
    /// </summary>
    /// <remarks>
    ///     The first two entries cover the overwhelming majority of cases: the layout produced by the bundled ExifTool
    ///     config, and ExifTool's own default for when that config was not loaded. The rest guard against sidecars,
    ///     track files and third-party writers that use ISO-8601, sub-seconds or explicit offsets.
    /// </remarks>
    private static readonly string[] MachineDateTimeFormats =
    [
        "yyyy-MM-dd HH:mm:ss",
        "yyyy:MM:dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF",
        "yyyy:MM:dd HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd HH:mm:sszzz",
        "yyyy:MM:dd HH:mm:sszzz",
        "yyyy-MM-dd HH:mm:ss.FFFFFFFzzz",
        "yyyy:MM:dd HH:mm:ss.FFFFFFFzzz",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz",
        "yyyy-MM-dd HH:mm",
        "yyyy:MM:dd HH:mm",
        "yyyy-MM-dd",
        "yyyy:MM:dd"
    ];

    /// <summary>
    ///     Styles used when parsing machine-readable dates. Surrounding whitespace is tolerated and an explicit offset
    ///     is honoured, but the result is deliberately <i>not</i> converted to local time: EXIF timestamps are
    ///     wall-clock values and shifting them by the operator's time zone would corrupt them.
    /// </summary>
    private const DateTimeStyles MachineDateTimeStyles =
        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault;

    /// <summary>
    ///     Number styles for reading a machine-generated number invariantly.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The important part is what is <b>missing</b>: <see cref="NumberStyles.AllowThousands" />. The obvious
    ///         choice, <see cref="NumberStyles.Any" />, includes it, and the invariant group separator is a comma - so
    ///         <c>double.Parse("5,4", NumberStyles.Any, InvariantCulture)</c> quietly returns <c>54</c> rather than
    ///         failing. .NET does not validate group sizes, so even <c>-33,8688</c> is accepted, as -338688.
    ///     </para>
    ///     <para>
    ///         That is how a coordinate typed by a user whose decimal separator is a comma ended up four orders of
    ///         magnitude out, placing the photo in the wrong hemisphere. Excluding grouping means such a value fails
    ///         the invariant parse instead, and falls through to being read in the user's own culture - which is what
    ///         they meant. ExifTool never emits digit grouping, so nothing legitimate is lost.
    ///     </para>
    /// </remarks>
    private const NumberStyles MachineNumberStyles = NumberStyles.Float;

    #region DateTime

    /// <summary>
    ///     Renders a <see cref="DateTime" /> for the given context.
    /// </summary>
    /// <param name="value">The value to render.</param>
    /// <param name="context">Why the value is being rendered - see <see cref="ValueFormatContext" />.</param>
    /// <returns>
    ///     For <see cref="ValueFormatContext.Display" />, the value in the current culture's general date/time format;
    ///     otherwise the value in <see cref="CanonicalDateTimeFormat" />.
    /// </returns>
    public static string FormatDateTime(DateTime value,
                                        ValueFormatContext context)
    {
        return context == ValueFormatContext.Display
            ? value.ToString(provider: CultureInfo.CurrentCulture)
            : value.ToString(format: CanonicalDateTimeFormat,
                             provider: CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Parses a date/time that arrived from ExifTool, from another part of the application, or from the user.
    /// </summary>
    /// <param name="value">The text to parse. May be null, empty or padded with whitespace.</param>
    /// <param name="context">
    ///     Where the text came from. This only decides which candidate layout is <i>tried first</i>; every layout is
    ///     attempted regardless, because in a code base this size a value can reach a parser by more than one route.
    /// </param>
    /// <param name="result">The parsed value, or <see cref="DateTime.MinValue" /> when parsing failed.</param>
    /// <returns><see langword="true" /> when the text was understood; otherwise <see langword="false" />.</returns>
    public static bool TryParseDateTime(string? value,
                                        ValueFormatContext context,
                                        out DateTime result)
    {
        result = DateTime.MinValue;

        if (string.IsNullOrWhiteSpace(value: value))
        {
            return false;
        }

        string trimmedValue = value!.Trim();

        // An all-zero timestamp ("0000:00:00 00:00:00") is ExifTool's way of saying "this tag exists but is blank".
        // It is not a date, and coercing it into one produces the year 1 in the UI.
        if (trimmedValue.StartsWith(value: "0000", comparisonType: StringComparison.Ordinal))
        {
            return false;
        }

        // Display context: the string most likely came out of a text box the user typed into, so try their culture
        // first. Everything else is machine-generated, so the canonical layouts go first.
        if (context == ValueFormatContext.Display &&
            DateTime.TryParse(s: trimmedValue,
                              provider: CultureInfo.CurrentCulture,
                              styles: MachineDateTimeStyles,
                              result: out result))
        {
            return true;
        }

        if (DateTime.TryParseExact(s: trimmedValue,
                                   formats: MachineDateTimeFormats,
                                   provider: CultureInfo.InvariantCulture,
                                   style: MachineDateTimeStyles,
                                   result: out result))
        {
            return true;
        }

        if (DateTime.TryParse(s: trimmedValue,
                              provider: CultureInfo.InvariantCulture,
                              styles: MachineDateTimeStyles,
                              result: out result))
        {
            return true;
        }

        // Last resort for the non-Display contexts: the value may have been rendered in the user's culture by older
        // code (or by a hand-edited settings file) before reaching us.
        if (context != ValueFormatContext.Display &&
            DateTime.TryParse(s: trimmedValue,
                              provider: CultureInfo.CurrentCulture,
                              styles: MachineDateTimeStyles,
                              result: out result))
        {
            return true;
        }

        result = DateTime.MinValue;
        return false;
    }

    /// <summary>
    ///     Convenience wrapper around <see cref="TryParseDateTime" /> for callers that model "no value" as null.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="context">Where the text came from.</param>
    /// <returns>The parsed value, or <see langword="null" /> when the text could not be understood.</returns>
    public static DateTime? ParseDateTimeOrNull(string? value,
                                                ValueFormatContext context)
    {
        return TryParseDateTime(value: value, context: context, result: out DateTime parsed)
            ? parsed
            : null;
    }

    #endregion

    #region Numbers

    /// <summary>
    ///     Renders a <see cref="double" /> culture-invariantly. See the class remarks for why the context does not
    ///     change the outcome.
    /// </summary>
    /// <param name="value">The value to render.</param>
    /// <param name="context">Accepted for symmetry with the other members; currently does not affect the result.</param>
    public static string FormatDouble(double value,
                                      ValueFormatContext context)
    {
        return value.ToString(provider: CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Renders an <see cref="int" /> culture-invariantly. See the class remarks for why the context does not
    ///     change the outcome.
    /// </summary>
    /// <param name="value">The value to render.</param>
    /// <param name="context">Accepted for symmetry with the other members; currently does not affect the result.</param>
    public static string FormatInt(int value,
                                   ValueFormatContext context)
    {
        return value.ToString(provider: CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Parses a <see cref="double" />, preferring the invariant culture but tolerating a value that was rendered
    ///     in the user's culture (which happens when text originates from a control the user typed into).
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="context">Where the text came from.</param>
    /// <param name="result">The parsed value, or zero when parsing failed.</param>
    /// <returns><see langword="true" /> when the text was understood; otherwise <see langword="false" />.</returns>
    public static bool TryParseDouble(string? value,
                                      ValueFormatContext context,
                                      out double result)
    {
        result = 0;

        if (string.IsNullOrWhiteSpace(value: value))
        {
            return false;
        }

        // MachineNumberStyles deliberately excludes AllowThousands: see the constant's remarks. Without that,
        // "5,4" parses invariantly as 54.
        if (double.TryParse(s: value,
                            style: MachineNumberStyles,
                            provider: CultureInfo.InvariantCulture,
                            result: out result))
        {
            return true;
        }

        // The invariant attempt failed, so this is not machine-generated text. If it came from a human it may well
        // be in their own notation - a Hungarian or Argentinian user typing 5,4 means five point four.
        return context == ValueFormatContext.Display &&
               double.TryParse(s: value,
                               style: NumberStyles.Any,
                               provider: CultureInfo.CurrentCulture,
                               result: out result);
    }

    /// <summary>
    ///     Parses an <see cref="int" />, preferring the invariant culture.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="context">Where the text came from.</param>
    /// <param name="result">The parsed value, or zero when parsing failed.</param>
    /// <returns><see langword="true" /> when the text was understood; otherwise <see langword="false" />.</returns>
    public static bool TryParseInt(string? value,
                                   ValueFormatContext context,
                                   out int result)
    {
        result = 0;

        if (string.IsNullOrWhiteSpace(value: value))
        {
            return false;
        }

        if (int.TryParse(s: value,
                         style: NumberStyles.Integer,
                         provider: CultureInfo.InvariantCulture,
                         result: out result))
        {
            return true;
        }

        return context == ValueFormatContext.Display &&
               int.TryParse(s: value,
                            style: NumberStyles.Any,
                            provider: CultureInfo.CurrentCulture,
                            result: out result);
    }

    #endregion

    #region Type-agnostic entry points

    /// <summary>
    ///     Renders any supported attribute value for the given context, dispatching on its runtime type.
    /// </summary>
    /// <param name="value">
    ///     The value to render. Supported runtime types are <see cref="string" />, <see cref="int" />,
    ///     <see cref="double" /> and <see cref="DateTime" /> - i.e. the types
    ///     <see cref="SourcesAndAttributes.GetElementAttributesType" /> can report.
    /// </param>
    /// <param name="context">Why the value is being rendered.</param>
    /// <returns>The rendered value, or <see cref="string.Empty" /> when the value is null.</returns>
    public static string Format(IConvertible? value,
                                ValueFormatContext context)
    {
        return value switch
        {
            null => string.Empty,
            string stringValue => stringValue,
            DateTime dateTimeValue => FormatDateTime(value: dateTimeValue, context: context),
            double doubleValue => FormatDouble(value: doubleValue, context: context),
            int intValue => FormatInt(value: intValue, context: context),
            _ => value.ToString(provider: CultureInfo.InvariantCulture)
        };
    }

    /// <summary>
    ///     Parses text into the CLR type an attribute is declared to hold.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="targetType">
    ///     The type to produce, as reported by <see cref="SourcesAndAttributes.GetElementAttributesType" />.
    /// </param>
    /// <param name="context">Where the text came from.</param>
    /// <param name="result">
    ///     The parsed value, or <see langword="null" /> when the text could not be understood as the target type.
    /// </param>
    /// <returns><see langword="true" /> when the text was understood; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when the requested target type is not a supported attribute type.
    /// </exception>
    public static bool TryParse(string? value,
                                Type targetType,
                                ValueFormatContext context,
                                out IConvertible? result)
    {
        if (targetType == typeof(string))
        {
            result = value;
            return value != null;
        }

        if (targetType == typeof(double))
        {
            bool parsed = TryParseDouble(value: value, context: context, result: out double doubleValue);
            result = parsed
                ? doubleValue
                : null;
            return parsed;
        }

        if (targetType == typeof(int))
        {
            bool parsed = TryParseInt(value: value, context: context, result: out int intValue);
            result = parsed
                ? intValue
                : null;
            return parsed;
        }

        if (targetType == typeof(DateTime))
        {
            bool parsed = TryParseDateTime(value: value, context: context, result: out DateTime dateTimeValue);
            result = parsed
                ? dateTimeValue
                : null;
            return parsed;
        }

        throw new ArgumentException(
            message: $"'{targetType.Name}' is not a supported attribute value type.",
            paramName: nameof(targetType));
    }

    #endregion
}
