using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace GeoTagNinja.Model;

/// <summary>
///     Provides a set of static methods for retrieving and manipulating attributes associated with the ElementAttribute
///     type.
/// </summary>
/// <remarks>
///     This class is used throughout the GeoTagNinja application to handle operations related to ElementAttribute objects,
///     such as retrieving associated attributes, output attributes, order ID, name, type, and determining if an attribute
///     is related to geographic data.
/// </remarks>
public static class SourcesAndAttributes
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
    ///     A dictionary that maps ElementAttribute values to their corresponding ElementAttributeMapping objects.
    /// </summary>
    /// <remarks>
    ///     This dictionary is used to define the mapping between different types of attributes (e.g., GPS, EXIF, XMP) and
    ///     their corresponding metadata.
    ///     Each ElementAttributeMapping object contains information about the attribute's name, type, input/output attributes,
    ///     and other properties.
    /// </remarks>
    private static readonly IDictionary<ElementAttribute, ElementAttributeMapping> TagsToAttributes =
        new Dictionary<ElementAttribute, ElementAttributeMapping>
        {
            {
                ElementAttribute.GPSAltitude, new ElementAttributeMapping
                {
                    Name = "GPSAltitude",
                    TypeOfElement = typeof(double),
                    InAttributes = new List<string>
                    {
                        "XMP:GPSAltitude",
                        "EXIF:GPSAltitude",
                        "GPS:GPSAltitude"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSAltitude",
                        "exif:GPSAltitude",
                        "XMP:GPSAltitude"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 8,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.GPSAltitudeRef, new ElementAttributeMapping
                {
                    Name = "GPSAltitudeRef",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:GPSAltitudeRef",
                        "EXIF:GPSAltitudeRef",
                        "GPS:GPSAltitudeRef"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSAltitudeRef",
                        "exif:GPSAltitudeRef",
                        "XMP:GPSAltitudeRef"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 9,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.GPSDestLatitude, new ElementAttributeMapping
                {
                    Name = "GPSDestLatitude",
                    TypeOfElement = typeof(double),
                    InAttributes = new List<string>
                    {
                        "XMP:GPSDestLatitude",
                        "EXIF:GPSDestLatitude",
                        "GPS:GPSDestLatitude"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSDestLatitude",
                        "exif:GPSDestLatitude",
                        "XMP:GPSDestLatitude"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 16,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.GPSDestLatitudeRef, new ElementAttributeMapping
                {
                    Name = "GPSDestLatitudeRef",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "EXIF:GPSDestLatitudeRef",
                        "GPS:GPSDestLatitudeRef"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSDestLatitudeRef",
                        "exif:GPSDestLatitudeRef"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 17,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.GPSDestLongitude, new ElementAttributeMapping
                {
                    Name = "GPSDestLongitude",
                    TypeOfElement = typeof(double),
                    InAttributes = new List<string>
                    {
                        "XMP:GPSDestLongitude",
                        "EXIF:GPSDestLongitude",
                        "GPS:GPSDestLongitude"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSDestLongitude",
                        "exif:GPSDestLongitude",
                        "XMP:GPSDestLongitude"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 18,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.GPSDestLongitudeRef, new ElementAttributeMapping
                {
                    Name = "GPSDestLongitudeRef",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "EXIF:GPSDestLongitudeRef",
                        "GPS:GPSDestLongitudeRef"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSDestLongitudeRef",
                        "exif:GPSDestLongitudeRef"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 19,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.GPSImgDirection, new ElementAttributeMapping
                {
                    Name = "GPSImgDirection",
                    TypeOfElement = typeof(double),
                    InAttributes = new List<string>
                    {
                        "XMP:GPSImgDirection",
                        "EXIF:GPSImgDirection"
                    },
                    OutAttributes = new List<string>
                    {
                        //        "XMP:GPSImgDirection",
                        //        "EXIF:GPSImgDirection"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 20
                }
            },
            {
                ElementAttribute.GPSImgDirectionRef, new ElementAttributeMapping
                {
                    Name = "GPSImgDirectionRef",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:GPSImgDirectionRef",
                        "EXIF:GPSImgDirectionRef"
                    },
                    OutAttributes = new List<string>
                    {
                        //        "XMP:GPSImgDirectionRef",
                        //        "EXIF:GPSImgDirectionRef"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 21
                }
            },
            {
                ElementAttribute.GPSLatitude, new ElementAttributeMapping
                {
                    Name = "GPSLatitude",
                    TypeOfElement = typeof(double),
                    InAttributes = new List<string>
                    {
                        "XMP:GPSLatitude",
                        "EXIF:GPSLatitude",
                        "GPS:GPSLatitude"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSLatitude",
                        "exif:GPSLatitude",
                        "XMP:GPSLatitude"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 2,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.GPSLatitudeRef, new ElementAttributeMapping
                {
                    Name = "GPSLatitudeRef",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "EXIF:GPSLatitudeRef",
                        "GPS:GPSLatitudeRef"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSLatitudeRef",
                        "exif:GPSLatitudeRef"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 3,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.GPSLongitude, new ElementAttributeMapping
                {
                    Name = "GPSLongitude",
                    TypeOfElement = typeof(double),
                    InAttributes = new List<string>
                    {
                        "XMP:GPSLongitude",
                        "EXIF:GPSLongitude",
                        "GPS:GPSLongitude"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSLongitude",
                        "exif:GPSLongitude",
                        "XMP:GPSLongitude"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 4,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.GPSLongitudeRef, new ElementAttributeMapping
                {
                    Name = "GPSLongitudeRef",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "EXIF:GPSLongitudeRef",
                        "GPS:GPSLongitudeRef"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSLongitudeRef",
                        "exif:GPSLongitudeRef"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 5,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.GPSSpeed, new ElementAttributeMapping
                {
                    Name = "GPSSpeed",
                    TypeOfElement = typeof(double),
                    InAttributes = new List<string>
                    {
                        "XMP:GPSSpeed",
                        "EXIF:GPSSpeed"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSSpeed",
                        "exif:GPSSpeed"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 6
                }
            },
            {
                ElementAttribute.GPSSpeedRef, new ElementAttributeMapping
                {
                    Name = "GPSSpeedRef",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:GPSSpeedRef",
                        "EXIF:GPSSpeedRef"
                    },
                    OutAttributes = new List<string>
                    {
                        "GPS:GPSSpeedRef",
                        "exif:GPSSpeedRef"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 7
                }
            },
            {
                ElementAttribute.Coordinates, new ElementAttributeMapping
                {
                    Name = "Coordinates",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 1
                }
            },
            {
                ElementAttribute.DestCoordinates, new ElementAttributeMapping
                {
                    Name = "DestCoordinates",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 15
                }
            },
            {
                ElementAttribute.City, new ElementAttributeMapping
                {
                    Name = "City",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:City",
                        "IPTC:City"
                    },
                    OutAttributes = new List<string>
                    {
                        "City",
                        "XMP-photoshop:City"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 13,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.CountryCode, new ElementAttributeMapping
                {
                    Name = "CountryCode",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:CountryCode",
                        "IPTC:Country-PrimaryLocationCode"
                    },
                    OutAttributes = new List<string>
                    {
                        "CountryCode",
                        "XMP-iptcCore:CountryCode",
                        "IPTC:Country-PrimaryLocationCode"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 11,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.Country, new ElementAttributeMapping
                {
                    Name = "Country",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:Country",
                        "IPTC:Country-PrimaryLocationName"
                    },
                    OutAttributes = new List<string>
                    {
                        "Country",
                        "XMP-photoshop:Country",
                        "IPTC:Country-PrimaryLocationName"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 10,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.State, new ElementAttributeMapping
                {
                    Name = "State",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:State",
                        "IPTC:Province-State"
                    },
                    OutAttributes = new List<string>
                    {
                        "State",
                        "XMP-photoshop:State",
                        "IPTC:Province-State"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 12,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.Sub_location, new ElementAttributeMapping
                {
                    Name = "Sub_location",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:Location",
                        "IPTC:Sub-location"
                    },
                    OutAttributes = new List<string>
                    {
                        "Sub-location",
                        "XMP-iptcCore:Location"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 14,
                    isGeoData = true
                }
            },
            {
                ElementAttribute.Make, new ElementAttributeMapping
                {
                    Name = "Make",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:Make",
                        "EXIF:Make"
                    },
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 22
                }
            },
            {
                ElementAttribute.Model, new ElementAttributeMapping
                {
                    Name = "Model",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:Model",
                        "EXIF:Model"
                    },
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 23
                }
            },
            {
                ElementAttribute.Rating, new ElementAttributeMapping
                {
                    Name = "Rating",
                    TypeOfElement = typeof(int),
                    InAttributes = new List<string>
                    {
                        "XMP:Rating",
                        "EXIF:Rating"
                    },
                    OutAttributes = new List<string>
                    {
                        "XMP:Rating",
                        "EXIF:Rating"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 24
                }
            },
            {
                ElementAttribute.ExposureTime, new ElementAttributeMapping
                {
                    Name = "ExposureTime",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:ExposureTime",
                        "EXIF:ExposureTime"
                    },
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 25
                }
            },
            {
                ElementAttribute.Fnumber, new ElementAttributeMapping
                {
                    Name = "Fnumber",
                    TypeOfElement = typeof(double),
                    InAttributes = new List<string>
                    {
                        "XMP:FNumber",
                        "EXIF:FNumber"
                    },
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 26
                }
            },
            {
                ElementAttribute.FocalLength, new ElementAttributeMapping
                {
                    Name = "FocalLength",
                    TypeOfElement = typeof(double),
                    InAttributes = new List<string>
                    {
                        "XMP:FocalLength",
                        "EXIF:FocalLength"
                    },
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 27
                }
            },
            {
                ElementAttribute.FocalLengthIn35mmFormat, new ElementAttributeMapping
                {
                    Name = "FocalLengthIn35mmFormat",
                    TypeOfElement = typeof(double),
                    InAttributes = new List<string>
                    {
                        "XMP:FocalLengthIn35mmFormat",
                        "EXIF:FocalLengthIn35mmFormat",
                        "Composite:FocalLength35efl"
                    },
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 28
                }
            },
            {
                ElementAttribute.ISO, new ElementAttributeMapping
                {
                    Name = "ISO",
                    TypeOfElement = typeof(int),
                    InAttributes = new List<string>
                    {
                        "XMP:ISO",
                        "EXIF:ISO",
                        "Composite:ISO"
                    },
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 29
                }
            },
            {
                ElementAttribute.LensSpec, new ElementAttributeMapping
                {
                    Name = "LensSpec",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "XMP:LensInfo",
                        "EXIF:LensModel",
                        "Composite:Lens"
                    },
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 30
                }
            },
            {
                ElementAttribute.TakenDate, new ElementAttributeMapping
                {
                    Name = "TakenDate",
                    TypeOfElement = typeof(DateTime),
                    InAttributes = new List<string>
                    {
                        "XMP:DateTimeOriginal",
                        "EXIF:DateTimeOriginal"
                    },
                    OutAttributes = new List<string>
                    {
                        "XMP:DateTimeOriginal",
                        "EXIF:DateTimeOriginal"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 31
                }
            },
            {
                ElementAttribute.CreateDate, new ElementAttributeMapping
                {
                    Name = "CreateDate",
                    TypeOfElement = typeof(DateTime),
                    InAttributes = new List<string>
                    {
                        "XMP:CreateDate",
                        "EXIF:CreateDate"
                    },
                    OutAttributes = new List<string>
                    {
                        "XMP:CreateDate",
                        "EXIF:CreateDate"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 32
                }
            },
            {
                ElementAttribute.TakenDateDaysShift, new ElementAttributeMapping
                {
                    Name = "TakenDateDaysShift",
                    TypeOfElement = typeof(int),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>()
                }
            },
            {
                ElementAttribute.TakenDateHoursShift, new ElementAttributeMapping
                {
                    Name = "TakenDateHoursShift",
                    TypeOfElement = typeof(int),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>()
                }
            },
            {
                ElementAttribute.TakenDateMinutesShift, new ElementAttributeMapping
                {
                    Name = "TakenDateMinutesShift",
                    TypeOfElement = typeof(int),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>()
                }
            },
            {
                ElementAttribute.TakenDateSecondsShift, new ElementAttributeMapping
                {
                    Name = "TakenDateSecondsShift",
                    TypeOfElement = typeof(int),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>()
                }
            },
            {
                ElementAttribute.CreateDateDaysShift, new ElementAttributeMapping
                {
                    Name = "CreateDateDaysShift",
                    TypeOfElement = typeof(int),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>()
                }
            },
            {
                ElementAttribute.CreateDateHoursShift, new ElementAttributeMapping
                {
                    Name = "CreateDateHoursShift",
                    TypeOfElement = typeof(int),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>()
                }
            },
            {
                ElementAttribute.CreateDateMinutesShift, new ElementAttributeMapping
                {
                    Name = "CreateDateMinutesShift",
                    TypeOfElement = typeof(int),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>()
                }
            },
            {
                ElementAttribute.CreateDateSecondsShift, new ElementAttributeMapping
                {
                    Name = "CreateDateSecondsShift",
                    TypeOfElement = typeof(int),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>()
                }
            },
            {
                ElementAttribute.OffsetTime, new ElementAttributeMapping
                {
                    Name = "OffsetTime",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>
                    {
                        "EXIF:OffsetTimeOriginal",
                        "EXIF:OffsetTime"
                    },
                    OutAttributes = new List<string>
                    {
                        "EXIF:OffsetTimeOriginal",
                        "EXIF:OffsetTime"
                    },
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 33
                }
            },
            {
                ElementAttribute.RemoveAllGPS, new ElementAttributeMapping
                {
                    Name = "gps*",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>()
                }
            },
            {
                ElementAttribute.GUID, new ElementAttributeMapping
                {
                    Name = "GUID",
                    TypeOfElement = typeof(string),
                    InAttributes = new List<string>(),
                    OutAttributes = new List<string>(),
                    IPTCKeywords = new List<string>(),
                    XMPSubjects = new List<string>(),
                    OrderID = 34
                }
            }
        };


    /// <summary>
    ///     Retrieves the list of attributes associated with a given attributeToFind.
    /// </summary>
    /// <param name="attributeToFind">
    ///     The attributeToFind of type ElementAttribute for which the associated attributes are to be
    ///     retrieved.
    /// </param>
    /// <returns>
    ///     A list of attributes associated with the given attributeToFind. If the attributeToFind does not have any associated
    ///     attributes, returns null.
    /// </returns>
    public static List<string> GetElementAttributesIn(ElementAttribute attributeToFind)

    {
        if (TagsToAttributes.TryGetValue(key: attributeToFind, value: out ElementAttributeMapping mapping))
        {
            if (mapping.InAttributes.Count > 0)
            {
                return mapping.InAttributes;
            }
        }

        return null;
    }

    /// <summary>
    ///     Retrieves the output attributes associated with a given attributeToFind.
    /// </summary>
    /// <param name="attributeToFind">The attributeToFind of type ElementAttribute for which to retrieve the output attributes.</param>
    /// <returns>
    ///     A list of output attributes associated with the specified attributeToFind. Returns null if no attributes are
    ///     found.
    /// </returns>
    public static List<string> GetElementAttributesOut(ElementAttribute attributeToFind)

    {
        if (TagsToAttributes.TryGetValue(key: attributeToFind, value: out ElementAttributeMapping mapping))
        {
            if (mapping.OutAttributes.Count > 0)
            {
                return mapping.OutAttributes;
            }
        }

        return null;
    }

    /// <summary>
    ///     Gets the order ID of the specified attributeToFind attribute.
    /// </summary>
    /// <param name="attributeToFind">The attributeToFind attribute for which to get the order ID.</param>
    /// <returns>The order ID of the specified attributeToFind attribute if found; otherwise, null.</returns>
    public static int GetElementAttributesOrderID(ElementAttribute attributeToFind)
    {
        if (TagsToAttributes.TryGetValue(key: attributeToFind, value: out ElementAttributeMapping mapping))
        {
            return mapping.OrderID;
        }

        return -1;
    }

    /// <summary>
    ///     Retrieves the name of the specified attributeToFind attribute.
    /// </summary>
    /// <param name="attributeToFind">The attributeToFind attribute to get the name for.</param>
    /// <returns>
    ///     The name of the specified attributeToFind attribute. If the attributeToFind attribute does not exist, it returns a
    ///     null
    ///     string
    ///     equivalent.
    /// </returns>
    public static string GetElementAttributesName(ElementAttribute attributeToFind)
    {
        if (TagsToAttributes.TryGetValue(key: attributeToFind, value: out ElementAttributeMapping mapping))
        {
            if (!string.IsNullOrWhiteSpace(value: mapping.Name))
            {
                return mapping.Name;
            }
        }

        return FrmMainApp.NullStringEquivalentBlank;
    }

    /// <summary>
    ///     Retrieves the ElementAttribute corresponding to the provided name.
    /// </summary>
    /// <param name="attributeToFind">The name of the ElementAttribute to retrieve</param>
    /// <returns>The ElementAttribute corresponding to the provided name.</returns>
    /// <exception cref="ArgumentException">Thrown when an ElementAttribute with the provided name does not exist.</exception>
    public static ElementAttribute GetElementAttributesElementAttribute(string attributeToFind)
    {
        foreach (KeyValuePair<ElementAttribute, ElementAttributeMapping> keyValuePair in TagsToAttributes)
        {
            if (keyValuePair.Value.Name == attributeToFind)
            {
                return keyValuePair.Key;
            }
        }

        throw new ArgumentException(message: $"Element attribute '{attributeToFind}' does not exist.");
    }

    /// <summary>
    ///     Gets the type of the specified ElementAttribute.
    /// </summary>
    /// <param name="attributeToFind">The ElementAttribute to find the type of.</param>
    /// <returns>The type of the specified ElementAttribute.</returns>
    /// <exception cref="ArgumentException">Thrown when the specified ElementAttribute does not exist.</exception>
    public static Type GetElementAttributesType(ElementAttribute attributeToFind)
    {
        if (TagsToAttributes.TryGetValue(key: attributeToFind, value: out ElementAttributeMapping mapping))
        {
            return mapping.TypeOfElement;
        }

        throw new ArgumentException(message: $"Element attribute '{attributeToFind}' does not exist.");
    }

    /// <summary>
    ///     Determines whether the specified attributeToFind attribute is related to geographic data.
    /// </summary>
    /// <param name="attributeToFind">The attributeToFind attribute to check.</param>
    /// <returns>Returns true if the specified attributeToFind attribute is related to geographic data, otherwise false.</returns>
    public static bool GetElementAttributesIsGeoData(ElementAttribute attributeToFind)
    {
        if (TagsToAttributes.TryGetValue(key: attributeToFind,
                                         value: out ElementAttributeMapping mapping))
        {
            return mapping.isGeoData;
        }

        return false;
    }

    /// <summary>
    ///     Represents a mapping between an element and its attributes in the GeoTagNinja application.
    /// </summary>
    /// <remarks>
    ///     This struct is used to map an element to its attributes, including input and output attributes, IPTC keywords, XMP
    ///     subjects, and geodata information.
    ///     It also contains the order ID for the element and a flag indicating whether the element contains geodata.
    /// </remarks>
    private struct ElementAttributeMapping
    {
        public string Name;
        public Type TypeOfElement;
        public List<string> InAttributes;
        public List<string> OutAttributes;
        public List<string> IPTCKeywords;
        public List<string> XMPSubjects;
        public int OrderID;
        public bool isGeoData;
    }
}