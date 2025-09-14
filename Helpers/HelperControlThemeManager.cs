using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static GeoTagNinja.Helpers.ThemeColour;

namespace GeoTagNinja.Helpers;

internal enum ThemeColour
{
    Light,
    Dark
}

/// <summary>
///     Provides methods for managing the theme of the application's controls.
/// </summary>
/// <remarks>
///     This class includes methods for applying the immersive dark mode to a window,
///     setting the theme color of the application during startup, and changing the theme of a specific control.
/// </remarks>
[SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming")]
internal static class HelperControlThemeManager
{
    private static readonly Color darkColor =
        ColorTranslator.FromHtml(htmlColor: "#1C1D23");

    private static readonly Color lessDarkColor =
        ColorTranslator.FromHtml(htmlColor: "#2B2D31");


    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [DllImport(dllName: "dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd,
                                                    int attr,
                                                    ref int attrValue,
                                                    int attrSize);

    /// <summary>
    ///     Applies the immersive dark mode to a window.
    /// </summary>
    /// <param name="handle">The handle of the window to which the dark mode will be applied.</param>
    /// <param name="enabled">A boolean value indicating whether to enable or disable the immersive dark mode.</param>
    /// <returns>Returns true if the operation was successful, otherwise returns false.</returns>
    /// <remarks>
    ///     This method uses the DwmSetWindowAttribute function from the dwmapi.dll library to apply the dark mode.
    ///     The method checks the version of Windows 10 and uses the appropriate attribute for the DwmSetWindowAttribute
    ///     function.
    ///     If the version of Windows 10 is less than 17763, the method returns false without making any changes.
    /// </remarks>
    private static bool UseImmersiveDarkMode(IntPtr handle,
                                             bool enabled)
    {
        if (IsWindows10OrGreater(build: 17763))
        {
            int attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
            if (IsWindows10OrGreater(build: 18985))
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
            }

            int useImmersiveDarkMode = enabled
                ? 1
                : 0;
            return DwmSetWindowAttribute(hwnd: handle, attr: attribute,
                                         attrValue: ref useImmersiveDarkMode,
                                         attrSize: sizeof(int)) ==
                   0;
        }

        return false;
    }

    private static bool IsWindows10OrGreater(int build = -1)
    {
        return Environment.OSVersion.Version.Major >= 10 &&
               Environment.OSVersion.Version.Build >= build;
    }

    /// <summary>
    ///     Sets the theme color of the application during startup (or alternatively called when a form opens.).
    /// </summary>
    /// <param name="themeColour">The theme color to be applied. Can be either Light or Dark.</param>
    /// <param name="parentControl">The parent control to which the theme color will be applied.</param>
    internal static void SetThemeColour(ThemeColour themeColour,
                                        Control parentControl)
    {
        // changed the logic here. basically if it's light mode then don't poke it because it just gets uglier even in that case.
        // for Dark we set it to the job, for Light, ignore.
        if (themeColour == Dark)
        {
            UseImmersiveDarkMode(handle: parentControl.Handle, enabled: true);

            HelperNonStatic helperNonstatic = new();
            if (parentControl is Form)
            {
                ChangeTheme(themeColour: themeColour, cItem: parentControl);
            }

            IEnumerable<Control> c =
                helperNonstatic.GetAllControls(control: parentControl);

            foreach (Control cItem in c)
            {
                ChangeTheme(themeColour: themeColour, cItem: cItem);
            }
        }
    }

    /// <summary>
    ///     Creates a new StringFormat object with specific alignment settings.
    /// </summary>
    /// <returns>
    ///     A StringFormat object with Near alignment for horizontal layout (left-aligned for left-to-right text)
    ///     and Center alignment for vertical layout (centered vertically).
    /// </returns>
    private static StringFormat GetStringFormat()
    {
        return new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
    }

    /// <summary>
    ///     Handles the DrawItem event of a TabControl.
    /// </summary>
    /// <param name="sender">The source of the event, expected to be a TabControl.</param>
    /// <param name="e">A DrawItemEventArgs that contains the event data.</param>
    /// <remarks>
    ///     This method is responsible for custom drawing of the TabControl's tabs. It adjusts the foreground and background
    ///     colors based on the user's theme settings.
    ///     It iterates through all the TabPages in the TabControl, drawing each tab and its text with the appropriate colors.
    ///     via https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.tabcontrol.drawitem?view=netframework-4.8.1
    /// </remarks>
    internal static void TabControl_DrawItem(object sender,
                                             DrawItemEventArgs e)
    {
        Color foreColor = HelperVariables.UserSettingUseDarkMode
            ? Color.White
            : Color.Black;

        Color backColor = HelperVariables.UserSettingUseDarkMode
            ? darkColor
            : SystemColors.Control;

        Graphics graphics = e.Graphics;
        Pen backColorPen = new(color: backColor);
        SolidBrush foreColorBrush = new(color: foreColor);
        SolidBrush backColorBrush = new(color: backColor);
        TabControl tctControl = sender as TabControl;
        StringFormat stringFormat = GetStringFormat();

        for (int i = 0; i < tctControl.TabPages.Count; i++)
        {
            TabPage tbpTabPage = tctControl.TabPages[index: i];
            Rectangle tabArea = tctControl.GetTabRect(index: i);
            RectangleF tabTextArea = tabArea;
            tabTextArea.X += 2; // add padding 

            graphics.DrawRectangle(pen: backColorPen, rect: tabArea);
            graphics.FillRectangle(brush: backColorBrush, rect: tabTextArea);
            graphics.DrawString(s: tbpTabPage.Text, font: e.Font, brush: foreColorBrush,
                                layoutRectangle: tabTextArea, format: stringFormat);
        }
    }

    /// <summary>
    ///     Changes the theme of a specific control based on the provided theme color.
    /// </summary>
    /// <param name="themeColour">The theme color to be applied. Can be either Light or Dark.</param>
    /// <param name="cItem">The control to which the theme color will be applied.</param>
    private static void ChangeTheme(ThemeColour themeColour,
                                    Control cItem)
    {
        if (themeColour == Dark)
        {
            cItem.BackColor = darkColor;
            cItem.ForeColor = Color.White;

            if (cItem is Button button)
            {
                button.BackColor = lessDarkColor;
                button.ForeColor = Color.White;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor =
                    ColorTranslator.FromHtml(htmlColor: "#BAB9B9");
            }
            else if (cItem is CheckBox checkBox)
            {
                checkBox.BackColor = darkColor;
                checkBox.ForeColor = Color.White;
                checkBox.FlatStyle = FlatStyle.Flat;
            }
            else if (cItem is TextBox textBox)
            {
                textBox.BackColor = darkColor;
                textBox.ForeColor = Color.White;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (cItem is Label label)
            {
                label.BackColor = darkColor;
                label.ForeColor = Color.White;
            }
            else if (cItem is ListView listView)
            {
                listView.BackColor = lessDarkColor;
                listView.ForeColor = Color.White;
                listView.BorderStyle = BorderStyle.None;
            }
            else if (cItem is TabControl tcr)
            {
                tcr.DrawItem += TabControl_DrawItem;
            }
            else if (cItem is TabPage tp)
            {
                tp.BackColor = darkColor;
            }
            else if (cItem is LinkLabel linkLabel)
            {
                linkLabel.LinkColor = Color.White;
            }
            else if (cItem is DataGridView dgv)
            {
                dgv.BackgroundColor = darkColor;
                dgv.ForeColor = Color.White;
                dgv.DefaultCellStyle.BackColor = darkColor;
                dgv.DefaultCellStyle.ForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = darkColor;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv.EnableHeadersVisualStyles = false;
                foreach (DataGridViewColumn column in dgv.Columns)
                {
                    if (column is DataGridViewComboBoxColumn comboBoxColumn)
                    {
                        comboBoxColumn.DisplayStyleForCurrentCellOnly = true;
                        comboBoxColumn.DefaultCellStyle.BackColor = darkColor;
                        comboBoxColumn.DefaultCellStyle.ForeColor = Color.White;
                    }
                }
            }
            else
            {
                cItem.BackColor = darkColor;
                cItem.ForeColor = Color.White;
            }
        }
    }
}