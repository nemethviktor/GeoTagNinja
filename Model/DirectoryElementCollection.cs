using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using Microsoft.WindowsAPICodePack.Taskbar;
using NLog;
using static System.Environment;
using HelperControlAndMessageBoxCustomMessageBoxManager =
    GeoTagNinja.Helpers.HelperControlAndMessageBoxCustomMessageBoxManager;

namespace GeoTagNinja.Model;

public class DirectoryElementCollection : List<DirectoryElement>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private ExifTool _ExifTool;
    private FrmPleaseWaitBox _frmPleaseWaitBoxInstance;
    private HelperNonStatic _helperNonStatic = new();

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
    ///     with the given item name. If nothing is found, return null.
    ///     Prepwork atm for allowing for multiple filenames across folders as these IDs are in fact unique.
    /// </summary>
    /// <param name="GUID"></param>
    /// <returns></returns>
    public DirectoryElement FindElementByItemGUID(string GUID)
    {
        foreach (DirectoryElement item in this)
        {
            if (item.GetAttributeValueString(attribute: SourcesAndAttributes.ElementAttribute.GUID,
                    nowSavingExif: false) ==
                GUID)
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    ///     Searches through the list of directory elements for the element
    ///     with the given item name. Uses FileNameWithPath. If nothing is found, return null.
    /// </summary>
    /// <param name="FileNameWithPath">The file name to search for (w/ path)</param>
    public DirectoryElement FindElementByFileNameWithPath(string FileNameWithPath)
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
    ///     Searches through all the DEs in this collection for elements
    ///     with dirty (to be saved) attributes.
    ///     TODO: THIS IS VERY INEFFICIENT. With larger element sets, we parse the whole
    ///     collection and for every element every attribute... Need a cache...?
    /// </summary>
    /// <returns>A HashSet of UIDs of dirty elements. Empty if there are none.</returns>
    public HashSet<string> FindDirtyElements()
    {
        HashSet<string> uids = new();
        foreach (DirectoryElement directoryElement in this)
        {
            if (directoryElement.HasDirtyAttributes())
            {
                uids.Add(
                    item: directoryElement.GetAttributeValueString(
                        attribute: SourcesAndAttributes.ElementAttribute.GUID, nowSavingExif: false));
            }
        }

        return uids;
    }


    /// <summary>
    ///     Attempts to find the first DE that matches an XMP file's logic. (ie.
    ///     20100504_Rome_01_Downtown_Colosseum__MG_2595.xmp --> 20100504_Rome_01_Downtown_Colosseum__MG_2595.CR2 assuming it
    ///     exists)
    /// </summary>
    /// <param name="XMPFileNameWithPath"></param>
    /// <returns></returns>
    public DirectoryElement FindElementByBelongingToXmpWithPath(string XMPFileNameWithPath)
    {
        foreach (DirectoryElement item in this)
        {
            if (item.FileNameWithPath.StartsWith(value: XMPFileNameWithPath))
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
    ///     item should be replaced (or an exception thrown)
    /// </param>
    private void Add(DirectoryElement item,
        bool replaceIfExists)
    {
        DirectoryElement exstgElement = FindElementByFileNameWithPath(FileNameWithPath: item.FileNameWithPath);
        if (exstgElement != null && replaceIfExists)
        {
            Remove(item: exstgElement);
            base.Add(item: item);
        }
        else if (exstgElement == null)
        {
            base.Add(item: item);
        }
        else
        {
            throw new ArgumentException(
                message: string.Format(
                    format:
                    "Error when adding element '{0}': the item must be unique but already exists in collection.",
                    arg0: item.FileNameWithPath));
        }
    }

    /// <summary>
    ///     Clears up the data stored in the 'stage' for the whole DE
    /// </summary>
    /// <param name="attributeVersion"></param>
    internal void CleanupAllDataInStage(DirectoryElement.AttributeVersion attributeVersion)
    {
        foreach (DirectoryElement item in this)
        {
            foreach (SourcesAndAttributes.ElementAttribute attribute in (SourcesAndAttributes.ElementAttribute[])
                     Enum.GetValues(enumType: typeof(SourcesAndAttributes.ElementAttribute)))
            {
                item.RemoveAttributeValue(
                    attribute: attribute,
                    version: attributeVersion);
            }
        }
    }

    /// <summary>
    ///     Parses the given folder (or list of files if in CollectionMode) into DirectoryElements.
    ///     The previous collection of directory elements is cleared before.
    ///     The updateProgressHandler to be passed optionally accepts a string
    ///     containing a short status text.
    /// </summary>
    /// <param name="folderOrCollectionFileName"></param>
    /// <param name="updateProgressHandler">The method to call for status updates</param>
    /// <param name="collectionModeEnabled"></param>
    /// <param name="processSubFolders"></param>
    /// <param name="cts"></param>
    public async Task ParseFolderOrFileListToDEsAsync(string folderOrCollectionFileName,
        Action<string> updateProgressHandler,
        bool collectionModeEnabled,
        bool processSubFolders,
        CancellationTokenSource cts)
    {
        Log.Trace(message: $"Start Parsing Folder '{folderOrCollectionFileName}'");
        updateProgressHandler(obj: "Scanning folder: Initializing ...");

        CancellationToken token = cts.Token;
        if (_ExifTool == null)
        {
            throw new InvalidOperationException(
                message:
                $"Cannot scan a folder (currently '{folderOrCollectionFileName}') when the EXIF Tool was not set for the DirectoryElementCollection.");
        }

    #region Normal Mode Non-Files

        if (!collectionModeEnabled)
        {
        #region MyComputer

            // ******************************
            // Special Case is "MyComputer"...
            // Only list drives... then exit
            if (folderOrCollectionFileName == SpecialFolder.MyComputer.ToString())
            {
                Log.Trace(message: "Listing Drives");
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    Log.Trace(message: $"Drive:{drive.Name}");
                    Add(item: new DirectoryElement(
                        itemNameWithoutPath: drive.Name,
                        type: DirectoryElement.ElementType.Drive,
                        fileNameWithPath: drive.RootDirectory.FullName
                    ), replaceIfExists: true);
                }

                CreateGUIDsForDirectoryElements();
                Log.Trace(message: "Listing Drives - OK");
                return;
            }

        #endregion

        #region DotDot

            // ******************************
            // first, add a parent folder. "dot dot"

            try
            {
                Log.Trace(message: "Files: Adding Parent Folder");
                string tmpStrParent = HelperFileSystemOperators.FsoGetParent(path: folderOrCollectionFileName);
                if (tmpStrParent != null &&
                    tmpStrParent != SpecialFolder.MyComputer.ToString())
                {
                    Add(item: new DirectoryElement(
                        itemNameWithoutPath: FrmMainApp.ParentFolder,
                        type: DirectoryElement.ElementType.ParentDirectory,
                        fileNameWithPath: tmpStrParent
                    ), replaceIfExists: true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(message: $"Could not add parent. Error: {ex.Message}");
                HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                    controlName: "mbx_DirectoryElementCollection_ErrorParsing",
                    captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Error,
                    buttons: MessageBoxButtons.OK);
            }

        #endregion

        #region Folders

            // ******************************
            // list folders, ReparsePoint means these are links.
            updateProgressHandler(obj: "Scanning folder: processing directories ...");
            Log.Trace(message: "Listing Folders");
            List<string> dirs = new();
            try
            {
                DirectoryInfo di = new(path: folderOrCollectionFileName);
                foreach (DirectoryInfo directoryInfo in di.GetDirectories())
                {
                    if (directoryInfo.FullName == SpecialFolder.MyComputer.ToString())
                    {
                        // It's the MyComputer entry
                        Log.Trace(message: $"MyComputer: {directoryInfo.Name}");
                        Add(item: new DirectoryElement(
                            itemNameWithoutPath: directoryInfo.Name,
                            type: DirectoryElement.ElementType.MyComputer,
                            fileNameWithPath: directoryInfo.FullName
                        ), replaceIfExists: true);
                    }
                    else if (directoryInfo.Attributes.ToString()
                                          .Contains(value: "Directory") &&
                             !directoryInfo.Attributes.ToString()
                                           .Contains(value: "ReparsePoint"))
                    {
                        Log.Trace(message: $"Folder: {directoryInfo.Name}");
                        Add(item: new DirectoryElement(
                            itemNameWithoutPath: directoryInfo.Name,
                            type: DirectoryElement.ElementType.SubDirectory,
                            fileNameWithPath: directoryInfo.FullName
                        ), replaceIfExists: true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(message: $"Error: {ex.Message}");
                HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                    controlName: "mbx_DirectoryElementCollection_ErrorParsing",
                    captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Error,
                    buttons: MessageBoxButtons.OK);
            }

            Log.Trace(message: "Listing Folders - OK");
        }

    #endregion

    #endregion

    #region Any Mode Files

        Log.Trace(message: "Loading allowedExtensions");
        string[] allowedImageExtensions = HelperGenericAncillaryListsArrays.AllCompatibleExtensionsExt();
        string[] allowedSidecarExtensions = HelperGenericAncillaryListsArrays.GetSideCarExtensionsArray();
        Log.Trace(message: "Loading allowedExtensions - OK");
        // ******************************
        // list files that have supported extensions
        // separate these into sidecar and image files
        updateProgressHandler(obj: "Scanning folder: processing supported files ...");
        Log.Trace(message: "Files: Listing Files");
        HashSet<FileInfo> imageFiles = _helperNonStatic.CreateHashSetWithComparer();
        HashSet<FileInfo> sidecarFiles = _helperNonStatic.CreateHashSetWithComparer();

        IEnumerable<FileInfo> filesInDir = null;
        int filesThatExistWithinCollection = 0;

    #region Collection Mode

        // if we're in collection-mode...
        if (collectionModeEnabled)
        {
            List<FileInfo> filesInCollection = new();
            foreach (string collectItemWithPath in File.ReadLines(path: Program.CollectionFileLocation))
            {
                if (File.Exists(path: collectItemWithPath))
                {
                    FileInfo fi = new(fileName: collectItemWithPath);
                    filesInCollection.Add(item: fi);
                    filesThatExistWithinCollection++;
                }
            }

            if (filesThatExistWithinCollection > 0)
            {
                filesInDir = filesInCollection;
            }
        }

    #endregion

    #region Normal Mode

        // if we're in normal mode or failed to gather any valid files...
        if (!collectionModeEnabled ||
            (collectionModeEnabled && filesThatExistWithinCollection == 0))
        {
            _frmPleaseWaitBoxInstance =
                (FrmPleaseWaitBox)Application.OpenForms[name: "FrmPleaseWaitBox"];

            if (_frmPleaseWaitBoxInstance != null)
            {
                _frmPleaseWaitBoxInstance.UpdateLabels(stage: FrmPleaseWaitBox.ActionStages.SCANNING);
            }

            try
            {
                await Task.Run(action: () =>
                {
                    filesInDir = _helperNonStatic.GetFiles(
                        folder: folderOrCollectionFileName,
                        filter: allowedImageExtensions.Concat(second: allowedSidecarExtensions).ToArray(),
                        recursive: processSubFolders,
                        updateProgressHandler: updateProgressHandler,
                        cancellationToken: token
                    ); // Force enumeration to ensure all files are processed
                });
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(value: "Operation canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(value: $"Error: {ex.Message}");
            }

            if (_frmPleaseWaitBoxInstance != null)
            {
                _frmPleaseWaitBoxInstance.UpdateLabels(stage: FrmPleaseWaitBox.ActionStages.PARSING);
            }
        }

        if (filesInDir != null)
        {
            foreach (FileInfo fileInfoItem in filesInDir)
            {
                // Check, if it is a sidecar file. If so,
                // add it to the list to attach to image files later
                if (allowedSidecarExtensions.Contains(
                        value: fileInfoItem.Extension.Replace(oldValue: ".", newValue: ""),
                        comparer: StringComparer.InvariantCultureIgnoreCase))
                {
                    sidecarFiles.Add(item: fileInfoItem);
                }

                // Image file
                else if (allowedImageExtensions.Contains(
                             value: fileInfoItem.Extension.Replace(oldValue: ".", newValue: ""),
                             comparer: StringComparer.InvariantCultureIgnoreCase))
                {
                    imageFiles.Add(item: fileInfoItem);

                    // collection mode throws a slight problem with sidecar files: 
                    // if an xmp (or other, currently undefined) file is not explicitly on the collection-mode-list then it gets ignored.
                    // so we check if there is an xmp file with the same name as the image here.

                    foreach (string sideCarExtension in allowedSidecarExtensions)
                    {
                        string imaginarySidecarFileNameWithPath =
                            $"{Path.GetFileNameWithoutExtension(path: fileInfoItem.FullName)}.{sideCarExtension}";
                        if (File.Exists(path: Path.Combine(path1: fileInfoItem.DirectoryName,
                                path2: imaginarySidecarFileNameWithPath)))
                        {
                            FileInfo fiSidecarFileInfoItem = new(fileName: Path.Combine(
                                path1: fileInfoItem.DirectoryName,
                                path2: imaginarySidecarFileNameWithPath));
                            sidecarFiles.Add(item: fiSidecarFileInfoItem);
                        }
                    }
                }
            }
        }

        Log.Trace(message: $"Files: Listing Files - OK, image file count: {imageFiles.Count}");

        // ******************************
        // Map sidecar files to image file
        IDictionary<FileInfo, FileInfo> imageToSidecarFileMapping = new Dictionary<FileInfo, FileInfo>();
        HashSet<FileInfo> overlappingXMPFileList = _helperNonStatic.CreateHashSetWithComparer();

        Log.Trace(message: $"Files: Checking sidecar files, count: {sidecarFiles.Count}");
        foreach (FileInfo sideCarFileInfoItem in sidecarFiles)
        {
            // Get (by comparing w/o extension +  folder) list of matching image files in lower case
            string scFilenameWithoutExtension = sideCarFileInfoItem.Name
                                                                   .Replace(oldValue: sideCarFileInfoItem.Extension,
                                                                        newValue: "").ToLower();
            string scFolder = sideCarFileInfoItem.DirectoryName;
            List<FileInfo> matchingImageFiles = imageFiles
                                               .Where(predicate: imgFile =>
                                                    imgFile.Name.Replace(oldValue: imgFile.Extension, newValue: "")
                                                           .ToLower() ==
                                                    scFilenameWithoutExtension && imgFile.DirectoryName == scFolder)
                                               .ToList();

            bool sidecarFileAlreadyAdded = false;
            foreach (FileInfo imagefileFileInfoItem in matchingImageFiles)
            {
                string imgFileExtension = imagefileFileInfoItem.Extension.TrimStart('.');

                // only add the sidecar file linkage if the particular extension is marked to use sidecars
                bool writeXMPSideCar = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                    dataTable: HelperVariables.DtHelperDataApplicationSettings,
                    settingTabPage: "tpg_FileOptions",
                    settingId: $"{imgFileExtension.ToLower()}_ckb_AddXMPSideCar"));
                if (writeXMPSideCar)
                {
                    if (sidecarFileAlreadyAdded)
                    {
                        overlappingXMPFileList.Add(item: sideCarFileInfoItem);
                        Log.Warn(message: $"Sidecar file '{sideCarFileInfoItem}' matches multiple image files!");
                    }

                    imageToSidecarFileMapping[key: imagefileFileInfoItem] = sideCarFileInfoItem;
                    sidecarFileAlreadyAdded = true;
                }
            }
        }

        // check if we have 1 xmp belonging to multiple images. 
        if (overlappingXMPFileList.Count > 0)
        {
            string overlappingXmpFileStr = "";
            foreach (FileInfo fi in overlappingXMPFileList)
            {
                overlappingXmpFileStr += fi.FullName + NewLine;
            }

            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_WarningMultipleImageFilesForXMP",
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Warning,
                buttons: MessageBoxButtons.OK, extraMessage: overlappingXmpFileStr);
        }

        // ******************************
        // Extract data for all files that are supported
        Log.Info(message: "Files: Extracting File Data");
        int fileCount = 0;
        _ExifTool ??= new ExifTool();

        if (_frmPleaseWaitBoxInstance != null)
        {
            _frmPleaseWaitBoxInstance.UpdateLabels(stage: FrmPleaseWaitBox.ActionStages
                                                                          .PARSING); // i'm pretty sure this is a duplicate
        }

        foreach (FileInfo imagefileFileInfoItem in imageFiles)
        {
            Log.Info(message: $"File: {imagefileFileInfoItem.FullName}");
            string fileNameWithoutPath = imagefileFileInfoItem.Name;
            if (_frmPleaseWaitBoxInstance != null)
            {
                Application.DoEvents();
                _frmPleaseWaitBoxInstance.lbl_PleaseWaitBoxMessage.Text = imagefileFileInfoItem.FullName;

                updateProgressHandler(
                    obj:
                    $"Scanning folder {100 * fileCount / imageFiles.Count:0}%: processing file '{fileNameWithoutPath}'");
            }

            // this is a bit complex but in _this_ loop we're not looking at sidecar files at _this_ stage whereas ...
            // I need to know if an xmp has changed or not
            bool fileNeedsReDEing = false;
            imageToSidecarFileMapping.TryGetValue(key: imagefileFileInfoItem, value: out FileInfo sidecarFileInfoItem);
            // so first we check the details of the image file then those of the xmp's
            for (int i = 0; i <= 1; i++)
            {
                string thisCheckSum = string.Empty;
                string storedChecksum = string.Empty;
                FileInfo fileNameWithPathToCheck = i == 0 ? imagefileFileInfoItem : sidecarFileInfoItem;

                // note to self: jpgs have no sidecars.
                if (fileNameWithPathToCheck != null &&
                    File.Exists(path: fileNameWithPathToCheck.FullName))
                {
                    if (HelperVariables.FileChecksumDictionary.TryGetValue(key: fileNameWithPathToCheck.FullName,
                            value: out string value))
                    {
                        storedChecksum = value;
                    }


                    // before anyone points out, yes we could store/check filesizes and match those two but the problem is that particularly
                    // xmp files can be modified within a single byte and then saved and have their datetime stamp changed to takendatetime
                    // so basically we could have two files that look identical on the surface but differ in content. 
                    // as such we do need to do the checksum test regardless of what the files appear like.

                    thisCheckSum =
                        HelperFileSystemGetChecksum.GetChecksum(fileNameWithPath: fileNameWithPathToCheck.FullName);

                    fileNeedsReDEing = fileNeedsReDEing || storedChecksum != thisCheckSum ||
                                       !HelperVariables.FileChecksumDictionary.ContainsKey(
                                           key: fileNameWithPathToCheck.FullName);
                }

                if (fileNeedsReDEing && !string.IsNullOrWhiteSpace(value: thisCheckSum))
                {
                    // update HelperVariables.fileChecksumhDictionary
                    HelperVariables.FileChecksumDictionary[key: fileNameWithPathToCheck.FullName] = thisCheckSum;
                }
            }


            if (fileNeedsReDEing)
            {
                // delete from DE-collection (xmp files don't have a DE entry)
                DirectoryElement? directoryElementToRemove =
                    FindElementByFileNameWithPath(FileNameWithPath: imagefileFileInfoItem.FullName);
                if (directoryElementToRemove is not null)
                {
                    Remove(item: directoryElementToRemove);
                }

                // Regular (image) files are added to the list of
                // Directory Elements...
                DirectoryElement fileToParseDictionaryElement = new(
                    itemNameWithoutPath: imagefileFileInfoItem.Name,
                    type: DirectoryElement.ElementType.File,
                    fileNameWithPath: imagefileFileInfoItem.FullName
                );

                // Add sidecar file and data if available
                IDictionary<string, string> dictProperties = new Dictionary<string, string>();
                if (sidecarFileInfoItem != null &&
                    File.Exists(path: sidecarFileInfoItem.FullName))
                {
                    Log.Info(
                        message: $"Files: Extracting File Data - adding sidecar file '{sidecarFileInfoItem}'");
                    fileToParseDictionaryElement.SidecarFile = sidecarFileInfoItem;
                    // Logically XMP should take priority because RAW files are not meant to be edited.
                    InitiateEXIFParsing(fileinfoItem: sidecarFileInfoItem, properties: dictProperties);
                }


                // Parse EXIF properties for the image.
                InitiateEXIFParsing(fileinfoItem: imagefileFileInfoItem, properties: dictProperties);

                // Insert into model
                fileToParseDictionaryElement.ParseAttributesFromExifToolOutput(dictTagsIn: dictProperties);

                Add(item: fileToParseDictionaryElement, replaceIfExists: true);
            }

            fileCount++;
            FrmMainApp.TaskbarManagerInstance.SetProgressValue(currentValue: fileCount, maximumValue: imageFiles.Count);
            Thread.Sleep(millisecondsTimeout: 1);
        }

        CreateGUIDsForDirectoryElements();

        FrmMainApp.TaskbarManagerInstance.SetProgressState(state: TaskbarProgressBarState.NoProgress);

        Log.Info(message: "Files: Extracting File Data - OK");

    #endregion

    #endregion
    }

    /// <summary>
    ///     Assigns a GUID for the DEs in the DECollection
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static void CreateGUIDsForDirectoryElements()
    {
        foreach (DirectoryElement directoryElement in FrmMainApp.DirectoryElements)
        {
            directoryElement.SetAttributeValue(attribute: SourcesAndAttributes.ElementAttribute.GUID, value: Guid
               .NewGuid()
               .ToString(), version: DirectoryElement.AttributeVersion.Original, isMarkedForDeletion: false);
        }
    }

    /// <summary>
    ///     Parses the given file using the given EXIF Tool object into the given
    ///     dictionary. Thereby, ignoring duplicate tags.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private void InitiateEXIFParsing(FileInfo fileinfoItem,
        IDictionary<string, string> properties)
    {
        // Gather EXIF data for the image file
        ICollection<KeyValuePair<string, string>> propertiesRead = new Dictionary<string, string>();
        _ExifTool.GetProperties(filename: fileinfoItem.FullName, propertiesRead: propertiesRead);

        // EXIF Tool can return duplicate properties - handle, but ignore these...
        foreach (KeyValuePair<string, string> kvp in propertiesRead)
        {
            if (!properties.ContainsKey(key: kvp.Key))
            {
                properties.Add(key: kvp.Key, value: kvp.Value);
            }
        }

        // force-derive Refs here because they can disagree between RAW and XMP files.
        if (fileinfoItem.Extension.EndsWith(value: ".xmp"))
        {
            // "EXIF" takes prio over "Composite"
            string tmpLatLongValStr;
            string tmpLatLongRefValStr = "";
            double tmpLonLongValDbl = 0.0;
            if (properties.ContainsKey(key: "XMP:GPSLongitude") &&
                !properties.ContainsKey(key: "EXIF:GPSLongitudeRef"))
            {
                tmpLatLongValStr = (from x in propertiesRead
                    where x.Key == "XMP:GPSLongitude"
                    select x.Value).FirstOrDefault();
                tmpLonLongValDbl = HelperExifDataPointInteractions.AdjustLatLongNegative(point: tmpLatLongValStr);
                tmpLatLongRefValStr = tmpLonLongValDbl < 0.0
                    ? "West"
                    : "East";

                properties.Add(key: "EXIF:GPSLongitudeRef", value: tmpLatLongRefValStr);
            }

            if (properties.ContainsKey(key: "XMP:GPSLatitude") &&
                !properties.ContainsKey(key: "EXIF:GPSLatitudeRef"))
            {
                tmpLatLongValStr = (from x in propertiesRead
                    where x.Key == "XMP:GPSLatitude"
                    select x.Value).FirstOrDefault();
                tmpLonLongValDbl = HelperExifDataPointInteractions.AdjustLatLongNegative(point: tmpLatLongValStr);
                tmpLatLongRefValStr = tmpLonLongValDbl < 0.0
                    ? "South"
                    : "North";

                properties.Add(key: "EXIF:GPSLatitudeRef", value: tmpLatLongRefValStr);
            }
        }
    }
}