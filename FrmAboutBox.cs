using GeoTagNinja.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Themer = WinFormsDarkThemerNinja.Themer;

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
        rtb_AboutBox.Text = string.Empty;

        rtb_AboutBox.LinkClicked += rtb_AboutBox_LinkClicked;

        HelperNonStatic helperNonstatic = new();
        HelperControlAndMessageBoxHandling.ReturnControlText(control: this, senderForm: this);

        // via https://stackoverflow.com/a/1601079/3968494
        Version version = Assembly.GetEntryAssembly()
                                  .GetName()
                                  .Version;
        DateTime buildDateTime = new DateTime(year: 2000, month: 1, day: 1).Add(value: new TimeSpan(
            ticks: (TimeSpan.TicksPerDay * version.Build) + // days since 1 January 2000
                   (TimeSpan.TicksPerSecond * 2 *
                   version.Revision))); // seconds since midnight, (multiply by 2 to get original)

        Text = AssemblyTitle;

        tbx_Description.Text = AssemblyDescription;
        List<(string text, string link)> aboutBoxEntries =
        [
            (text: AssemblyTitle, link: null),
            (text: $"Version/Build: {Assembly.GetExecutingAssembly()
                                             .GetName()
                                             .Version.Build.ToString(provider: CultureInfo.InvariantCulture)} [{buildDateTime:yyyyMMdd:HHmm}]",
             link: null),

            (text: $"Rights: {AssemblyCopyright}", link: null),
            (text: $"Written by: {AssemblyCompany}", link: null),
            (text: "Paypal: ", link: "https://paypal.me/NemethV"),
            (text: "GitHub: ", link: "https://github.com/nemethviktor/GeoTagNinja"),
            (text: $"ExifTool Ver: {HelperVariables.CurrentExifToolVersionLocal}", link: null)
        ];
        foreach ((string text, string link) in aboutBoxEntries)
        {
            AppendText(box: rtb_AboutBox, text: text, link: link);
        }
    }

    public sealed override string Text
    {
        get => base.Text;
        set => base.Text = value;
    }

    private static void AppendText(RichTextBox box,
        string text,
        string link = null)
    {
        box.AppendText(text: $"{text} {link}{Environment.NewLine}");
    }

    private void btn_Generic_OK_Click(object sender,
        EventArgs e)
    {
        Hide();
    }

    private void rtb_AboutBox_LinkClicked(object sender,
        LinkClickedEventArgs e)
    {
        _ = Process.Start(fileName: e.LinkText);
    }

    #region Assembly Attribute Accessors

    /// <summary>
    ///     Gets the title of the assembly currently executing.
    ///     The title is retrieved from the AssemblyTitleAttribute of the assembly.
    ///     If the AssemblyTitleAttribute is not found or the title is empty,
    ///     it returns the file name of the assembly without the extension.
    /// </summary>
    private static string AssemblyTitle
    {
        get
        {
            object[] attributes = Assembly.GetExecutingAssembly()
                                          .GetCustomAttributes(attributeType: typeof(AssemblyTitleAttribute),
                                               inherit: false);
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
    private static string AssemblyDescription
    {
        get
        {
            object[] attributes = Assembly.GetExecutingAssembly()
                                          .GetCustomAttributes(attributeType: typeof(AssemblyDescriptionAttribute),
                                               inherit: false);
            return attributes.Length == 0 ? "" : ((AssemblyDescriptionAttribute)attributes[0]).Description;
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
                                          .GetCustomAttributes(attributeType: typeof(AssemblyProductAttribute),
                                               inherit: false);
            return attributes.Length == 0 ? "" : ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    /// <summary>
    ///     Gets the copyright information from the assembly.
    /// </summary>
    /// <returns>
    ///     The copyright information if it exists; otherwise, an empty string.
    /// </returns>
    private static string AssemblyCopyright
    {
        get
        {
            object[] attributes = Assembly.GetExecutingAssembly()
                                          .GetCustomAttributes(attributeType: typeof(AssemblyCopyrightAttribute),
                                               inherit: false);
            return attributes.Length == 0 ? "" : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }

    /// <summary>
    ///     Gets the company information from the assembly.
    /// </summary>
    /// <returns>
    ///     The company information as a string. If no company information is found in the assembly, an empty string is
    ///     returned.
    /// </returns>
    private static string AssemblyCompany
    {
        get
        {
            object[] attributes = Assembly.GetExecutingAssembly()
                                          .GetCustomAttributes(attributeType: typeof(AssemblyCompanyAttribute),
                                               inherit: false);
            return attributes.Length == 0 ? "" : ((AssemblyCompanyAttribute)attributes[0]).Company;
        }
    }

    #endregion

    private void FrmAboutBox_Load(object sender, EventArgs e)
    {

        Themer.ApplyThemeToControl(
            control: this,
            themeStyle: HelperVariables.UserSettingUseDarkMode ?
            Themer.ThemeStyle.Custom :
            Themer.ThemeStyle.Default
            );
    }
}