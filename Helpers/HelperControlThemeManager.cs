﻿using System;
using System.Collections.Generic;
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

internal static class HelperControlThemeManager
{
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
    internal static bool UseImmersiveDarkMode(IntPtr handle,
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
            return DwmSetWindowAttribute(hwnd: handle, attr: attribute, attrValue: ref useImmersiveDarkMode, attrSize: sizeof(int)) == 0;
        }

        return false;
    }

    private static bool IsWindows10OrGreater(int build = -1)
    {
        return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
    }

    /// <summary>
    ///     Sets the theme color of the application during startup (or alternatively called when a form opens.).
    /// </summary>
    /// <param name="themeColour">The theme color to be applied. Can be either Light or Dark.</param>
    /// <param name="parentControl">The parent control to which the theme color will be applied.</param>
    internal static void SetThemeColour(ThemeColour themeColour,
                                        Control parentControl)
    {
        if (themeColour == Dark)
        {
            UseImmersiveDarkMode(handle: parentControl.Handle, enabled: true);
        }

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: parentControl);

        foreach (Control cItem in c)
        {
            ChangeTheme(themeColour: themeColour, cItem: cItem);
        }
    }

    /// <summary>
    ///     Changes the theme of a specific control based on the provided theme color.
    /// </summary>
    /// <param name="themeColour">The theme color to be applied. Can be either Light or Dark.</param>
    /// <param name="cItem">The control to which the theme color will be applied.</param>
    internal static void ChangeTheme(ThemeColour themeColour,
                                     Control cItem)
    {
        if (themeColour == Dark)
        {
            cItem.ForeColor = Color.Black;
            cItem.BackColor = SystemColors.ControlDark;
        }
        else
        {
            cItem.ForeColor = Color.Black;
            cItem.BackColor = SystemColors.Control;
        }
    }
}