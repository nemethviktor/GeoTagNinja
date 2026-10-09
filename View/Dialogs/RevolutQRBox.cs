using GeoTagNinja.Helpers.UI;
using System;
using System.Windows.Forms;

namespace GeoTagNinja.View.Dialogs
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
            ThemeHelper.ApplyTo(control: this);
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
