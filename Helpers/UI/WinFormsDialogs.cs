using GeoTagNinja.Helpers.Update;
using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;
using GeoTagNinja.View.FileList;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     The WinForms host's answer to <see cref="IUiHost" />: the themed message box of <see cref="ThemeHelper" />.
///     The Core enums share their numeric values with the WinForms ones, hence the casts.
/// </summary>
internal sealed class WinFormsDialogs : IUiHost
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

    public UiDialogResult ShowChangelog(bool beforeUpdate,
                                        string intro,
                                        IReadOnlyList<ChangelogSection> sections,
                                        out bool doNotShowAgain)
    {
        using FrmChangelog dialog = new(
            mode: beforeUpdate ? FrmChangelog.Mode.BeforeUpdate : FrmChangelog.Mode.AfterUpdate,
            intro: intro, sections: sections.ToList());
        DialogResult result = dialog.ShowDialog(owner: FrmMainApp.Instance);
        doNotShowAgain = dialog.DoNotShowAgain;
        return (UiDialogResult)(int)result;
    }

    public void SetWaitStage(UiWaitStage stage)
    {
        FrmPleaseWaitBox waitBox = (FrmPleaseWaitBox)Application.OpenForms[name: "FrmPleaseWaitBox"];
        waitBox?.UpdateControlsVisibility(stage: stage == UiWaitStage.Scanning
            ? FrmPleaseWaitBox.ActionStages.SCANNING
            : FrmPleaseWaitBox.ActionStages.PARSING);
    }

    public void SetTaskbarProgress(UiTaskbarProgress state)
    {
        FrmMainApp.TaskbarManagerInstance.SetProgressState(state: state switch
        {
            UiTaskbarProgress.Indeterminate => TaskbarProgressBarState.Indeterminate,
            UiTaskbarProgress.Normal => TaskbarProgressBarState.Normal,
            UiTaskbarProgress.Error => TaskbarProgressBarState.Error,
            UiTaskbarProgress.Paused => TaskbarProgressBarState.Paused,
            _ => TaskbarProgressBarState.NoProgress
        });
    }

    public void ProcessUiEvents()
    {
        Application.DoEvents();
    }

    public void SetStatusText(string text)
    {
        FrmMainApp main = FrmMainApp.Instance;
        if (main != null)
        {
            FrmMainApp.HandlerUpdateLabelText(label: main.lbl_ParseProgress, text: text);
        }
    }

    public void ScrollFileListTo(string itemText)
    {
        FrmMainApp.Instance?.lvw_FileList.ScrollToDataPoint(itemText: itemText);
    }

    public void MarkFileListItemClean(DirectoryElement directoryElement)
    {
        FrmMainApp.Instance?.lvw_FileList.UpdateDirectoryElementItemColour(
            directoryElement: directoryElement, color: System.Drawing.Color.Black);
    }

    public int PickToponomyRow(DataTable choices,
                               string fileNameWithoutPath)
    {
        return ToponomyPicker.Pick(dtSQLToponomyData: choices, fileNameWithoutPath: fileNameWithoutPath);
    }

    public IReadOnlyList<DirectoryElement> GetSelectedDirectoryElements()
    {
        FrmMainApp main = FrmMainApp.Instance;
        return main == null
            ? []
            : main.lvw_FileList.SelectedItems.Cast<ListViewItem>()
                  .Select(selector: item => item.Tag as DirectoryElement)
                  .Where(predicate: element => element != null)
                  .ToList();
    }

    public void MarkFileListItemChanged(DirectoryElement directoryElement)
    {
        FrmMainApp.Instance?.lvw_FileList.UpdateDirectoryElementItemColour(
            directoryElement: directoryElement, color: ThemeHelper.ErrorColour);
    }

    public Task RefreshFileListRowAsync(DirectoryElement directoryElement)
    {
        return FileListViewReadWrite.ListViewUpdateRowFromDEStage3ReadyToWrite(dirElemFileToModify: directoryElement);
    }

    public void RequestMapRefresh()
    {
        FrmMainApp.Instance?.Request_Map_NavigateGo();
    }

    public void ShowReport(string text)
    {
        ReportBox.Show(text: text);
    }

    public void ExitApplication()
    {
        Application.Exit();
    }

    public void ReloadFavouritesDropDown()
    {
        FrmMainApp.Instance?.ClearReloadFavouritesDropDownValues();
    }
}
