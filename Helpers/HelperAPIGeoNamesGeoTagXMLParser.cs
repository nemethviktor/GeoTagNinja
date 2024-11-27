// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.

using System;
using System.ComponentModel;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace GeoTagNinja.Helpers;
// ReSharper disable InconsistentNaming

/// <remarks />
[Serializable, DesignerCategory(category: "code"), XmlType(AnonymousType = true, Namespace = "adobe:ns:meta/"), XmlRoot(Namespace = "adobe:ns:meta/", IsNullable = false)]
public class xmpmeta
{
    private RDF rDFField;

    private string xmptkField;

    /// <remarks />
    [XmlElement(Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#")]
    public RDF RDF
    {
        get => rDFField;
        set => rDFField = value;
    }

    /// <remarks />
    [XmlAttribute(Form = XmlSchemaForm.Qualified)]
    public string xmptk
    {
        get => xmptkField;
        set => xmptkField = value;
    }
}

/// <remarks />
[Serializable, DesignerCategory(category: "code"), XmlType(AnonymousType = true, Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#"), XmlRoot(Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#", IsNullable = false)]
public class RDF
{
    private RDFDescription descriptionField;

    /// <remarks />
    public RDFDescription Description
    {
        get => descriptionField;
        set => descriptionField = value;
    }
}

/// <remarks />
[Serializable, DesignerCategory(category: "code"), XmlType(AnonymousType = true, Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#")]
public class RDFDescription
{
    private string aboutField;

    private string gPSAltitudeField;

    private byte gPSAltitudeRefField;

    private string gPSDateStampField;

    private string gPSImgDirectionField;

    private string gPSImgDirectionRefField;

    private string gPSLatitudeField;

    private string gPSLatitudeRefField;

    private string gPSLongitudeField;

    private string gPSLongitudeRefField;

    private decimal gPSSpeedField;

    private string gPSSpeedRefField;

    private string gPSTimeStampField;

    private byte gPSTrackField;

    private string gPSTrackRefField;

    private string gPSDOPField;

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSAltitude
    {
        get => gPSAltitudeField;
        set => gPSAltitudeField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public byte GPSAltitudeRef
    {
        get => gPSAltitudeRefField;
        set => gPSAltitudeRefField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSLatitude
    {
        get => gPSLatitudeField;
        set => gPSLatitudeField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSLongitude
    {
        get => gPSLongitudeField;
        set => gPSLongitudeField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSDateStamp
    {
        get => gPSDateStampField;
        set => gPSDateStampField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSLatitudeRef
    {
        get => gPSLatitudeRefField;
        set => gPSLatitudeRefField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSLongitudeRef
    {
        get => gPSLongitudeRefField;
        set => gPSLongitudeRefField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSTimeStamp
    {
        get => gPSTimeStampField;
        set => gPSTimeStampField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public byte GPSTrack
    {
        get => gPSTrackField;
        set => gPSTrackField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public decimal GPSSpeed
    {
        get => gPSSpeedField;
        set => gPSSpeedField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSDOP
    {
        get => gPSDOPField;
        set => gPSDOPField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSImgDirection
    {
        get => gPSImgDirectionField;
        set => gPSImgDirectionField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSTrackRef
    {
        get => gPSTrackRefField;
        set => gPSTrackRefField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSSpeedRef
    {
        get => gPSSpeedRefField;
        set => gPSSpeedRefField = value;
    }

    /// <remarks />
    [XmlElement(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSImgDirectionRef
    {
        get => gPSImgDirectionRefField;
        set => gPSImgDirectionRefField = value;
    }

    /// <remarks />
    [XmlAttribute(Form = XmlSchemaForm.Qualified)]
    public string about
    {
        get => aboutField;
        set => aboutField = value;
    }
}