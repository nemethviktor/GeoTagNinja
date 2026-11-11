using GeoTagNinja.Helpers.FileSystem;
using GeoTagNinja.Helpers.Localisation;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.View.FileList;

public partial class FileListView
{
    /// <summary>
    ///     The metadata the Batch Rename dialog's Metadata piece can use. Each is offered under the caption
    ///     <c>lbl_BatchRename_Field_&lt;attribute name&gt;</c> in <c>Strings.resx</c>.
    /// </summary>
    internal static readonly ElementAttribute[] BatchRenameFields =
    [
        ElementAttribute.Country, ElementAttribute.CountryCode, ElementAttribute.State, ElementAttribute.City,
        ElementAttribute.Sublocation, ElementAttribute.Make, ElementAttribute.Model, ElementAttribute.LensSpec,
        ElementAttribute.Rating, ElementAttribute.ExposureTime, ElementAttribute.Fnumber, ElementAttribute.FocalLength,
        ElementAttribute.ISO, ElementAttribute.Artist, ElementAttribute.Byline, ElementAttribute.Copyright,
        ElementAttribute.Credit, ElementAttribute.Source, ElementAttribute.ObjectName, ElementAttribute.Headline,
        ElementAttribute.Category
    ];

    /// <summary>
    ///     Opens Batch Rename for the selected files (in the order they are listed) and, if the user goes ahead, renames,
    ///     moves or copies them with their XMP sidecars and brings the list and the model up to date.
    /// </summary>
    internal void ShowBatchRenameDialog()
    {
        FrmMainApp frmMain = FrmMainApp.Instance;
        if (frmMain != null &&
            !frmMain.EnsureMetadataIsReadyAndWarnUserIfNot())
        {
            return;
        }

        List<ListViewItem> rows = SelectedItems.Cast<ListViewItem>()
                                               .Where(predicate: lvi => lvi.Tag is DirectoryElement
                                               {
                                                   Type: DirectoryElement.ElementType.File
                                               })
                                               .ToList();
        if (rows.Count == 0)
        {
            ThemeHelper.ShowMessageBox(message: BatchRenameText.Get(key: "mbx_BatchRename_SelectFiles"), caption: BatchRenameText.Get(key: "frm_BatchRename"),
                buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
            return;
        }

        List<BatchRenameSource> sources = rows.Select(selector: BatchRenameSourceOf).ToList();
        MarkSharedSidecars(sources: sources);

        using FrmBatchRename dialog = new(sources: sources,
            fields: BatchRenameFields.Select(selector: attribute => (GetElementAttributesName(attributeToFind: attribute),
                                                  BatchRenameText.Get(key: $"lbl_BatchRename_Field_{GetElementAttributesName(attributeToFind: attribute)}")))
                                     .ToList());
        if (dialog.ShowDialog(owner: FindForm()) != DialogResult.OK)
        {
            return;
        }

        // A step can still fail after the checks passed (a file gets locked a moment later). The executor puts back
        // whatever it had done; the user then chooses between stopping and going on without that file.
        BatchRenameResult result;
        List<BatchRenameEntry> skipped = [];
        while (true)
        {
            result = BatchRenameExecutor.Execute(entries: dialog.ResultPlan, destination: dialog.Result.Destination);
            if (result.Success)
            {
                break;
            }

            Log.Error(message: $"Batch rename failed: {result.Error}");
            if (result.FailedEntry == null)
            {
                ThemeHelper.ShowMessageBox(
                    message: BatchRenameText.Get(key: "mbx_BatchRename_NothingRenamed", result.Error),
                    caption: BatchRenameText.Get(key: "frm_BatchRename"), buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                return;
            }

            string failedName = Path.GetFileName(path: result.FailedEntry.Source.FullPath);
            if (ThemeHelper.ShowMessageBoxWithResult(
                    message: BatchRenameText.Get(key: "mbx_BatchRename_StepFailed", result.Error, failedName),
                    caption: BatchRenameText.Get(key: "frm_BatchRename"), buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            skipped.AddRange(collection: BatchRenameExecutor.ExcludeWithDependents(entries: dialog.ResultPlan, failed: result.FailedEntry));
            if (dialog.ResultPlan.All(predicate: entry => entry.Unchanged))
            {
                ThemeHelper.ShowMessageBox(message: BatchRenameText.Get(key: "mbx_BatchRename_NothingLeft"), caption: BatchRenameText.Get(key: "frm_BatchRename"),
                    buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
                return;
            }
        }

        ApplyBatchRenameToList(applied: result.Applied, destination: dialog.Result.Destination);
        Log.Info(message: $"Batch rename done: {result.Applied.Count} file(s), {skipped.Count} left as they were");
        if (skipped.Count > 0)
        {
            string names = string.Join(separator: Environment.NewLine,
                values: skipped.Take(count: 10).Select(selector: entry => Path.GetFileName(path: entry.Source.FullPath)));
            if (skipped.Count > 10)
            {
                names += Environment.NewLine + BatchRenameText.Get(key: "mbx_BatchRename_AndMore", skipped.Count - 10);
            }

            ThemeHelper.ShowMessageBox(
                message: BatchRenameText.Get(key: "mbx_BatchRename_DoneSkipped", result.Applied.Count, names),
                caption: BatchRenameText.Get(key: "frm_BatchRename"), buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
        }
    }

    private static BatchRenameSource BatchRenameSourceOf(ListViewItem lvi)
    {
        DirectoryElement de = (DirectoryElement)lvi.Tag;
        string directory = Path.GetDirectoryName(path: de.FileNameWithPath) ?? string.Empty;
        FileInfo info = new(fileName: de.FileNameWithPath);

        Dictionary<string, string> metadata = new(comparer: StringComparer.Ordinal);
        foreach (ElementAttribute attribute in BatchRenameFields)
        {
            // RoundTrip is the invariant text: a file name must not depend on the user's culture
            metadata[key: GetElementAttributesName(attributeToFind: attribute)] = de.GetAttributeValueAsString(attribute: attribute,
                version: null, notFoundValue: string.Empty, context: ValueFormatContext.RoundTrip);
        }

        return new BatchRenameSource
        {
            FullPath = de.FileNameWithPath,
            FolderName = Path.GetFileName(path: directory.TrimEnd(Path.DirectorySeparatorChar)) is { Length: > 0 } name
                ? name
                : directory,
            HasSidecar = de.SidecarFile is { Exists: true },
            PreservedFileName = de.GetAttributeValueAsString(attribute: ElementAttribute.PreservedFileName, version: null,
                notFoundValue: null, context: ValueFormatContext.RoundTrip),
            DateTaken = de.GetAttributeValue<DateTime>(attribute: ElementAttribute.TakenDate, version: null),
            DateCreated = de.GetAttributeValue<DateTime>(attribute: ElementAttribute.CreateDate, version: null),
            FileCreated = info.Exists
                ? info.CreationTime
                : null,
            FileModified = info.Exists
                ? info.LastWriteTime
                : null,
            Metadata = metadata,
            Tag = lvi
        };
    }

    /// <summary>
    ///     Flags the files whose sidecar another file outside the batch also uses (a RAW and its JPEG share
    ///     <c>name.xmp</c>): renaming one must not take the sidecar away from the other.
    /// </summary>
    private static void MarkSharedSidecars(List<BatchRenameSource> sources)
    {
        HashSet<string> inBatch = new(collection: sources.Select(selector: s => s.FullPath), comparer: StringComparer.OrdinalIgnoreCase);
        foreach (BatchRenameSource source in sources.Where(predicate: s => s.HasSidecar))
        {
            try
            {
                string directory = Path.GetDirectoryName(path: source.FullPath) ?? string.Empty;
                string baseName = Path.GetFileNameWithoutExtension(path: source.FullPath);
                source.SidecarShared = Directory.EnumerateFiles(path: directory, searchPattern: baseName + ".*")
                                                .Any(predicate: other =>
                                                    !inBatch.Contains(item: other) &&
                                                    !other.EndsWith(value: ".xmp", comparisonType: StringComparison.OrdinalIgnoreCase) &&
                                                    string.Equals(a: Path.GetFileNameWithoutExtension(path: other), b: baseName,
                                                        comparisonType: StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                // cannot tell: the safe answer is to leave the sidecar where it is and copy it
                Log.Warn(exception: ex, message: $"Could not check whether the sidecar of {source.FullPath} is shared");
                source.SidecarShared = true;
            }
        }
    }

    private void ApplyBatchRenameToList(List<BatchRenameEntry> applied,
                                        BatchRenameDestination destination)
    {
        if (destination == BatchRenameDestination.CopyToFolder)
        {
            return; // the originals are untouched, and the copies are in another folder
        }

        BeginUpdate();
        try
        {
            foreach (BatchRenameEntry entry in applied)
            {
                ListViewItem lvi = (ListViewItem)entry.Source.Tag;
                DirectoryElement de = (DirectoryElement)lvi.Tag;
                bool sameFolder = string.Equals(a: Path.GetDirectoryName(path: entry.NewPath),
                    b: Path.GetDirectoryName(path: entry.Source.FullPath), comparisonType: StringComparison.OrdinalIgnoreCase);

                if (sameFolder)
                {
                    de.UpdatePathAfterRename(newPath: entry.NewPath);
                    lvi.Text = entry.NewFileName;
                    lvi.Name = entry.NewPath;
                }
                else
                {
                    // moved to another folder: it is no longer part of what is shown
                    _ = _masterCollection?.Remove(item: de);
                    _ = _itemByElement.Remove(key: de);
                    Items.Remove(item: lvi);
                }
            }

            ItemsVersion++;
            ContentVersion++;
        }
        finally
        {
            EndUpdate();
        }
    }
}
