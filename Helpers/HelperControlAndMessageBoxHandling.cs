using System;
using System.Globalization;
using System.Windows.Forms;

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
    ///     Bit of out sync with the rest but this returns the localised captions for MessageBoxes (e.g. "info" or "error")
    /// </summary>
    /// <param name="captionType">E.g. "info", "error"....</param>
    /// <returns>Localised version of the above.</returns>
    internal static string GenericGetMessageBoxCaption(string captionType)
    {
        FrmMainApp frmMainAppInstance =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        return HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: ControlType.MessageBoxCaption,
            objectName: captionType
        );
    }

    /// <summary>
    ///     This is a special member of the objectMapping/Language and really should sit there not here.
    ///     MessageBoxes are a bit more complicate to work with than "simple" objects and this takes their language-value and
    ///     returns it efficiently
    /// </summary>
    /// <param name="messageBoxName">A pseudonym for the MessageBox whose value is requested.</param>
    /// <returns>MessageBox text contents</returns>
    internal static string GenericGetMessageBoxText(string messageBoxName)
    {
        return HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: ControlType.MessageBox,
            objectName: messageBoxName
        );
    }

    /// <summary>
    ///     Rather than setting the cItem's Text, it just returns the value as string. Useful where ad-hoc modifications need
    ///     to be made on the fly, such as a metric/imperial conversion
    /// </summary>
    /// <param name="cItem">Name of the Item</param>
    /// <param name="senderForm">Name of the Form on which the Item is</param>
    /// <returns></returns>
    internal static string ReturnControlTextAsString(Control cItem,
                                                     Form senderForm)
    {
        return HelperDataLanguageTZ.DataReadDTObjectText(
            objectType: HelperDataLanguageTZ.GetControlType(
                controlType: cItem.GetType()),
            objectName: cItem.Name
        );
    }

    /// <summary>
    ///     This (mostly) sets the various texts for most Controls in various forms, especially labels and buttons/boxes.
    /// </summary>
    /// <param name="cItem">The Control whose details need adjusting</param>
    /// <param name="senderForm">Name of the Form on which the Control appears</param>
    /// <param name="parentNameToUse"></param>
    internal static void ReturnControlText(Control cItem,
                                           Form senderForm,
                                           string parentNameToUse = null)
    {
        if (parentNameToUse == null &&
            !(cItem is Form))
        {
            parentNameToUse = cItem.Parent.Name;
        }

        FrmMainApp frmMainAppInstance =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

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
                objectType: HelperDataLanguageTZ.GetControlType(
                    controlType: cItem.GetType()),
                objectName: cItem.Name
            );
        }
        else if (cItem is Form)
        {
            cItem.Text = HelperDataLanguageTZ.DataReadDTObjectText(
                objectType: ControlType.Form,
                objectName: cItem.Name);
        }
        else if (cItem is TextBox ||
                 cItem is ComboBox)
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
        else if (cItem is NumericUpDown nud)
        {
            if (senderForm.Name == "FrmSettings")
            {
                FrmMainApp.Logger.Trace(message: "Starting - cItem: " + nud.Name);
                _ = decimal.TryParse(
                    s: HelperDataApplicationSettings.DataReadSQLiteSettings(
                        tableName: "settings",
                        settingTabPage: parentNameToUse,
                        settingId: nud.Name
                    ), result: out decimal outVal);

                // if this doesn't exist, it'd return 0, which is illegal because the min-values can be higher than that
                nud.Value = Math.Max(val1: nud.Minimum, val2: outVal);
                nud.Text = outVal.ToString(provider: CultureInfo.InvariantCulture);
            }
        }
    }
}
