using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinFormsDarkThemerNinja;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     Single entry point for dark mode. Wraps <see cref="Themer" /> and adds the things it doesn't do (title bars,
///     scrollbars, dynamically-built menus, item colours that stay legible on a dark background).
/// </summary>
/// <remarks>
///     In light mode nothing is styled: the only work done is undoing designer settings (owner-draw tabs/lists) that
///     only make sense when themed. Everything here is safe to call from a designer-instantiated control.
/// </remarks>
internal static class ThemeHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    /// <summary>
    ///     Background of themed surfaces. Kept identical to the colour the file list menus already used.
    /// </summary>
    internal static readonly Color BackDark = ColorTranslator.FromHtml(htmlColor: "#1C1D23");

    internal static readonly Color ForeDark = Color.White;

    /// <summary>
    ///     Slightly lighter than <see cref="BackDark" />, for hover/selection.
    /// </summary>
    internal static readonly Color HighlightDark = ColorTranslator.FromHtml(htmlColor: "#3A3D4A");

    internal static bool IsDark => HelperVariables.UserSettingUseDarkMode;

    /// <summary>
    ///     Colour for items flagged as errored/changed. Plain red in light mode (unchanged), a lighter red in dark mode
    ///     where pure red on near-black is hard to read.
    /// </summary>
    internal static Color ErrorColour => IsDark ? Color.FromArgb(red: 255, green: 110, blue: 110) : Color.Red;

    /// <summary>
    ///     Colour for normal items. Black in light mode (unchanged), white in dark mode.
    /// </summary>
    internal static Color DefaultItemColour => IsDark ? ForeDark : Color.Black;

    /// <summary>
    ///     Maps the legacy literals Red/Black onto their theme-aware equivalents. Any other colour passes through.
    /// </summary>
    internal static Color MapItemColour(Color color)
    {
        if (color.ToArgb() == Color.Red.ToArgb())
        {
            return ErrorColour;
        }

        if (color.ToArgb() == Color.Black.ToArgb())
        {
            return DefaultItemColour;
        }

        return color;
    }

    private static bool InDesigner => LicenseManager.UsageMode == LicenseUsageMode.Designtime;

    /// <summary>
    ///     Themes a form (or any control tree) according to the user's setting.
    /// </summary>
    /// <param name="control">The root control, normally a Form.</param>
    internal static void ApplyTo(Control control)
    {
        if (control == null || InDesigner)
        {
            return;
        }

        Themer.ApplyThemeToControl(
            control: control,
            themeStyle: IsDark ? Themer.ThemeStyle.Custom : Themer.ThemeStyle.Default);

        if (!IsDark)
        {
            ResetOwnerDraw(root: control);
            return;
        }

        if (control is Form form)
        {
            ApplyTitleBar(form: form);
        }

        ApplyScrollbars(root: control);
    }

    /// <summary>
    ///     The designer sets owner-draw on tabs and lists because the themer needs it; unthemed they look off.
    /// </summary>
    private static void ResetOwnerDraw(Control root)
    {
        switch (root)
        {
            case TabControl tcr:
                tcr.DrawMode = TabDrawMode.Normal;
                break;
            case ListView lvw:
                lvw.OwnerDraw = false;
                break;
        }

        foreach (Control child in root.Controls)
        {
            ResetOwnerDraw(root: child);
        }
    }

    private static void ApplyTitleBar(Form form)
    {
        void Set()
        {
            int on = 1;
            if (DwmSetWindowAttribute(hwnd: form.Handle, attr: DWMWA_USE_IMMERSIVE_DARK_MODE, attrValue: ref on,
                    attrSize: sizeof(int)) != 0)
            {
                _ = DwmSetWindowAttribute(hwnd: form.Handle, attr: DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1,
                    attrValue: ref on, attrSize: sizeof(int));
            }
        }

        if (form.IsHandleCreated)
        {
            Set();
        }

        // handles get recreated (e.g. ShowInTaskbar / ControlBox changes), so don't rely on the first one.
        form.HandleCreated += (_, _) => Set();
    }

    private static void ApplyScrollbars(Control root)
    {
        if (root is ListView or TreeView or ListBox or TextBoxBase)
        {
            void Set()
            {
                _ = SetWindowTheme(hwnd: root.Handle, subAppName: "DarkMode_Explorer", subIdList: null);
            }

            if (root.IsHandleCreated)
            {
                Set();
            }

            root.HandleCreated += (_, _) => Set();
        }

        foreach (Control child in root.Controls)
        {
            ApplyScrollbars(root: child);
        }
    }

    /// <summary>
    ///     Replacement for raw <see cref="MessageBox.Show(string)" /> calls: themed in dark mode, the unchanged native box in
    ///     light mode. Safe to call from worker threads and before settings/forms exist.
    /// </summary>
    internal static DialogResult ShowMessageBoxWithResult(string message,
                                                          string caption = null,
                                                          MessageBoxButtons buttons = MessageBoxButtons.OK,
                                                          MessageBoxIcon icon = MessageBoxIcon.None)
    {
        Form owner = View.Forms.FrmMainApp.Instance;
        if (owner is { InvokeRequired: true })
        {
            return (DialogResult)owner.Invoke(method: new Func<DialogResult>(() =>
                ShowMessageBoxWithResult(message: message, caption: caption, buttons: buttons, icon: icon)));
        }

        return IsDark
            ? Themer.ShowMessageBoxWithResult(message: message, caption: caption, buttons: buttons, icon: icon,
                themeStyle: Themer.ThemeStyle.Custom)
            : MessageBox.Show(text: message, caption: caption ?? string.Empty, buttons: buttons, icon: icon);
    }

    /// <inheritdoc cref="ShowMessageBoxWithResult" />
    internal static void ShowMessageBox(string message,
                                        string caption = null,
                                        MessageBoxButtons buttons = MessageBoxButtons.OK,
                                        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        _ = ShowMessageBoxWithResult(message: message, caption: caption, buttons: buttons, icon: icon);
    }

    /// <summary>
    ///     Styles a ToolStrip (context menu, dropdown) and all its items, recursively. No-op in light mode.
    ///     Separators and arrows are handled by the renderer; BackColor alone doesn't reach them.
    /// </summary>
    internal static void StyleMenu(ToolStrip menu)
    {
        if (!IsDark || menu == null)
        {
            return;
        }

        menu.Renderer = new DarkMenuRenderer();
        menu.BackColor = BackDark;
        menu.ForeColor = ForeDark;
        StyleItems(items: menu.Items);
    }

    private static void StyleItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.BackColor = BackDark;
            item.ForeColor = ForeDark;
            if (item is ToolStripDropDownItem { HasDropDownItems: true } dropDownItem)
            {
                dropDownItem.DropDown.Renderer = new DarkMenuRenderer();
                dropDownItem.DropDown.BackColor = BackDark;
                StyleItems(items: dropDownItem.DropDownItems);
            }
        }
    }

    /// <summary>
    ///     Renders menus in the dark palette, including the separators and arrows BackColor doesn't reach.
    /// </summary>
    internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(professionalColorTable: new DarkColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = ForeDark;
            base.OnRenderArrow(e: e);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? ForeDark : Color.Gray;
            base.OnRenderItemText(e: e);
        }
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => BackDark;
        public override Color ImageMarginGradientBegin => BackDark;
        public override Color ImageMarginGradientMiddle => BackDark;
        public override Color ImageMarginGradientEnd => BackDark;
        public override Color MenuBorder => HighlightDark;
        public override Color MenuItemBorder => HighlightDark;
        public override Color MenuItemSelected => HighlightDark;
        public override Color MenuItemSelectedGradientBegin => HighlightDark;
        public override Color MenuItemSelectedGradientEnd => HighlightDark;
        public override Color MenuItemPressedGradientBegin => HighlightDark;
        public override Color MenuItemPressedGradientEnd => HighlightDark;
        public override Color SeparatorDark => HighlightDark;
        public override Color SeparatorLight => BackDark;
    }

    [DllImport(dllName: "dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd,
                                                    int attr,
                                                    ref int attrValue,
                                                    int attrSize);

    [DllImport(dllName: "uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd,
                                             string subAppName,
                                             string subIdList);
}
