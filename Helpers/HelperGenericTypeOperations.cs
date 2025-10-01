using System;
using System.Globalization;
using System.Linq;

namespace GeoTagNinja.Helpers;

internal static class HelperGenericTypeOperations
{
    public static double? TryParseNullableDouble(string val)
    {
        return double.TryParse(s: val, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double outValue)
            ? outValue
            : null;
    }

    public static int? TryParseNullableInt(string val)
    {
        return int.TryParse(s: val, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out int outValue)
            ? outValue
            : null;
    }

    public static DateTime? TryParseNullableDateTime(string val)
    {
        DateTime.TryParse(
            s: val,
            provider: CultureInfo.CurrentCulture,
            styles: DateTimeStyles.None,
            result: out DateTime outValue
        );

        return outValue;
    }

    /// <summary>
    ///     A "coalesce" function.
    /// </summary>
    /// <param name="strings">Array of string values to be queried</param>
    /// <returns>The first non-null value</returns>
    internal static string Coalesce(params string[] strings)
    {
        return strings.FirstOrDefault(predicate: s => !string.IsNullOrWhiteSpace(value: s));
    }

    internal static DateTime? ConvertStringToDateTime(string dateTimeToConvert)
    {
        bool isDT = DateTime.TryParse(s: dateTimeToConvert, result: out DateTime tryDataValueDT);
        return isDT ? tryDataValueDT : null;
    }

    internal static string ConvertStringToDateTimeBackToString(string dateTimeToConvert)
    {
        bool isDT = DateTime.TryParse(s: dateTimeToConvert, result: out DateTime tryDataValueDT);
        string tryDataValueStr = tryDataValueDT.ToString(format:
            $"{CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern}");
        return isDT ? tryDataValueStr : FrmMainApp.NullStringEquivalentGeneric;
    }
}