using NLog;
using System;
using System.Collections.Generic;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ExifToolWrapper;

namespace GeoTagNinja.Model;

public class DirectoryElementCollection : List<DirectoryElement>
{

    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    internal ExifTool _ExifTool = null;

    public ExifTool ExifTool {
        get { return _ExifTool; }
        set {
            if (_ExifTool != null)
                _ExifTool.Dispose();
            _ExifTool = value;
        }
    }

    /// <summary>
    ///     Searches through the list of directory elements for the element
    ///     with the given item name. If nothing is found, return null.
    /// </summary>
    /// <param name="itemName">The file name to search for</param>
    public DirectoryElement FindElementByItemName(string itemName)
    {
        foreach (DirectoryElement item in this)
        {
            if (item.ItemName == itemName)
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
        DirectoryElement exstgElement = FindElementByItemName(itemName: item.ItemName);
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
                                           arg0: item.ItemName));
            }

            base.Add(item: item);
        }
    }

    /// <summary>
    /// Parses the given folder into DirectoryElements.
    /// 
    /// The previous collection of directory elements is cleared before.
    /// The statusMethod to be passed optionally accepts a string
    /// containing a short status text.
    /// </summary>
    /// <param name="folder">The folder to parse</param>
    /// <param name="statusMethod">The method to call for status updates</param>
    public void ParseFolderToDEs(string folder, Action<string> statusMethod)
    {
        Logger.Trace(message: $"Start Parsing Folder '{folder}'");
        statusMethod("Scanning folder: Initializing ...");

        if (_ExifTool == null)
            throw new InvalidOperationException($"Cannot scan a folder (currently '{folder}') when the EXIF Tool was not set for the DirectoryElementCollection.");

        // ******************************
        // Special Case is "MyComputer"...
        // Only list drives... then exit
        if (folder == SpecialFolder.MyComputer.ToString())
        {
            Logger.Trace(message: "Listing Drives");
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                Logger.Trace(message: "Drive:" + drive.Name);
                this.Add(item: new DirectoryElement(
                                           itemName: drive.Name,
                                           type: DirectoryElement.ElementType.Drive,
                                           fullPathAndName: drive.RootDirectory.FullName
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
                this.Add(item: new DirectoryElement(
                                           itemName: FrmMainApp.ParentFolder,
                                           type: DirectoryElement.ElementType.ParentDirectory,
                                           fullPathAndName: tmpStrParent
                                       ));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(message: $"Could not add parent. Error: {ex.Message}");
            MessageBox.Show(text: ex.Message, caption: "Error while parsing folder to DEs");
        }

        // ******************************
        // list folders, ReparsePoint means these are links.
        statusMethod("Scanning folder: processing directories ...");
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
                    this.Add(item: new DirectoryElement(
                                               itemName: directoryInfo.Name,
                                               type: DirectoryElement.ElementType.MyComputer,
                                               fullPathAndName: directoryInfo.FullName
                                           ));
                }
                else if (directoryInfo.Attributes.ToString()
                        .Contains(value: "Directory") &&
                    !directoryInfo.Attributes.ToString()
                        .Contains(value: "ReparsePoint"))
                {
                    Logger.Trace(message: "Folder: " + directoryInfo.Name);
                    this.Add(item: new DirectoryElement(
                                               itemName: directoryInfo.Name,
                                               type: DirectoryElement.ElementType.SubDirectory,
                                               fullPathAndName: directoryInfo.FullName
                                           ));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(message: "Error: " + ex.Message);
            MessageBox.Show(text: ex.Message, caption: "Error while parsing folder to DEs");
        }
        Logger.Trace(message: "Listing Folders - OK");

        Logger.Trace(message: "Loading allowedExtensions");
        string[] allowedImageExtensions = AncillaryListsArrays.AllCompatibleExtensionsExt();
        string[] allowedSideCarExt = AncillaryListsArrays.GetSideCarExtensionsArray();
        Logger.Trace(message: "Loading allowedExtensions - OK");

        // ******************************
        // list files that have supported extensions
        // separate these into side car and image files
        statusMethod("Scanning folder: processing supported files ...");
        Logger.Trace(message: "Files: Listing Files");
        List<string> imageFiles = new();
        List<string> sidecarFiles = new List<string>();

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

        foreach (string filename in filesInDir)
        {
            // Check, if it is a side car file. If so,
            // add it to the list to attach to image files later
            if (allowedSideCarExt.Contains(Path.GetExtension(path: filename).ToLower().Replace(".", "")))
                sidecarFiles.Add(filename);

            // Image file
            else if (allowedImageExtensions.Contains(Path.GetExtension(path: filename).ToLower().Replace(".", "")))
                imageFiles.Add(filename);
        }
        Logger.Trace(message: "Files: Listing Files - OK");

        // ******************************
        // Map side car files to image file
        IDictionary<string,string> image2sidecar = new Dictionary<string,string>();
        foreach (string sidecarFile in sidecarFiles)
        {
            // Get (by comparing w/o extension) list of matching image files in lower case
            string scFilenameWOExt = Path.GetFileNameWithoutExtension(sidecarFile).ToLower();
            List<string> matchingImageFiles = imageFiles
                .Where(predicate: imgFile => Path.GetFileNameWithoutExtension(imgFile).ToLower() == scFilenameWOExt)
                .ToList();
            if (matchingImageFiles.Count>1)
                Logger.Warn(message: $"Sidecar file '{sidecarFile}' matches multiple image files!");
            foreach (string imgFile in matchingImageFiles)
                image2sidecar[imgFile] = sidecarFile;
        }

        // ******************************
        // Extract data for all files that are supported
        Logger.Trace(message: "Files: Extracting File Data");
        int count = 0;
        if (_ExifTool == null) _ExifTool = new ExifTool();
        foreach (string fileNameWithPath in imageFiles)
        {
            Logger.Trace(message: $"File: {fileNameWithPath}");
            string fileNameWithoutPath = Path.GetFileName(path: fileNameWithPath);
            if (count % 10 == 0)
                statusMethod($"Scanning folder {100*count/imageFiles.Count:0}%: processing file '{fileNameWithoutPath}'");

            // Regular (image) files are added to the list of
            // Directory Elements...
            DirectoryElement de = new DirectoryElement(
                                        itemName: Path.GetFileName(path: fileNameWithoutPath),
                                        type: DirectoryElement.ElementType.File,
                                        fullPathAndName: fileNameWithPath
                                    );

            // Parse EXIF Props
            IDictionary<string, string> props = new Dictionary<string, string>();
            InitiateEXIFParsing(fileNameWithPath, props);

            // Add sidecar file and data if available
            if (image2sidecar.ContainsKey(fileNameWithPath))
            {
                string scFile = image2sidecar[fileNameWithPath];
                Logger.Trace(message: $"Files: Extracting File Data - adding side car file '{scFile}'");
                de.SidecarFile = scFile;
                InitiateEXIFParsing(scFile, props);
            }

            // Insert into model
            de.ParseAttributesFromExifToolOutput(props);

            this.Add(item: de);
            count++;
        }
        Logger.Trace(message: "Files: Extracting File Data - OK");
    }

    /// <summary>
    /// Parses the given file using the given EXIF Tool object into the given
    /// dictionary. Thereby, ignoring duplicate tags.
    /// </summary>
    private void InitiateEXIFParsing(string fileToParse, IDictionary<string, string> props)
    {
        // Gather EXIF data for the image file
        ICollection<KeyValuePair<string, string>> propsRead = new List<KeyValuePair<string, string>>();
        _ExifTool.GetProperties(fileToParse, propsRead);
        // EXIF Tool can return duplicate properties - handle, but ignore these...
        foreach (KeyValuePair<string, string> kvp in propsRead)
            if (!props.ContainsKey(kvp.Key))
                props.Add(kvp.Key, kvp.Value);
    }


}