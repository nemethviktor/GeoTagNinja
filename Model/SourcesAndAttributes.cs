using System.Collections.Generic;
using System.Diagnostics;

namespace GeoTagNinja.Model
{
    public class SourcesAndAttributes
    {
        public enum ElementAttribute
        {
            GPSAltitude,
            GPSAltitudeRef,
            GPSDestLatitude,
            GPSDestLatitudeRef,
            GPSDestLongitude,
            GPSDestLongitudeRef,
            GPSImgDirection,
            GPSImgDirectionRef,
            GPSLatitude,
            GPSLatitudeRef,
            GPSLongitude,
            GPSLongitudeRef,
            GPSSpeed,
            GPSSpeedRef,
//            Coordinates,
//            DestCoordinates,
            City,
            CountryCode,
            Country,
            State,
            Sub_location,
            Make,
            Model,
            Rating,
            ExposureTime,
            Fnumber,
            FocalLength,
            FocalLengthIn35mmFormat,
            ISO,
            LensSpec,
            TakenDate,
            CreateDate,
            OffsetTime
        }

        public static string GetAttributeName(ElementAttribute attribute)
        {
            switch (attribute) {
                case ElementAttribute.GPSAltitude:
                    return "GPSAltitude";
                case ElementAttribute.GPSAltitudeRef:
                    return "GPSAltitudeRef";
                case ElementAttribute.GPSDestLatitude:
                    return "GPSDestLatitude";
                case ElementAttribute.GPSDestLatitudeRef:
                    return "GPSDestLatitudeRef";
                case ElementAttribute.GPSDestLongitude:
                    return "GPSDestLongitude";
                case ElementAttribute.GPSDestLongitudeRef:
                    return "GPSDestLongitudeRef";
                case ElementAttribute.GPSImgDirection:
                    return "GPSImgDirection";
                case ElementAttribute.GPSImgDirectionRef:
                    return "GPSImgDirectionRef";
                case ElementAttribute.GPSLatitude:
                    return "GPSLatitude";
                case ElementAttribute.GPSLatitudeRef:
                    return "GPSLatitudeRef";
                case ElementAttribute.GPSLongitude:
                    return "GPSLongitude";
                case ElementAttribute.GPSLongitudeRef:
                    return "GPSLongitudeRef";
                case ElementAttribute.GPSSpeed:
                    return "GPSSpeed";
                case ElementAttribute.GPSSpeedRef:
                    return "GPSSpeedRef";
/*                case ElementAttribute.Coordinates:
                    return "Coordinates";
                case ElementAttribute.DestCoordinates:
                    return "DestCoordinates";
*/                case ElementAttribute.City:
                    return "City";
                case ElementAttribute.CountryCode:
                    return "CountryCode";
                case ElementAttribute.Country:
                    return "Country";
                case ElementAttribute.State:
                    return "State";
                case ElementAttribute.Sub_location:
                    return "Sub_location";
                case ElementAttribute.Make:
                    return "Make";
                case ElementAttribute.Model:
                    return "Model";
                case ElementAttribute.Rating:
                    return "Rating";
                case ElementAttribute.ExposureTime:
                    return "ExposureTime";
                case ElementAttribute.Fnumber:
                    return "Fnumber";
                case ElementAttribute.FocalLength:
                    return "FocalLength";
                case ElementAttribute.FocalLengthIn35mmFormat:
                    return "FocalLengthIn35mmFormat";
                case ElementAttribute.ISO:
                    return "ISO";
                case ElementAttribute.LensSpec:
                    return "LensSpec";
                case ElementAttribute.TakenDate:
                    return "TakenDate";
                case ElementAttribute.CreateDate:
                    return "CreateDate";
                case ElementAttribute.OffsetTime:
                    return "OffsetTime";
                default:
                    Debug.Fail("Trying to get attribute name of unknown attribute with value " + attribute.ToString());
                    break;
            }
            return null;
        }


        public readonly static IDictionary<ElementAttribute, List<string>> TagsToAttributesOrder =
            new Dictionary<ElementAttribute, List<string>>()
            {
                { ElementAttribute.City, new List<string>() {
                    "XMP:City",
                    "IPTC:City"
                } },
                { ElementAttribute.Country, new List<string>()
                {
                    "IPTC:Country-PrimaryLocationName",
                    "XMP:Country"
                } },
                { ElementAttribute.CountryCode, new List<string>() {
                    "XMP:CountryCode",
                    "IPTC:Country-PrimaryLocationCode"
                } },
                { ElementAttribute.CreateDate, new List<string>() {
                    "XMP:CreateDate",
                    "EXIF:CreateDate"
                } },
                { ElementAttribute.ExposureTime, new List<string>() {
                    "XMP:ExposureTime",
                    "EXIF:ExposureTime"
                } },
                { ElementAttribute.Fnumber, new List<string>() {
                    "XMP:FNumber",
                    "EXIF:FNumber"
                } },
                { ElementAttribute.FocalLength, new List<string>() {
                    "XMP:FocalLength",
                    "EXIF:FocalLength"
                } },
                { ElementAttribute.FocalLengthIn35mmFormat, new List<string>() {
                    "XMP:FocalLengthIn35mmFormat",
                    "EXIF:FocalLengthIn35mmFormat",
                    "Composite:FocalLength35efl"
                } },
                { ElementAttribute.GPSAltitude, new List<string>() {
                    "XMP:GPSAltitude",
                    "EXIF:GPSAltitude",
                    "Composite:GPSAltitude"
                } },
                { ElementAttribute.GPSAltitudeRef, new List<string>() {
                    "XMP:GPSAltitudeRef",
                    "EXIF:GPSAltitudeRef"
                } },
                { ElementAttribute.GPSDestLatitude, new List<string>() {
                    "XMP:GPSDestLatitude",
                    "EXIF:GPSDestLatitude"
                } },
                { ElementAttribute.GPSDestLatitudeRef, new List<string>() {
                    "EXIF:GPSDestLatitudeRef",
                    "Composite:GPSDestLatitudeRef"
                } },
                { ElementAttribute.GPSDestLongitude, new List<string>() {
                    "XMP:GPSDestLongitude",
                    "EXIF:GPSDestLongitude"
                } },
                { ElementAttribute.GPSDestLongitudeRef, new List<string>() {
                    "EXIF:GPSDestLongitudeRef",
                    "Composite:GPSDestLongitudeRef"
                } },
                { ElementAttribute.GPSImgDirection, new List<string>() {
                    "XMP:GPSImgDirection",
                    "EXIF:GPSImgDirection"
                } },
                { ElementAttribute.GPSImgDirectionRef, new List<string>() {
                    "XMP:GPSImgDirectionRef",
                    "EXIF:GPSImgDirectionRef"
                } },
                { ElementAttribute.GPSLatitude, new List<string>() {
                    "XMP:GPSLatitude",
                    "EXIF:GPSLatitude",
                    "Composite:GPSLatitude"
                } },
                { ElementAttribute.GPSLatitudeRef, new List<string>() {
                    "EXIF:GPSLatitudeRef",
                    "Composite:GPSLatitudeRef"
                } },
                { ElementAttribute.GPSLongitude, new List<string>() {
                    "XMP:GPSLongitude",
                    "EXIF:GPSLongitude",
                    "Composite:GPSLongitude"
                } },
                { ElementAttribute.GPSLongitudeRef, new List<string>() {
                    "EXIF:GPSLongitudeRef",
                    "Composite:GPSLongitudeRef"
                } },
                { ElementAttribute.GPSSpeed, new List<string>() {
                    "XMP:GPSSpeed",
                    "EXIF:GPSSpeed"
                } },
                { ElementAttribute.GPSSpeedRef, new List<string>() {
                    "XMP:GPSSpeedRef",
                    "EXIF:GPSSpeedRef"
                } },
                { ElementAttribute.ISO, new List<string>() {
                    "XMP:ISO",
                    "EXIF:ISO",
                    "Composite:ISO"
                } },
                { ElementAttribute.LensSpec, new List<string>() {
                    "XMP:LensInfo",
                    "EXIF:LensModel",
                    "Composite:Lens"
                } },
                { ElementAttribute.Make, new List<string>() {
                    "XMP:Make",
                    "EXIF:Make"
                } },
                { ElementAttribute.Model, new List<string>() {
                    "XMP:Model",
                    "EXIF:Model"
                } },
                { ElementAttribute.OffsetTime, new List<string>() {
                    "EXIF:OffsetTimeOriginal",
                    "EXIF:OffsetTime"
                } },
                { ElementAttribute.Rating, new List<string>() {
                    "XMP:Rating",
                    "EXIF:Rating"
                } },
                { ElementAttribute.State, new List<string>() {
                    "XMP:State",
                    "IPTC:Province-State"
                } },
                { ElementAttribute.Sub_location, new List<string>() {
                    "XMP:Location",
                    "IPTC:Sub-location"
                } },
                { ElementAttribute.TakenDate, new List<string>() {
                    "XMP:DateTimeOriginal",
                    "EXIF:DateTimeOriginal"
                } }

            };

    }
}
