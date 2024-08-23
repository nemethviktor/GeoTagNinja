using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GeoTagNinja.Helpers;

namespace GeoTagNinja.View.DialogAndMessageBoxes;

internal class DialogWithOrWithoutCheckBox
{
    /// <summary>
    ///     Displays a dialog box with a set of checkboxes and buttons.
    /// </summary>
    /// <param name="labelText">The text to be displayed at the top of the dialog box.</param>
    /// <param name="caption">The text to be displayed in the title bar of the dialog box.</param>
    /// <param name="buttonsDictionary">
    ///     A dictionary where each key-value pair represents a button in the dialog. The key is
    ///     the text to be displayed on the button, and the value is the return value when the button is clicked.
    /// </param>
    /// <param name="orientation">
    ///     The layout orientation of the checkboxes and buttons in the dialog. Can be either "Vertical"
    ///     or "Horizontal".
    /// </param>
    /// <param name="checkboxesDictionary">
    ///     A dictionary where each key-value pair represents a checkbox in the dialog. The key
    ///     is the text to be displayed next to the checkbox, and the value is the return value when the checkbox is selected.
    /// </param>
    /// <returns>A list of strings representing the return values of the selected checkboxes and clicked button.</returns>
    internal static List<string> DisplayAndReturnList(string labelText,
                                                      string caption,
                                                      Dictionary<string, string>
                                                          buttonsDictionary,
                                                      string orientation,
                                                      Dictionary<string, string>
                                                          checkboxesDictionary)
    {
        List<string> returnChoicesList = new();
        Form promptBoxForm = new()
        {
            Text = caption,
            ControlBox = false,
            FormBorderStyle = FormBorderStyle.Fixed3D
        };

        // Create the FlowLayoutPanel
        FlowLayoutPanel flowLayoutPanel = new();
        // Set the flow direction to top-to-bottom if the orientation is "Vertical"
        if (orientation == "Vertical")
        {
            flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        }

        // apply theme
        HelperControlThemeManager.SetThemeColour(
            themeColour: HelperVariables.UserSettingUseDarkMode
                ? ThemeColour.Dark
                : ThemeColour.Light, parentControl: promptBoxForm);

        Label lblText = new();
        lblText.Text = labelText;
        lblText.MaximumSize = new Size(width: 300, height: 0);
        lblText.AutoSize = true;
        flowLayoutPanel.SetFlowBreak(control: lblText, value: true);
        flowLayoutPanel.Controls.Add(value: lblText);

        // these are to make my life easier -- gets overwritten further down
        string acceptButtonReturnText = "##yes##";
        string cancelButtonReturnText = "##no##";

        if (orientation == "Horizontal")
        {
            AddButtons();
            AddCheckBoxes();
        }
        else
        {
            AddCheckBoxes();
            AddButtons();
        }

        flowLayoutPanel.Padding = new Padding(all: 5);
        flowLayoutPanel.AutoSize = true;

        promptBoxForm.Controls.Add(value: flowLayoutPanel);
        promptBoxForm.AutoSize = true;
        promptBoxForm.ShowInTaskbar = false;

        promptBoxForm.StartPosition = FormStartPosition.CenterScreen;
        promptBoxForm.ShowDialog();

        // in case of idiots break glass -- basically if someone ALT+F4s then we reset stuff to "no".
        if (!returnChoicesList.Contains(value: acceptButtonReturnText) &&
            !returnChoicesList.Contains(value: cancelButtonReturnText))
        {
            returnChoicesList.Add(item: cancelButtonReturnText);
        }

        return returnChoicesList;

        void AddButtons()
        {
            // add buttons
            for (int i = 0; i < buttonsDictionary.Count; i++)
            {
                KeyValuePair<string, string> keyValuePair =
                    buttonsDictionary.ElementAt(index: i);
                string btnText = keyValuePair.Key;
                Button btn = new()
                {
                    UseMnemonic = true,
                    Text = btnText is "Yes" or "No"
                        ? "&" + btnText
                        : btnText,
                    Name = keyValuePair.Value
                };
                if (btnText.ToLower()
                           .EndsWith(value: "yes") ||
                    btnText.ToLower()
                           .EndsWith(value: "ok"))
                {
                    promptBoxForm.AcceptButton = btn;
                    acceptButtonReturnText = keyValuePair.Value;
                }
                else if (btnText.ToLower()
                                .EndsWith(value: "no") ||
                         btnText.ToLower()
                                .EndsWith(value: "cancel"))
                {
                    promptBoxForm.CancelButton = btn;
                    cancelButtonReturnText = keyValuePair.Value;
                }

                btn.Click += (s,
                              e) =>
                {
                    returnChoicesList = ProcessCheckBoxes();
                    returnChoicesList.Add(item: btn.Name);
                    if (promptBoxForm.AcceptButton == btn)
                    {
                        returnChoicesList.Add(item: acceptButtonReturnText);
                    }
                    else if (promptBoxForm.CancelButton == btn)
                    {
                        returnChoicesList.Add(item: cancelButtonReturnText);
                    }

                    promptBoxForm.Close();
                };
                btn.AutoSize = true;
                flowLayoutPanel.Controls.Add(value: btn);
                // add flowbreak to the last btn
                if (i == buttonsDictionary.Count &&
                    orientation == "Horizontal")
                {
                    flowLayoutPanel.SetFlowBreak(control: btn, value: true);
                }
            }
        }

        void AddCheckBoxes()
        {
            if (checkboxesDictionary.Count > 0)
            {
                // add checkboxes 
                for (int i = 0; i < checkboxesDictionary.Count; i++)
                {
                    KeyValuePair<string, string> keyValuePair =
                        checkboxesDictionary.ElementAt(index: i);
                    CheckBox chk = new();
                    chk.Text = keyValuePair.Key;
                    chk.AutoSize = true;

                    flowLayoutPanel.Controls.Add(value: chk);

                    if (i == checkboxesDictionary.Count &&
                        orientation == "Vertical")
                    {
                        flowLayoutPanel.SetFlowBreak(control: chk, value: true);
                    }
                }
            }
        }

        List<string> ProcessCheckBoxes()
        {
            List<string> s = new();
            // see if user added any checkbox choices
            foreach (CheckBox chk in flowLayoutPanel.Controls.OfType<CheckBox>())
            {
                if (chk.Checked)
                {
                    checkboxesDictionary.TryGetValue(key: chk.Text,
                                                     value: out string chkstr);
                    s.Add(item: chkstr);
                }
            }

            return s;
        }
    }
}


