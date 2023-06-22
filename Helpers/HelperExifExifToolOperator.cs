using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GeoTagNinja.Model;
using NLog;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers;

internal static class HelperExifExifToolOperator
{
    private static int _exifInvokeCounter;

    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The differentiated callers of the RunExifTool method
    /// </summary>
    public enum INITIATOR
    {
        WRITE_2_FILE,           // = "ExifWriteExifToFile",
        CHECK_4_NEW_VERSION,    // = "GenericCheckForNewVersions"
        GET_TRACK_SYNC_DATA,    // = "ExifGetTrackSyncData"
        GET_IMAGE_PREVIEWS      // = "ExifGetImagePreviews"
    };
    
    /// <summary>
    /// Handles process output while saving file changes
    /// </summary>
    private class exifOutputHandler_SaveChanges
    {

        // Session duration
        private DirectoryElementCollection dirElemCollection;
        private FrmMainApp frmMainAppInstance;
        private Label statusLabel;
        private string errorMesg = "";

        // Info kept in between calls of handle_line
        string fileNameWithPath;
        DirectoryElement dirElemFileToDrop = null;
        string fileNameWithoutPath;
        int lviIndex = 0;
        string prevLine = "";

        public exifOutputHandler_SaveChanges(
            DirectoryElementCollection dirElemCollection,
            FrmMainApp frmMainAppInstance,
            Label statusLabel
            )
        {
            this.dirElemCollection = dirElemCollection;
            this.frmMainAppInstance = frmMainAppInstance;
            this.statusLabel = statusLabel;
        }


        /// <summary>
        /// Is called when a new line of stdout while saving changes is passed
        /// 
        /// Updates are passed in line by line. The first line contains the
        /// image name, the second the number of files processed (should be 1).
        /// Thus, the image ref (from the first line) is kept in variable
        /// dirElemFileToDrop and then used during the second call.
        /// </summary>
        public void handle_line_stdout(string readLine,
                    bool processOriginalFile,
                    bool writeXmpSideCar)
        {
            // First line contains multiple "=" and the file name
            if (readLine.Contains(value: "="))
            {
                fileNameWithPath = readLine.Replace(oldValue: "=", newValue: "")
                    .Replace(oldValue: "/", newValue: @"\")
                    .Split('[')
                    .FirstOrDefault()
                    .Trim();

                // basically we need to check that the combination of what-to-save is _not_ xmp-only. 
                // if it _is_ xmp-only then this causes a problem because the xmp file isn't really a DE per se so it'd never get removed from the queue.
                // problem is that if only the xmp file gets overwritten then there is no indication of the original file here. 

                if (!processOriginalFile && writeXmpSideCar && fileNameWithPath.EndsWith(value: ".xmp"))
                {
                    string pathOfFile = fileNameWithPath.Substring(startIndex: 0, length: fileNameWithPath.Length - 4);
                    dirElemFileToDrop = dirElemCollection.FindElementByBelongingToXmpWithPath(XMPFileNameWithPath: pathOfFile);
                }
                else
                {
                    dirElemFileToDrop = dirElemCollection.FindElementByFileNameWithPath(FileNameWithPath: fileNameWithPath);
                }
            }

            // Second line is called after first line, thus dirElemFileToDrop is set
            if (dirElemFileToDrop != null)
            {
                fileNameWithoutPath = dirElemFileToDrop.ItemNameWithoutPath;
                FrmMainApp.HandlerUpdateLabelText(label: statusLabel, text: "Processing: " + fileNameWithoutPath);
                Logger.Debug(message: "Writing " + fileNameWithoutPath + " [this is via OutputDataReceived]");

                try
                {
                    if (lviIndex % 10 == 0)
                    {
                        Application.DoEvents();
                        frmMainAppInstance.lvw_FileList.ScrollToDataPoint(itemText: fileNameWithoutPath);
                    }

                    if ((readLine.Contains(value: "files updated") || readLine.Contains(value: "files created")) &&
                        !readLine.Trim()
                            .StartsWith(value: "0"))
                    {
                        removeDirElementFromDE3(dirElemToDrop: dirElemFileToDrop);
                        if (!processOriginalFile && writeXmpSideCar)
                        {
                            string pathOfFile = fileNameWithPath.Substring(startIndex: 0, length: fileNameWithPath.Length - 4);
                            dirElemFileToDrop = dirElemCollection.FindElementByBelongingToXmpWithPath(XMPFileNameWithPath: pathOfFile);
                            removeDirElementFromDE3(dirElemToDrop: dirElemFileToDrop);
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                lviIndex++;

                if (    !readLine.Contains(value: "files updated") &&
                        !readLine.Contains(value: "files created") &&
                        !readLine.Contains(value: fileNameWithoutPath.Substring(startIndex: 0, length: fileNameWithoutPath.LastIndexOf(value: '.'))))
                {
                    errorMesg += "ERROR: " + readLine + Environment.NewLine;
                }
            }

            prevLine = readLine;
        }

        public string getErrorMsg() { return errorMesg; }

        private void removeDirElementFromDE3(DirectoryElement dirElemToDrop)
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


    /// <summary>
    /// This is a unified method for calling ExifTool. Any special "dealings" are done inside the message handling.
    /// (possibly the wrong spot to do it)
    ///     
    /// This method performs a long-running task - invoke it in some task form if wished for.
    /// 
    /// The method runs synchronous and waits for the exit of the called exif tool.
    /// This method - depending on the initiator - performs UI updates. If the UI thread is blocked,
    /// it will get deadlocked. In order to allow UI updates, it frequently calls Application.DoEvents().
    /// 
    /// This method will not set a "ready message" at the end.
    /// </summary>
    /// <param name="exiftoolCmd">Variables that need to be passed to ET</param>
    /// <param name="frmMainAppInstance">
    ///     Only relevant when called from ExifWriteExifToFile -> used to update the values/colour
    ///     in the main listView
    /// </param>
    /// <param name="initiator">Caller out of the INITIATOR enum.</param>
    /// <param name="processOriginalFile1"></param>
    /// <param name="writeXmpSideCar"></param>
    internal static void RunExifTool(string exiftoolCmd,
                                           FrmMainApp frmMainAppInstance,
                                           INITIATOR initiator,
                                           bool processOriginalFile = false,
                                           bool writeXmpSideCar = false)
    {
        _exifInvokeCounter += 1;
        Logger.Trace(message: "Start EXIF Tool number " + _exifInvokeCounter + " for " + initiator + " with cmdLine: " + exiftoolCmd);

        using Process prcExifTool = new();

        prcExifTool.StartInfo = new ProcessStartInfo(fileName: @"c:\windows\system32\cmd.exe")
        {
            Arguments = @"/c " + HelperVariables.SDoubleQuote + HelperVariables.SDoubleQuote +
                Path.Combine(path1: HelperVariables.ResourcesFolderPath, path2: "exiftool.exe") +
                HelperVariables.SDoubleQuote + " " + exiftoolCmd + HelperVariables.SDoubleQuote,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        /* Pitfall when using process with synchronous output redirection, cf.
         * https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?redirectedfrom=MSDN&view=net-7.0#System_Diagnostics_Process_StandardOutput
         * https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
         * https://stackoverflow.com/questions/26713373/process-waitforexit-doesnt-return-even-though-process-hasexited-is-true
         * 
         * With async (handlers), risk of running into deadlock when synchronously
         * waiting (in the UI thread) for the exif process and handling the async output
         * data event with Invoke to update an UI object...
         */

        // The delegate method called with read lines from stdout / stderr
        Action<string> stdout_delegate = null;
        Action<string> stderr_delegate = null;
        // Collected error message for WRITE_2_FILE
        string collectedErrorMessage = "";
        // More complex output handler for WRITE_2_FILE
        exifOutputHandler_SaveChanges oh = null;

        switch (initiator)
        {
            case INITIATOR.WRITE_2_FILE:
                oh = new exifOutputHandler_SaveChanges(
                        dirElemCollection: FrmMainApp.DirectoryElements,
                        frmMainAppInstance: frmMainAppInstance,
                        statusLabel: frmMainAppInstance.lbl_ParseProgress
                    );
                stdout_delegate = delegate (string readLine)
                {
                    oh.handle_line_stdout(readLine, processOriginalFile, writeXmpSideCar);
                };

                stderr_delegate = delegate (string readLine)
                {
                    if (!string.IsNullOrEmpty(value: readLine))
                        collectedErrorMessage += "ERROR: " + readLine.ToString() + Environment.NewLine;
                };
                break;

            case INITIATOR.CHECK_4_NEW_VERSION:
                stdout_delegate = delegate (string readLine)
                {
                    if (!string.IsNullOrEmpty(value: readLine))
                    {
                        HelperVariables._sOutputMsg += readLine.ToString() + Environment.NewLine;
                    }

                    decimal.TryParse(s: HelperVariables._sOutputMsg.Replace(oldValue: "\r", newValue: "")
                                            .Replace(oldValue: "\n", newValue: ""),
                                        provider: CultureInfo.InvariantCulture,
                                        style: NumberStyles.Any,
                                        result: out HelperVariables._currentExifToolVersionLocal
                    );
                };
                break;

            case INITIATOR.GET_TRACK_SYNC_DATA:
            default:
                stdout_delegate = delegate (string readLine)
                {
                    if (!string.IsNullOrEmpty(value: readLine))
                    {
                        HelperVariables._sOutputMsg += readLine.ToString() + Environment.NewLine;
                    }
                };

                stderr_delegate = delegate (string readLine)
                {
                    if (!string.IsNullOrEmpty(value: readLine))
                        HelperVariables._sOutputMsg += "ERROR: " + readLine.ToString() + Environment.NewLine;
                };
                break;
        }

        // As we let the tool run async - ensure that resources are disposed after
        // exiting...

        Logger.Trace(message: "EXIF number " + _exifInvokeCounter + ": Start");
        prcExifTool.Start();

        string readLine;
        while ((!prcExifTool.StandardOutput.EndOfStream) |
            (!prcExifTool.StandardError.EndOfStream))
        {
            if (!prcExifTool.StandardOutput.EndOfStream)
            {
                readLine = prcExifTool.StandardOutput.ReadLine();
                // In case we have an output, call appr. method....
                if (!string.IsNullOrEmpty(value: readLine))
                    stdout_delegate(readLine);
            }

            if (!prcExifTool.StandardError.EndOfStream)
            {
                readLine = prcExifTool.StandardError.ReadLine();
                // In case we have an output, call appr. method....
                if (!string.IsNullOrEmpty(value: readLine))
                    stderr_delegate(readLine);
            }

            // Ensure to process UI Updates, etc.
            Application.DoEvents();
        }

        // Exiting / dispose
        Logger.Trace(message: "EXIF number " + _exifInvokeCounter + ": Close");
        prcExifTool.Close();
        Logger.Trace(message: "Closed exifTool");

        if (oh != null)
            if (!string.IsNullOrEmpty(oh.getErrorMsg()))
                collectedErrorMessage += "--- via stdout ---" + Environment.NewLine + oh.getErrorMsg();
        if (!string.IsNullOrEmpty(value: collectedErrorMessage))
            MessageBox.Show(text: collectedErrorMessage);
    }
}