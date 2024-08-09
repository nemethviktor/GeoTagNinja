using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using GeoTagNinja.Helpers;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Model;

internal class TagsToModelValueTransformations
{
    /// <summary>
    ///     Compose the GPSLat/Long value with their respective reference (N for North, etc).
    ///     Ensure dec. sep. is "."
    /// </summary>
    public static double? T2M_GPSLatLong(ElementAttribute attribute,
                                         string parseResult,
                                         IDictionary<ElementAttribute, IConvertible> parsed_Values,
                                         Func<ElementAttribute, bool> ParseMissingAttribute)
    {
        if (parseResult == null)
        {
            return null; // not set
        }

        // Get the Ref Attribute for the corresponding data point and thereof the first character
        // (Should be N of North, etc.)
        // If this character is not contained in the data point value, add it before it
        ElementAttribute refAttrib;
        switch (attribute)
        {
            case ElementAttribute.GPSLatitude:
                refAttrib = ElementAttribute.GPSLatitudeRef;
                break;
            case ElementAttribute.GPSDestLatitude:
                refAttrib = ElementAttribute.GPSDestLatitudeRef;
                break;
            case ElementAttribute.GPSLongitude:
                refAttrib = ElementAttribute.GPSLongitudeRef;
                break;
            case ElementAttribute.GPSDestLongitude:
                refAttrib = ElementAttribute.GPSDestLongitudeRef;
                break;
            default:
                throw new ArgumentException(message: $"T2M_GPSLatLong does not support attribute '{GetElementAttributesName(attributeToFind: attribute)}'");
        }

        // If reference is set, concat if needed
        string tmpLatLongRefVal = ""; // this will be something like "N" or "North" etc.
        if (parsed_Values.ContainsKey(key: refAttrib))
            // Was parsed already
        {
            tmpLatLongRefVal = (string)parsed_Values[key: refAttrib];
        }
        else
        {
            // Otherwise parse
            bool parseOk = ParseMissingAttribute(arg: refAttrib);
            if (parseOk)
            {
                tmpLatLongRefVal = (string)parsed_Values[key: refAttrib];
            }
        }

        // Not set attribs are null (or just doesn't start with one of the below)
        if (tmpLatLongRefVal == null ||
            !Regex.IsMatch(input: tmpLatLongRefVal, pattern: "^[SWNE\"-]", options: RegexOptions.IgnoreCase))
        {
            tmpLatLongRefVal = "";
        }

        if (tmpLatLongRefVal.Length > 0)
        {
            tmpLatLongRefVal = tmpLatLongRefVal.Substring(startIndex: 0, length: 1);
            if (!parseResult.Contains(value: tmpLatLongRefVal))
            {
                parseResult = tmpLatLongRefVal + parseResult;
            }
        }

        // Finally ensure that dec sep. is "." and account for direction
        // of coordinates by using +/-
        return HelperExifDataPointInteractions.AdjustLatLongNegative(point: parseResult);
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
            if (parseBool)
            {
                return HelperVariables.UserSettingUseImperial
                    ? Math.Round(value: tmpAltitude * HelperVariables.MetreToFeet, digits: 2)
                    : tmpAltitude;
            }

            return null;
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

        if (parseResult.ToLower()
                .Contains(value: "below") ||
            parseResult.Contains(value: "1"))
        {
            return "Below Sea Level";
        }

        return "Above Sea Level";
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

        if (parseResult.ToLower()
                       .Contains(value: "true") ||
            parseResult.ToLower()
                       .Contains(value: "geo") ||
            parseResult.ToUpper()
                       .StartsWith("T"))
        {
            return "Geographic North";
        }

        return "Magnetic North";
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
    ///     Ensure the value is an actual date-time...
    /// </summary>
    public static DateTime? T2M_TakenCreatedDate(string parseResult)
    {
        if (parseResult == null)
        {
            return null; // not set
        }

        if (parseResult.Contains("0000"))
        {
            return null; // not set
        }

        //return outDateTime.ToString(format: CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern);
        return DateTime.TryParse(s: parseResult, result: out DateTime outDateTime)
            ? outDateTime
            : null;
    }
}