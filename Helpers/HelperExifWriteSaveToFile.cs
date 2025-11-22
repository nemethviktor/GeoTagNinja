using GeoTagNinja.Model;
using Microsoft.WindowsAPICodePack.Taskbar;
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
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers;

internal static class HelperExifWriteSaveToFile
{
    internal static async Task ExifWriteExifToFile(HashSet<string> distinctGUIDs)
    {
        FrmMainApp.Log.Info(message: "Starting");

        HelperGenericFileLocking.FilesAreBeingSaved = true;
        string argsFile = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "exifArgsToWrite.args");
        string exiftoolCmd =
            $" -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 -@ {HelperVariables.DoubleQuoteStr}{argsFile}{HelperVariables.DoubleQuoteStr}";

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        // if user switches folder in the process of writing this will keep it standard
        Debug.Assert(condition: frmMainAppInstance != null, message: $"{nameof(frmMainAppInstance)} != null");

        File.Delete(path: argsFile);

        bool processOriginalFile = false;
        bool resetFileDateToCreated = false;
        bool writeXMPSideCar = false;
        bool doNotCreateBackup = false;

        bool failWriteNothingEnabled = false;
        bool queueWasEmpty = true;

        FrmMainApp.TaskbarManagerInstance.SetProgressState(state: TaskbarProgressBarState.Indeterminate);
        string exifArgsForOriginalFile = "";
        string exifArgsForSidecar = "";

        foreach (string GUID in distinctGUIDs)
        {
            DirectoryElement dirElemFileToModify = FrmMainApp.DirectoryElements.FindElementByItemGUID(GUID: GUID);
            FrmMainApp.Log.Trace(message: dirElemFileToModify.FileNameWithPath);

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
                    processOriginalFile = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                        dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: "tpg_FileOptions",
                        settingId:
                        $"{fileExtension.ToLower()}_ckb_ProcessOriginalFile"));
                    writeXMPSideCar = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                        dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: "tpg_FileOptions",
                        settingId:
                        $"{fileExtension.ToLower()}_ckb_AddXMPSideCar"));

                    // this is an issue in Adobe Bridge (unsure). Rating in the XMP needs to be parsed and re-saved.
                    int ratingInXmp = -1;
                    string xmpFileLocation = Path.Combine(path1: folderNameToWrite, path2:
                        $"{Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath))}.xmp");
                    if (File.Exists(path: xmpFileLocation) && writeXMPSideCar) // don't bother if we don't need to (over)write the xmp
                    {
                        foreach (string line in File.ReadLines(path: xmpFileLocation))
                        {
                            if (line.Contains(value: "xmp:Rating="))
                            {
                                _ = int.TryParse(s: line.Replace(oldValue: "xmp:Rating=", newValue: "")
                                                            .Replace(oldValue: "\"", newValue: ""), result: out ratingInXmp);
                                break;
                            }

                            if (line.Contains(value: "<xmp:Rating>"))
                            {
                                _ = int.TryParse(s: line.Replace(oldValue: "<xmp:Rating>", newValue: "")
                                                            .Replace(oldValue: "</xmp:Rating>", newValue: "")
                                                            .Replace(oldValue: " ", newValue: ""), result: out ratingInXmp);
                                break;
                            }
                        }
                    }

                    queueWasEmpty = false;

                    resetFileDateToCreated = Convert.ToBoolean(
                        value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                            dataTable: HelperVariables.DtHelperDataApplicationSettings,
                            settingTabPage: "tpg_FileOptions", settingId:
                            $"{fileExtension.ToLower()}_ckb_ResetFileDateToCreated"));
                    doNotCreateBackup = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                        dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: "tpg_FileOptions",
                        settingId:
                        $"{fileExtension.ToLower()}_ckb_OverwriteOriginal"));

                    // it's a lot less complicated to just pretend we want both the Original File and the Sidecar updated and then not-include them later than to have a Yggdrasil of IFs scattered all over.
                    // ... which latter I would inevitably f...k up at some point.

                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig,
                        whatText: Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath),
                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                        exifArgsForSidecar: ref exifArgsForSidecar); //needs to include folder name
                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-ignoreMinorErrors",
                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                        exifArgsForSidecar: ref exifArgsForSidecar);
                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-progress",
                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                        exifArgsForSidecar: ref exifArgsForSidecar);

                    // this doesn't need to be sent back to the actual XMP file, it's a bug.
                    if (ratingInXmp >= 0)
                    {
                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: $"-Rating={ratingInXmp}",
                            exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                            exifArgsForSidecar: ref exifArgsForSidecar);
                    }

                    // this is re issue #159 and blocking certain warnings from cropping up
                    // -api
                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-api",
                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                        exifArgsForSidecar: ref exifArgsForSidecar);


                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText: Path.Combine(
                            path1: folderNameToWrite, path2:
                            $"{Path.GetFileNameWithoutExtension(path: Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath))}.xmp"),
                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                        exifArgsForSidecar: ref exifArgsForSidecar); //needs to include folder name
                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText: "-progress",
                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                        exifArgsForSidecar: ref exifArgsForSidecar);

                    // sidecar copying needs to be in a separate batch, as technically it's a different file

                    if (writeXMPSideCar)
                    {
                        FrmMainApp.Log.Trace(message: $"{fileNameWithPath} - writeXMPSideCar - {writeXMPSideCar}");

                        if (!File.Exists(path: xmpFileLocation))
                        {
                            FrmMainApp.Log.Trace(message:
                                $"{fileNameWithPath} - writeXMPSideCar - {writeXMPSideCar} - File has been created.");

                            // otherwise create a new one. 
                            xmpFileLocation = Path.Combine(path1: folderNameToWrite, path2: fileNameWithoutPath);
                            UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText:
                                $"-tagsfromfile={xmpFileLocation}",
                                exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                exifArgsForSidecar: ref exifArgsForSidecar);
                        }
                    }

                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText: "-ignoreMinorErrors",
                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                        exifArgsForSidecar: ref exifArgsForSidecar);

                    DataTable dtFileWriteQueue = new();
                    dtFileWriteQueue.Clear();
                    _ = dtFileWriteQueue.Columns.Add(columnName: "ItemNameWithoutPath");
                    _ = dtFileWriteQueue.Columns.Add(columnName: "settingId");
                    _ = dtFileWriteQueue.Columns.Add(columnName: "settingValue");

                    foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                    {
                        if (dirElemFileToModify.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite))
                        {
                            DataRow drFileDataRow = dtFileWriteQueue.NewRow();
                            drFileDataRow[columnName: "ItemNameWithoutPath"] = dirElemFileToModify.ItemNameWithoutPath;
                            drFileDataRow[columnName: "settingId"] = GetElementAttributesName(attributeToFind: attribute);
                            drFileDataRow[columnName: "settingValue"] = !dirElemFileToModify.IsMarkedForDeletion(
                                    attribute: attribute,
                                    version: DirectoryElement.AttributeVersion
                                                             .Stage3ReadyToWrite)
                                ? dirElemFileToModify.GetAttributeValueString(
                                        attribute: attribute,
                                        version: DirectoryElement.AttributeVersion
                                                                 .Stage3ReadyToWrite,
                                        nowSavingExif: true)
                                : "";

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

                            ElementAttribute attribute = GetElementAttributesElementAttribute(attributeToFind: settingId);
                            List<string> orderedTags = [];
                            try
                            {
                                orderedTags =
                                    GetElementAttributesOut(attributeToFind: attribute);
                            }
                            catch
                            {
                                // ignore.
                            }

                            FrmMainApp.Log.Trace(message: $"{fileNameWithPath} - {settingId}: {settingValue}");

                            // non-xmp always
                            if (deleteAllGPSData && !deleteTagAlreadyAdded)
                            {
                                UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-gps*=",
                                    exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                    exifArgsForSidecar: ref exifArgsForSidecar);

                                // this is moved up/in here because the deletion of all gps has to come before just about anything else in case user wants to add (rather than delete) in more tags (later).

                                UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both, whatText: "-xmp:gps*=",
                                    exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                    exifArgsForSidecar: ref exifArgsForSidecar);

                                deleteTagAlreadyAdded = true;
                            }

                            if (orderedTags != null)
                            {
                                foreach (string objectTagNameOut in orderedTags)
                                {
                                    exifToolAttribute = objectTagNameOut;
                                    updateExifVal = settingValue;

                                    if (updateExifVal != "")
                                    {
                                        if (
                                            !objectTagNameOut.EndsWith(value: "DateTimeOriginal") && // TakenDate
                                            !objectTagNameOut.EndsWith(value: "CreateDate") // CreateDate
                                        )
                                        {
                                            UpdateArgsFile(
                                                argfileToUpdate: ArgfileToUpdate.Both,
                                                whatText: $"-{exifToolAttribute}={updateExifVal}",
                                                exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                                exifArgsForSidecar: ref exifArgsForSidecar);
                                        }

                                        //if lat/long/alt then add Ref. 
                                        if (!exifToolAttribute.StartsWith(value: "XMP"))
                                        {
                                            if (exifToolAttribute.EndsWith(value: "GPSLatitude"))
                                            {
                                                if (updateExifVal.Substring(
                                                        startIndex: 0, length: 1) ==
                                                    FrmMainApp
                                                       .NullStringEquivalentGeneric)
                                                {
                                                    UpdateArgsFile(
                                                        argfileToUpdate: ArgfileToUpdate
                                                           .Both,
                                                        whatText: $"-{exifToolAttribute}Ref=South",
                                                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                                        exifArgsForSidecar: ref exifArgsForSidecar);
                                                }
                                                else
                                                {
                                                    UpdateArgsFile(
                                                        argfileToUpdate: ArgfileToUpdate
                                                           .Both,
                                                        whatText: $"-{exifToolAttribute}Ref=North",
                                                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                                        exifArgsForSidecar: ref exifArgsForSidecar);
                                                }
                                            }
                                            else if (exifToolAttribute.EndsWith(value: "GPSLongitude"))
                                            {
                                                if (updateExifVal.Substring(
                                                        startIndex: 0, length: 1) ==
                                                    FrmMainApp
                                                       .NullStringEquivalentGeneric)
                                                {
                                                    UpdateArgsFile(
                                                        argfileToUpdate: ArgfileToUpdate
                                                           .Both,
                                                        whatText: $"-{exifToolAttribute}Ref=West",
                                                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                                        exifArgsForSidecar: ref exifArgsForSidecar);
                                                }
                                                else
                                                {
                                                    UpdateArgsFile(
                                                        argfileToUpdate: ArgfileToUpdate
                                                           .Both,
                                                        whatText: $"-{exifToolAttribute}Ref=East",
                                                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                                        exifArgsForSidecar: ref exifArgsForSidecar);
                                                }
                                            }
                                            else if (exifToolAttribute.EndsWith(value: "GPSAltitude"))
                                            {
                                                _ = double.TryParse(
                                                    s: updateExifVal,
                                                    style: NumberStyles.AllowDecimalPoint,
                                                    provider: CultureInfo
                                                       .InvariantCulture,
                                                    result: out double tmpAltitude);
                                                updateExifVal = (HelperVariables
                                                       .UserSettingUseImperial
                                                        ? Math.Round(
                                                            value: tmpAltitude /
                                                                   HelperVariables.MetreToFeet,
                                                            digits: 2)
                                                        : tmpAltitude)
                                                   .ToString(
                                                        provider:
                                                        CultureInfo.InvariantCulture);

                                                // add ref -- "ExifTool will also accept number when writing this tag, with negative numbers indicating below sea level"
                                                UpdateArgsFile(
                                                    argfileToUpdate: ArgfileToUpdate.Both,
                                                    whatText: $"-{exifToolAttribute}Ref={(tmpAltitude > 0
                                                        ? "0"
                                                        : "-1")}", exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                                    exifArgsForSidecar: ref exifArgsForSidecar);

                                                // same as below/generic
                                                UpdateArgsFile(
                                                    argfileToUpdate: ArgfileToUpdate.Both,
                                                    whatText: $"-{exifToolAttribute}={updateExifVal}",
                                                    exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                                    exifArgsForSidecar: ref exifArgsForSidecar);
                                            }
                                        }
                                    }
                                    else //delete tag
                                    {
                                        UpdateArgsFile(
                                            argfileToUpdate: ArgfileToUpdate.Both,
                                            whatText: $"-{exifToolAttribute}=",
                                            exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                            exifArgsForSidecar: ref exifArgsForSidecar);

                                        //if lat/long then add Ref. 
                                        if (!exifToolAttribute.StartsWith(value: "XMP"))
                                        {
                                            if (
                                                exifToolAttribute.EndsWith(
                                                    value: "GPSLatitude") ||
                                                exifToolAttribute.EndsWith(
                                                    value: "GPSLongitude") ||
                                                exifToolAttribute.EndsWith(
                                                    value: "GPSAltitude")
                                            )
                                            {
                                                UpdateArgsFile(
                                                    argfileToUpdate: ArgfileToUpdate.Both,
                                                    whatText: $"-{exifToolAttribute}Ref=",
                                                    exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                                    exifArgsForSidecar: ref exifArgsForSidecar);
                                            }
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
                                            updateExifVal = DateTime
                                               .Parse(s: settingValue)
                                               .ToString(format: "yyyy-MM-dd HH:mm:ss",
                                               provider: CultureInfo.InvariantCulture);
                                        }
                                        catch
                                        {
                                            updateExifVal = "";
                                        }
                                    }

                                    FrmMainApp.Log.Trace(
                                        message: $"{fileNameWithPath} - {exifToolAttribute}: {updateExifVal}");

                                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both,
                                        whatText: $"-{exifToolAttribute}={updateExifVal}",
                                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                        exifArgsForSidecar: ref exifArgsForSidecar);
                                }
                            }
                        }
                    }

                    if (doNotCreateBackup)
                    {
                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-overwrite_original_in_place",
                            exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                            exifArgsForSidecar: ref exifArgsForSidecar);
                    }

                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-iptc:codedcharacterset=utf8",
                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                        exifArgsForSidecar: ref exifArgsForSidecar);

                    if (resetFileDateToCreated)
                    {
                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig,
                            whatText: "-filemodifydate<datetimeoriginal",
                            exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                            exifArgsForSidecar: ref exifArgsForSidecar);
                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig,
                            whatText: "-filecreatedate<datetimeoriginal",
                            exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                            exifArgsForSidecar: ref exifArgsForSidecar);
                    }

                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-IPTCDigest=",
                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                        exifArgsForSidecar: ref exifArgsForSidecar);
                    UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: "-execute",
                        exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                        exifArgsForSidecar: ref exifArgsForSidecar);

                    if (processOriginalFile)
                    {
                        File.AppendAllText(path: argsFile, contents: exifArgsForOriginalFile, encoding: Encoding.UTF8);
                    }

                    if (writeXMPSideCar)
                    {
                        if (doNotCreateBackup)
                        {
                            //UpdateArgsFile(ArgfileToUpdate.SideCar, "-IPTCDigest=" + Environment.NewLine;
                            UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar,
                                whatText: "-overwrite_original_in_place",
                                exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                exifArgsForSidecar: ref exifArgsForSidecar);
                        }

                        UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.SideCar, whatText: "-execute",
                            exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                            exifArgsForSidecar: ref exifArgsForSidecar);

                        File.AppendAllText(path: argsFile, contents: exifArgsForSidecar, encoding: Encoding.UTF8);
                    }

                    if (!processOriginalFile && !writeXMPSideCar)
                    {
                        failWriteNothingEnabled = true;
                        FrmMainApp.Log.Info(message: "Both file-writes disabled. Nothing Written.");
                        HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                            controlName: "mbx_Helper_WarningNoWriteSettingEnabled",
                            captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Warning,
                            buttons: MessageBoxButtons.OK);
                    }
                }
            }
        }

        // this is the "optimal" scenario
        if (!failWriteNothingEnabled && !queueWasEmpty)
        {
            FrmMainApp.Log.Info(message: "Starting ExifTool.");
            ///////////////

            await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                                         frmMainAppInstance: frmMainAppInstance,
                                                         initiator: HelperGenericAncillaryListsArrays.ExifToolInititators.ExifWriteExifToFile,
                                                         processOriginalFile: processOriginalFile,
                                                         writeXmpSideCar: writeXMPSideCar
            );
        }
        else if (!queueWasEmpty)
        {
            FrmMainApp.Log.Info(message: "Both file-writes disabled. Nothing Written.");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_Helper_WarningNoWriteSettingEnabled",
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Warning,
                buttons: MessageBoxButtons.OK);
        }
        else
        {
            FrmMainApp.Log.Info(message: "Queue was empty. Nothing Written.");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_Helper_WarningNothingInWriteQueue",
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Warning,
                buttons: MessageBoxButtons.OK);
        }

        ///////////////
        FrmMainApp.TaskbarManagerInstance.SetProgressState(state: TaskbarProgressBarState.NoProgress);
        FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Ready.");
        HelperGenericFileLocking.FilesAreBeingSaved = false;
    }

    /// <summary>
    ///     Updates the arguments file
    /// </summary>
    /// <param name="argfileToUpdate">Which file to update -> <see cref="ArgfileToUpdate" /></param>
    /// <param name="whatText">The text to add</param>
    /// <param name="exifArgsForOriginalFile">String for the "main" image</param>
    /// <param name="exifArgsForSidecar">String for the sidecar image</param>
    /// <param name="addNewLine">Whether to add a new line (default true)</param>
    private static void UpdateArgsFile(ArgfileToUpdate argfileToUpdate,
                                       string whatText,
                                       ref string exifArgsForOriginalFile,
                                       ref string exifArgsForSidecar,
                                       bool addNewLine = true)
    {
        if (argfileToUpdate is ArgfileToUpdate.Orig or
            ArgfileToUpdate.Both)
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

        if (argfileToUpdate is ArgfileToUpdate.SideCar or
            ArgfileToUpdate.Both)
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