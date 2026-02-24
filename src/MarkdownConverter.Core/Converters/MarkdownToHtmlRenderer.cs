using Markdig;
using Markdig.Extensions.AutoIdentifiers;

namespace MarkdownConverter.Converters;

public sealed class MarkdownToHtmlRenderer
{
    private static readonly string EmbeddedCss = @"
@page {
    size: A4;
    margin: 18mm;
}
* {
    box-sizing: border-box;
}
body {
    font-family: 'Segoe UI', 'Inter', system-ui, -apple-system, sans-serif;
    font-size: 14px;
    line-height: 1.6;
    color: #0f172a;
    background: #f8fafc;
    margin: 0;
    padding: 32px;
}
.markdown-body {
    max-width: 960px;
    margin: 0 auto;
}
h1, h2, h3 {
    font-weight: 700;
    margin: 1.5rem 0 0.75rem;
    line-height: 1.25;
}
h1 {
    font-size: 2.4rem;
}
h2 {
    font-size: 1.9rem;
}
h3 {
    font-size: 1.5rem;
}
p, li {
    margin: 0 0 0.75rem;
}
ul, ol {
    margin: 0 0 0.75rem;
    padding-left: 1.5rem;
}
ul ul, ol ul, ul ol, ol ol {
    padding-left: 1rem;
}
strong {
    font-weight: 700;
}
em {
    font-style: italic;
}
del {
    text-decoration: line-through;
    color: #8b5cf6;
}
code {
    font-family: 'JetBrains Mono', 'Consolas', 'SFMono-Regular', monospace;
    background: #eef2ff;
    padding: 0.2rem 0.4rem;
    border-radius: 4px;
    font-size: 0.95em;
}
pre {
    background: #0f172a;
    color: #e2e8f0;
    padding: 1rem 1.25rem;
    border-radius: 10px;
    border: 1px solid #111827;
    line-height: 1.4;
    white-space: pre-wrap;
    overflow-wrap: anywhere;
    word-break: break-word;
    overflow: visible;
    margin: 1rem 0;
}
pre code {
    background: none;
    color: inherit;
    padding: 0;
    white-space: inherit;
    overflow-wrap: inherit;
    word-break: inherit;
}
blockquote {
    border-left: 4px solid #cbd5f5;
    padding-left: 1rem;
    color: #475467;
    background: #eef2ff;
    margin: 1.25rem 0;
}
table {
    border-collapse: collapse;
    width: 100%;
    margin: 1.25rem 0 1.5rem;
    page-break-inside: avoid;
}
thead tr {
    background: #0f172a;
    color: #f8fafc;
}
th, td {
    border: 1px solid #d0d7de;
    padding: 0.65rem 0.85rem;
    vertical-align: top;
}
tbody tr:nth-child(even) {
    background: #f5f7fb;
}
a {
    color: #2563eb;
    text-decoration: none;
}
a:hover {
    text-decoration: underline;
}
img {
    max-width: 100%;
    height: auto;
    display: block;
    margin: 1rem 0;
}
@media print {
    body {
        background: #fff;
    }
    .markdown-body {
        padding: 0;
    }
    table, pre, blockquote {
        page-break-inside: avoid;
    }
}
";

    private readonly MarkdownPipeline _pipeline;

    public MarkdownToHtmlRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoLinks()
            .UsePipeTables()
            .UseGridTables()
            .UseTaskLists()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .UseEmojiAndSmiley()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();
    }

    public string Render(string markdownText)
    {
        var htmlBody = string.IsNullOrWhiteSpace(markdownText)
            ? string.Empty
            : Markdown.ToHtml(markdownText, _pipeline).Trim();

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"" />
  <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>Markdown Export</title>
  <style>{EmbeddedCss}</style>
</head>
<body>
  <div class=""markdown-body"">
    {htmlBody}
  </div>
</body>
</html>";
    }
}
