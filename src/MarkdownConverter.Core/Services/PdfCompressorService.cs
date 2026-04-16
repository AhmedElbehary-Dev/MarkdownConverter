using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docnet.Core;
using Docnet.Core.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MarkdownConverter.Services;

/// <summary>
/// Compression quality presets that map to Ghostscript's -dPDFSETTINGS values.
/// </summary>
public enum CompressionQuality
{
    /// <summary>72 dpi — smallest file, screen-only quality.</summary>
    Screen,
    /// <summary>150 dpi — good balance of quality and size (recommended).</summary>
    Ebook,
    /// <summary>300 dpi — high quality suitable for printing.</summary>
    Printer,
    /// <summary>300 dpi — highest quality, preserves color profiles.</summary>
    Prepress
}

public sealed class PdfCompressorService
{
    private static readonly string[] GhostscriptCandidates =
    {
        "gswin64c",
        "gswin32c",
        "gs"
    };

    static PdfCompressorService()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Compress a PDF file. Tries Ghostscript first (best results), then falls back
    /// to a render-and-rebuild approach using Docnet + PdfSharp.
    /// Returns the output file path.
    /// </summary>
    public async Task<string> CompressAsync(
        string inputPath,
        string outputPath,
        CompressionQuality quality,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Input PDF not found.", inputPath);

        // Always compress to a temp file first, then move to the final destination.
        // This avoids file-locking issues when output == input (in-place replacement)
        // and ensures a clean atomic write.
        var tempOutputPath = Path.Combine(
            Path.GetTempPath(),
            $"mdc_compressed_{Guid.NewGuid():N}.pdf");

        try
        {
            // Try Ghostscript first – it gives the best compression while preserving text
            var gsException = await TryGhostscriptAsync(inputPath, tempOutputPath, quality, cancellationToken);
            if (gsException == null)
            {
                MoveToFinalDestination(tempOutputPath, outputPath);
                return outputPath;
            }

            // Fallback: Render every page to an image at target DPI, then rebuild the PDF.
            try
            {
                await Task.Run(() => CompressWithRenderAndRebuild(inputPath, tempOutputPath, quality), cancellationToken);
                MoveToFinalDestination(tempOutputPath, outputPath);
                return outputPath;
            }
            catch (Exception renderEx)
            {
                throw new InvalidOperationException(
                    $"PDF compression failed.\n\n" +
                    $"Ghostscript: {gsException.Message}\n\n" +
                    $"Render fallback: {renderEx.Message}\n\n" +
                    $"For best results (preserving text), install Ghostscript and ensure 'gs' or 'gswin64c' is on PATH.",
                    renderEx);
            }
        }
        finally
        {
            TryDeleteFile(tempOutputPath);
        }
    }

    /// <summary>
    /// Moves the temp file to the final destination, overwriting any existing file.
    /// </summary>
    private static void MoveToFinalDestination(string tempPath, string finalPath)
    {
        if (File.Exists(finalPath))
            File.Delete(finalPath);

        File.Move(tempPath, finalPath);
    }

    /// <summary>
    /// Returns null on success, or the exception on failure.
    /// </summary>
    private async Task<Exception?> TryGhostscriptAsync(
        string inputPath,
        string outputPath,
        CompressionQuality quality,
        CancellationToken cancellationToken)
    {
        var gsCommand = GhostscriptCandidates.FirstOrDefault(IsCommandAvailable);
        if (gsCommand == null)
        {
            return new InvalidOperationException(
                "Ghostscript not found on PATH (tried gswin64c, gswin32c, gs).");
        }

        var pdfSettings = quality switch
        {
            CompressionQuality.Screen => "/screen",
            CompressionQuality.Ebook => "/ebook",
            CompressionQuality.Printer => "/printer",
            CompressionQuality.Prepress => "/prepress",
            _ => "/ebook"
        };

        var psi = new ProcessStartInfo
        {
            FileName = gsCommand,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("-sDEVICE=pdfwrite");
        psi.ArgumentList.Add("-dCompatibilityLevel=1.4");
        psi.ArgumentList.Add($"-dPDFSETTINGS={pdfSettings}");
        psi.ArgumentList.Add("-dNOPAUSE");
        psi.ArgumentList.Add("-dQUIET");
        psi.ArgumentList.Add("-dBATCH");
        // Extra optimizations
        psi.ArgumentList.Add("-dDetectDuplicateImages=true");
        psi.ArgumentList.Add("-dCompressFonts=true");
        psi.ArgumentList.Add("-dSubsetFonts=true");
        psi.ArgumentList.Add("-dColorImageDownsampleType=/Bicubic");
        psi.ArgumentList.Add("-dGrayImageDownsampleType=/Bicubic");
        psi.ArgumentList.Add("-dMonoImageDownsampleType=/Subsample");
        psi.ArgumentList.Add($"-sOutputFile={outputPath}");
        psi.ArgumentList.Add(inputPath);

        try
        {
            using var process = new Process { StartInfo = psi };
            process.Start();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                return new InvalidOperationException(
                    $"Ghostscript exited with code {process.ExitCode}. {stderr}".Trim());
            }

            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            {
                return new InvalidOperationException("Ghostscript did not produce an output file.");
            }

            return null; // success
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            return ex;
        }
    }

    /// <summary>
    /// Renders every page of the input PDF to BGRA bitmaps at reduced resolution,
    /// then writes them as BMP temp files and rebuilds a new compressed PDF using PdfSharp.
    /// PdfSharp applies FlateDecode compression to all image streams internally,
    /// and the resolution reduction itself is the main source of file size savings.
    /// </summary>
    private static void CompressWithRenderAndRebuild(
        string inputPath,
        string outputPath,
        CompressionQuality quality)
    {
        var (maxDim, _) = GetRenderDimensions(quality);

        using var docReader = DocLib.Instance.GetDocReader(inputPath, new PageDimensions(maxDim, maxDim));
        int pageCount = docReader.GetPageCount();

        if (pageCount == 0)
            throw new InvalidOperationException("PDF has no pages.");

        using var outputDoc = new PdfDocument();
        var tempFiles = new List<string>();

        try
        {
            for (int i = 0; i < pageCount; i++)
            {
                using var pageReader = docReader.GetPageReader(i);
                int width = pageReader.GetPageWidth();
                int height = pageReader.GetPageHeight();
                byte[] bgraBytes = pageReader.GetImage();

                if (width <= 0 || height <= 0 || bgraBytes.Length == 0)
                    continue;

                // Write BGRA pixels as a temporary BMP file
                string tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"mdc_compress_{Guid.NewGuid():N}.bmp");
                WriteBmpFile(bgraBytes, width, height, tempPath);
                tempFiles.Add(tempPath);

                // Create a PDF page sized to match the rendered aspect ratio
                // Use PDF points (1 point = 1/72 inch)
                var page = outputDoc.AddPage();
                double aspectRatio = (double)width / height;

                if (aspectRatio > 1.0)
                {
                    // Landscape
                    page.Width = XUnit.FromPoint(842);
                    page.Height = XUnit.FromPoint(595);
                }
                else
                {
                    // Portrait
                    page.Width = XUnit.FromPoint(595);
                    page.Height = XUnit.FromPoint(842);
                }

                using var xImage = XImage.FromFile(tempPath);
                using var gfx = XGraphics.FromPdfPage(page);

                // Scale image to fill page while maintaining aspect ratio
                double pageW = page.Width.Point;
                double pageH = page.Height.Point;
                double scale = Math.Min(pageW / width, pageH / height);
                double drawW = width * scale;
                double drawH = height * scale;
                double x = (pageW - drawW) / 2.0;
                double y = (pageH - drawH) / 2.0;

                gfx.DrawImage(xImage, x, y, drawW, drawH);
            }

            outputDoc.Save(outputPath);
        }
        finally
        {
            // Clean up all temp files
            foreach (var f in tempFiles)
                TryDeleteFile(f);
        }
    }

    /// <summary>
    /// Returns (maxDimension, jpegQuality) for each compression quality level.
    /// Docnet renders into a box of maxDim x maxDim, preserving aspect ratio.
    /// </summary>
    private static (int MaxDimension, int JpegQuality) GetRenderDimensions(CompressionQuality quality)
    {
        return quality switch
        {
            CompressionQuality.Screen   => (800, 50),     // ~72 dpi – max compression
            CompressionQuality.Ebook    => (1500, 70),    // ~150 dpi – recommended
            CompressionQuality.Printer  => (2400, 85),    // ~300 dpi – high quality
            CompressionQuality.Prepress => (3000, 95),    // ~300+ dpi – max quality
            _                           => (1500, 70)
        };
    }

    /// <summary>
    /// Writes raw BGRA pixel data as a 32-bit BMP file.
    /// Uses bottom-up row order for maximum compatibility with image loaders.
    /// </summary>
    private static void WriteBmpFile(byte[] bgraPixels, int width, int height, string path)
    {
        int stride = width * 4;
        int pixelDataSize = stride * height;
        int fileSize = 54 + pixelDataSize;

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536);
        using var bw = new BinaryWriter(fs);

        // ── BMP File Header (14 bytes) ──
        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(fileSize);       // total file size
        bw.Write(0);              // reserved
        bw.Write(54);             // offset to pixel data

        // ── BITMAPINFOHEADER (40 bytes) ──
        bw.Write(40);             // header size
        bw.Write(width);
        bw.Write(height);         // positive = bottom-up row order
        bw.Write((short)1);       // color planes
        bw.Write((short)32);      // bits per pixel (BGRA)
        bw.Write(0);              // compression (BI_RGB)
        bw.Write(pixelDataSize);  // image data size
        bw.Write(2835);           // X pixels per meter (~72 dpi)
        bw.Write(2835);           // Y pixels per meter
        bw.Write(0);              // colors in color table
        bw.Write(0);              // important colors

        // ── Pixel data (bottom-up: write last row first) ──
        for (int y = height - 1; y >= 0; y--)
        {
            bw.Write(bgraPixels, y * stride, stride);
        }
    }

    public static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup
        }
    }

    private static bool IsCommandAvailable(string commandName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path)) return false;

        var pathSeparator = OperatingSystem.IsWindows() ? ';' : ':';
        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [string.Empty];

        return path.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(dir => extensions.Any(ext => File.Exists(Path.Join(dir, commandName + ext))));
    }
}
