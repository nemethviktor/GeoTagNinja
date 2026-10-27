using GeoTagNinja.Helpers.Exif;
using NUnit.Framework;
using System;

namespace GeoTagNinja.Tests;

/// <summary>
///     The de-duplication of ExifTool argument lines (<see cref="ExifArgumentLines" />). The write path relies on exact
///     repeats being ignored, and used to wrongly ignore merely similar arguments (a substring match).
/// </summary>
[TestFixture]
public class ExifArgumentLinesTests
{
    private static readonly string NL = Environment.NewLine;

    [Test]
    public void AnExactRepeatIsNotAddedAgain()
    {
        string args = "";
        Assert.That(ExifArgumentLines.AddUnique(arguments: ref args, line: "-GPSLatitude=51.5"), Is.True);
        Assert.That(ExifArgumentLines.AddUnique(arguments: ref args, line: "-GPSLatitude=51.5"), Is.False);
        Assert.That(args, Is.EqualTo($"-GPSLatitude=51.5{NL}"));
    }

    [Test]
    public void ADifferentValueOfTheSameTagThatMerelyStartsTheSameWayIsKept()
    {
        // this is what the substring check got wrong: "Foo" was swallowed because "Foobar" contained it
        string args = "";
        _ = ExifArgumentLines.AddUnique(arguments: ref args, line: "-IPTC:City=Foobar");
        Assert.That(ExifArgumentLines.AddUnique(arguments: ref args, line: "-IPTC:City=Foo"), Is.True);
        Assert.That(args, Is.EqualTo($"-IPTC:City=Foobar{NL}-IPTC:City=Foo{NL}"));
    }

    [Test]
    public void AShorterPathIsNotMistakenForALongerOneItIsPartOf()
    {
        string args = "";
        _ = ExifArgumentLines.AddUnique(arguments: ref args, line: @"C:\photos\IMG_10.jpg");
        Assert.That(ExifArgumentLines.AddUnique(arguments: ref args, line: @"C:\photos\IMG_1.jpg"), Is.True);
    }

    [Test]
    public void AClearIsNotMistakenForASetOfTheSameTag()
    {
        // "-GPSLatitude=" is a substring of "-GPSLatitude=51.5" but a different instruction
        string args = "";
        _ = ExifArgumentLines.AddUnique(arguments: ref args, line: "-GPSLatitude=51.5");
        Assert.That(ExifArgumentLines.AddUnique(arguments: ref args, line: "-GPSLatitude="), Is.True);
    }

    [Test]
    public void TheRepeatsTheWritePathMakesOnPurposeAreStillCaught()
    {
        // a tag is set early on and again by the generic step at the end; a delete is added twice (once empty)
        string args = "";
        foreach (string line in new[] { "-Title=Hello", "-City=", "-Title=Hello", "-City=" })
        {
            _ = ExifArgumentLines.AddUnique(arguments: ref args, line: line);
        }

        Assert.That(args, Is.EqualTo($"-Title=Hello{NL}-City={NL}"));
    }

    [Test]
    public void LineBreaksOfEitherKindAreUnderstood()
    {
        Assert.That(ExifArgumentLines.ContainsLine(arguments: "-a=1\r\n-b=2\r\n", line: "-b=2"), Is.True);
        Assert.That(ExifArgumentLines.ContainsLine(arguments: "-a=1\n-b=2\n", line: "-b=2"), Is.True);
        Assert.That(ExifArgumentLines.ContainsLine(arguments: "-a=1\r\n-b=2\r\n", line: "-b=2\r"), Is.False);
    }

    [Test]
    public void EmptyArgumentsContainNothing()
    {
        Assert.That(ExifArgumentLines.ContainsLine(arguments: "", line: "-a=1"), Is.False);
        Assert.That(ExifArgumentLines.ContainsLine(arguments: null!, line: "-a=1"), Is.False);
    }

    [Test]
    public void TheLineBreakIsOptional()
    {
        string args = "";
        _ = ExifArgumentLines.AddUnique(arguments: ref args, line: "-x", addNewLine: false);
        Assert.That(args, Is.EqualTo("-x"));
    }
}
