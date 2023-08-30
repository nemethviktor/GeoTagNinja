using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using GeoTagNinja.Model;

namespace GeoTagNinja;

internal static class FileListViewMapNavigation
{
    /// <summary>
    /// Iterates over all selected items in the FileListView and adds them
    /// to the list of markers to show.
    /// 
    /// If HelperVariables.SResetMapToZero is set, clears the marker list and
    /// adds 0|0 to it.
    /// 
    /// It's tested whether the file still exists - if not, do not add it.
    /// </summary>
    internal static void ListViewItemClickNavigate()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        HelperVariables.HtmlAddMarker = "";
        HelperVariables.HsMapMarkers.Clear();

        // ReSharper disable once InconsistentNaming
        foreach (ListViewItem lvw_FileListItem in frmMainAppInstance.lvw_FileList.SelectedItems)
        {
            DirectoryElement de = lvw_FileListItem.Tag as DirectoryElement;

            (double, double) deCoords = getCoordinateValues(directoryElement: de);
            if (deCoords != (0.0, 0.0))
            {
                frmMainAppInstance.nud_lat.Value = Convert.ToDecimal(value: deCoords.Item1, provider: CultureInfo.InvariantCulture);
                frmMainAppInstance.nud_lng.Value = Convert.ToDecimal(value: deCoords.Item2, provider: CultureInfo.InvariantCulture);
                HelperVariables.HsMapMarkers.Add(item: (deCoords.Item1.ToString(provider: CultureInfo.InvariantCulture), deCoords.Item2.ToString(provider: CultureInfo.InvariantCulture)));
            }
        }

        [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
        (double, double) getCoordinateValues(DirectoryElement directoryElement)
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