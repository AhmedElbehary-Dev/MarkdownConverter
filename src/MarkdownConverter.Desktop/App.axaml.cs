using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MarkdownConverter.Converters;
using MarkdownConverter.Platform;
using MarkdownConverter.Desktop.Services;
using MarkdownConverter.ViewModels;
using System;
using System.Text;

namespace MarkdownConverter.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var pdfLoader = new RuntimePdfNativeLibraryLoader();
        try
        {
            // Prefer early failure for missing native PDF runtime, but keep app booting so DOCX/XLSX remain usable.
            pdfLoader.EnsureLoaded();
        }
        catch (Exception)
        {
            // PDF conversion will surface a detailed error on first use if runtime files are missing.
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(CreateMainViewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainViewModel CreateMainViewModel(Func<Avalonia.Controls.Window?> getOwner)
    {
        var services = new AvaloniaUiPlatformServices(getOwner);
        return new MainViewModel(new ConversionService(), services);
    }
}
