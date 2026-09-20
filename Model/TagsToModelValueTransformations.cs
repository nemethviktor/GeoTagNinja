using GeoTagNinja.Helpers;
using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Model;

internal class TagsToModelValueTransformations
{
    /// <summary>
    /// Composes GPS Latitude/Longitude values with their respective references (North, South, East, West).
    /// Handles cases where Reference tags are missing by deriving them from the coordinate's sign
    /// and explicitly setting the Reference attribute in the parsed values collection.
    /// </summary>
    /// <param name="attribute">The coordinate attribute being parsed (Latitude or Longitude).</param>
    /// <param name="parseResult">The raw string value from ExifTool.</param>
    /// <param name="parsed_Values">The dictionary of values already parsed in this pass.</param>
    /// <param name="ParseMissingAttribute">A delegate to trigger parsing of a dependent attribute if not yet available.</param>
    /// <returns>A double representing the coordinate, adjusted for hemisphere.</returns>
    /// <exception cref="ArgumentException">Thrown if an unsupported attribute is passed to the transformation.</exception>
    public static double? T2M_GPSLatLong(
        ElementAttribute attribute,
        string parseResult,
        IDictionary<ElementAttribute, IConvertible> parsed_Values,
        Func<ElementAttribute, bool> ParseMissingAttribute)
    {
        if (parseResult == null)
        {
            return null;
        }

        // 1. Identify the corresponding Reference attribute.
        ElementAttribute refAttrib = attribute switch
        {
            ElementAttribute.GPSLatitude => ElementAttribute.GPSLatitudeRef,
            ElementAttribute.GPSDestLatitude => ElementAttribute.GPSDestLatitudeRef,
            ElementAttribute.GPSLongitude => ElementAttribute.GPSLongitudeRef,
            ElementAttribute.GPSDestLongitude => ElementAttribute.GPSDestLongitudeRef,
            _ => throw new ArgumentException(message: $"T2M_GPSLatLong does not support attribute '{SourcesAndAttributes.GetElementAttributesName(attributeToFind: attribute)}'")
        };

        // 2. Parse the numeric coordinate. This goes through DataPointInteractions rather than double.TryParse
        // because the track-file path used to, and ExifTool does emit WGS84 degrees-and-minutes ("41,53.239N")
        // as well as plain signed decimals.
        if (!DataPointInteractions.TryAdjustLatLongNegative(
            point: parseResult,
            coordinate: out double baseValue))
        {
            return null;
        }

        // 3. Attempt to retrieve the existing Reference value (e.g., from EXIF).
        string? refValue = null;
        if (ParseMissingAttribute(arg: refAttrib) && parsed_Values.TryGetValue(key: refAttrib, out IConvertible? val))
        {
            refValue = val.ToString(provider: System.Globalization.CultureInfo.InvariantCulture);
        }

        // 4. Force-derive Reference if missing (common in XMP signed floats).
        if (string.IsNullOrWhiteSpace(value: refValue))
        {
            refValue = attribute is ElementAttribute.GPSLatitude or ElementAttribute.GPSDestLatitude
                ? (baseValue < 0) ? "South" : "North"
                : (baseValue < 0) ? "West" : "East";

            // 5. Explicitly publish the derived Reference back to the parsed values.
            // This ensures the literal string is available for the ListView and other logic.
            if (!parsed_Values.ContainsKey(key: refAttrib))
            {
                parsed_Values.Add(key: refAttrib, value: refValue);
            }
        }

        // 6. Final Calculation: Apply standard hemispheric logic.
        // We treat South and West as negative, but keep the absolute value of the coordinate 
        // to avoid double-negatives if the source was already a signed float.
        double absoluteValue = Math.Abs(value: baseValue);
        bool isNegative = refValue.StartsWith(value: "S", comparisonType: StringComparison.OrdinalIgnoreCase) ||
                          refValue.StartsWith(value: "W", comparisonType: StringComparison.OrdinalIgnoreCase);

        return isNegative ? -absoluteValue : absoluteValue;
    }

    /// <summary>
    ///     Extract altitude from given string that also contains text
    ///     Supports ###/### m 
    /// </summary>
    public static double? T2M_GPSAltitude(string parseResult)
    {
        if (parseResult == null)
        {
            return null; // not set
        }

        // Remove the bit with "m"
        if (parseResult.Contains(value: "m"))
        {
            parseResult = parseResult.Split('m')[0]
                .Trim()
                .Replace(oldChar: ',', newChar: '.');
        }

        if (parseResult.Contains(value: "/"))
        {
            if (parseResult.Contains(value: ",") || parseResult.Contains(value: "."))
            {
                parseResult = parseResult.Split('/')[0]
                    .Trim()
                    .Replace(oldChar: ',', newChar: '.');
            }
            else // attempt to convert it to decimal
            {
                try
                {
                    bool parseBool = double.TryParse(s: parseResult.Split('/')[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double numerator);
                    parseBool = double.TryParse(s: parseResult.Split('/')[1], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double denominator);
                    double tmpAltitude = Math.Round(value: numerator / denominator, digits: 2);

                    return HelperVariables.UserSettingUseImperial
                        ? Math.Round(value: tmpAltitude * HelperVariables.MetreToFeet, digits: 2)
                        : tmpAltitude;
                }
                catch
                {
                    return null;
                }
            }
        }

        // Finally convert what we have...
        try
        {
            bool parseBool = double.TryParse(s: parseResult, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double tmpAltitude);
            return parseBool
                ? HelperVariables.UserSettingUseImperial
                    ? Math.Round(value: tmpAltitude * HelperVariables.MetreToFeet, digits: 2)
                    : tmpAltitude
                : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Standardize string to "Below Sea Level" or
    ///     "Above Sea Level" (default).
    /// </summary>
    public static string T2M_AltitudeRef(string parseResult)
    {
        if (parseResult == null)
        {
            return null; // not set
        }

        return parseResult.ToLower()
                .Contains(value: "below") ||
            parseResult.Contains(value: "1")
            ? "Below Sea Level"
            : "Above Sea Level";
    }

    /// <summary>
    ///     Extract GPSImgDirection
    /// </summary>
    public static double? T2M_GPSImgDirection(string parseResult)
    {
        if (parseResult == null)
        {
            // not set
            return null;
        }

        try
        {
            if (parseResult.Contains('/'))
            {
                bool parseBool = double.TryParse(s: parseResult.Split('/')[0],
                                                 style: NumberStyles.Any,
                                                 provider: CultureInfo.InvariantCulture,
                                                 result: out double numerator);
                parseBool = double.TryParse(s: parseResult.Split('/')[1],
                                            style: NumberStyles.Any,
                                            provider: CultureInfo.InvariantCulture,
                                            result: out double denominator);
                double gpsImgDirection = Math.Round(value: numerator / denominator, digits: 2);

                return gpsImgDirection;
            }
            else
            {
                bool _ = double.TryParse(s: parseResult,
                                         style: NumberStyles.Any,
                                         provider: CultureInfo.InvariantCulture,
                                         result: out double value);
                double gpsImgDirection = Math.Round(value: value, digits: 2);

                return gpsImgDirection;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Standardize string to "Geographic North" or
    ///     "Magnetic North" (default).
    /// </summary>
    public static string T2M_GPSImgDirectionRef(string parseResult)
    {
        if (parseResult == null)
        {
            return null; // not set
        }

        return parseResult.ToLower()
                       .Contains(value: "true") ||
            parseResult.ToLower()
                       .Contains(value: "geo") ||
            parseResult.ToUpper()
                       .StartsWith("T")
            ? "Geographic North"
            : "Magnetic North";
    }

    /// <summary>
    ///     Standardize the exposure time value - removing "sec" and
    ///     trail/lead white space.
    /// </summary>
    public static string T2M_ExposureTime(string parseResult)
    {
        if (parseResult == null)
        {
            return null; // not set
        }

        return parseResult.Replace(oldValue: "sec", newValue: "")
            .Trim();
    }

    /// <summary>
    ///     Extract a numeric value from the attribute by also removing
    ///     identified "clutter". Also calcs encountered quotient.
    /// </summary>
    public static double T2M_F_FocalLength(ElementAttribute attribute,
                                           string parseResult)
    {
        if (parseResult == null)
        {
            return 0; // not set
        }

        // Pre-work on focal length 35mm:
        // at least with a Canon 40D this returns stuff like: "51.0 mm (35 mm equivalent: 81.7 mm)" so i think it's safe to assume that 
        // this might need a bit of debugging and community feeback. or someone with decent regex knowledge
        if (attribute == ElementAttribute.FocalLengthIn35mmFormat)
        {
            if (parseResult.Contains(value: ':'))
            {
                parseResult = Regex.Replace(input: parseResult, pattern: @"[^\d:.]", replacement: "")
                    .Split(':')
                    .Last();
            }
            else
            {
                // this is untested. soz. feedback welcome.
                parseResult = Regex.Replace(input: parseResult, pattern: @"[^\d:.]", replacement: "");
            }
        }

        // Generic removal of other stuff
        parseResult = parseResult
            .Replace(oldValue: "mm", newValue: "")
            .Replace(oldValue: "f/", newValue: "")
            .Replace(oldValue: "f", newValue: "")
            .Replace(oldValue: "[", newValue: "")
            .Replace(oldValue: "]", newValue: "")
            .Trim();

        // If there is a quotient in it - calculate it
        if (parseResult.Contains(value: "/"))
        {
            parseResult = Math.Round(value:
                                     double.Parse(s: parseResult.Split('/')[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture) /
                                     double.Parse(s: parseResult.Split('/')[1], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture),
                                     digits: 1)
                .ToString();
        }

        bool _ = double.TryParse(s: parseResult,
                                 style: NumberStyles.Any,
                                 provider: CultureInfo.InvariantCulture,
                                 result: out double returnVal);
        return _
            ? returnVal
            : FrmMainApp.NullDoubleEquivalent;
    }

    /// <summary>
    ///     Identical to the above but ISO is an int, not a double
    /// </summary>
    /// <param name="attribute"></param>
    /// <param name="parseResult"></param>
    /// <returns></returns>
    public static int T2M_F_ISO(ElementAttribute attribute,
                                string parseResult)
    {
        if (parseResult == null)
        {
            return 0; // not set
        }

        parseResult = Regex.Replace(input: parseResult, pattern: @"[^\d:.]", replacement: "");

        bool _ = int.TryParse(s: parseResult.ToString(provider: CultureInfo.InvariantCulture), result: out int returnVal);

        return _
            ? returnVal
            : 0;
    }

    /// <summary>
    ///     Extracts the dilution of precision.
    /// </summary>
    /// <remarks>
    ///     It is documented as a plain number but is frequently written as a rational - "43/10" rather than "4.3" -
    ///     so the rational form is accepted too. Only the track-file reader used to handle that.
    /// </remarks>
    public static double? T2M_GPSDOP(string parseResult)
    {
        return T2M_NumberOrRational(parseResult: parseResult);
    }

    /// <summary>
    ///     Extracts the horizontal positioning error, falling back to GPSDOP * 3 when the tag itself is absent.
    /// </summary>
    /// <remarks>
    ///     The fallback is the rule of thumb the track-file reader has always applied; it now applies to files read
    ///     off disk as well, so the same tag set yields the same value whichever way it arrived.
    /// </remarks>
    /// <remarks>
    ///     The attribute is declared as text, so a value the file does carry is passed through verbatim - only the
    ///     derived one is rendered here, and then invariantly.
    /// </remarks>
    /// <param name="parseResult">The raw tag value from ExifTool, or null when the file has no such tag.</param>
    /// <param name="parsed_Values">The dictionary of values already parsed in this pass.</param>
    /// <param name="ParseMissingAttribute">A delegate to trigger parsing of a dependent attribute if not yet available.</param>
    public static string T2M_GPSHPositioningError(
        string parseResult,
        IDictionary<ElementAttribute, IConvertible> parsed_Values,
        Func<ElementAttribute, bool> ParseMissingAttribute)
    {
        if (!string.IsNullOrWhiteSpace(value: parseResult))
        {
            return parseResult;
        }

        if (!ParseMissingAttribute(arg: ElementAttribute.GPSDOP) ||
            !parsed_Values.TryGetValue(key: ElementAttribute.GPSDOP, out IConvertible? dop) ||
            dop is not double dopAsDouble)
        {
            return null;
        }

        return AttributeValueFormatter.FormatDouble(value: dopAsDouble * 3,
            context: ValueFormatContext.ExifTool);
    }

    /// <summary>
    ///     Reads a number that ExifTool may have written either plainly or as a rational ("43/10").
    /// </summary>
    private static double? T2M_NumberOrRational(string parseResult)
    {
        if (string.IsNullOrWhiteSpace(value: parseResult))
        {
            // not set
            return null;
        }

        if (parseResult.Contains(value: "/"))
        {
            string[] parts = parseResult.Split('/');
            if (parts.Length != 2 ||
                !double.TryParse(s: parts[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double numerator) ||
                !double.TryParse(s: parts[1], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double denominator) ||
                denominator == 0)
            {
                return null;
            }

            return Math.Round(value: numerator / denominator, digits: 2);
        }

        return double.TryParse(
            s: parseResult,
            style: NumberStyles.Any,
            provider: CultureInfo.InvariantCulture,
            result: out double returnVal)
            ? returnVal
            : null;
    }

    /// <summary>
    ///     Converts an ExifTool timestamp into a real <see cref="DateTime" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This used to call <c>DateTime.TryParse</c> with the current culture, which only worked because the
    ///         bundled <c>.ExifTool_config</c> rewrites ExifTool's native <c>YYYY:MM:DD hh:mm:ss</c> into a form the
    ///         BCL happens to recognise. Whenever that config was not picked up, or the user's culture used a
    ///         non-Gregorian calendar, every timestamp in the application silently went blank.
    ///     </para>
    ///     <para>
    ///         <see cref="AttributeValueFormatter" /> now handles both layouts explicitly, so the outcome no longer
    ///         depends on either the Perl config or the machine's locale. It also rejects the all-zero timestamp that
    ///         ExifTool emits for a present-but-empty tag.
    ///     </para>
    /// </remarks>
    /// <param name="parseResult">The raw tag value from ExifTool.</param>
    /// <returns>The parsed timestamp, or <see langword="null" /> when the tag held no usable date.</returns>
    public static DateTime? T2M_TakenCreatedDate(string parseResult)
    {
        return AttributeValueFormatter.ParseDateTimeOrNull(value: parseResult,
            context: ValueFormatContext.ExifTool);
    }

    /// <summary>
    ///     Applies the clean-up an attribute needs to the raw text ExifTool produced for it, and returns the result
    ///     as the CLR type <see cref="SourcesAndAttributes.GetElementAttributesType" /> declares for that attribute.
    /// </summary>
    /// <remarks>
    ///     This is the single dispatch point for "which transformation belongs to which attribute". Both readers -
    ///     the file read pass and the track-file overlay - go through it, so neither can drift from the other.
    /// </remarks>
    /// <param name="attribute">The attribute being parsed.</param>
    /// <param name="parseResult">The raw tag value, or null when the tag set held nothing for this attribute.</param>
    /// <param name="parsed_Values">The dictionary of values already parsed in this pass.</param>
    /// <param name="ParseMissingAttribute">A delegate to trigger parsing of a dependent attribute if not yet available.</param>
    /// <returns>The typed value, or null when the attribute could not be established.</returns>
    public static IConvertible? TransformTagValue(
        ElementAttribute attribute,
        string parseResult,
        IDictionary<ElementAttribute, IConvertible> parsed_Values,
        Func<ElementAttribute, bool> ParseMissingAttribute)
    {
        switch (attribute)
        {
            case ElementAttribute.GPSAltitude:
                return T2M_GPSAltitude(parseResult: parseResult);

            case ElementAttribute.GPSAltitudeRef:
                return T2M_AltitudeRef(parseResult: parseResult);

            case ElementAttribute.GPSLatitude:
            case ElementAttribute.GPSDestLatitude:
            case ElementAttribute.GPSLongitude:
            case ElementAttribute.GPSDestLongitude:
                return T2M_GPSLatLong(attribute: attribute,
                    parseResult: parseResult,
                    parsed_Values: parsed_Values,
                    ParseMissingAttribute: ParseMissingAttribute);

            case ElementAttribute.GPSImgDirection:
                return T2M_GPSImgDirection(parseResult: parseResult);

            case ElementAttribute.GPSImgDirectionRef:
                return T2M_GPSImgDirectionRef(parseResult: parseResult);

            case ElementAttribute.ExposureTime:
                return T2M_ExposureTime(parseResult: parseResult);

            case ElementAttribute.Fnumber:
            case ElementAttribute.FocalLength:
            case ElementAttribute.FocalLengthIn35mmFormat:
                return T2M_F_FocalLength(attribute: attribute, parseResult: parseResult);

            case ElementAttribute.GPSDOP:
                return T2M_GPSDOP(parseResult: parseResult);

            case ElementAttribute.GPSHPositioningError:
                return T2M_GPSHPositioningError(parseResult: parseResult,
                    parsed_Values: parsed_Values,
                    ParseMissingAttribute: ParseMissingAttribute);

            case ElementAttribute.ISO:
                return T2M_F_ISO(attribute: attribute, parseResult: parseResult);

            case ElementAttribute.TakenDate:
            case ElementAttribute.CreateDate:
                return T2M_TakenCreatedDate(parseResult: parseResult);

            default:

                // No bespoke transformation for this attribute: convert the raw ExifTool text straight into the
                // declared type. Anything unparseable becomes the blank value for that type, which is what the
                // rest of the pipeline treats as "the file did not have this".
                Type typeOfAttribute = GetElementAttributesType(attributeToFind: attribute);

                _ = AttributeValueFormatter.TryParse(value: parseResult,
                    targetType: typeOfAttribute,
                    context: ValueFormatContext.ExifTool,
                    result: out IConvertible? convertedValue);

                return convertedValue ??
                       (typeOfAttribute == typeof(string)
                           ? parseResult
                           : DirectoryElement.BlankValueFor(attribute: attribute));
        }
    }
}