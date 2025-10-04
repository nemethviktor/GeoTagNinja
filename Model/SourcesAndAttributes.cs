using System;
using System.Collections.Generic;
using static GeoTagNinja.View.ListView.FileListView;

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
    private const int ORDER_INCREMENT_VALUE = 1;

    /// <summary>
    ///     ElementAttributes available. Do not reorder the list as the order of apperance is directly translated to the order
    ///     in which items appear in the columns of the listviews.
    ///     When inserting new items that do not require an order ID (ie stuff the users don't see or aren't real tags per se),
    ///     put them at the end.
    /// </summary>
    public enum ElementAttribute
    {
        Folder,
        Coordinates,
        GPSLatitude,
        GPSLatitudeRef,
        GPSLongitude,
        GPSLongitudeRef,
        GPSSpeed,
        GPSSpeedRef,
        GPSAltitude,
        GPSAltitudeRef,
        Country,
        CountryCode,
        State,
        City,
        Sublocation,
        DestCoordinates,
        GPSDestLatitude,
        GPSDestLatitudeRef,
        GPSDestLongitude,
        GPSDestLongitudeRef,
        GPSImgDirection,
        GPSImgDirectionRef,
        GPSDOP,
        GPSHPositioningError,
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
        GPSDateTime,
        OffsetTime,
        IPTCKeywords,
        XMLSubjects,

        // items without OrderID below this line

        TakenDateDaysShift,
        TakenDateHoursShift,
        TakenDateMinutesShift,
        TakenDateSecondsShift,
        CreateDateDaysShift,
        CreateDateHoursShift,
        CreateDateMinutesShift,
        CreateDateSecondsShift,
        RemoveAllGPS,
        GUID,
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
    private static readonly IDictionary<ElementAttribute, ElementAttributeMapping>
        TagsToAttributes =
            new Dictionary<ElementAttribute, ElementAttributeMapping>
            {
                {
                    ElementAttribute.GPSAltitude, new ElementAttributeMapping
                    {
                        Name = "GPSAltitude",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPS_ALTITUDE,
                        TypeOfElement = typeof(double),
                        InAttributes =
                        [
                            "XMP:GPSAltitude",
                            "EXIF:GPSAltitude",
                            "GPS:GPSAltitude"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSAltitude",
                            "exif:GPSAltitude",
                            "XMP:GPSAltitude"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSAltitude,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.GPSAltitudeRef, new ElementAttributeMapping
                    {
                        Name = "GPSAltitudeRef",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPS_ALTITUDE_REF,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:GPSAltitudeRef",
                            "EXIF:GPSAltitudeRef",
                            "GPS:GPSAltitudeRef"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSAltitudeRef",
                            "exif:GPSAltitudeRef",
                            "XMP:GPSAltitudeRef"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSAltitudeRef,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.GPSDestLatitude, new ElementAttributeMapping
                    {
                        Name = "GPSDestLatitude",
                        ColumnHeader =
                            COL_NAME_PREFIX + FileListColumns.GPS_DEST_LATITUDE,
                        TypeOfElement = typeof(double),
                        InAttributes =
                        [
                            "XMP:GPSDestLatitude",
                            "EXIF:GPSDestLatitude",
                            "GPS:GPSDestLatitude"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSDestLatitude",
                            "exif:GPSDestLatitude",
                            "XMP:GPSDestLatitude"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSDestLatitude,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.GPSDestLatitudeRef, new ElementAttributeMapping
                    {
                        Name = "GPSDestLatitudeRef",
                        ColumnHeader = COL_NAME_PREFIX +
                                       FileListColumns.GPS_DEST_LATITUDE_REF,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "EXIF:GPSDestLatitudeRef",
                            "GPS:GPSDestLatitudeRef"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSDestLatitudeRef",
                            "exif:GPSDestLatitudeRef"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSDestLatitudeRef,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.GPSDestLongitude, new ElementAttributeMapping
                    {
                        Name = "GPSDestLongitude",
                        ColumnHeader =
                            COL_NAME_PREFIX + FileListColumns.GPS_DEST_LONGITUDE,
                        TypeOfElement = typeof(double),
                        InAttributes =
                        [
                            "XMP:GPSDestLongitude",
                            "EXIF:GPSDestLongitude",
                            "GPS:GPSDestLongitude"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSDestLongitude",
                            "exif:GPSDestLongitude",
                            "XMP:GPSDestLongitude"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSDestLongitude,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.GPSDestLongitudeRef, new ElementAttributeMapping
                    {
                        Name = "GPSDestLongitudeRef",
                        ColumnHeader = COL_NAME_PREFIX +
                                       FileListColumns.GPS_DEST_LONGITUDE_REF,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "EXIF:GPSDestLongitudeRef",
                            "GPS:GPSDestLongitudeRef"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSDestLongitudeRef",
                            "exif:GPSDestLongitudeRef"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSDestLongitudeRef,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.GPSImgDirection, new ElementAttributeMapping
                    {
                        Name = "GPSImgDirection",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPS_IMGDIRECTION,
                        TypeOfElement = typeof(double),
                        InAttributes =
                        [
                            "XMP:GPSImgDirection",
                            "EXIF:GPSImgDirection"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSImgDirection
                    }
                },
                {
                    ElementAttribute.GPSImgDirectionRef, new ElementAttributeMapping
                    {
                        Name = "GPSImgDirectionRef",
                        ColumnHeader = COL_NAME_PREFIX +
                                       FileListColumns.GPS_IMGDIRECTION_REF,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:GPSImgDirectionRef",
                            "EXIF:GPSImgDirectionRef"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSImgDirectionRef
                    }
                },
                {
                    ElementAttribute.GPSLatitude, new ElementAttributeMapping
                    {
                        Name = "GPSLatitude",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPS_LATITUDE,
                        TypeOfElement = typeof(double),
                        InAttributes =
                        [
                            "XMP:GPSLatitude",
                            "EXIF:GPSLatitude",
                            "GPS:GPSLatitude"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSLatitude",
                            "exif:GPSLatitude",
                            "XMP:GPSLatitude"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSLatitude,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.GPSLatitudeRef, new ElementAttributeMapping
                    {
                        Name = "GPSLatitudeRef",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPS_LATITUDE_REF,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "EXIF:GPSLatitudeRef",
                            "GPS:GPSLatitudeRef"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSLatitudeRef",
                            "exif:GPSLatitudeRef"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSLatitudeRef,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.GPSLongitude, new ElementAttributeMapping
                    {
                        Name = "GPSLongitude",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPS_LONGITUDE,
                        TypeOfElement = typeof(double),
                        InAttributes =
                        [
                            "XMP:GPSLongitude",
                            "EXIF:GPSLongitude",
                            "GPS:GPSLongitude"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSLongitude",
                            "exif:GPSLongitude",
                            "XMP:GPSLongitude"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSLongitude,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.GPSLongitudeRef, new ElementAttributeMapping
                    {
                        Name = "GPSLongitudeRef",
                        ColumnHeader =
                            COL_NAME_PREFIX + FileListColumns.GPS_LONGITUDE_REF,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "EXIF:GPSLongitudeRef",
                            "GPS:GPSLongitudeRef"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSLongitudeRef",
                            "exif:GPSLongitudeRef"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSLongitudeRef,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.GPSSpeed, new ElementAttributeMapping
                    {
                        Name = "GPSSpeed",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPS_SPEED,
                        TypeOfElement = typeof(double),
                        InAttributes =
                        [
                            "XMP:GPSSpeed",
                            "EXIF:GPSSpeed"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSSpeed",
                            "exif:GPSSpeed"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSSpeed
                    }
                },
                {
                    ElementAttribute.GPSSpeedRef, new ElementAttributeMapping
                    {
                        Name = "GPSSpeedRef",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPS_SPEED_REF,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:GPSSpeedRef",
                            "EXIF:GPSSpeedRef"
                        ],
                        OutAttributes =
                        [
                            "GPS:GPSSpeedRef",
                            "exif:GPSSpeedRef"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSSpeedRef
                    }
                },
                {
                    ElementAttribute.Coordinates, new ElementAttributeMapping
                    {
                        Name = "Coordinates",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.COORDINATES,
                        TypeOfElement = typeof(string),
                        InAttributes = [],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.Coordinates
                    }
                },
                {
                    ElementAttribute.DestCoordinates, new ElementAttributeMapping
                    {
                        Name = "DestCoordinates",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.DEST_COORDINATES,
                        TypeOfElement = typeof(string),
                        InAttributes = [],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.DestCoordinates
                    }
                },
                {
                    ElementAttribute.City, new ElementAttributeMapping
                    {
                        Name = "City",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.CITY,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:City",
                            "IPTC:City"
                        ],
                        OutAttributes =
                        [
                            "City",
                            "XMP-photoshop:City"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.City,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.CountryCode, new ElementAttributeMapping
                    {
                        Name = "CountryCode",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.COUNTRY_CODE,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:CountryCode",
                            "IPTC:Country-PrimaryLocationCode"
                        ],
                        OutAttributes =
                        [
                            "CountryCode",
                            "XMP-iptcCore:CountryCode",
                            "IPTC:Country-PrimaryLocationCode"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.CountryCode,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.Country, new ElementAttributeMapping
                    {
                        Name = "Country",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.COUNTRY,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:Country",
                            "IPTC:Country-PrimaryLocationName"
                        ],
                        OutAttributes =
                        [
                            "Country",
                            "XMP-photoshop:Country",
                            "IPTC:Country-PrimaryLocationName"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.Country,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.State, new ElementAttributeMapping
                    {
                        Name = "State",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.STATE,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:State",
                            "IPTC:Province-State"
                        ],
                        OutAttributes =
                        [
                            "State",
                            "XMP-photoshop:State",
                            "IPTC:Province-State"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.State,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.Sublocation, new ElementAttributeMapping
                    {
                        Name = "Sublocation",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.Sublocation,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:Location",
                            "IPTC:Sub-location"
                        ],
                        OutAttributes =
                        [
                            "Sub-location",
                            "XMP-iptcCore:Location"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.Sublocation,
                        isGeoData = true
                    }
                },
                {
                    ElementAttribute.Make, new ElementAttributeMapping
                    {
                        Name = "Make",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.MAKE,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:Make",
                            "EXIF:Make"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.Make
                    }
                },
                {
                    ElementAttribute.Model, new ElementAttributeMapping
                    {
                        Name = "Model",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.MODEL,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:Model",
                            "EXIF:Model"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.Model
                    }
                },
                {
                    ElementAttribute.Rating, new ElementAttributeMapping
                    {
                        Name = "Rating",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.RATING,
                        TypeOfElement = typeof(int),
                        InAttributes =
                        [
                            "XMP:Rating",
                            "EXIF:Rating"
                        ],
                        OutAttributes =
                        [
                            "XMP:Rating",
                            "EXIF:Rating"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.Rating
                    }
                },
                {
                    ElementAttribute.ExposureTime, new ElementAttributeMapping
                    {
                        Name = "ExposureTime",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.EXPOSURETIME,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:ExposureTime",
                            "EXIF:ExposureTime"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.ExposureTime
                    }
                },
                {
                    ElementAttribute.Fnumber, new ElementAttributeMapping
                    {
                        Name = "Fnumber",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.FNUMBER,
                        TypeOfElement = typeof(double),
                        InAttributes =
                        [
                            "XMP:FNumber",
                            "EXIF:FNumber"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.Fnumber
                    }
                },
                {
                    ElementAttribute.FocalLength, new ElementAttributeMapping
                    {
                        Name = "FocalLength",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.FOCAL_LENGTH,
                        TypeOfElement = typeof(double),
                        InAttributes =
                        [
                            "XMP:FocalLength",
                            "EXIF:FocalLength"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.FocalLength
                    }
                },
                {
                    ElementAttribute.FocalLengthIn35mmFormat, new ElementAttributeMapping
                    {
                        Name = "FocalLengthIn35mmFormat",
                        ColumnHeader = COL_NAME_PREFIX +
                                       FileListColumns.FOCAL_LENGTH_IN_35MM_FORMAT,
                        TypeOfElement = typeof(double),
                        InAttributes =
                        [
                            "XMP:FocalLengthIn35mmFormat",
                            "EXIF:FocalLengthIn35mmFormat",
                            "Composite:FocalLength35efl"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.FocalLengthIn35mmFormat
                    }
                },
                {
                    ElementAttribute.ISO, new ElementAttributeMapping
                    {
                        Name = "ISO",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.ISO,
                        TypeOfElement = typeof(int),
                        InAttributes =
                        [
                            "XMP:ISO",
                            "EXIF:ISO",
                            "Composite:ISO"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.ISO
                    }
                },
                {
                    ElementAttribute.LensSpec, new ElementAttributeMapping
                    {
                        Name = "LensSpec",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.LENS_SPEC,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "XMP:LensInfo",
                            "EXIF:LensModel",
                            "Composite:Lens"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.LensSpec
                    }
                },
                {
                    ElementAttribute.TakenDate, new ElementAttributeMapping
                    {
                        Name = "TakenDate",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.TAKEN_DATE,
                        TypeOfElement = typeof(DateTime),
                        InAttributes =
                        [
                            "XMP:DateTimeOriginal",
                            "EXIF:DateTimeOriginal"
                        ],
                        OutAttributes =
                        [
                            "XMP:DateTimeOriginal",
                            "EXIF:DateTimeOriginal"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.TakenDate
                    }
                },
                {
                    ElementAttribute.CreateDate, new ElementAttributeMapping
                    {
                        Name = "CreateDate",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.CREATE_DATE,
                        TypeOfElement = typeof(DateTime),
                        InAttributes =
                        [
                            "XMP:CreateDate",
                            "EXIF:CreateDate"
                        ],
                        OutAttributes =
                        [
                            "XMP:CreateDate",
                            "EXIF:CreateDate"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.CreateDate
                    }
                },
                {
                    ElementAttribute.GPSDateTime, new ElementAttributeMapping
                    {
                        Name = "GPSDateTime",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPS_DATETIME,
                        TypeOfElement = typeof(DateTime),
                        InAttributes =
                        [
                            "Composite:GPSDateTime"
                        ],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSDateTime
                    }
                },
                {
                    ElementAttribute.TakenDateDaysShift, new ElementAttributeMapping
                    {
                        Name = "TakenDateDaysShift",
                        // ColumnHeader = COL_NAME_PREFIX + FileListColumns. , 
                        TypeOfElement = typeof(int),
                        InAttributes = [],
                        OutAttributes = []
                    }
                },
                {
                    ElementAttribute.TakenDateHoursShift, new ElementAttributeMapping
                    {
                        Name = "TakenDateHoursShift",
                        // ColumnHeader = COL_NAME_PREFIX + FileListColumns. , 
                        TypeOfElement = typeof(int),
                        InAttributes = [],
                        OutAttributes = []
                    }
                },
                {
                    ElementAttribute.TakenDateMinutesShift, new ElementAttributeMapping
                    {
                        Name = "TakenDateMinutesShift",
                        // ColumnHeader = COL_NAME_PREFIX + FileListColumns. , 
                        TypeOfElement = typeof(int),
                        InAttributes = [],
                        OutAttributes = []
                    }
                },
                {
                    ElementAttribute.TakenDateSecondsShift, new ElementAttributeMapping
                    {
                        Name = "TakenDateSecondsShift",
                        // ColumnHeader = COL_NAME_PREFIX + FileListColumns. , 
                        TypeOfElement = typeof(int),
                        InAttributes = [],
                        OutAttributes = []
                    }
                },
                {
                    ElementAttribute.CreateDateDaysShift, new ElementAttributeMapping
                    {
                        Name = "CreateDateDaysShift",
                        // ColumnHeader = COL_NAME_PREFIX + FileListColumns. , 
                        TypeOfElement = typeof(int),
                        InAttributes = [],
                        OutAttributes = []
                    }
                },
                {
                    ElementAttribute.CreateDateHoursShift, new ElementAttributeMapping
                    {
                        Name = "CreateDateHoursShift",
                        // ColumnHeader = COL_NAME_PREFIX + FileListColumns. , 
                        TypeOfElement = typeof(int),
                        InAttributes = [],
                        OutAttributes = []
                    }
                },
                {
                    ElementAttribute.CreateDateMinutesShift, new ElementAttributeMapping
                    {
                        Name = "CreateDateMinutesShift",
                        // ColumnHeader = COL_NAME_PREFIX + FileListColumns. , 
                        TypeOfElement = typeof(int),
                        InAttributes = [],
                        OutAttributes = []
                    }
                },
                {
                    ElementAttribute.CreateDateSecondsShift, new ElementAttributeMapping
                    {
                        Name = "CreateDateSecondsShift",
                        // ColumnHeader = COL_NAME_PREFIX + FileListColumns. , 
                        TypeOfElement = typeof(int),
                        InAttributes = [],
                        OutAttributes = []
                    }
                },
                {
                    ElementAttribute.OffsetTime, new ElementAttributeMapping
                    {
                        Name = "OffsetTime",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.OFFSET_TIME,
                        TypeOfElement = typeof(string),
                        InAttributes =
                        [
                            "EXIF:OffsetTimeOriginal",
                            "EXIF:OffsetTime"
                        ],
                        OutAttributes =
                        [
                            "EXIF:OffsetTimeOriginal",
                            "EXIF:OffsetTime"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.OffsetTime
                    }
                },
                {
                    ElementAttribute.Folder, new ElementAttributeMapping
                    {
                        Name = "Folder",
                        // ColumnHeader = COL_NAME_PREFIX + FileListColumns. , 
                        TypeOfElement = typeof(string),
                        InAttributes = [],
                        OutAttributes = [],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.Folder
                    }
                },
                {
                    ElementAttribute.RemoveAllGPS, new ElementAttributeMapping
                    {
                        Name = "gps*",
                        // ColumnHeader = COL_NAME_PREFIX + FileListColumns. , 
                        TypeOfElement = typeof(string),
                        InAttributes = [],
                        OutAttributes = []
                    }
                },
                {
                    ElementAttribute.IPTCKeywords, new ElementAttributeMapping
                    {
                        Name = "IPTCKeywords",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.IPTC_KEYWORDS,
                        TypeOfElement = typeof(string),
                        InAttributes = ["IPTC:Keywords"],
                        OutAttributes = ["IPTC:Keywords"],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.IPTCKeywords
                    }
                },
                {
                    ElementAttribute.XMLSubjects, new ElementAttributeMapping
                    {
                        Name = "XMLSubjects",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.XML_SUBJECTS,
                        TypeOfElement = typeof(string),
                        InAttributes = ["XMP:Subject"],
                        OutAttributes = ["XMP:Subject"],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.XMLSubjects
                    }
                },
                {
                    ElementAttribute.GUID, new ElementAttributeMapping
                    {
                        Name = "GUID",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GUID,
                        TypeOfElement = typeof(string),
                        InAttributes = [],
                        OutAttributes = []
                    }
                },
                {
                    ElementAttribute.GPSDOP, new ElementAttributeMapping
                    {
                        Name = "GPSDOP",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPSDOP,
                        TypeOfElement = typeof(double),
                        InAttributes = ["EXIF:GPSDOP"],
                        OutAttributes =
                        [
                            "EXIF:GPSDOP",
                            "XMP:GPSDOP"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSDOP
                    }
                },
                {
                    ElementAttribute.GPSHPositioningError, new ElementAttributeMapping
                    {
                        Name = "GPSHPositioningError",
                        ColumnHeader = COL_NAME_PREFIX + FileListColumns.GPSHPOSITIONINGERROR,
                        TypeOfElement = typeof(string),
                        InAttributes = ["EXIF:GPSHPositioningError"],
                        OutAttributes =
                        [
                            "EXIF:GPSHPositioningError",
                            "XMP:GPSHPositioningError"
                        ],
                        OrderID = ORDER_INCREMENT_VALUE + (int)ElementAttribute.GPSHPositioningError
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
        if (TagsToAttributes.TryGetValue(key: attributeToFind,
                                         value: out ElementAttributeMapping mapping))
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
        if (TagsToAttributes.TryGetValue(key: attributeToFind,
                                         value: out ElementAttributeMapping mapping))
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
        return TagsToAttributes.TryGetValue(key: attributeToFind,
                value: out ElementAttributeMapping mapping)
            ? mapping.OrderID
            : -1;
    }

    /// <summary>
    ///     Retrieves the name of the specified attributeToFind attribute.
    /// </summary>
    /// <param name="attributeToFind">The attributeToFind attribute to get the name for.</param>
    /// <returns>
    ///     The name of the specified attributeToFind attribute. If the attributeToFind attribute does not exist, it returns a
    ///     null string equivalent.
    /// </returns>
    public static string GetElementAttributesName(ElementAttribute attributeToFind)
    {
        if (TagsToAttributes.TryGetValue(key: attributeToFind,
                                         value: out ElementAttributeMapping mapping))
        {
            if (!string.IsNullOrWhiteSpace(value: mapping.Name))
            {
                return mapping.Name;
            }
        }

        return FrmMainApp.NullStringEquivalentBlank;
    }

    /// <summary>
    ///     Retrieves the column header of the specified attributeToFind attribute.
    /// </summary>
    /// <param name="attributeToFind">The attributeToFind attribute to get the column header for.</param>
    /// <returns>
    ///     The column header of the specified attributeToFind attribute. If the attributeToFind attribute does not exist, it
    ///     returns a null string equivalent.
    /// </returns>
    public static string GetElementAttributesColumnHeader(
        ElementAttribute attributeToFind)
    {
        if (TagsToAttributes.TryGetValue(key: attributeToFind,
                                         value: out ElementAttributeMapping mapping))
        {
            if (!string.IsNullOrWhiteSpace(value: mapping.ColumnHeader))
            {
                return COL_NAME_PREFIX + mapping.Name;
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
    public static ElementAttribute GetElementAttributesElementAttribute(
        string attributeToFind)
    {
        foreach (KeyValuePair<ElementAttribute, ElementAttributeMapping> keyValuePair in
                 TagsToAttributes)
        {
            if (keyValuePair.Value.Name == attributeToFind)
            {
                return keyValuePair.Key;
            }
        }

        throw new ArgumentException(
            message: $"Element attribute '{attributeToFind}' does not exist.");
    }

    /// <summary>
    ///     Gets the type of the specified ElementAttribute.
    /// </summary>
    /// <param name="attributeToFind">The ElementAttribute to find the type of.</param>
    /// <returns>The type of the specified ElementAttribute.</returns>
    /// <exception cref="ArgumentException">Thrown when the specified ElementAttribute does not exist.</exception>
    public static Type GetElementAttributesType(ElementAttribute attributeToFind)
    {
        return TagsToAttributes.TryGetValue(key: attributeToFind,
                                         value: out ElementAttributeMapping mapping)
            ? mapping.TypeOfElement
            : throw new ArgumentException(
            message: $"Element attribute '{attributeToFind}' does not exist.");
    }

    /// <summary>
    ///     Determines whether the specified attributeToFind attribute is related to geographic data.
    /// </summary>
    /// <param name="attributeToFind">The attributeToFind attribute to check.</param>
    /// <returns>Returns true if the specified attributeToFind attribute is related to geographic data, otherwise false.</returns>
    public static bool GetElementAttributesIsGeoData(ElementAttribute attributeToFind)
    {
        return TagsToAttributes.TryGetValue(key: attributeToFind,
            value: out ElementAttributeMapping mapping) && mapping.isGeoData;
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
        public string ColumnHeader;
        public Type TypeOfElement;
        public List<string> InAttributes;
        public List<string> OutAttributes;
        public List<string> IPTCKeywords;
        public List<string> XMPSubjects;
        public int OrderID;
        public bool isGeoData;
    }
}