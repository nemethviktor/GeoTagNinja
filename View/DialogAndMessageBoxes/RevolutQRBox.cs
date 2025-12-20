using GeoTagNinja.Helpers;
using System;
using System.Windows.Forms;
using WinFormsDarkThemerNinja;

namespace GeoTagNinja.View.DialogAndMessageBoxes
{
    /// <summary>
    /// Represents a Windows Form that displays a QR code for Revolut payment processing.
    /// </summary>
    /// <remarks>This form applies a theme based on the user's dark mode setting when loaded. It is intended
    /// to be used as part of a payment workflow where users can scan a QR code to complete a transaction.</remarks>
    public partial class RevolutQRBox : Form
    {
        public RevolutQRBox()
        {
            InitializeComponent();
        }

        private void RevolutQRBox_Load(object sender, EventArgs e)
        {
            Themer.ApplyThemeToControl(
                control: this,
                themeStyle: HelperVariables.UserSettingUseDarkMode ?
                Themer.ThemeStyle.Custom :
                Themer.ThemeStyle.Default
                );
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
