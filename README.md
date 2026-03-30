# Markdown Converter Pro

<p align="center">
  <img src="img/md_converter.png" alt="Markdown Converter Pro" width="120">
</p>

<p align="center">
  <a href="https://github.com/AhmedElbehary-Dev/MarkdownConverter/actions/workflows/ci.yml"><img src="https://github.com/AhmedElbehary-Dev/MarkdownConverter/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI Status"></a>
  <a href="https://github.com/AhmedElbehary-Dev/MarkdownConverter/actions/workflows/release.yml"><img src="https://github.com/AhmedElbehary-Dev/MarkdownConverter/actions/workflows/release.yml/badge.svg" alt="Release Status"></a>
  <a href="https://github.com/AhmedElbehary-Dev/MarkdownConverter/actions/workflows/codeql.yml"><img src="https://github.com/AhmedElbehary-Dev/MarkdownConverter/actions/workflows/codeql.yml/badge.svg" alt="Security Scan"></a>
  <a href="https://github.com/AhmedElbehary-Dev/MarkdownConverter/releases"><img src="https://img.shields.io/github/v/release/AhmedElbehary-Dev/MarkdownConverter" alt="Latest Version"></a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10">
  <img src="https://img.shields.io/badge/Platform-Win%20%7C%20Mac%20%7C%20Linux-lightgrey" alt="Platforms">
  <img src="https://img.shields.io/badge/Security-Anti--Malware%20Checked-brightgreen?logo=googlechrome&logoColor=white" alt="Malware Free">
  <a href="SECURITY.md"><img src="https://img.shields.io/badge/Security-Policy-blue" alt="Security Policy"></a>
  <a href="LICENSE.txt"><img src="https://img.shields.io/badge/License-MIT-blue" alt="License"></a>
  <a href="https://github.com/AhmedElbehary-Dev/MarkdownConverter/pulls"><img src="https://img.shields.io/badge/PRs-welcome-brightgreen.svg" alt="PRs Welcome"></a>
</p>

<p align="center">
  <a href="https://github.com/AhmedElbehary-Dev/MarkdownConverter/stargazers"><img src="https://img.shields.io/github/stars/AhmedElbehary-Dev/MarkdownConverter?style=social" alt="GitHub stars"></a>
  <a href="https://github.com/AhmedElbehary-Dev/MarkdownConverter/network/members"><img src="https://img.shields.io/github/forks/AhmedElbehary-Dev/MarkdownConverter?style=social" alt="GitHub forks"></a>
</p>

<p align="center">
  <b>Convert Markdown to PDF, Word, and Excel — fast, offline, cross-platform.</b>
</p>

---

## Features

- **Drag & Drop** — drop `.md` files anywhere in the window to convert
- **Quick Paste** — paste markdown directly without creating files, with auto-detected titles and a history library
- **PDF** — print-ready A4 layout via HTML-to-PDF rendering
- **Word (DOCX)** — editable documents from HTML-to-DOCX conversion
- **Excel (XLSX)** — tables extracted into worksheets, non-table content collected in a Notes sheet
- **Output controls** — choose folder, copy path, open after convert, overwrite toggle
- **Progress & notifications** — inline progress indicator and toast alerts

## Screenshots

### Main Window
![Main Window](img/screen_1.png)

### Quick Paste
![Quick Paste](img/screen_2.png)

### PDF Editor
![PDF Editor](img/screen_3.png)

## Quick Start

**Requirements:** .NET 10 SDK · Windows / Linux / macOS (x64)

PDF export needs the native `libwkhtmltox` runtime placed under `src/MarkdownConverter.Desktop/runtimes/{rid}/native/`.

```bash
dotnet restore
dotnet build MarkdownConverter.sln
dotnet run --project src/MarkdownConverter.Desktop/MarkdownConverter.Desktop.csproj
```

## Usage

1. Launch the app.
2. Drop a `.md` file into the window (or click **Browse**).
3. Pick the output format and folder.
4. Click **Convert**.

## Project Structure

```
src/
  MarkdownConverter.Core/       # Cross-platform conversion core & MVVM
  MarkdownConverter.Desktop/    # Avalonia desktop app (Win / Linux / macOS)
tests/
  MarkdownConverter.Tests/      # Test harness
  Fixtures/                     # Markdown fixtures
  Baselines/                    # Baseline output scaffolds
Samples/                        # Sample markdown for manual validation
```

## Dependencies

| Package | Purpose |
|---------|---------|
| Markdig | Markdown parsing |
| DinkToPdf | PDF export (wkhtmltopdf wrapper) |
| OpenXml + HtmlToOpenXml | Word export |
| ClosedXML | Excel export |

## License

See [`LICENSE.txt`](LICENSE.txt).

## Contributing

Issues and pull requests are welcome.
