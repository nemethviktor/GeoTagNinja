using GeoTagNinja.Resources.Languages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace GeoTagNinja.Helpers.Localisation;

/// <summary>
/// Provides helper methods for retrieving localized strings and managing ancillary lists.
/// </summary>
internal static class LanguageManager
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

        System.Resources.ResourceManager rm = new(resourceSource: typeof(Strings)); // "Strings" here is Languages.Strings type.

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

    /// <summary>
    ///     Share of the English strings a language must have translated before a new user whose Windows is set to that
    ///     language is given the app in it. Below this a half-English UI would be worse than a fully English one.
    /// </summary>
    internal const double MinimumCoverageForAutoSelect = 0.8;

    /// <summary>
    ///     Picks the language a user who has never chosen one should get: the first of the Windows display language
    ///     (and its parents, e.g. hu-HU then hu) that is translated well enough, otherwise English.
    ///     Nothing is stored for this, so the app keeps following Windows (and translation progress) until the user
    ///     picks a language in Settings.
    /// </summary>
    /// <returns>A TwoLetterISOLanguageName, as stored in the cbx_Language setting.</returns>
    public static string ResolveDefaultLanguage()
    {
        System.Resources.ResourceManager rm = new(resourceSource: typeof(Strings));
        HashSet<string> englishKeys = [];
        foreach (DictionaryEntry entry in rm.GetResourceSet(culture: CultureInfo.InvariantCulture,
                     createIfNotExists: true, tryParents: false))
        {
            _ = englishKeys.Add(item: (string)entry.Key);
        }

        int englishCount = englishKeys.Count;

        for (CultureInfo culture = CultureInfo.InstalledUICulture;
             culture.LCID != CultureInfo.InvariantCulture.LCID;
             culture = culture.Parent)
        {
            string code = culture.TwoLetterISOLanguageName;
            if (code == HelperVariables.DefaultEnglishString)
            {
                break;
            }

            try
            {
                // Same test as GetTranslatedLanguages: the file must exist for exactly this culture, otherwise the
                // app could not load it once the code is used as the UI culture.
                ResourceSet rs = rm.GetResourceSet(culture: CultureInfo.GetCultureInfo(name: code),
                    createIfNotExists: true, tryParents: false);
                if (rs != null &&
                    CountTranslated(resourceSet: rs, englishKeys: englishKeys) >=
                    englishCount * MinimumCoverageForAutoSelect)
                {
                    return code;
                }
            }
            catch (CultureNotFoundException)
            {
                // dont care
            }
        }

        return HelperVariables.DefaultEnglishString;
    }

    /// <summary>
    ///     Counts the non-empty string values, restricted to keys the English file also has when given
    ///     (so an obsolete key left in a translation does not count towards its coverage).
    /// </summary>
    private static int CountTranslated(ResourceSet resourceSet, HashSet<string> englishKeys)
    {
        int count = 0;
        if (resourceSet == null)
        {
            return count;
        }

        foreach (DictionaryEntry entry in resourceSet)
        {
            if (entry.Value is string { Length: > 0 } &&
                (englishKeys == null || englishKeys.Contains(item: (string)entry.Key)))
            {
                count++;
            }
        }

        return count;
    }
}