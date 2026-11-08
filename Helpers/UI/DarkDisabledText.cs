using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     Makes the caption of a disabled control readable in dark mode.
/// </summary>
/// <remarks>
///     <para>
///         WinForms draws disabled text in a colour derived from the control's back colour (labels, via
///         <c>ControlPaint.Dark</c>) or in the system's grey (buttons, check boxes), and on the dark background that
///         comes out as near-black on near-black. The themer does not touch it, and a <c>ForeColor</c> is ignored for
///         disabled controls, so the caption is painted again on top, in a muted but legible grey.
///     </para>
///     <para>
///         Covers <see cref="Label" />, <see cref="CheckBox" />, <see cref="RadioButton" />, <see cref="Button" /> and
///         <see cref="GroupBox" />. A control counts as disabled when it, or anything above it, is: a child of a
///         disabled group box keeps <c>Enabled == true</c> but is drawn disabled. Text boxes and drop-downs are left as
///         they are (their disabled text is drawn in a colour that is already legible).
///     </para>
/// </remarks>
internal static class DarkDisabledText
{
    private static readonly Color DisabledText = Color.FromArgb(red: 150, green: 155, blue: 165);

    private static readonly ConditionalWeakTable<Control, object> Hooked = new();

    /// <summary>
    ///     Hooks every supported control under <paramref name="root" />. Safe to call again on the same tree.
    /// </summary>
    internal static void ApplyTo(Control root)
    {
        if (root is Label or CheckBox or RadioButton or Button or GroupBox &&
            !Hooked.TryGetValue(key: root, value: out _))
        {
            Hooked.Add(key: root, value: new object());
            root.Paint += OnPaint;
            // children of a disabled parent are redrawn by WinForms when it changes, but ask anyway.
            root.EnabledChanged += (sender,
                                    _) => ((Control)sender).Invalidate();
        }

        foreach (Control child in root.Controls)
        {
            ApplyTo(root: child);
        }
    }

    private static void OnPaint(object sender,
                                PaintEventArgs e)
    {
        Control control = (Control)sender;
        if (IsEnabled(control: control) || string.IsNullOrEmpty(value: control.Text))
        {
            return;
        }

        Color back = EffectiveBackColour(control: control);
        Rectangle area;
        TextFormatFlags flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding;

        switch (control)
        {
            case Button:
                area = Rectangle.Inflate(rect: control.ClientRectangle, x: -2, y: -2);
                flags |= TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
                break;
            case CheckBox or RadioButton:
                int glyph = (int)(e.Graphics.DpiX / 96f * 13) + 4;
                area = new Rectangle(x: glyph, y: 0, width: control.Width - glyph, height: control.Height);
                flags |= TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
                break;
            case GroupBox:
                Size captionSize = TextRenderer.MeasureText(text: control.Text, font: control.Font);
                area = new Rectangle(x: (int)(e.Graphics.DpiX / 96f * 7), y: 0, width: captionSize.Width + 2,
                    height: captionSize.Height);
                flags |= TextFormatFlags.Left | TextFormatFlags.Top;
                break;
            default: // Label
                area = control.ClientRectangle;
                flags |= AlignmentFlags(alignment: ((Label)control).TextAlign);
                break;
        }

        using (SolidBrush brush = new(color: back))
        {
            e.Graphics.FillRectangle(brush: brush, rect: area);
        }

        TextRenderer.DrawText(dc: e.Graphics, text: control.Text, font: control.Font, bounds: area,
            foreColor: DisabledText, backColor: back, flags: flags);
    }

    /// <summary>
    ///     Whether the control, and every control it sits in, is enabled.
    /// </summary>
    private static bool IsEnabled(Control control)
    {
        for (Control current = control; current != null; current = current.Parent)
        {
            if (!current.Enabled)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     The colour actually showing behind a control: its own, or the nearest parent's when it is transparent.
    /// </summary>
    private static Color EffectiveBackColour(Control control)
    {
        for (Control current = control; current != null; current = current.Parent)
        {
            if (current.BackColor.A == 255)
            {
                return current.BackColor;
            }
        }

        return ThemeHelper.BackDark;
    }

    private static TextFormatFlags AlignmentFlags(ContentAlignment alignment)
    {
        return alignment switch
        {
            ContentAlignment.TopLeft => TextFormatFlags.Left | TextFormatFlags.Top,
            ContentAlignment.TopCenter => TextFormatFlags.HorizontalCenter | TextFormatFlags.Top,
            ContentAlignment.TopRight => TextFormatFlags.Right | TextFormatFlags.Top,
            ContentAlignment.MiddleLeft => TextFormatFlags.Left | TextFormatFlags.VerticalCenter,
            ContentAlignment.MiddleCenter => TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
            ContentAlignment.MiddleRight => TextFormatFlags.Right | TextFormatFlags.VerticalCenter,
            ContentAlignment.BottomLeft => TextFormatFlags.Left | TextFormatFlags.Bottom,
            ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom,
            _ => TextFormatFlags.Right | TextFormatFlags.Bottom
        };
    }
}
