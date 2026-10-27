using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace GeoTagNinja.View.Controls;

/// <summary>
///     The pictures for folders, drives and the "up" entry in the thumbnail view, loaded once from the app's
///     <c>images</c> folder and shared. (They used to be copied into every element as its "thumbnail".)
/// </summary>
internal static class FolderIcons
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, Image?> Loaded = [];

    /// <summary>The shared icon for a non-file element, or null if its image file is missing. Do not dispose it.</summary>
    internal static Image? For(DirectoryElement directoryElement)
    {
        string fileName = directoryElement.Type switch
        {
            DirectoryElement.ElementType.SubDirectory => "Folder.png",
            DirectoryElement.ElementType.ParentDirectory => "Parentfolder.png",
            DirectoryElement.ElementType.MyComputer => "Computer.png",
            DirectoryElement.ElementType.Drive => DriveIconName(driveName: directoryElement.FileNameWithPath),
            _ => "Unknown.png"
        };

        lock (Gate)
        {
            if (!Loaded.TryGetValue(key: fileName, value: out Image? image))
            {
                string fullPath = Path.Combine(path1: AppDomain.CurrentDomain.BaseDirectory, path2: "images", path3: fileName);
                image = File.Exists(path: fullPath)
                    ? Image.FromFile(filename: fullPath)
                    : null;
                Loaded[key: fileName] = image;
            }

            return image;
        }
    }

    /// <summary>
    ///     The icon file for a drive, by <see cref="DriveType" />. (These were exported from Windows by hand; pulling them
    ///     from the shell at runtime interferes with the app's high-DPI handling, which is why they are files.)
    /// </summary>
    private static string DriveIconName(string driveName)
    {
        try
        {
            return new DriveInfo(driveName: driveName).DriveType switch
            {
                DriveType.Removable => "Removabledrive.png",
                DriveType.Fixed => "Harddrive.png",
                DriveType.Network => "Networkdrive.png",
                DriveType.CDRom => "CDdrive.png",
                _ => "Otherdrive.png"
            };
        }
        catch
        {
            return "Otherdrive.png";
        }
    }
}
