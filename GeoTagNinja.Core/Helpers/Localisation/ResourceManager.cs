using GeoTagNinja.Resources.Languages;
using System.Globalization;
using System.Threading;

namespace GeoTagNinja.Helpers.Localisation;

internal static class ResourceManager
{
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
            LanguageLists.GetGenericControlName(controlName: controlName);

        string resourceValue = string.Empty;

        System.Resources.ResourceManager resourceManager = new(baseName: $"GeoTagNinja.Resources.Languages.{location}",
            assembly: typeof(Strings).Assembly);

        if (resourceKey.Contains(value: "Generic") ||
            location == HelperVariables.ResourceNameForGenericControlItems)
        {
            resourceKey = $"Generic_{resourceKey.Substring(startIndex: resourceKey.IndexOf(value: '_') + 1)}";
            // no idea how this comes along but alas.
            if (resourceKey.StartsWith(value: "Generic_Generic"))
            {
                resourceKey = resourceKey.Replace(oldValue: "Generic_Generic", newValue: "Generic_");
            }
        }
        // Alternatively if it's not been designated as Generic but is nonetheless then we use the lookup dict
        else if (resourceKeyInGenericMapping != HelperVariables.ControlItemNameNotGeneric)
        {
            resourceKey = resourceKeyInGenericMapping;
        }

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
