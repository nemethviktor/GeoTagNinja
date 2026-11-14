namespace GeoTagNinja.Model;

public class MapWebMessage
{
#pragma warning disable IDE1006 // Naming Styles
    // These properties mirror the JSON sent from map.html. Most fields are nullable because
    // different map events send different payloads (coordinates, layer changes, or viewport state).
    public double? lat { get; set; } // note to self: don't allow ReSharper to rename these.
    public double? lng { get; set; } // note to self: don't allow ReSharper to rename these.
    public string layer { get; set; } // this is layer.name
    public bool? isDragged { get; set; }
    public int? zoomLevel { get; set; }
    public double? minLat { get; set; }
    public double? minLng { get; set; }
    public double? maxLat { get; set; }
    public double? maxLng { get; set; }
    public double? centerLat { get; set; }
    public double? centerLng { get; set; }
#pragma warning restore IDE1006 // Naming Styles
}