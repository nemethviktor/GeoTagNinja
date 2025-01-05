using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using GeoTagNinja.Resources.Languages;

namespace GeoTagNinja.Helpers;

internal static class HelperLocalisationResourceManager
{
    /// <summary>
    ///     Returns the value of a resource's text (based on control item)
    /// </summary>
    /// <param name="control">Name of the control</param>
    /// <param name="location">Which resource file to look in</param>
    /// <returns></returns>
    public static string GetResourceValue(Control control, string location)
    {
        ResourceManager resourceManager = new(resourceSource: typeof(Strings));
        string resourceKeyInGenericMapping =
            HelperGenericAncillaryListsArrays.GetGenericControlName(controlName: control.Name);

        // Attempt to get the resource value based on the control's name
        // This has been done a little oddly because items like btn_OK became btn_Generic_Ok and lbl_Generic_OK so we're taking whatever comes after the first underscore.
        string resourceKey;
        if (control.Name.Contains(value: "Generic"))
        {
            resourceKey = control.Name.Substring(startIndex: control.Name.IndexOf(value: '_') + 1);
        }
        // Alternatively if it's not been designated as Generic but is nonetheless then we use the lookup dict
        else if (resourceKeyInGenericMapping != HelperVariables.ControlItemNameNotGeneric)
        {
            resourceKey = resourceKeyInGenericMapping;
        }
        // Still alternatively we just take the name of the item
        else
        {
            resourceKey = control.Name;
        }

        if (control is Form &&
            !resourceKey.StartsWith(value: "frm_"))
        {
            resourceKey = $"frm_{resourceKey}";
        }

        string resourceValue = string.Empty;
        try
        {
            Thread.CurrentThread.CurrentUICulture =
                new CultureInfo(name: CultureInfo.GetCultureInfo(name: FrmMainApp.AppLanguage).Name);
            resourceValue = resourceManager.GetString(name: resourceKey);
        }
        catch
        {
            // nothing
        }

        if (resourceKey.Contains(value: "Altitude") &&
            !string.IsNullOrEmpty(value: resourceValue))
        {
            resourceValue = $"{resourceValue} [{HelperVariables.UOMAbbreviated}]";
            return resourceValue;
        }

        return !string.IsNullOrEmpty(value: resourceValue)
            ? resourceValue
            : string.Empty;
    }

    /// <summary>
    ///     Returns the value of a resource's text (based on control name)
    /// </summary>
    /// <param name="controlName">Name of the control as string</param>
    /// <param name="location">Which resource file to look in</param>
    /// <returns></returns>
    public static string GetResourceValue(string controlName, string location)
    {
        string resourceKey = controlName; // bit lame but to keep in line with the above block.
        string resourceKeyInGenericMapping =
            HelperGenericAncillaryListsArrays.GetGenericControlName(controlName: controlName);

        string resourceValue = string.Empty;

        ResourceManager resourceManager = new(baseName: $"GeoTagNinja.Resources.Languages.{location}",
            assembly: typeof(HelperLocalisationResourceManager).Assembly);

        if (resourceKey.Contains(value: "Generic") ||
            location == HelperVariables.ResourceNameForGenericControlItems)
        {
            resourceKey = $"Generic_{resourceKey.Substring(startIndex: resourceKey.IndexOf(value: '_') + 1)}";
        }
        // Alternatively if it's not been designated as Generic but is nonetheless then we use the lookup dict
        else if (resourceKeyInGenericMapping != HelperVariables.ControlItemNameNotGeneric)
        {
            resourceKey = resourceKeyInGenericMapping;
        }

        try
        {
            Thread.CurrentThread.CurrentUICulture =
                new CultureInfo(name: CultureInfo.GetCultureInfo(name: FrmMainApp.AppLanguage).Name);
            resourceValue = resourceManager.GetString(name: resourceKey);
        }
        catch
        {
            // nothing
        }

        if (resourceKey.Contains(value: "Altitude") &&
            !string.IsNullOrEmpty(value: resourceValue))
        {
            resourceValue = $"{resourceValue} [{HelperVariables.UOMAbbreviated}]";
            return resourceValue;
        }

        return !string.IsNullOrEmpty(value: resourceValue)
            ? resourceValue
            : string.Empty;
    }
}
