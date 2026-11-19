using GeoTagNinja.Helpers.Localisation;
using GeoTagNinja.Resources.Languages;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     The WinForms half of the text lookup: works out the resource key from a <see cref="Control" />'s name.
///     The string-only lookup lives in Core (<see cref="ResourceManager" />).
/// </summary>
internal static class ControlResourceManager
{
    /// <summary>
    ///     Returns the value of a resource's text (based on control item)
    /// </summary>
    /// <param name="control">Name of the control</param>
    /// <param name="location">Which resource file to look in</param>
    /// <returns></returns>
    public static string GetResourceValue(Control control, string location)
    {
        System.Resources.ResourceManager resourceManager = new(resourceSource: typeof(Strings));
        string resourceKeyInGenericMapping =
            LanguageLists.GetGenericControlName(controlName: control.Name);

        // Attempt to get the resource value based on the control's name
        // This has been done a little oddly because items like btn_OK became btn_Generic_Ok and lbl_Generic_OK so we're taking whatever comes after the first underscore.
        string resourceKey = control.Name.Contains(value: "Generic")
            ? control.Name.Substring(startIndex: control.Name.IndexOf(value: '_') + 1)
            // Alternatively if it's not been designated as Generic but is nonetheless then we use the lookup dict
            : resourceKeyInGenericMapping != HelperVariables.ControlItemNameNotGeneric
                ? resourceKeyInGenericMapping
                // Still alternatively we just take the name of the item
                : control.Name;
        if (control is Form &&
            !resourceKey.StartsWith(value: "frm_"))
        {
            resourceKey = $"frm_{resourceKey}";
        }

        string resourceValue = string.Empty;
        try
        {
            Thread.CurrentThread.CurrentUICulture =
                new CultureInfo(name: LanguageManager.CultureFor(code: HelperVariables.AppLanguage).Name);
            resourceValue = resourceManager.GetString(name: resourceKey);
        }
        catch
        {
            // nothing
        }

        if (resourceKey.Contains(value: "Altitude") &&
            !string.IsNullOrEmpty(value: resourceValue) &&
            resourceKey != "ckb_DoNotDownloadAltitude")
        {
            resourceValue = $"{resourceValue} [{HelperVariables.UOMAbbreviated}]";
            return resourceValue;
        }

        return !string.IsNullOrEmpty(value: resourceValue)
            ? resourceValue
            : string.Empty;
    }
}
