using GeoTagNinja.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using WinFormsDarkThemerNinja;

namespace GeoTagNinja;

public partial class FrmPleaseWaitBox : Form
{
    private FrmMainApp _frmMainAppInstance =
        (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

    public FrmPleaseWaitBox()
    {
        InitializeComponent();
        Debug.Assert(condition: _frmMainAppInstance != null, message: $"{nameof(_frmMainAppInstance)} != null");
        lbl_CancelPressed.Visible = false;

        lbl_PleaseWaitBoxActionScanning.Visible = false;
        lbl_PleaseWaitBoxActionParsing.Visible = false;
        lbl_PleaseWaitBoxActionPopulatingListView.Visible = false;
    }

    private void btn_Cancel_Click(object sender, EventArgs e)
    {
        // Check if `_cts` is already disposed or null
        if (_frmMainAppInstance._cts == null ||
            _frmMainAppInstance._cts.Token.IsCancellationRequested)
        {
            Console.WriteLine(value: "Cancellation already requested or `_cts` disposed.");
            return;
        }

        // Request cancellation
        _frmMainAppInstance._cts.Cancel();
        Enabled = false;
        lbl_CancelPressed.Visible = true;
    }

    private void FrmPleaseWaitBox_Load(object sender, EventArgs e)
    {
        HelperControlAndMessageBoxHandling.ReturnControlText(control: this, senderForm: this);
        _frmMainAppInstance.Enabled = false;
        GetControlNames();

        Themer.ApplyThemeToControl(
            control: this,
            themeStyle: HelperVariables.UserSettingUseDarkMode ?
            Themer.ThemeStyle.Custom :
            Themer.ThemeStyle.Default
            );
    }

    private void GetControlNames()
    {
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> controls = helperNonstatic.GetAllControls(control: this);
        foreach (Control control in controls)
        {
            if (
                control is Button or
                CheckBox or
                GroupBox or
                Label or
                RadioButton or
                TabPage
            )
            {
                // gets logged inside.
                HelperControlAndMessageBoxHandling.FakeControlTypes fakeControlType = control switch
                {
                    Button => HelperControlAndMessageBoxHandling.FakeControlTypes.Button,
                    CheckBox => HelperControlAndMessageBoxHandling.FakeControlTypes.CheckBox,
                    GroupBox => HelperControlAndMessageBoxHandling.FakeControlTypes.GroupBox,
                    Label => HelperControlAndMessageBoxHandling.FakeControlTypes.Label,
                    RadioButton => HelperControlAndMessageBoxHandling.FakeControlTypes.RadioButton,
                    TabPage => HelperControlAndMessageBoxHandling.FakeControlTypes.TabPage,
                    _ => HelperControlAndMessageBoxHandling.FakeControlTypes.Undefined
                };

                control.Text = HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: control.Name,
                    fakeControlType: fakeControlType);
            }
        }
    }

    private void FrmPleaseWaitBox_FormClosing(object sender, FormClosingEventArgs e)
    {
        _frmMainAppInstance.Enabled = true;
    }

    internal void UpdateLabels(ActionStages stage)
    {
        if (stage == ActionStages.SCANNING)
        {
            lbl_ParsingFolders.Visible = false;
            lbl_CancelPressed.Visible = false;
            lbl_PleaseWaitBoxMessage.Visible = false;
            lbl_PressCancelToStop.Visible = true;
            btn_Cancel.Visible = true;
            lbl_PleaseWaitBoxActionScanning.Visible = true;
            lbl_PleaseWaitBoxActionParsing.Visible = false;
            lbl_PleaseWaitBoxActionPopulatingListView.Visible = false;
        }
        else if (stage == ActionStages.PARSING)
        {
            lbl_ParsingFolders.Visible = true;
            lbl_CancelPressed.Visible = false;
            lbl_PleaseWaitBoxMessage.Visible = true;
            lbl_PressCancelToStop.Visible = false;
            btn_Cancel.Visible = true;
            lbl_PleaseWaitBoxActionScanning.Visible = false;
            lbl_PleaseWaitBoxActionParsing.Visible = true;
            lbl_PleaseWaitBoxActionPopulatingListView.Visible = false;
        }
        else if (stage == ActionStages.POPULATING_LISTVIEW)
        {
            lbl_ParsingFolders.Visible = false;
            lbl_CancelPressed.Visible = false;
            lbl_PleaseWaitBoxMessage.Visible = true;
            lbl_PressCancelToStop.Visible = false;
            btn_Cancel.Visible = false;
            lbl_PleaseWaitBoxActionScanning.Visible = false;
            lbl_PleaseWaitBoxActionParsing.Visible = false;
            lbl_PleaseWaitBoxActionPopulatingListView.Visible = true;
        }

        Application.DoEvents();
    }

    internal enum ActionStages
    {
        SCANNING,
        PARSING,
        POPULATING_LISTVIEW
    }
}