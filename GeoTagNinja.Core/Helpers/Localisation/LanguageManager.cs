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

    private static readonly Dictionary<string, CultureInfo> CultureForCodeCache = new();

    /// <summary>
    ///     The culture to use as the UI culture for a stored language code (a TwoLetterISOLanguageName such as "hu").
    ///     Usually that is simply the culture of the same name. But a language can ship under a more specific name:
    ///     Simplified Chinese is <c>Strings.zh-Hans.resx</c>, yet the settings list and the stored value say "zh", and
    ///     .NET never looks from "zh" down to "zh-Hans". So when "zh" has no resources of its own, the culture of that
    ///     language that does have a resource file is used instead. Falls back to the plain culture (English, or a
    ///     language nobody has translated, then simply shows the neutral resources).
    /// </summary>
    /// <param name="code">A TwoLetterISOLanguageName.</param>
    public static CultureInfo CultureFor(string code)
    {
        if (CultureForCodeCache.TryGetValue(key: code, value: out CultureInfo cached))
        {
            return cached;
        }

        CultureInfo plain = CultureInfo.GetCultureInfo(name: code);
        CultureInfo found = plain;
        if (code != HelperVariables.DefaultEnglishString)
        {
            System.Resources.ResourceManager rm = new(resourceSource: typeof(Strings));
            if (!HasResources(rm: rm, culture: plain))
            {
                foreach (CultureInfo culture in CultureInfo.GetCultures(types: CultureTypes.AllCultures))
                {
                    if (culture.TwoLetterISOLanguageName == code &&
                        culture.Name != plain.Name &&
                        HasResources(rm: rm, culture: culture))
                    {
                        found = culture;
                        break;
                    }
                }
            }
        }

        CultureForCodeCache[key: code] = found;
        return found;
    }

    private static bool HasResources(System.Resources.ResourceManager rm, CultureInfo culture)
    {
        try
        {
            return rm.GetResourceSet(culture: culture, createIfNotExists: true, tryParents: false) != null;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

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
                // The resource file must be for this culture or one of its parents: a Windows set to zh-TW (so
                // zh-Hant, zh) must not get the Simplified Chinese file, but zh-CN (zh-Hans, zh) may.
                CultureInfo resourceCulture = CultureFor(code: code);
                ResourceSet rs = IsSelfOrParent(culture: culture, candidate: resourceCulture)
                    ? rm.GetResourceSet(culture: resourceCulture, createIfNotExists: true, tryParents: false)
                    : null;
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

    private static bool IsSelfOrParent(CultureInfo culture, CultureInfo candidate)
    {
        for (CultureInfo c = culture; c.LCID != CultureInfo.InvariantCulture.LCID; c = c.Parent)
        {
            if (c.Name == candidate.Name)
            {
                return true;
            }
        }

        return false;
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