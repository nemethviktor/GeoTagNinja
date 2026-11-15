using System.Collections.Generic;

namespace GeoTagNinja.Helpers.Map;

/// <summary>
///     The tile layers offered by the embedded Leaflet map.
/// </summary>
/// <remarks>
///     The keys must match the layer names declared in <c>Resources/map.html</c>; the values are what the
///     user sees in the layer menu.
/// </remarks>
internal static class MapLayers
{
    /// <summary>
    ///     Returns the KVP combination of map layer options
    /// </summary>
    /// <returns></returns>
    internal static Dictionary<string, string> GetMapLayers()
    {
        Dictionary<string, string> result = new()
        {
            { "lyr_streets_osm", "Streets (OSM)" },
            { "lyr_streets_esri", "Streets (ESRI)" },
            { "lyr_streets_carto", "Streets (Carto)" },
            { "lyr_satellite_esri", "Satellite (ESRI)" }
        };

        return result;
    }
}
