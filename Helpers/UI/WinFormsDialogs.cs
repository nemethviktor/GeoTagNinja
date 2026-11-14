using GeoTagNinja.Helpers.Update;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     The WinForms host's answer to <see cref="IUserDialogs" />: the themed message box of <see cref="ThemeHelper" />.
///     The Core enums share their numeric values with the WinForms ones, hence the casts.
/// </summary>
internal sealed class WinFormsDialogs : IUserDialogs
{
    public UiDialogResult ShowMessage(string message,
                                      string caption,
                                      UiMessageButtons buttons,
                                      UiMessageIcon icon)
    {
        return (UiDialogResult)(int)ThemeHelper.ShowMessageBoxWithResult(
            message: message,
            caption: caption,
            buttons: (MessageBoxButtons)(int)buttons,
            icon: (MessageBoxIcon)(int)icon);
    }

    public void ShowChangelogAfterUpdate(int lastRunBuild,
                                         int currentBuild)
    {
        ChangelogNotes.ShowAfterUpdate(lastRunBuild: lastRunBuild, currentBuild: currentBuild);
    }
}
