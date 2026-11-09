using GeoTagNinja.Helpers.Update;
using NUnit.Framework;
using System.Linq;

namespace GeoTagNinja.Tests;

/// <summary>
///     What the update dialogs may show from <c>changelog.md</c>: only released builds, only the range the user
///     is missing, and nothing that can inject markup.
/// </summary>
[TestFixture]
public class ChangelogParserTests
{
    private const string Changelog =
        "# GeoTagNinja Changelog\r\n\r\n" +
        "**Build 9xxx [202xxxxx]** (upcoming release)\r\n- NEW & UPDATED:\r\n\t- Secret future feature\r\n" +
        "**Build 9807 [20261107]**\r\n- NEW & UPDATED:\r\n\t- Added `KML` **export**\r\n- BUGS & FIXES:\r\n\t- Fixed a thing\r\n" +
        "**Build 9794 [20261025]**\r\n- NEW & UPDATED:\r\n\t- Older entry\r\n" +
        "**Build 9519 [20260123] // Build 9520 [20260124]**\r\n- Pair entry\r\n" +
        "**Build 9456 [20251121]**\r\n- Ancient entry\r\n";

    [Test]
    public void TheUpcomingEntryIsNeverReturned_NoMatterHowHighTheLimit()
    {
        var sections = ChangelogParser.Between(markdown: Changelog, afterBuild: 0, upToBuild: int.MaxValue);

        Assert.That(sections.Select(selector: s => s.Build), Is.EqualTo(new[] { 9807, 9794, 9520, 9456 }));
        Assert.That(ChangelogParser.ToHtmlBody(sections: sections), Does.Not.Contain("Secret future feature"));
    }

    [Test]
    public void OnlyEntriesNewerThanInstalledAndNotNewerThanReleasedAreReturned()
    {
        var sections = ChangelogParser.Between(markdown: Changelog, afterBuild: 9520, upToBuild: 9794);

        Assert.That(sections.Select(selector: s => s.Build), Is.EqualTo(new[] { 9794 }));
    }

    [Test]
    public void AHeadingNamingTwoBuildsCountsAsTheHigherOne()
    {
        var sections = ChangelogParser.Between(markdown: Changelog, afterBuild: 9519, upToBuild: 9520);

        Assert.That(sections.Select(selector: s => s.Build), Is.EqualTo(new[] { 9520 }));
    }

    [Test]
    public void EmptyOrMissingTextGivesNothing()
    {
        Assert.That(ChangelogParser.Between(markdown: null, afterBuild: 0, upToBuild: 9999), Is.Empty);
        Assert.That(ChangelogParser.Between(markdown: "", afterBuild: 0, upToBuild: 9999), Is.Empty);
    }

    [Test]
    public void BulletsNestByIndentAndInlineFormattingIsRendered()
    {
        var sections = ChangelogParser.Between(markdown: Changelog, afterBuild: 9794, upToBuild: 9807);
        string html = ChangelogParser.ToHtmlBody(sections: sections);

        Assert.That(html, Does.Contain("<h2>Build 9807 [20261107]</h2>"));
        Assert.That(html, Does.Contain("NEW &amp; UPDATED:"));
        Assert.That(html, Does.Contain("<code>KML</code>"));
        Assert.That(html, Does.Contain("<strong>export</strong>"));
        // "Added KML export" is a child of "NEW & UPDATED:", not a sibling: it sits inside that item's own <ul>
        Assert.That(html, Does.Match(@"NEW &amp; UPDATED:\s*<ul>\s*<li>Added "));
    }

    [Test]
    public void TabIndentedThirdLevelBulletsNestToo()
    {
        string md = "**Build 100 [20200101]**\r\n- top\r\n\t- middle\r\n\t\t- deepest\r\n- next top\r\n";
        string html = ChangelogParser.ToHtmlBody(sections: ChangelogParser.Between(markdown: md, afterBuild: 0,
            upToBuild: 100));

        Assert.That(html, Does.Match(@"top\s*<ul>\s*<li>middle\s*<ul>\s*<li>deepest</li>"));
        Assert.That(html, Does.Match(@"<li>next top</li>"));
    }

    [Test]
    public void MarkupInTheTextIsEscaped_AndOnlyHttpLinksKeepTheirTarget()
    {
        string md = "**Build 100 [20200101]**\n- <script>alert(1)</script> [ok](https://example.com/a?b=1&c=2) " +
                    "[bad](javascript:alert(1)) `<b>` *soft*\n";
        string html = ChangelogParser.ToHtmlBody(sections: ChangelogParser.Between(markdown: md, afterBuild: 0,
            upToBuild: 100));

        Assert.That(html, Does.Not.Contain("<script>"));
        Assert.That(html, Does.Contain("&lt;script&gt;"));
        Assert.That(html, Does.Contain("href=\"https://example.com/a?b=1&amp;c=2\""));
        Assert.That(html, Does.Not.Contain("javascript:"));
        Assert.That(html, Does.Contain("<code>&lt;b&gt;</code>"));
        Assert.That(html, Does.Contain("<em>soft</em>"));
    }

    [Test]
    public void ThePreReleaseEntryIsShownAsTheBuildBeingOffered_OnlyWhenAskedFor()
    {
        // what the tag of a pre-release looks like: the in-progress entry is still unnumbered
        string snapshot = "**Build 9xxx [202xxxxx]** (upcoming release)\r\n- Pre-release feature\r\n" +
                          "**Build 9807 [20261107]**\r\n- Stable thing\r\n";

        var stable = ChangelogParser.Between(markdown: snapshot, afterBuild: 9807, upToBuild: 9809);
        var preRelease = ChangelogParser.Between(markdown: snapshot, afterBuild: 9807, upToBuild: 9809,
            upcomingAsBuild: 9809);

        Assert.That(stable, Is.Empty, message: "a stable user is never shown the unnumbered entry");
        Assert.That(preRelease.Select(selector: s => s.Build), Is.EqualTo(new[] { 9809 }));
        Assert.That(preRelease[index: 0].Heading, Is.EqualTo("Build 9809 (pre-release)"));
        Assert.That(preRelease[index: 0].Body, Does.Contain("Pre-release feature"));
    }

    [Test]
    public void OnlyTheFirstUnnumberedEntryIsTakenAsThePreRelease()
    {
        string snapshot = "**Build 9xxx [202xxxxx]**\r\n- one\r\n**Build 9yyy [x]**\r\n- two\r\n";

        var sections = ChangelogParser.Between(markdown: snapshot, afterBuild: 0, upToBuild: 9809,
            upcomingAsBuild: 9809);

        Assert.That(sections, Has.Count.EqualTo(1));
    }}
