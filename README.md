# Markdown Converter Pro

Fast, offline conversion of Markdown files to PDF, Word, and Excel on Windows.

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

Markdown Converter Pro is a Windows desktop app that turns `.md` and `.markdown` files into polished PDF, DOCX, and XLSX outputs with a clean, drag-and-drop UI.

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

- Windows 10/11
- .NET SDK that supports `net10.0-windows` (for example, the .NET 10 SDK)
- Visual Studio 2022+ with the .NET Desktop Development workload (optional but recommended)

Build and run:

```powershell
# from the repo root

dotnet restore

dotnet build

dotnet run --project MarkdownConverter.csproj
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

- PDF and Word styles live in `Converters/MarkdownToHtmlRenderer.cs` (embedded CSS).
- Excel table styling is configured in `Converters/XlsxExporter.cs`.

## Project structure

```
.
?? Converters/           # PDF, Word, Excel exporters + HTML renderer
?? Controls/             # Custom WPF controls
?? Models/               # Data models (formats, options)
?? Samples/              # Markdown samples to validate rendering
?? Themes/               # App theme and resources
?? UI/                   # Dialogs and WPF converters
?? ViewModels/           # MVVM logic
?? img/                  # Icons and images
?? MainWindow.xaml       # Main UI layout
?? App.xaml              # App resources
?? MarkdownConverter.csproj
```

## Samples

Use the sample Markdown snippets in `Samples/ConversionSamples.md` to validate formatting across exports.

## Dependencies

Key packages used in this project:

- Markdig (Markdown parsing)
- DinkToPdf / DinkToPdfAll (PDF export)
- DocumentFormat.OpenXml + HtmlToOpenXml (Word export)
- ClosedXML (Excel export)

## License

See `LICENSE.txt`.

## Contributing

Issues and pull requests are welcome. Please include a clear description of the change and the expected behavior.
