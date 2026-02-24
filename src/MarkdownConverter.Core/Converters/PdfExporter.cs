using DinkToPdf;
using DinkToPdf.Contracts;
using MarkdownConverter.Platform;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkdownConverter.Converters;

public sealed class PdfExporter
{
    private static readonly IConverter Converter = new SynchronizedConverter(new PdfTools());
    private static readonly string[] ChromiumCommandCandidates =
    {
        "google-chrome",
        "google-chrome-stable",
        "chromium-browser",
        "chromium",
        "msedge",
        "microsoft-edge"
    };

    private readonly MarkdownToHtmlRenderer _htmlRenderer;
    private readonly IPdfNativeLibraryLoader _pdfNativeLibraryLoader;

    public PdfExporter(MarkdownToHtmlRenderer htmlRenderer)
        : this(htmlRenderer, new RuntimePdfNativeLibraryLoader())
    {
    }

    public PdfExporter(MarkdownToHtmlRenderer htmlRenderer, IPdfNativeLibraryLoader pdfNativeLibraryLoader)
    {
        _htmlRenderer = htmlRenderer ?? throw new ArgumentNullException(nameof(htmlRenderer));
        _pdfNativeLibraryLoader = pdfNativeLibraryLoader ?? throw new ArgumentNullException(nameof(pdfNativeLibraryLoader));
    }

    public Task ExportAsync(string markdownText, string outputPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var html = _htmlRenderer.Render(markdownText);

        return Task.Run(async () =>
        {
            await ExportWithFallbacksAsync(html, outputPath, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    private async Task ExportWithFallbacksAsync(string html, string outputPath, CancellationToken cancellationToken)
    {
        Exception? dinkFailure = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExportWithDinkToPdf(html, outputPath);
            return;
        }
        catch (Exception ex) when (IsDinkToPdfLoadOrRuntimeIssue(ex))
        {
            dinkFailure = ex;
        }

        Exception? chromiumFailure = null;
        try
        {
            await ExportWithChromiumCliAsync(html, outputPath, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or FileNotFoundException)
        {
            chromiumFailure = ex;
        }

        Exception? wkhtmlFailure = null;
        try
        {
            await ExportWithWkhtmltopdfCliAsync(html, outputPath, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or FileNotFoundException)
        {
            wkhtmlFailure = ex;
        }

        throw BuildCombinedPdfFailure(dinkFailure, chromiumFailure, wkhtmlFailure);
    }

    private void ExportWithDinkToPdf(string html, string outputPath)
    {
        _pdfNativeLibraryLoader.EnsureLoaded();

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

        Converter.Convert(document);
    }

    private static bool IsDinkToPdfLoadOrRuntimeIssue(Exception ex)
    {
        if (ex is AggregateException aggregate)
        {
            foreach (var inner in aggregate.Flatten().InnerExceptions)
            {
                if (IsDinkToPdfLoadOrRuntimeIssue(inner))
                {
                    return true;
                }
            }
        }

        if (ex is DllNotFoundException dllEx)
        {
            return dllEx.Message.Contains("wkhtmltox", StringComparison.OrdinalIgnoreCase);
        }

        if (ex is TypeInitializationException typeInit && typeInit.InnerException != null)
        {
            return IsDinkToPdfLoadOrRuntimeIssue(typeInit.InnerException);
        }

        return ex.Message.Contains("wkhtmltox", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("libwkhtmltox", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ExportWithChromiumCliAsync(string html, string outputPath, CancellationToken cancellationToken)
    {
        var tempHtmlPath = Path.Combine(Path.GetTempPath(), $"mdc-{Guid.NewGuid():N}.html");
        try
        {
            await File.WriteAllTextAsync(tempHtmlPath, html, cancellationToken).ConfigureAwait(false);

            var browserCommand = ChromiumCommandCandidates.FirstOrDefault(IsCommandAvailable);
            if (string.IsNullOrWhiteSpace(browserCommand))
            {
                throw new InvalidOperationException(
                    "No Chromium-based browser CLI found (tried google-chrome/chromium/msedge on PATH).");
            }

            var inputUrl = new Uri(tempHtmlPath).AbsoluteUri;
            var psi = new ProcessStartInfo
            {
                FileName = browserCommand,
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            psi.ArgumentList.Add("--headless");
            psi.ArgumentList.Add("--disable-gpu");
            psi.ArgumentList.Add("--disable-dev-shm-usage");
            psi.ArgumentList.Add("--disable-crash-reporter");
            psi.ArgumentList.Add("--disable-breakpad");
            psi.ArgumentList.Add("--no-first-run");
            psi.ArgumentList.Add("--no-default-browser-check");
            psi.ArgumentList.Add("--allow-file-access-from-files");
            psi.ArgumentList.Add("--disable-features=Translate");
            psi.ArgumentList.Add("--print-to-pdf-no-header");
            psi.ArgumentList.Add($"--print-to-pdf={outputPath}");
            psi.ArgumentList.Add(inputUrl);

            if (OperatingSystem.IsLinux())
            {
                psi.ArgumentList.Add("--no-sandbox");
            }

            await RunCliAndEnsureSuccessAsync(
                psi,
                cancellationToken,
                "Chromium headless PDF export failed").ConfigureAwait(false);

            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            {
                throw new InvalidOperationException("Chromium headless did not produce a PDF file.");
            }
        }
        finally
        {
            TryDeleteFile(tempHtmlPath);
        }
    }

    private static async Task ExportWithWkhtmltopdfCliAsync(string html, string outputPath, CancellationToken cancellationToken)
    {
        var tempHtmlPath = Path.Combine(Path.GetTempPath(), $"mdc-{Guid.NewGuid():N}.html");
        try
        {
            await File.WriteAllTextAsync(tempHtmlPath, html, cancellationToken).ConfigureAwait(false);

            using var process = new Process
            {
                StartInfo = BuildWkhtmltopdfStartInfo(tempHtmlPath, outputPath)
            };
            await RunCliAndEnsureSuccessAsync(
                process.StartInfo,
                cancellationToken,
                "wkhtmltopdf CLI failed").ConfigureAwait(false);
        }
        finally
        {
            TryDeleteFile(tempHtmlPath);
        }
    }

    private static ProcessStartInfo BuildWkhtmltopdfStartInfo(string inputHtmlPath, string outputPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "wkhtmltopdf",
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("--quiet");
        psi.ArgumentList.Add("--encoding");
        psi.ArgumentList.Add("utf-8");
        psi.ArgumentList.Add("--disable-javascript");
        psi.ArgumentList.Add("--enable-local-file-access");
        psi.ArgumentList.Add("--dpi");
        psi.ArgumentList.Add("300");
        psi.ArgumentList.Add("--page-size");
        psi.ArgumentList.Add("A4");
        psi.ArgumentList.Add("--margin-top");
        psi.ArgumentList.Add("18");
        psi.ArgumentList.Add("--margin-bottom");
        psi.ArgumentList.Add("18");
        psi.ArgumentList.Add("--margin-left");
        psi.ArgumentList.Add("16");
        psi.ArgumentList.Add("--margin-right");
        psi.ArgumentList.Add("16");
        psi.ArgumentList.Add(inputHtmlPath);
        psi.ArgumentList.Add(outputPath);

        return psi;
    }

    private static async Task RunCliAndEnsureSuccessAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken,
        string failurePrefix)
    {
        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception startEx) when (startEx is Win32Exception or FileNotFoundException)
        {
            throw new InvalidOperationException(failurePrefix, startEx);
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode == 0)
        {
            return;
        }

        var stderr = process.StartInfo.RedirectStandardError
            ? await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false)
            : string.Empty;

        throw new InvalidOperationException(
            $"{failurePrefix} with exit code {process.ExitCode}.{Environment.NewLine}{stderr}".Trim());
    }

    private static bool IsCommandAvailable(string commandName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var pathSeparator = OperatingSystem.IsWindows() ? ';' : ':';
        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [string.Empty];

        foreach (var dir in path.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(dir, commandName + extension);
                if (File.Exists(candidate))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static Exception BuildCombinedPdfFailure(Exception? dinkFailure, Exception? chromiumFailure, Exception? wkhtmlFailure)
    {
        var message = new StringBuilder()
            .AppendLine("PDF conversion failed. Tried HTML-to-PDF backends in order:")
            .AppendLine("1. DinkToPdf (libwkhtmltox native library)")
            .AppendLine("2. Chromium/Chrome headless CLI")
            .AppendLine("3. wkhtmltopdf CLI")
            .AppendLine()
            .AppendLine("Install one supported backend (recommended: Chrome/Chromium already on PATH, or wkhtmltopdf), or provide libwkhtmltox runtime files under runtimes/<rid>/native.")
            .ToString()
            .TrimEnd();

        var failures = new Exception?[] { dinkFailure, chromiumFailure, wkhtmlFailure };
        var inner = new AggregateException(failures.Where(e => e is not null).Cast<Exception>());

        return new InvalidOperationException(message, inner);
    }
}
