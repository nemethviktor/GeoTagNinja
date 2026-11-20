using NLog;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Exif;

internal static class ExifToolOperator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // this is a list of junk that we generally want to ignore coming from exiftool's error reporting
    private static List<string> ignoreExifToolWarningsList =
    [
        "requires ExifVersion",
        "is not allowed in JPEG",
        "MakerNote",
        "Warning",
        "files updated",
        "files created"
    ];

    private static int _exifInvokeCounter;

    /// <summary>
    ///     When a write run is collecting missing files (see the <c>missingFiles</c> parameter of
    ///     <see cref="RunExifTool" />), recognises ExifTool's "File not found" line and its "N files weren't updated due
    ///     to errors" summary. The path from the former is added to <paramref name="missingFiles" />.
    /// </summary>
    /// <returns>True if the line is one of those two and so must not be shown to the user.</returns>
    private static bool TryCaptureMissingFile(string line,
                                              ISet<string> missingFiles)
    {
        if (missingFiles == null || string.IsNullOrEmpty(value: line))
        {
            return false;
        }

        const string notFoundMarker = "File not found - ";
        int markerIndex = line.IndexOf(value: notFoundMarker, comparisonType: StringComparison.OrdinalIgnoreCase);
        if (markerIndex >= 0)
        {
            lock (missingFiles)
            {
                _ = missingFiles.Add(item: NormaliseArgPath(path: line.Substring(startIndex: markerIndex + notFoundMarker.Length)));
            }

            return true;
        }

        // the summary ExifTool prints after the failed file; it adds nothing the retry does not already handle
        return line.IndexOf(value: "due to errors", comparisonType: StringComparison.OrdinalIgnoreCase) >= 0 &&
               line.IndexOf(value: "weren't update", comparisonType: StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    ///     ExifTool reports paths with forward slashes; the argfile has them as Windows paths. Makes them comparable.
    /// </summary>
    internal static string NormaliseArgPath(string path)
    {
        return path.Trim().Replace(oldChar: '/', newChar: '\\');
    }

    /// <summary>
    ///     This is a unified method for calling ExifTool. Any special "dealings" are done inside the message handling.
    ///     (possibly the wrong spot to do it)
    /// </summary>
    /// <param name="exiftoolCmd">Variables that need to be passed to ET</param>
    /// <param name="initiator">String value of "who called it". </param>
    /// <param name="processOriginalFile"></param>
    /// <param name="writeXmpSideCar"></param>
    /// <param name="missingFiles">
    ///     Write runs only. When not null, a file ExifTool reports as "not found" is added here (as a normalised path)
    ///     instead of being shown to the user, so the caller can retry it. Null shows every message as before.
    /// </param>
    /// <returns>Empty Task</returns>
    internal static async Task RunExifTool(string exiftoolCmd,
                                           ExifToolInititators initiator,
                                           bool processOriginalFile = false,
                                           bool writeXmpSideCar = false,
                                           ISet<string> missingFiles = null)
    {
        int lviIndex = 0;
        _exifInvokeCounter += 1;
        Log.Trace(message:
            $"Start EXIF Tool number {_exifInvokeCounter} for {initiator} with cmdLine: {exiftoolCmd}");
        await Task.Run(action: () =>
        {
            using Process prcExifTool = new();

            prcExifTool.StartInfo =
                new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
                {
                    Arguments =
                        $@"/c {HelperVariables.DoubleQuoteStr}{HelperVariables.DoubleQuoteStr}{HelperVariables.ExifToolExePathToUse}{HelperVariables.DoubleQuoteStr} {exiftoolCmd}{HelperVariables.DoubleQuoteStr}",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

            prcExifTool.EnableRaisingEvents = true;

            switch (initiator)
            {
                case ExifToolInititators.ExifWriteExifToFile:
                    string fileNameWithPath = null;
                    string fileNameWithoutPath = null;
                    DirectoryElement dirElemFileToDrop = null;

                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (!string.IsNullOrEmpty(value: data.Data))
                        {
                            if (TryCaptureMissingFile(line: data.Data, missingFiles: missingFiles))
                            {
                                return;
                            }

                            if (data.Data.Contains(value: "="))
                            {
                                string exifToolLineWithoutEqualSigns =
                                    data.Data.Replace(oldValue: "=", newValue: "");
                                fileNameWithPath = exifToolLineWithoutEqualSigns
                                                  .Replace(oldChar: '/', newChar: '\\')
                                                  .Remove(
                                                       startIndex:
                                                       exifToolLineWithoutEqualSigns
                                                          .LastIndexOf(value: '['))
                                                  .Trim();

                                // basically we need to check that the combination of what-to-save is _not_ xmp-only. 
                                // if it _is_ xmp-only then this causes a problem because the xmp file isn't really a DE per se so it'd never get removed from the queue.
                                // problem is that if only the xmp file gets overwritten then there is no indication of the original file here. 

                                if (!processOriginalFile &&
                                    writeXmpSideCar &&
                                    fileNameWithPath.EndsWith(value: ".xmp"))
                                {
                                    string pathOfFile =
                                        fileNameWithPath.Substring(
                                            startIndex: 0,
                                            length: fileNameWithPath.Length - 4);
                                    dirElemFileToDrop =
                                        HelperVariables.DirectoryElements
                                                  .FindElementByBelongingToXmpWithPath(
                                                       XMPFileNameWithPath: pathOfFile);
                                }
                                else
                                {
                                    dirElemFileToDrop =
                                        HelperVariables.DirectoryElements
                                                  .FindElementByFileNameWithPath(
                                                       FileNameWithPath:
                                                       fileNameWithPath);
                                }
                            }

                            if (dirElemFileToDrop != null)
                            {
                                fileNameWithoutPath =
                                    dirElemFileToDrop.ItemNameWithoutPath;
                                UiServices.SetStatusText(text: $"Processing: {fileNameWithoutPath}");
                                Log.Debug(
                                    message: $"Writing {fileNameWithoutPath} [this is via OutputDataReceived]");

                                try
                                {
                                    if (lviIndex % 10 == 0)
                                    {
                                        UiServices.ProcessUiEvents();

                                        // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                        UiServices.ScrollFileListTo(itemText: fileNameWithoutPath);
                                    }

                                    if ((data.Data.Contains(value: "files updated") ||
                                         data.Data.Contains(value: "files created")) &&
                                        !data.Data.Trim().StartsWith(value: "0"))
                                    {
                                        RemoveDirElementFromDe3AndCopyDataToOriginal(
                                            dirElemToDrop: dirElemFileToDrop);
                                    }

                                    if (Path.GetExtension(path: fileNameWithoutPath) == ".xmp")
                                    {
                                        if (lviIndex % 10 == 0)
                                        {
                                            UiServices.ProcessUiEvents();
                                            // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                            UiServices.ScrollFileListTo(itemText:
                                                                   fileNameWithoutPath); // this is redundant here.
                                        }

                                        if ((data.Data.Contains(value: "files updated") ||
                                             data.Data.Contains(value: "files created")) &&
                                            !data.Data.Trim().StartsWith(value: "0"))
                                        {
                                            RemoveDirElementFromDe3AndCopyDataToOriginal(
                                                dirElemToDrop: dirElemFileToDrop);
                                            if (!processOriginalFile && writeXmpSideCar)
                                            {
                                                string pathOfFile =
                                                    fileNameWithPath.Substring(
                                                        startIndex: 0,
                                                        length: fileNameWithPath.Length - 4);
                                                dirElemFileToDrop =
                                                    HelperVariables.DirectoryElements
                                                              .FindElementByBelongingToXmpWithPath(
                                                                   XMPFileNameWithPath:
                                                                   pathOfFile);
                                                RemoveDirElementFromDe3AndCopyDataToOriginal(
                                                    dirElemToDrop: dirElemFileToDrop);
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    // ignored
                                }

                                lviIndex++;

                                string dataData = data.Data;
                                bool hideWarning = ignoreExifToolWarningsList.Any(predicate: s =>
                                    dataData.IndexOf(value: s, comparisonType: StringComparison.OrdinalIgnoreCase) >=
                                    0);

                                if (!hideWarning &&
                                    !data.Data.Contains(
                                        value: fileNameWithoutPath.Substring(
                                            startIndex: 0,
                                            length: fileNameWithoutPath.LastIndexOf(value: '.'))))
                                {
                                    bool pathIsLikelyUTF = fileNameWithPath.Any(predicate: c => c > 127);
                                    UiServices.ShowMessageBox(message: data.Data +
                                                          (pathIsLikelyUTF
                                                              ? Environment.NewLine + Environment.NewLine +
                                                                HelperControlAndMessageBoxHandling
                                                                   .ReturnControlText(
                                                                        controlName:
                                                                        "mbx_GenericPathLikelyUTF",
                                                                        fakeControlType:
                                                                        HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox)
                                                              : ""));
                                }
                            }
                        }
                    };

                    prcExifTool.ErrorDataReceived += (_,
                                                      data) =>
                    {
                        if (!string.IsNullOrEmpty(value: data.Data))
                        {
                            if (TryCaptureMissingFile(line: data.Data, missingFiles: missingFiles))
                            {
                                return;
                            }

                            string dataData = data.Data;
                            bool hideWarning = ignoreExifToolWarningsList.Any(predicate: s =>
                                dataData.IndexOf(value: s, comparisonType: StringComparison.OrdinalIgnoreCase) >= 0);

                            if (!hideWarning)
                            {
                                UiServices.ShowMessageBox(message: data.Data);
                            }
                        }
                    };
                    break;
                case ExifToolInititators.ExifGetTrackSyncDataReadSyncPhotos:
                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (data.Data is { Length: > 0 } &&
                            // this piece of info is irrelevant and confusing to the user
                            !data.Data.ToString()
                                 .EndsWith(value: ".xmp does not exist") &&
                            // this piece of info is irrelevant and confusing to the user
                            !data.Data.ToString()
                                 .EndsWith(value: "is not defined")
                           )
                        {
                            HelperVariables._sOutputAndErrorMsg +=
                                data.Data.ToString() + Environment.NewLine;
                        }
                    };
                    prcExifTool.ErrorDataReceived += (_,
                                                      data) =>
                    {
                        if (data.Data != null &&
                            data.Data.Length > 0)
                        {
                            HelperVariables._sOutputAndErrorMsg += $"ERROR: {data.Data}{Environment.NewLine}";
                        }
                    };
                    break;
                case ExifToolInititators.ExifGetTrackSyncDataReadTrackPath:
                case ExifToolInititators.ExifGetTrackSyncDataWriteTrackPath:
                default:
                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (data.Data != null &&
                            data.Data.Length > 0)
                        {
                            HelperVariables._sOutputAndErrorMsg +=
                                data.Data.ToString() + Environment.NewLine;
                        }
                    };

                    prcExifTool.ErrorDataReceived += (_,
                                                      data) =>
                    {
                        if (data.Data != null &&
                            data.Data.Length > 0)
                        {
                            HelperVariables._sOutputAndErrorMsg += $"ERROR: {data.Data}{Environment.NewLine}";
                        }
                    };
                    break;
            }

            Log.Trace(message: $"EXIF number {_exifInvokeCounter}: Start");
            _ = prcExifTool.Start();
            prcExifTool.BeginOutputReadLine();
            prcExifTool.BeginErrorReadLine();
            Log.Trace(message: $"EXIF number {_exifInvokeCounter}: Wait for Exit");
            prcExifTool.WaitForExit();
            Log.Trace(message: $"EXIF number {_exifInvokeCounter}: Close");
            prcExifTool.Close();
            Log.Trace(message: "Closing exifTool");

        });
    }

    /// <summary>
    /// Copies the Stage3ReadyToWrite value to Original assuming that 1) it exists and 2) it isn't an item that's pending deletion...
    /// Then it deletes the original DE3 (Stage3ReadyToWrite) value.
    /// </summary>
    /// <param name="dirElemToDrop">The DirectoryElement to be removed from the DE3 collection.</param>
    
    private static void RemoveDirElementFromDe3AndCopyDataToOriginal(
        DirectoryElement dirElemToDrop)
    {
        string fileNameExtension = dirElemToDrop.Extension;
        if (!fileNameExtension.EndsWith(value: ".xmp"))
        {
            foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(
                         enumType: typeof(ElementAttribute)))
            {
                if (dirElemToDrop.HasSpecificAttributeWithVersion(
                        attribute: attribute,
                        version: DirectoryElement.AttributeVersion
                                                 .Stage3ReadyToWrite))
                {
                    if (!dirElemToDrop.IsMarkedForDeletion(
                            attribute: attribute,
                            version: DirectoryElement.AttributeVersion
                                                     .Stage3ReadyToWrite))
                    {
                        dirElemToDrop.SetAttributeValueAnyType(
                            attribute: attribute,
                            value:
                            dirElemToDrop.GetAttributeValueAsString(
                                attribute: attribute,
                                version: DirectoryElement.AttributeVersion
                                                         .Stage3ReadyToWrite,
                                context: ValueFormatContext.RoundTrip),
                            version: DirectoryElement.AttributeVersion.Original,
                            isMarkedForDeletion: dirElemToDrop.IsMarkedForDeletion(
                                attribute: attribute,
                                version: DirectoryElement.AttributeVersion
                                                         .Stage3ReadyToWrite));
                    }
                }

                dirElemToDrop.RemoveAttributeValue(
                    attribute: attribute,
                    version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
            }
        }

        UiServices.MarkFileListItemClean(directoryElement: dirElemToDrop);
    }
}