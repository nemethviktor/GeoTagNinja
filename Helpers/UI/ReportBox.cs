using System.Drawing;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     The WinForms dialog that shows a block of report text (moved out of ReadTrackFile).
/// </summary>
internal static class ReportBox
{
    internal static void Show(string text)
    {
        Form reportBox = new()
        {
            ControlBox = false
        };
        FlowLayoutPanel panel = new();

        TextBox tbxText = new()
        {
            Size = new Size(width: 700, height: 400),

            Text = text,
            ScrollBars = ScrollBars.Vertical,
            Multiline = true,
            WordWrap = true,
            ReadOnly = true,
            SelectionStart = 1,
            SelectionLength = 0
        };

        panel.SetFlowBreak(control: tbxText, value: true);
        panel.Controls.Add(value: tbxText);

        Button btnOk = new()
        { Text = "OK" };
        btnOk.Click += (sender,
                        e) =>
        {
            reportBox.Close();
        };
        btnOk.Location = new Point(x: 10, y: tbxText.Bottom + 5);
        btnOk.AutoSize = true;
        panel.Controls.Add(value: btnOk);

        panel.Padding = new Padding(all: 5);
        panel.AutoSize = true;

        reportBox.Controls.Add(value: panel);
        reportBox.MinimumSize =
            new Size(width: tbxText.Width + 40, height: btnOk.Bottom + 50);
        reportBox.ShowInTaskbar = false;

        reportBox.StartPosition = FormStartPosition.CenterScreen;
        ThemeHelper.ApplyTo(control: reportBox);
        reportBox.ShowDialog();
    }
}
