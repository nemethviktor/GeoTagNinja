﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using GeoTagNinja.View.DialogAndMessageBoxes;
using Microsoft.WindowsAPICodePack.Taskbar;
using NLog;
using static GeoTagNinja.Model.SourcesAndAttributes;
using static System.Environment;

namespace GeoTagNinja.Model;

public class DirectoryElementCollection : List<DirectoryElement>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private ExifTool _ExifTool;

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
            if (item.GetAttributeValueString(attribute: ElementAttribute.GUID,
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
                        attribute: ElementAttribute.GUID, nowSavingExif: false));
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
    ///     Parses the given folder (or list of files if in CollectionMode) into DirectoryElements.
    ///     The previous collection of directory elements is cleared before.
    ///     The statusMethod to be passed optionally accepts a string
    ///     containing a short status text.
    /// </summary>
    /// <param name="folderOrCollectionFileName"></param>
    /// <param name="statusMethod">The method to call for status updates</param>
    /// <param name="collectionModeEnabled"></param>
    public void ParseFolderOrFileListToDEs(string folderOrCollectionFileName,
        Action<string> statusMethod,
        bool collectionModeEnabled)
    {
        Log.Trace(message: $"Start Parsing Folder '{folderOrCollectionFileName}'");
        statusMethod(obj: "Scanning folder: Initializing ...");

        if (_ExifTool == null)
        {
            throw new InvalidOperationException(
                message:
                $"Cannot scan a folder (currently '{folderOrCollectionFileName}') when the EXIF Tool was not set for the DirectoryElementCollection.");
        }

        if (!collectionModeEnabled)
        {
            // ******************************
            // Special Case is "MyComputer"...
            // Only list drives... then exit
            if (folderOrCollectionFileName == SpecialFolder.MyComputer.ToString())
            {
                Log.Trace(message: "Listing Drives");
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    Log.Trace(message: "Drive:" + drive.Name);
                    Add(item: new DirectoryElement(
                        itemNameWithoutPath: drive.Name,
                        type: DirectoryElement.ElementType.Drive,
                        fileNameWithPath: drive.RootDirectory.FullName
                    ), replaceIfExists: true);
                }

                CreateGuiDsForDirectoryElements();
                Log.Trace(message: "Listing Drives - OK");
                return;
            }

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
                CustomMessageBox customMessageBox = new(
                    text: HelperControlAndMessageBoxHandling.ReturnControlText(
                        controlName: "mbx_DirectoryElementCollection_ErrorParsing",
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                    caption: HelperControlAndMessageBoxHandling
                       .ReturnControlText(
                            controlName: HelperControlAndMessageBoxHandling.MessageBoxCaption.Error.ToString(),
                            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBoxCaption),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error);
                customMessageBox.ShowDialog();
            }

            // ******************************
            // list folders, ReparsePoint means these are links.
            statusMethod(obj: "Scanning folder: processing directories ...");
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
                        Log.Trace(message: "MyComputer: " + directoryInfo.Name);
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
                        Log.Trace(message: "Folder: " + directoryInfo.Name);
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
                Log.Error(message: "Error: " + ex.Message);
                CustomMessageBox customMessageBox = new(
                    text: HelperControlAndMessageBoxHandling.ReturnControlText(
                        controlName: "mbx_DirectoryElementCollection_ErrorParsing",
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                    caption: HelperControlAndMessageBoxHandling
                       .ReturnControlText(
                            controlName: HelperControlAndMessageBoxHandling.MessageBoxCaption.Error.ToString(),
                            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBoxCaption),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error);
                customMessageBox.ShowDialog();
            }

            Log.Trace(message: "Listing Folders - OK");
        }

        Log.Trace(message: "Loading allowedExtensions");
        string[] allowedImageExtensions = HelperGenericAncillaryListsArrays.AllCompatibleExtensionsExt();
        string[] allowedSideCarExtensions = HelperGenericAncillaryListsArrays.GetSideCarExtensionsArray();
        Log.Trace(message: "Loading allowedExtensions - OK");

        // ******************************
        // list files that have supported extensions
        // separate these into side car and image files
        statusMethod(obj: "Scanning folder: processing supported files ...");
        Log.Trace(message: "Files: Listing Files");
        HashSet<string> imageFiles = new();
        HashSet<string> sidecarFiles = new();

        string[] filesInDir = { };
        int filesThatExistWithinCollection = 0;

        // if we're in collection-mode...
        if (collectionModeEnabled)
        {
            List<string> filesInCollection = new();
            foreach (string collectItemWithPath in File.ReadLines(path: Program.collectionFileLocation))
            {
                if (File.Exists(path: collectItemWithPath))
                {
                    filesInCollection.Add(item: collectItemWithPath);
                    filesThatExistWithinCollection++;
                }
            }

            if (filesThatExistWithinCollection > 0)
            {
                filesInDir = filesInCollection.ToArray();
            }
        }

        // if we're in normal mode or failed to gather any valid files...
        if (!collectionModeEnabled ||
            (collectionModeEnabled && filesThatExistWithinCollection == 0))
        {
            try
            {
                filesInDir = Directory.GetFiles(path: folderOrCollectionFileName);
            }
            catch (Exception ex)
            {
                Log.Trace(message: "Files: Listing Files - Error: " + ex.Message);
                MessageBox.Show(text: ex.Message);
                return;
            }
        }

        foreach (string fileNameWithExtension in filesInDir)
        {
            // Check, if it is a side car file. If so,
            // add it to the list to attach to image files later
            if (allowedSideCarExtensions.Contains(value: Path.GetExtension(path: fileNameWithExtension)
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

                // collection mode throws a slight problem with sidecar files: 
                // if an xmp (or other, currently undefined) file is not explicitly on the collection-mode-list then it gets ignored.
                // so we check if there is an xmp file with the same name as the image here.
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path: fileNameWithExtension);
                foreach (string sideCarExtension in allowedSideCarExtensions)
                {
                    string imaginaryFileNameWithPath = fileNameWithoutExtension + "." + sideCarExtension;
                    if (File.Exists(path: Path.Combine(path1: Path.GetDirectoryName(path: fileNameWithExtension),
                            path2: imaginaryFileNameWithPath)))
                    {
                        sidecarFiles.Add(item: Path.Combine(path1: Path.GetDirectoryName(path: fileNameWithExtension),
                            path2: imaginaryFileNameWithPath));
                    }
                }
            }
        }

        Log.Trace(message: "Files: Listing Files - OK, image file count: " + imageFiles.Count);

        // ******************************
        // Map side car files to image file
        IDictionary<string, string> image2sidecar = new Dictionary<string, string>();
        HashSet<string> overlappingXmpFileList = new();

        Log.Trace(message: "Files: Checking sidecar files, count: " + sidecarFiles.Count);
        foreach (string sidecarFile in sidecarFiles)
        {
            // Get (by comparing w/o extension) list of matching image files in lower case
            string scFilenameWithoutExtension = Path.GetFileNameWithoutExtension(path: sidecarFile)
                                                    .ToLower();
            List<string> matchingImageFiles = imageFiles
                                             .Where(predicate: imgFile =>
                                                  Path.GetFileNameWithoutExtension(path: imgFile)
                                                      .ToLower() ==
                                                  scFilenameWithoutExtension)
                                             .ToList();

            bool sidecarFileAlreadyAdded = false;
            foreach (string imgFile in matchingImageFiles)
            {
                string imgFileExtension = Path.GetExtension(path: imgFile)
                                              .Substring(startIndex: 1);

                // only add the sidecar file linkage if the particular extension is marked to use sidecars
                bool writeXMPSideCar = Convert.ToBoolean(value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                    dataTable: HelperVariables.DtHelperDataApplicationSettings,
                    settingTabPage: "tpg_FileOptions",
                    settingId: imgFileExtension.ToLower() +
                               "_" +
                               "ckb_AddXMPSideCar"));
                if (writeXMPSideCar)
                {
                    if (sidecarFileAlreadyAdded)
                    {
                        overlappingXmpFileList.Add(item: sidecarFile);
                        Log.Warn(message: $"Sidecar file '{sidecarFile}' matches multiple image files!");
                    }

                    image2sidecar[key: imgFile] = sidecarFile;
                    sidecarFileAlreadyAdded = true;
                }
            }
        }

        if (overlappingXmpFileList.Count > 0)
        {
            string overlappingXmpFileStr = "";
            foreach (string s in overlappingXmpFileList)
            {
                overlappingXmpFileStr += s + NewLine;
            }

            CustomMessageBox customMessageBox = new(
                text: HelperControlAndMessageBoxHandling.ReturnControlText(
                          controlName:
                          "mbx_FrmMainApp_WarningMultipleImageFilesForXMP",
                          fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox) +
                      NewLine +
                      overlappingXmpFileStr,
                caption: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: HelperControlAndMessageBoxHandling.MessageBoxCaption
                                                                   .Warning.ToString(),
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBoxCaption),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
            customMessageBox.ShowDialog();
        }

        // ******************************
        // Extract data for all files that are supported
        Log.Info(message: "Files: Extracting File Data");
        int fileCount = 0;
        _ExifTool ??= new ExifTool();

        foreach (string fileNameWithPath in imageFiles)
        {
            Log.Info(message: $"File: {fileNameWithPath}");
            string fileNameWithoutPath = Path.GetFileName(path: fileNameWithPath);
            if (fileCount % 10 == 0)
            {
                statusMethod(
                    obj:
                    $"Scanning folder {100 * fileCount / imageFiles.Count:0}%: processing file '{fileNameWithoutPath}'");
            }

            // this is a bit complex but in _this_ loop we're not looking at sidecar files at _this_ stage whereas ...
            // I need to know if an xmp has changed or not
            bool fileNeedsReDEing = false;
            image2sidecar.TryGetValue(key: fileNameWithPath, value: out string sideCarFileNameWithPath);
            // so first we check the details of the image file then those of the xmp's
            for (int i = 0; i <= 1; i++)
            {
                string thisCheckSum = string.Empty;
                string storedChecksum = string.Empty;
                string fileNameWithPathToCheck = i == 0 ? fileNameWithPath : sideCarFileNameWithPath;

                // note to self: jpgs have no sidecars.
                if (File.Exists(path: fileNameWithPathToCheck))
                {
                    if (HelperVariables.fileChecksumhDictionary.TryGetValue(key: fileNameWithPathToCheck,
                            value: out string value))
                    {
                        storedChecksum = value;
                    }


                    // before anyone points out, yes we could store/check filesizes and match those two but the problem is that particularly
                    // xmp files can be modified within a single byte and then saved and have their datetime stamp changed to takendatetime
                    // so basically we could have two files that look identical on the surface but differ in content. 
                    // as such we do need to do the checksum test regardless of what the files appear like.

                    thisCheckSum = HelperFileSystemGetChecksum.GetChecksum(fileNameWithPath: fileNameWithPathToCheck);

                    fileNeedsReDEing = fileNeedsReDEing || storedChecksum != thisCheckSum ||
                                       !HelperVariables.fileChecksumhDictionary.ContainsKey(
                                           key: fileNameWithPathToCheck);
                }

                if (fileNeedsReDEing && !string.IsNullOrWhiteSpace(value: thisCheckSum))
                {
                    // update HelperVariables.fileChecksumhDictionary
                    HelperVariables.fileChecksumhDictionary[key: fileNameWithPathToCheck] = thisCheckSum;
                }
            }

            if (fileNeedsReDEing)
            {
                // delete from DE-collection (xmp files don't have a DE entry)
                DirectoryElement? directoryElementToRemove =
                    FindElementByFileNameWithPath(FileNameWithPath: fileNameWithPath);
                if (directoryElementToRemove is not null)
                {
                    Remove(item: directoryElementToRemove);
                }

                // Regular (image) files are added to the list of
                // Directory Elements...
                DirectoryElement fileToParseDictionaryElement = new(
                    itemNameWithoutPath: Path.GetFileName(path: fileNameWithoutPath),
                    type: DirectoryElement.ElementType.File,
                    fileNameWithPath: fileNameWithPath
                );

                // Add sidecar file and data if available
                IDictionary<string, string> dictProperties = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(value: sideCarFileNameWithPath))
                {
                    Log.Info(
                        message: $"Files: Extracting File Data - adding side car file '{sideCarFileNameWithPath}'");
                    fileToParseDictionaryElement.SidecarFile = sideCarFileNameWithPath;
                    // Logically XMP should take priority because RAW files are not meant to be edited.
                    InitiateEXIFParsing(fileNameWithPathToParse: sideCarFileNameWithPath, properties: dictProperties);
                }

                // Parse EXIF properties
                InitiateEXIFParsing(fileNameWithPathToParse: fileNameWithPath, properties: dictProperties);

                // Insert into model
                fileToParseDictionaryElement.ParseAttributesFromExifToolOutput(dictTagsIn: dictProperties);

                Add(item: fileToParseDictionaryElement, replaceIfExists: true);
            }

            fileCount++;
            FrmMainApp.TaskbarManagerInstance.SetProgressValue(currentValue: fileCount, maximumValue: imageFiles.Count);
            Thread.Sleep(millisecondsTimeout: 1);
        }

        CreateGuiDsForDirectoryElements();

        FrmMainApp.TaskbarManagerInstance.SetProgressState(state: TaskbarProgressBarState.NoProgress);

        Log.Info(message: "Files: Extracting File Data - OK");
    }

    /// <summary>
    ///     Assigns a GUID for the DEs in the DECollection
    /// </summary>
    private static void CreateGuiDsForDirectoryElements()
    {
        foreach (DirectoryElement directoryElement in FrmMainApp.DirectoryElements)
        {
            directoryElement.SetAttributeValue(attribute: ElementAttribute.GUID, value: Guid.NewGuid()
               .ToString(), version: DirectoryElement.AttributeVersion.Original, isMarkedForDeletion: false);
        }
    }

    /// <summary>
    ///     Parses the given file using the given EXIF Tool object into the given
    ///     dictionary. Thereby, ignoring duplicate tags.
    /// </summary>
    // ReSharper disable once InconsistentNaming
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

        // force-derive Refs here because they can disagree between RAW and XMP files.
        if (fileNameWithPathToParse.EndsWith(value: ".xmp"))
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