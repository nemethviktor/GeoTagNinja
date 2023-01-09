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


    public void ParseFolderToDEs(string folder, Action<string> statusMethod)
    {
        Logger.Trace(message: $"Start Parsing Folder '{folder}'");
        statusMethod("Scanning folder: Initializing ...");

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
        string[] allowedExtensions = AncillaryListsArrays.AllCompatibleExtensionsExt();
        Logger.Trace(message: "Loading allowedExtensions - OK");

        // ******************************
        // list files that have supported extensions
        statusMethod("Scanning folder: processing supported files ...");
        Logger.Trace(message: "Files: Listing Files");
        List<string> listFilesWithPath = new();
        try
        {
            listFilesWithPath = Directory
                .GetFiles(path: folder)
                .Where(predicate: file => allowedExtensions.Any(predicate: file.ToLower()
                                                                    .EndsWith))
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.Trace(message: "Files: Listing Files - Error: " + ex.Message);
            MessageBox.Show(text: ex.Message);
        }
        listFilesWithPath.Sort();
        Logger.Trace(message: "Files: Listing Files - OK");

        // ******************************
        // Extract data for all files that are supported
        Logger.Trace(message: "Files: Extracting File Data");
        ExifTool etw = new ExifTool();
        int count = 0;
        foreach (string fileNameWithPath in listFilesWithPath)
        {
            Logger.Trace(message: $"File: {fileNameWithPath}");
            string fileNameWithoutPath = Path.GetFileName(path: fileNameWithPath);
            statusMethod($"Scanning folder {100*count/listFilesWithPath.Count:0}%: processing file '{fileNameWithoutPath}'");
            DirectoryElement de = new DirectoryElement(
                                       itemName: Path.GetFileName(path: fileNameWithoutPath),
                                       type: DirectoryElement.ElementType.File,
                                       fullPathAndName: fileNameWithPath
                                   );
            ICollection<KeyValuePair<string, string>> props = new Dictionary<string, string>();
            etw.GetProperties(fileNameWithPath, props);
            de.ParseAttributesFromExifToolOutput((IDictionary<string, string>)props);
            this.Add(item: de);
            count++;
        }
        etw.Dispose();
        Logger.Trace(message: "Files: Extracting File Data - OK");
    }

}