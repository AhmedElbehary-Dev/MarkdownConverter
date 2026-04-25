using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MarkdownConverter.Desktop.Platform;

/// <summary>
/// Manages the Windows auto-start registry entry so the app can
/// launch at login and sit in the system tray.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowsStartupManager
{
    private const string AppName = "MarkdownConverterPro";
    private const string RegistryRunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Adds or removes the app from Windows startup (current user).
    /// </summary>
    public static void SetStartWithWindows(bool enable)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        using var key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, writable: true);
        if (key == null) return;

        if (enable)
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                // --minimized flag tells the app to start hidden in the system tray
                key.SetValue(AppName, $"\"{exePath}\" --minimized");
            }
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }

    /// <summary>
    /// Checks if the app is currently set to start with Windows.
    /// </summary>
    public static bool IsStartWithWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        using var key = Registry.CurrentUser.OpenSubKey(RegistryRunPath);
        return key?.GetValue(AppName) != null;
    }
}
