using GeoTagNinja.Helpers.Help;
using NUnit.Framework;
using System.Linq;

namespace GeoTagNinja.Tests;

/// <summary>
///     The Help (F1) text: it is embedded and parses, ids are unique, and every id the code opens the Help at exists
///     (a renamed heading would otherwise silently open the wrong page).
/// </summary>
[TestFixture]
public class HelpContentTests
{
    /// <summary>The ids passed to <c>FrmHelp.Attach</c> / <c>FrmHelp.Show</c> in the code. Add one here when you add a caller.</summary>
    private static readonly string[] IdsUsedByCode =
    [
        HelpContent.DefaultTopicId, "edit-files", "favourites", "gpx", "settings", "batch-rename"
    ];

    [Test]
    public void EmbeddedHelp_HasTopics_WithTextAndUniqueIds()
    {
        var topics = HelpContent.Topics;

        Assert.That(actual: topics.Count, expression: Is.GreaterThan(expected: 5));
        Assert.That(actual: topics.Select(selector: t => t.Id), expression: Is.Unique);
        Assert.That(actual: topics.All(predicate: t => t.Title.Length > 0 && t.Body.Length > 0), expression: Is.True);
    }

    [TestCaseSource(sourceName: nameof(IdsUsedByCode))]
    public void EveryIdTheCodeOpensTheHelpAt_Exists(string id)
    {
        Assert.That(actual: HelpContent.Topics.Any(predicate: t => t.Id == id), expression: Is.True);
    }

    [Test]
    public void Find_FallsBackToTheDefaultTopic_ForAnUnknownId()
    {
        Assert.That(actual: HelpContent.Find(id: "no-such-topic").Id, expression: Is.EqualTo(expected: HelpContent.DefaultTopicId));
        Assert.That(actual: HelpContent.Find(id: null).Id, expression: Is.EqualTo(expected: HelpContent.DefaultTopicId));
    }

    [Test]
    public void Parse_SplitsAtHeadings_AndIgnoresTextBeforeTheFirst()
    {
        var topics = HelpContent.Parse(markdown: "intro\n## One {#a}\nline 1\nline 2\n## Two {#b}\nline 3\n");

        Assert.That(actual: topics.Select(selector: t => t.Id), expression: Is.EqualTo(expected: new[] { "a", "b" }));
        Assert.That(actual: topics[index: 0].Title, expression: Is.EqualTo(expected: "One"));
        Assert.That(actual: topics[index: 0].Body, expression: Is.EqualTo(expected: "line 1\r\nline 2".Replace(oldValue: "\r\n", newValue: System.Environment.NewLine)));
    }
}
