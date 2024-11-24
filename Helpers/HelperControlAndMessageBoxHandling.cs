using System;
using System.Globalization;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

namespace GeoTagNinja.Helpers;

internal static class HelperControlAndMessageBoxHandling
{
    public enum MessageBoxCaption
    {
        None,
        Information,
        Error,
        Exclamation,
        Question,
        Warning,
        Stop,
        Hand,
        Asterisk
    }

    /// <summary>
    ///     This is probably the worst idea ever but I'm creating fake control types for allocating them to the text-reader
    ///     e.g. ColumnHeader isn't a real control type but I use them.
    /// </summary>
    public enum FakeControlTypes
    {
        Button,
        CheckBox,
        ColumnHeader,
        Form,
        GroupBox,
        MessageBox,
        MessageBoxCaption,
        Generic,
        Label,
        PictureBox,
        RadioButton,
        TabPage,
        ToolStripButton,
        ToolStripMenuItem,
        ToolTip,
        Undefined
    }

    /// <summary>
    ///     This (mostly) sets the various texts for most Controls in various forms, especially labels and buttons/boxes.
    /// </summary>
    /// <param name="cItem">The Control whose details need adjusting</param>
    /// <param name="senderForm">Name of the Form on which the Control appears. Only relevant for NUDs on the Settings Form</param>
    /// <param name="parentNameToUse">Obsolete most likely at this stage.</param>
    internal static void ReturnControlText(Control cItem,
        Form senderForm = null,
        string parentNameToUse = null)
    {
        if (parentNameToUse == null &&
            cItem is not Form)
        {
            parentNameToUse = cItem.Parent.Name;
        }


        FrmMainApp frmMainAppInstance =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];


        // Note to self: do not add TextBoxes and ComboBoxes.
        // Also cannot add ColumnHeader because it's not a Control
        if (
            cItem is Label ||
            cItem is GroupBox ||
            cItem is Button ||
            cItem is CheckBox ||
            cItem is TabPage ||
            cItem is RichTextBox ||
            cItem is RadioButton ||
            cItem is Form
            //||
        )
        {
            FrmMainApp.Log.Trace(message: $"Starting - cItem: {cItem.Name}");
            string location = HelperGenericAncillaryListsArrays.IsGenericControlName(controlName: cItem.Name) ||
                              cItem.Name.Contains(value: "Generic")
                ? HelperVariables.ResourceNameForGenericControlItems
                : HelperVariables.ControlItemNameNotGeneric;

            location = location switch
            {
                HelperVariables.ControlItemNameNotGeneric => cItem switch
                {
                    Label => "Label",
                    GroupBox => "GroupBox",
                    Button => "Button",
                    CheckBox => "CheckBox",
                    TabPage => "TabPage",
                    RichTextBox => "RichTextBox",
                    RadioButton => "RadioButton",
                    Form => "Form",
                    _ => null
                },
                _ => location
            };

            cItem.Text = HelperLocalisationResourceManager.GetResourceValue(control: cItem, location: location);
        }

        else if (cItem is NumericUpDown nud)
        {
            if (senderForm != null &&
                senderForm.Name == "FrmSettings")
            {
                FrmMainApp.Log.Trace(message: $"Starting - cItem: {nud.Name}");
                _ = decimal.TryParse(
                    s: HelperDataApplicationSettings.DataReadSQLiteSettings(
                        dataTable: HelperVariables.DtHelperDataApplicationSettings,
                        settingTabPage: parentNameToUse,
                        settingId: nud.Name
                    ), result: out decimal outVal);

                // if this doesn't exist, it'd return 0, which is illegal because the min-values can be higher than that
                nud.Value = Math.Max(val1: nud.Minimum, val2: outVal);
                nud.Text = outVal.ToString(provider: CultureInfo.InvariantCulture);
            }
        }
    }

    internal static string ReturnControlText(string controlName, FakeControlTypes fakeControlType)
    {
        FrmMainApp.Log.Trace(message: $"Starting - cItem: {controlName}");
        string location = HelperGenericAncillaryListsArrays.IsGenericControlName(controlName: controlName) ||
                          controlName.Contains(value: "Generic")
            ? HelperVariables.ResourceNameForGenericControlItems
            : HelperVariables.ControlItemNameNotGeneric;

        location = location switch
        {
            HelperVariables.ControlItemNameNotGeneric => fakeControlType switch
            {
                FakeControlTypes.Button => "Button",
                FakeControlTypes.CheckBox => "CheckBox",
                FakeControlTypes.ColumnHeader => "ColumnHeader",
                FakeControlTypes.Form => "Form",
                FakeControlTypes.Generic => "Generic",
                FakeControlTypes.GroupBox => "GroupBox",
                FakeControlTypes.Label => "Label",
                FakeControlTypes.MessageBox => "MessageBox",
                FakeControlTypes.MessageBoxCaption => "MessageBoxCaption",
                // FakeControlTypes.PictureBox => "PictureBox", // PictureBoxes shouldn't have text assigned to them.
                FakeControlTypes.RadioButton => "RadioButton",
                FakeControlTypes.TabPage => "TabPage",
                FakeControlTypes.ToolStripButton => "ToolStripButton",
                FakeControlTypes.ToolStripMenuItem => "ToolStripMenuItem",
                FakeControlTypes.ToolTip => "ToolTip",
                _ => null
            },
            _ => location
        };

        return HelperLocalisationResourceManager.GetResourceValue(controlName: controlName, location: location);
    }
}
