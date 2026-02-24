using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToOpenXml;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkdownConverter.Converters;

public sealed class DocxExporter
{
    private readonly MarkdownToHtmlRenderer _htmlRenderer;

    public DocxExporter(MarkdownToHtmlRenderer htmlRenderer)
    {
        _htmlRenderer = htmlRenderer ?? throw new ArgumentNullException(nameof(htmlRenderer));
    }

    public Task ExportAsync(string markdownText, string outputPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var html = _htmlRenderer.Render(markdownText);

        using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles();
        stylesPart.Styles.Save();

        var converter = new HtmlConverter(mainPart);
        var paragraphs = converter.Parse(html);
        if (paragraphs != null)
        {
            mainPart.Document.Body ??= new Body();
            foreach (var element in paragraphs)
            {
                mainPart.Document.Body.Append(element);
            }
        }

        mainPart.Document.Save();
        return Task.CompletedTask;
    }
}
