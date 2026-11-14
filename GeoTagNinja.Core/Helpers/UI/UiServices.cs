using NLog;

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
///     What UI-free code needs from whichever UI hosts it. Each app registers its own implementation at start-up
///     through <see cref="UiServices.Dialogs" />.
/// </summary>
internal interface IUserDialogs
{
    /// <summary>
    ///     Shows a message box and waits. Must be safe to call from worker threads.
    /// </summary>
    UiDialogResult ShowMessage(string message,
                               string caption,
                               UiMessageButtons buttons,
                               UiMessageIcon icon);

    /// <summary>
    ///     Shows what changed since the build that last ran, when the user wants that (see ChangelogNotes).
    /// </summary>
    void ShowChangelogAfterUpdate(int lastRunBuild,
                                  int currentBuild);
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
    internal static IUserDialogs Dialogs { get; set; }

    internal static UiDialogResult ShowMessageBoxWithResult(string message,
                                                            string caption = null,
                                                            UiMessageButtons buttons = UiMessageButtons.OK,
                                                            UiMessageIcon icon = UiMessageIcon.None)
    {
        if (Dialogs != null)
        {
            return Dialogs.ShowMessage(message: message, caption: caption, buttons: buttons, icon: icon);
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
        Dialogs?.ShowChangelogAfterUpdate(lastRunBuild: lastRunBuild, currentBuild: currentBuild);
    }
}
