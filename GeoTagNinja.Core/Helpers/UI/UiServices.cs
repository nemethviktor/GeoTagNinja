using GeoTagNinja.Helpers.Update;
using GeoTagNinja.Model;
using NLog;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace GeoTagNinja.Helpers.UI;

// Enum values deliberately match System.Windows.Forms.MessageBoxButtons / MessageBoxIcon / DialogResult,
// so the WinForms host can cast between them. Other hosts (Avalonia) map them their own way.

/// <summary>
///     Which buttons a message box offers.
/// </summary>
internal enum UiMessageButtons
{
    OK = 0,
    OKCancel = 1,
    AbortRetryIgnore = 2,
    YesNoCancel = 3,
    YesNo = 4,
    RetryCancel = 5
}

/// <summary>
///     Which icon a message box shows.
/// </summary>
internal enum UiMessageIcon
{
    None = 0,
    Error = 16,
    Question = 32,
    Warning = 48,
    Information = 64
}

/// <summary>
///     What the user chose in a message box.
/// </summary>
internal enum UiDialogResult
{
    None = 0,
    OK = 1,
    Cancel = 2,
    Abort = 3,
    Retry = 4,
    Ignore = 5,
    Yes = 6,
    No = 7
}

/// <summary>
///     Which stage the "please wait" box shows while a folder is read.
/// </summary>
internal enum UiWaitStage
{
    Scanning,
    Parsing
}

/// <summary>
///     State of the taskbar button's progress indicator.
/// </summary>
internal enum UiTaskbarProgress
{
    NoProgress,
    Indeterminate,
    Normal,
    Error,
    Paused
}

/// <summary>
///     What UI-free code needs from whichever UI hosts it. Each app registers its own implementation at start-up
///     through <see cref="UiServices.Host" />.
/// </summary>
internal interface IUiHost
{
    /// <summary>
    ///     Shows a message box and waits. Must be safe to call from worker threads.
    /// </summary>
    UiDialogResult ShowMessage(string message,
                               string caption,
                               UiMessageButtons buttons,
                               UiMessageIcon icon);

    /// <summary>
    ///     Shows the changelog sections in a dialog. Before an update the answer is Yes (update now) or No (later);
    ///     after one it is just closed, and <paramref name="doNotShowAgain" /> says whether the user asked not to see
    ///     it after future updates.
    /// </summary>
    UiDialogResult ShowChangelog(bool beforeUpdate,
                                 string intro,
                                 IReadOnlyList<ChangelogSection> sections,
                                 out bool doNotShowAgain);

    /// <summary>
    ///     Tells the "please wait" box (if one is open) which stage the folder read has reached.
    /// </summary>
    void SetWaitStage(UiWaitStage stage);

    /// <summary>
    ///     Sets the taskbar button's progress indicator. Must be called on the UI thread.
    /// </summary>
    void SetTaskbarProgress(UiTaskbarProgress state);

    /// <summary>
    ///     Lets the UI repaint and answer input during a long loop on the UI thread (WinForms: Application.DoEvents).
    /// </summary>
    void ProcessUiEvents();

    /// <summary>
    ///     Sets the status text next to the progress bar. Safe to call from any thread.
    /// </summary>
    void SetStatusText(string text);

    /// <summary>
    ///     Scrolls the file list so the entry with this file name is visible.
    /// </summary>
    void ScrollFileListTo(string itemText);

    /// <summary>
    ///     Shows a file's list entry in the normal colour again (its pending edits have been written).
    /// </summary>
    void MarkFileListItemClean(DirectoryElement directoryElement);

    /// <summary>
    ///     GeoNames returned several places for a coordinate: lets the user choose one (the host may first show the file
    ///     in question). Blocks until chosen.
    /// </summary>
    /// <returns>Zero-based index of the chosen row; zero when nothing could be asked.</returns>
    int PickToponomyRow(DataTable choices,
                        string fileNameWithoutPath);

    /// <summary>
    ///     The files currently selected in the file list.
    /// </summary>
    IReadOnlyList<DirectoryElement> GetSelectedDirectoryElements();

    /// <summary>
    ///     Shows a file's list entry in the "has pending changes / problem" colour.
    /// </summary>
    void MarkFileListItemChanged(DirectoryElement directoryElement);

    /// <summary>
    ///     Re-draws a file's list row from its Stage3ReadyToWrite values.
    /// </summary>
    Task RefreshFileListRowAsync(DirectoryElement directoryElement);

    /// <summary>
    ///     Asks the map to re-read and redraw (for example after track overlay data changed).
    /// </summary>
    void RequestMapRefresh();

    /// <summary>
    ///     Shows a block of text (a report) in a dialog and waits for it to be closed.
    /// </summary>
    void ShowReport(string text);

    /// <summary>
    ///     Closes the application (a start-up problem that can't be recovered from).
    /// </summary>
    void ExitApplication();

    /// <summary>
    ///     Re-reads the favourites into the favourites drop-down.
    /// </summary>
    void ReloadFavouritesDropDown();
}

/// <summary>
///     Entry point for UI-free code that has to tell the user something.
/// </summary>
internal static class UiServices
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     The host's implementation. Until one is registered (tests, or before start-up finishes) messages are only
    ///     logged and the answer is <see cref="UiDialogResult.OK" /> (or <see cref="UiDialogResult.None" /> when the box
    ///     offers more than OK).
    /// </summary>
    internal static IUiHost Host { get; set; }

    internal static UiDialogResult ShowMessageBoxWithResult(string message,
                                                            string caption = null,
                                                            UiMessageButtons buttons = UiMessageButtons.OK,
                                                            UiMessageIcon icon = UiMessageIcon.None)
    {
        if (Host != null)
        {
            return Host.ShowMessage(message: message, caption: caption, buttons: buttons, icon: icon);
        }

        Log.Warn(message: $"No dialog host registered. Message not shown: {message}");
        return buttons == UiMessageButtons.OK
            ? UiDialogResult.OK
            : UiDialogResult.None;
    }

    internal static void ShowMessageBox(string message,
                                        string caption = null,
                                        UiMessageButtons buttons = UiMessageButtons.OK,
                                        UiMessageIcon icon = UiMessageIcon.None)
    {
        _ = ShowMessageBoxWithResult(message: message, caption: caption, buttons: buttons, icon: icon);
    }

    internal static void ShowChangelogAfterUpdate(int lastRunBuild,
                                                  int currentBuild)
    {
        ChangelogNotes.ShowAfterUpdate(lastRunBuild: lastRunBuild, currentBuild: currentBuild);
    }

    internal static UiDialogResult ShowChangelog(bool beforeUpdate,
                                                 string intro,
                                                 IReadOnlyList<ChangelogSection> sections,
                                                 out bool doNotShowAgain)
    {
        if (Host != null)
        {
            return Host.ShowChangelog(beforeUpdate: beforeUpdate, intro: intro, sections: sections,
                doNotShowAgain: out doNotShowAgain);
        }

        doNotShowAgain = false;
        return UiDialogResult.None;
    }

    internal static void SetWaitStage(UiWaitStage stage)
    {
        Host?.SetWaitStage(stage: stage);
    }

    internal static void SetTaskbarProgress(UiTaskbarProgress state)
    {
        Host?.SetTaskbarProgress(state: state);
    }

    internal static void ProcessUiEvents()
    {
        Host?.ProcessUiEvents();
    }

    internal static void SetStatusText(string text)
    {
        Host?.SetStatusText(text: text);
    }

    internal static void ScrollFileListTo(string itemText)
    {
        Host?.ScrollFileListTo(itemText: itemText);
    }

    internal static void MarkFileListItemClean(DirectoryElement directoryElement)
    {
        Host?.MarkFileListItemClean(directoryElement: directoryElement);
    }

    internal static int PickToponomyRow(DataTable choices,
                                        string fileNameWithoutPath)
    {
        return Host?.PickToponomyRow(choices: choices, fileNameWithoutPath: fileNameWithoutPath) ?? 0;
    }

    internal static IReadOnlyList<DirectoryElement> GetSelectedDirectoryElements()
    {
        return Host?.GetSelectedDirectoryElements() ?? [];
    }

    internal static void MarkFileListItemChanged(DirectoryElement directoryElement)
    {
        Host?.MarkFileListItemChanged(directoryElement: directoryElement);
    }

    internal static Task RefreshFileListRowAsync(DirectoryElement directoryElement)
    {
        return Host?.RefreshFileListRowAsync(directoryElement: directoryElement) ?? Task.CompletedTask;
    }

    internal static void RequestMapRefresh()
    {
        Host?.RequestMapRefresh();
    }

    internal static void ShowReport(string text)
    {
        Host?.ShowReport(text: text);
    }

    internal static void ExitApplication()
    {
        Host?.ExitApplication();
    }

    internal static void ReloadFavouritesDropDown()
    {
        Host?.ReloadFavouritesDropDown();
    }
}
