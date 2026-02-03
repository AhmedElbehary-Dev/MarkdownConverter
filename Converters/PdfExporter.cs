using DinkToPdf;
using DinkToPdf.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarkdownConverter.Converters;

public sealed class PdfExporter
{
    private static readonly IConverter Converter = new SynchronizedConverter(new PdfTools());
    private readonly MarkdownToHtmlRenderer _htmlRenderer;

    public PdfExporter(MarkdownToHtmlRenderer htmlRenderer)
    {
        _htmlRenderer = htmlRenderer ?? throw new ArgumentNullException(nameof(htmlRenderer));
    }

    public Task ExportAsync(string markdownText, string outputPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var html = _htmlRenderer.Render(markdownText);

        var document = new HtmlToPdfDocument
        {
            GlobalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = DinkToPdf.Orientation.Portrait,
                PaperSize = DinkToPdf.PaperKind.A4,
                DocumentTitle = "Markdown PDF Export",
                Out = outputPath,
                Margins = new MarginSettings
                {
                    Top = 18,
                    Bottom = 18,
                    Left = 16,
                    Right = 16
                },
                DPI = 300
            },
            Objects =
            {
                new ObjectSettings
                {
                    HtmlContent = html,
                    WebSettings = new WebSettings
                    {
                        DefaultEncoding = "utf-8",
                        LoadImages = true,
                        EnableIntelligentShrinking = true,
                        EnableJavascript = false,
                        PrintMediaType = true,
                        Background = true,
                        MinimumFontSize = 10
                    },
                    LoadSettings = new LoadSettings
                    {
                        StopSlowScript = true,
                        BlockLocalFileAccess = false
                    }
                }
            }
        };

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Converter.Convert(document);
        }, cancellationToken);
    }
}
