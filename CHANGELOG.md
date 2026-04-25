# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.1] - 2026-04-25
### Added
- Window centering on startup for the main application.
- `CenterOwner` positioning for all sub-windows (PDF Editor, Quick Paste, etc.).

### Fixed
- Fixed process locking issues during build and installation.

## [2.1.0] - 2026-04-25
### Added
- New UI dashboard with integrated tools.
- PDF Editor for page removal and extraction.
- PDF Compressor for reducing file size.
- Image-to-PDF conversion tool.
- Quick Paste feature for instant markdown export.
- MSIX packaging support for Windows 11.
- AUR (PKGBUILD) and NixOS (flake.nix) support for Linux.

### Fixed
- Installation race conditions by moving uninstaller to `PrepareToInstall`.
- System tray exit hanging issues.

## [2.0.9] - 2026-04-20
### Added
- Initial batch processing support.
- Improved PDF export quality.

---
*For full commit history, see the [GitHub repository](https://github.com/AhmedElbehary-Dev/MarkdownConverter).*
