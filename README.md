# Markdown Converter Pro

Fast, offline conversion of Markdown files to PDF, Word, and Excel on Windows, Linux, and macOS.

<p align="center">
  <img src="img/md_converter.png" alt="Markdown Converter Pro Logo" width="200">
</p>

## Table of contents

- Overview
- Features
- Screenshot
- Formats
- Quick start
- Usage
- Conversion pipeline
- Markdown support
- Customize styling
- Project structure
- Samples
- Dependencies
- License
- Contributing

## Overview

Markdown Converter Pro is now split into a cross-platform conversion core and an Avalonia desktop app that turns `.md` and `.markdown` files into PDF, DOCX, and XLSX outputs with a drag-and-drop workflow.

## Features

- Drag and drop Markdown files anywhere in the window
- Output formats: PDF (A4), Word (DOCX), Excel (XLSX)
- Table-aware Excel export with one worksheet per table
- Notes sheet in Excel for non-table content
- Consistent styling for PDF and Word via embedded HTML/CSS
- Output controls: choose folder, copy output path, open after convert, overwrite toggle
- Progress indicator and toast notifications

## Screenshot

![App Screenshot](img/main_screenshot.png)

## Formats

- PDF: HTML-to-PDF rendering with a print-ready A4 layout
- Word: HTML-to-DOCX conversion for editable documents
- Excel: Tables extracted into worksheets, with notes collected separately

## Quick start

Requirements:

- Windows / Linux / macOS (x64)
- .NET 10 SDK
- wkhtmltopdf native runtime (`libwkhtmltox`) for PDF export:
  - `src/MarkdownConverter.Desktop/runtimes/win-x64/native/libwkhtmltox.dll`
  - `src/MarkdownConverter.Desktop/runtimes/linux-x64/native/libwkhtmltox.so`
  - `src/MarkdownConverter.Desktop/runtimes/osx-x64/native/libwkhtmltox.dylib`

Build and run:

```bash
# from the repo root

dotnet restore --ignore-failed-sources

dotnet build MarkdownConverter.sln --ignore-failed-sources -m:1

dotnet run --project src/MarkdownConverter.Desktop/MarkdownConverter.Desktop.csproj --ignore-failed-sources
```

## Usage

1. Launch the app.
2. Drag a `.md` or `.markdown` file into the window (or click Browse).
3. Choose the output format (PDF, Word, or Excel).
4. Pick the output folder.
5. Click Convert.

## Conversion pipeline

- Markdown is parsed with Markdig and rendered to HTML.
- PDF export uses DinkToPdf (HTML-to-PDF, A4, 300 DPI, no JS).
- Word export uses OpenXML + HtmlToOpenXml to transform HTML into DOCX.
- Excel export parses Markdown tables and builds worksheets via ClosedXML.

## Markdown support

The renderer enables advanced Markdown features, including:

- GitHub-style auto identifiers for headings
- Pipe tables and grid tables
- Task lists
- Auto links
- Emoji and smiley shortcuts
- Soft line breaks treated as hard line breaks

## Customize styling

- PDF and Word styles live in `src/MarkdownConverter.Core/Converters/MarkdownToHtmlRenderer.cs` (embedded CSS).
- Excel table styling is configured in `src/MarkdownConverter.Core/Converters/XlsxExporter.cs`.

## Project structure

```
.
?? src/MarkdownConverter.Core/      # Cross-platform conversion core + MVVM
?? src/MarkdownConverter.Desktop/   # Avalonia desktop app (Win/Linux/macOS)
?? tests/MarkdownConverter.Tests/   # Offline-friendly test harness
?? tests/Fixtures/                  # Markdown fixtures for parity checks
?? tests/Baselines/                 # Baseline output scaffold + metadata template
?? Samples/                         # Sample markdown used for manual validation
?? Converters/, ViewModels/, UI/    # Legacy WPF source tree retained for reference/migration
?? MarkdownConverter.csproj         # Legacy Windows-only project (not in solution)
```

## Samples

Use the sample Markdown snippets in `Samples/ConversionSamples.md` to validate formatting across exports.

## Dependencies

Key packages used in this project:

- Markdig (Markdown parsing)
- DinkToPdf (PDF export API wrapper)
- Native wkhtmltopdf runtime files (`libwkhtmltox`) per target platform
- DocumentFormat.OpenXml + HtmlToOpenXml (Word export)
- ClosedXML (Excel export)

## License

See `LICENSE.txt`.

## Contributing

Issues and pull requests are welcome. Please include a clear description of the change and the expected behavior.
