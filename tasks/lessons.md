# Lessons Learned

## 1. Avalonia / Desktop App Packaging
- **Pattern:** Launching an Avalonia or WPF UI application opens a PowerShell/Console window in the background alongside the main GUI.
- **Root Cause:** The `OutputType` in the main executable `.csproj` was set to `Exe` instead of `WinExe`. `Exe` tells the Windows OS to allocate a console subsystem, whereas `WinExe` uses the Windows GUI subsystem.
- **Rule:** Always ensure that desktop applications (Avalonia, WPF, Windows Forms) have `<OutputType>WinExe</OutputType>` in their main `.csproj` to prevent unwanted command prompt windows from opening alongside the GUI.
