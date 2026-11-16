using GeoTagNinja.Helpers.Update;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GeoTagNinja.Helpers.Help;

/// <summary>One page of the in-app Help.</summary>
internal sealed class HelpTopic
{
    internal HelpTopic(string id,
                       string title,
                       string body)
    {
        Id = id;
        Title = title;
        Body = body;
    }

    /// <summary>The stable key callers use to open the Help at this topic (the <c>{#id}</c> in the heading).</summary>
    internal string Id { get; }

    /// <summary>What the topic list shows.</summary>
    internal string Title { get; }

    /// <summary>The topic's Markdown.</summary>
    internal string Body { get; }

    /// <summary>The title: it is what a list box shows for the topic.</summary>
    public override string ToString()
    {
        return Title;
    }
}

/// <summary>
///     The text of the in-app Help (F1). It lives in <c>help.md</c>, next to this file, embedded in the assembly: one
///     <c>## Title {#id}</c> section per topic. Edit that file, not this one, to change the Help.
/// </summary>
/// <remarks>
///     The text is English and lives outside <c>Strings.resx</c> on purpose, as the update changelog does: a build can go
///     out before translators catch up, and prose this long is hard to translate piecemeal.
/// </remarks>
internal static class HelpContent
{
    /// <summary>The topic that opens when the caller has no better one.</summary>
    internal const string DefaultTopicId = "getting-started";

    private const string ResourceName = "GeoTagNinja.Helpers.Help.help.md";

    private static readonly Regex HeadingRegex = new(pattern: @"^##\s+(?<title>.*?)\s*\{#(?<id>[A-Za-z0-9\-]+)\}\s*$",
        options: RegexOptions.Compiled);

    private static IReadOnlyList<HelpTopic> _topics;

    /// <summary>All topics, in the order of the file.</summary>
    internal static IReadOnlyList<HelpTopic> Topics => _topics ??= Parse(markdown: LoadMarkdown());

    /// <summary>The topic with this id, or the default topic if there is none (so a stale id never shows nothing).</summary>
    internal static HelpTopic Find(string id)
    {
        IReadOnlyList<HelpTopic> topics = Topics;
        foreach (HelpTopic topic in topics)
        {
            if (string.Equals(a: topic.Id, b: id, comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                return topic;
            }
        }

        foreach (HelpTopic topic in topics)
        {
            if (topic.Id == DefaultTopicId)
            {
                return topic;
            }
        }

        return topics.Count > 0
            ? topics[index: 0]
            : new HelpTopic(id: DefaultTopicId, title: "Help", body: "The Help text could not be loaded.");
    }

    /// <summary>Splits the Help Markdown into topics at each <c>## Title {#id}</c> line. Text before the first is ignored.</summary>
    internal static IReadOnlyList<HelpTopic> Parse(string markdown)
    {
        List<HelpTopic> topics = [];
        string id = null;
        string title = null;
        StringBuilder body = new();

        void Flush()
        {
            if (id != null)
            {
                topics.Add(item: new HelpTopic(id: id, title: title, body: body.ToString().Trim()));
            }

            body.Clear();
        }

        foreach (string line in (markdown ?? "").Replace(oldValue: "\r\n", newValue: "\n").Split(separator: '\n'))
        {
            Match heading = HeadingRegex.Match(input: line);
            if (heading.Success)
            {
                Flush();
                id = heading.Groups[groupname: "id"].Value;
                title = heading.Groups[groupname: "title"].Value;
            }
            else if (id != null)
            {
                _ = body.AppendLine(value: line);
            }
        }

        Flush();
        return topics;
    }

    /// <summary>A complete page for the topic, in the app's light or dark colours.</summary>
    internal static string ToDocument(HelpTopic topic,
                                      bool dark)
    {
        return ChangelogParser.ToDocument(
            sections: [new ChangelogSection(build: 0, heading: topic.Title, body: topic.Body)],
            dark: dark);
    }

    /// <summary>The topic as plain text, for when the HTML viewer is not available.</summary>
    internal static string ToPlainText(HelpTopic topic)
    {
        return ChangelogParser.ToPlainText(
            sections: [new ChangelogSection(build: 0, heading: topic.Title, body: topic.Body)]);
    }

    private static string LoadMarkdown()
    {
        using Stream stream = typeof(HelpContent).Assembly.GetManifestResourceStream(name: ResourceName);
        if (stream == null)
        {
            return "";
        }

        using StreamReader reader = new(stream: stream, encoding: Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
