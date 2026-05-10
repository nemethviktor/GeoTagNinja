using GeoTagNinja.Helpers;
using Microsoft.WindowsAPICodePack.Taskbar;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsDarkThemerNinja;
using static System.Environment;

namespace GeoTagNinja.Model;

public class DirectoryElementCollection : List<DirectoryElement>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private ExifTool _ExifTool;
    private FrmPleaseWaitBox _frmPleaseWaitBoxInstance;
    private HelperNonStatic _helperNonStatic = new();

    /// <summary>
    /// Provides access to the currently running metadata hydration task.
    /// </summary>
    public Task? HydrationTask { get; private set; }

    /// <summary>
    /// Indicates whether the metadata hydration process is currently active.
    /// </summary>
    public bool IsHydrating => HydrationTask != null && !HydrationTask.IsCompleted;

    public ExifTool ExifTool
    {
        get => _ExifTool;
        set
        {
            // Don't just dispose immediately if a task might be using it
            ExifTool oldTool = _ExifTool;
            _ExifTool = value;

            _ = Task.Run(() =>
            {
                Thread.Sleep(500); // Give background tasks a moment to see the cancellation
                oldTool?.Dispose();
            });
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
            if (item.GetAttributeValueAsString(attribute: SourcesAndAttributes.ElementAttribute.GUID,
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
    /// <param name="optionalListOfDEs">An optional list of DEs to check (in case we only want to save some of the items.)</param>
    public HashSet<string> FindDirtyElements(List<DirectoryElement> optionalListOfDEs = null)
    {
        List<DirectoryElement> directoryElements = optionalListOfDEs ?? this;
        HashSet<string> uids = [];
        foreach (DirectoryElement directoryElement in directoryElements)
        {
            if (directoryElement.HasDirtyAttributes())
            {
                _ = uids.Add(
                    item: directoryElement.GetAttributeValueAsString(
                        attribute: SourcesAndAttributes.ElementAttribute.GUID,
                        nowSavingExif: false));
            }
        }

        return uids;
    }

    /// <summary>
    /// Finds the best matching DirectoryElement for an XMP file. 
    /// Prioritizes raw/non-jpg files over jpg files.
    /// </summary>
    /// <param name="XMPFileNameWithPath">Full path of the XMP file.</param>
    /// <returns>The best matching DirectoryElement or null if none found.</returns>
    public DirectoryElement FindElementByBelongingToXmpWithPath(string XMPFileNameWithPath)
    {
        // Strip the .xmp extension to get the base path (e.g., .../img_01)
        string basePath = XMPFileNameWithPath.EndsWith(".xmp", StringComparison.OrdinalIgnoreCase)
            ? XMPFileNameWithPath.Substring(0, XMPFileNameWithPath.Length - 4)
            : XMPFileNameWithPath;

        DirectoryElement jpgFallback = null;

        foreach (DirectoryElement item in this)
        {
            // Check if the file starts with the same name
            if (item.FileNameWithPath.StartsWith(value: basePath, comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                // Ignore the XMP file itself
                if (item.FileNameWithPath.EndsWith(".xmp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check if it is a JPG
                bool isJpg = item.FileNameWithPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                             item.FileNameWithPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);

                if (!isJpg)
                {
                    // High priority: Found a Raw or other format. Return immediately.
                    return item;
                }
                else
                {
                    // Low priority: Keep the JPG in case we don't find anything better.
                    jpgFallback = item;
                }
            }
        }

        // If we finished the loop and never found a Raw file, return the JPG (if found)
        return jpgFallback;
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
            _ = Remove(item: exstgElement);
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
        CancellationTokenSource cts,
        Action<DirectoryElement> onElementFound = null) // ADD THIS PARAMETER
    {
        Log.Trace(message: $"Start Parsing Folder '{folderOrCollectionFileName}'");
        updateProgressHandler(obj: "Scanning folder: Initializing ...");

        CancellationToken cancellationToken = cts.Token;
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
                    DirectoryElement de = new(
                        itemNameWithoutPath: drive.Name,
                        type: DirectoryElement.ElementType.Drive,
                        fileNameWithPath: drive.RootDirectory.FullName);
                    Add(item: de, replaceIfExists: true);

                    // STREAM TO UI:
                    onElementFound?.Invoke(de);
                }

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
                    DirectoryElement de = new(
                        itemNameWithoutPath: FrmMainApp.ParentFolder,
                        type: DirectoryElement.ElementType.ParentDirectory,
                        fileNameWithPath: tmpStrParent);

                    Add(item: de, replaceIfExists: true);

                    // STREAM TO UI:
                    onElementFound?.Invoke(de);
                }
            }
            catch (Exception ex)
            {
                Log.Error(message: $"Could not add parent. Error: {ex.Message}");
                Themer.ShowMessageBox(
                    message: HelperControlAndMessageBoxHandling.ReturnControlText(
                        controlName: "mbx_DirectoryElementCollection_ErrorParsing",
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                    icon: MessageBoxIcon.Error,
                    buttons: MessageBoxButtons.OK);
            }

            #endregion

            #region Folders

            // ******************************
            // list folders, ReparsePoint means these are links.
            updateProgressHandler(obj: "Scanning folder: processing directories ...");
            Log.Trace(message: "Listing Folders");
            List<string> dirs = [];
            try
            {
                DirectoryInfo di = new(path: folderOrCollectionFileName);
                foreach (DirectoryInfo directoryInfo in di.GetDirectories())
                {
                    if (directoryInfo.FullName == SpecialFolder.MyComputer.ToString())
                    {
                        // It's the MyComputer entry
                        Log.Trace(message: $"MyComputer: {directoryInfo.Name}");
                        DirectoryElement de = new(
                            itemNameWithoutPath: directoryInfo.Name,
                            type: DirectoryElement.ElementType.MyComputer,
                            fileNameWithPath: directoryInfo.FullName);

                        Add(item: de, replaceIfExists: true);

                        // STREAM TO UI:
                        onElementFound?.Invoke(de);
                    }
                    else if (directoryInfo.Attributes.ToString()
                                          .Contains(value: "Directory") &&
                             !directoryInfo.Attributes.ToString()
                                           .Contains(value: "ReparsePoint"))
                    {
                        Log.Trace(message: $"Folder: {directoryInfo.Name}");
                        DirectoryElement de = new(
                            itemNameWithoutPath: directoryInfo.Name,
                            type: DirectoryElement.ElementType.SubDirectory,
                            fileNameWithPath: directoryInfo.FullName);

                        Add(item: de, replaceIfExists: true);

                        // STREAM TO UI:
                        onElementFound?.Invoke(de);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(message: $"Error: {ex.Message}");
                Themer.ShowMessageBox(
                    message: HelperControlAndMessageBoxHandling.ReturnControlText(
                        controlName: "mbx_DirectoryElementCollection_ErrorParsing",
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                    icon: MessageBoxIcon.Error,
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
            List<FileInfo> filesInCollection = [];
            foreach (string collectItemWithPath in File.ReadLines(path: Program.CollectionFileLocation))
            {
                if (File.Exists(path: collectItemWithPath))
                {
                    FileInfo fi = new(fileName: collectItemWithPath);
                    DirectoryElement de = new(
                        itemNameWithoutPath: fi.Name,
                        type: DirectoryElement.ElementType.File,
                        fileNameWithPath: fi.FullName);

                    Add(item: de);

                    // INSERT THIS LINE:
                    onElementFound?.Invoke(de);

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

            _frmPleaseWaitBoxInstance?.UpdateControlsVisibility(stage: FrmPleaseWaitBox.ActionStages.SCANNING);


            try
            {
                await Task.Run(action: () =>
                {
                    filesInDir = _helperNonStatic.GetFilesFromAFolder(
                        folder: folderOrCollectionFileName,
                        filter: allowedImageExtensions.Concat(second: allowedSidecarExtensions).ToArray(),
                        recursive: processSubFolders,
                        updateProgressHandler: updateProgressHandler,
                        cancellationToken: cancellationToken
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
                    _ = sidecarFiles.Add(item: fileInfoItem);
                }

                // Image file
                else if (allowedImageExtensions.Contains(
                             value: fileInfoItem.Extension.Replace(oldValue: ".", newValue: ""),
                             comparer: StringComparer.InvariantCultureIgnoreCase))
                {
                    _ = imageFiles.Add(item: fileInfoItem);

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
                            _ = sidecarFiles.Add(item: fiSidecarFileInfoItem);
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
                        _ = overlappingXMPFileList.Add(item: sideCarFileInfoItem);
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

            Themer.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_FrmMainApp_WarningMultipleImageFilesForXMP",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox
                    ) +
                    Environment.NewLine + $"{overlappingXmpFileStr}",
                icon: MessageBoxIcon.Warning,
                buttons: MessageBoxButtons.OK);
        }

        // ******************************
        // Extract data for all files that are supported
        Log.Info(message: "Files: Extracting File Data");
        int fileCount = 0;
        _ExifTool ??= new ExifTool();

        _frmPleaseWaitBoxInstance?.UpdateControlsVisibility(stage: FrmPleaseWaitBox.ActionStages
                                                                          .PARSING);
        await Task.Run(action: () =>
        {
            ParseImagesToDirectoryElements(updateProgressHandler: updateProgressHandler,
                imageFiles: imageFiles,
                sidecarFiles: sidecarFiles,
                fileCount: fileCount,
                imageToSidecarFileMapping: imageToSidecarFileMapping,
                cancellationToken: cancellationToken,
                onElementFound: onElementFound);
        });

        FrmMainApp.TaskbarManagerInstance.SetProgressState(state: TaskbarProgressBarState.NoProgress);

        Log.Info(message: "Files: Extracting File Data - OK");

        #endregion

        #endregion
    }

    /// <summary>
    /// Starts and awaits a background metadata hydration task, storing the running task in HydrationTask for
    /// monitoring.
    /// </summary>
    /// <remarks>Captures the running task in HydrationTask so the UI can monitor progress and then awaits its
    /// completion.</remarks>
    /// <param name="onUpdated">Callback invoked when a DirectoryElement is updated during hydration.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the background read operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StartBackgroundMetadataRead(Action<DirectoryElement> onUpdated, CancellationToken ct)
    {
        // Capture the task so the UI can monitor it
        HydrationTask = RunHydrationInternal(onUpdated: onUpdated, ct: ct);
        await HydrationTask;
    }

    /// <summary>
    /// Starts an asynchronous background scan that reads metadata for file elements and invokes a callback when
    /// metadata is available.
    /// </summary>
    /// <remarks>Processes only file elements, prefers a sidecar file when present, and invokes ExifTool on a
    /// background thread to avoid blocking. Aggregates properties by key, updates DirectoryElement attributes, clears
    /// the IsDirty flag on success, invokes the callback to refresh UI rows (including reapplying cached attributes),
    /// and logs errors without propagating exceptions.</remarks>
    /// <param name="onUpdated">Callback invoked for each DirectoryElement when cached attributes are reapplied or when new metadata has been
    /// read; may be null.</param>
    /// <param name="ct">Cancellation token used to cancel the background processing; checked between files.</param>
    /// <returns>A Task that completes when the background metadata read finishes or is canceled.</returns>
    public async Task RunHydrationInternal(Action<DirectoryElement> onUpdated, CancellationToken ct)
    {
        Log.Info("Metadata Hydration: Task Started");

        // Get a snapshot of the files
        List<DirectoryElement> workList;
        lock (this) { workList = this.Where(x => x.Type == DirectoryElement.ElementType.File).ToList(); }

        foreach (DirectoryElement de in workList)
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            // We only skip if:
            // 1. It already HAS attributes (HasAttributes is true)
            // 2. AND it is NOT dirty (checksum matches)
            if (de.IsHydrated && !de.IsDirty)
            {
                // Still notify UI to show cached data
                onUpdated?.Invoke(de);
                continue;
            }

            try
            {
                // Prefer Sidecar, then RAW
                string fileToRead = de.SidecarFile?.FullName ?? de.FileNameWithPath;
                List<KeyValuePair<string, string>> props = [];

                // Run ExifTool
                await Task.Run(() =>
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    ExifTool.GetProperties(fileToRead, props);
                }, ct);

                if (props.Count > 0 && !ct.IsCancellationRequested)
                {
                    Dictionary<string, string> dict = props.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.First().Value);
                    de.ParseAttributesFromExifToolOutput(dict);

                    // Reset dirty flag so next refresh skips this file
                    de.IsDirty = false;

                    onUpdated?.Invoke(de);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Hydration failed");
            }
        }
        Log.Info("Metadata Hydration: Task Finished");
    }

    /// <summary>
    /// Parses the provided image files into DirectoryElement instances, links matching .xmp sidecar files, computes and
    /// updates file checksums, marks elements as dirty when content changes, and notifies via an optional callback.
    /// </summary>
    /// <remarks>Updates HelperVariables.FileChecksumDictionary with computed checksums and returns early if
    /// cancellation is requested.</remarks>
    /// <param name="updateProgressHandler">Handler invoked to report progress messages.</param>
    /// <param name="imageFiles">Set of image files to process into directory elements.</param>
    /// <param name="sidecarFiles">Set of available sidecar files (.xmp) used to link to images.</param>
    /// <param name="fileCount">Total number of files expected for progress calculation.</param>
    /// <param name="imageToSidecarFileMapping">Optional mapping from image FileInfo to its sidecar FileInfo.</param>
    /// <param name="cancellationToken">CancellationToken to observe for cooperative cancellation.</param>
    /// <param name="onElementFound">Optional callback invoked for each created or updated DirectoryElement.</param>
    private void ParseImagesToDirectoryElements(Action<string> updateProgressHandler,
                                                HashSet<FileInfo> imageFiles,
                                                HashSet<FileInfo> sidecarFiles,
                                                int fileCount,
                                                IDictionary<FileInfo, FileInfo> imageToSidecarFileMapping,
                                                CancellationToken cancellationToken,
                                                Action<DirectoryElement> onElementFound = null)
    {
        foreach (FileInfo imagefileFileInfoItem in imageFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // 1. Find the element in THIS collection (the cache)
            DirectoryElement? de = this.FirstOrDefault(x => x.FileNameWithPath == imagefileFileInfoItem.FullName);
            bool isNew = de == null;

            if (isNew)
            {
                de = new DirectoryElement(imagefileFileInfoItem.Name, DirectoryElement.ElementType.File, imagefileFileInfoItem.FullName);
                Add(de);
            }

            // 2. THE LINK: Find the sidecar in the list passed to this method
            string baseName = Path.GetFileNameWithoutExtension(imagefileFileInfoItem.Name);
            de.SidecarFile = sidecarFiles.FirstOrDefault(x =>
                x.Name.Equals(baseName + ".xmp", StringComparison.InvariantCultureIgnoreCase));

            // 3. Checksum Logic (Abbreviated for clarity)
            bool fileNeedsReDEing = isNew;
            bool checksumChanged = false;
            for (int i = 0; i <= 1; i++)
            {
                string thisCheckSum = string.Empty;
                string storedChecksum = string.Empty;
                FileInfo fileNameWithPathToCheck = i == 0 ? imagefileFileInfoItem : de.SidecarFile;

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
                    checksumChanged = storedChecksum != thisCheckSum;

                    fileNeedsReDEing = fileNeedsReDEing || checksumChanged ||
                                       !HelperVariables.FileChecksumDictionary.ContainsKey(
                                           key: fileNameWithPathToCheck.FullName);
                }

                if (fileNeedsReDEing && !string.IsNullOrWhiteSpace(value: thisCheckSum))
                {
                    // update HelperVariables.fileChecksumhDictionary
                    HelperVariables.FileChecksumDictionary[key: fileNameWithPathToCheck.FullName] = thisCheckSum;
                }
            }

            de.IsDirty = !de.IsHydrated || fileNeedsReDEing;

            // 4. Notify UI
            onElementFound?.Invoke(de);
        }
    }
}