using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarkdownConverter.Converters;

public sealed class ConversionService
{
    private readonly MarkdownToHtmlRenderer _markdownRenderer;
    private readonly PdfExporter _pdfExporter;
    private readonly DocxExporter _docxExporter;
    private readonly XlsxExporter _xlsxExporter;

    public ConversionService()
        : this(new MarkdownToHtmlRenderer())
    {
    }

    public ConversionService(MarkdownToHtmlRenderer markdownRenderer)
    {
        _markdownRenderer = markdownRenderer ?? throw new ArgumentNullException(nameof(markdownRenderer));
        _pdfExporter = new PdfExporter(_markdownRenderer);
        _docxExporter = new DocxExporter(_markdownRenderer);
        _xlsxExporter = new XlsxExporter();
    }

    public async Task ConvertAsync(string markdownFilePath, string outputPath, string formatValue, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(markdownFilePath))
        {
            throw new ArgumentException("Input markdown path is required.", nameof(markdownFilePath));
        }

        if (!File.Exists(markdownFilePath))
        {
            throw new FileNotFoundException("Markdown file not found.", markdownFilePath);
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new InvalidOperationException("Unable to determine output directory.");
        }

        Directory.CreateDirectory(outputDirectory);

        var markdownText = await File.ReadAllTextAsync(markdownFilePath, cancellationToken).ConfigureAwait(false);
        progress?.Report(5);

        var normalizedFormat = formatValue?.ToLowerInvariant();

        switch (normalizedFormat)
        {
            case "pdf":
                progress?.Report(15);
                await _pdfExporter.ExportAsync(markdownText, outputPath, cancellationToken).ConfigureAwait(false);
                break;
            case "docx":
                progress?.Report(15);
                await _docxExporter.ExportAsync(markdownText, outputPath, cancellationToken).ConfigureAwait(false);
                break;
            case "xlsx":
                progress?.Report(15);
                await _xlsxExporter.ExportAsync(markdownText, outputPath, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new NotSupportedException($"Unsupported output format: {formatValue}");
        }

        progress?.Report(95);
        progress?.Report(100);
    }
}
