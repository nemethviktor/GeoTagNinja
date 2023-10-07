using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using GeoTagNinja.Helpers;

namespace GeoTagNinja;

internal partial class FrmAboutBox : Form
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FrmAboutBox" /> class.
    ///     This constructor sets up the initial state of the AboutBox form.
    ///     It initializes the components, sets up the theme color based on the user's settings,
    ///     and populates the form with assembly information and relevant links.
    /// </summary>
    public FrmAboutBox()
    {
        InitializeComponent();
        rtb_AboutBox.LinkClicked += rtb_AboutBox_LinkClicked;
        HelperControlThemeManager.SetThemeColour(themeColour: HelperVariables.UserSettingUseDarkMode
                                                     ? ThemeColour.Dark
                                                     : ThemeColour.Light, parentControl: this);
        HelperNonStatic helperNonstatic = new();
        HelperControlAndMessageBoxHandling.ReturnControlText(cItem: this, senderForm: this);

        // via https://stackoverflow.com/a/1601079/3968494
        Version version = Assembly.GetEntryAssembly()
                                  .GetName()
                                  .Version;
        DateTime buildDateTime = new DateTime(year: 2000, month: 1, day: 1).Add(value: new TimeSpan(
                                                                                    ticks: TimeSpan.TicksPerDay * version.Build + // days since 1 January 2000
                                                                                           TimeSpan.TicksPerSecond * 2 * version.Revision)); // seconds since midnight, (multiply by 2 to get original)

        Text = AssemblyTitle;

        tbx_Description.Text = AssemblyDescription;
        List<(string text, string link)> aboutBoxEntries = new()
        {
            (AssemblyTitle, null),
            ("Version/Build: " +
             Assembly.GetExecutingAssembly()
                     .GetName()
                     .Version.Build.ToString(provider: CultureInfo.InvariantCulture) +
             " [" +
             buildDateTime.ToString(format: "yyyyMMdd:HHmm") +
             "]", null),
            ("Rights: " + AssemblyCopyright, null),
            ("Written by: " + AssemblyCompany, null),
            ("Paypal: ", "https://paypal.me/NemethV"),
            ("GitHub: ", "https://github.com/nemethviktor/GeoTagNinja")
        };
        foreach ((string text, string link) in aboutBoxEntries)
        {
            AppendText(box: rtb_AboutBox, text: text, link: link);
        }
    }

    private void AppendText(RichTextBox box,
                            string text,
                            string link = null)
    {
        box.AppendText(text: text + " " + link + Environment.NewLine);
    }


    public sealed override string Text
    {
        get => base.Text;
        set => base.Text = value;
    }

    private void Btn_OK_Click(object sender,
                              EventArgs e)
    {
        Hide();
    }

    private void rtb_AboutBox_LinkClicked(object sender,
                                          LinkClickedEventArgs e)
    {
        Process.Start(fileName: e.LinkText);
    }


#region Assembly Attribute Accessors

    /// <summary>
    ///     Gets the title of the assembly currently executing.
    ///     The title is retrieved from the AssemblyTitleAttribute of the assembly.
    ///     If the AssemblyTitleAttribute is not found or the title is empty,
    ///     it returns the file name of the assembly without the extension.
    /// </summary>
    private string AssemblyTitle
    {
        get
        {
            object[] attributes = Assembly.GetExecutingAssembly()
                                          .GetCustomAttributes(attributeType: typeof(AssemblyTitleAttribute), inherit: false);
            if (attributes.Length > 0)
            {
                AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (titleAttribute.Title != "")
                {
                    return titleAttribute.Title;
                }
            }

            return Path.GetFileNameWithoutExtension(path: Assembly.GetExecutingAssembly()
                                                                  .CodeBase);
        }
    }


    /// <summary>
    ///     Gets the assembly description of the executing assembly.
    /// </summary>
    /// <remarks>
    ///     The assembly description is retrieved from the AssemblyDescriptionAttribute of the executing assembly.
    ///     If the AssemblyDescriptionAttribute is not found, an empty string is returned.
    /// </remarks>
    private string AssemblyDescription
    {
        get
        {
            object[] attributes = Assembly.GetExecutingAssembly()
                                          .GetCustomAttributes(attributeType: typeof(AssemblyDescriptionAttribute), inherit: false);
            if (attributes.Length == 0)
            {
                return "";
            }

            return ((AssemblyDescriptionAttribute)attributes[0]).Description;
        }
    }

    /// <summary>
    ///     Gets the product name of the assembly.
    /// </summary>
    /// <returns>
    ///     The product name of the assembly if the AssemblyProductAttribute is found; otherwise, an empty string.
    /// </returns>
    private string AssemblyProduct
    {
        get
        {
            object[] attributes = Assembly.GetExecutingAssembly()
                                          .GetCustomAttributes(attributeType: typeof(AssemblyProductAttribute), inherit: false);
            if (attributes.Length == 0)
            {
                return "";
            }

            return ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    /// <summary>
    ///     Gets the copyright information from the assembly.
    /// </summary>
    /// <returns>
    ///     The copyright information if it exists; otherwise, an empty string.
    /// </returns>
    private string AssemblyCopyright
    {
        get
        {
            object[] attributes = Assembly.GetExecutingAssembly()
                                          .GetCustomAttributes(attributeType: typeof(AssemblyCopyrightAttribute), inherit: false);
            if (attributes.Length == 0)
            {
                return "";
            }

            return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }

    /// <summary>
    ///     Gets the company information from the assembly.
    /// </summary>
    /// <returns>
    ///     The company information as a string. If no company information is found in the assembly, an empty string is
    ///     returned.
    /// </returns>
    private string AssemblyCompany
    {
        get
        {
            object[] attributes = Assembly.GetExecutingAssembly()
                                          .GetCustomAttributes(attributeType: typeof(AssemblyCompanyAttribute), inherit: false);
            if (attributes.Length == 0)
            {
                return "";
            }

            return ((AssemblyCompanyAttribute)attributes[0]).Company;
        }
    }

#endregion
}