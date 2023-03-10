using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using static System.Environment;

namespace GeoTagNinja.Helpers;

internal static class HelperFileSystemOperators
{
    /// <summary>
    ///     When the user leaves the current folder (or refreshes it) we check if there is anything in the write-queue
    ///     If the Q is empty we do as the user requested, else ask user if they want to write the data in the Q or discard it.
    /// </summary>
    /// <returns>Realistically nothing but it sets s_changeFolderIsOkay according to the user input and circumstances</returns>
    internal static async Task FsoCheckOutstandingFiledataOkayToChangeFolderAsync()
    {
        HelperVariables.SChangeFolderIsOkay = false;

        // check if there's anything in the write-Q
        bool dataToWrite = false;
        foreach (DirectoryElement dirElemFileToModify in FrmMainApp.DirectoryElements)
        {
            {
                foreach (SourcesAndAttributes.ElementAttribute attribute in (SourcesAndAttributes.ElementAttribute[])Enum.GetValues(enumType: typeof(SourcesAndAttributes.ElementAttribute)))
                {
                    if (dirElemFileToModify.HasSpecificAttributeWithVersion(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite))
                    {
                        dataToWrite = true;
                        break;
                    }
                }
            }
        }

        if (dataToWrite)
        {
            // ask: do you want to write/save?
            DialogResult dialogResult = MessageBox.Show(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_QuestionFileQIsNotEmpty"),
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Question"),
                buttons: MessageBoxButtons.YesNoCancel,
                icon: MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                while (HelperGenericFileLocking.FileListBeingUpdated || HelperGenericFileLocking.FilesAreBeingSaved)
                {
                    await Task.Delay(millisecondsDelay: 10);
                }

                await HelperExifWriteSaveToFile.ExifWriteExifToFile();
                HelperVariables.SChangeFolderIsOkay = true;
            }
            else if (dialogResult == DialogResult.No)
            {
                foreach (DirectoryElement dirElemFileToModify in FrmMainApp.DirectoryElements)
                {
                    {
                        foreach (SourcesAndAttributes.ElementAttribute attribute in (SourcesAndAttributes.ElementAttribute[])Enum.GetValues(enumType: typeof(SourcesAndAttributes.ElementAttribute)))
                        {
                            dirElemFileToModify.RemoveAttributeValue(attribute: attribute, version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite);
                        }
                    }
                }

                HelperVariables.SChangeFolderIsOkay = true;
            }
        }
        else
        {
            HelperVariables.SChangeFolderIsOkay = true;
        }
    }

    /// <summary>
    ///     This is called mostly on app exit and start - delete any remaining files such as image previews, txt files and
    ///     ... anything else that isn't an sqlite file.
    /// </summary>
    internal static void FsoCleanUpUserFolder()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        DirectoryInfo di = new(path: HelperVariables.UserDataFolderPath);

        foreach (FileInfo file in di.EnumerateFiles())
        {
            if (file.Extension != ".sqlite" && file.Name != "logfile.txt")
            {
                try
                {
                    FrmMainApp.Logger.Trace(message: "Deleting:" + file.Name);
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
}