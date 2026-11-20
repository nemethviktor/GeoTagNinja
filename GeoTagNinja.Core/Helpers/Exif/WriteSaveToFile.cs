using TagList = GeoTagNinja.Model.TagList;
using NLog;
using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.FileSystem;
using GeoTagNinja.Helpers.Geo;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Exif;

internal static class WriteSaveToFile
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    internal static async Task ExifWriteExifToFile(HashSet<string> distinctGUIDs)
    {
        Log.Info(message: "Starting");

        FileLocking.FilesAreBeingSaved = true;
        string argsFile = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "exifArgsToWrite.args");
        string exiftoolCmd =
            $" -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 -@ {HelperVariables.DoubleQuoteStr}{argsFile}{HelperVariables.DoubleQuoteStr}";

        // if user switches folder in the process of writing this will keep it standard

        File.Delete(path: argsFile);

        bool processOriginalFile = false;
        bool resetFileDateToCreated = false;
        bool writeXMPSideCar = false;
        bool doNotCreateBackup = false;

        bool failWriteNothingEnabled = false;
        bool queueWasEmpty = true;
        bool skippedVideoFile = false;

        UiServices.SetTaskbarProgress(state: UiTaskbarProgress.Indeterminate);
        string exifArgsForOriginalFile = "";
        string exifArgsForSidecar = "";

        foreach (string GUID in distinctGUIDs)
        {
            DirectoryElement dirElemFileToModify = HelperVariables.DirectoryElements.FindElementByItemGUID(GUID: GUID);
            Log.Trace(message: dirElemFileToModify.FileNameWithPath);

            if (dirElemFileToModify != null)
            {
                string fileNameWithPath = dirElemFileToModify.FileNameWithPath;
                string fileNameWithoutPath = dirElemFileToModify.ItemNameWithoutPath;
                string folderNameToWrite = Path.GetDirectoryName(path: dirElemFileToModify.FileNameWithPath);
                if (File.Exists(path: fileNameWithPath))
                {
                    // A video has no EXIF block: ExifTool drops the GPS tags written for photos and still reports
                    // success. Only its location is written, as the QuickTime location atoms players read
                    // (VideoLocationArguments); other staged changes to it are not written and the user is told. A video
                    // with nothing to write is skipped before any argument is built, and its edits stay staged.
                    List<string> videoLocationLines = null;
                    if (SupportedFileExtensions.IsVideoFile(fileNameOrExtension: fileNameWithoutPath))
                    {
                        VideoLocationArguments.Plan videoPlan = VideoLocationArguments.PlanFor(element: dirElemFileToModify, useImperial: HelperVariables.UserSettingUseImperial);
                        if (videoPlan.HasOtherEdits)
                        {
                            skippedVideoFile = true;
                        }

                        if (!videoPlan.HasWritableEdit)
                        {
                            Log.Warn(message: $"{fileNameWithPath} - nothing writable staged for this video (only its location and XMP metadata can be written). Skipped.");
                            skippedVideoFile = true;
                            continue;
                        }

                        videoLocationLines = videoPlan.Lines;
                    }

                    exifArgsForOriginalFile = "";
                    exifArgsForSidecar = "";
                    string fileExtension = Path.GetExtension(path: fileNameWithoutPath)
                                               .Substring(startIndex: 1);

                    // check that either/or the orig file or the xmp needs overwriting
                    processOriginalFile = Convert.ToBoolean(value: ApplicationSettings.DataReadSQLiteSettings(
                        dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: "tpg_FileOptions",
                        settingId:
                        $"{fileExtension.ToLower()}_ckb_ProcessOriginalFile"));
                    writeXMPSideCar = Convert.ToBoolean(value: ApplicationSettings.DataReadSQLiteSettings(
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
                        value: ApplicationSettings.DataReadSQLiteSettings(
                            dataTable: HelperVariables.DtHelperDataApplicationSettings,
                            settingTabPage: "tpg_FileOptions", settingId:
                            $"{fileExtension.ToLower()}_ckb_ResetFileDateToCreated"));
                    doNotCreateBackup = Convert.ToBoolean(value: ApplicationSettings.DataReadSQLiteSettings(
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

                    // (No "-api" line here. #159 added "-api" followed by a "NoWarning=(...)" line to hide harmless ExifTool
                    // warnings; #165 moved that filtering to the app (ExifToolOperator.ignoreExifToolWarningsList) and deleted the
                    // second line but left the first. A bare "-api" makes ExifTool take the NEXT argument as an API option name
                    // and drop it, so the first tag line of every write was lost: masked for a photo (the first line is a GPS
                    // latitude that is written again under another name), but a video's location would lose one of its two
                    // atoms.)

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
                        Log.Trace(message: $"{fileNameWithPath} - writeXMPSideCar - {writeXMPSideCar}");

                        if (!File.Exists(path: xmpFileLocation))
                        {
                            Log.Trace(message:
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

                    // keywords that name the old location follow the edit (or removal) of it
                    if (HelperVariables.UserSettingSyncGeoKeywords)
                    {
                        _ = GeoKeywordSync.StageFor(element: dirElemFileToModify);
                    }

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
                                ? dirElemFileToModify.GetAttributeValueAsString(
                                        attribute: attribute,
                                        version: DirectoryElement.AttributeVersion
                                                                 .Stage3ReadyToWrite,
                                        context: ValueFormatContext.ExifTool)
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
                        if (videoLocationLines != null)
                        {
                            foreach (string videoLine in videoLocationLines)
                            {
                                UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Orig, whatText: videoLine,
                                    exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                    exifArgsForSidecar: ref exifArgsForSidecar);
                            }
                        }

                        foreach (DataRow dataRow in dtFileWriteQueue.Rows)
                        {
                            string settingId = dataRow[columnName: "settingId"]
                               .ToString();
                            string settingValue = dataRow[columnName: "settingValue"]
                               .ToString();

                            // the display sentinel means "blank"; it must delete the tag, never be written into the file as a literal "-"
                            if (settingValue == NullEquivalents.StringGeneric)
                            {
                                settingValue = NullEquivalents.StringBlank;
                            }

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

                            // A video: its location is written above; of the rest only what can go into its XMP (see
                            // VideoLocationArguments), and only those tags, as ExifTool drops the IPTC/EXIF ones for a video.
                            if (videoLocationLines != null)
                            {
                                if (!VideoLocationArguments.IsXmpWritable(attribute: attribute))
                                {
                                    continue;
                                }

                                orderedTags = VideoLocationArguments.XmpTagsOnly(tags: orderedTags);
                            }

                            Log.Trace(message: $"{fileNameWithPath} - {settingId}: {settingValue}");

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

                                    // A multi-value tag is written item by item; the generic "-Tag=value" below would
                                    // store the whole list as one entry.
                                    if (GetElementAttributesType(attributeToFind: attribute) == typeof(TagList))
                                    {
                                        foreach (string listLine in ListTagArguments.Build(tag: objectTagNameOut,
                                                     items: TagList.Parse(text: settingValue)))
                                        {
                                            UpdateArgsFile(argfileToUpdate: ArgfileToUpdate.Both,
                                                whatText: listLine,
                                                exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                                exifArgsForSidecar: ref exifArgsForSidecar);
                                        }

                                        continue;
                                    }

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

                                        // an image direction is meaningless to ExifTool without saying whether it is true or magnetic north
                                        if (exifToolAttribute.EndsWith(value: "GPSImgDirection"))
                                        {
                                            UpdateArgsFile(
                                                argfileToUpdate: ArgfileToUpdate.Both,
                                                whatText: $"-{exifToolAttribute}Ref={(dirElemFileToModify.GetAttributeValueAsString(attribute: ElementAttribute.GPSImgDirectionRef, notFoundValue: "").StartsWith(value: "Magnetic", comparisonType: StringComparison.OrdinalIgnoreCase) ? "M" : "T")}",
                                                exifArgsForOriginalFile: ref exifArgsForOriginalFile,
                                                exifArgsForSidecar: ref exifArgsForSidecar);
                                        }

                                        //if lat/long/alt then add Ref. 
                                        if (!exifToolAttribute.StartsWith(value: "XMP"))
                                        {
                                            if (exifToolAttribute.EndsWith(value: "GPSLatitude") ||
                                                exifToolAttribute.EndsWith(value: "GPSDestLatitude"))
                                            {
                                                if (updateExifVal.Substring(
                                                        startIndex: 0, length: 1) ==
                                                    NullEquivalents.StringGeneric)
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
                                            else if (exifToolAttribute.EndsWith(value: "GPSLongitude") ||
                                                     exifToolAttribute.EndsWith(value: "GPSDestLongitude"))
                                            {
                                                if (updateExifVal.Substring(
                                                        startIndex: 0, length: 1) ==
                                                    NullEquivalents.StringGeneric)
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
                                                    value: "GPSDestLatitude") ||
                                                exifToolAttribute.EndsWith(
                                                    value: "GPSDestLongitude") ||
                                                exifToolAttribute.EndsWith(
                                                    value: "GPSImgDirection") ||
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
                                        // settingValue was rendered with ValueFormatContext.ExifTool, so it is
                                        // already canonical; re-parsing it only guards against a value that
                                        // reached the queue by some other route. Parsing it with the current
                                        // culture (as this did) threw on any machine whose locale disagreed with
                                        // that layout, and the catch then silently dropped the timestamp.
                                        updateExifVal =
                                            AttributeValueFormatter.TryParseDateTime(
                                                value: settingValue,
                                                context: ValueFormatContext.ExifTool,
                                                result: out DateTime parsedTagDate)
                                                ? AttributeValueFormatter.FormatDateTime(
                                                    value: parsedTagDate,
                                                    context: ValueFormatContext.ExifTool)
                                                : "";
                                    }

                                    Log.Trace(
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

                    // -P keeps the file's existing modification date, so a location added to an old photo doesn't make
                    // it look newly modified in File Explorer. The per-extension "reset to CreateDate" option below
                    // is an explicit request for a particular date, so it takes precedence and -P is not sent.
                    // This is appended directly rather than via UpdateArgsFile, whose "already contains" check would
                    // skip "-P" whenever a file path happened to contain it (e.g. a "Trip-Paris" folder).
                    if (HelperVariables.UserSettingPreserveFileModifiedDate && !resetFileDateToCreated)
                    {
                        exifArgsForOriginalFile += $"-P{Environment.NewLine}";
                        exifArgsForSidecar += $"-P{Environment.NewLine}";
                    }

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
                        Log.Info(message: "Both file-writes disabled. Nothing Written.");
                        UiServices.ShowMessageBox(
                            message: HelperControlAndMessageBoxHandling.ReturnControlText(
                                controlName: "mbx_Helper_WarningNoWriteSettingEnabled",
                                fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                            icon: UiMessageIcon.Warning,
                            buttons: UiMessageButtons.OK);
                    }
                }
            }
        }

        // this is the "optimal" scenario
        if (!failWriteNothingEnabled && !queueWasEmpty)
        {
            Log.Info(message: "Starting ExifTool.");
            ///////////////

            await RunWriteWithRetry(argsFile: argsFile,
                                    exiftoolCmd: exiftoolCmd,
                                    processOriginalFile: processOriginalFile,
                                    writeXMPSideCar: writeXMPSideCar);
        }
        else if (!queueWasEmpty)
        {
            Log.Info(message: "Both file-writes disabled. Nothing Written.");
            UiServices.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_Helper_WarningNoWriteSettingEnabled",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                icon: UiMessageIcon.Warning,
                buttons: UiMessageButtons.OK);
        }
        else if (!skippedVideoFile)
        {
            Log.Info(message: "Queue was empty. Nothing Written.");
            UiServices.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_Helper_WarningNothingInWriteQueue",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                icon: UiMessageIcon.Warning,
                buttons: UiMessageButtons.OK);
        }

        if (skippedVideoFile)
        {
            UiServices.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_Helper_WarningVideoNotWritable",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                icon: UiMessageIcon.Warning,
                buttons: UiMessageButtons.OK);
        }

        ///////////////
        UiServices.SetTaskbarProgress(state: UiTaskbarProgress.NoProgress);
        UiServices.SetStatusText(text: $"{HelperControlAndMessageBoxHandling.ReturnControlText("Generic_Ready", HelperControlAndMessageBoxHandling.FakeControlTypes.Generic)}.");
        FileLocking.FilesAreBeingSaved = false;
    }

    private const int MaxQuietRetryPasses = 20;
    private const int MaxPassesWithoutProgress = 3;
    private const int RetryDelayMilliseconds = 1000;

    /// <summary>
    ///     Runs the write argfile through ExifTool. Cloud-backed folders (Proton Drive, #237) can make files vanish
    ///     for a moment after a neighbour was written, so ExifTool answers "File not found" for files that exist and
    ///     only about one file per run gets saved. The files reported missing are therefore retried quietly, after a
    ///     short pause, for as long as each pass makes progress. Whatever is still failing then gets a final run that
    ///     shows ExifTool's messages as usual, so a genuinely missing file is still reported (just a few seconds later).
    /// </summary>
    private static async Task RunWriteWithRetry(string argsFile,
                                                string exiftoolCmd,
                                                bool processOriginalFile,
                                                bool writeXMPSideCar)
    {
        List<List<string>> blocks = ReadArgBlocks(argsFile: argsFile);
        HashSet<string> missingFiles = new(comparer: StringComparer.OrdinalIgnoreCase);
        int passesWithoutProgress = 0;
        int previousMissingCount = int.MaxValue;

        for (int pass = 0; pass < MaxQuietRetryPasses; pass++)
        {
            missingFiles.Clear();
            await ExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                               initiator: ExifToolInititators.ExifWriteExifToFile,
                                               processOriginalFile: processOriginalFile,
                                               writeXmpSideCar: writeXMPSideCar,
                                               missingFiles: missingFiles);

            if (missingFiles.Count == 0)
            {
                return;
            }

            passesWithoutProgress = missingFiles.Count < previousMissingCount ? 0 : passesWithoutProgress + 1;
            previousMissingCount = missingFiles.Count;
            Log.Info(message: $"Pass {pass + 1}: {missingFiles.Count} file(s) reported as not found ({string.Join(separator: ", ", values: missingFiles)}).");

            if (passesWithoutProgress >= MaxPassesWithoutProgress)
            {
                break;
            }

            // keep only the blocks of the files that failed
            blocks = blocks.Where(predicate: block => missingFiles.Contains(item: ExifToolOperator.NormaliseArgPath(path: block[index: 0])))
                           .ToList();
            if (blocks.Count == 0)
            {
                // ExifTool named something that is not one of ours; nothing to retry, so don't hide it.
                break;
            }

            WriteArgBlocks(argsFile: argsFile, blocks: blocks);
            await Task.Delay(millisecondsDelay: RetryDelayMilliseconds);
        }

        // Last go, loud: shows the user whatever still cannot be written.
        if (missingFiles.Count > 0)
        {
            List<List<string>> remaining = blocks.Where(predicate: block => missingFiles.Contains(item: ExifToolOperator.NormaliseArgPath(path: block[index: 0])))
                                                 .ToList();
            if (remaining.Count > 0)
            {
                WriteArgBlocks(argsFile: argsFile, blocks: remaining);
            }

            await ExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                               initiator: ExifToolInititators.ExifWriteExifToFile,
                                               processOriginalFile: processOriginalFile,
                                               writeXmpSideCar: writeXMPSideCar);
        }
    }

    /// <summary>
    ///     Splits the argfile into one block per ExifTool command (a block ends at its "-execute" line). The first line
    ///     of a block is the file path it applies to.
    /// </summary>
    private static List<List<string>> ReadArgBlocks(string argsFile)
    {
        List<List<string>> blocks = [];
        List<string> current = [];
        foreach (string line in File.ReadAllLines(path: argsFile, encoding: Encoding.UTF8))
        {
            if (line == "-execute")
            {
                if (current.Count > 0)
                {
                    blocks.Add(item: current);
                }

                current = [];
            }
            else if (line.Length > 0)
            {
                current.Add(item: line);
            }
        }

        return blocks;
    }

    private static void WriteArgBlocks(string argsFile,
                                       List<List<string>> blocks)
    {
        StringBuilder sb = new();
        foreach (List<string> block in blocks)
        {
            foreach (string line in block)
            {
                _ = sb.Append(value: line).Append(value: Environment.NewLine);
            }

            _ = sb.Append(value: "-execute").Append(value: Environment.NewLine);
        }

        File.WriteAllText(path: argsFile, contents: sb.ToString(), encoding: Encoding.UTF8);
    }

    /// <summary>
    ///     Updates the arguments file. An argument is added unless the file already has exactly that line (see
    ///     <see cref="ExifArgumentLines" /> for why this is not a substring test).
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
            _ = ExifArgumentLines.AddUnique(arguments: ref exifArgsForOriginalFile, line: whatText, addNewLine: addNewLine);
        }

        if (argfileToUpdate is ArgfileToUpdate.SideCar or
            ArgfileToUpdate.Both)
        {
            _ = ExifArgumentLines.AddUnique(arguments: ref exifArgsForSidecar, line: whatText, addNewLine: addNewLine);
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