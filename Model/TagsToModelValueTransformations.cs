using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Model
{
    internal class TagsToModelValueTransformations
    {

        /// <summary>
        /// Compose the GPSLat/Long value with their respective reference (N for North, etc).
        /// Ensure dec. sep. is "."
        /// </summary>
        public static double? T2M_GPSLatLong(ElementAttribute attribute,
            string parse_result,
            IDictionary<ElementAttribute, IConvertible> parsed_Values,
            Func<ElementAttribute, bool> ParseMissingAttribute)
        {
            if (parse_result == null) return null;  // not set

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
                    refAttrib = ElementAttribute.GPSLatitudeRef;
                    break;
                case ElementAttribute.GPSLongitude:
                    refAttrib = ElementAttribute.GPSLatitudeRef;
                    break;
                case ElementAttribute.GPSDestLongitude:
                    refAttrib = ElementAttribute.GPSLatitudeRef;
                    break;
                default:
                    throw new ArgumentException($"T2M_GPSLatLong does not support attribute '{GetAttributeName(attribute)}'");
            }

            // If reference is set, concat if needed
            string tmpLatLongRefVal = "";
            if (parsed_Values.ContainsKey(refAttrib))
                // Was parsed already
                tmpLatLongRefVal = (string)parsed_Values[refAttrib];
            else
            {
                // It was not parsed, yet
                bool parseOk = ParseMissingAttribute(refAttrib);
                if (parseOk)
                    tmpLatLongRefVal = (string)parsed_Values[refAttrib];
            }
            // Not set attribs are null
            if (tmpLatLongRefVal == null) tmpLatLongRefVal = "";

            if (tmpLatLongRefVal.Length > 0)
            {
                tmpLatLongRefVal = tmpLatLongRefVal.Substring(startIndex: 0, length: 1);
                if (!parse_result.Contains(value: tmpLatLongRefVal))
                    parse_result = tmpLatLongRefVal + parse_result;
            }

            // Finally ensure that dec sep. is "." and account for direction
            // of coordinates by using +/-
            return HelperStatic.GenericAdjustLatLongNegative(point: parse_result);
        }


        /// <summary>
        /// Extract hight from given string that also contains text
        /// Supports ###/### m bla
        /// </summary>
        public static double? T2M_GPSAltitude(string parse_result)
        {
            if (parse_result == null) return null;  // not set

            // Remove the bit with "m"
            if (parse_result.Contains(value: "m"))
            {
                parse_result = parse_result.Split('m')[0]
                    .Trim()
                    .Replace(oldChar: ',', newChar: '.');
            }

            if (parse_result.Contains(value: "/"))
            {
                if (parse_result.Contains(value: ",") || parse_result.Contains(value: "."))
                {
                    parse_result = parse_result.Split('/')[0]
                        .Trim()
                        .Replace(oldChar: ',', newChar: '.');
                }
                else // attempt to convert it to decimal
                {
                    try
                    {
                        bool parseBool = double.TryParse(s: parse_result.Split('/')[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double numerator);
                        parseBool = double.TryParse(s: parse_result.Split('/')[1], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double denominator);
                        return Math.Round(value: numerator / denominator, digits: 2);
                    }
                    catch
                    {
                        parse_result = "0.0";
                    }
                }
            }

            // Finally convert what we have...
            try
            {
                bool parseBool = double.TryParse(s: parse_result, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double dbl_result);
                return dbl_result;
            }
            catch
            {
                return 0.0;
            }
        }


        /// <summary>
        /// Standardize string to "Below Sea Level" or
        /// "Above Sea Level" (default).
        /// </summary>
        public static string T2M_AltitudeRef(string parse_result)
        {
            if (parse_result == null) return null;  // not set

            if (parse_result.ToLower().Contains(value: "below") ||
                parse_result.Contains(value: "1"))
                return "Below Sea Level";
            return "Above Sea Level";
        }


        /// <summary>
        /// Extract a numeric value from the attribute by also removing
        /// identified "clutter". Also calcs encountered quotient.
        /// </summary>
        public static string T2M_F_FocalLength_ISO(ElementAttribute attribute, string parse_result)
        {
            if (parse_result == null) return null;  // not set

            // Pre-work on focal length 35mm:
            // at least with a Canon 40D this returns stuff like: "51.0 mm (35 mm equivalent: 81.7 mm)" so i think it's safe to assume that 
            // this might need a bit of debugging and community feeback. or someone with decent regex knowledge
            if (attribute == ElementAttribute.FocalLengthIn35mmFormat)
            {
                if (parse_result.Contains(value: ':'))
                {
                    parse_result = Regex.Replace(input: parse_result, pattern: @"[^\d:.]", replacement: "")
                        .Split(':')
                        .Last();
                }
                else
                {
                    // this is untested. soz. feedback welcome.
                    parse_result = Regex.Replace(input: parse_result, pattern: @"[^\d:.]", replacement: "");
                }
            }

            // Generic removal of other stuff
            parse_result = parse_result
                .Replace(oldValue: "mm", newValue: "")
                .Replace(oldValue: "f/", newValue: "")
                .Replace(oldValue: "f", newValue: "")
                .Replace(oldValue: "[", newValue: "")
                .Replace(oldValue: "]", newValue: "")
                .Trim();

            // If there is a quotient in it - calculate it
            if (parse_result.Contains(value: "/"))
                parse_result = Math.Round(value:
                    double.Parse(s: parse_result.Split('/')[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture) /
                    double.Parse(s: parse_result.Split('/')[1], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture),
                    digits: 1).ToString();

            return parse_result;
        }
    }
}
