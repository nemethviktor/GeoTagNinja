namespace GeoTagNinja.Model
{
    public class GeoSetterFavourite
    {
        public string name { get; set; }
        public double lat { get; set; }
        public double lng { get; set; }
        public int radius { get; set; }
        public int autoassign { get; set; }
        public int snap { get; set; }
        public int alt { get; set; }
        public string tz { get; set; }
        public string ctrycode { get; set; }
        public string ctry { get; set; }
        public string state { get; set; }
        public string city { get; set; }
        public string subloc { get; set; }
    }
}
