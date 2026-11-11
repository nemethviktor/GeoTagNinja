using GeoTagNinja.View.Forms;
using System;
using System.Globalization;

namespace GeoTagNinja.Helpers.Localisation;

/// <summary>
///     The text of Batch Rename (captions, messages and the reasons a file can't be renamed), looked up in
///     <c>Strings.resx</c> so it can be translated.
/// </summary>
/// <remarks>
///     <para>
///         Not <c>HelperControlAndMessageBoxHandling.ReturnControlText</c>: that returns an empty string when no language
///         is set (as in the tests) and changes the thread's UI culture as a side effect. This asks the resources
///         directly, in the app's language if there is one, and the resource fallback takes care of any language that has
///         not translated a key yet (English is the neutral resource).
///     </para>
///     <para>
///         A key that is missing altogether comes back as the key itself, so the gap is visible on screen (and
///         <c>BatchRenameTextTests</c> catches the ones built from enum names) rather than blank.
///     </para>
/// </remarks>
internal static class BatchRenameText
{
    private static readonly System.Resources.ResourceManager Resources =
        new(baseName: "GeoTagNinja.Resources.Languages.Strings", assembly: typeof(BatchRenameText).Assembly);

    /// <summary>The text for a key, with <c>{0}</c>, <c>{1}</c>... filled in from <paramref name="args" />.</summary>
    internal static string Get(string key,
                               params object[] args)
    {
        string text = Resources.GetString(name: key, culture: Culture());
        if (string.IsNullOrEmpty(value: text))
        {
            text = key;
        }

        return args.Length == 0
            ? text
            : string.Format(provider: CultureInfo.CurrentCulture, format: text, args: args);
    }

    /// <summary>The key's text exists (in the neutral resources, which every language falls back to).</summary>
    internal static bool Exists(string key)
    {
        return !string.IsNullOrEmpty(value: Resources.GetString(name: key, culture: CultureInfo.InvariantCulture));
    }

    private static CultureInfo Culture()
    {
        try
        {
            string language = FrmMainApp.AppLanguage;
            return string.IsNullOrEmpty(value: language)
                ? CultureInfo.CurrentUICulture
                : CultureInfo.GetCultureInfo(name: language);
        }
        catch (Exception ex) when (ex is CultureNotFoundException or ArgumentException)
        {
            return CultureInfo.CurrentUICulture;
        }
    }
}
