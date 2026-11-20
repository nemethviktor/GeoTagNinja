using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     The picture Windows Explorer would show for a file, asked of the shell (<c>IShellItemImageFactory</c>). It is
///     the app's fallback for a video when no ffmpeg is set up: Windows decodes the frame, so it only works for the
///     codecs Windows has (H.264 everywhere; HEVC/H.265, which many phones record, only with Microsoft's codec).
/// </summary>
internal static class ShellThumbnail
{
    // THUMBNAILONLY: fail when there is no real thumbnail, rather than hand back the generic file icon (the app draws
    // its own video symbol in that case).
    private const int SiigbfThumbnailOnly = 0x8;

    private static readonly Guid ImageFactoryId = new(g: "bcc18b79-ba16-442f-80c4-8a59c30c463b");

    [StructLayout(layoutKind: LayoutKind.Sequential)]
    private struct Size
    {
        public int Width;
        public int Height;
    }

    [ComImport]
    [Guid(guid: "bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(interfaceType: ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(Size size,
                     int flags,
                     out IntPtr bitmapHandle);
    }

    [DllImport(dllName: "shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName([MarshalAs(unmanagedType: UnmanagedType.LPWStr)] string path,
                                                           IntPtr bindContext,
                                                           ref Guid interfaceId,
                                                           [MarshalAs(unmanagedType: UnmanagedType.Interface)] out IShellItemImageFactory factory);

    [DllImport(dllName: "gdi32.dll")]
    private static extern bool DeleteObject(IntPtr objectHandle);

    /// <summary>
    ///     Windows' thumbnail of the file, its long edge at most <paramref name="size" /> pixels, or null when Windows
    ///     has none (an unknown codec, an unreadable file).
    /// </summary>
    /// <returns>The bitmap, which the caller owns.</returns>
    internal static SKBitmap? TryGet(string path,
                                     int size)
    {
        IntPtr handle = IntPtr.Zero;
        try
        {
            Guid id = ImageFactoryId;
            SHCreateItemFromParsingName(path: path, bindContext: IntPtr.Zero, interfaceId: ref id, factory: out IShellItemImageFactory factory);
            int result = factory.GetImage(size: new Size { Width = size, Height = size }, flags: SiigbfThumbnailOnly, bitmapHandle: out handle);
            if (result != 0 ||
                handle == IntPtr.Zero)
            {
                return null;
            }

            using Bitmap picture = Image.FromHbitmap(hbitmap: handle);
            using MemoryStream stream = new();
            picture.Save(stream: stream, format: ImageFormat.Png);
            using SKBitmap decoded = SKBitmap.Decode(buffer: stream.ToArray());
            return decoded == null
                ? null
                : Exif.ImageOps.FitWithin(source: decoded, maxEdge: size);
        }
        catch (Exception)
        {
            // no shell thumbnail for this file; the caller shows its own symbol
            return null;
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                _ = DeleteObject(objectHandle: handle);
            }
        }
    }
}
