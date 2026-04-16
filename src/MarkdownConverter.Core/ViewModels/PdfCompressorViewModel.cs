using System;
using System.IO;
using System.Threading.Tasks;
using MarkdownConverter.Services;
using MarkdownConverter.Platform;

namespace MarkdownConverter.ViewModels;

public class PdfCompressorViewModel : ViewModelBase
{
    private readonly PdfCompressorService _compressorService;
    private readonly IUiPlatformServices _platformServices;

    private string _inputFilePath = string.Empty;
    private string _statusText = "Select a PDF file to compress.";
    private bool _isCompressing;
    private string _originalSizeText = string.Empty;
    private string _compressedSizeText = string.Empty;
    private string _reductionText = string.Empty;
    private bool _hasResult;
    private int _selectedQualityIndex = 1; // default to Ebook

    public string InputFilePath
    {
        get => _inputFilePath;
        set
        {
            if (SetProperty(ref _inputFilePath, value))
            {
                OnPropertyChanged(nameof(HasFile));
                CompressCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasFile => !string.IsNullOrWhiteSpace(InputFilePath);

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsCompressing
    {
        get => _isCompressing;
        set
        {
            if (SetProperty(ref _isCompressing, value))
            {
                BrowsePdfCommand.RaiseCanExecuteChanged();
                CompressCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string OriginalSizeText
    {
        get => _originalSizeText;
        set => SetProperty(ref _originalSizeText, value);
    }

    public string CompressedSizeText
    {
        get => _compressedSizeText;
        set => SetProperty(ref _compressedSizeText, value);
    }

    public string ReductionText
    {
        get => _reductionText;
        set => SetProperty(ref _reductionText, value);
    }

    public bool HasResult
    {
        get => _hasResult;
        set => SetProperty(ref _hasResult, value);
    }

    public int SelectedQualityIndex
    {
        get => _selectedQualityIndex;
        set => SetProperty(ref _selectedQualityIndex, value);
    }

    public string[] QualityLabels { get; } =
    [
        "Screen (72 dpi) — Smallest file",
        "Ebook (150 dpi) — Recommended",
        "Printer (300 dpi) — High quality",
        "Prepress (300 dpi) — Maximum quality"
    ];

    public AsyncRelayCommand BrowsePdfCommand { get; }
    public AsyncRelayCommand CompressCommand { get; }
    public AsyncRelayCommand ReplaceOriginalCommand { get; }

    public PdfCompressorViewModel(PdfCompressorService compressorService, IUiPlatformServices platformServices)
    {
        _compressorService = compressorService;
        _platformServices = platformServices;

        BrowsePdfCommand = new AsyncRelayCommand(BrowsePdfAsync, () => !IsCompressing);
        CompressCommand = new AsyncRelayCommand(CompressSaveAsAsync, () => !IsCompressing && HasFile);
        ReplaceOriginalCommand = new AsyncRelayCommand(ReplaceOriginalAsync, () => !IsCompressing && HasFile);
    }

    private async Task BrowsePdfAsync()
    {
        var result = await _platformServices.PickPdfFileAsync();
        if (string.IsNullOrEmpty(result)) return;

        InputFilePath = result;
        HasResult = false;
        CompressedSizeText = string.Empty;
        ReductionText = string.Empty;

        var originalSize = new FileInfo(result).Length;
        OriginalSizeText = PdfCompressorService.FormatFileSize(originalSize);
        StatusText = $"Loaded: {Path.GetFileName(result)} ({OriginalSizeText})";
    }

    private async Task CompressSaveAsAsync()
    {
        if (!File.Exists(InputFilePath)) return;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(InputFilePath);
        var suggestedName = $"{nameWithoutExt}_compressed";

        var outputPath = await _platformServices.SavePdfFileAsync(suggestedName);
        if (string.IsNullOrEmpty(outputPath)) return;

        await RunCompressionAsync(outputPath);
    }

    private async Task ReplaceOriginalAsync()
    {
        if (!File.Exists(InputFilePath)) return;

        var confirm = await _platformServices.ShowConfirmAsync(
            "Replace Original",
            $"This will overwrite the original file:\n{Path.GetFileName(InputFilePath)}\n\nA backup will be created before replacing. Continue?");

        if (!confirm) return;

        // Create a backup copy before replacing
        var backupPath = InputFilePath + ".bak";
        File.Copy(InputFilePath, backupPath, overwrite: true);

        try
        {
            await RunCompressionAsync(InputFilePath);

            StatusText += $" Backup saved as: {Path.GetFileName(backupPath)}";
        }
        catch
        {
            // Restore from backup if compression failed
            if (File.Exists(backupPath) && !File.Exists(InputFilePath))
            {
                File.Move(backupPath, InputFilePath);
            }
            throw;
        }
    }

    private async Task RunCompressionAsync(string outputPath)
    {
        var quality = (CompressionQuality)SelectedQualityIndex;

        IsCompressing = true;
        HasResult = false;
        StatusText = $"Compressing with {QualityLabels[SelectedQualityIndex]}...";

        var originalSize = new FileInfo(InputFilePath).Length;

        try
        {
            await _compressorService.CompressAsync(InputFilePath, outputPath, quality);

            var compressedSize = new FileInfo(outputPath).Length;
            CompressedSizeText = PdfCompressorService.FormatFileSize(compressedSize);

            if (compressedSize < originalSize)
            {
                double reductionPercent = (1.0 - (double)compressedSize / originalSize) * 100;
                ReductionText = $"↓ {reductionPercent:F1}% smaller";
                StatusText = $"Compressed successfully! Saved {PdfCompressorService.FormatFileSize(originalSize - compressedSize)}.";
            }
            else
            {
                ReductionText = "No reduction (PDF already optimized)";
                StatusText = "Compression complete. File was already optimized — no size reduction.";
            }

            HasResult = true;
            await _platformServices.ShowMessageAsync(
                $"PDF compressed successfully!\n\n" +
                $"Original: {OriginalSizeText}\n" +
                $"Compressed: {CompressedSizeText}\n" +
                $"Reduction: {ReductionText}\n\n" +
                $"Saved to: {outputPath}",
                ToastKind.Success);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            StatusText = $"Compression failed: {ex.Message}";
            await _platformServices.ShowMessageAsync(
                $"Compression failed:\n{ex.Message}",
                ToastKind.Error);
        }
        finally
        {
            IsCompressing = false;
        }
    }
}
