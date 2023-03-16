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
    /// <returns>Empty Task</returns>
    internal static async Task RunExifTool(string exiftoolCmd,
                                           FrmMainApp frmMainAppInstance,
                                           string initiator)
    {
        int lviIndex = 0;
        _exifInvokeCounter += 1;
        FrmMainApp.Logger.Trace(message: "Start EXIF Tool number " + _exifInvokeCounter + " for " + initiator + " with cmdLine: " + exiftoolCmd);
        await Task.Run(action: () =>
        {
            using Process prcExifTool = new();

            prcExifTool.StartInfo = new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
            {
                Arguments = @"/c " + HelperVariables.SDoubleQuote + HelperVariables.SDoubleQuote + Path.Combine(path1: HelperVariables.ResourcesFolderPath, path2: "exiftool.exe") + HelperVariables.SDoubleQuote + " " + exiftoolCmd + HelperVariables.SDoubleQuote,
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
                                fileNameWithPath = data.Data.Replace(oldValue: "=", newValue: "")
                                    .Replace(oldValue: "/", newValue: @"\")
                                    .Split('[')
                                    .FirstOrDefault()
                                    .Trim();

                                dirElemFileToDrop = FrmMainApp.DirectoryElements.FindElementByFileNameWithPath(FileNameWithPath: fileNameWithPath);
                            }

                            if (dirElemFileToDrop != null)
                            {
                                fileNameWithoutPath = dirElemFileToDrop.ItemNameWithoutPath;
                                FrmMainApp.HandlerUpdateLabelText(label: frmMainAppInstance.lbl_ParseProgress, text: "Processing: " + fileNameWithoutPath);
                                FrmMainApp.Logger.Debug(message: "Writing " + fileNameWithoutPath + " [this is via OutputDataReceived]");

                                try
                                {
                                    if (lviIndex % 10 == 0)
                                    {
                                        Application.DoEvents();

                                        // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                        frmMainAppInstance.lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath);
                                    }

                                    if ((data.Data.Contains(value: "files updated") || data.Data.Contains(value: "files created")) &&
                                        !data.Data.Trim()
                                            .StartsWith(value: "0"))
                                    {
                                        removeDirElementFromDE3(dirElemToDrop: dirElemFileToDrop);
                                    }

                                    if (Path.GetExtension(path: fileNameWithoutPath) == ".xmp")
                                    {
                                        // problem is that if only the xmp file gets overwritten then there is no indication of the original file here. 
                                        // FindItemWithText -> https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.listview.finditemwithtext?view=netframework-4.8
                                        // "Finds the first ListViewItem with __that begins with__ the given text value."

                                        if (lviIndex % 10 == 0)
                                        {
                                            Application.DoEvents();
                                            // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                                            frmMainAppInstance.lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath); // this is redundant here.
                                        }

                                        if ((data.Data.Contains(value: "files updated") || data.Data.Contains(value: "files created")) &&
                                            !data.Data.Trim()
                                                .StartsWith(value: "0"))
                                        {
                                            removeDirElementFromDE3(dirElemToDrop: dirElemFileToDrop);
                                        }
                                    }
                                }
                                catch
                                {
                                    // ignored
                                }

                                lviIndex++;

                                if (!data.Data.Contains(value: "files updated") && !data.Data.Contains(value: "files created") && !data.Data.Contains(value: fileNameWithoutPath))
                                {
                                    MessageBox.Show(text: data.Data);
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
                        if (data.Data != null && data.Data.Length > 0)
                        {
                            HelperVariables._sOutputMsg += data.Data.ToString() + Environment.NewLine;
                        }

                        decimal.TryParse(s: HelperVariables._sOutputMsg.Replace(oldValue: "\r", newValue: "")
                                             .Replace(oldValue: "\n", newValue: ""),
                                         provider: CultureInfo.InvariantCulture,
                                         style: NumberStyles.Any,
                                         result: out HelperVariables._currentExifToolVersionLocal
                        );
                    };

                    break;
                case "ExifGetTrackSyncData":
                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (data.Data != null && data.Data.Length > 0)
                        {
                            HelperVariables._sOutputMsg += data.Data.ToString() + Environment.NewLine;
                        }
                    };
                    break;
                default:
                    prcExifTool.OutputDataReceived += (_,
                                                       data) =>
                    {
                        if (data.Data != null && data.Data.Length > 0)
                        {
                            HelperVariables._sOutputMsg += data.Data.ToString() + Environment.NewLine;
                        }
                    };

                    prcExifTool.ErrorDataReceived += (_,
                                                      data) =>
                    {
                        if (data.Data != null && data.Data.Length > 0)
                        {
                            HelperVariables._sOutputMsg += "ERROR: " + data.Data.ToString() + Environment.NewLine;
                        }
                    };
                    break;
            }

            FrmMainApp.Logger.Trace(message: "EXIF number " + _exifInvokeCounter + ": Start");
            prcExifTool.Start();
            prcExifTool.BeginOutputReadLine();
            prcExifTool.BeginErrorReadLine();
            FrmMainApp.Logger.Trace(message: "EXIF number " + _exifInvokeCounter + ": Wait for Exit");
            prcExifTool.WaitForExit();
            FrmMainApp.Logger.Trace(message: "EXIF number " + _exifInvokeCounter + ": Close");
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

        void removeDirElementFromDE3(DirectoryElement dirElemToDrop)
        {
            string fileNameExtension = dirElemToDrop.Extension;
            if (!fileNameExtension.EndsWith(value: ".xmp"))
            {
                foreach (ElementAttribute attribute in (ElementAttribute[])Enum.GetValues(enumType: typeof(ElementAttribute)))
                {
                    dirElemToDrop.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                }
            }

            frmMainAppInstance.lvw_FileList.UpdateItemColour(itemText: dirElemToDrop.ItemNameWithoutPath, color: Color.Black);
        }
    }
}