# Packaging & Distribution Guide

This document explains how to build and distribute MarkdownConverter across all supported platforms.

## Quick Start

### Windows

```powershell
# Build the single .exe installer
.\scripts\build-local.ps1
```

That's it. One file: `release\MarkdownConverter-Setup-x64.exe`

### Linux (Native)

```bash
# Build all Linux packages
cd packaging/linux
chmod +x build-all-linux.sh
./build-all-linux.sh v2.1.0

# Build specific packages only
./build-all-linux.sh v2.1.0 --skip-flatpak --skip-snap

# Build individual packages
./build-deb.sh v2.1.0
./build-rpm.sh v2.1.0
./build-appimage.sh v2.1.0
./build-flatpak.sh v2.1.0
./build-snap.sh v2.1.0
```

## Release Process

1. **Prepare the release:**
   ```powershell
   .\scripts\prepare-release.ps1
   ```
   This automatically:
   - Calculates the next version number
   - Updates all `.csproj` files
   - Updates Windows installers (`.iss`, `.wxs`)
   - Updates Linux packaging (Flatpak, Snap)
   - Generates `RELEASE_NOTES_TMP.md`

2. **Build installer:**
   ```powershell
   .\scripts\build-local.ps1
   ```

3. **Tag and push:**
   ```powershell
   $desc = Get-Content RELEASE_NOTES_TMP.md | Out-String
   git tag -a v2.1.0 -m "$desc"
   git push origin v2.1.0
   ```

   The GitHub Actions workflow will automatically build all platform packages and create the release.

## Supported Platforms & Formats

### Windows

| Format | File | Description |
|--------|------|-------------|
| **Setup** | `MarkdownConverter-Setup-x64.exe` | Single installer — next, next, finish |
| **MSIX** | `MarkdownConverter-win-x64.msix` | Microsoft Store compatible, clean install/uninstall, sandboxed |

### Linux

| Format | File | Distribution | Features |
|--------|------|-------------|----------|
| **Debian** | `markdownconverter_X.Y.Z_amd64.deb` | Debian, Ubuntu, Linux Mint | • Proper maintainer scripts (postinst/prerm/postrm)<br>• Desktop integration with icons<br>• MIME type registration<br>• Complete cleanup on removal |
| **RPM** | `markdownconverter-X.Y.Z-1.x86_64.rpm` | Fedora, RHEL, CentOS, openSUSE | %post/%preun/%postun scripts<br>• Desktop & icon integration<br>• Upgrade detection<br>• Complete uninstallation |
| **Flatpak** | `MarkdownConverter-linux-x64.flatpak` | Any distro (via Flatpak) | • Sandboxed execution<br>• Bundled dependencies<br>• Works on any distro with Flatpak<br>• Automatic cleanup on uninstall |
| **Snap** | `MarkdownConverter-linux-x64.snap` | Ubuntu, Snap-enabled distros | • Confined security model<br>• Auto-updates (if published to Snap Store)<br>• Cross-distro compatibility |
| **AppImage** | `MarkdownConverter-linux-x64.AppImage` | Any distro (portable) | • No installation required<br>• Runs on any modern Linux<br>• Self-contained with all dependencies<br>• Zstd compression for smaller size |
| **AUR** | `PKGBUILD` | Arch Linux, Manjaro | • Integrates with Arch `pacman`<br>• Built from portable release |
| **Nix** | `flake.nix` | NixOS, Nix package manager | • Declarative dependency management<br>• Reproducible builds |
| **Portable tar.gz** | `MarkdownConverter-linux-x64-portable.tar.gz` | Manual extraction | • Manual installation<br>• Custom deployment scenarios |

## Installation & Uninstallation Details

### Windows

**Install:**
```cmd
MarkdownConverter-Setup-x64.exe
```

**Silent install:**
```cmd
MarkdownConverter-Setup-x64.exe /VERYSILENT /NORESTART
```

**Uninstall:**
- Via Windows Settings → Apps & Features
- Via Control Panel → Programs and Features

**What gets removed on uninstall:**
- All application files from the installation directory
- Start Menu shortcuts and folder
- Desktop shortcut (if created)
- Registry entries under `HKCU\Software\MarkdownConverterTeam`

**Upgrade behavior:**
- Automatically detects and removes any existing installation
- Version checking warns if attempting to install an older version
- Clean replacement — no leftover files

### Windows - MSIX

**Build:**
```powershell
.\packaging\windows\build-msix.ps1
```

**Install:**
Double-click the `.msix` file or run:
```powershell
Add-AppxPackage -Path .\release\MarkdownConverter-win-x64.msix
```
*Note: Unless signed with a trusted certificate, Windows may require Developer Mode to install.*

### Linux - Debian (.deb)

**Install:**
```bash
sudo dpkg -i markdownconverter_*.deb
sudo apt-get install -f  # Fix dependencies if needed
```

**Uninstall (keep config):**
```bash
sudo apt-get remove markdownconverter
```

**Complete removal (purge):**
```bash
sudo apt-get purge markdownconverter
sudo apt-get autoremove
```

**What gets cleaned up:**
- Application files from `/usr/lib/markdownconverter/`
- Symlink from `/usr/bin/markdownconverter`
- Desktop entry from `/usr/share/applications/`
- Icons from `/usr/share/pixmaps/` and `/usr/share/icons/hicolor/`
- Desktop database and icon cache updated automatically

### Linux - RPM (.rpm)

**Install:**
```bash
sudo dnf install markdownconverter-*.rpm   # Fedora/RHEL 8+
sudo yum install markdownconverter-*.rpm   # Older systems
```

**Uninstall:**
```bash
sudo dnf remove markdownconverter
```

**Cleanup includes:**
- All files from `/usr/lib/markdownconverter/`
- Symlink from `/usr/bin/markdownconverter`
- Desktop entry and icons
- Automatic desktop database and icon cache refresh

### Linux - Flatpak

**Install:**
```bash
flatpak install MarkdownConverter-linux-x64.flatpak
```

**Run:**
```bash
flatpak run com.markdownconverter.app
```

**Uninstall:**
```bash
flatpak uninstall com.markdownconverter.app
```

**Benefits:**
- Sandboxed for security
- Works on any distro with Flatpak support
- Automatic cleanup on uninstall
- Can access home directory and downloads

### Linux - Snap

**Install:**
```bash
sudo snap install MarkdownConverter-linux-x64.snap --dangerous
```

**Run:**
```bash
markdownconverter
```

**Uninstall:**
```bash
sudo snap remove markdownconverter
```

**Note:** The `--dangerous` flag is needed for local installation. When published to the Snap Store, this is not needed.

### Linux - AppImage

**Make executable:**
```bash
chmod +x MarkdownConverter-linux-x64.AppImage
```

**Run:**
```bash
./MarkdownConverter-linux-x64.AppImage
```

**Features:**
- No installation required
- Can be run from any location
- Integrates with desktop via optional desktop integration
- Self-contained with all dependencies

### Linux - AUR (Arch Linux)

**Install via helper (e.g., yay):**
When published to the AUR, users can install via:
```bash
yay -S markdownconverter-bin
```

**Manual build:**
```bash
cd packaging/linux/AUR
makepkg -si
```

### Linux - NixOS

**Run directly via flakes:**
```bash
nix run github:AhmedElbehary-Dev/MarkdownConverter
```

## Build Requirements

### Windows

- **.NET 10.0 SDK** — to publish the application
- **Inno Setup** — to build the `.exe` installer (https://jrsoftware.org/isdl.php)

### Linux (for building packages)

**All distributions:**
- .NET 10.0 SDK
- dpkg (for .deb)
- rpm build tools (for .rpm)

**AppImage:**
- libfuse2
- ImageMagick (optional, for icon conversion)

**Flatpak:**
- flatpak
- flatpak-builder
- Flathub remote configured

**Snap:**
- snapd
- snapcraft (`sudo snap install snapcraft --classic`)

## CI/CD - Automated Builds

The GitHub Actions workflow in `.github/workflows/release.yml` automatically builds all packages when a version tag is pushed:

```bash
# Trigger automated build
git tag -a v2.1.0 -m "Release v2.1.0"
git push origin v2.1.0
```

This produces:
- Windows: `.exe`, `.msi`, `.zip`
- Linux: `.deb`, `.rpm`, `.AppImage`, `.flatpak`, `.snap`, `.tar.gz`

All artifacts are automatically attached to the GitHub Release.

## Troubleshooting

### Windows

**Inno Setup not found:**
- Install from https://jrsoftware.org/isdl.php
- Ensure `iscc.exe` is in your PATH
- Restart your terminal after installation

**Old version still appears after upgrade:**
- The installer automatically removes old versions
- Check `C:\Program Files\MarkdownConverter\` for leftover files
- Clean registry manually: `HKCU\Software\MarkdownConverter\`

### Linux

**Dependencies not met (.deb):**
```bash
sudo apt-get install -f
```

**AppImage won't run:**
```bash
# Install FUSE
sudo apt-get install libfuse2
```

**Flatpak build fails:**
- Ensure Flathub remote is added: `flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo`

**Snap build fails:**
- Install snapcraft: `sudo snap install snapcraft --classic`
- May need to run in LXD container for strict confinement

## Publishing to Package Repositories

### Snap Store
```bash
snapcraft login
snapcraft upload MarkdownConverter-linux-x64.snap
```

### Flatpak on Flathub
1. Fork https://github.com/flathub/flathub
2. Submit `com.markdownconverter.app.yml` as a PR
3. Follow Flathub submission guidelines

### Debian/Ubuntu PPA
```bash
# Use dput to upload to PPA
dput ppa:your-ppa markdownconverter_*.changes
```

### RPM on COPR (Fedora)
```bash
# Upload to Fedora COPR
copr-cli build your-project MarkdownConverter-*.src.rpm
```

## Version Management

All version numbers are managed centrally:

1. **`.csproj` files** - Source of truth
2. **`setup.iss`** - Patched during build
3. **`setup.wxs`** - Patched during build
4. **`com.markdownconverter.app.yml`** - Flatpak manifest
5. **`snap/snapcraft.yaml`** - Snap manifest

Use `prepare-release.ps1` to update all versions automatically.

## File Structure

```
packaging/
├── linux/
│   ├── build-deb.sh              # Debian package builder
│   ├── build-rpm.sh              # RPM package builder
│   ├── build-appimage.sh         # AppImage builder
│   ├── build-flatpak.sh          # Flatpak builder
│   ├── build-snap.sh             # Snap builder
│   ├── build-all-linux.sh        # All-in-one Linux builder
│   ├── markdownconverter.spec    # RPM spec file
│   ├── com.markdownconverter.app.yml      # Flatpak manifest
│   ├── com.markdownconverter.app.desktop  # Flatpak desktop entry
│   └── snap/
│       ├── snapcraft.yaml        # Snap configuration
│       └── markdownconverter-snap.desktop  # Snap desktop entry
└── windows/
    ├── setup.iss                 # Inno Setup script (primary Windows installer)
    └── setup.wxs                 # WiX v4 (kept for reference, not used in CI)
```

## Support

- **Issues**: https://github.com/AhmedElbehary-Dev/MarkdownConverter/issues
- **Email**: as.elbehary@gmail.com
- **License**: MIT
