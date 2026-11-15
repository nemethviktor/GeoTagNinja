using GeoTagNinja.Helpers.UI;
using Microsoft.Web.WebView2.Core;
using NLog;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.Startup;

/// <summary>
///     The WebView2 check that belongs to the WinForms app: the Avalonia app brings its own WebView2 reference.
/// </summary>
internal static class WebView2Check
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Makes sure there is webview2 installed and working
    /// </summary>
    public static Task Run()
    {
        // Check webView2 availability
        Log.Info(message: "Starting");

        try
        {
            string webView2Version = "";
            webView2Version =
                CoreWebView2Environment.GetAvailableBrowserVersionString(
                    browserExecutableFolder: null);
            Log.Trace(message: $"Check webView2 version is: {webView2Version}");
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Error: {ex.Message}");
            UiServices.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_FrmMainApp_ErrorCantLoadWebView2",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox
                    ) +
                    Environment.NewLine + $"{ex.Message}",
                icon: UiMessageIcon.Error,
                buttons: UiMessageButtons.OK);

            Application.Exit();
        }

        return Task.CompletedTask;
    }
}
