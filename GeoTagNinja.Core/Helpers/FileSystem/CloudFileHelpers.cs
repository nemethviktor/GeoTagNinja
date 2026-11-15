using System.IO;

namespace GeoTagNinja.Helpers.FileSystem;

public static class CloudFileHelpers
{
    // Attributes defined in Windows API:
    // FILE_ATTRIBUTE_OFFLINE = 0x1000
    // FILE_ATTRIBUTE_RECALL_ON_OPEN = 0x400000
    private const FileAttributes RecallOnOpenAttribute = (FileAttributes)0x400000;

    /// <summary>
    /// Checks whether a file exists purely in the cloud and accessing it will trigger a download.
    /// </summary>
    public static bool IsCloudOnlyPlaceholder(FileInfo fileInfo)
    {
        if (!fileInfo.Exists) return false;

        // Refresh file info to pull the latest attributes from OS
        fileInfo.Refresh();

        bool isOffline = fileInfo.Attributes.HasFlag(FileAttributes.Offline);
        bool isRecallOnOpen = fileInfo.Attributes.HasFlag(RecallOnOpenAttribute);

        return isOffline || isRecallOnOpen;
    }

    /// <summary>
    /// Checks via file path string.
    /// </summary>
    public static bool IsCloudOnlyPlaceholder(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        return IsCloudOnlyPlaceholder(new FileInfo(filePath));
    }
}