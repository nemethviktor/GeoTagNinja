using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using GeoTagNinja.View.Controls;
using GeoTagNinja.View.Forms;
using ImageMagick;
using Sdcb.LibRaw;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Encoder = System.Drawing.Imaging.Encoder;

namespace GeoTagNinja.Helpers.Exif;

internal static class ReadGetImagePreviews
{

    /// <summary>The newest request per picture box, so an older one can be cancelled when it is superseded.</summary>
    private static readonly ConditionalWeakTable<PictureBox, CancellationTokenSource> PendingRequests = new();

    /// <summary>Limits concurrent preview decodes.</summary>
    private static readonly SemaphoreSlim DecodeGate = new(initialCount: 2);

    internal enum Initiator
    {
        FrmMainAppPictureBox,
        FrmMainAppAPIDataSelection,
        FrmEditFileData
    }

    /// <summary>
    ///     Loads and shows the preview of the DirectoryElement selected. The image is produced off the UI thread, in
    ///     memory (see <see cref="LoadPreviewInMemory" />); only if that fails is the already-running ExifTool asked.
    ///     A newer request for the same picture box supersedes (and discards the result of) an older one, so arrowing
    ///     quickly through files never shows a stale preview or queues up work.
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

        PictureBox? target = initiator switch
        {
            Initiator.FrmMainAppPictureBox or Initiator.FrmMainAppAPIDataSelection => FrmMainApp.Instance?.pbx_imagePreview,
            Initiator.FrmEditFileData => (Application.OpenForms[name: "FrmEditFileData"] as FrmEditFileData)?.pbx_imagePreview,
            _ => null
        };

        if (target == null)
        {
            return;
        }

        CancellationTokenSource request = new();
        CancellationTokenSource? superseded;
        lock (PendingRequests)
        {
            if (PendingRequests.TryGetValue(key: target, value: out superseded))
            {
                _ = PendingRequests.Remove(key: target);
            }

            PendingRequests.Add(key: target, value: request);
        }

        superseded?.Cancel();

        // dispose what we're replacing; it is ours (created below), and the control no longer holds it afterwards.
        Image? previous = target.Image;
        target.Image = null;
        previous?.Dispose();

        string fileNameWithPath = directoryElement.FileNameWithPath;
        string extension = directoryElement.Extension.TrimStart('.').ToLowerInvariant();
        Image? img = null;

        try
        {
            // Only a couple of decodes at a time; requests superseded while waiting here never start.
            await DecodeGate.WaitAsync(cancellationToken: request.Token);
            try
            {
                img = await Task.Run(function: () => LoadPreviewInMemory(fileNameWithPath: fileNameWithPath,
                    extension: extension), cancellationToken: request.Token);
            }
            finally
            {
                _ = DecodeGate.Release();
            }

            if (img == null &&
                !request.IsCancellationRequested)
            {
                img = await LoadPreviewViaExifTool(directoryElement: directoryElement);
            }
        }
        catch (OperationCanceledException)
        {
            // superseded
        }

        if (request.IsCancellationRequested)
        {
            img?.Dispose();
            return;
        }

        if (img != null)
        {
            target.Image = img;
        }
        else if (initiator == Initiator.FrmMainAppPictureBox)
        {
            // sometimes no preview can be had at all.
            (target as ImagePreview)?.SetErrorMessage(message: HelperControlAndMessageBoxHandling.ReturnControlText(
                controlName: "pbx_imagePreviewCouldNotRetrieve",
                fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.PictureBox));
        }
    }

    /// <summary>
    ///     Produces the preview without any external process: plain images are decoded directly, RAW files give up
    ///     their largest embedded JPEG (via LibRaw), HEIC/WebP are converted by Magick. The slow-to-produce kinds are
    ///     kept in <see cref="PreviewCache" />. Runs on a worker thread.
    /// </summary>
    /// <returns>The image (caller owns it), or null if nothing in-process could read the file.</returns>
    private static Image? LoadPreviewInMemory(string fileNameWithPath,
                                              string extension)
    {
        try
        {
            if (ThumbnailGenerator.IsGdiPlusFormat(extension: extension))
            {
                using FileStream stream = new(path: fileNameWithPath, mode: FileMode.Open, access: FileAccess.Read,
                    share: FileShare.ReadWrite);
                using Image decoded = Image.FromStream(stream: stream);
                decoded.ExifRotate();
                // copy so the result doesn't depend on the file stream
                return new Bitmap(original: decoded);
            }

            PreviewEntry? entry = PreviewCache.TryGet(fileNameWithPath: fileNameWithPath);
            if (entry == null)
            {
                entry = ThumbnailGenerator.IsMagickOnlyFormat(extension: extension)
                    ? ReadMagickAsJpeg(fileNameWithPath: fileNameWithPath)
                    : RawPreview.TryReadLargestEmbeddedJpeg(fileNameWithPath: fileNameWithPath);

                if (entry != null)
                {
                    PreviewCache.Add(fileNameWithPath: fileNameWithPath, entry: entry);
                }
            }

            return entry == null
                ? null
                : RawPreview.DecodeOriented(jpeg: entry.Jpeg, exifOrientation: entry.ExifOrientation);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     HEIC/WebP etc: decode with Magick, apply orientation, hand back as JPEG bytes (so the result can be cached
    ///     cheaply). The orientation is already baked in, hence 0.
    /// </summary>
    private static PreviewEntry? ReadMagickAsJpeg(string fileNameWithPath)
    {
        try
        {
            using MagickImage image = new(fileName: fileNameWithPath);
            image.AutoOrient();
            image.Quality = 92;
            return new PreviewEntry(jpeg: image.ToByteArray(format: MagickFormat.Jpeg), exifOrientation: 0);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Last resort for files LibRaw/Magick/GDI+ couldn't read: ask the already-running ExifTool process for an
    ///     embedded preview (see <see cref="ExifTool.TryGetEmbeddedPreview" />). No new process is started.
    /// </summary>
    private static Task<Image?> LoadPreviewViaExifTool(DirectoryElement directoryElement)
    {
        return Task.Run<Image?>(function: () =>
        {
            ExifTool? exifTool = FrmMainApp.DirectoryElements.ExifTool;
            if (exifTool == null ||
                !exifTool.TryGetEmbeddedPreview(filename: directoryElement.FileNameWithPath, jpeg: out byte[] jpeg,
                    exifOrientation: out int exifOrientation))
            {
                return null;
            }

            try
            {
                return RawPreview.DecodeOriented(jpeg: jpeg, exifOrientation: exifOrientation);
            }
            catch
            {
                return null;
            }
        });
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
}
