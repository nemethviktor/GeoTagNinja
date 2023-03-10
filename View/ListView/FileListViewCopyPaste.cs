﻿using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja;

internal static class FileListViewCopyPaste
{
    /// <summary>
    ///     This drives the logic for "copying" (as in copy-paste) the geodata from one file to others.
    /// </summary>
    internal static void ListViewCopyGeoData()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (frmMainAppInstance.lvw_FileList.SelectedItems.Count == 1)
        {
            ListViewItem lvi = frmMainAppInstance.lvw_FileList.SelectedItems[index: 0];

            // don't copy folders....
            if (File.Exists(path: Path.Combine(path1: FrmMainApp.FolderName, path2: lvi.Text)))
            {
                // The reason why we're using a CopyPoolDict here rather than just read straight out of the DE is because if the user changes folder
                // ... then the DE-data would be cleared and thus cross-folder-paste wouldn't be possible
                FrmMainApp.CopyPoolDict.Clear();

                List<ElementAttribute> listOfTagsToCopy = new()
                {
                    ElementAttribute.Coordinates,
                    ElementAttribute.GPSLatitude,
                    ElementAttribute.GPSLatitudeRef,
                    ElementAttribute.GPSLongitude,
                    ElementAttribute.GPSLongitudeRef,
                    ElementAttribute.GPSSpeed,
                    ElementAttribute.GPSSpeedRef,
                    ElementAttribute.GPSAltitude,
                    ElementAttribute.GPSAltitudeRef,
                    ElementAttribute.Country,
                    ElementAttribute.CountryCode,
                    ElementAttribute.State,
                    ElementAttribute.City,
                    ElementAttribute.Sub_location,
                    ElementAttribute.DestCoordinates,
                    ElementAttribute.GPSDestLatitude,
                    ElementAttribute.GPSDestLatitudeRef,
                    ElementAttribute.GPSDestLongitude,
                    ElementAttribute.GPSDestLongitudeRef,
                    ElementAttribute.GPSImgDirection,
                    ElementAttribute.GPSImgDirectionRef,
                    ElementAttribute.TakenDate,
                    ElementAttribute.CreateDate,
                    ElementAttribute.OffsetTime,
                    ElementAttribute.TakenDateSecondsShift,
                    ElementAttribute.TakenDateMinutesShift,
                    ElementAttribute.TakenDateHoursShift,
                    ElementAttribute.TakenDateDaysShift,
                    ElementAttribute.CreateDateSecondsShift,
                    ElementAttribute.CreateDateMinutesShift,
                    ElementAttribute.CreateDateHoursShift,
                    ElementAttribute.CreateDateDaysShift
                };

                string fileNameWithoutPath = lvi.Text;
                DirectoryElement dirElemFileToCopyFrom = FrmMainApp.DirectoryElements.FindElementByItemName(FileNameWithPath: Path.Combine(path1: FrmMainApp.FolderName, path2: fileNameWithoutPath));
                foreach (ElementAttribute attribute in listOfTagsToCopy)
                {
                    // this would sit in Stage3ReadyToWrite if exists
                    if (dirElemFileToCopyFrom.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite))
                    {
                        FrmMainApp.CopyPoolDict.Add(key: attribute,
                                                    value: dirElemFileToCopyFrom.GetAttributeValueString(
                                                        attribute: attribute,
                                                        version: DirectoryElement.AttributeVersion
                                                            .Stage3ReadyToWrite));
                    }
                    else if (dirElemFileToCopyFrom.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Original))
                    {
                        FrmMainApp.CopyPoolDict.Add(key: attribute,
                                                    value: dirElemFileToCopyFrom.GetAttributeValueString(
                                                        attribute: attribute,
                                                        version: DirectoryElement.AttributeVersion.Original));
                    }
                }
            }
        }
        else
        {
            MessageBox.Show(text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningTooManyFilesSelected"),
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    ///     This drives the logic for "pasting" (as in copy-paste) the geodata from one file to others.
    ///     See further comments inside
    /// </summary>
    internal static void ListViewPasteGeoData()
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        // check there's anything in copy-pool
        if (FrmMainApp.CopyPoolDict.Count > 0 && frmMainAppInstance != null)
        {
            FrmPasteWhat frmPasteWhat = new(initiator: frmMainAppInstance.Name);
            frmPasteWhat.ShowDialog();
        }
        else
        {
            MessageBox.Show(text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNothingToPaste"),
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }
    }
}