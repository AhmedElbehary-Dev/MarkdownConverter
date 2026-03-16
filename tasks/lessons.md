# Lessons Learned

## 1. Avalonia / Desktop App Packaging
- **Pattern:** Launching an Avalonia or WPF UI application opens a PowerShell/Console window in the background alongside the main GUI.
- **Root Cause:** The `OutputType` in the main executable `.csproj` was set to `Exe` instead of `WinExe`. `Exe` tells the Windows OS to allocate a console subsystem, whereas `WinExe` uses the Windows GUI subsystem.
- **Rule:** Always ensure that desktop applications (Avalonia, WPF, Windows Forms) have `<OutputType>WinExe</OutputType>` in their main `.csproj` to prevent unwanted command prompt windows from opening alongside the GUI.

## 2. Linux Packaging (dpkg-deb) on Windows (WSL)
- **Pattern:** `dpkg-deb` fails with "control directory has bad permissions 777" when running on an NTFS mount in WSL.
- **Root Cause:** NTFS-mounted drives in WSL typically default to `777` permissions for all files, which `dpkg-deb` rejects for security reasons (it requires `0755` or similar).
- **Rule:** When building Debian packages inside WSL, always perform the packaging steps (where `dpkg-deb` is called) within the native Linux filesystem (e.g., `~/build-dir`) rather than on a `/mnt/c/` or `/mnt/d/` drive. Use a helper script to copy the final `.deb` back to the Windows host if needed.

