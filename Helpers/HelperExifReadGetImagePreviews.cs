using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers;

internal static class HelperExifReadGetImagePreviews
{
    /// <summary>
    ///     This generates (technically, extracts) the image previews from files for the
    ///     user when they click on a filename
    ///     ... in whichever listview.
    /// </summary>
    /// <param name="fileNameWithPath">Path of file for which the preview needs creating</param>
    /// <returns>Realistically nothing but the process generates the bitmap if possible</returns>
    internal static async Task ExifGetImagePreviews(string fileNameWithPath)
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        #region ExifToolConfiguration

        string exifToolExe = Path.Combine(path1: HelperVariables.ResourcesFolderPath, path2: "exiftool.exe");

        // want to give this a different name from the usual exifArgs.args just in case that's still being accessed (as much as it shouldn't be)
        Regex rgx = new(pattern: "[^a-zA-Z0-9]");
        string folderName = Path.GetDirectoryName(fileNameWithPath);
        string fileNameReplaced = rgx.Replace(input: fileNameWithPath.Replace(oldValue: folderName, newValue: ""), replacement: "_");
        string argsFile = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "exifArgs_getPreview_" + fileNameReplaced + ".args");
        string exiftoolCmd = " -charset utf8 -charset filename=utf8 -b -preview:GTNPreview -w! " + HelperVariables.DoubleQuoteStr + HelperVariables.UserDataFolderPath + @"\%F.jpg" + HelperVariables.DoubleQuoteStr + " -@ " + HelperVariables.DoubleQuoteStr + argsFile + HelperVariables.DoubleQuoteStr;

        File.Delete(path: argsFile);

        #endregion

        // add required tags

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        if (File.Exists(path: fileNameWithPath))
        {
            File.AppendAllText(path: argsFile, contents: fileNameWithPath + Environment.NewLine, encoding: Encoding.UTF8);
            File.AppendAllText(path: argsFile, contents: "-execute" + Environment.NewLine);
        }

        FrmMainApp.Logger.Trace(message: "Starting ExifTool");
        ///////////////
        await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                                     frmMainAppInstance: null,
                                                     initiator: "ExifGetImagePreviews");
        ///////////////
        FrmMainApp.Logger.Debug(message: "Done");
    }

    /// <summary>
    ///     Triggers the "create preview" process for the file it's sent to check
    /// </summary>
    /// <param name="fileNameWithPath">Filename w/ path to check</param>
    /// <param name="initiator"></param>
    /// <returns></returns>
    internal static async Task GenericCreateImagePreview(string fileNameWithPath,
                                                         string initiator)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
        Image img = null;
        FileInfo fi = new(fileName: fileNameWithPath);
        string generatedFileName = null;

        if (initiator == "FrmMainApp" && frmMainAppInstance != null)
        {
            frmMainAppInstance.pbx_imagePreview.Image = null;
            generatedFileName = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: frmMainAppInstance.lvw_FileList.SelectedItems[index: 0]
                                                                                                   .Text +
                                                                                               ".jpg");
        }
        else if (initiator == "FrmMainAppAPIDataSelection" && frmMainAppInstance != null)
        {
            frmMainAppInstance.pbx_imagePreview.Image = null;
            string fileNameWithoutPath = Path.GetFileName(path: fileNameWithPath);
            generatedFileName = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: fileNameWithoutPath + ".jpg");
        }
        else if (initiator == "FrmEditFileData" && frmEditFileDataInstance != null)
        {
            frmEditFileDataInstance.pbx_imagePreview.Image = null;
            generatedFileName = Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: frmEditFileDataInstance.lvw_FileListEditImages.SelectedItems[index: 0]
                                                                                                   .Text +
                                                                                               ".jpg");
        }

        //sometimes the file doesn't get created. (ie exiftool may fail to extract a preview.)
        string pbxErrorMsg = HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: ControlType.PictureBox,
            objectName: "pbx_imagePreviewCouldNotRetrieve"
        );

        try
        {
            // via https://stackoverflow.com/a/6576645/3968494
            using FileStream stream = new(path: fileNameWithPath, mode: FileMode.Open, access: FileAccess.Read);
            img = Image.FromStream(stream: stream);
        }
        catch
        {
            // nothing.
        }

        if (img == null)
        {
            // don't run the thing again if file has already been generated
            if (!File.Exists(path: generatedFileName))
            {
                await ExifGetImagePreviews(fileNameWithPath: fileNameWithPath);
            }

            if (File.Exists(path: generatedFileName))
            {
                try
                {
                    using FileStream stream = new(path: generatedFileName, mode: FileMode.Open, access: FileAccess.Read);
                    img = Image.FromStream(stream: stream);

                    img.ExifRotate();
                }
                catch
                {
                    // nothing
                }
            }
        }

        if (img != null)
        {
            if ((initiator == "FrmMainApp" || initiator == "FrmMainAppAPIDataSelection") && frmMainAppInstance != null)
            {
                frmMainAppInstance.pbx_imagePreview.Image = img;
            }
            else if (initiator == "FrmEditFileData" && frmEditFileDataInstance != null)
            {
                frmEditFileDataInstance.pbx_imagePreview.Image = img;
            }
        }

        else
        {
            if (initiator == "FrmMainApp" && frmMainAppInstance != null)
            {
                frmMainAppInstance.pbx_imagePreview.SetErrorMessage(message: pbxErrorMsg);
            }
            else if (initiator == "FrmEditFileData" && frmEditFileDataInstance != null)
            {
                // frmEditFileDataInstance.pbx_imagePreview.SetErrorMessage(message: pbxErrorMsg); // <- nonesuch.
            }
        }
    }
}