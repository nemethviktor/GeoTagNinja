using System.Collections.Generic;
using System.Linq;

namespace GeoTagNinja.Helpers.FileSystem;

/// <summary>
///     Which file extensions GeoTagNinja will open, which of them carry an XMP sidecar, and which count as
///     track files.
/// </summary>
internal static class SupportedFileExtensions
{
    /// <summary>
    ///     this one basically handles what extensions we work with.
    ///     the actual list is used for file-specific Settings as well as the general running of the app
    ///     leave the \t in!
    /// </summary>
    internal static string[] AllCompatibleExtensions()
    {
        string[] result =
        {
            "360	GoPro 360 video (MP4-based, experimental)",
            "3g2	3GPP2 video (QuickTime-based, experimental)",
            "3gp	3GPP video (QuickTime-based, experimental)",
            "arq	Sony Alpha Pixel-Shift RAW (TIFF-based)",
            "arw	Sony Alpha RAW (TIFF-based)",
            "avif	AV1 Image File Format (QuickTime-based)",
            "cr2	Canon RAW 2 (TIFF-based) (CR2 spec)",
            "cr3	Canon RAW 3 (QuickTime-based) (CR3 spec)",
            "crm	Canon Cinema RAW Light (QuickTime-based)",
            "crw	Canon RAW (CIFF)",
            "dcp	DNG Camera Profile (DNG-like)",
            "dng	Digital Negative (TIFF-based)",
            "erf	Epson RAW Format (TIFF-based)",
            "exv	Exiv2 metadata file (JPEG-based)",
            "fff	Hasselblad Flexible File Format (TIFF-based)",
            "gpr	GoPro RAW (DNG-based)",
            "hdp	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)",
            "heic	High Efficiency Image Format (QuickTime-based)",
            "heif	High Efficiency Image Format (QuickTime-based)",
            "hif	High Efficiency Image Format (QuickTime-based)",
            "iiq	Phase One Intelligent Image Quality RAW (TIFF-based)",
            "insp	Insta360 Picture (JPEG-based)",
            "jp2	JPEG 2000 image [Compound/Extended]",
            "jpe	Joint Photographic Experts Group image",
            "jpeg	Joint Photographic Experts Group image",
            "jpf	JPEG 2000 image [Compound/Extended]",
            "jpg	Joint Photographic Experts Group image",
            "jpm	JPEG 2000 image [Compound/Extended]",
            "jpx	JPEG 2000 image [Compound/Extended]",
            "jxl	JPEG XL (codestream and ISO BMFF)",
            "jxr	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)",
            "lrv	GoPro low-resolution video (MP4-based, experimental)",
            "m4v	iTunes video (MPEG-4, experimental)",
            "mef	Mamiya (RAW) Electronic Format (TIFF-based)",
            "mie	Meta Information Encapsulation (MIE specification)",
            "mos	Creo Leaf Mosaic (TIFF-based)",
            "mov	Apple QuickTime movie (experimental)",
            "mp4	MPEG-4 video (experimental)",
            "mpo	Extended Multi-Picture format (JPEG with MPF extensions)",
            "mrw	Minolta RAW",
            "nef	Nikon (RAW) Electronic Format (TIFF-based)",
            "nrw	Nikon RAW (2) (TIFF-based)",
            "orf	Olympus RAW Format (TIFF-based)",
            "ori	Olympus RAW Format (TIFF-based)",
            "pef	Pentax (RAW) Electronic Format (TIFF-based)",
            "png	Portable Network Graphics",
            "psb	Photoshop Large Document",
            "psd	Photoshop Document",
            "raf	FujiFilm RAW Format",
            "raw	Kyocera Contax N Digital RAW",
            "rw2	Panasonic RAW 2 (TIFF-based)",
            "rwl	Leica RAW (TIFF-based)",
            "sr2	Sony RAW 2 (TIFF-based)",
            "srw	Samsung RAW format (TIFF-based)",
            "thm	Thumbnail image (JPEG)",
            "tif	QuickTime Image File",
            "tiff	Tagged Image File Format",
            "wdp	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)",
            "webp	WebP Image File",
            "x3f	Sigma/Foveon RAW"
        };
        return result;
    }

    /// <summary>
    ///     The video extensions in <see cref="AllCompatibleExtensions" />. They are listed and read like photos (their
    ///     location and dates come from the QuickTime atoms), but only their location can be written, by
    ///     <see cref="Exif.VideoLocationArguments" />: ExifTool silently drops the EXIF/GPS tags written for photos when
    ///     the target is an MP4/MOV, so a generic save would claim success and change nothing a player can see.
    /// </summary>
    private static readonly HashSet<string> VideoExtensions = new(comparer: System.StringComparer.OrdinalIgnoreCase)
    {
        "360", "3g2", "3gp", "lrv", "m4v", "mov", "mp4"
    };

    /// <summary>
    ///     Whether the file is a video (see <see cref="VideoExtensions" />).
    /// </summary>
    /// <param name="fileNameOrExtension">A file name, path or bare extension, with or without the leading dot.</param>
    internal static bool IsVideoFile(string fileNameOrExtension)
    {
        if (string.IsNullOrWhiteSpace(value: fileNameOrExtension))
        {
            return false;
        }

        string extension = fileNameOrExtension.Contains(value: ".")
            ? System.IO.Path.GetExtension(path: fileNameOrExtension)
            : fileNameOrExtension;

        return VideoExtensions.Contains(item: extension.TrimStart('.'));
    }

    /// <summary>
    ///     List of extensions that take an XMP sidecar
    /// </summary>
    internal static string[] FileExtensionsThatUseXMP()
    {
        List<string> retList = [];
        foreach (string extension in AllCompatibleExtensions())
        {
            if (extension.ToLower()
                         .Contains(value: "raw") ||
                extension.ToLower()
                         .Contains(value: "tiff"))
            {
                retList.Add(item: extension.Split('\t')
                                           .FirstOrDefault()
                                          ?.Trim());
            }
        }

        return retList.ToArray();
    }

    /// <summary>
    ///     Extracts only the file name extensions from the list of
    ///     AllCompatibleExtensions, leaving out the video ones unless the user has switched video support on
    ///     (<see cref="HelperVariables.UserSettingEnableVideoFiles" />). The returned array is a copy and
    ///     can be used freely.
    /// </summary>
    /// <returns>An array of file extensions supported</returns>
    internal static string[] AllCompatibleExtensionsExt()
    {
        string[] allowedExtensions = AllCompatibleExtensions();

        // List contains the extension at then beginning and
        // after white space more description --> loop
        // to get only the extensions
        for (int i = 0; i < allowedExtensions.Length; i++)
        {
            allowedExtensions[i] = allowedExtensions[i]
                                  .Split('\t')
                                  .FirstOrDefault()
                                 ?.Trim();
        }

        return HelperVariables.UserSettingEnableVideoFiles
            ? allowedExtensions
            : allowedExtensions.Where(predicate: extension => !IsVideoFile(fileNameOrExtension: extension))
                               .ToArray();
    }

    /// <summary>
    ///     List of supported sidecar file extensions.
    ///     The extension must be in lower case due to its use in comparisons!
    ///     Dictionary Extension -> Description
    /// </summary>
    private static IDictionary<string, string> SideCarExtensions()
    {
        IDictionary<string, string> result = new Dictionary<string, string>
        {
            { "xmp", "XMP SideCar File" }
        };
        return result;
    }

    /// <summary>
    ///     Returns an array of extensions (string) of compatible sidecar files.
    ///     The returned array is a copy and can be used freely.
    /// </summary>
    internal static string[] GetSideCarExtensionsArray()
    {
        return SideCarExtensions()
              .Keys.ToArray();
    }

    internal static string[] GpxExtensions()
    {
        string[] result =
        [
            "gpx", // GPX	
            "nmea", // NMEA (RMC, GGA, GLL and GSA sentences)	
            "kml", // KML	
            "igc", // IGC (glider format)	
            "xml", "tcx", // Garmin XML and TCX	
            "log", // Magellan eXplorist PMGNTRK + // Honeywell PTNTHPR + // Bramor gEO log	
            "txt", // Winplus Beacon .TXT	
            "json", // Google Takeout .JSON	
            "csv" // GPS/IMU .CSV + // DJI .CSV + // ExifTool .CSV file	
        ];

        return result;
    }
}
