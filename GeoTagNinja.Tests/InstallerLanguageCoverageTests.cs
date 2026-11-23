using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace GeoTagNinja.Tests;

/// <summary>
///     The installer (<c>GeoTagNinja_Setup.vdproj</c>) lists every file by hand: it has no wildcards. A language that
///     builds fine but is not listed there is simply missing from the installed app, which then offers English only.
///     These fail when a <c>Strings.&lt;code&gt;.resx</c> has no satellite assembly in the installer. The one
///     exception to "pure logic only": it reads two source files, found through the compile-time path of this file.
/// </summary>
[TestFixture]
public class InstallerLanguageCoverageTests
{
    /// <summary>A backslash, as the regex engine wants it (<c>\x5C</c>), twice because the .vdproj writes it doubled.</summary>
    private const string DoubledBackslash = @"\x5C{2}";

    private static string RepoRoot([CallerFilePath] string thisFile = "")
    {
        return Path.GetFullPath(path: Path.Combine(path1: Path.GetDirectoryName(path: thisFile)!, path2: ".."));
    }

    private static string[] LanguageCodes()
    {
        return Directory.GetFiles(path: Path.Combine(path1: RepoRoot(), path2: "Resources", path3: "Languages"), searchPattern: "Strings.*.resx")
                        .Select(selector: file => Path.GetFileNameWithoutExtension(path: file).Substring(startIndex: "Strings.".Length))
                        .OrderBy(keySelector: code => code)
                        .ToArray();
    }

    [TestCaseSource(nameof(LanguageCodes))]
    public void EveryTranslation_IsShippedByTheInstaller(string code)
    {
        string setup = File.ReadAllText(path: Path.Combine(path1: RepoRoot(), path2: "GeoTagNinja_Setup", path3: "GeoTagNinja_Setup.vdproj"));

        // The file entry for the satellite assembly names its target folder by key...
        Match file = Regex.Match(input: setup,
            pattern: DoubledBackslash + Regex.Escape(str: code) + DoubledBackslash
                     + @"GeoTagNinja\.Core\.resources\.dll""\s+""TargetName"" = ""8:GeoTagNinja\.Core\.resources\.dll""\s+""Tag"" = ""8:""\s+""Folder"" = ""8:(?<key>_[0-9A-F]+)""");

        Assert.That(actual: file.Success, expression: Is.True,
            message: $"Strings.{code}.resx has no '{code}/GeoTagNinja.Core.resources.dll' file entry in GeoTagNinja_Setup.vdproj, so the installed app would not offer it. "
                     + $"Add one (Setup project > File System > Application Folder > a '{code}' subfolder), copying an existing language.");

        // ...and that folder has to be called after the language, or .NET never finds the satellite assembly.
        Assert.That(actual: Regex.IsMatch(input: setup, pattern: ":" + file.Groups[groupname: "key"].Value + @"""\s+\{\s+""Name"" = ""8:" + Regex.Escape(str: code) + @""""),
            expression: Is.True, message: $"The '{code}' file entry points at an installer folder that is not named '{code}'.");
    }
}
