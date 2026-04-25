using Avalonia;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MarkdownConverter.Desktop;

public static class Program
{
    /// <summary>
    /// When true, the app was launched with --minimized (e.g. at Windows startup)
    /// and should start hidden in the system tray.
    /// </summary>
    public static bool StartMinimized { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        if (IsHeadlessLinuxSession())
        {
            Console.WriteLine("No X display detected (DISPLAY is not set). Skipping GUI launch in headless environment.");
            return;
        }

        // Check if the app should start minimized to the system tray
        StartMinimized = args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) when (IsDisplayStartupFailure(ex))
        {
            Console.WriteLine($"GUI launch skipped: {ex.Message}");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    private static bool IsHeadlessLinuxSession()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"))
            && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
    }

    private static bool IsDisplayStartupFailure(Exception ex)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            && ex.Message.Contains("XOpenDisplay failed", StringComparison.OrdinalIgnoreCase);
    }
}

