using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace GeoTagNinja.Helpers.Update;

/// <summary>
///     One released build's entry in <c>changelog.md</c>.
/// </summary>
internal sealed class ChangelogSection
{
    internal ChangelogSection(int build,
                              string heading,
                              string body)
    {
        Build = build;
        Heading = heading;
        Body = body;
    }

    /// <summary>
    ///     The build the entry is for. When the heading names several (<c>Build 9519 [..] // Build 9520 [..]</c>) it is
    ///     the highest, which is the one that decides whether the entry has been released yet.
    /// </summary>
    internal int Build { get; }

    /// <summary>The heading line, without the surrounding <c>**</c>.</summary>
    internal string Heading { get; }

    /// <summary>The entry's lines under the heading (Markdown, with its original line breaks).</summary>
    internal string Body { get; }
}

/// <summary>
///     Reads <c>changelog.md</c> and turns the part the user should see into HTML. No files, web or UI in here, so it
///     can be tested.
/// </summary>
/// <remarks>
///     The file starts with the entry for the <i>upcoming</i> release, headed <c>**Build 9xxx [202xxxxx]**</c>. That
///     is not a number, so it never counts as a build and is never shown. Everything else is selected purely by
///     build number, so an entry is shown only when its build has actually been released (and is newer than what the
///     user has). The file is split into entries by that heading (it is not a Markdown heading, so a Markdown parser
///     can't do it), and each entry's body is rendered with Markdig. Raw HTML in the text is escaped rather than
///     passed through, and only <c>http(s)</c> links are kept.
/// </remarks>
internal static class ChangelogParser
{
    private static readonly Regex HeadingLine = new(pattern: @"^\*\*Build\s", options: RegexOptions.Compiled);

    // "Build 9xxx" must not match: after the 9 there is no word boundary because "x" is a word character.
    private static readonly Regex BuildNumber = new(pattern: @"Build\s+(\d+)\b", options: RegexOptions.Compiled);

    /// <summary>
    ///     CommonMark plus nothing else. <c>DisableHtml</c> makes any raw HTML in the changelog show as text instead of
    ///     being rendered.
    /// </summary>
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().DisableHtml().Build();

    /// <summary>
    ///     Splits the changelog into its build entries, newest first as in the file. Entries without a real build
    ///     number (the upcoming one) are left out, unless <paramref name="upcomingAsBuild" /> says what it is.
    /// </summary>
    /// <param name="markdown">The changelog.</param>
    /// <param name="upcomingAsBuild">
    ///     For users who take pre-releases: the build the unnumbered upcoming entry stands for (the pre-release they are
    ///     being offered, or are running). In a snapshot taken at a pre-release that entry <i>is</i> the pre-release.
    ///     0 (everyone else) leaves the upcoming entry out.
    /// </param>
    internal static List<ChangelogSection> Parse(string markdown,
                                                 int upcomingAsBuild = 0)
    {
        List<ChangelogSection> sections = [];
        if (string.IsNullOrWhiteSpace(value: markdown))
        {
            return sections;
        }

        string[] lines = markdown.Replace(oldValue: "\r\n", newValue: "\n").Split(separator: '\n');
        string heading = null;
        int build = 0;
        StringBuilder body = new();

        void Flush()
        {
            if (heading != null && build > 0)
            {
                sections.Add(item: new ChangelogSection(build: build, heading: heading,
                    body: body.ToString().TrimEnd()));
            }

            heading = null;
            build = 0;
            _ = body.Clear();
        }

        foreach (string line in lines)
        {
            if (HeadingLine.IsMatch(input: line))
            {
                Flush();
                heading = line.Replace(oldValue: "**", newValue: "").Trim();
                build = BuildNumber.Matches(input: line)
                                   .Cast<Match>()
                                   .Select(selector: m => int.TryParse(s: m.Groups[groupnum: 1].Value,
                                                              style: NumberStyles.None,
                                                              provider: CultureInfo.InvariantCulture,
                                                              result: out int n)
                                                              ? n
                                                              : 0)
                                   .DefaultIfEmpty(defaultValue: 0)
                                   .Max();

                if (build == 0 && upcomingAsBuild > 0)
                {
                    // the one unnumbered entry; only ever one, at the top
                    build = upcomingAsBuild;
                    heading = $"Build {upcomingAsBuild.ToString(provider: CultureInfo.InvariantCulture)} (pre-release)";
                    upcomingAsBuild = 0;
                }
            }
            else if (heading != null)
            {
                _ = body.AppendLine(value: line);
            }
        }

        Flush();
        return sections;
    }

    /// <summary>
    ///     The entries newer than <paramref name="afterBuild" /> and no newer than <paramref name="upToBuild" />.
    /// </summary>
    /// <param name="markdown">The changelog.</param>
    /// <param name="afterBuild">The build the user has (or last saw notes for): entries up to it are known.</param>
    /// <param name="upToBuild">The newest build that is out (or running): later entries are not released.</param>
    /// <param name="upcomingAsBuild">See <see cref="Parse" />: 0 unless the user takes pre-releases.</param>
    internal static List<ChangelogSection> Between(string markdown,
                                                   int afterBuild,
                                                   int upToBuild,
                                                   int upcomingAsBuild = 0)
    {
        return Parse(markdown: markdown, upcomingAsBuild: upcomingAsBuild)
              .Where(predicate: s => s.Build > afterBuild && s.Build <= upToBuild)
              .OrderByDescending(keySelector: s => s.Build)
              .ToList();
    }

    /// <summary>
    ///     The HTML body content (no <c>html</c>/<c>body</c> wrapper) for the entries.
    /// </summary>
    internal static string ToHtmlBody(IEnumerable<ChangelogSection> sections)
    {
        StringBuilder html = new();
        foreach (ChangelogSection section in sections)
        {
            _ = html.Append(value: "<h2>").Append(value: WebUtility.HtmlEncode(value: section.Heading))
                    .AppendLine(value: "</h2>");
            _ = html.AppendLine(value: RenderMarkdown(markdown: section.Body));
        }

        return html.ToString();
    }

    /// <summary>
    ///     A complete page. Scripts are not needed and are disabled by the viewer; the colours follow the app's theme.
    /// </summary>
    internal static string ToDocument(IEnumerable<ChangelogSection> sections,
                                      bool dark)
    {
        string back = dark ? "#202020" : "#ffffff";
        string fore = dark ? "#e6e6e6" : "#1a1a1a";
        string codeBack = dark ? "#333333" : "#f0f0f0";
        string link = dark ? "#6cb6ff" : "#0b5cad";
        string rule = dark ? "#444444" : "#d8d8d8";

        return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>" +
               $"body{{background:{back};color:{fore};font:14px 'Segoe UI',sans-serif;margin:12px 18px;line-height:1.4}}" +
               $"h2{{font-size:16px;margin:20px 0 6px;padding-bottom:4px;border-bottom:1px solid {rule}}}" +
               "h2:first-child{margin-top:0}" +
               "ul{margin:2px 0;padding-left:22px}li{margin:3px 0}li>p{margin:0}p{margin:6px 0}" +
               $"code{{background:{codeBack};padding:0 4px;border-radius:3px;font-family:Consolas,monospace;font-size:13px}}" +
               $"pre{{background:{codeBack};padding:8px;overflow:auto}}pre code{{padding:0}}" +
               $"a{{color:{link}}}" +
               "</style></head><body>" +
               ToHtmlBody(sections: sections) +
               "</body></html>";
    }

    /// <summary>
    ///     The same entries as plain text, for when the HTML viewer is not available.
    /// </summary>
    internal static string ToPlainText(IEnumerable<ChangelogSection> sections)
    {
        StringBuilder text = new();
        foreach (ChangelogSection section in sections)
        {
            _ = text.AppendLine(value: section.Heading)
                    .AppendLine(value: new string(c: '-', count: Math.Min(val1: section.Heading.Length, val2: 60)))
                    .AppendLine(value: PlainBullets(body: section.Body.Replace(oldValue: "**", newValue: "").Replace(oldValue: "`", newValue: "")))
                    .AppendLine();
        }

        return text.ToString().Replace(oldValue: "\r\n", newValue: "\n").Replace(oldValue: "\n", newValue: Environment.NewLine);
    }

    /// <summary>One level of tab (or four spaces) becomes four spaces, and the dash becomes a dot.</summary>
    private static string PlainBullets(string body)
    {
        return Regex.Replace(input: body, pattern: @"^(?<indent>[ \t]*)- ", options: RegexOptions.Multiline,
            evaluator: m => new string(c: ' ', count: 4 * (m.Groups[groupname: "indent"].Value.Count(predicate: c => c == '\t') +
                                                         m.Groups[groupname: "indent"].Value.Count(predicate: c => c == ' ') / 4)) + "• ");
    }

    /// <summary>
    ///     Markdown to HTML with Markdig. Links other than <c>http(s)</c> (<c>javascript:</c> and the like) are
    ///     replaced by an empty target; the viewer also refuses them, this is belt and braces.
    /// </summary>
    private static string RenderMarkdown(string markdown)
    {
        using StringWriter writer = new();
        HtmlRenderer renderer = new(writer: writer)
        {
            LinkRewriter = url => Uri.TryCreate(uriString: url, uriKind: UriKind.Absolute, result: out Uri parsed) &&
                                  (parsed.Scheme == Uri.UriSchemeHttps || parsed.Scheme == Uri.UriSchemeHttp)
                ? url
                : "#"
        };
        Pipeline.Setup(renderer: renderer);
        MarkdownDocument document = Markdown.Parse(markdown: markdown, pipeline: Pipeline);
        _ = renderer.Render(markdownObject: document);
        writer.Flush();
        return writer.ToString();
    }
}
