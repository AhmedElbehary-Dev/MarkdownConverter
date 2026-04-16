using Avalonia.Controls;
using MarkdownConverter.Desktop.Services;
using MarkdownConverter.Services;
using MarkdownConverter.ViewModels;

namespace MarkdownConverter.Desktop.UI;

public partial class PdfCompressorWindow : Window
{
    public PdfCompressorWindow()
    {
        InitializeComponent();

        var platformServices = new AvaloniaUiPlatformServices(() => this);
        var compressorService = new PdfCompressorService();
        var viewModel = new PdfCompressorViewModel(compressorService, platformServices);

        DataContext = viewModel;
    }
}
