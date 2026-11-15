using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     Hands Core's SkiaSharp pictures to WinForms (System.Drawing).
/// </summary>
internal static class SkiaBridge
{
    /// <summary>
    ///     Copies a SkiaSharp bitmap into a new System.Drawing bitmap that owns its pixels. The source is not disposed.
    /// </summary>
    internal static Bitmap ToBitmap(SKBitmap source)
    {
        SKImageInfo info = new(width: source.Width, height: source.Height, colorType: SKColorType.Bgra8888,
            alphaType: SKAlphaType.Premul);
        using SKBitmap converted = source.ColorType == SKColorType.Bgra8888 && source.AlphaType == SKAlphaType.Premul
            ? null
            : source.Copy(colorType: SKColorType.Bgra8888);
        SKBitmap pixels = converted ?? source;

        Bitmap bitmap = new(width: info.Width, height: info.Height, format: PixelFormat.Format32bppPArgb);
        BitmapData data = bitmap.LockBits(rect: new Rectangle(x: 0, y: 0, width: info.Width, height: info.Height),
            flags: ImageLockMode.WriteOnly, format: PixelFormat.Format32bppPArgb);
        try
        {
            int rowBytes = info.Width * 4;
            IntPtr sourcePointer = pixels.GetPixels();
            for (int row = 0; row < info.Height; row++)
            {
                byte[] line = new byte[rowBytes];
                Marshal.Copy(source: IntPtr.Add(pointer: sourcePointer, offset: row * pixels.RowBytes), destination: line,
                    startIndex: 0, length: rowBytes);
                Marshal.Copy(source: line, startIndex: 0, destination: IntPtr.Add(pointer: data.Scan0, offset: row * data.Stride),
                    length: rowBytes);
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapdata: data);
        }

        return bitmap;
    }
}
