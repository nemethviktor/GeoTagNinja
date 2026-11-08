using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.Model;
using NUnit.Framework;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace GeoTagNinja.Tests;

/// <summary>
///     The multi-value tag type (keywords and friends): normalisation, the ExifTool bracketed-list reader, the
///     canonical text round trip and the write arguments, under every culture.
/// </summary>
[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]
public class TagListTests
{
    private readonly CultureInfo _culture;
    private CultureInfo _originalCulture;

    public TagListTests(string cultureName)
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

    [Test]
    public void From_TrimsDropsBlanksAndDuplicatesKeepingOrder()
    {
        TagList list = TagList.From(items: ["  b ", "a", "", null, "b", "a "]);
        CollectionAssert.AreEqual(expected: new[] { "b", "a" }, actual: list.ToArray());
    }

    [Test]
    public void From_DuplicateCheckIsOrdinal_TurkishIStaysDistinct()
    {
        // "I" and "i" are one letter pair in en-US and two in tr-TR; neither may merge here.
        TagList list = TagList.From(items: ["I", "i", "İ", "ı"]);
        Assert.AreEqual(expected: 4, actual: list.Count);
    }

    [Test]
    public void Empty_RendersAsTheBlankSentinelAndParsesBack()
    {
        Assert.AreEqual(expected: NullEquivalents.StringGeneric, actual: TagList.Empty.ToString());
        Assert.IsTrue(condition: TagList.Parse(text: NullEquivalents.StringGeneric).IsEmpty);
        Assert.IsTrue(condition: TagList.Parse(text: "").IsEmpty);
        Assert.IsTrue(condition: TagList.Parse(text: null).IsEmpty);
    }

    [Test]
    public void CanonicalText_RoundTrips_IncludingCommasInItems()
    {
        TagList list = TagList.From(items: ["gam, ma", "café", "a;b"]);
        TagList back = TagList.Parse(text: list.ToString());
        Assert.AreEqual(expected: list, actual: back);
        Assert.AreEqual(expected: 3, actual: back.Count);
    }

    [Test]
    public void AttributeValueFormatter_RoundTripsThroughText()
    {
        TagList list = TagList.From(items: ["one", "two"]);
        string text = AttributeValueFormatter.Format(value: list, context: ValueFormatContext.Display);
        Assert.IsTrue(condition: AttributeValueFormatter.TryParse(value: text,
            targetType: typeof(TagList),
            context: ValueFormatContext.Display,
            result: out System.IConvertible parsed));
        Assert.AreEqual(expected: list, actual: parsed);
    }

    [TestCase("[alpha,beta]", new[] { "alpha", "beta" })]
    [TestCase("[solo]", new[] { "solo" })]
    [TestCase("[gam|, ma,a||b,|[x|]]", new[] { "gam, ma", "a|b", "[x]" })]
    [TestCase("plain keyword", new[] { "plain keyword" })]
    [TestCase("[]", new string[0])]
    public void ParseExifToolOutput_ReadsBracketedEscapedLists(string raw,
                                                               string[] expected)
    {
        CollectionAssert.AreEqual(expected: expected, actual: TagList.ParseExifToolOutput(text: raw).ToArray());
    }

    [Test]
    public void ListTagArguments_AssignsEachItemSeparately()
    {
        System.Collections.Generic.IReadOnlyList<string> lines = ListTagArguments.Build(tag: "IPTC:Keywords",
            items: TagList.From(items: ["a", "b, c"]));
        CollectionAssert.AreEqual(
            expected: new[] { "-IPTC:Keywords=a", "-IPTC:Keywords=b, c" },
            actual: lines.ToArray());
    }

    [Test]
    public void ListTagArguments_EmptyListOnlyDeletes()
    {
        CollectionAssert.AreEqual(expected: new[] { "-XMP:Subject=" },
            actual: ListTagArguments.Build(tag: "XMP:Subject", items: TagList.Empty).ToArray());
    }

    [Test]
    public void ExifStructTags_ExpandsCreatorContactInfoIntoFlatTags()
    {
        System.Collections.Generic.Dictionary<string, string> tags = new()
        {
            ["XMP:CREATORCONTACTINFO"] =
                "{CiAdrCity=Ulaanbaatar|, MN,CiAdrExtadr=1 Main St,CiAdrRegion=UB,CiEmailWork=a@b.c}"
        };

        ExifStructTags.ExpandCreatorContactInfo(tagsKeyedUpper: tags);

        Assert.AreEqual(expected: "Ulaanbaatar, MN", actual: tags["XMP:CREATORCITY"]);
        Assert.AreEqual(expected: "1 Main St", actual: tags["XMP:CREATORADDRESS"]);
        Assert.AreEqual(expected: "UB", actual: tags["XMP:CREATORREGION"]);
        Assert.AreEqual(expected: "a@b.c", actual: tags["XMP:CREATORWORKEMAIL"]);
        Assert.IsFalse(condition: tags.ContainsKey(key: "XMP:CREATORCOUNTRY"));
    }

    [Test]
    public void ExifStructTags_DoesNotOverwriteAFlatTagThatIsAlreadyThere()
    {
        System.Collections.Generic.Dictionary<string, string> tags = new()
        {
            ["XMP:CREATORCONTACTINFO"] = "{CiAdrCity=FromStruct}",
            ["XMP:CREATORCITY"] = "Flat"
        };

        ExifStructTags.ExpandCreatorContactInfo(tagsKeyedUpper: tags);

        Assert.AreEqual(expected: "Flat", actual: tags["XMP:CREATORCITY"]);
    }

    [TestCase("[Jo]", "Jo")]
    [TestCase("Jo", "Jo")]
    [TestCase("[Jo,Al|, Bo]", "Jo; Al, Bo")]
    public void Byline_ListIsShownAsOneText(string raw,
                                            string expected)
    {
        Assert.AreEqual(expected: expected, actual: TagsToModelValueTransformations.T2M_ListAsText(parseResult: raw));
    }
}
