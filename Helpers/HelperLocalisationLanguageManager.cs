using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using GeoTagNinja.Resources.Languages;

namespace GeoTagNinja.Helpers;

internal static class HelperLocalisationLanguageManager
{
    /// <summary>
    ///     Gets the list of languages as TwoLetterISOLanguageName, into which the app has been at least partially translated.
    ///     This is called _only_ from within Settings.
    /// </summary>
    /// <returns></returns>
    public static List<string> GetTranslatedLanguages()
    {
        List<string> languages = new();

        languages.Add(item: "en");

        ResourceManager rm = new(resourceSource: typeof(Strings)); // "Strings" here is Languages.Strings type.

        CultureInfo[] cultures = CultureInfo.GetCultures(types: CultureTypes.AllCultures);
        foreach (CultureInfo culture in cultures)
        {
            try
            {
                ResourceSet rs = rm.GetResourceSet(culture: culture, createIfNotExists: true, tryParents: false);
                // or ResourceSet rs = rm.GetResourceSet(new CultureInfo(culture.TwoLetterISOLanguageName), true, false);
                if (rs != null &&
                    culture.TwoLetterISOLanguageName != "iv")
                {
                    languages.Add(item: culture.TwoLetterISOLanguageName);
                }
            }
            catch (CultureNotFoundException exc)
            {
                // dont care
            }
        }


        return languages;
    }
}