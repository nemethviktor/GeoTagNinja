using GeoTagNinja.Helpers;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     Turns a System.Drawing image upright by its EXIF orientation (the WinForms preview path; Core does the same
///     with SkiaSharp in <c>ImageOps</c>).
/// </summary>
internal static class ImageExifRotation
{
    /// <summary>
    /// Corrects the orientation of the specified image based on its EXIF orientation data.
    /// </summary>
    /// <remarks>If the image does not contain EXIF orientation data, no changes are made. The method modifies
    /// the image in place and removes the EXIF orientation property after rotation.</remarks>
    /// <param name="img">The image to be rotated. This parameter must not be null and should contain EXIF orientation data for the method
    /// to perform any rotation.</param>
    internal static void ExifRotate(this Image img)
    {
        // via https://stackoverflow.com/a/48347653/3968494
        if (!img.PropertyIdList.Contains(value: HelperVariables.exifOrientationID))
        {
            return;
        }

        PropertyItem prop =
            img.GetPropertyItem(propid: HelperVariables.exifOrientationID);
        int val = BitConverter.ToUInt16(value: prop.Value, startIndex: 0);
        RotateFlipType rot = RotateFlipType.RotateNoneFlipNone;

        if (val is 3 or
            4)
        {
            rot = RotateFlipType.Rotate180FlipNone;
        }
        else if (val is 5 or
                 6)
        {
            rot = RotateFlipType.Rotate90FlipNone;
        }
        else if (val is 7 or
                 8)
        {
            rot = RotateFlipType.Rotate270FlipNone;
        }

        if (val is 2 or
            4 or
            5 or
            7)
        {
            rot |= RotateFlipType.RotateNoneFlipX;
        }

        if (rot != RotateFlipType.RotateNoneFlipNone)
        {
            img.RotateFlip(rotateFlipType: rot);
            img.RemovePropertyItem(propid: HelperVariables.exifOrientationID);
        }
    }
}
