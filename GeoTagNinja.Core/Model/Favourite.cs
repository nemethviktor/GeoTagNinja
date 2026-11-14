using System.Collections.Generic;

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

        /// <summary>
        ///     How far (in metres) from the favourite's position a photo may be and still count as "at" the favourite.
        ///     Zero means the favourite has no area, so nothing is ever matched to it.
        /// </summary>
        public int Radius { get; set; }

        /// <summary>
        ///     Photos that fall inside <see cref="Radius" /> get this favourite's location details staged automatically
        ///     when they are read.
        /// </summary>
        public bool AutoAssign { get; set; }

        /// <summary>
        ///     When a photo is auto-assigned, also move its coordinates onto the favourite's.
        /// </summary>
        public bool Snap { get; set; }

        /// <summary>
        ///     IANA time zone id (for example <c>Europe/Budapest</c>), or blank. Stored with the favourite only.
        /// </summary>
        public string TimeZoneId { get; set; }

        /// <summary>
        ///     Keywords belonging to the favourite. Stored with the favourite only.
        /// </summary>
        public List<string> Keywords { get; set; } = [];

        /// <summary>
        ///     A deep copy, so the manage dialog can edit a working copy and leave the real list alone until OK.
        /// </summary>
        public Favourite Clone()
        {
            Favourite copy = (Favourite)MemberwiseClone();
            copy.Keywords = [.. Keywords ?? []];
            return copy;
        }
    }
}
