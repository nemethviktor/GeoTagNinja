using GeoTagNinja.Helpers.Help;
using GeoTagNinja.Helpers.UI;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using NLog;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     The in-app Help (<c>F1</c>): a list of topics on the left and the selected topic on the right. Open it through
///     <see cref="Show" />, which keeps a single window and moves it to the topic asked for.
/// </summary>
/// <remarks>
///     Built entirely in code, so there is no designer or .resx to keep in step. The text is in
///     <c>GeoTagNinja.Core/Helpers/Help/help.md</c> (see <see cref="HelpContent" />); it is English on purpose, like the
///     update changelog. The window is not modal, so it can stay open beside the app. The page is rendered in a WebView2
///     with scripts off, links open in the default browser, and if WebView2 can't start the topic is shown plain.
/// </remarks>
internal sealed class FrmHelp : Form
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static FrmHelp _instance;

    private readonly ListBox _topicList = new();
    private readonly WebView2 _web = new();
    private TextBox _plain;
    private bool _webReady;
    private bool _webUnavailable;
    private HelpTopic _current;

    private FrmHelp()
    {
        Text = "GeoTagNinja - Help";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        ShowIcon = false;
        KeyPreview = true;
        ClientSize = new Size(width: 900, height: 600);
        MinimumSize = new Size(width: 560, height: 360);

        _topicList.Dock = DockStyle.Left;
        _topicList.Width = 210;
        _topicList.IntegralHeight = false;
        _topicList.BorderStyle = BorderStyle.None;
        foreach (HelpTopic topic in HelpContent.Topics)
        {
            _ = _topicList.Items.Add(item: topic);
        }

        _topicList.SelectedIndexChanged += (_,
                                            _) =>
        {
            if (_topicList.SelectedItem is HelpTopic topic)
            {
                ShowTopic(topic: topic);
            }
        };

        Splitter splitter = new() { Dock = DockStyle.Left, Width = 4 };

        _web.Dock = DockStyle.Fill;
        _web.AllowExternalDrop = false;
        _web.DefaultBackgroundColor = ThemeHelper.IsDark
            ? Color.FromArgb(red: 32, green: 32, blue: 32)
            : Color.White;

        // z-order: fill first, so the docked list and splitter are laid out before it
        Controls.Add(value: _web);
        Controls.Add(value: splitter);
        Controls.Add(value: _topicList);

        KeyDown += (_,
                    e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                Close();
            }
        };

        // F1 inside the Help itself would only reopen it
        HelpRequested += (_,
                          e) => e.Handled = true;

        Shown += async (_,
                        _) =>
        {
            ThemeHelper.ApplyTo(control: this);
            await InitialiseWebView();
        };

        FormClosed += (_,
                       _) => _instance = null;
    }

    /// <summary>
    ///     Opens the Help at a topic, or brings the open one to the front and moves it there.
    /// </summary>
    /// <param name="owner">The form the user pressed F1 in (null for none).</param>
    /// <param name="topicId">A topic id from <c>help.md</c>; unknown or null opens the first topic.</param>
    internal static void Show(IWin32Window owner,
                              string topicId)
    {
        HelpTopic topic = HelpContent.Find(id: topicId);

        if (_instance == null ||
            _instance.IsDisposed)
        {
            _instance = new FrmHelp();
            _instance.SelectTopic(topic: topic);
            _instance.Show(owner: owner);
        }
        else
        {
            _instance.SelectTopic(topic: topic);
            if (_instance.WindowState == FormWindowState.Minimized)
            {
                _instance.WindowState = FormWindowState.Normal;
            }

            _instance.Activate();
        }
    }

    /// <summary>
    ///     Makes <c>F1</c> in <paramref name="form" /> (or any control on it) open the Help at a topic. Call it once, after
    ///     the form's controls exist. The main window does not need it: its <c>Help</c> menu item owns <c>F1</c>.
    /// </summary>
    /// <param name="form">The form to wire up.</param>
    /// <param name="topicId">The topic that matches what the form does (an id from <c>help.md</c>).</param>
    internal static void Attach(Form form,
                                string topicId)
    {
        form.HelpRequested += (_,
                               e) =>
        {
            e.Handled = true;
            Show(owner: form, topicId: topicId);
        };
    }

    private void SelectTopic(HelpTopic topic)
    {
        foreach (object item in _topicList.Items)
        {
            if (item is HelpTopic candidate &&
                candidate.Id == topic.Id)
            {
                _topicList.SelectedItem = item;
                return;
            }
        }
    }

    private void ShowTopic(HelpTopic topic)
    {
        _current = topic;
        if (_webUnavailable)
        {
            ShowPlainText();
        }
        else if (_webReady)
        {
            _web.NavigateToString(htmlContent: HelpContent.ToDocument(topic: topic, dark: ThemeHelper.IsDark));
        }

        // otherwise InitialiseWebView shows _current once the viewer is up
    }

    /// <summary>
    ///     Starts the WebView2. (The map and the changelog use the same environment settings; WebView2 refuses two
    ///     different ones on the same user-data folder.)
    /// </summary>
    private async System.Threading.Tasks.Task InitialiseWebView()
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

            _webReady = true;
            if (_current != null)
            {
                ShowTopic(topic: _current);
            }
        }
        catch (Exception ex)
        {
            Log.Warn(message: $"Help: WebView2 not available, showing plain text: {ex.Message}");
            _webUnavailable = true;
            if (_current != null)
            {
                ShowPlainText();
            }
        }
    }

    private void ShowPlainText()
    {
        if (_plain == null)
        {
            _plain = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font(familyName: "Segoe UI", emSize: 9.5f)
            };
            _web.Visible = false;
            Controls.Add(value: _plain);
            _plain.BringToFront();
            ThemeHelper.ApplyTo(control: _plain);
        }

        _plain.Text = HelpContent.ToPlainText(topic: _current);
        _plain.SelectionStart = 0;
        _plain.SelectionLength = 0;
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
                Log.Warn(message: $"Help: could not open {uri}: {ex.Message}");
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
