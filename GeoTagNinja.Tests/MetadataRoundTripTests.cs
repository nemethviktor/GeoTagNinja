using GeoTagNinja.Model;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Threading;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Tests;

/// <summary>
///     Whole-path checks through <see cref="DirectoryElement" />: a value entered by the user, carried through the
///     staged-edit model, and rendered for ExifTool.
/// </summary>
/// <remarks>
///     These are the two regressions reported from the field, expressed as tests. They deliberately go through the
///     same public API the edit form uses rather than calling the formatter directly.
/// </remarks>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class MetadataRoundTripTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public MetadataRoundTripTests(string cultureName)
    {
        _culture = new CultureInfo(name: cultureName);
    }

    [SetUp]
    public void SetUp()
    {
        _originalCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = _culture;
    }

    [TearDown]
    public void TearDown()
    {
        Thread.CurrentThread.CurrentCulture = _originalCulture;
    }

    private static DirectoryElement NewFile()
    {
        return new DirectoryElement(
            itemNameWithoutPath: "sample.jpg",
            type: DirectoryElement.ElementType.File,
            fileNameWithPath: @"C:\photos\sample.jpg");
    }

    /// <summary>
    ///     "Insert create date" on a file that has none: the form hands over a date rendered in the user's culture,
    ///     and what reaches the file must still be the same instant, in ExifTool's layout.
    /// </summary>
    [Test]
    public void InsertedCreateDate_ReachesExifToolUnchanged()
    {
        DateTime chosen = new(year: 2018, month: 6, day: 22, hour: 19, minute: 32, second: 53);
        DirectoryElement de = NewFile();

        // What a DateTimePicker / text box would give us.
        string asTypedByUser = chosen.ToString(provider: _culture);

        de.SetAttributeValueAnyType(
            attribute: ElementAttribute.CreateDate,
            value: asTypedByUser,
            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
            isMarkedForDeletion: false);

        string forExifTool = de.GetAttributeValueAsString(
            attribute: ElementAttribute.CreateDate,
            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
            context: ValueFormatContext.ExifTool);

        Assert.That(actual: forExifTool, expression: Is.EqualTo(expected: "2018-06-22 19:32:53"),
            message: $"user typed '{asTypedByUser}' in {_culture.Name}");
    }

    /// <summary>
    ///     A decimal typed by the user must reach the file as the same number, with a dot.
    /// </summary>
    [Test]
    public void TypedAltitude_ReachesExifToolUnchanged()
    {
        DirectoryElement de = NewFile();
        string asTypedByUser = 5.4.ToString(provider: _culture);

        de.SetAttributeValueAnyType(
            attribute: ElementAttribute.GPSAltitude,
            value: asTypedByUser,
            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
            isMarkedForDeletion: false);

        string forExifTool = de.GetAttributeValueAsString(
            attribute: ElementAttribute.GPSAltitude,
            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
            context: ValueFormatContext.ExifTool);

        Assert.That(actual: forExifTool, expression: Is.EqualTo(expected: "5.4"),
            message: $"user typed '{asTypedByUser}' in {_culture.Name}");
    }

    /// <summary>A coordinate typed by the user must not drift, in either sign.</summary>
    [TestCase(-33.8688)]
    [TestCase(151.2093)]
    public void TypedCoordinate_ReachesExifToolUnchanged(double coordinate)
    {
        DirectoryElement de = NewFile();
        string asTypedByUser = coordinate.ToString(provider: _culture);

        de.SetAttributeValueAnyType(
            attribute: ElementAttribute.GPSLatitude,
            value: asTypedByUser,
            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
            isMarkedForDeletion: false);

        Assert.That(actual: de.GetAttributeValueAsString(
                        attribute: ElementAttribute.GPSLatitude,
                        version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                        context: ValueFormatContext.ExifTool),
            expression: Is.EqualTo(expected: coordinate.ToString(provider: CultureInfo.InvariantCulture)),
            message: $"user typed '{asTypedByUser}' in {_culture.Name}");
    }

    /// <summary>Text values must survive untouched, including non-ASCII place names.</summary>
    [TestCase("Ciudad Autonoma de Buenos Aires")]
    [TestCase("Szekesfehervar")]
    [TestCase("Bengaluru")]
    [TestCase("Sydney")]
    public void TextValues_SurviveUnchanged(string city)
    {
        DirectoryElement de = NewFile();
        de.SetAttributeValueAnyType(
            attribute: ElementAttribute.City,
            value: city,
            version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
            isMarkedForDeletion: false);

        Assert.That(actual: de.GetAttributeValueAsString(
                        attribute: ElementAttribute.City,
                        version: DirectoryElement.AttributeVersion.Stage3ReadyToWrite,
                        context: ValueFormatContext.ExifTool),
            expression: Is.EqualTo(expected: city));
    }
}
