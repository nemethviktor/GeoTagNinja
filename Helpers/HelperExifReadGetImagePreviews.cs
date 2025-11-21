using GeoTagNinja.Model;
using ImageMagick;
using Sdcb.LibRaw;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Encoder = System.Drawing.Imaging.Encoder;

namespace GeoTagNinja.Helpers;

internal static class HelperExifReadGetImagePreviews
{
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
    ///     user when they click on a filename in whichever listview.
    /// </summary>
    /// <param name="fileNameWithPath">Path of file for which the preview needs creating</param>
    /// <param name="initiator">The source of the caller. This can drives whether we're asking for a preview or a thumbnail</param>
    /// <param name="addSmallThumbnailToFileName">Whether to add "_small_thumbnail" to the filename. (defaults to false)</param>
    /// <returns>Realistically nothing but the process generates the bitmap if possible</returns>
    internal static async Task UseExifToolToGeneratePreviewsOrThumbnails(string fileNameWithPath,
                                                                         Initiator initiator,
                                                                         bool addSmallThumbnailToFileName = false)
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
            $@" -charset utf8 -charset filename=utf8 -b -preview:{(initiator == Initiator.FrmMainAppListViewThumbnail ? "GTNPreviewThumb" : "GTNPreview")} -w! {HelperVariables.DoubleQuoteStr}{HelperVariables.UserDataFolderPath}\%F{(addSmallThumbnailToFileName ? "_small_thumbnail" : "")}.jpg{HelperVariables.DoubleQuoteStr} -@ {HelperVariables.DoubleQuoteStr}{argsFile}{HelperVariables.DoubleQuoteStr}";

        File.Delete(path: argsFile);

        #endregion

        // add required tags

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

        if (!File.Exists(path: generatedFileName))
        {
            try
            {
                if (MagickExtensionList.Contains(item: directoryElement.Extension.TrimStart('.').ToLower()))
                {
                    // actually the reason i'm not using this for _every_ file is that it's just f...ing slow with NEF files(which is what I have plenty of), so it's prohibitive to run on RAW files. 
                    //  since i don't have a better way to deal with HEIC/WEBP files atm this is as good as it gets.
                    UseMagickImageToGeneratePreview(originalImagePath: fileNameWithPath,
                        generatedJpegPath: generatedFileName);
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
        }

        if (img == null)
        {
            // don't run the thing again if file has already been generated
            if (!File.Exists(path: generatedFileName))
            {
                await UseExifToolToGeneratePreviewsOrThumbnails(fileNameWithPath: fileNameWithPath,
                    initiator: initiator);
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
                    await UseExifToolToGeneratePreviewsOrThumbnails(fileNameWithPath: fileNameWithPath,
                        initiator: initiator);
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
    ///     Takes a HEIC/WebP/etc file and dumps a JPG. Uses MagickImage. See comment above as to why this isn't being used for
    ///     all RAW
    ///     files. (Too slow).
    /// </summary>
    /// <param name="originalImagePath"></param>
    /// <param name="generatedJpegPath"></param>
    private static void UseMagickImageToGeneratePreview(string originalImagePath,
                                                        string generatedJpegPath)
    {
        using MagickImage image = new(fileName: originalImagePath);
        // The AutoOrient() method adjusts the image to respect its orientation.
        image.AutoOrient();
        // Save the image as a JPEG.
        image.Write(fileName: generatedJpegPath);
    }

    /// <summary>
    ///     Use LibRaw to create thumbnail. Does not always rotate images properly.
    /// </summary>
    /// <param name="originalImagePath"></param>
    /// <param name="generatedJpegPath"></param>
    /// <param name="thumbnailIndex">
    ///     The idea here is that not all raw images have a 0 index thumbnail so forcing a zero is
    ///     likely to fail
    /// </param>
    internal static void UseLibRawToGenerateThumbnail(string originalImagePath,
                                                      string generatedJpegPath,
                                                      int thumbnailIndex)
    {
        try
        {
            using RawContext r = RawContext.OpenFile(filePath: originalImagePath);
            using ProcessedImage image = r.ExportThumbnail(thumbnailIndex: thumbnailIndex);

            using Bitmap bmp =
                (Bitmap)Image.FromStream(stream: new MemoryStream(buffer: image.AsSpan<byte>().ToArray()));
            bmp.Save(filename: generatedJpegPath, format: ImageFormat.Jpeg);
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    ///     Use LibRaw to pull the full image.
    /// </summary>
    /// <param name="originalImagePath"></param>
    /// <param name="generatedJpegPath"></param>
    /// <param name="imgWidth"></param>
    /// <param name="imgHeight"></param>
    internal static void UseLibRawToGenerateFullImage(string originalImagePath,
                                                      string generatedJpegPath,
                                                      int? imgWidth = null,
                                                      int? imgHeight = null)
    {
        try
        {
            using RawContext r = RawContext.OpenFile(filePath: originalImagePath);
            r.Unpack();
            r.DcrawProcess();
            using ProcessedImage image = r.MakeDcrawMemoryImage();

            Bitmap bmp = ProcessedImageToBitmap(rgbImage: image);

            Bitmap ProcessedImageToBitmap(ProcessedImage rgbImage)
            {
                rgbImage.SwapRGB();
                int witdthToUse = imgWidth ?? rgbImage.Width;
                int heightToUse = imgHeight ?? rgbImage.Height;
                using Bitmap bmpNewBitmap = new(width: witdthToUse, height: heightToUse, stride: rgbImage.Width * 3,
                    format: PixelFormat.Format24bppRgb, scan0: rgbImage.DataPointer);
                bmpNewBitmap.Save(filename: generatedJpegPath, format: ImageFormat.Jpeg);
                return null;
            }
        }
        catch

        {
            // ignored
        }
    }


    /// <summary>
    ///     Same as <see cref="UseMagickImageToGeneratePreview(string,string)" /> but specific to thumbnail creation.
    /// </summary>
    /// <param name="originalImagePath"></param>
    /// <param name="generatedJpegPath"></param>
    /// <param name="imgWidth"></param>
    /// <param name="imgHeight"></param>
    internal static void UseMagickImageToGeneratePreview(string originalImagePath,
                                                         string generatedJpegPath,
                                                         int imgWidth,
                                                         int imgHeight)
    {
        using MagickImage image = new(fileName: originalImagePath);
        MagickGeometry size = new(width: (uint)imgWidth, height: (uint)imgHeight);

        image.AutoOrient();
        image.Resize(geometry: size);
        image.Write(fileName: generatedJpegPath);
    }


    /// <summary>
    ///     Basically used for non-special files' thumb generation. (Using Windows's own logic/capabilities.)
    ///     Via https://www.thatsoftwaredude.com/content/11478/how-to-resize-image-files-with-c
    /// </summary>
    /// <param name="fileNameIn"></param>
    /// <param name="fileNameOut"></param>
    /// <param name="maxWidth"></param>
    /// <param name="maxHeight"></param>
    internal static void UseWindowsImageHandlerToCreateThumbnail(string fileNameIn,
                                                                 string fileNameOut,
                                                                 int maxWidth,
                                                                 int maxHeight)
    {
        try
        {
            Image img = Image.FromFile(filename: fileNameIn);
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