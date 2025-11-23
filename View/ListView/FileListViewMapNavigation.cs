using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Forms;

namespace GeoTagNinja.View.ListView;

internal static class FileListViewMapNavigation
{
    /// <summary>
    ///     Extracted method for navigating to a set of coordinates on the map.
    /// </summary>
    /// <returns>Nothing in reality</returns>
    internal static void ListViewItemClickNavigate()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        HelperVariables.HTMLAddMarker = "";
        HelperVariables.HsMapMarkers.Clear();

        // reset the Favourites dropdown. this is needed bcs if there's an item in the dropdown and user clicks on an image in listview the coords change on the map
        // and it becomes impossible to select that fav's original coords again, which is illogical.
        frmMainAppInstance.cbx_Favourites.SelectedItem = null;

        
        foreach (ListViewItem lvw_FileListItem in frmMainAppInstance.lvw_FileList.SelectedItems)
        {
            DirectoryElement de = lvw_FileListItem.Tag as DirectoryElement;

            (double, double) deCoords = getCoordinateValues(directoryElement: de);
            if (deCoords != (0.0, 0.0))
            {
                frmMainAppInstance.nud_lat.Value = Convert.ToDecimal(value: deCoords.Item1, provider: CultureInfo.InvariantCulture);
                frmMainAppInstance.nud_lng.Value = Convert.ToDecimal(value: deCoords.Item2, provider: CultureInfo.InvariantCulture);
                _ = HelperVariables.HsMapMarkers.Add(item: (deCoords.Item1.ToString(provider: CultureInfo.InvariantCulture), deCoords.Item2.ToString(provider: CultureInfo.InvariantCulture)));
            }
        }

        return;

        [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
        static (double, double) getCoordinateValues(DirectoryElement directoryElement)
        {
            double dataInDELat = (double)directoryElement.GetAttributeValue<double>(
                attribute: SourcesAndAttributes.ElementAttribute.GPSLatitude,
                version: directoryElement.GetMaxAttributeVersion(attribute: SourcesAndAttributes.ElementAttribute.GPSLatitude),
                notFoundValue: 0.0);

            double dataInDELong = (double)directoryElement.GetAttributeValue<double>(
                attribute: SourcesAndAttributes.ElementAttribute.GPSLongitude,
                version: directoryElement.GetMaxAttributeVersion(attribute: SourcesAndAttributes.ElementAttribute.GPSLongitude),
                notFoundValue: 0.0);

            return (dataInDELat, dataInDELong);

            // leave as-is (most likely the last photo)
        }
    }
}