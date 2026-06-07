/*
---
name: ExifToolWrapper.cs
description: C# Wrapper for Phil Harvey's excellent ExifTool
url: https://github.com/FileMeta/ExifToolWrapper/raw/master/ExifToolWrapper.cs
version: 1.2.2
keywords: CodeBit
dateModified: 2019-12-14; unknown time by Urmel; 2026-07-06 by V Nemeth
license: http://unlicense.org
about: https://sno.phy.queensu.ca/~phil/exiftool/
# Metadata in MicroYaml format. See http://filemeta.org/CodeBit.html
...
*/

/*
Unlicense: http://unlicense.org

This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or distribute
this software, either in source code form or as a compiled binary, for any
purpose, commercial or non-commercial, and by any means.

In jurisdictions that recognize copyright laws, the author or authors of this
software dedicate any and all copyright interest in the software to the
public domain. We make this dedication for the benefit of the public at large
and to the detriment of our heirs and successors. We intend this dedication
to be an overt act of relinquishment in perpetuity of all present and future
rights to this software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <http://unlicense.org/>
*/
//#define EXIF_TRACE

using GeoTagNinja.Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GeoTagNinja.Model;

/// <summary>
/// Manages a persistent ExifTool process and provides methods to extract metadata from files via the ExifTool stay_open
/// protocol, handling process I/O and lifecycle.
/// </summary>
/// <remarks>Constructor launches the ExifTool executable (using HelperVariables.ExifToolExePathToUse) and throws
/// an ApplicationException on failure. Communication uses UTF-8 without BOM and configured timeouts. Not thread-safe;
/// call Dispose to terminate the external process cleanly (sends the stay_open exit command, waits for exit, and
/// disposes streams). Includes a static TryParseDate helper for ExifTool date strings (format "YYYY:MM:DD
/// hh:mm:ss").</remarks>
public class ExifTool : IDisposable
{
    private const string c_arguments = @"-stay_open 1 -@ - -common_args -api ""Filter=s/\r|\n/ /g "" -a -s -s -struct -G -ee -charset UTF8 -charset filename=utf8 -args";

    private const string c_exitCommand = "-stay_open\nFalse\n";
    private const int c_timeout = 30000; // in milliseconds
    private const int c_exitTimeout = 15000;

    private static readonly Encoding s_Utf8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    //private readonly string c_exeName = Path.Combine(path1: Path.Combine(path1: AppDomain.CurrentDomain.BaseDirectory, path2: "Resources"), path2: "exiftool.exe"); // "exiftool.exe";

    private Process m_exifTool;
    private StreamWriter m_in;
    private StreamReader m_out;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Initializes a new ExifTool instance and launches the external ExifTool process with redirected standard input
    /// and output.
    /// </summary>
    /// <remarks>Configures ProcessStartInfo to run ExifTool without a window and with redirected streams,
    /// sets UTF-8 (no BOM) for standard output, and wraps StandardInput.BaseStream with a StreamWriter to control input
    /// encoding on frameworks that lack StandardInputEncoding. Assigns the process to an internal field and exposes its
    /// standard output stream.</remarks>
    /// <exception cref="ApplicationException">Thrown when the ExifTool process fails to start or exits immediately, or when launching the executable fails
    /// (for example if ExifTool.exe is missing or not on the PATH). When caused by an OS error the inner exception is a
    /// Win32Exception.</exception>
    public ExifTool()
    {
        // Prepare process start
        ProcessStartInfo psi = new(fileName: HelperVariables.ExifToolExePathToUse,
                                   arguments: c_arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = s_Utf8NoBOM
        };

        try
        {
            m_exifTool = Process.Start(startInfo: psi);
            if (m_exifTool == null || m_exifTool.HasExited)
            {
                throw new ApplicationException(message: "Failed to launch ExifTool!");
            }
        }
        catch (Win32Exception err)
        {
            throw new ApplicationException(message: "Failed to load ExifTool. 'ExifTool.exe' should be located in the same directory as the application or on the path.", innerException: err);
        }

        // ProcessStartInfo in .NET Framework doesn't have a StandardInputEncoding property (though it does in .NET Core)
        // So, we have to wrap it this way.
        m_in = new StreamWriter(stream: m_exifTool.StandardInput.BaseStream, encoding: s_Utf8NoBOM);
        m_out = m_exifTool.StandardOutput;
    }

    /// <summary>
    ///     Retrieves specific predefined tag properties for a target file. 
    ///     Dynamically configures the extraction arguments to strip out unneeded attributes.
    /// </summary>
    /// <param name="filename">The absolute path of the target file to scan.</param>
    /// <param name="propertiesRead">The collection to hold the extracted metadata properties.</param>
    public void GetProperties(string filename,
                              ICollection<KeyValuePair<string, string>> propertiesRead)
    {
        if (Helpers.HelperVariables.ApplicationIsClosing == true)
        {
            return;
        }

        // 1. Send the primary target filename block
        m_in.Write(value: filename);
        m_in.Write(value: "\n");

        // 2. DYNAMIC PRE-FILTER: Feed the specific parameters required
        // This instructs ExifTool to ignore anything not registered inside the Mapping profiles.
        IEnumerable<string> targetedTags = Model.SourcesAndAttributes.GetAllRequiredInAttributes();
        foreach (string tag in targetedTags)
        {
            // Format as an explicit extraction target argument (e.g., "-XMP:GPSAltitude")
            m_in.Write(value: $"-{tag}\n");
        }

        // 3. Fire the execution sequence
        m_in.Write(value: "-execute\n");
        m_in.Flush();

        // 4. Safe sequential parsing loop...
        for (; ; )
        {
            string? line = m_out.ReadLine();
            if (line == null)
            {
                break;
            }

            if (line.StartsWith(value: "{ready"))
            {
                break;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(value: line) || line[index: 0] != '-')
                {
                    continue;
                }

                int eq = line.IndexOf(value: '=');
                if (eq > 1)
                {
                    string key = line.Substring(startIndex: 1, length: eq - 1);
                    string value = line.Substring(startIndex: eq + 1).Trim();

                    if (!propertiesRead.Any(predicate: f => f.Key == key))
                    {
                        propertiesRead.Add(item: new KeyValuePair<string, string>(key: key, value: value));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(exception: ex, message: $"Failed to parse pre-filtered line token: '{line}'");
            }
        }
    }
    #region Static Methods

    /// <summary>
    ///     Attempt to parse a date-time in the format used by ExifTool
    /// </summary>
    /// <param name="s">The string to be parsed</param>
    /// <param name="kind">
    ///     The <see cref="DateTimeKind" /> to be assigned to the resulting value. It is generally
    ///     determined by the definition of the corresponding field.
    /// </param>
    /// <param name="date">The resulting parsed date.</param>
    /// <returns>True if successful, else false.</returns>
    /// <remarks>
    ///     <para>ExifTool formats dates as follows: "YYYY:MM:DD hh:mm:ss". For example, "2018:06:22 19:32:53".</para>
    /// </remarks>
    public static bool TryParseDate(string s,
                                    DateTimeKind kind,
                                    out DateTime date)
    {
        date = DateTime.MinValue;
        s = s.Trim();
        if (!int.TryParse(s: s.Substring(startIndex: 0, length: 4), result: out int year))
        {
            return false;
        }

        if (s[index: 4] != ':')
        {
            return false;
        }

        if (!int.TryParse(s: s.Substring(startIndex: 5, length: 2), result: out int month))
        {
            return false;
        }

        if (s[index: 7] != ':')
        {
            return false;
        }

        if (!int.TryParse(s: s.Substring(startIndex: 8, length: 2), result: out int day))
        {
            return false;
        }

        if (s[index: 10] != ' ')
        {
            return false;
        }

        if (!int.TryParse(s: s.Substring(startIndex: 11, length: 2), result: out int hour))
        {
            return false;
        }

        if (s[index: 13] != ':')
        {
            return false;
        }

        if (!int.TryParse(s: s.Substring(startIndex: 14, length: 2), result: out int minute))
        {
            return false;
        }

        if (s[index: 16] != ':')
        {
            return false;
        }

        if (!int.TryParse(s: s.Substring(startIndex: 17, length: 2), result: out int second))
        {
            return false;
        }

        if (year is < 1900 or > 2200)
        {
            return false;
        }

        if (month is < 1 or > 12)
        {
            return false;
        }

        if (day is < 1 or > 31)
        {
            return false;
        }

        if (hour is < 0 or > 23)
        {
            return false;
        }

        if (minute is < 0 or > 59)
        {
            return false;
        }

        if (second is < 0 or > 59)
        {
            return false;
        }

        try
        {
            date = new DateTime(year: year, month: month, day: day, hour: hour, minute: minute, second: second, millisecond: 0, kind: kind);
        }
        catch (Exception)
        {
            return false; // Probaby a month with too many days.
        }

        return true;
    }

    #endregion Static Methods

    #region IDisposable Support

    protected virtual void Dispose(bool disposing)
    {
        if (m_exifTool != null)
        {
            if (!disposing)
            {
                Debug.Fail(message: "Failed to dispose ExifTool.");
            }

            // If process is running, shut it down cleanly
            if (!m_exifTool.HasExited)
            {
                m_in.Write(value: c_exitCommand);
                m_in.Close();

                if (!m_exifTool.WaitForExit(milliseconds: c_exitTimeout))
                {
                    m_exifTool.Kill();
                    Debug.Fail(message: "Timed out waiting for exiftool to exit.");
                }
#if EXIF_TRACE
                    else
                    {
                        Debug.WriteLine("ExifTool exited cleanly.");
                    }
#endif
            }

            m_out?.Dispose();
            m_out = null;
            m_in?.Dispose();
            m_in = null;

            m_exifTool.Dispose();
            m_exifTool = null;
        }
    }

    ~ExifTool()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(disposing: false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(disposing: true);
        GC.SuppressFinalize(obj: this);
    }

    #endregion
}