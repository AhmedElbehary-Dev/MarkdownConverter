using Avalonia;
using System;
using System.Runtime.InteropServices;

namespace MarkdownConverter.Desktop;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (IsHeadlessLinuxSession())
        {
            Console.WriteLine("No X display detected (DISPLAY is not set). Skipping GUI launch in headless environment.");
            return;
        }

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
