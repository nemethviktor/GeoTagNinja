namespace GeoTagNinja.Model;

public class MapWebMessage
{
#pragma warning disable IDE1006 // Naming Styles
    public double lat { get; set; } // note to self: don't allow ReSharper to rename these.

    public double lng { get; set; } // note to self: don't allow ReSharper to rename these.
    public string layer { get; set; } // this is layer.name
    public bool isDragged { get; set; }
#pragma warning restore IDE1006 // Naming Styles
}