using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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

        // it could happen that there is a ".." (root/parent) in the write-Q, which is erroneous but causes a problem here. 
        IEnumerable<DataRow> drDT3Rows =
            from drDT3 in FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.AsEnumerable()
            where drDT3.Field<string>(columnName: "fileNameWithoutPath") != ".."
            select drDT3;

        if (drDT3Rows.Any())
        {
            FrmMainApp.DtFileDataToWriteStage3ReadyToWrite = drDT3Rows.CopyToDataTable();
        }
        else
        {
            FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Clear();
        }

        if (FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Rows.Count > 0)
        {
            DialogResult dialogResult = MessageBox.Show(
                text: GenericGetMessageBoxText(messageBoxName: "mbx_Helper_QuestionFileQIsNotEmpty"),
                HelperStatic.GenericGetMessageBoxCaption(captionType: "Question"),
                buttons: MessageBoxButtons.YesNoCancel,
                icon: MessageBoxIcon.Question);
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
        FrmMainApp.Logger.Debug(message: "Starting");

        DirectoryInfo di = new(path: FrmMainApp.UserDataFolderPath);

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
}