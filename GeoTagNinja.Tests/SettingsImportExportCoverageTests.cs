using GeoTagNinja.Helpers.Data;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace GeoTagNinja.Tests;

/// <summary>
///     Settings import/export has to cover everything the user made in the settings database, so that moving to another
///     machine does not lose it. These stop a new table (or option) being added without the other half.
/// </summary>
[TestFixture]
public class SettingsImportExportCoverageTests
{
    /// <summary>Window positions and sizes: specific to the machine, so deliberately not exported.</summary>
    private const string MachineSpecificTable = "appLayout";

    private static string[] OptionNames => Enum.GetNames(enumType: typeof(SettingsImportExportOptions));

    [TestCaseSource(nameof(OptionNames))]
    public void EveryOption_MapsToAKnownTable(string option)
    {
        string table = SettingsImportExportTables.GetSettingsImportExportTableName(settingName: option);

        Assert.That(actual: table, expression: Is.Not.Null, message: $"{option} has no table");
        Assert.That(actual: DatabaseSchema.IsKnownTable(tableName: table), expression: Is.True, message: $"{option} maps to unknown table {table}");
    }

    [TestCaseSource(nameof(OptionNames))]
    public void EveryOption_HasACaptionInTheDialog(string option)
    {
        ResourceManager resources = new(baseName: "GeoTagNinja.Resources.Languages.Strings", assembly: typeof(DatabaseSchema).Assembly);

        Assert.That(actual: resources.GetString(name: "ckb_ImportExport_" + option, culture: CultureInfo.InvariantCulture),
            expression: Is.Not.Null.And.Not.Empty, message: $"ckb_ImportExport_{option} is missing from Strings.resx");
    }

    [Test]
    public void EveryTableOfTheDatabase_IsExportable_ExceptTheMachineSpecificOne()
    {
        string[] mapped = OptionNames.Select(selector: option => SettingsImportExportTables.GetSettingsImportExportTableName(settingName: option))
                                     .ToArray();

        foreach (string table in DatabaseSchema.AllTableNames.Where(predicate: name => name != MachineSpecificTable))
        {
            Assert.That(actual: mapped, expression: Does.Contain(expected: table).IgnoreCase,
                message: $"The table '{table}' cannot be imported or exported: add a SettingsImportExportOptions member for it.");
        }
    }
}
