using System;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers;

internal static class HelperControlAndMessageBoxHandling
{
    /// <summary>
    ///     Bit of out sync with the rest but this returns the localised captions for messageboxes (e.g. "info" or "error")
    /// </summary>
    /// <param name="captionType">E.g. "info", "error"....</param>
    /// <returns>Localised version of the above.</returns>
    internal static string GenericGetMessageBoxCaption(string captionType)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        return HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: "messageBoxCaption",
            objectName: captionType
        );
    }

    /// <summary>
    ///     This is a special member of the objectMapping/Language and really should sit there not here.
    ///     Messageboxes are a bit more complicate to work with than "simple" objects and this takes their language-value and
    ///     returns it efficiently
    /// </summary>
    /// <param name="messageBoxName">A pseudonym for the messagebox whose value is requested.</param>
    /// <returns>Messagebox text contents</returns>
    internal static string GenericGetMessageBoxText(string messageBoxName)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        return HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: "messageBox",
            objectName: messageBoxName
        );
    }

    /// <summary>
    ///     This (mostly) sets the various texts for most Controls in various forms, especially labels and buttons/boxes.
    /// </summary>
    /// <param name="cItem">The Control whose details need adjusting</param>
    /// <param name="senderForm"></param>
    /// <param name="parentNameToUse"></param>
    internal static void ReturnControlText(Control cItem,
                                           Form senderForm,
                                           string parentNameToUse = null)
    {
        if (parentNameToUse == null && !(cItem is Form))
        {
            parentNameToUse = cItem.Parent.Name;
        }

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        if (
            cItem is Label ||
            cItem is GroupBox ||
            cItem is Button ||
            cItem is CheckBox ||
            cItem is TabPage ||
            cItem is RichTextBox ||
            cItem is RadioButton
            //||
        )
        {
            FrmMainApp.Logger.Trace(message: "Starting - cItem: " + cItem.Name);
            // for some reason there is no .Last() being offered here
            cItem.Text = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: cItem.GetType()
                    .ToString()
                    .Split('.')[cItem.GetType()
                                    .ToString()
                                    .Split('.')
                                    .Length -
                                1],
                objectName: cItem.Name
            );
        }
        else if (cItem is Form)
        {
            cItem.Text = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: "Form",
                objectName: cItem.Name);
        }
        else if (cItem is TextBox || cItem is ComboBox)
        {
            if (senderForm.Name == "FrmSettings")
            {
                cItem.Text = HelperDataApplicationSettings.DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: parentNameToUse,
                    settingId: cItem.Name
                );
            }
        }
        else if (cItem is NumericUpDown)
        {
            if (senderForm.Name == "FrmSettings")
            {
                NumericUpDown nud = (NumericUpDown)cItem;
                FrmMainApp.Logger.Trace(message: "Starting - cItem: " + nud.Name);
                _ = decimal.TryParse(s: HelperDataApplicationSettings.DataReadSQLiteSettings(
                                         tableName: "settings",
                                         settingTabPage: parentNameToUse,
                                         settingId: cItem.Name
                                     ), result: out decimal outVal);

                // if this doesn't exist, it'd return 0, which is illegal because the min-values can be higher than that
                nud.Value = Math.Max(val1: nud.Minimum, val2: outVal);
                nud.Text = outVal.ToString(provider: CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>
    ///     A custom dialogbox-like form that includes a checkbox too.
    ///     TODO: make it more reusable. Atm it's a bit fixed as there's only 1 place that calls it. Basically a "source"
    ///     parameter needs to be added in at some stage.
    /// </summary>
    /// <param name="labelText">String of the "main" message.</param>
    /// <param name="caption">Caption of the box - the one that appears on the top.</param>
    /// <param name="checkboxText">Text of the checkbox.</param>
    /// <param name="returnCheckboxText">A yes-no style logic that gets returned/amended to the return string if checked.</param>
    /// <param name="button1Text">Label of the button</param>
    /// <param name="returnButton1Text">String val of what's sent further if the btn is pressed</param>
    /// <param name="button2Text">Same as above</param>
    /// <param name="returnButton2Text">Same as above</param>
    /// <returns>A string that can be reused. Needs fine-tuning in the future as it's single-purpose atm. Lazy. </returns>
    internal static string ShowDialogWithCheckBox(string labelText,
                                                  string caption,
                                                  string checkboxText,
                                                  string returnCheckboxText,
                                                  string button1Text,
                                                  string returnButton1Text,
                                                  string button2Text,
                                                  string returnButton2Text)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        string returnString = "";
        Form promptBox = new();
        promptBox.Text = caption;
        promptBox.ControlBox = false;
        FlowLayoutPanel panel = new();

        Label lblText = new();
        lblText.Text = labelText;
        lblText.AutoSize = true;
        panel.SetFlowBreak(control: lblText, value: true);
        panel.Controls.Add(value: lblText);

        Button btnYes = new()
            { Text = button1Text };
        btnYes.Click += (sender,
                         e) =>
        {
            returnString = returnButton1Text;
            promptBox.Close();
        };
        btnYes.Location = new Point(x: 10, y: lblText.Bottom + 5);
        btnYes.AutoSize = true;

        panel.Controls.Add(value: btnYes);

        Button btnNo = new()
            { Text = button2Text };
        btnNo.Click += (sender,
                        e) =>
        {
            returnString = returnButton2Text;
            promptBox.Close();
        };

        btnNo.Location = new Point(x: btnYes.Width + 20, y: lblText.Bottom + 5);
        btnNo.AutoSize = true;
        panel.SetFlowBreak(control: btnNo, value: true);
        panel.Controls.Add(value: btnNo);

        CheckBox chk = new();
        chk.Text = checkboxText;
        chk.AutoSize = true;
        chk.Location = new Point(x: 10, y: btnYes.Bottom + 5);

        panel.Controls.Add(value: chk);
        panel.Padding = new Padding(all: 5);
        panel.AutoSize = true;

        promptBox.Controls.Add(value: panel);
        promptBox.Size = new Size(width: lblText.Width + 40, height: chk.Bottom + 50);
        promptBox.ShowInTaskbar = false;
        promptBox.AcceptButton = btnYes;
        promptBox.CancelButton = btnNo;
        promptBox.StartPosition = FormStartPosition.CenterScreen;
        promptBox.ShowDialog();

        if (chk.Checked)
        {
            returnString += returnCheckboxText;
        }

        // in case of idiots break glass -- basically if someone ALT+F4s then we reset stuff to "no".
        if (!returnString.Contains(value: returnButton1Text) && !returnString.Contains(value: returnButton2Text))
        {
            returnString = returnButton2Text;
        }

        return returnString;
    }
}