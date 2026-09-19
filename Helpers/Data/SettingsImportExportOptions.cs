namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     The groups of settings that can be exported to, or imported from, a settings file.
/// </summary>
/// <remarks>
///     <para>
///         The member names are load-bearing: <see cref="SettingsExport" /> and <see cref="SettingsImport" /> enumerate
///         them with <see cref="System.Enum.GetNames" /> and feed each name to
///         <see cref="SettingsImportExportTables.GetSettingsImportExportTableName" /> to find the backing SQLite table.
///         Renaming a member therefore changes the on-disk format.
///     </para>
///     <para>
///         This enum used to be declared in <c>FrmSettings.cs</c>, alongside but outside the form class, purely
///         because that is where the export button lives. It describes the data, not the dialog, so it belongs here
///         with the code that reads and writes it.
///     </para>
/// </remarks>
internal enum SettingsImportExportOptions
{
    /// <summary>General application and user preferences.</summary>
    ApplicationSettings,

    /// <summary>The per-country rules deciding which admin level supplies the city name.</summary>
    CityRulesSettings,

    /// <summary>The user-defined toponomy override rules.</summary>
    CustomRulesSettings
}
