using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using GeoTagNinja.View.ListView;
using ImageMagick;
using Encoder = System.Drawing.Imaging.Encoder;

namespace GeoTagNinja.Helpers;

internal static class HelperExifReadGetImagePreviews
{
    [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
    internal enum Initiator
    {
        FrmMainAppPictureBox,
        FrmMainAppAPIDataSelection,
        FrmMainAppListViewThumbnail,
        FrmEditFileData
    }

    private static readonly List<string> MagickExtensionList =
    [
        "heic",
        "heif",
        "webp"
    ];


    /// <summary>
    ///     This generates (technically, extracts) the image previews from files for the
    ///     user when they click on a filename
    ///     ... in whichever listview.
    /// </summary>
    /// <param name="fileNameWithPath">Path of file for which the preview needs creating</param>
    /// <param name="initiator"></param>
    /// <returns>Realistically nothing but the process generates the bitmap if possible</returns>
    [SuppressMessage(category: "ReSharper", checkId: "AssignNullToNotNullAttribute")]
    private static async Task ExifGetImagePreviews(string fileNameWithPath,
                                                   Initiator initiator)
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
            $@" -charset utf8 -charset filename=utf8 -b -preview:{(initiator == Initiator.FrmMainAppListViewThumbnail ? "GTNPreviewThumb" : "GTNPreview")} -w! {HelperVariables.DoubleQuoteStr}{HelperVariables.UserDataFolderPath}\%F.jpg{HelperVariables.DoubleQuoteStr} -@ {HelperVariables.DoubleQuoteStr}{argsFile}{HelperVariables.DoubleQuoteStr}";

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

    private static async Task ExifGetImagePreviewsForThumbnails(List<string> fileNamesWithPaths,
                                                                Initiator initiator)
    {
        FrmMainApp.Log.Info(message: "Starting");

    #region ExifToolConfiguration

        string argsFile = Path.Combine(path1: HelperVariables.UserDataFolderPath,
            path2: "exifArgs_getPreviewThumbs.args");
        File.Delete(path: argsFile);

        foreach (string fileNameWithPath in fileNamesWithPaths)
        {
            if (File.Exists(path: fileNameWithPath))
            {
                File.AppendAllText(path: argsFile,
                    contents: fileNameWithPath + Environment.NewLine,
                    encoding: Encoding.UTF8);
            }
        }


        //foreach (string fileNameWithPath in fileNamesWithPaths.Where(predicate: File.Exists))
        //{
        //    File.AppendAllText(path: argsFile,
        //        contents: fileNameWithPath + Environment.NewLine,
        //        encoding: Encoding.UTF8);
        //}

        string exiftoolCmd =
            $@" -charset utf8 -charset filename=utf8 -b -preview:{(initiator == Initiator.FrmMainAppListViewThumbnail ? "GTNPreviewThumb" : "GTNPreview")} -w! {HelperVariables.DoubleQuoteStr}{HelperVariables.UserDataFolderPath}\%F.jpg{HelperVariables.DoubleQuoteStr} -@ {HelperVariables.DoubleQuoteStr}{argsFile}{HelperVariables.DoubleQuoteStr}";

    #endregion

        // add required tags
        File.AppendAllText(path: argsFile, contents: $"-execute{Environment.NewLine}");

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
                                                         Initiator initiator)
    {
        // ignore if not a file.
        if (directoryElement.Type != DirectoryElement.ElementType.File)
        {
            return;
        }

        string fileNameWithPath = directoryElement.FileNameWithPath;
        string fileNameWithoutPath = directoryElement.ItemNameWithoutPath;

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
        Image img = null;
        //FileInfo fi = new(fileName: directoryElement.FileNameWithPath);
        string generatedFileName = null;

        if ((initiator == Initiator.FrmMainAppPictureBox || initiator == Initiator.FrmMainAppAPIDataSelection) &&
            frmMainAppInstance != null)
        {
            frmMainAppInstance.pbx_imagePreview.Image = null;
            generatedFileName = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                path2: $"{fileNameWithoutPath}.jpg");
        }
        else if (initiator == Initiator.FrmMainAppListViewThumbnail &&
                 frmMainAppInstance != null)
        {
            generatedFileName = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                path2: $"{fileNameWithoutPath}.jpg");
        }

        else if (initiator == Initiator.FrmEditFileData &&
                 frmEditFileDataInstance != null)
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
            if (MagickExtensionList.Contains(item: directoryElement.Extension.TrimStart('.').ToLower()))
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
                await ExifGetImagePreviews(fileNameWithPath: fileNameWithPath, initiator: initiator);
            }

            if (File.Exists(path: generatedFileName))
            {
                try
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
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
            if ((initiator == Initiator.FrmMainAppPictureBox ||
                 initiator == Initiator.FrmMainAppAPIDataSelection) &&
                frmMainAppInstance != null)
            {
                frmMainAppInstance.pbx_imagePreview.Image = img;
            }
            else if (initiator == Initiator.FrmMainAppListViewThumbnail)
            {
                if (!File.Exists(path: generatedFileName))
                {
                    await ExifGetImagePreviews(fileNameWithPath: fileNameWithPath, initiator: initiator);
                }
            }
            else if (initiator == Initiator.FrmEditFileData &&
                     frmEditFileDataInstance != null)
            {
                frmEditFileDataInstance.pbx_imagePreview.Image = img;
            }
        }

        else
        {
            if (initiator == Initiator.FrmMainAppPictureBox &&
                frmMainAppInstance != null)
            {
                frmMainAppInstance.pbx_imagePreview.SetErrorMessage(message: pbxErrorMsg);
            }
            else if (initiator == Initiator.FrmEditFileData &&
                     frmEditFileDataInstance != null)
            {
                // frmEditFileDataInstance.pbx_imagePreview.SetErrorMessage(message: pbxErrorMsg); // <- nonesuch.
            }
        }
    }

    /// <summary>
    ///     The whole fuckery with the previews having to have three different branches is down to (speed) or the lack of it.
    ///     The fastest is Windows's own implementation but it only supports JPGs and basic formats.
    ///     ET supports most formats but is relatively slow
    ///     Mick is dreadfully slow but also supports some obscure formats like HEIC
    ///     Also there's some issue w/ ET and Orientation sometimes not being picked up.
    /// </summary>
    /// <param name="directoryElementList"></param>
    /// <param name="initiator"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static async Task GenericCreateImagePreviewForThumbnails(List<DirectoryElement> directoryElementList,
                                                                      Initiator initiator)
    {
        List<string> previewsToBeGeneratedByExifToolList = new();
        foreach (DirectoryElement directoryElement in directoryElementList)
        {
            if (directoryElement.Type != DirectoryElement.ElementType.File)
            {
                return;
            }

            string fileNameWithPath = directoryElement.FileNameWithPath;
            string fileNameWithoutPath = directoryElement.ItemNameWithoutPath;
            string generatedFileName = default;
            Image img = null;

            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            if (initiator == Initiator.FrmMainAppListViewThumbnail &&
                frmMainAppInstance != null)
            {
                generatedFileName = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                    path2: $"{fileNameWithoutPath}.jpg");
            }
            else
            {
                throw new Exception(message: "GenericCreateImagePreviewForThumbnails - What are you doing here?");
                return;
            }


            try
            {
                if (MagickExtensionList.Contains(item: directoryElement.Extension.TrimStart('.').ToLower()))
                {
                    // actually the reason i'm not using this for _every_ file is that it's just f...ing slow with NEF files(which is what I have plenty of), so it's prohibitive to run on RAW files. 
                    //  since i don't have a better way to deal with HEIC/WEBP files atm this is as good as it gets.
                    UseMagickImageToGeneratePreview(
                        originalImagePath: fileNameWithPath,
                        jpegPath: generatedFileName,
                        imgWidth: FileListView.ThumbnailSize,
                        imgHeight: FileListView.ThumbnailSize);
                }
                else
                {
                    CreateThumbnail(fileNameIn: fileNameWithPath,
                        fileNameOut: generatedFileName,
                        maxWidth: FileListView.ThumbnailSize,
                        maxHeight: FileListView.ThumbnailSize);
                }
            }
            catch
            {
                // nothing.
            }

            if (img == null &&
                !File.Exists(path: generatedFileName))
            {
                previewsToBeGeneratedByExifToolList.Add(item: fileNameWithPath);
            }


            //if (File.Exists(path: generatedFileName))
            //{
            //    try
            //    {
            //        // ReSharper disable once AssignNullToNotNullAttribute
            //        using FileStream stream = new(path: generatedFileName,
            //            mode: FileMode.Open,
            //            access: FileAccess.Read);
            //        img = Image.FromStream(stream: stream);
            //
            //        img.ExifRotate();
            //    }
            //    catch
            //    {
            //        // nothing
            //    }
            //}


            if (previewsToBeGeneratedByExifToolList.Any())
            {
                await ExifGetImagePreviewsForThumbnails(fileNamesWithPaths: previewsToBeGeneratedByExifToolList,
                    initiator: initiator);
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

    /// <summary>
    ///     Same as <see cref="UseMagickImageToGeneratePreview(string,string)" /> but specific to thumbnail creation.
    /// </summary>
    /// <param name="originalImagePath"></param>
    /// <param name="jpegPath"></param>
    /// <param name="imgWidth"></param>
    /// <param name="imgHeight"></param>
    private static void UseMagickImageToGeneratePreview(string originalImagePath,
                                                        string jpegPath,
                                                        int imgWidth,
                                                        int imgHeight)
    {
        using MagickImage image = new(fileName: originalImagePath);
        MagickGeometry size = new(width: imgWidth, height: imgHeight);

        image.AutoOrient();
        image.Resize(geometry: size);
        image.Write(fileName: jpegPath);
    }


    /// <summary>
    ///     Basically used for non-special files' thumb generation.
    ///     Via https://www.thatsoftwaredude.com/content/11478/how-to-resize-image-files-with-c
    /// </summary>
    /// <param name="fileNameIn"></param>
    /// <param name="fileNameOut"></param>
    /// <param name="maxWidth"></param>
    /// <param name="maxHeight"></param>
    private static void CreateThumbnail(string fileNameIn,
                                        string fileNameOut,
                                        int maxWidth,
                                        int maxHeight)
    {
        try
        {
            Image img = Bitmap.FromFile(filename: fileNameIn);
            string strName = string.Format(format: "thumb_{0}", arg0: fileNameOut);

            double newWidth = img.Width;
            double newHeight = img.Height;

            // finds the small dimensions
            for (int i = 99; i > 0; i--)
            {
                newWidth = i / 100.0 * newWidth;
                newHeight = i / 100.0 * newHeight;

                if (newWidth <= maxWidth ||
                    newHeight <= maxHeight)
                {
                    break;
                }
            }

            Bitmap bmSmall = new(original: img, newSize: new Size(width: (int)newWidth, height: (int)newHeight));

            // via https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-set-jpeg-compression-level?view=netframeworkdesktop-4.8
            Encoder myEncoder = Encoder.Quality;
            ImageCodecInfo jpgEncoder = GetEncoder(format: ImageFormat.Jpeg);

            EncoderParameters encoderParams = new(count: 1);
            EncoderParameter encoderParameter = new(encoder: myEncoder, value: 90L);
            encoderParams.Param[0] = encoderParameter;


            Image newImg = bmSmall;
            newImg.Save(filename: fileNameOut, encoder: jpgEncoder, encoderParams: encoderParams);
            img.Dispose();
            newImg.Dispose();
        }
        catch (Exception ex)
        {
            Debug.Print(message: ex.Message);
        }
    }

    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }

        return null;
    }
}