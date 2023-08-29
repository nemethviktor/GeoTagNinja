using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using Microsoft.WindowsAPICodePack.Taskbar;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers;

internal static class HelperExifWriteSaveToFile
{
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

        // Get items that need saving...
        HashSet<string> DistinctGUIDs = FrmMainApp.DirectoryElements.FindDirtyElements();

        FrmMainApp.TaskbarManagerInstance.SetProgressState(state: TaskbarProgressBarState.Indeterminate);
        string exifArgsForOriginalFile = "";
        string exifArgsForSidecar = "";
        // check there's anything to write.
        foreach (string GUID in DistinctGUIDs)
        {
            DirectoryElement dirElemFileToModify = FrmMainApp.DirectoryElements.FindElementByItemGUID(GUID: GUID);
            FrmMainApp.Logger.Trace(message: dirElemFileToModify.FileNameWithPath);

            if (dirElemFileToModify != null)
            {
                string fileNameWithPath = dirElemFileToModify.FileNameWithPath;
                string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;
                string folderNameToWrite = Path.GetDirectoryName(path: dirElemFileToModify.FileNameWithPath);
                if (File.Exists(path: fileNameWithPath))
                {
                    exifArgsForOriginalFile = "";
                    exifArgsForSidecar = "";
                    string fileExtension = Path.GetExtension(path: fileNameWithoutPath)
                                               .Substring(startIndex: 1);

                    // check that either/or the orig file or the xmp needs overwriting
                    processOriginalFile = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_ProcessOriginalFile"));
                    writeXMPSideCar = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_AddXMPSideCar"));

                    // this is an issue in Adobe Bridge (unsure). Rating in the XMP needs to be parsed and re-saved.
                    int ratingInXmp = -1;
                    string xmpFileLocation = Path.Combine(path1: folderNameToWrite, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath)) + ".xmp");
                    if (File.Exists(path: xmpFileLocation) && writeXMPSideCar) // don't bother if we don't need to (over)write the xmp
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

                    resetFileDateToCreated = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_ResetFileDateToCreated"));
                    doNotCreateBackup = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_FileOptions", settingId: fileExtension.ToLower() + "_" + "ckb_OverwriteOriginal"));

                    // it's a lot less complicated to just pretend we want both the Original File and the Sidecar updated and then not-include them later than to have a Yggdrasil of IFs scattered all over.
                    // ... which latter I would inevitable f...k up at some point.

                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath)); //needs to include folder name
                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-ignoreMinorErrors");
                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-progress");

                    // this doesn't need to be sent back to the actual XMP file, it's a bug.
                    if (ratingInXmp >= 0)
                    {
                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-Rating=" + ratingInXmp);
                    }

                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText: Path.Combine(path1: folderNameToWrite, path2: Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath)) + ".xmp")); //needs to include folder name
                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText: "-progress");

                    // sidecar copying needs to be in a separate batch, as technically it's a different file

                    if (writeXMPSideCar)
                    {
                        FrmMainApp.Logger.Trace(message: fileNameWithPath + " - writeXMPSideCar - " + writeXMPSideCar);

                        if (!File.Exists(path: xmpFileLocation))
                        {
                            FrmMainApp.Logger.Trace(message: fileNameWithPath + " - writeXMPSideCar - " + writeXMPSideCar + " - File has been created.");

                            // otherwise create a new one. 
                            xmpFileLocation = Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath);
                            UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText: "-tagsfromfile=" + xmpFileLocation);
                        }
                    }

                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText: "-ignoreMinorErrors");

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

                        string exifToolAttribute;
                        string updateExifVal;

                        bool deleteAllGPSData = dtFileWriteQueue.AsEnumerable()
                                                                .Any(predicate: row => "gps*" == row.Field<string>(columnName: "settingId"));

                        bool deleteTagAlreadyAdded = false;

                        // add tags to argsFile
                        foreach (DataRow dataRow in dtFileWriteQueue.Rows)
                        {
                            string settingId = dataRow[columnName: "settingId"]
                               .ToString();
                            string settingValue = dataRow[columnName: "settingValue"]
                               .ToString();

                            ElementAttribute attribute = GetAttributeFromString(attributeToFind: settingId);
                            List<string> orderedTags = new List<string>();
                            try
                            {
                                orderedTags = TagsToAttributesOut[key: attribute];
                            }
                            catch
                            {
                                // ignore.
                            }

                            FrmMainApp.Logger.Trace(message: fileNameWithPath + " - " + settingId + ": " + settingValue);

                            // non-xmp always
                            if (deleteAllGPSData && !deleteTagAlreadyAdded)
                            {
                                UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-gps*=");

                                // this is moved up/in here because the deletion of all gps has to come before just about anything else in case user wants to add (rather than delete) in more tags (later).

                                UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-xmp:gps*=");

                                deleteTagAlreadyAdded = true;
                            }

                            foreach (string objectTagNameOut in orderedTags)
                            {
                                exifToolAttribute = objectTagNameOut;
                                updateExifVal = settingValue;

                                if (updateExifVal != "")
                                {
                                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-" + exifToolAttribute + "=" + updateExifVal);

                                    //if lat/long/alt then add Ref. 
                                    if (!exifToolAttribute.StartsWith("XMP") && exifToolAttribute.EndsWith(value: "GPSLatitude"))
                                    {
                                        if (updateExifVal.Substring(startIndex: 0, length: 1) == FrmMainApp.NullStringEquivalentGeneric)
                                        {
                                            UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-" + exifToolAttribute + "Ref" + "=" + "South");
                                        }
                                        else
                                        {
                                            UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-" + exifToolAttribute + "Ref" + "=" + "North");
                                        }
                                    }
                                    else if (!exifToolAttribute.StartsWith("XMP") && exifToolAttribute.EndsWith(value: "GPSLongitude"))
                                    {
                                        if (updateExifVal.Substring(startIndex: 0, length: 1) == FrmMainApp.NullStringEquivalentGeneric)
                                        {
                                            UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-" + exifToolAttribute + "Ref" + "=" + "West");
                                        }
                                        else
                                        {
                                            UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-" + exifToolAttribute + "Ref" + "=" + "East");
                                        }
                                    }
                                    else if (exifToolAttribute.EndsWith(value: "GPSAltitude"))
                                    {
                                        double.TryParse(s: updateExifVal, style: NumberStyles.AllowDecimalPoint, provider: CultureInfo.InvariantCulture, result: out double tmpAltitude);
                                        updateExifVal = (HelperVariables.UseImperial
                                            ? Math.Round(value: tmpAltitude / HelperVariables.METRETOFEET, digits: 2)
                                            : tmpAltitude).ToString(provider: CultureInfo.InvariantCulture);

                                        // add ref -- "ExifTool will also accept number when writing this tag, with negative numbers indicating below sea level"
                                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-" +
                                                                                                        exifToolAttribute +
                                                                                                        "Ref" +
                                                                                                        "=" +
                                                                                                        (tmpAltitude > 0
                                                                                                            ? "0"
                                                                                                            : "-1"));

                                        // same as below/generic
                                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-" + exifToolAttribute + "=" + updateExifVal);
                                    }
                                }
                                else //delete tag
                                {
                                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-" + exifToolAttribute + "=");

                                    //if lat/long then add Ref. 
                                    if (
                                        exifToolAttribute.EndsWith(value: "GPSLatitude") ||
                                        exifToolAttribute.EndsWith(value: "GPSLongitude")
                                    )
                                    {
                                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-" + exifToolAttribute + "Ref" + "=");
                                    }
                                }

                                if (objectTagNameOut.EndsWith(value: "DateTimeOriginal") || // TakenDate
                                    objectTagNameOut.EndsWith(value: "CreateDate") // CreateDate
                                   )
                                {
                                    bool isTakenDate = false;
                                    bool isCreateDate = false;
                                    if (objectTagNameOut.EndsWith(value: "DateTimeOriginal"))
                                    {
                                        isTakenDate = true;
                                    }
                                    else if (objectTagNameOut.EndsWith(value: "CreateDate"))
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

                                UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-" + exifToolAttribute + "=" + updateExifVal);
                            }
                        }
                    }

                    if (doNotCreateBackup)
                    {
                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-overwrite_original_in_place");
                    }

                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-iptc:codedcharacterset=utf8");

                    if (resetFileDateToCreated)
                    {
                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-filemodifydate<datetimeoriginal");
                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-filecreatedate<datetimeoriginal");
                    }

                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-IPTCDigest=");
                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-execute");

                    if (processOriginalFile)
                    {
                        File.AppendAllText(path: argsFile, contents: exifArgsForOriginalFile, encoding: Encoding.UTF8);
                    }

                    if (writeXMPSideCar)
                    {
                        if (doNotCreateBackup)
                        {
                            //UpdateArgsFile(ArgfileToUpdate.SideCar, "-IPTCDigest=" + Environment.NewLine;
                            UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText: "-overwrite_original_in_place");
                        }

                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText: "-execute");

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

            // this patches the args files in a more organised way.
            void UpdateArgsFile(ArgfileToUpdate argfileToUpdate,
                                string whatText,
                                bool addNewLine = true)
            {
                if (argfileToUpdate == ArgfileToUpdate.Orig || argfileToUpdate == ArgfileToUpdate.Both)
                {
                    if (!exifArgsForOriginalFile.Contains(value: whatText))
                    {
                        exifArgsForOriginalFile += whatText +
                                                   (addNewLine
                                                       ? Environment.NewLine
                                                       : "");
                    }
                }

                ;

                if (argfileToUpdate == ArgfileToUpdate.SideCar || argfileToUpdate == ArgfileToUpdate.Both)
                {
                    if (!exifArgsForSidecar.Contains(value: whatText))
                    {
                        exifArgsForSidecar += whatText +
                                              (addNewLine
                                                  ? Environment.NewLine
                                                  : "");
                    }
                }
            }
        }

        // this is the "optimal" scenario
        if (!failWriteNothingEnabled && !queueWasEmpty)
        {
            FrmMainApp.Logger.Info(message: "Starting ExifTool.");
            ///////////////

            ;
            await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                                         frmMainAppInstance: frmMainAppInstance,
                                                         initiator: "ExifWriteExifToFile",
                                                         processOriginalFile: processOriginalFile = processOriginalFile,
                                                         writeXmpSideCar: writeXMPSideCar = writeXMPSideCar
            );
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
        FrmMainApp.TaskbarManagerInstance.SetProgressState(state: TaskbarProgressBarState.NoProgress);
        FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Ready.");
        HelperGenericFileLocking.FilesAreBeingSaved = false;
    }

    /// <summary>
    ///     Writes outstanding exif changes to files.
    /// </summary>
    /// <returns>Reastically nothing but writes the exif tags and updates the listview rows where necessary</returns>
    private enum ArgfileToUpdate
    {
        Orig,
        SideCar,
        Both
    }
}