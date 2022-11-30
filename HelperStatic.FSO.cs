using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Environment;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
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
    ///     When the user leaves the current folder (or refreshes it) we check if there is anything in the write-queue
    ///     If the Q is empty we do as the user requested, else ask user if they want to write the data in the Q or discard it.
    /// </summary>
    /// <returns>Realistically nothing but it sets s_changeFolderIsOkay according to the user input and circumstances</returns>
    internal static async Task FsoCheckOutstandingFiledataOkayToChangeFolderAsync()
    {
        SChangeFolderIsOkay = false;

        // check if there's anything in the write-Q
        if (FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Rows.Count > 0)
        {
            DialogResult dialogResult = MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_QuestionFileQIsNotEmpty"), caption: "Info", buttons: MessageBoxButtons.YesNoCancel, icon: MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                while (FileListBeingUpdated || FilesAreBeingSaved)
                {
                    await Task.Delay(millisecondsDelay: 10);
                }

                await ExifWriteExifToFile();
                SChangeFolderIsOkay = true;
            }
            else if (dialogResult == DialogResult.No)
            {
                FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Rows.Clear();
                SChangeFolderIsOkay = true;
            }
        }
        else
        {
            SChangeFolderIsOkay = true;
        }
    }

    /// <summary>
    ///     This is called mostly on app exit and start - delete any remaining files such as image previews, txt files and
    ///     ... anything else that isn't an sqlite file.
    /// </summary>
    internal static void FsoCleanUpUserFolder()
    {
        DirectoryInfo di = new(path: FrmMainApp.UserDataFolderPath);

        foreach (FileInfo file in di.EnumerateFiles())
        {
            if (file.Extension != ".sqlite")
            {
                try
                {
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
    ///     Creates an MD5 hash of the given fileNameWithPath.
    ///     Via https://lonewolfonline.net/calculate-md5-checksum-file/
    /// </summary>
    /// <param name="fileNameWithPath">Filename to process</param>
    /// <returns>MD5 hash</returns>
    internal static string Md5SumByProcess(string fileNameWithPath)
    {
        HashAlgorithm MD5 = new MD5CryptoServiceProvider();

        try
        {
            using BufferedStream stream = new(stream: File.OpenRead(path: fileNameWithPath), bufferSize: 100000);
            return BitConverter.ToString(value: MD5.ComputeHash(inputStream: stream))
                .Replace(oldValue: "-", newValue: string.Empty);
        }
        catch
        {
            return string.Empty;
        }
    }
}