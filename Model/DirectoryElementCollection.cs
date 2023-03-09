using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ExifToolWrapper;
using NLog;
using static System.Environment;

namespace GeoTagNinja.Model;

public class DirectoryElementCollection : List<DirectoryElement>
{
    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    internal ExifTool _ExifTool;

    public ExifTool ExifTool
    {
        get => _ExifTool;
        set
        {
            if (_ExifTool != null)
            {
                _ExifTool.Dispose();
            }

            _ExifTool = value;
        }
    }

    /// <summary>
    ///     Searches through the list of directory elements for the element
    ///     with the given item name. Uses FileNameWithPath. If nothing is found, return null.
    /// </summary>
    /// <param name="FileNameWithPath">The file name to search for (w/ path)</param>
    public DirectoryElement FindElementByItemName(string FileNameWithPath)
    {
        foreach (DirectoryElement item in this)
        {
            if (item.FileNameWithPath == FileNameWithPath)
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    ///     Adds a DirectoryElement to this list. Hereby it is checked, whether
    ///     already an item with the same name exists. If this is the case,
    ///     either replace it with the one passed (replaceIfExists must be se to
    ///     true) or an ArgumentException is thrown.
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <param name="replaceIfExists">
    ///     Whether in case of already existing item the existing
    ///     item should be replace (or an exception thrown)
    /// </param>
    public void Add(DirectoryElement item,
                    bool replaceIfExists)
    {
        DirectoryElement exstgElement = FindElementByItemName(FileNameWithPath: item.FileNameWithPath);
        if (exstgElement != null)
        {
            if (replaceIfExists)
            {
                Remove(item: exstgElement);
            }
            else
            {
                throw new ArgumentException(
                    message: string.Format(format: "Error when adding element '{0}': the item must be unique but already exists in collection.",
                                           arg0: item.FileNameWithPath));
            }

            base.Add(item: item);
        }
    }

    /// <summary>
    ///     Parses the given folder into DirectoryElements.
    ///     The previous collection of directory elements is cleared before.
    ///     The statusMethod to be passed optionally accepts a string
    ///     containing a short status text.
    /// </summary>
    /// <param name="folder">The folder to parse</param>
    /// <param name="statusMethod">The method to call for status updates</param>
    public void ParseFolderToDEs(string folder,
                                 Action<string> statusMethod)
    {
        Logger.Trace(message: $"Start Parsing Folder '{folder}'");
        statusMethod(obj: "Scanning folder: Initializing ...");

        if (_ExifTool == null)
        {
            throw new InvalidOperationException(message: $"Cannot scan a folder (currently '{folder}') when the EXIF Tool was not set for the DirectoryElementCollection.");
        }

        // ******************************
        // Special Case is "MyComputer"...
        // Only list drives... then exit
        if (folder == SpecialFolder.MyComputer.ToString())
        {
            Logger.Trace(message: "Listing Drives");
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                Logger.Trace(message: "Drive:" + drive.Name);
                Add(item: new DirectoryElement(
                        itemNameWithoutPath: drive.Name,
                        type: DirectoryElement.ElementType.Drive,
                        fileNameWithPath: drive.RootDirectory.FullName
                    ));
            }

            Logger.Trace(message: "Listing Drives - OK");
            return;
        }

        // ******************************
        // first, add a parent folder. "dot dot"
        try
        {
            Logger.Trace(message: "Files: Adding Parent Folder");
            string tmpStrParent = HelperStatic.FsoGetParent(path: folder);
            if (tmpStrParent != null && tmpStrParent != SpecialFolder.MyComputer.ToString())
            {
                Add(item: new DirectoryElement(
                        itemNameWithoutPath: FrmMainApp.ParentFolder,
                        type: DirectoryElement.ElementType.ParentDirectory,
                        fileNameWithPath: tmpStrParent
                    ));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(message: $"Could not add parent. Error: {ex.Message}");
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_DirectoryElementCollection_ErrorParsing"),
                            caption: HelperStatic.GenericGetMessageBoxCaption(captionType: "Error"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Error);
        }

        // ******************************
        // list folders, ReparsePoint means these are links.
        statusMethod(obj: "Scanning folder: processing directories ...");
        Logger.Trace(message: "Listing Folders");
        List<string> dirs = new();
        try
        {
            DirectoryInfo di = new(path: folder);
            foreach (DirectoryInfo directoryInfo in di.GetDirectories())
            {
                if (directoryInfo.FullName == SpecialFolder.MyComputer.ToString())
                {
                    // It's the MyComputer entry
                    Logger.Trace(message: "MyComputer: " + directoryInfo.Name);
                    Add(item: new DirectoryElement(
                            itemNameWithoutPath: directoryInfo.Name,
                            type: DirectoryElement.ElementType.MyComputer,
                            fileNameWithPath: directoryInfo.FullName
                        ));
                }
                else if (directoryInfo.Attributes.ToString()
                             .Contains(value: "Directory") &&
                         !directoryInfo.Attributes.ToString()
                             .Contains(value: "ReparsePoint"))
                {
                    Logger.Trace(message: "Folder: " + directoryInfo.Name);
                    Add(item: new DirectoryElement(
                            itemNameWithoutPath: directoryInfo.Name,
                            type: DirectoryElement.ElementType.SubDirectory,
                            fileNameWithPath: directoryInfo.FullName
                        ));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_DirectoryElementCollection_ErrorParsing"),
                            caption: HelperStatic.GenericGetMessageBoxCaption(captionType: "Error"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Error);
        }

        Logger.Trace(message: "Listing Folders - OK");

        Logger.Trace(message: "Loading allowedExtensions");
        string[] allowedImageExtensions = AncillaryListsArrays.AllCompatibleExtensionsExt();
        string[] allowedSideCarExt = AncillaryListsArrays.GetSideCarExtensionsArray();
        Logger.Trace(message: "Loading allowedExtensions - OK");

        // ******************************
        // list files that have supported extensions
        // separate these into side car and image files
        statusMethod(obj: "Scanning folder: processing supported files ...");
        Logger.Trace(message: "Files: Listing Files");
        List<string> imageFiles = new();
        List<string> sidecarFiles = new();

        string[] filesInDir;
        try
        {
            filesInDir = Directory.GetFiles(path: folder);
        }
        catch (Exception ex)
        {
            Logger.Trace(message: "Files: Listing Files - Error: " + ex.Message);
            MessageBox.Show(text: ex.Message);
            return;
        }

        foreach (string fileNameWithExtension in filesInDir)
        {
            // Check, if it is a side car file. If so,
            // add it to the list to attach to image files later
            if (allowedSideCarExt.Contains(value: Path.GetExtension(path: fileNameWithExtension)
                                               .ToLower()
                                               .Replace(oldValue: ".", newValue: "")))
            {
                sidecarFiles.Add(item: fileNameWithExtension);
            }

            // Image file
            else if (allowedImageExtensions.Contains(value: Path.GetExtension(path: fileNameWithExtension)
                                                         .ToLower()
                                                         .Replace(oldValue: ".", newValue: "")))
            {
                imageFiles.Add(item: fileNameWithExtension);
            }
        }

        Logger.Trace(message: "Files: Listing Files - OK");

        // ******************************
        // Map side car files to image file
        IDictionary<string, string> image2sidecar = new Dictionary<string, string>();
        foreach (string sidecarFile in sidecarFiles)
        {
            // Get (by comparing w/o extension) list of matching image files in lower case
            string scFilenameWithoutExtension = Path.GetFileNameWithoutExtension(path: sidecarFile)
                .ToLower();
            List<string> matchingImageFiles = imageFiles
                .Where(predicate: imgFile => Path.GetFileNameWithoutExtension(path: imgFile)
                                                 .ToLower() ==
                                             scFilenameWithoutExtension)
                .ToList();
            if (matchingImageFiles.Count > 1)
            {
                Logger.Warn(message: $"Sidecar file '{sidecarFile}' matches multiple image files!");
            }

            foreach (string imgFile in matchingImageFiles)
            {
                image2sidecar[key: imgFile] = sidecarFile;
            }
        }

        // ******************************
        // Extract data for all files that are supported
        Logger.Trace(message: "Files: Extracting File Data");
        int fileCount = 0;
        if (_ExifTool == null)
        {
            _ExifTool = new ExifTool();
        }

        foreach (string fileNameWithPath in imageFiles)
        {
            Logger.Trace(message: $"File: {fileNameWithPath}");
            string fileNameWithoutPath = Path.GetFileName(path: fileNameWithPath);
            if (fileCount % 10 == 0)
            {
                statusMethod(obj: $"Scanning folder {100 * fileCount / imageFiles.Count:0}%: processing file '{fileNameWithoutPath}'");
            }

            // Regular (image) files are added to the list of
            // Directory Elements...
            DirectoryElement fileToParseDictionaryElement = new(
                itemNameWithoutPath: Path.GetFileName(path: fileNameWithoutPath),
                type: DirectoryElement.ElementType.File,
                fileNameWithPath: fileNameWithPath
            );

            // Parse EXIF properties
            IDictionary<string, string> dictProperties = new Dictionary<string, string>();
            InitiateEXIFParsing(fileNameWithPathToParse: fileNameWithPath, properties: dictProperties);

            // Add sidecar file and data if available
            if (image2sidecar.ContainsKey(key: fileNameWithPath))
            {
                string sideCarFileNameWithPath = image2sidecar[key: fileNameWithPath];
                Logger.Trace(message: $"Files: Extracting File Data - adding side car file '{sideCarFileNameWithPath}'");
                fileToParseDictionaryElement.SidecarFile = sideCarFileNameWithPath;
                InitiateEXIFParsing(fileNameWithPathToParse: sideCarFileNameWithPath, properties: dictProperties);
            }

            // Insert into model
            fileToParseDictionaryElement.ParseAttributesFromExifToolOutput(dictTagsIn: dictProperties);

            Add(item: fileToParseDictionaryElement);
            fileCount++;
        }

        Logger.Trace(message: "Files: Extracting File Data - OK");
    }

    /// <summary>
    ///     Parses the given file using the given EXIF Tool object into the given
    ///     dictionary. Thereby, ignoring duplicate tags.
    /// </summary>
    private void InitiateEXIFParsing(string fileNameWithPathToParse,
                                     IDictionary<string, string> properties)
    {
        // Gather EXIF data for the image file
        ICollection<KeyValuePair<string, string>> propertiesRead = new Dictionary<string, string>();
        _ExifTool.GetProperties(filename: fileNameWithPathToParse, propertiesRead: propertiesRead);
        // EXIF Tool can return duplicate properties - handle, but ignore these...
        foreach (KeyValuePair<string, string> kvp in propertiesRead)
        {
            if (!properties.ContainsKey(key: kvp.Key))
            {
                properties.Add(key: kvp.Key, value: kvp.Value);
            }
        }
    }
}