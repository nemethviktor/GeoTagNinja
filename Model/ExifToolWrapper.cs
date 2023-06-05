﻿/*
---
name: ExifToolWrapper.cs
description: C# Wrapper for Phil Harvey's excellent ExifTool
url: https://github.com/FileMeta/ExifToolWrapper/raw/master/ExifToolWrapper.cs
version: 1.2
keywords: CodeBit
dateModified: 2019-12-14
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GeoTagNinja;

namespace ExifToolWrapper;

public class ExifTool : IDisposable
{
    // const string c_arguments = @"-stay_open 1 -@ - -common_args -charset UTF8 -G1 -args";
    // -stay_open 1 -@ - -common_args invokes to stay open, parse args from stdin/cmdline and
    // use args for all future -execute command
    private const string c_arguments = @"-stay_open 1 -@ - -common_args -api ""Filter=s/\r|\n/ /g "" -a -s -s -struct -G -ee -charset UTF8 -charset filename=utf8 -args";

    // TODO:                             string commonArgs = @" -api ""Filter=s/\r|\n/ /g "" -a -s -s -struct -sort -G -ee -charset utf8 -charset filename=utf8 -charset photoshop=utf8 -charset exif=utf8 -charset iptc=utf8 ";
    private const string c_exitCommand = "-stay_open\nFalse\n";
    private const int c_timeout = 30000; // in milliseconds
    private const int c_exitTimeout = 15000;

    private static readonly Encoding s_Utf8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string c_exeName = Path.Combine(path1: Path.Combine(path1: AppDomain.CurrentDomain.BaseDirectory, path2: "Resources"), path2: "exiftool.exe"); // "exiftool.exe";

    private Process m_exifTool;
    private StreamWriter m_in;
    private StreamReader m_out;

    public ExifTool()
    {
        // Prepare process start
        ProcessStartInfo psi = new(fileName: c_exeName, arguments: c_arguments);
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardInput = true;
        psi.RedirectStandardOutput = true;
        psi.StandardOutputEncoding = s_Utf8NoBOM;

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

    public void GetProperties(string filename,
                              ICollection<KeyValuePair<string, string>> propertiesRead)
    {
        m_in.Write(value: filename);
        m_in.Write(value: "\n-execute\n");
        m_in.Flush();
        #if EXIF_TRACE
            Debug.WriteLine(filename);
            Debug.WriteLine("-execute");
        #endif
        for (;;)
        {
            string line = m_out.ReadLine();
            #if EXIF_TRACE
                Debug.WriteLine(line);
            #endif
            if (line.StartsWith(value: "{ready"))
            {
                break;
            }

            if (line[index: 0] == '-')
            {
                int eq = line.IndexOf(value: '=');
                if (eq > 1)
                {
                    string key = line.Substring(startIndex: 1, length: eq - 1);
                    string value = line.Substring(startIndex: eq + 1)
                        .Trim();
                    if (!propertiesRead.Any(predicate: f => f.Key == key))
                    {
                        propertiesRead.Add(item: new KeyValuePair<string, string>(key: key, value: value));
                    }
                }
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
        int year, month, day, hour, minute, second;
        s = s.Trim();
        if (!int.TryParse(s: s.Substring(startIndex: 0, length: 4), result: out year))
        {
            return false;
        }

        if (s[index: 4] != ':')
        {
            return false;
        }

        if (!int.TryParse(s: s.Substring(startIndex: 5, length: 2), result: out month))
        {
            return false;
        }

        if (s[index: 7] != ':')
        {
            return false;
        }

        if (!int.TryParse(s: s.Substring(startIndex: 8, length: 2), result: out day))
        {
            return false;
        }

        if (s[index: 10] != ' ')
        {
            return false;
        }

        if (!int.TryParse(s: s.Substring(startIndex: 11, length: 2), result: out hour))
        {
            return false;
        }

        if (s[index: 13] != ':')
        {
            return false;
        }

        if (!int.TryParse(s: s.Substring(startIndex: 14, length: 2), result: out minute))
        {
            return false;
        }

        if (s[index: 16] != ':')
        {
            return false;
        }

        if (!int.TryParse(s: s.Substring(startIndex: 17, length: 2), result: out second))
        {
            return false;
        }

        if (year < 1900 || year > 2200)
        {
            return false;
        }

        if (month < 1 || month > 12)
        {
            return false;
        }

        if (day < 1 || day > 31)
        {
            return false;
        }

        if (hour < 0 || hour > 23)
        {
            return false;
        }

        if (minute < 0 || minute > 59)
        {
            return false;
        }

        if (second < 0 || second > 59)
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

            if (m_out != null)
            {
                m_out.Dispose();
                m_out = null;
            }

            if (m_in != null)
            {
                m_in.Dispose();
                m_in = null;
            }

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