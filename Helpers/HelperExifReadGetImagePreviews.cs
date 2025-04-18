﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using ImageMagick;

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
        FrmMainApp.Log.Info(message: "Starting");

    #region ExifToolConfiguration

        // want to give this a different name from the usual exifArgs.args just in case that's still being accessed (as much as it shouldn't be)
        Regex rgx = new(pattern: "[^a-zA-Z0-9]");
        string folderName = Path.GetDirectoryName(path: fileNameWithPath);
        string fileNameReplaced =
            rgx.Replace(
                input: fileNameWithPath.Replace(oldValue: folderName, newValue: ""),
                replacement: "_");
        string argsFile = Path.Combine(path1: HelperVariables.UserDataFolderPath,
            path2: $"exifArgs_getPreview_{fileNameReplaced}.args");
        string exiftoolCmd =
            $@" -charset utf8 -charset filename=utf8 -b -preview:GTNPreview -w! {HelperVariables.DoubleQuoteStr}{HelperVariables.UserDataFolderPath}\%F.jpg{HelperVariables.DoubleQuoteStr} -@ {HelperVariables.DoubleQuoteStr}{argsFile}{HelperVariables.DoubleQuoteStr}";

        File.Delete(path: argsFile);

    #endregion

        // add required tags

        FrmMainApp frmMainAppInstance =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        if (File.Exists(path: fileNameWithPath))
        {
            File.AppendAllText(path: argsFile,
                               contents: fileNameWithPath + Environment.NewLine,
                               encoding: Encoding.UTF8);
            File.AppendAllText(path: argsFile,
                contents: $"-execute{Environment.NewLine}");
        }

        FrmMainApp.Log.Trace(message: "Starting ExifTool");
        ///////////////
        await HelperExifExifToolOperator.RunExifTool(exiftoolCmd: exiftoolCmd,
                                                     frmMainAppInstance: null,
                                                     initiator:
                                                     HelperGenericAncillaryListsArrays.ExifToolInititators
                                                        .ExifGetImagePreviews);
        ///////////////
        FrmMainApp.Log.Debug(message: "Done");
    }

    /// <summary>
    ///     Triggers ExifTool's preview creation/extraction for the DirectoryElement selected
    /// </summary>
    /// <param name="directoryElement">The DE for which the preview should be created</param>
    /// <param name="initiator">The source of the request</param>
    /// <returns></returns>
    internal static async Task GenericCreateImagePreview(DirectoryElement directoryElement,
        string initiator)
    {
        // ignore if not a file.
        if (directoryElement.Type != DirectoryElement.ElementType.File)
        {
            return;
        }

        string fileNameWithPath = directoryElement.FileNameWithPath;
        string fileNameWithoutPath = directoryElement.ItemNameWithoutPath;

        FrmMainApp frmMainAppInstance =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        FrmEditFileData frmEditFileDataInstance =
            (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
        Image img = null;
        //FileInfo fi = new(fileName: directoryElement.FileNameWithPath);
        string generatedFileName = null;

        if (initiator == "FrmMainApp" && frmMainAppInstance != null)
        {
            frmMainAppInstance.pbx_imagePreview.Image = null;
            generatedFileName = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                path2: $"{fileNameWithoutPath}.jpg");
        }
        else if (initiator == "FrmMainAppAPIDataSelection" && frmMainAppInstance != null)
        {
            frmMainAppInstance.pbx_imagePreview.Image = null;
            generatedFileName = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                path2: $"{fileNameWithoutPath}.jpg");
        }
        else if (initiator == "FrmEditFileData" && frmEditFileDataInstance != null)
        {
            frmEditFileDataInstance.pbx_imagePreview.Image = null;
            generatedFileName = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                path2: $"{fileNameWithoutPath}.jpg");
        }

        // sometimes the file doesn't get created. (ie exiftool may fail to extract a preview.)
        string pbxErrorMsg = HelperControlAndMessageBoxHandling.ReturnControlText(
            controlName: "pbx_imagePreviewCouldNotRetrieve",
            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.PictureBox
        );

        try
        {
            List<string> magickExtensionList =
            [
                "heic",
                "heif",
                "webp"
            ];

            if (magickExtensionList.Contains(item: directoryElement.Extension.TrimStart('.').ToLower()))
            {
                // actually the reason i'm not using this for _every_ file is that it's just f...ing slow with NEF files(which is what I have plenty of), so it's prohibitive to run on RAW files. 
                //  since i don't have a better way to deal with HEIC/WEBP files atm this is as good as it gets.
                UseMagickImageToGeneratePreview(originalImagePath: fileNameWithPath,
                    jpegPath: generatedFileName);
            }
            else
            {
                // via https://stackoverflow.com/a/6576645/3968494
                using FileStream stream = new(path: fileNameWithPath, mode: FileMode.Open,
                    access: FileAccess.Read);
                img = Image.FromStream(stream: stream);
            }
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
                    using FileStream stream = new(path: generatedFileName,
                                                  mode: FileMode.Open,
                                                  access: FileAccess.Read);
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
            if ((initiator == "FrmMainApp" ||
                 initiator == "FrmMainAppAPIDataSelection") &&
                frmMainAppInstance != null)
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

    /// <summary>
    ///     Takes a HEIC/WebP/etc file and dumps a JPG. Uses MagickImage. See comment above as to why this isn't being used for
    ///     all RAW
    ///     files. (Too slow).
    /// </summary>
    /// <param name="originalImagePath"></param>
    /// <param name="jpegPath"></param>
    private static void UseMagickImageToGeneratePreview(string originalImagePath,
                                                        string jpegPath)
    {
        using MagickImage image = new(fileName: originalImagePath);
        // The AutoOrient() method adjusts the image to respect its orientation.
        image.AutoOrient();
        // Save the image as a JPEG.
        image.Write(fileName: jpegPath);
    }
}