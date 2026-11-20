using NLog;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GeoTagNinja.Helpers.Exif;

/// <summary>
///     Makes the picture shown for a video (a thumbnail, or the preview pane): a still frame. A video file carries no
///     embedded picture, so the frame has to be decoded, in this order:
///     <list type="number">
///         <item>
///             <description>
///                 With the ffmpeg the user pointed <c>Settings</c> at (<see cref="HelperVariables.UserSettingFfmpegPath" />),
///                 if that path is valid. It reads every codec ffmpeg does, so HEVC/H.265 phone clips work without
///                 anything installed in Windows.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Otherwise the platform's own thumbnail (<see cref="PlatformFrameProvider" />, the Windows shell in the
///                 WinForms app), which only knows the codecs Windows has.
///             </description>
///         </item>
///     </list>
///     If neither produces a frame the caller shows a generic video symbol instead.
/// </summary>
internal static class VideoFrameProvider
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>How long ffmpeg may take to produce one frame, or to answer <c>-version</c>.</summary>
    private const int FrameTimeoutMilliseconds = 15000;

    private const int VersionTimeoutMilliseconds = 5000;

    /// <summary>
    ///     The host's own way to make a video thumbnail (long edge, in pixels, as the second argument), or null when it
    ///     has none. Set once at start-up; the Windows shell in the WinForms app.
    /// </summary>
    internal static Func<string, int, SKBitmap?>? PlatformFrameProvider { get; set; }

    private static readonly object ValidationLock = new();
    private static string? _validatedFor;
    private static string? _validatedExecutable;

    /// <summary>
    ///     Finds the ffmpeg executable the user's setting points at: the file itself, or a folder holding
    ///     <c>ffmpeg.exe</c> / <c>ffmpeg</c> (people tend to paste the <c>bin</c> folder). Only checks that the file is
    ///     there; <see cref="IsValidFfmpeg" /> checks that it is ffmpeg.
    /// </summary>
    /// <param name="configured">The setting's text: a file, a folder, blank, or quoted.</param>
    /// <returns>The path of the executable, or null.</returns>
    internal static string? ResolveFfmpegPath(string? configured)
    {
        if (string.IsNullOrWhiteSpace(value: configured))
        {
            return null;
        }

        string path = configured!.Trim()
                                 .Trim('"');
        try
        {
            if (Directory.Exists(path: path))
            {
                foreach (string name in new[] { "ffmpeg.exe", "ffmpeg" })
                {
                    string candidate = Path.Combine(path1: path, path2: name);
                    if (File.Exists(path: candidate))
                    {
                        return candidate;
                    }
                }

                return null;
            }

            return File.Exists(path: path)
                ? path
                : null;
        }
        catch (Exception)
        {
            // an illegal character in a half-typed path, say
            return null;
        }
    }

    /// <summary>
    ///     Whether the setting names a working ffmpeg: the file is there and it says "ffmpeg version" when asked. The
    ///     answer is remembered for the path, so the process is started once, not once per thumbnail.
    /// </summary>
    internal static bool IsValidFfmpeg(string? configured)
    {
        return ValidFfmpegExecutable(configured: configured) != null;
    }

    private static string? ValidFfmpegExecutable(string? configured)
    {
        string? key = configured?.Trim();
        lock (ValidationLock)
        {
            if (key == _validatedFor)
            {
                return _validatedExecutable;
            }

            string? executable = ResolveFfmpegPath(configured: configured);
            bool valid = executable != null &&
                         AnswersAsFfmpeg(executable: executable);

            _validatedFor = key;
            _validatedExecutable = valid
                ? executable
                : null;

            if (!string.IsNullOrEmpty(value: key) &&
                !valid)
            {
                Log.Warn(message: $"The ffmpeg path in Settings is not usable: '{key}'. Video previews fall back to the Windows thumbnail.");
            }

            return _validatedExecutable;
        }
    }

    private static bool AnswersAsFfmpeg(string executable)
    {
        try
        {
            using Process process = Process.Start(startInfo: new ProcessStartInfo(fileName: executable, arguments: "-hide_banner -version")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process == null)
            {
                return false;
            }

            Task<string> errors = process.StandardError.ReadToEndAsync();
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(milliseconds: VersionTimeoutMilliseconds))
            {
                TryKill(process: process);
                return false;
            }

            _ = errors;
            return output.Result.TrimStart()
                         .StartsWith(value: "ffmpeg version", comparisonType: StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    ///     A still frame of the video, scaled so its long edge is at most <paramref name="size" /> pixels, or null when
    ///     neither ffmpeg nor the platform could make one. Slow (it starts a process), so call from a worker thread.
    /// </summary>
    /// <returns>The bitmap, which the caller owns.</returns>
    internal static SKBitmap? TryCreate(string videoPath,
                                        int size)
    {
        string? ffmpeg = ValidFfmpegExecutable(configured: HelperVariables.UserSettingFfmpegPath);
        if (ffmpeg != null)
        {
            SKBitmap? frame = TryFfmpegFrame(ffmpeg: ffmpeg, videoPath: videoPath, size: size);
            if (frame != null)
            {
                return frame;
            }
        }

        try
        {
            return PlatformFrameProvider?.Invoke(arg1: videoPath, arg2: size);
        }
        catch (Exception ex)
        {
            Log.Debug(exception: ex, message: $"Platform video thumbnail failed for {videoPath}");
            return null;
        }
    }

    private static SKBitmap? TryFfmpegFrame(string ffmpeg,
                                            string videoPath,
                                            int size)
    {
        // A second in, to skip a black first frame; a clip shorter than that gives nothing, so try the very start too.
        foreach (string seek in new[] { "1", "0" })
        {
            byte[]? jpeg = RunFfmpeg(ffmpeg: ffmpeg, videoPath: videoPath, seekSeconds: seek);
            if (jpeg == null ||
                jpeg.Length == 0)
            {
                continue;
            }

            // ffmpeg has already applied the rotation the file asks for (a phone held upright).
            using SKBitmap? decoded = SKBitmap.Decode(buffer: jpeg);
            if (decoded != null)
            {
                return ImageOps.FitWithin(source: decoded, maxEdge: size);
            }
        }

        return null;
    }

    private static byte[]? RunFfmpeg(string ffmpeg,
                                     string videoPath,
                                     string seekSeconds)
    {
        try
        {
            using Process process = Process.Start(startInfo: new ProcessStartInfo(fileName: ffmpeg,
                arguments: $"-v error -nostdin -ss {seekSeconds} -i \"{videoPath}\" -frames:v 1 -an -sn -f image2pipe -c:v mjpeg -q:v 3 pipe:1")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process == null)
            {
                return null;
            }

            // both pipes have to be drained while it runs, or a chatty ffmpeg blocks on a full buffer
            using MemoryStream picture = new();
            Task copy = process.StandardOutput.BaseStream.CopyToAsync(destination: picture);
            _ = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(milliseconds: FrameTimeoutMilliseconds))
            {
                TryKill(process: process);
                Log.Warn(message: $"ffmpeg took too long to read a frame from {videoPath}");
                return null;
            }

            _ = copy.Wait(millisecondsTimeout: FrameTimeoutMilliseconds);
            return process.ExitCode == 0
                ? picture.ToArray()
                : null;
        }
        catch (Exception ex)
        {
            Log.Debug(exception: ex, message: $"ffmpeg could not make a frame for {videoPath}");
            return null;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill();
        }
        catch (Exception)
        {
            // already gone
        }
    }
}
