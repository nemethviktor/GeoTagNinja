using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers;

internal static class HelperExifExifToolOperator
{
    private static int _exifInvokeCounter;

    /// <summary>
    ///     This is a unified method for calling ExifTool. Any special "dealings" are done inside the message handling.
    ///     (possibly the wrong spot to do it)
    /// </summary>
    /// <param name="exiftoolCmd">Variables that need to be passed to ET</param>
    /// <param name="frmMainAppInstance">
    ///     Only relevant when called from ExifWriteExifToFile -> used to update the values/colour
    ///     in the main listView
    /// </param>
    /// <param name="initiator">String value of "who called it". </param>
    /// <param name="processOriginalFile"></param>
    /// <param name="writeXmpSideCar"></param>
    /// <returns>Empty Task</returns>
    internal static async Task RunExifTool(string exiftoolCmd,
                                           FrmMainApp frmMainAppInstance,
                                           string initiator,
                                           bool processOriginalFile = false,
                                           bool writeXmpSideCar = false)
    {
        int lviIndex = 0;
        _exifInvokeCounter += 1;
        FrmMainApp.Logger.Trace(message: "Start EXIF Tool number " +
                                         _exifInvokeCounter +
                                         " for " +
                                         initiator +
                                         " with cmdLine: " +
                                         exiftoolCmd);
        await Task.Run(action: () =>
        {
            using Process prcExifTool = new();

            prcExifTool.StartInfo =
                new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
                {
                    Arguments = @"/c " +
                                HelperVariables.DoubleQuoteStr +
                                HelperVariables.DoubleQuoteStr +
                                HelperVariables.ExifToolExePathToUse +
                                HelperVariables.DoubleQuoteStr +
                                " " +
                                exiftoolCmd +
                                HelperVariables.DoubleQuoteStr,
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
                case "ExifWriteExifToFile":
                    string fileNameWithPath = null;
                    string fileNameWithoutPath = null;
                    DirectoryElement dirElemFileToDrop = null;

                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (!string.IsNullOrEmpty(value: data.Data))
                        {
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
                                        FrmMainApp.DirectoryElements
                                                  .FindElementByBelongingToXmpWithPath(
                                                       XMPFileNameWithPath: pathOfFile);
                                }
                                else
                                {
                                    dirElemFileToDrop =
                                        FrmMainApp.DirectoryElements
                                                  .FindElementByFileNameWithPath(
                                                       FileNameWithPath:
                                                       fileNameWithPath);
                                }
                            }

                            if (dirElemFileToDrop != null)
                            {
                                fileNameWithoutPath =
                                    dirElemFileToDrop.ItemNameWithoutPath;
                                FrmMainApp.HandlerUpdateLabelText(
                                    label: frmMainAppInstance.lbl_ParseProgress,
                                    text: "Processing: " + fileNameWithoutPath);
                                FrmMainApp.Logger.Debug(
                                    message: "Writing " +
                                             fileNameWithoutPath +
                                             " [this is via OutputDataReceived]");

                                try
                                {
                                    if (lviIndex % 10 == 0)
                                    {
                                        Application.DoEvents();

                                        // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                        frmMainAppInstance.lvw_FileList.ScrollToDataPoint(
                                            itemText: fileNameWithoutPath);
                                    }

                                    if ((data.Data.Contains(value: "files updated") ||
                                         data.Data.Contains(value: "files created")) &&
                                        !data.Data.Trim()
                                             .StartsWith(value: "0"))
                                    {
                                        RemoveDirElementFromDe3AndCopyDataToOriginal(
                                            dirElemToDrop: dirElemFileToDrop,
                                            frmMainAppInstance: frmMainAppInstance);
                                    }

                                    if (Path.GetExtension(path: fileNameWithoutPath) ==
                                        ".xmp")
                                    {
                                        if (lviIndex % 10 == 0)
                                        {
                                            Application.DoEvents();
                                            // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                            frmMainAppInstance.lvw_FileList
                                               .ScrollToDataPoint(
                                                    itemText:
                                                    fileNameWithoutPath); // this is redundant here.
                                        }

                                        if ((data.Data.Contains(value: "files updated") ||
                                             data.Data.Contains(
                                                 value: "files created")) &&
                                            !data.Data.Trim()
                                                 .StartsWith(value: "0"))
                                        {
                                            RemoveDirElementFromDe3AndCopyDataToOriginal(
                                                dirElemToDrop: dirElemFileToDrop,
                                                frmMainAppInstance: frmMainAppInstance);
                                            if (!processOriginalFile && writeXmpSideCar)
                                            {
                                                string pathOfFile =
                                                    fileNameWithPath.Substring(
                                                        startIndex: 0,
                                                        length: fileNameWithPath.Length -
                                                        4);
                                                dirElemFileToDrop =
                                                    FrmMainApp.DirectoryElements
                                                       .FindElementByBelongingToXmpWithPath(
                                                            XMPFileNameWithPath:
                                                            pathOfFile);
                                                RemoveDirElementFromDe3AndCopyDataToOriginal(
                                                    dirElemToDrop: dirElemFileToDrop,
                                                    frmMainAppInstance:
                                                    frmMainAppInstance);
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    // ignored
                                }

                                lviIndex++;

                                if (!data.Data.Contains(value: "files updated") &&
                                    !data.Data.Contains(value: "files created") &&
                                    !data.Data.Contains(
                                        value: fileNameWithoutPath.Substring(
                                            startIndex: 0,
                                            length: fileNameWithoutPath.LastIndexOf(
                                                value: '.'))))
                                {
                                    bool pathIsLikelyUTF =
                                        fileNameWithPath.Any(predicate: c => c > 127);
                                    MessageBox.Show(text: data.Data +
                                                          (pathIsLikelyUTF
                                                              ? Environment.NewLine +
                                                              Environment.NewLine +
                                                              HelperControlAndMessageBoxHandling
                                                                 .GenericGetMessageBoxText(
                                                                      messageBoxName:
                                                                      "mbx_GenericPathLikelyUTF")
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
                            MessageBox.Show(text: data.Data);
                        }
                    };
                    break;
                case "GenericCheckForNewVersions":
                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (data.Data != null &&
                            data.Data.Length > 0)
                        {
                            HelperVariables._sOutputAndErrorMsg +=
                                data.Data.ToString() + Environment.NewLine;
                        }

                        decimal.TryParse(s: HelperVariables._sOutputAndErrorMsg
                                            .Replace(oldValue: "\r", newValue: "")
                                            .Replace(oldValue: "\n", newValue: ""),
                                         provider: CultureInfo.InvariantCulture,
                                         style: NumberStyles.Any,
                                         result: out HelperVariables
                                            .CurrentExifToolVersionLocal
                        );
                    };

                    break;
                case "ExifGetTrackSyncData":
                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (data.Data is
                            {
                                Length: > 0
                            } &&
                            // this piece of info is irrelevant and confusing to the user
                            !data.Data.ToString()
                                 .EndsWith(
                                      value:
                                      ".xmp does not exist") &&
                            // this piece of info is irrelevant and confusing to the user
                            !data.Data.ToString()
                                 .EndsWith(
                                      value:
                                      "is not defined")
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
                            HelperVariables._sOutputAndErrorMsg += "ERROR: " +
                                data.Data.ToString() +
                                Environment.NewLine;
                        }
                    };
                    break;
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
                            HelperVariables._sOutputAndErrorMsg += "ERROR: " +
                                data.Data.ToString() +
                                Environment.NewLine;
                        }
                    };
                    break;
            }

            FrmMainApp.Logger.Trace(message: "EXIF number " +
                                             _exifInvokeCounter +
                                             ": Start");
            prcExifTool.Start();
            prcExifTool.BeginOutputReadLine();
            prcExifTool.BeginErrorReadLine();
            FrmMainApp.Logger.Trace(message: "EXIF number " +
                                             _exifInvokeCounter +
                                             ": Wait for Exit");
            prcExifTool.WaitForExit();
            FrmMainApp.Logger.Trace(message: "EXIF number " +
                                             _exifInvokeCounter +
                                             ": Close");
            prcExifTool.Close();
            FrmMainApp.Logger.Trace(message: "Closing exifTool");

            // if still here then exorcise
            try
            {
                FrmMainApp.Logger.Trace(message: "Killing exifTool");
                prcExifTool.Kill();
            }
            catch
            {
                // "funnily" enough this seems to persist for some reason. Unsure why.
                FrmMainApp.Logger.Error(message: "Killing exifTool failed");
            }
        });
    }

    /// <summary>
    /// Copies the Stage3ReadyToWrite value to Original assuming that 1) it exists and 2) it isn't an item that's pending deletion...
    /// Then it deletes the original DE3 (Stage3ReadyToWrite) value.
    /// </summary>
    /// <param name="dirElemToDrop">The DirectoryElement to be removed from the DE3 collection.</param>
    /// <param name="frmMainAppInstance">An instance of the FrmMainApp form.</param>
    // ReSharper disable once InconsistentNaming
    private static void RemoveDirElementFromDe3AndCopyDataToOriginal(
        DirectoryElement dirElemToDrop,
        FrmMainApp frmMainAppInstance)
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
                            dirElemToDrop.GetAttributeValueString(
                                attribute: attribute,
                                version: DirectoryElement.AttributeVersion
                                                         .Stage3ReadyToWrite,
                                nowSavingExif: false),
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

        frmMainAppInstance.lvw_FileList.UpdateItemColour(
            itemText: dirElemToDrop.ItemNameWithoutPath, color: Color.Black);
    }
}