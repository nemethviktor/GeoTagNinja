using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using GeoTagNinja.Helpers;

namespace GeoTagNinja;

public partial class FrmPleaseWaitBox : Form
{
    private FrmMainApp frmMainAppInstance =
        (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

    public FrmPleaseWaitBox()
    {
        InitializeComponent();
        Debug.Assert(condition: frmMainAppInstance != null, message: nameof(frmMainAppInstance) + " != null");
        lbl_CancelPressed.Visible = false;
        HelperControlThemeManager.SetThemeColour(themeColour: HelperVariables.UserSettingUseDarkMode
            ? ThemeColour.Dark
            : ThemeColour.Light, parentControl: this);
    }

    private void btn_Cancel_Click(object sender, EventArgs e)
    {
        // Check if `_cts` is already disposed or null
        if (frmMainAppInstance._cts == null ||
            frmMainAppInstance._cts.Token.IsCancellationRequested)
        {
            Console.WriteLine(value: "Cancellation already requested or `_cts` disposed.");
            return;
        }

        // Request cancellation
        frmMainAppInstance._cts.Cancel();
        Enabled = false;
        lbl_CancelPressed.Visible = true;
    }

    private void FrmPleaseWaitBox_Load(object sender, EventArgs e)
    {
        HelperControlAndMessageBoxHandling.ReturnControlText(cItem: this, senderForm: this);
        frmMainAppInstance.Enabled = false;
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (
                cItem is Button ||
                cItem is CheckBox ||
                cItem is GroupBox ||
                cItem is Label ||
                cItem is RadioButton ||
                cItem is TabPage
            )
            {
                // gets logged inside.
                HelperControlAndMessageBoxHandling.FakeControlTypes fakeControlType = cItem switch
                {
                    Button => HelperControlAndMessageBoxHandling.FakeControlTypes.Button,
                    CheckBox => HelperControlAndMessageBoxHandling.FakeControlTypes.CheckBox,
                    GroupBox => HelperControlAndMessageBoxHandling.FakeControlTypes.GroupBox,
                    Label => HelperControlAndMessageBoxHandling.FakeControlTypes.Label,
                    RadioButton => HelperControlAndMessageBoxHandling.FakeControlTypes.RadioButton,
                    TabPage => HelperControlAndMessageBoxHandling.FakeControlTypes.TabPage,
                    _ => HelperControlAndMessageBoxHandling.FakeControlTypes.Undefined
                };

                cItem.Text = HelperControlAndMessageBoxHandling.ReturnControlText(controlName: cItem.Name,
                    fakeControlType: fakeControlType);
            }
        }

        lbl_PleaseWaitBoxActionParsing.Visible = false;
        lbl_PleaseWaitBoxActionScanning.Visible = false;
    }

    private void FrmPleaseWaitBox_FormClosing(object sender, FormClosingEventArgs e)
    {
        frmMainAppInstance.Enabled = true;
    }
}