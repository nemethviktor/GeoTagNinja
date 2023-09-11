using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace GeoTagNinja.Model;

public class SourcesAndAttributes
{
    /// <summary>
    ///     ElementAttributes available
    /// </summary>
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
        Coordinates,
        DestCoordinates,
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
        TakenDateDaysShift,
        TakenDateHoursShift,
        TakenDateMinutesShift,
        TakenDateSecondsShift,
        CreateDateDaysShift,
        CreateDateHoursShift,
        CreateDateMinutesShift,
        CreateDateSecondsShift,
        OffsetTime,
        RemoveAllGPS,
        GUID
    }

    /// <summary>
    ///     List of ElementAttributes. Reorder to change default column layout.
    /// </summary>
    public static readonly List<ElementAttribute> TagsToColumnHeaderOrder = new()
    {
        ElementAttribute.Coordinates,
        ElementAttribute.GPSLatitude,
        ElementAttribute.GPSLatitudeRef,
        ElementAttribute.GPSLongitude,
        ElementAttribute.GPSLongitudeRef,
        ElementAttribute.GPSSpeed,
        ElementAttribute.GPSSpeedRef,
        ElementAttribute.GPSAltitude,
        ElementAttribute.GPSAltitudeRef,
        ElementAttribute.Country,
        ElementAttribute.CountryCode,
        ElementAttribute.State,
        ElementAttribute.City,
        ElementAttribute.Sub_location,
        ElementAttribute.DestCoordinates,
        ElementAttribute.GPSDestLatitude,
        ElementAttribute.GPSDestLatitudeRef,
        ElementAttribute.GPSDestLongitude,
        ElementAttribute.GPSDestLongitudeRef,
        ElementAttribute.GPSImgDirection,
        ElementAttribute.GPSImgDirectionRef,
        ElementAttribute.Make,
        ElementAttribute.Model,
        ElementAttribute.Rating,
        ElementAttribute.ExposureTime,
        ElementAttribute.Fnumber,
        ElementAttribute.FocalLength,
        ElementAttribute.FocalLengthIn35mmFormat,
        ElementAttribute.ISO,
        ElementAttribute.LensSpec,
        ElementAttribute.TakenDate,
        ElementAttribute.CreateDate,
        ElementAttribute.OffsetTime,
        ElementAttribute.GUID
    };

    /// <summary>
    ///     This is the list of/for attribs that are considered "geodata"
    /// </summary>
    public static readonly List<ElementAttribute> GeoDataAttributes = new()
    {
        ElementAttribute.GPSLatitude,
        ElementAttribute.GPSLatitudeRef,
        ElementAttribute.GPSLongitude,
        ElementAttribute.GPSLongitudeRef,
        ElementAttribute.GPSDestLatitude,
        ElementAttribute.GPSDestLatitudeRef,
        ElementAttribute.GPSDestLongitude,
        ElementAttribute.GPSDestLongitudeRef,
        ElementAttribute.CountryCode,
        ElementAttribute.Country,
        ElementAttribute.City,
        ElementAttribute.State,
        ElementAttribute.Sub_location,
        ElementAttribute.GPSAltitude,
        ElementAttribute.GPSAltitudeRef
    };

    /// <summary>
    ///     This defines the "tags-in" (as in exif tags) for each EA. The order does matter as it's basically sorted.
    /// </summary>
    public static readonly IDictionary<ElementAttribute, List<string>> TagsToAttributesIn =
        new Dictionary<ElementAttribute, List<string>>
        {
            {
                ElementAttribute.City, new List<string>
                {
                    "XMP:City",
                    "IPTC:City"
                }
            },
            {
                ElementAttribute.Country, new List<string>
                {
                    "XMP:Country",
                    "IPTC:Country-PrimaryLocationName"
                }
            },
            {
                ElementAttribute.CountryCode, new List<string>
                {
                    "XMP:CountryCode",
                    "IPTC:Country-PrimaryLocationCode"
                }
            },
            {
                ElementAttribute.CreateDate, new List<string>
                {
                    "XMP:CreateDate",
                    "EXIF:CreateDate"
                }
            },
            {
                ElementAttribute.ExposureTime, new List<string>
                {
                    "XMP:ExposureTime",
                    "EXIF:ExposureTime"
                }
            },
            {
                ElementAttribute.Fnumber, new List<string>
                {
                    "XMP:FNumber",
                    "EXIF:FNumber"
                }
            },
            {
                ElementAttribute.FocalLength, new List<string>
                {
                    "XMP:FocalLength",
                    "EXIF:FocalLength"
                }
            },
            {
                ElementAttribute.FocalLengthIn35mmFormat, new List<string>
                {
                    "XMP:FocalLengthIn35mmFormat",
                    "EXIF:FocalLengthIn35mmFormat",
                    "Composite:FocalLength35efl"
                }
            },
            {
                ElementAttribute.GPSAltitude, new List<string>
                {
                    "XMP:GPSAltitude",
                    "EXIF:GPSAltitude",
                    "GPS:GPSAltitude"
                }
            },
            {
                ElementAttribute.GPSAltitudeRef, new List<string>
                {
                    "XMP:GPSAltitudeRef",
                    "EXIF:GPSAltitudeRef",
                    "GPS:GPSAltitudeRef"
                }
            },
            {
                ElementAttribute.GPSDestLatitude, new List<string>
                {
                    "XMP:GPSDestLatitude",
                    "EXIF:GPSDestLatitude",
                    "GPS:GPSDestLatitude"
                }
            },
            {
                ElementAttribute.GPSDestLatitudeRef, new List<string>
                {
                    "EXIF:GPSDestLatitudeRef",
                    "GPS:GPSDestLatitudeRef"
                }
            },
            {
                ElementAttribute.GPSDestLongitude, new List<string>
                {
                    "XMP:GPSDestLongitude",
                    "EXIF:GPSDestLongitude",
                    "GPS:GPSDestLongitude"
                }
            },
            {
                ElementAttribute.GPSDestLongitudeRef, new List<string>
                {
                    "EXIF:GPSDestLongitudeRef",
                    "GPS:GPSDestLongitudeRef"
                }
            },
            {
                ElementAttribute.GPSImgDirection, new List<string>
                {
                    "XMP:GPSImgDirection",
                    "EXIF:GPSImgDirection"
                }
            },
            {
                ElementAttribute.GPSImgDirectionRef, new List<string>
                {
                    "XMP:GPSImgDirectionRef",
                    "EXIF:GPSImgDirectionRef"
                }
            },
            {
                ElementAttribute.GPSLatitude, new List<string>
                {
                    "XMP:GPSLatitude",
                    "EXIF:GPSLatitude",
                    "GPS:GPSLatitude"
                }
            },
            {
                ElementAttribute.GPSLatitudeRef, new List<string>
                {
                    "EXIF:GPSLatitudeRef",
                    "GPS:GPSLatitudeRef"
                }
            },
            {
                ElementAttribute.GPSLongitude, new List<string>
                {
                    "XMP:GPSLongitude",
                    "EXIF:GPSLongitude",
                    "GPS:GPSLongitude"
                }
            },
            {
                ElementAttribute.GPSLongitudeRef, new List<string>
                {
                    "EXIF:GPSLongitudeRef",
                    "GPS:GPSLongitudeRef"
                }
            },
            {
                ElementAttribute.GPSSpeed, new List<string>
                {
                    "XMP:GPSSpeed",
                    "EXIF:GPSSpeed"
                }
            },
            {
                ElementAttribute.GPSSpeedRef, new List<string>
                {
                    "XMP:GPSSpeedRef",
                    "EXIF:GPSSpeedRef"
                }
            },
            {
                ElementAttribute.ISO, new List<string>
                {
                    "XMP:ISO",
                    "EXIF:ISO",
                    "Composite:ISO"
                }
            },
            {
                ElementAttribute.LensSpec, new List<string>
                {
                    "XMP:LensInfo",
                    "EXIF:LensModel",
                    "Composite:Lens"
                }
            },
            {
                ElementAttribute.Make, new List<string>
                {
                    "XMP:Make",
                    "EXIF:Make"
                }
            },
            {
                ElementAttribute.Model, new List<string>
                {
                    "XMP:Model",
                    "EXIF:Model"
                }
            },
            {
                ElementAttribute.OffsetTime, new List<string>
                {
                    "EXIF:OffsetTimeOriginal",
                    "EXIF:OffsetTime"
                }
            },
            {
                ElementAttribute.Rating, new List<string>
                {
                    "XMP:Rating",
                    "EXIF:Rating"
                }
            },
            {
                ElementAttribute.State, new List<string>
                {
                    "XMP:State",
                    "IPTC:Province-State"
                }
            },
            {
                ElementAttribute.Sub_location, new List<string>
                {
                    "XMP:Location",
                    "IPTC:Sub-location"
                }
            },
            {
                ElementAttribute.TakenDate, new List<string>
                {
                    "XMP:DateTimeOriginal",
                    "EXIF:DateTimeOriginal"
                }
            }
        };

    /// <summary>
    ///     This defines the "tags-out" (as in exif tags) for each EA. The order does not matter as they all get written.
    /// </summary>
    public static readonly IDictionary<ElementAttribute, List<string>> TagsToAttributesOut =
        new Dictionary<ElementAttribute, List<string>>
        {
            {
                ElementAttribute.City, new List<string>
                {
                    "City",
                    "XMP-photoshop:City"
                }
            },
            {
                ElementAttribute.Country, new List<string>
                {
                    "Country",
                    "XMP-photoshop:Country",
                    "IPTC:Country-PrimaryLocationName"
                }
            },
            {
                ElementAttribute.CountryCode, new List<string>
                {
                    "CountryCode",
                    "XMP-iptcCore:CountryCode",
                    "IPTC:Country-PrimaryLocationCode"
                }
            },
            {
                ElementAttribute.CreateDate, new List<string>
                {
                    "XMP:CreateDate",
                    "EXIF:CreateDate"
                }
            },
            {
                ElementAttribute.ExposureTime, new List<string>
                {
                    "exif:ExposureTime"
                }
            },
            {
                ElementAttribute.GPSAltitude, new List<string>
                {
                    "GPS:GPSAltitude",
                    "exif:GPSAltitude",
                    "XMP:GPSAltitude"
                }
            },
            {
                ElementAttribute.GPSAltitudeRef, new List<string>
                {
                    "GPS:GPSAltitudeRef",
                    "exif:GPSAltitudeRef",
                    "XMP:GPSAltitudeRef"
                }
            },
            {
                ElementAttribute.GPSDestLatitude, new List<string>
                {
                    "GPS:GPSDestLatitude",
                    "exif:GPSDestLatitude",
                    "XMP:GPSDestLatitude"
                }
            },
            {
                ElementAttribute.GPSDestLatitudeRef, new List<string>
                {
                    "GPS:GPSDestLatitudeRef",
                    "exif:GPSDestLatitudeRef"
                }
            },
            {
                ElementAttribute.GPSDestLongitude, new List<string>
                {
                    "GPS:GPSDestLongitude",
                    "exif:GPSDestLongitude",
                    "XMP:GPSDestLongitude"
                }
            },
            {
                ElementAttribute.GPSDestLongitudeRef, new List<string>
                {
                    "GPS:GPSDestLongitudeRef",
                    "exif:GPSDestLongitudeRef"
                }
            },
            //{
            //    ElementAttribute.GPSImgDirection, new List<string>
            //    {
            //        "XMP:GPSImgDirection",
            //        "EXIF:GPSImgDirection"
            //    }
            //},
            //{
            //    ElementAttribute.GPSImgDirectionRef, new List<string>
            //    {
            //        "XMP:GPSImgDirectionRef",
            //        "EXIF:GPSImgDirectionRef"
            //    }
            //},
            {
                ElementAttribute.GPSLatitude, new List<string>
                {
                    "GPS:GPSLatitude",
                    "exif:GPSLatitude",
                    "XMP:GPSLatitude"
                }
            },
            {
                ElementAttribute.GPSLatitudeRef, new List<string>
                {
                    "GPS:GPSLatitudeRef",
                    "exif:GPSLatitudeRef"
                }
            },
            {
                ElementAttribute.GPSLongitude, new List<string>
                {
                    "GPS:GPSLongitude",
                    "exif:GPSLongitude",
                    "XMP:GPSLongitude"
                }
            },
            {
                ElementAttribute.GPSLongitudeRef, new List<string>
                {
                    "GPS:GPSLongitudeRef",
                    "exif:GPSLongitudeRef"
                }
            },
            {
                ElementAttribute.GPSSpeed, new List<string>
                {
                    "GPS:GPSSpeed",
                    "exif:GPSSpeed"
                }
            },
            {
                ElementAttribute.GPSSpeedRef, new List<string>
                {
                    "GPS:GPSSpeedRef",
                    "exif:GPSSpeedRef"
                }
            },

            {
                ElementAttribute.OffsetTime, new List<string>
                {
                    "EXIF:OffsetTimeOriginal",
                    "EXIF:OffsetTime"
                }
            },
            {
                ElementAttribute.Rating, new List<string>
                {
                    "XMP:Rating",
                    "EXIF:Rating"
                }
            },
            {
                ElementAttribute.State, new List<string>
                {
                    "State",
                    "XMP-photoshop:State",
                    "IPTC:Province-State"
                }
            },
            {
                ElementAttribute.Sub_location, new List<string>
                {
                    "Sub-location",
                    "XMP-iptcCore:Location"
                }
            },
            {
                ElementAttribute.TakenDate, new List<string>
                {
                    "XMP:DateTimeOriginal",
                    "EXIF:DateTimeOriginal"
                }
            }
        };

    /// <summary>
    ///     Returns the (usually column-header) equivalent of an Attribute (e.g. ElementAttribute.RemoveAllGPS -> "gps*" or
    ///     ElementAttribute.GPSAltitude -> "GPSAltitude")
    /// </summary>
    /// <param name="attribute">ElementAttribute Name e.g. ElementAttribute.GPSAltitude</param>
    /// <returns>string, e.g. "GPSAltitude"</returns>
    /// <exception cref="ArgumentException"></exception>
    public static string GetAttributeName(ElementAttribute attribute)
    {
        switch (attribute)
        {
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
            case ElementAttribute.Coordinates:
                return "Coordinates";
            case ElementAttribute.DestCoordinates:
                return "DestCoordinates";
            case ElementAttribute.City:
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
            case ElementAttribute.TakenDateDaysShift:
                return "TakenDateDaysShift";
            case ElementAttribute.TakenDateHoursShift:
                return "TakenDateHoursShift";
            case ElementAttribute.TakenDateMinutesShift:
                return "TakenDateMinutesShift";
            case ElementAttribute.TakenDateSecondsShift:
                return "TakenDateSecondsShift";
            case ElementAttribute.CreateDateDaysShift:
                return "CreateDateDaysShift";
            case ElementAttribute.CreateDateHoursShift:
                return "CreateDateHoursShift";
            case ElementAttribute.CreateDateMinutesShift:
                return "CreateDateMinutesShift";
            case ElementAttribute.CreateDateSecondsShift:
                return "CreateDateSecondsShift";
            case ElementAttribute.OffsetTime:
                return "OffsetTime";
            case ElementAttribute.RemoveAllGPS:
                return "gps*";
            case ElementAttribute.GUID:
                return "GUID";
            default:
                throw new ArgumentException(message: "Trying to get attribute name of unknown attribute with value " + attribute);
        }
    }

    /// <summary>
    ///     Finds and returns the ElementAttribute eqv of a string (e.g. "gps*" -> ElementAttribute.RemoveAllGPS  or
    ///     "GPSAltitude" -> ElementAttribute.GPSAltitude )
    /// </summary>
    /// <param name="attributeToFind">ColumnHeader or other string (e.g. "gps*", "GPSAltitude")</param>
    /// <returns>ElementAttribute e.g. ElementAttribute.RemoveAllGPS </returns>
    /// <exception cref="ArgumentException"></exception>
    public static ElementAttribute GetAttributeFromString(string attributeToFind)
    {
        switch (attributeToFind)
        {
            case "GPSAltitude":
                return ElementAttribute.GPSAltitude;
            case "GPSAltitudeRef":
                return ElementAttribute.GPSAltitudeRef;
            case "GPSDestLatitude":
                return ElementAttribute.GPSDestLatitude;
            case "GPSDestLatitudeRef":
                return ElementAttribute.GPSDestLatitudeRef;
            case "GPSDestLongitude":
                return ElementAttribute.GPSDestLongitude;
            case "GPSDestLongitudeRef":
                return ElementAttribute.GPSDestLongitudeRef;
            case "Coordinates":
                return ElementAttribute.Coordinates;
            case "DestCoordinates":
                return ElementAttribute.DestCoordinates;
            case "GPSImgDirection":
                return ElementAttribute.GPSImgDirection;
            case "GPSImgDirectionRef":
                return ElementAttribute.GPSImgDirectionRef;
            case "GPSLatitude":
                return ElementAttribute.GPSLatitude;
            case "GPSLatitudeRef":
                return ElementAttribute.GPSLatitudeRef;
            case "GPSLongitude":
                return ElementAttribute.GPSLongitude;
            case "GPSLongitudeRef":
                return ElementAttribute.GPSLongitudeRef;
            case "GPSSpeed":
                return ElementAttribute.GPSSpeed;
            case "GPSSpeedRef":
                return ElementAttribute.GPSSpeedRef;
            case "City":
                return ElementAttribute.City;
            case "CountryCode":
                return ElementAttribute.CountryCode;
            case "Country":
                return ElementAttribute.Country;
            case "State":
                return ElementAttribute.State;
            case "Sub_location":
                return ElementAttribute.Sub_location;
            case "Make":
                return ElementAttribute.Make;
            case "Model":
                return ElementAttribute.Model;
            case "Rating":
                return ElementAttribute.Rating;
            case "ExposureTime":
                return ElementAttribute.ExposureTime;
            case "Fnumber":
                return ElementAttribute.Fnumber;
            case "FocalLength":
                return ElementAttribute.FocalLength;
            case "FocalLengthIn35mmFormat":
                return ElementAttribute.FocalLengthIn35mmFormat;
            case "ISO":
                return ElementAttribute.ISO;
            case "LensSpec":
                return ElementAttribute.LensSpec;
            case "TakenDate":
                return ElementAttribute.TakenDate;
            case "CreateDate":
                return ElementAttribute.CreateDate;
            case "TakenDateDaysShift":
                return ElementAttribute.TakenDateDaysShift;
            case "TakenDateHoursShift":
                return ElementAttribute.TakenDateHoursShift;
            case "TakenDateMinutesShift":
                return ElementAttribute.TakenDateMinutesShift;
            case "TakenDateSecondsShift":
                return ElementAttribute.TakenDateSecondsShift;
            case "CreateDateDaysShift":
                return ElementAttribute.CreateDateDaysShift;
            case "CreateDateHoursShift":
                return ElementAttribute.CreateDateHoursShift;
            case "CreateDateMinutesShift":
                return ElementAttribute.CreateDateMinutesShift;
            case "CreateDateSecondsShift":
                return ElementAttribute.CreateDateSecondsShift;
            case "OffsetTime":
                return ElementAttribute.OffsetTime;
            case "OffsetTimeList":
            case "gps*":
                return ElementAttribute.RemoveAllGPS;
            case "GUID":
                return ElementAttribute.GUID;
            default:
                throw new ArgumentException(message: "Trying to get attribute name of unknown attribute with value " + attributeToFind);
        }
    }

    public static Type GetAttributeType(ElementAttribute attribute)
    {
        switch (attribute)
        {
            case ElementAttribute.GPSAltitude:
                return typeof(double);
            case ElementAttribute.GPSAltitudeRef:
                return typeof(string);
            case ElementAttribute.GPSDestLatitude:
                return typeof(double);
            case ElementAttribute.GPSDestLatitudeRef:
                return typeof(string);
            case ElementAttribute.GPSDestLongitude:
                return typeof(double);
            case ElementAttribute.GPSDestLongitudeRef:
                return typeof(string);
            case ElementAttribute.GPSImgDirection:
                return typeof(double);
            case ElementAttribute.GPSImgDirectionRef:
                return typeof(string);
            case ElementAttribute.GPSLatitude:
                return typeof(double);
            case ElementAttribute.GPSLatitudeRef:
                return typeof(string);
            case ElementAttribute.GPSLongitude:
                return typeof(double);
            case ElementAttribute.GPSLongitudeRef:
                return typeof(string);
            case ElementAttribute.GPSSpeed:
                return typeof(string);
            case ElementAttribute.GPSSpeedRef:
                return typeof(string);
            case ElementAttribute.Coordinates:
                return typeof(string);
            case ElementAttribute.DestCoordinates:
                return typeof(string);
            case ElementAttribute.City:
                return typeof(string);
            case ElementAttribute.CountryCode:
                return typeof(string);
            case ElementAttribute.Country:
                return typeof(string);
            case ElementAttribute.State:
                return typeof(string);
            case ElementAttribute.Sub_location:
                return typeof(string);
            case ElementAttribute.Make:
                return typeof(string);
            case ElementAttribute.Model:
                return typeof(string);
            case ElementAttribute.Rating:
                return typeof(string);
            case ElementAttribute.ExposureTime:
                return typeof(string);
            case ElementAttribute.Fnumber:
                return typeof(double);
            case ElementAttribute.FocalLength:
                return typeof(double);
            case ElementAttribute.FocalLengthIn35mmFormat:
                return typeof(double);
            case ElementAttribute.ISO:
                return typeof(int);
            case ElementAttribute.LensSpec:
                return typeof(string);
            case ElementAttribute.TakenDate:
                return typeof(DateTime);
            case ElementAttribute.CreateDate:
                return typeof(DateTime);
            case ElementAttribute.TakenDateDaysShift:
                return typeof(int);
            case ElementAttribute.TakenDateHoursShift:
                return typeof(int);
            case ElementAttribute.TakenDateMinutesShift:
                return typeof(int);
            case ElementAttribute.TakenDateSecondsShift:
                return typeof(int);
            case ElementAttribute.CreateDateDaysShift:
                return typeof(int);
            case ElementAttribute.CreateDateHoursShift:
                return typeof(int);
            case ElementAttribute.CreateDateMinutesShift:
                return typeof(int);
            case ElementAttribute.CreateDateSecondsShift:
                return typeof(int);
            case ElementAttribute.OffsetTime:
                return typeof(string);
            case ElementAttribute.RemoveAllGPS:
                return typeof(string);
            case ElementAttribute.GUID:
                return typeof(string);
            default:
                throw new ArgumentException(message: "Trying to get attribute type of unknown attribute with value " + attribute);
        }
    }
}