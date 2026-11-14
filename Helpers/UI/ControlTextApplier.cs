using NLog;
using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.Localisation;
using GeoTagNinja.View.Forms;
using System;
using System.Globalization;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.UI;

internal static class ControlTextApplier
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     This (mostly) sets the various texts for most Controls in various forms, especially labels and buttons/boxes.
    /// </summary>
    /// <param name="control">The Control whose details need adjusting</param>
    /// <param name="senderForm">Name of the Form on which the Control appears. Only relevant for NUDs on the Settings Form</param>
    /// <param name="parentNameToUse">Obsolete most likely at this stage.</param>
    internal static void ReturnControlText(Control control, Form senderForm = null, string parentNameToUse = null)
    {
        if (parentNameToUse == null &&
            control is not Form)
        {
            parentNameToUse = control.Parent.Name;
        }

        // Note to self: do not add TextBoxes and ComboBoxes.
        // Also cannot add ColumnHeader because it's not a Control
        if (
            control is Label or
            GroupBox or
            Button or
            CheckBox or
            TabPage or
            RichTextBox or
            RadioButton or
            Form
        //||
        )
        {
            Log.Trace(message: $"Starting - control: {control.Name}");

            control.Text = ControlResourceManager.GetResourceValue(control: control, location: "Strings");
        }

        else if (control is NumericUpDown nud)
        {
            if (senderForm != null &&
                senderForm.Name == "FrmSettings")
            {
                Log.Trace(message: $"Starting - control: {nud.Name}");
                _ = decimal.TryParse(
                    s: ApplicationSettings.DataReadSQLiteSettings(
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
}
