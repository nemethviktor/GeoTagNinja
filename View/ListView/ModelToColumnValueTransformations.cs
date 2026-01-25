using GeoTagNinja.Model;
using static GeoTagNinja.View.ListView.FileListView;

namespace GeoTagNinja.View.ListView;

internal class ModelToColumnValueTransformations
{
    /// <summary>
    ///     Combine the values of lat & long into one column
    /// </summary>
    public static string M2C_CoordinatesInclDest(string column,
                                                 DirectoryElement item,
                                                 string notFoundValue)
    {
        // Pick the right attribs depending on which column
        SourcesAndAttributes.ElementAttribute latAttrib = SourcesAndAttributes.ElementAttribute.GPSLatitude;
        SourcesAndAttributes.ElementAttribute longAttrib = SourcesAndAttributes.ElementAttribute.GPSLongitude;
        if (column == FileListColumns.DEST_COORDINATES)
        {
            latAttrib = SourcesAndAttributes.ElementAttribute.GPSDestLatitude;
            longAttrib = SourcesAndAttributes.ElementAttribute.GPSDestLongitude;
        }

        string latValue = item.GetAttributeValueAsString(
            attribute: latAttrib,
            notFoundValue: notFoundValue,
            nowSavingExif: false);
        string longValue = item.GetAttributeValueAsString(
            attribute: longAttrib,
            notFoundValue: notFoundValue,
            nowSavingExif: false);

        return $"{latValue};{longValue}";
    }
}