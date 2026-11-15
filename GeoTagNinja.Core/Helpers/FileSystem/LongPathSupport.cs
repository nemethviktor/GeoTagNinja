using GeoTagNinja.Helpers.Exif;
using NLog;
using System;
using System.IO;

namespace GeoTagNinja.Helpers.FileSystem;

/// <summary>
///     Whether this app can work with paths of 260 characters or more. Two things must both be true: Windows (plus this
///     app's <c>longPathAware</c> manifest) lets .NET open them, and the ExifTool in use can read and write them (13.x
///     can; the single-file 12.x the app used to ship cannot).
/// </summary>
internal static class LongPathSupport
{
    /// <summary>The classic MAX_PATH limit, less the terminating null.</summary>
    internal const int ClassicMaxPathLength = 259;

    /// <summary>Windows' limit for extended-length paths (32,767 characters), less a little headroom.</summary>
    internal const int ExtendedMaxPathLength = 32000;

    /// <summary>The first ExifTool version that can open paths over 259 characters.</summary>
    private const decimal FirstExifToolWithLongPaths = 13.0m;

    private const int ProbeLength = 270;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Probed once, on first use: a handful of file operations in the temp folder.
    private static readonly Lazy<int> s_maxPathLength = new(valueFactory: DetermineMaxPathLength);

    /// <summary>The longest full path the app should let a user create (for example a rename target).</summary>
    internal static int MaxPathLength => s_maxPathLength.Value;

    /// <summary>
    ///     Tries it for real: creates a directory whose full path is over 260 characters under the temp folder, checks it,
    ///     and removes it again. This covers the registry switch (<c>LongPathsEnabled</c>), group policy and the app
    ///     manifest at once, which reading the registry value alone would not.
    /// </summary>
    internal static bool FileSystemAllowsLongPaths()
    {
        string root = Path.Combine(path1: Path.GetTempPath(), path2: "gtn_lp_" + Guid.NewGuid().ToString(format: "N"));
        try
        {
            string deep = root;
            while (deep.Length < ProbeLength)
            {
                deep = Path.Combine(path1: deep, path2: new string(c: 'd', count: 40));
            }

            _ = Directory.CreateDirectory(path: deep);
            return Directory.Exists(path: deep);
        }
        catch (Exception ex)
        {
            Log.Info(message: $"Long paths are not available: {ex.GetType().Name}");
            return false;
        }
        finally
        {
            try
            {
                if (Directory.Exists(path: root))
                {
                    Directory.Delete(path: root, recursive: true);
                }
            }
            catch
            {
                // a stray empty folder in %TEMP% is harmless
            }
        }
    }

    private static int DetermineMaxPathLength()
    {
        // Only a Roaming install carries a known version; the packaged fallback exiftool.exe is 12.x.
        bool exifToolIsNewEnough =
            HelperVariables.ExifToolExePathToUse == HelperVariables.ExifToolExePathRoamingPerm &&
            ExifToolInstaller.GetInstalledVersion(
                userDataFolder: HelperVariables.UserDataFolderPath,
                installedExePath: HelperVariables.ExifToolExePathRoamingPerm) >= FirstExifToolWithLongPaths;

        bool result = exifToolIsNewEnough && FileSystemAllowsLongPaths();
        Log.Info(message: $"Long paths: ExifTool new enough = {exifToolIsNewEnough}, usable = {result}.");
        return result ? ExtendedMaxPathLength : ClassicMaxPathLength;
    }
}
