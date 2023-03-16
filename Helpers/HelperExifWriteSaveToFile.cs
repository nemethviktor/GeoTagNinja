using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers;

internal static class HelperExifWriteSaveToFile
{
    /// <summary>
    ///     Writes outstanding exif changes to files.
    /// </summary>
    /// <returns>Reastically nothing but writes the exif tags and updates the listview rows where necessary</returns>
    internal static async Task ExifWriteExifToFile()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        HelperGenericFileLocking.FilesAreBeingSaved = true;
        string argsFile = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "exifArgsToWrite.args");
        string exiftoolCmd = " -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8" + " -@ " + HelperVariables.SDoubleQuote + argsFile + HelperVariables.SDoubleQuote;

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        // if user switches folder in the process of writing this will keep it standard
        Debug.Assert(condition: frmMainAppInstance != null, message: nameof(frmMainAppInstance) + " != null");

        File.Delete(path: argsFile);

        bool processOriginalFile = false;
        bool resetFileDateToCreated = false;
        bool writeXMPSideCar = false;
        bool doNotCreateBackup = false;

        bool failWriteNothingEnabled = false;
        bool queueWasEmpty = true;

        // get tag names
        DataTable dtObjectattributesOut = HelperDataOtherDataRelated.JoinDataTables(t1: HelperVariables.DtObjectNames, t2: HelperVariables.DtObjectattributesOut,
                                                                                    (row1,
                                                                                     row2) =>
                                                                                        row1.Field<string>(columnName: "objectName") == row2.Field<string>(columnName: "objectName"));

        HashSet<string> DistinctGUIDs = new();
        foreach (DirectoryElement directoryElement in FrmMainApp.DirectoryElements)
        {
            foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
            {
                if (directoryElement.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite))
                {
                    DistinctGUIDs.Add(item: directoryElement.UniqueID.ToString());
                    break;
                }
            }
        }

        // check there's anything to write.
        foreach (string GUID in DistinctGUIDs)
        {
            DirectoryElement dirElemFileToModify = FrmMainApp.DirectoryElements.FindElementByItemUniqueID(UniqueID: GUID);
            FrmMainApp.Logger.Trace(message: dirElemFileToModify.FileNameWithPath);

            if (dirElemFileToModify != null)
            {
                string fileNameWithPath = dirElemFileToModify.FileNameWithPath;
                string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;
                string folderNameToWrite = Path.GetDirectoryName(path: dirElemFileToModify.FileNameWithPath);
                if (File.Exists(path: fileNameWithPath))
                {
                    string exifArgsForOriginalFile = "";
                    string exifArgsForSidecar = "";
                    string fileExtension = Path.GetExtension(path: fileNameWithoutPath)
                        .Substring(startIndex: 1);

                    // this is an issue in Adobe Bridge (unsure). Rating in the XMP needs to be parsed and re-saved.
                    int ratingInXmp = -1;
                    string xmpFileLocation = Path.Combine(path1: folderNameToWrite, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath)) + ".xmp");
                    if (File.Exists(path: xmpFileLocation))
                    {
                        foreach (string line in File.ReadLines(path: xmpFileLocation))
                        {
                            if (line.Contains(value: "xmp:Rating="))
                            {
                                bool _ = int.TryParse(s: line.Replace(oldValue: "xmp:Rating=", newValue: "")
                                                          .Replace(oldValue: "\"", newValue: ""), result: out ratingInXmp);
                                break;
                            }

                            if (line.Contains(value: "<xmp:Rating>"))
                            {
                                bool _ = int.TryParse(s: line.Replace(oldValue: "<xmp:Rating>", newValue: "")
                                                          .Replace(oldValue: "</xmp:Rating>", newValue: "")
                                                          .Replace(oldValue: " ", newValue: ""), result: out ratingInXmp);
                                break;
                            }
                        }
                    }

                    queueWasEmpty = false;

                    processOriginalFile = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_ProcessOriginalFile"));
                    resetFileDateToCreated = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_ResetFileDateToCreated"));
                    writeXMPSideCar = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_AddXMPSideCar"));
                    doNotCreateBackup = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_OverwriteOriginal"));

                    // it's a lot less complicated to just pretend we want both the Original File and the Sidecar updated and then not-include them later than to have a Yggdrasil of IFs scattered all over.
                    // ... which latter I would inevitable f...k up at some point.

                    exifArgsForOriginalFile += Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath) + Environment.NewLine; //needs to include folder name
                    exifArgsForOriginalFile += "-ignoreMinorErrors" + Environment.NewLine;
                    exifArgsForOriginalFile += "-progress" + Environment.NewLine;

                    // this doesn't need to be sent back to the actual XMP file, it's a bug.
                    if (ratingInXmp >= 0)
                    {
                        exifArgsForOriginalFile += "-Rating=" + ratingInXmp + Environment.NewLine;
                    }

                    exifArgsForSidecar += Path.Combine(path1: folderNameToWrite, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath)) + ".xmp") + Environment.NewLine; //needs to include folder name
                    exifArgsForSidecar += "-progress" + Environment.NewLine;

                    // sidecar copying needs to be in a separate batch, as technically it's a different file

                    if (writeXMPSideCar)
                    {
                        FrmMainApp.Logger.Trace(message: fileNameWithPath + " - writeXMPSideCar - " + writeXMPSideCar);

                        if (!File.Exists(path: xmpFileLocation))
                        {
                            FrmMainApp.Logger.Trace(message: fileNameWithPath + " - writeXMPSideCar - " + writeXMPSideCar + " - File has been created.");

                            // otherwise create a new one. 
                            xmpFileLocation = Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath);
                            exifArgsForSidecar += "-tagsfromfile=" + xmpFileLocation + Environment.NewLine;
                        }
                    }

                    exifArgsForSidecar += "-ignoreMinorErrors" + Environment.NewLine;

                    DataTable dtFileWriteQueue = new();
                    dtFileWriteQueue.Clear();
                    dtFileWriteQueue.Columns.Add(columnName: "ItemNameWithoutPath");
                    dtFileWriteQueue.Columns.Add(columnName: "settingId");
                    dtFileWriteQueue.Columns.Add(columnName: "settingValue");

                    foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                    {
                        if (dirElemFileToModify.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite))
                        {
                            DataRow drFileDataRow = dtFileWriteQueue.NewRow();
                            drFileDataRow[columnName: "ItemNameWithoutPath"] = dirElemFileToModify.ItemNameWithoutPath;
                            drFileDataRow[columnName: "settingId"] = GetAttributeName(attribute: attribute);
                            if (!dirElemFileToModify.IsMarkedForDeletion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite))
                            {
                                drFileDataRow[columnName: "settingValue"] = dirElemFileToModify.GetAttributeValueString(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                            }
                            else
                            {
                                drFileDataRow[columnName: "settingValue"] = "";
                            }

                            dtFileWriteQueue.Rows.Add(row: drFileDataRow);
                        }
                    }

                    if (dtFileWriteQueue.Rows.Count > 0)
                    {
                        // get tags for this file

                        DataTable dtObjectattributesOutWithData = HelperDataOtherDataRelated.JoinDataTables(t1: dtObjectattributesOut, t2: dtFileWriteQueue,
                                                                                                            (row1,
                                                                                                             row2) =>
                                                                                                                row1.Field<string>(columnName: "objectName") == row2.Field<string>(columnName: "settingId"));

                        string exifToolAttribute;
                        string updateExifVal;

                        bool deleteAllGPSData = dtFileWriteQueue.AsEnumerable()
                            .Any(predicate: row => "gps*" == row.Field<string>(columnName: "settingId"));

                        bool deleteTagAlreadyAdded = false;

                        // add tags to argsFile
                        foreach (DataRow dataRow in dtObjectattributesOutWithData.Rows)
                        {
                            string settingId = dataRow[columnName: "settingId"]
                                .ToString();
                            string settingValue = dataRow[columnName: "settingValue"]
                                .ToString();

                            FrmMainApp.Logger.Trace(message: fileNameWithPath + " - " + settingId + ": " + settingValue);

                            // non-xmp always
                            if (deleteAllGPSData && !deleteTagAlreadyAdded)
                            {
                                exifArgsForOriginalFile += "-gps*=" + Environment.NewLine;
                                exifArgsForSidecar += "-gps*=" + Environment.NewLine;

                                // this is moved up/in here because the deletion of all gps has to come before just about anything else in case user wants to add (rather than delete) in more tags (later).

                                exifArgsForOriginalFile += "-xmp:gps*=" + Environment.NewLine;
                                exifArgsForSidecar += "-xmp:gps*=" + Environment.NewLine;

                                deleteTagAlreadyAdded = true;
                            }

                            string objectTagNameOut = dataRow[columnName: "objectTagName_Out"]
                                .ToString();
                            exifToolAttribute = dataRow[columnName: "objectTagName_Out"]
                                .ToString();
                            updateExifVal = dataRow[columnName: "settingValue"]
                                .ToString();

                            if (!objectTagNameOut.Contains(value: ":"))
                            {
                                if (updateExifVal != "")
                                {
                                    exifArgsForOriginalFile += "-" + exifToolAttribute + "=" + updateExifVal + Environment.NewLine;
                                    exifArgsForSidecar += "-" + exifToolAttribute + "=" + updateExifVal + Environment.NewLine;

                                    //if lat/long then add Ref. 
                                    if (exifToolAttribute == "GPSLatitude" ||
                                        exifToolAttribute == "GPSLatitude" ||
                                        exifToolAttribute == "exif:GPSLatitude" ||
                                        exifToolAttribute == "exif:GPSLatitude")
                                    {
                                        if (updateExifVal.Substring(startIndex: 0, length: 1) == FrmMainApp.NullStringEquivalentGeneric)
                                        {
                                            exifArgsForOriginalFile += "-" + exifToolAttribute + "Ref" + "=" + "South" + Environment.NewLine;
                                            exifArgsForSidecar += "-" + exifToolAttribute + "Ref" + "=" + "South" + Environment.NewLine;
                                        }
                                        else
                                        {
                                            exifArgsForOriginalFile += "-" + exifToolAttribute + "Ref" + "=" + "North" + Environment.NewLine;
                                            exifArgsForSidecar += "-" + exifToolAttribute + "Ref" + "=" + "North" + Environment.NewLine;
                                        }
                                    }
                                    else if (exifToolAttribute == "GPSLongitude" ||
                                             exifToolAttribute == "GPSLongitude" ||
                                             exifToolAttribute == "exif:GPSLongitude" ||
                                             exifToolAttribute == "exif:GPSLongitude")
                                    {
                                        if (updateExifVal.Substring(startIndex: 0, length: 1) == FrmMainApp.NullStringEquivalentGeneric)
                                        {
                                            exifArgsForOriginalFile += "-" + exifToolAttribute + "Ref" + "=" + "West" + Environment.NewLine;
                                            exifArgsForSidecar += "-" + exifToolAttribute + "Ref" + "=" + "West" + Environment.NewLine;
                                        }
                                        else
                                        {
                                            exifArgsForOriginalFile += "-" + exifToolAttribute + "Ref" + "=" + "East" + Environment.NewLine;
                                            exifArgsForSidecar += "-" + exifToolAttribute + "Ref" + "=" + "East" + Environment.NewLine;
                                        }
                                    }
                                }
                                else //delete tag
                                {
                                    exifArgsForOriginalFile += "-" + exifToolAttribute + "=" + Environment.NewLine;
                                    exifArgsForSidecar += "-" + exifToolAttribute + "=" + Environment.NewLine;

                                    //if lat/long then add Ref. 
                                    if (
                                        exifToolAttribute == "GPSLatitude" ||
                                        exifToolAttribute == "GPSLatitude" ||
                                        exifToolAttribute == "exif:GPSLatitude" ||
                                        exifToolAttribute == "exif:GPSLatitude" ||
                                        exifToolAttribute == "GPSLongitude" ||
                                        exifToolAttribute == "GPSLongitude" ||
                                        exifToolAttribute == "exif:GPSLongitude" ||
                                        exifToolAttribute == "exif:GPSLongitude"
                                    )
                                    {
                                        exifArgsForOriginalFile += "-" + exifToolAttribute + "Ref" + "=" + Environment.NewLine;
                                        exifArgsForSidecar += "-" + exifToolAttribute + "Ref" + "=" + Environment.NewLine;
                                    }
                                }
                            }
                            else
                            {
                                if (objectTagNameOut == "EXIF:DateTimeOriginal" || // TakenDate
                                    objectTagNameOut == "EXIF:CreateDate" || // CreateDate
                                    objectTagNameOut == "XMP:DateTimeOriginal" || // TakenDate
                                    objectTagNameOut == "XMP:CreateDate" // CreateDate
                                   )
                                {
                                    bool isTakenDate = false;
                                    bool isCreateDate = false;
                                    if (objectTagNameOut == "EXIF:DateTimeOriginal" || objectTagNameOut == "XMP:DateTimeOriginal")
                                    {
                                        isTakenDate = true;
                                    }
                                    else if (objectTagNameOut == "EXIF:CreateDate" || objectTagNameOut == "XMP:CreateDate")
                                    {
                                        isCreateDate = true;
                                    }

                                    try
                                    {
                                        updateExifVal = DateTime.Parse(s: settingValue)
                                            .ToString(format: "yyyy-MM-dd HH:mm:ss");
                                    }
                                    catch
                                    {
                                        updateExifVal = "";
                                    }

                                    if (isCreateDate)
                                    {
                                        // update FrmMainApp.OriginalCreateDateDict -- there should be only 1 row
                                        if (FrmMainApp.OriginalCreateDateDict.ContainsKey(key: fileNameWithoutPath))
                                        {
                                            FrmMainApp.OriginalCreateDateDict.Remove(key: fileNameWithoutPath);
                                        }

                                        if (updateExifVal != "")
                                        {
                                            FrmMainApp.OriginalCreateDateDict[key: fileNameWithoutPath] = updateExifVal;
                                        }
                                    }
                                    else if (isTakenDate)
                                    {
                                        // update FrmMainApp.OriginalTakenDateDict -- there should be only 1 row
                                        if (FrmMainApp.OriginalTakenDateDict.ContainsKey(key: fileNameWithoutPath))
                                        {
                                            FrmMainApp.OriginalTakenDateDict.Remove(key: fileNameWithoutPath);
                                        }

                                        if (updateExifVal != "")
                                        {
                                            FrmMainApp.OriginalTakenDateDict[key: fileNameWithoutPath] = updateExifVal;
                                        }
                                    }
                                }

                                FrmMainApp.Logger.Trace(message: fileNameWithPath + " - " + exifToolAttribute + ": " + updateExifVal);

                                exifArgsForOriginalFile += "-" + exifToolAttribute + "=" + updateExifVal + Environment.NewLine;
                                exifArgsForSidecar += "-" + exifToolAttribute + "=" + updateExifVal + Environment.NewLine;
                            }
                        }
                    }

                    if (doNotCreateBackup)
                    {
                        exifArgsForOriginalFile += "-overwrite_original_in_place" + Environment.NewLine;
                    }

                    exifArgsForOriginalFile += "-iptc:codedcharacterset=utf8" + Environment.NewLine;

                    if (resetFileDateToCreated)
                    {
                        exifArgsForOriginalFile += "-filemodifydate<datetimeoriginal" + Environment.NewLine;
                        exifArgsForOriginalFile += "-filecreatedate<datetimeoriginal" + Environment.NewLine;
                    }

                    exifArgsForOriginalFile += "-IPTCDigest=" + Environment.NewLine;

                    exifArgsForOriginalFile += "-execute" + Environment.NewLine;

                    if (processOriginalFile)
                    {
                        File.AppendAllText(path: argsFile, contents: exifArgsForOriginalFile, encoding: Encoding.UTF8);
                    }

                    if (writeXMPSideCar)
                    {
                        if (doNotCreateBackup)
                        {
                            //exifArgsForSidecar += "-IPTCDigest=" + Environment.NewLine;
                            exifArgsForSidecar += "-overwrite_original_in_place" + Environment.NewLine;
                        }

                        exifArgsForSidecar += "-execute" + Environment.NewLine;
                        File.AppendAllText(path: argsFile, contents: exifArgsForSidecar, encoding: Encoding.UTF8);
                    }

                    if (!processOriginalFile && !writeXMPSideCar)
                    {
                        failWriteNothingEnabled = true;
                        FrmMainApp.Logger.Info(message: "Both file-writes disabled. Nothing Written.");
                        MessageBox.Show(
                            text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNoWriteSettingEnabled"),
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
                    }
                }
            }
        }

        //FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();

        if (!failWriteNothingEnabled && !queueWasEmpty)
        {
            FrmMainApp.Logger.Info(message: "Starting ExifTool.");
            ///////////////

            ;
            await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                                         frmMainAppInstance: frmMainAppInstance,
                                                         initiator: "ExifWriteExifToFile");
        }
        else if (!queueWasEmpty)
        {
            FrmMainApp.Logger.Info(message: "Both file-writes disabled. Nothing Written.");
            MessageBox.Show(text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNoWriteSettingEnabled"),
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }
        else
        {
            FrmMainApp.Logger.Info(message: "Queue was empty. Nothing Written.");
            MessageBox.Show(text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningNothingInWriteQueue"),
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }

        ///////////////
        FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Ready.");
        HelperGenericFileLocking.FilesAreBeingSaved = false;
    }
}