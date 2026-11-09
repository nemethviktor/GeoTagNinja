using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Helpers.Update;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     Shows what changed in one or more released builds: before an update (with Update now / Later) or after one
///     (with a tick to stop being shown this).
/// </summary>
/// <remarks>
///     Built entirely in code, so there is no designer or .resx to keep in step. The text is English and lives in
///     code on purpose: a build can go out before translators catch up. The changelog is rendered as HTML in a
///     WebView2 with scripts off, and any link in it opens in the default browser rather than inside the dialog. If
///     WebView2 can't start, the same text is shown plain.
/// </remarks>
internal sealed class FrmChangelog : Form
{
    /// <summary>Which of the two situations the dialog is shown in.</summary>
    internal enum Mode
    {
        /// <summary>An update is available: Update now (<see cref="DialogResult.Yes" />) or Later.</summary>
        BeforeUpdate,

        /// <summary>The app has just been updated: just Close, and a tick to not be shown this again.</summary>
        AfterUpdate
    }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IReadOnlyList<ChangelogSection> _sections;
    private readonly WebView2 _web = new();
    private readonly CheckBox _ckbDoNotShowAgain = new();

    /// <summary>
    ///     After the dialog closes in <see cref="Mode.AfterUpdate" />: true if the user ticked "don't show this
    ///     again".
    /// </summary>
    internal bool DoNotShowAgain => _ckbDoNotShowAgain.Checked;

    /// <param name="mode">Before or after an update.</param>
    /// <param name="intro">The sentence above the notes.</param>
    /// <param name="sections">What to show (newest first).</param>
    internal FrmChangelog(Mode mode,
                          string intro,
                          IReadOnlyList<ChangelogSection> sections)
    {
        _sections = sections;

        Text = mode == Mode.BeforeUpdate
            ? "GeoTagNinja - update available"
            : "GeoTagNinja - what's new";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        ShowIcon = false;
        ClientSize = new Size(width: 760, height: 560);
        MinimumSize = new Size(width: 480, height: 320);

        Label lblIntro = new()
        {
            Text = intro,
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 52,
            Padding = new Padding(left: 12, top: 10, right: 12, bottom: 4)
        };

        Panel pnlButtons = new()
        {
            Dock = DockStyle.Bottom,
            Height = 48
        };

        _web.Dock = DockStyle.Fill;
        _web.AllowExternalDrop = false;
        _web.DefaultBackgroundColor = ThemeHelper.IsDark
            ? Color.FromArgb(red: 32, green: 32, blue: 32)
            : Color.White;

        if (mode == Mode.BeforeUpdate)
        {
            Button btnYes = new()
            {
                Text = "Update now",
                DialogResult = DialogResult.Yes,
                Size = new Size(width: 110, height: 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Button btnNo = new()
            {
                Text = "Later",
                DialogResult = DialogResult.No,
                Size = new Size(width: 110, height: 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlButtons.Controls.Add(value: btnYes);
            pnlButtons.Controls.Add(value: btnNo);
            pnlButtons.Resize += (_,
                                  _) => PlaceButtons(panel: pnlButtons, btnYes, btnNo);
            PlaceButtons(panel: pnlButtons, btnYes, btnNo);
            AcceptButton = btnYes;
            CancelButton = btnNo;
        }
        else
        {
            _ckbDoNotShowAgain.Text = "Don't show this after updates (can be turned back on in Settings)";
            _ckbDoNotShowAgain.AutoSize = true;
            _ckbDoNotShowAgain.Location = new Point(x: 12, y: 14);
            Button btnClose = new()
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Size = new Size(width: 110, height: 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlButtons.Controls.Add(value: _ckbDoNotShowAgain);
            pnlButtons.Controls.Add(value: btnClose);
            pnlButtons.Resize += (_,
                                  _) => PlaceButtons(panel: pnlButtons, btnClose);
            PlaceButtons(panel: pnlButtons, btnClose);
            AcceptButton = btnClose;
            CancelButton = btnClose;
        }

        // z-order: fill last, so the docked top/bottom strips are laid out first
        Controls.Add(value: _web);
        Controls.Add(value: pnlButtons);
        Controls.Add(value: lblIntro);

        Shown += async (_,
                        _) =>
        {
            ThemeHelper.ApplyTo(control: this);
            await ShowNotes();
        };
    }

    /// <summary>Lines the buttons up along the right edge, last one rightmost.</summary>
    private static void PlaceButtons(Panel panel,
                                     params Button[] buttons)
    {
        int right = panel.ClientSize.Width - 12;
        for (int i = buttons.Length - 1; i >= 0; i--)
        {
            buttons[i].Location = new Point(x: right - buttons[i].Width, y: 10);
            right -= buttons[i].Width + 8;
        }
    }

    /// <summary>
    ///     Renders the notes in the WebView2, or as plain text if that can't start. (The map uses the same environment
    ///     settings; WebView2 refuses two different ones on the same user-data folder.)
    /// </summary>
    private async System.Threading.Tasks.Task ShowNotes()
    {
        try
        {
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: Path.GetTempPath(),
                options: new CoreWebView2EnvironmentOptions(additionalBrowserArguments: null, language: "en"));
            await _web.EnsureCoreWebView2Async(environment: environment);

            _web.CoreWebView2.Settings.IsScriptEnabled = false;
            _web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _web.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _web.CoreWebView2.Settings.AreDevToolsEnabled = false;

            // links open in the browser, never in here
            _web.CoreWebView2.NavigationStarting += (_,
                                                     e) =>
            {
                if (e.IsUserInitiated)
                {
                    e.Cancel = true;
                    OpenExternally(uri: e.Uri);
                }
            };
            _web.CoreWebView2.NewWindowRequested += (_,
                                                     e) =>
            {
                e.Handled = true;
                OpenExternally(uri: e.Uri);
            };

            _web.NavigateToString(htmlContent: ChangelogParser.ToDocument(sections: _sections,
                dark: ThemeHelper.IsDark));
        }
        catch (Exception ex)
        {
            Log.Warn(message: $"Changelog: WebView2 not available, showing plain text: {ex.Message}");
            ShowPlainText();
        }
    }

    private void ShowPlainText()
    {
        TextBox plain = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = new Font(familyName: "Segoe UI", emSize: 9.5f),
            Text = ChangelogParser.ToPlainText(sections: _sections)
        };
        _web.Visible = false;
        Controls.Add(value: plain);
        plain.BringToFront();
        ThemeHelper.ApplyTo(control: plain);
        plain.SelectionStart = 0;
        plain.SelectionLength = 0;
    }

    private static void OpenExternally(string uri)
    {
        // only ever hand http(s) to the shell
        if (Uri.TryCreate(uriString: uri, uriKind: UriKind.Absolute, result: out Uri parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttps || parsed.Scheme == Uri.UriSchemeHttp))
        {
            try
            {
                _ = Process.Start(startInfo: new ProcessStartInfo(fileName: parsed.AbsoluteUri)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Warn(message: $"Changelog: could not open {uri}: {ex.Message}");
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _web.Dispose();
        }

        base.Dispose(disposing: disposing);
    }
}
