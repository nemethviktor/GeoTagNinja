using GeoTagNinja.Resources.Languages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace GeoTagNinja.Helpers;

internal static class HelperLocalisationLanguageManager
{
    /// <summary>
    ///     Gets the list of languages as TwoLetterISOLanguageName, into which the app has been at least partially translated
    ///     [length > 0].
    ///     This is called _only_ from within Settings.
    /// </summary>
    /// <returns>A List(string) of TwoLetterISOLanguageNames</returns>
    public static List<string> GetTranslatedLanguages()
    {
        List<string> languages = [];

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
                    // We want to make sure there are > 0 items in there. E.g. someone could do a weblate with an empty set, which is no use. 
                    IDictionaryEnumerator enumerator = rs.GetEnumerator();
                    using IDisposable enumerator1 = enumerator as IDisposable;
                    int length = 0;
                    while (enumerator.MoveNext())
                    {
                        length++;
                        // tbh we don't need to count it all
                        if (length > 2)
                        {
                            break;
                        }
                    }

                    if (length > 0)
                    {
                        languages.Add(item: culture.TwoLetterISOLanguageName);
                    }
                }
            }
            catch (CultureNotFoundException)
            {
                // dont care
            }
        }

        return languages;
    }
}