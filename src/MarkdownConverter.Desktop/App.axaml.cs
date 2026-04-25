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
    private TrayIconViewModel? _trayViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Set DataContext for TrayIcon command bindings declared in App.axaml
        _trayViewModel = new TrayIconViewModel();
        DataContext = _trayViewModel;
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
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or InvalidOperationException)
        {
            // PDF conversion will surface a detailed error on first use if runtime files are missing.
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(CreateMainViewModel);

            // Keep the app running when the window is "closed" (it hides to tray instead)
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainViewModel CreateMainViewModel(Func<Avalonia.Controls.Window?> getOwner)
    {
        var services = new AvaloniaUiPlatformServices(getOwner);
        return new MainViewModel(new ConversionService(), services);
    }
}
