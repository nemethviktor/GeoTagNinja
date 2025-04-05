namespace GeoTagNinja.Model
{
    public class Favourite
    {
        public string FavouriteName { get; set; }
        public string GPSLatitude { get; set; }
        public string GPSLatitudeRef { get; set; }
        public string GPSLongitude { get; set; }
        public string GPSLongitudeRef { get; set; }
        public string GPSAltitude { get; set; }
        public string GPSAltitudeRef { get; set; }
        public string Coordinates { get; set; }
        public string City { get; set; }
        public string CountryCode { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string Sublocation { get; set; }
    }
}
