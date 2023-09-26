using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GeoTagNinja.Helpers;

namespace GeoTagNinja.View.DialogAndMessageBoxes;

internal class DialogWithoutCheckBox
{
    /// <summary>
    ///     Displays a dialog box without a checkbox and returns a list of user choices.
    /// </summary>
    /// <param name="labelText">The text to be displayed in the dialog box.</param>
    /// <param name="caption">The title of the dialog box.</param>
    /// <param name="buttonsDictionary">
    ///     A dictionary containing the text and return values of the buttons to be displayed in
    ///     the dialog box.
    /// </param>
    /// <param name="orientation">The orientation of the buttons in the dialog box. Can be either "Vertical" or "Horizontal".</param>
    /// <returns>
    ///     A list of strings representing the user's choices. The choices are determined by the buttons clicked by the
    ///     user.
    /// </returns>
    /// <remarks>
    ///     The dialog box is displayed in the center of the screen. The theme of the dialog box is determined by the user's
    ///     settings.
    ///     If the user closes the dialog box without making a choice, the function returns a list containing the return value
    ///     of the "no" or "cancel" button.
    /// </remarks>
    internal static List<string> DisplayAndReturnList(string labelText,
                                                      string caption,
                                                      Dictionary<string, string>
                                                          buttonsDictionary,
                                                      string orientation)
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

        AddButtons();
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
                Button btn = new();
                btn.Text = keyValuePair.Key;
                btn.Tag = keyValuePair.Value;
                switch (keyValuePair.Value.ToLower())
                {
                    case "yes":
                    case "ok":
                        promptBoxForm.AcceptButton = btn;
                        acceptButtonReturnText = keyValuePair.Value;
                        break;
                    case "no":
                    case "cancel":
                        promptBoxForm.CancelButton = btn;
                        cancelButtonReturnText = keyValuePair.Value;
                        break;
                }

                btn.Click += (s,
                              e) =>
                {
                    if (promptBoxForm.AcceptButton == btn)
                    {
                        returnChoicesList.Add(item: acceptButtonReturnText);
                    }
                    else if (promptBoxForm.CancelButton == btn)
                    {
                        returnChoicesList.Add(item: cancelButtonReturnText);
                    }
                    else
                    {
                        returnChoicesList.Add(item: btn.Tag.ToString());
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
    }
}


