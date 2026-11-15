using NLog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     Installs ExifTool into the user's Roaming folder from a zip as published by ExifTool.org
///     (<c>exiftool-13.63_64.zip</c>: an <c>exiftool(-k).exe</c> launcher plus an <c>exiftool_files</c> folder).
///     Two callers: <see cref="EnsureBundledVersionInstalled" /> at startup (the app ships one such zip in
///     <c>Resources</c>, because ExifTool 12.x - the last single-file build - cannot open paths of 260 characters or
///     more, while 13.x can), and the close-time update in <c>FrmMainApp</c> (a newer zip downloaded in the
///     background).
/// </summary>
internal static class ExifToolInstaller
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Records which version the installed ExifTool is, so we do not have to start it just to ask. Lives inside
    ///     <c>exiftool_files</c> so it is deleted together with the files it describes.
    /// </summary>
    private const string VersionMarkerName = "gtn_version.txt";

    private const string FilesFolderName = "exiftool_files";
    private const string LauncherNameInZip = "exiftool(-k).exe";
    private const int VersionQueryTimeoutMs = 5000;

    private static readonly Regex ZipNameRegex = new(
        pattern: @"^exiftool-(?<ver>\d+\.\d+)_(?<bits>32|64)\.zip$",
        options: RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    ///     Reads the ExifTool version out of an official zip's file name (<c>exiftool-13.63_64.zip</c> is 13.63).
    /// </summary>
    /// <returns>The version, or null if the name does not follow the ExifTool.org pattern.</returns>
    internal static decimal? ParseZipVersion(string zipPath)
    {
        Match match = ZipNameRegex.Match(input: Path.GetFileName(path: zipPath));
        return match.Success &&
               decimal.TryParse(s: match.Groups["ver"].Value,
                   style: NumberStyles.AllowDecimalPoint,
                   provider: CultureInfo.InvariantCulture,
                   result: out decimal version)
            ? version
            : null;
    }

    /// <summary>
    ///     Startup: makes sure the ExifTool in the user folder is at least as new as the zip shipped in
    ///     <paramref name="resourcesFolder" />. Never throws; if anything goes wrong the caller falls back to the
    ///     packaged single-file <c>exiftool.exe</c> (or leaves whatever is installed alone).
    /// </summary>
    internal static void EnsureBundledVersionInstalled(string resourcesFolder,
                                                       string userDataFolder,
                                                       string installedExePath)
    {
        try
        {
            string bundledZip = FindBundledZip(resourcesFolder: resourcesFolder);
            decimal? bundledVersion = bundledZip == null ? null : ParseZipVersion(zipPath: bundledZip);
            if (bundledVersion == null)
            {
                return;
            }

            decimal installedVersion = GetInstalledVersion(
                userDataFolder: userDataFolder,
                installedExePath: installedExePath);

            if (installedVersion >= bundledVersion.Value)
            {
                return;
            }

            Log.Info(message: $"Installing bundled ExifTool {bundledVersion.Value.ToString(provider: CultureInfo.InvariantCulture)} " +
                              $"(installed: {installedVersion.ToString(provider: CultureInfo.InvariantCulture)}).");
            _ = ExtractZip(zipPath: bundledZip, userDataFolder: userDataFolder, installedExePath: installedExePath);
        }
        catch (Exception ex)
        {
            Log.Error(message: $"Could not install the bundled ExifTool: {ex.Message}");
        }
    }

    /// <summary>
    ///     Replaces the ExifTool in the user folder with the one in the zip. The zip is unpacked next to the
    ///     installation first, so a corrupt or truncated archive throws before anything of the old install is deleted.
    /// </summary>
    /// <returns>True if the new version is in place.</returns>
    internal static bool ExtractZip(string zipPath,
                                    string userDataFolder,
                                    string installedExePath)
    {
        string unpackedDir = Path.Combine(path1: userDataFolder,
            path2: Path.GetFileNameWithoutExtension(path: zipPath));
        string filesDir = Path.Combine(path1: userDataFolder, path2: FilesFolderName);

        try
        {
            // leftover from an interrupted earlier attempt
            if (Directory.Exists(path: unpackedDir))
            {
                Directory.Delete(path: unpackedDir, recursive: true);
            }

            // The zip contains a single top-level folder named like the zip itself.
            ZipFile.ExtractToDirectory(sourceArchiveFileName: zipPath, destinationDirectoryName: userDataFolder);

            string newLauncher = Path.Combine(path1: unpackedDir, path2: LauncherNameInZip);
            string newFilesDir = Path.Combine(path1: unpackedDir, path2: FilesFolderName);
            if (!File.Exists(path: newLauncher) || !Directory.Exists(path: newFilesDir))
            {
                throw new InvalidDataException(message: $"{Path.GetFileName(path: zipPath)} does not look like an ExifTool distribution.");
            }

            // Old install out, new one in. The exe is renamed from "exiftool(-k).exe": the "(-k)" in the name is what
            // makes ExifTool wait for a key press before exiting, which a background process must not do.
            File.Delete(path: installedExePath);
            if (Directory.Exists(path: filesDir))
            {
                Directory.Delete(path: filesDir, recursive: true);
            }

            Directory.Move(sourceDirName: newFilesDir, destDirName: filesDir);
            File.Move(sourceFileName: newLauncher, destFileName: installedExePath);

            decimal? version = ParseZipVersion(zipPath: zipPath);
            if (version != null)
            {
                File.WriteAllText(path: Path.Combine(path1: filesDir, path2: VersionMarkerName),
                    contents: version.Value.ToString(provider: CultureInfo.InvariantCulture));
            }

            return true;
        }
        catch (Exception ex)
        {
            // If the old install was already removed, the app reverts to the packaged exiftool.exe.
            Log.Error(message: $"ExifTool extraction failed: {ex.Message}");
            return false;
        }
        finally
        {
            try
            {
                if (Directory.Exists(path: unpackedDir))
                {
                    Directory.Delete(path: unpackedDir, recursive: true);
                }
            }
            catch
            {
                // harmless: FsoCleanUpUserFolder removes stray folders
            }
        }
    }

    /// <summary>The highest-versioned <c>exiftool-x.yy_64.zip</c> in the folder, or null if there is none.</summary>
    private static string FindBundledZip(string resourcesFolder)
    {
        if (!Directory.Exists(path: resourcesFolder))
        {
            return null;
        }

        return Directory.EnumerateFiles(path: resourcesFolder, searchPattern: "exiftool-*_64.zip")
                        .Select(selector: path => (path, version: ParseZipVersion(zipPath: path)))
                        .Where(predicate: item => item.version != null)
                        .OrderByDescending(keySelector: item => item.version)
                        .Select(selector: item => item.path)
                        .FirstOrDefault();
    }

    /// <summary>
    ///     0 if nothing is installed. Uses the marker file; for an install that has none (ExifTool put there by an
    ///     earlier GeoTagNinja's auto-update) asks the exe once and then writes the marker.
    /// </summary>
    internal static decimal GetInstalledVersion(string userDataFolder,
                                               string installedExePath)
    {
        if (!File.Exists(path: installedExePath))
        {
            return 0;
        }

        string markerPath = Path.Combine(path1: userDataFolder, path2: FilesFolderName, path3: VersionMarkerName);
        if (File.Exists(path: markerPath) &&
            decimal.TryParse(s: File.ReadAllText(path: markerPath).Trim(),
                style: NumberStyles.AllowDecimalPoint,
                provider: CultureInfo.InvariantCulture,
                result: out decimal fromMarker))
        {
            return fromMarker;
        }

        decimal fromExe = QueryVersion(exePath: installedExePath);
        if (fromExe > 0 && Directory.Exists(path: Path.GetDirectoryName(path: markerPath)))
        {
            File.WriteAllText(path: markerPath, contents: fromExe.ToString(provider: CultureInfo.InvariantCulture));
        }

        return fromExe;
    }

    /// <summary>Runs <c>exiftool -ver</c>. Returns 0 if it does not answer in time or says something unexpected.</summary>
    private static decimal QueryVersion(string exePath)
    {
        try
        {
            using Process process = Process.Start(startInfo: new ProcessStartInfo(fileName: exePath, arguments: "-ver")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            });
            if (process == null)
            {
                return 0;
            }

            process.StandardInput.Close();
            if (!process.WaitForExit(milliseconds: VersionQueryTimeoutMs))
            {
                process.Kill();
                return 0;
            }

            return decimal.TryParse(s: process.StandardOutput.ReadToEnd().Trim(),
                style: NumberStyles.AllowDecimalPoint,
                provider: CultureInfo.InvariantCulture,
                result: out decimal version)
                ? version
                : 0;
        }
        catch
        {
            return 0;
        }
    }
}
