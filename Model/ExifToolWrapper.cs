/*
---
name: ExifToolWrapper.cs
description: C# Wrapper for Phil Harvey's excellent ExifTool
url: https://github.com/FileMeta/ExifToolWrapper/raw/master/ExifToolWrapper.cs
version: 1.3.GeoTagNinja (adapted)
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
using System.Text;
using System.Diagnostics;
using System.IO;
using GeoTagNinja;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace ExifToolWrapper
{
    public class ExifTool : IDisposable
    {
        string c_exeName = Path.Combine(path1: FrmMainApp.ResourcesFolderPath, path2: "exiftool.exe");    // "exiftool.exe";
        // -stay_open 1 -@ - -common_args invokes to stay open, parse args from stdin/cmdline and
        // use args for all future -execute command
        const string c_arguments = @"-stay_open 1 -@ - -common_args -api ""Filter=s/\r|\n/ /g "" -a -s -s -struct -G -ee -charset UTF8 -charset filename=utf8 -args";
        const string c_exitCommand = "-stay_open\nFalse\n";

        /// <summary>
        /// Time out in millisecs when reading byte output from the
        /// exif tool. Will occur at the end of almost every image
        /// extracted!
        /// </summary>
        const int c_timeout = 50;

        /// <summary>
        /// Time out in millisecs before killing exif tool when trying to
        /// shutdown the ExifTool instance
        /// </summary>
        const int c_exitTimeout = 15000;

        const char SDoubleQuote = '"';
        const string c_args_tags = "";
        // const string c_args_image = @"-b -preview:GTNPreview -w! " + SDoubleQuote + FrmMainApp.UserDataFolderPath + @"\%F.jpg" + SDoubleQuote;

        // Line breaks needed for EXIF to recognize the commands...
        const string c_args_image = "\n-b\n-preview:%IMAGE_TITLE%\n";

        const string c_read_text = "{ready}";

        static Encoding s_Utf8NoBOM = new UTF8Encoding(false);

        Process m_exifTool;
        StreamWriter m_in;
        StreamReader m_out;

        public ExifTool()
        {
            // Prepare process start
            var psi = new ProcessStartInfo(c_exeName, c_arguments);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.StandardOutputEncoding = s_Utf8NoBOM;

            try
            {
                m_exifTool = Process.Start(psi);
                if (m_exifTool == null || m_exifTool.HasExited)
                {
                    throw new ApplicationException("Failed to launch ExifTool!");
                }
            }
            catch (System.ComponentModel.Win32Exception err)
            {
                throw new ApplicationException("Failed to load ExifTool. 'ExifTool.exe' should be located in the same directory as the application or on the path.", err);
            }

            // ProcessStartInfo in .NET Framework doesn't have a StandardInputEncoding property (though it does in .NET Core)
            // So, we have to wrap it this way.
            m_in = new StreamWriter(m_exifTool.StandardInput.BaseStream, s_Utf8NoBOM);
            m_out = m_exifTool.StandardOutput;
        }


        /// <summary>
        /// Extracts an image out of a file using the ExifTool.
        /// </summary>
        /// <param name="filename">The filename to extract from</param>
        /// <param name="imageTitle">The image title, defaults to GTNPreview</param>
        /// <returns>A stream with the image data (jpg, png likely)</returns>
        public Stream GetImage(string filename, string imageTitle = "GTNPreview")
        {
            m_in.Write(c_args_image.Replace("%IMAGE_TITLE%", imageTitle));
            m_in.Write(filename);
            // Filename has no line breaks - so add it to subsequent message...
            m_in.Write("\n-execute\n");
            m_in.Flush();
#if EXIF_TRACE
            Debug.WriteLine(c_args_image);
            Debug.WriteLine(filename);
            Debug.WriteLine("-execute");
#endif

            if (!m_out.BaseStream.CanRead)
            {
                // TOdo Handle problem
                throw new IOException("Reading data from EXIFTool resulted in an error - Could not open EXIF Tool output channel.");
            }


            // Read and ReadByte methods expect to read something
            // --> hangs if there is no more data to read
            // --> use async method with timeout until {ready} is encountered.
            //     If it is not encountered within timeout, exif tool hangs...
            // Note: a readasync operation cannot be canceled
            // (see https://stackoverflow.com/questions/15273752/how-to-cancel-networkstream-readasync-without-closing-stream)
            // --> read until full ready message is in result or result < max read size

            byte[] buffer = new byte[4096];
            List<byte[]> bytesList = new List<byte[]>();
            MemoryStream thumbStream = new MemoryStream();
            int bytesRead = 0;
            Task<int> readTask = null;
            bool readyReceived = false;

            for (; ; )
            {
                // Read until time out
                if (readTask == null)
                    readTask = m_out.BaseStream.ReadAsync(buffer, 0, buffer.Length);
                readTask.Wait(c_timeout);
                int incrementSize = readTask.Result;

                if (readTask.IsCompleted)
                {
                    // Signal successful completion
                    readTask = null;

                    if (incrementSize > 0) // if something was read
                    {
                        bytesRead += incrementSize;
                        byte[] tmp = new byte[incrementSize];   // Account for not adding full buffer size
                        Array.Copy(buffer, 0, tmp, 0, incrementSize);
                        bytesList.Add(tmp);
                        thumbStream.Write(tmp, 0, incrementSize);
                    }
                    else  // Pot. academic situation that read completed with no data and previous read did not contain ready mark
                        break;

                    // Check if the ready mark is at the end
                    // account for ready text + CR-LF at end
                    thumbStream.Seek(-(c_read_text.Length + 2), SeekOrigin.End);
                    byte[] checkText = new byte[c_read_text.Length + 2];
                    thumbStream.Read(checkText, 0, checkText.Length);
                    string checkTextStr = System.Text.Encoding.UTF8.GetString(checkText);
                    if (checkTextStr.Trim() == c_read_text)
                    {
                        readyReceived = true;
                        // remove ready string
                        thumbStream.Seek(-(c_read_text.Length + 2), SeekOrigin.End);
                        thumbStream.SetLength((int)thumbStream.Length - c_read_text.Length - 2);
                        break;
                    }

                    if (incrementSize < buffer.Length)  // No more data to read, but no ready mark
                        break;

                    // Still more to read
                    continue;
                }

                // Time out reached
                break;
            }

            // If we have a readTask object, we encountered a time out
            // --> TODO: Mititgate by killing and restarting exif tool
            if (readTask != null)
                throw new IOException("Reading data from EXIFTool resulted in an error - Reading did not finish in time. Exif Tool hangs.");

            // If we have no readTask object and no readyReceived, we have
            // some other issue. --> return no result
            if (!readyReceived)
                throw new IOException("Reading data from EXIFTool resulted in an error - Reading finished with no valid result.");

            // If length is < 10, there is no image result
            if (thumbStream.Length < 10)
                return null;

            return thumbStream;
        }



        /// <summary>
        /// Extracts the tags out of a file using the ExifTool.
        /// 
        /// Note that duplicate tags may be added - object handed in
        /// propsRead must thus support duplicate keys.
        /// </summary>
        /// <param name="filename">The filename to extract tags from</param>
        /// <param name="propsRead">A collection to which the tags are added</param>
        public void GetProperties(string filename, ICollection<KeyValuePair<string, string>> propsRead)
        {
            m_in.Write(filename);
            m_in.Write("\n-execute\n");
            m_in.Flush();
#if EXIF_TRACE
            Debug.WriteLine(filename);
            Debug.WriteLine("-execute");
#endif
            for (; ; )
            {
                var line = m_out.ReadLine();
#if EXIF_TRACE
                Debug.WriteLine(line);
#endif
                if (line.StartsWith("{ready")) break;
                if (line[0] == '-')
                {
                    int eq = line.IndexOf('=');
                    if (eq > 1)
                    {
                        string key = line.Substring(1, eq - 1);
                        string value = line.Substring(eq + 1).Trim();
                        propsRead.Add(new KeyValuePair<string, string>(key, value));
                    }
                }
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (m_exifTool != null)
            {
                if (!disposing)
                {
                    System.Diagnostics.Debug.Fail("Failed to dispose ExifTool.");
                }

                // If process is running, shut it down cleanly
                if (!m_exifTool.HasExited)
                {
                    m_in.Write(c_exitCommand);
                    m_in.Close();
 
                    if (!m_exifTool.WaitForExit(c_exitTimeout))
                    {
                        m_exifTool.Kill();
                        Debug.Fail("Timed out waiting for exiftool to exit.");
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
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Static Methods

        /// <summary>
        /// Attempt to parse a date-time in the format used by ExifTool
        /// </summary>
        /// <param name="s">The string to be parsed</param>
        /// <param name="kind">The <see cref="DateTimeKind"/> to be assigned to the resulting value. It is generally
        /// determined by the definition of the corresponding field.</param>
        /// <param name="date">The resulting parsed date.</param>
        /// <returns>True if successful, else false.</returns>
        /// <remarks>
        /// <para>ExifTool formats dates as follows: "YYYY:MM:DD hh:mm:ss". For example, "2018:06:22 19:32:53".</para>
        /// </remarks>
        public static bool TryParseDate(string s, DateTimeKind kind, out DateTime date)
        {
            date = DateTime.MinValue;
            int year, month, day, hour, minute, second;
            s = s.Trim();
            if (!int.TryParse(s.Substring(0, 4), out year)) return false;
            if (s[4] != ':') return false;
            if (!int.TryParse(s.Substring(5, 2), out month)) return false;
            if (s[7] != ':') return false;
            if (!int.TryParse(s.Substring(8, 2), out day)) return false;
            if (s[10] != ' ') return false;
            if (!int.TryParse(s.Substring(11, 2), out hour)) return false;
            if (s[13] != ':') return false;
            if (!int.TryParse(s.Substring(14, 2), out minute)) return false;
            if (s[16] != ':') return false;
            if (!int.TryParse(s.Substring(17, 2), out second)) return false;

            if (year < 1900 || year > 2200) return false;
            if (month < 1 || month > 12) return false;
            if (day < 1 || day > 31) return false;
            if (hour < 0 || hour > 23) return false;
            if (minute < 0 || minute > 59) return false;
            if (second < 0 || second > 59) return false;

            try
            {
                date = new DateTime(year, month, day, hour, minute, second, 0, kind);
            }
            catch (Exception)
            {
                return false; // Probaby a month with too many days.
            }

            return true;
        }

        #endregion Static Methods
    }
}
