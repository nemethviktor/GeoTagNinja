using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers;

internal partial class HelperNonStatic
{
    private FrmPleaseWaitBox _frmPleaseWaitBoxInstance;


    /// <summary>
    ///     Loops a given folder while ignoring access and other errors.
    ///     via https://stackoverflow.com/a/33172145/3968494
    /// </summary>
    /// <param name="folder">The root folder to parse</param>
    /// <param name="filter">Filters to enact</param>
    /// <param name="recursive">Whether recursive or not</param>
    /// <param name="updateProgressHandler">Action link to the <see cref="FrmMainApp.HandlerUpdateLabelText" /></param>
    /// <param name="cancellationToken">The CT</param>
    /// <returns></returns>
    internal IEnumerable<FileInfo> GetFilesFromAFolder(string folder,
                                                       string[] filter,
                                                       bool recursive,
                                                       Action<string> updateProgressHandler,
                                                       CancellationToken cancellationToken)
    {
        _frmPleaseWaitBoxInstance =
            (FrmPleaseWaitBox)Application.OpenForms[name: "FrmPleaseWaitBox"];
        Debug.Assert(condition: _frmPleaseWaitBoxInstance != null,
            message: $"{nameof(_frmPleaseWaitBoxInstance)} != null");
        IEnumerable<string> found = new List<string>();
        try
        {
            found = Directory.GetFiles(path: folder)
                             .Where(predicate: file =>
                                  filter.Any(predicate: ext => file.ToLower().EndsWith(value: ext)));
        }
        catch (Exception ex)
        {
            Console.WriteLine(value: $"Error accessing folder {folder}: {ex.Message}");
        }

        int counter = 0;
        foreach (string file in found)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            if (counter % 10 == 0)
            {
                Application.DoEvents();
            }

            // updateProgressHandler(obj: $"Scanning file: {file}");
            _frmPleaseWaitBoxInstance.lbl_PleaseWaitBoxMessage.Text = file;

            yield return new FileInfo(fileName: file);
            counter++;
        }

        if (recursive)
        {
            IEnumerable<string> directories = new List<string>();
            try
            {
                directories = Directory.GetDirectories(path: folder);
            }
            catch (Exception ex)
            {
                Console.WriteLine(value: $"Error accessing subdirectories in {folder}: {ex.Message}");
            }

            foreach (string dir in directories)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                foreach (FileInfo subFile in GetFilesFromAFolder(folder: dir, filter: filter, recursive: recursive,
                             updateProgressHandler: updateProgressHandler, cancellationToken: cancellationToken))
                {
                    yield return subFile;
                }
            }
        }
    }
}