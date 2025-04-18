﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using static System.Environment;
using static GeoTagNinja.Helpers.HelperControlAndMessageBoxHandling;

namespace GeoTagNinja.Helpers;

internal static class HelperFileSystemOperators
{
    /// <summary>
    ///     When the user leaves the current folder (or refreshes it) we check if there is anything in the write-queue
    ///     If the Q is empty we do as the user requested, else ask user if they want to write the data in the Q or discard it.
    /// </summary>
    /// <param name="isTheAppClosing">
    ///     Whether the app is closing at the time of the request. Relevant for avoiding a bug
    ///     whereby the dialogue-box gets prompted twice.
    /// </param>
    /// <returns>Realistically nothing but it sets s_changeFolderIsOkay according to the user input and circumstances</returns>
    internal static async Task FsoCheckOutstandingFileDataOkayToChangeFolderAsync(bool isTheAppClosing)
    {
        HelperVariables.OperationChangeFolderIsOkay = false;

        // check if there's anything in the write-Q
        bool dataToWrite = false;
        foreach (DirectoryElement dirElemFileToModify in FrmMainApp.DirectoryElements)
        {
            {
                foreach (SourcesAndAttributes.ElementAttribute attribute in (SourcesAndAttributes.ElementAttribute[])
                         Enum.GetValues(enumType: typeof(SourcesAndAttributes.ElementAttribute)))
                {
                    if (dirElemFileToModify.HasSpecificAttributeWithVersion(attribute: attribute,
                            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite) &&
                        dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                    {
                        if (dirElemFileToModify.Type == DirectoryElement.ElementType.File)
                        {
                            dataToWrite = true;
                            break;
                        }

                        // this shouldn't really come up but alas it does and i'm lazy to debug properly
                        dirElemFileToModify.RemoveAttributeValue(attribute: attribute,
                            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                    }
                }
            }
        }

        if (dataToWrite && !HelperVariables.AppIsClosingAndWriteFileQuestionHasBeenAsked)
        {
            // ask: do you want to write/save?
            if (isTheAppClosing)
            {
                HelperVariables.AppIsClosingAndWriteFileQuestionHasBeenAsked = true;
            }

            DialogResult dialogResult = HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBoxWithResult(
                controlName: "mbx_Helper_QuestionFileQIsNotEmpty", captionType: MessageBoxCaption.Question,
                buttons: MessageBoxButtons.YesNoCancel);

            if (dialogResult == DialogResult.Yes)
            {
                while (HelperGenericFileLocking.FileListBeingUpdated || HelperGenericFileLocking.FilesAreBeingSaved)
                {
                    await Task.Delay(millisecondsDelay: 10);
                }

                await HelperExifWriteSaveToFile.ExifWriteExifToFile();
                HelperVariables.OperationChangeFolderIsOkay = true;
            }
            else if (dialogResult == DialogResult.No)
            {
                foreach (DirectoryElement dirElemFileToModify in FrmMainApp.DirectoryElements)
                {
                    {
                        foreach (SourcesAndAttributes.ElementAttribute attribute in (
                                     SourcesAndAttributes.ElementAttribute[])Enum.GetValues(
                                     enumType: typeof(SourcesAndAttributes.ElementAttribute)))
                        {
                            dirElemFileToModify.RemoveAttributeValue(attribute: attribute,
                                version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                        }
                    }
                }

                HelperVariables.OperationChangeFolderIsOkay = true;
            }
        }
        else
        {
            HelperVariables.OperationChangeFolderIsOkay = true;
        }
    }

    /// <summary>
    ///     This is called mostly on app exit and start - delete any remaining files such as image previews, txt files and
    ///     ... anything else that isn't an sqlite file.
    /// </summary>
    internal static void FsoCleanUpUserFolder()
    {
        FrmMainApp.Log.Info(message: "Starting");

        DirectoryInfo di = new(path: HelperVariables.UserDataFolderPath);
        List<string> filesToKeep = ["exiftool.exe", ".ExifTool_config"];
        List<string> foldersToKeep = ["exiftool_files"];
        foreach (DirectoryInfo directory in di.EnumerateDirectories())
        {
            if (!foldersToKeep.Contains(item: directory.Name))
            {
                try
                {
                    FrmMainApp.Log.Trace(message: $"Deleting:{directory.Name}");
                    directory.Delete(recursive: true);
                }
                catch
                {
                    // nothing
                }
            }
        }

        foreach (FileInfo file in di.EnumerateFiles())
        {
            if (file.Extension != ".sqlite" &&
                !file.Name.StartsWith(value: "logfile") &&
                !filesToKeep.Contains(item: file.Name))
            {
                try
                {
                    FrmMainApp.Log.Trace(message: $"Deleting:{file.Name}");
                    file.Delete();
                }
                catch
                {
                    // nothing
                }
            }
        }
    }

    /// <summary>
    ///     Gets the parent folder of the current path (used when user wants to move a folder up.)
    ///     We can't just do a "cd.." because top level folders don't handle that logic well.
    /// </summary>
    /// <param name="path">The "current" path from which the user wants to move one level up</param>
    /// <returns>Parent path string of "current" path (as described above)</returns>
    internal static string FsoGetParent(string path)
    {
        DirectoryInfo dir = new(path: path);
        string parentName;
        if (dir.ToString() == Path.GetPathRoot(path: dir.ToString()))
        {
            parentName = SpecialFolder.MyComputer.ToString();
        }
        else if (dir.Parent == null)
        {
            parentName = null;
        }
        else
        {
            parentName = dir.Parent.FullName;
        }

        return parentName;
    }


    /// <summary>
    ///     Mimics the normal GetDirectories but handles Unauthorized errors efficiently
    ///     from/via https://stackoverflow.com/a/7296968/3968494
    /// </summary>
    /// <param name="path"></param>
    /// <param name="searchPattern"></param>
    /// <param name="searchOption"></param>
    /// <returns></returns>
    internal static List<string> GetDirectories(string path, string searchPattern = "*",
        SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (searchOption == SearchOption.TopDirectoryOnly)
        {
            return Directory.GetDirectories(path: path, searchPattern: searchPattern).ToList();
        }

        List<string> directories =
            new List<string>(collection: GetDirectories(path: path, searchPattern: searchPattern));

        for (int i = 0; i < directories.Count; i++)
        {
            directories.AddRange(collection: GetDirectories(path: directories[index: i], searchPattern: searchPattern));
        }

        return directories;
    }

    private static List<string> GetDirectories(string path, string searchPattern)
    {
        try
        {
            return Directory.GetDirectories(path: path, searchPattern: searchPattern).ToList();
        }
        catch (UnauthorizedAccessException)
        {
            return new List<string>();
        }
    }
}