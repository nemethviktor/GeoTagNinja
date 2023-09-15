﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using GeoTagNinja.Helpers;

namespace GeoTagNinja;

/// <summary>
///     This is generated by a template so I'm not adding extra commentary. Atm it's pretty basic anyway
///     ... it just pulls some of the Assembly info. If someone is genuienly courious, open an issue.
/// </summary>
internal partial class FrmAboutBox : Form
{
    public FrmAboutBox()
    {
        InitializeComponent();
        HelperControlThemeManager.SetThemeColour(themeColour: HelperVariables.SUseDarkMode
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
        tbx_ProductName.Text = AssemblyProduct;
        tbx_Version.Text = "Version/Build: " +
                           Assembly.GetExecutingAssembly()
                               .GetName()
                               .Version.Build.ToString(provider: CultureInfo.InvariantCulture) +
                           " [" +
                           buildDateTime.ToString(format: "yyyyMMdd:HHmm") +
                           "]";

        tbx_Copyright.Text = "Rights: " + AssemblyCopyright;

        tbx_CompanyName.Text = "Written by: " + AssemblyCompany;

        tbx_Description.Text = AssemblyDescription;

        tbx_Paypal.Links.Clear();
        tbx_Paypal.Links.Add(start: 0, length: tbx_Paypal.Width, linkData: "https://www.paypal.com/donate/?hosted_button_id=R5GSBXW8A5NNN");

        tbx_Website.Text = "Web: https://github.com/nemethviktor/GeoTagNinja";
        tbx_Website.Links.Add(start: 0, length: tbx_Website.Width, linkData: "https://github.com/nemethviktor/GeoTagNinja");
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

    private void tbx_Paypal_Click(object sender,
                                  EventArgs e)
    {
        Process.Start(fileName: "https://www.paypal.com/donate/?hosted_button_id=R5GSBXW8A5NNN");
    }

    private void tbx_Website_LinkClicked(object sender,
                                         LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(fileName: e.Link.LinkData as string);
    }

    private void FrmAboutBox_Load(object sender,
                                  EventArgs e)
    {
        LinkLabel.Link link = new();
        link.LinkData = "https://geotag.ninja";
        tbx_Website.Links.Add(value: link);
    }

    #region Assembly Attribute Accessors

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

    public string AssemblyVersion => Assembly.GetExecutingAssembly()
        .GetName()
        .Version.ToString();

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