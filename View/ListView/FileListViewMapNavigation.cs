using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using GeoTagNinja.View.ListView;

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

        foreach (ListViewItem lvw_FileListItem in frmMainAppInstance.lvw_FileList.SelectedItems)
        {
            DirectoryElement de = lvw_FileListItem.Tag as DirectoryElement;

            // make sure file still exists. just in case someone deleted it elsewhere
            if (File.Exists(path: de.FileNameWithPath) && lvw_FileListItem.SubItems.Count > 1)
            {
                int coordCol = frmMainAppInstance.lvw_FileList.GetColumnIndex(column: FileListView.FileListColumns.COORDINATES);
                string firstSelectedItem = lvw_FileListItem.SubItems[index: coordCol]
                    .Text;
                if (firstSelectedItem.Replace(oldValue: FileListView.UNKNOWN_VALUE_FILE, newValue: "") != "")
                {
                    string strLat;
                    string strLng;
                    try
                    {
                        strLat = firstSelectedItem.Split(';')[0]
                            .Replace(oldChar: ',', newChar: '.');
                        strLng = firstSelectedItem.Split(';')[1]
                            .Replace(oldChar: ',', newChar: '.');
                    }
                    catch
                    {
                        strLat = "fail";
                        strLng = "fail";
                    }

                    double parsedLat;
                    double parsedLng;
                    if (double.TryParse(s: strLat, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLat) && double.TryParse(s: strLng, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out parsedLng))
                    {
                        frmMainAppInstance.nud_lat.Text = strLat;
                        frmMainAppInstance.nud_lng.Text = strLng;

                        frmMainAppInstance.nud_lat.Value = Convert.ToDecimal(value: strLat, provider: CultureInfo.InvariantCulture);
                        frmMainAppInstance.nud_lng.Value = Convert.ToDecimal(value: strLng, provider: CultureInfo.InvariantCulture);
                        HelperVariables.HsMapMarkers.Add(item: (strLat, strLng));
                    }
                }
                else if (HelperVariables.SResetMapToZero)
                {
                    frmMainAppInstance.nud_lat.Text = "0";
                    frmMainAppInstance.nud_lng.Text = "0";

                    frmMainAppInstance.nud_lat.Value = 0;
                    frmMainAppInstance.nud_lng.Value = 0;
                    HelperVariables.HsMapMarkers.Add(item: ("0", "0"));
                }
                // leave as-is (most likely the last photo)
            }
        }
    }
}