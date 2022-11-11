// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "adobe:ns:meta/")]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "adobe:ns:meta/", IsNullable = false)]
public partial class xmpmeta
{

    private RDF rDFField;

    private string xmptkField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#")]
    public RDF RDF
    {
        get
        {
            return this.rDFField;
        }
        set
        {
            this.rDFField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
    public string xmptk
    {
        get
        {
            return this.xmptkField;
        }
        set
        {
            this.xmptkField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#")]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#", IsNullable = false)]
public partial class RDF
{

    private RDFDescription descriptionField;

    /// <remarks/>
    public RDFDescription Description
    {
        get
        {
            return this.descriptionField;
        }
        set
        {
            this.descriptionField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#")]
public partial class RDFDescription
{

    private string gPSAltitudeField;

    private byte gPSAltitudeRefField;

    private string gPSLatitudeField;

    private string gPSLongitudeField;

    private string gPSDateStampField;

    private string gPSLatitudeRefField;

    private string gPSLongitudeRefField;

    private string gPSTimeStampField;

    private byte gPSTrackField;

    private decimal gPSSpeedField;

    private string gPSImgDirectionField;

    private string gPSTrackRefField;

    private string gPSSpeedRefField;

    private string gPSImgDirectionRefField;

    private string aboutField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSAltitude
    {
        get
        {
            return this.gPSAltitudeField;
        }
        set
        {
            this.gPSAltitudeField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public byte GPSAltitudeRef
    {
        get
        {
            return this.gPSAltitudeRefField;
        }
        set
        {
            this.gPSAltitudeRefField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSLatitude
    {
        get
        {
            return this.gPSLatitudeField;
        }
        set
        {
            this.gPSLatitudeField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSLongitude
    {
        get
        {
            return this.gPSLongitudeField;
        }
        set
        {
            this.gPSLongitudeField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSDateStamp
    {
        get
        {
            return this.gPSDateStampField;
        }
        set
        {
            this.gPSDateStampField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSLatitudeRef
    {
        get
        {
            return this.gPSLatitudeRefField;
        }
        set
        {
            this.gPSLatitudeRefField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSLongitudeRef
    {
        get
        {
            return this.gPSLongitudeRefField;
        }
        set
        {
            this.gPSLongitudeRefField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSTimeStamp
    {
        get
        {
            return this.gPSTimeStampField;
        }
        set
        {
            this.gPSTimeStampField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public byte GPSTrack
    {
        get
        {
            return this.gPSTrackField;
        }
        set
        {
            this.gPSTrackField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public decimal GPSSpeed
    {
        get
        {
            return this.gPSSpeedField;
        }
        set
        {
            this.gPSSpeedField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSImgDirection
    {
        get
        {
            return this.gPSImgDirectionField;
        }
        set
        {
            this.gPSImgDirectionField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSTrackRef
    {
        get
        {
            return this.gPSTrackRefField;
        }
        set
        {
            this.gPSTrackRefField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSSpeedRef
    {
        get
        {
            return this.gPSSpeedRefField;
        }
        set
        {
            this.gPSSpeedRefField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://ns.adobe.com/exif/1.0/")]
    public string GPSImgDirectionRef
    {
        get
        {
            return this.gPSImgDirectionRefField;
        }
        set
        {
            this.gPSImgDirectionRefField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
    public string about
    {
        get
        {
            return this.aboutField;
        }
        set
        {
            this.aboutField = value;
        }
    }
}

