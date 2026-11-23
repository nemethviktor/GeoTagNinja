using NUnit.Framework;
using System.IO;
using System.Runtime.CompilerServices;

namespace GeoTagNinja.Tests;

/// <summary>
///     The WiX installer (<c>GeoTagNinja.Setup/Files.wxs</c>) ships translations by wildcard, so a new
///     <c>Strings.&lt;code&gt;.resx</c> needs no installer edit. This guards the one thing that could silently bring the
///     old problem back (an installed app that offers English only): the wildcard for the satellite assemblies being
///     edited away or narrowed. One of the few tests that read a source file, found through this file's compile-time path.
/// </summary>
[TestFixture]
public class InstallerLanguageCoverageTests
{
    private static string RepoRoot([CallerFilePath] string thisFile = "")
    {
        return Path.GetFullPath(path: Path.Combine(path1: Path.GetDirectoryName(path: thisFile)!, path2: ".."));
    }

    [Test]
    public void Installer_ShipsEveryTranslation_ByWildcard()
    {
        string files = File.ReadAllText(path: Path.Combine(path1: RepoRoot(), path2: "GeoTagNinja.Setup", path3: "Files.wxs"));

        Assert.That(actual: files, expression: Does.Contain(expected: @"**\GeoTagNinja.Core.resources.dll"),
            message: @"GeoTagNinja.Setup\Files.wxs no longer globs '**\GeoTagNinja.Core.resources.dll', so translations would not be installed.");
    }
}
