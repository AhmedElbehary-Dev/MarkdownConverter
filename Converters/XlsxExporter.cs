using ClosedXML.Excel;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkdownConverter.Converters;

public sealed class XlsxExporter
{
    private readonly MarkdownPipeline _pipeline;

    public XlsxExporter()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePipeTables()
            .UseGridTables()
            .UseAutoLinks()
            .UseTaskLists()
            .UseEmojiAndSmiley()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();
    }

    public Task ExportAsync(string markdownText, string outputPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = Markdown.Parse(markdownText, _pipeline);
        var tables = ExtractTables(document).ToList();

        using var workbook = new XLWorkbook();

        foreach (var tableInfo in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddTableWorksheet(workbook, tableInfo);
        }

        var notes = ExtractNotes(document);
        if (notes.Any() || !tables.Any())
        {
            AddNotesWorksheet(workbook, notes);
        }

        workbook.SaveAs(outputPath);
        return Task.CompletedTask;
    }

    private static IEnumerable<MarkdownTable> ExtractTables(MarkdownDocument document)
    {
        string? lastHeading = null;
        var tableCount = 0;

        foreach (var block in document)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    lastHeading = ExtractInlineText(heading.Inline);
                    break;
                case Table table:
                    tableCount++;
                    var name = !string.IsNullOrWhiteSpace(lastHeading) ? lastHeading : $"Table {tableCount}";
                    yield return new MarkdownTable(name, table);
                    break;
            }
        }
    }

    private static List<string> ExtractNotes(MarkdownDocument document)
    {
        var notes = new List<string>();

        foreach (var block in document)
        {
            if (block is Table)
            {
                continue;
            }

            var text = ExtractBlockText(block);
            if (!string.IsNullOrWhiteSpace(text))
            {
                notes.Add(text.Trim());
            }
        }

        return notes;
    }

    private static void AddTableWorksheet(XLWorkbook workbook, MarkdownTable tableInfo)
    {
        var worksheet = workbook.Worksheets.Add(EnsureUniqueSheetName(workbook, tableInfo.Name));
        var rows = ExtractRows(tableInfo.Table).ToList();
        if (!rows.Any())
        {
            worksheet.Cell(1, 1).Value = "No table rows detected.";
            return;
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var rowModel = rows[rowIndex];
            for (var colIndex = 0; colIndex < rowModel.Cells.Count; colIndex++)
            {
                worksheet.Cell(rowIndex + 1, colIndex + 1).Value = rowModel.Cells[colIndex];
            }

            if (rowModel.IsHeader)
            {
                var headerRange = worksheet.Range(rowIndex + 1, 1, rowIndex + 1, Math.Max(1, rowModel.Cells.Count));
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        var usedRange = worksheet.RangeUsed();
        if (usedRange != null)
        {
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        }

        if (rows.First().IsHeader)
        {
            worksheet.SheetView.FreezeRows(1);
        }

        worksheet.Columns().AdjustToContents();
    }

    private static void AddNotesWorksheet(XLWorkbook workbook, IReadOnlyList<string> notes)
    {
        var worksheet = workbook.Worksheets.Add(EnsureUniqueSheetName(workbook, "Notes"));
        worksheet.Cell(1, 1).Value = "Notes";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        var currentRow = 2;
        foreach (var note in notes)
        {
            worksheet.Cell(currentRow++, 1).Value = note;
        }

        worksheet.Columns().AdjustToContents();
    }

    private static IEnumerable<TableRowModel> ExtractRows(Table table)
    {
        foreach (var block in table)
        {
            if (block is not TableRow row)
            {
                continue;
            }

            var cells = ExtractCells(row);
            yield return new TableRowModel(cells, row.IsHeader);
        }
    }

    private static List<string> ExtractCells(TableRow row)
    {
        var cells = new List<string>();
        foreach (var block in row)
        {
            if (block is not TableCell tableCell)
            {
                continue;
            }

            var text = ExtractBlockText(tableCell);
            cells.Add(text);
        }

        return cells;
    }

    private static string ExtractBlockText(Block block)
    {
        var builder = new StringBuilder();

        switch (block)
        {
            case ParagraphBlock paragraph:
                builder.Append(ExtractInlineText(paragraph.Inline));
                break;
            case HeadingBlock heading:
                builder.Append(ExtractInlineText(heading.Inline));
                break;
            case FencedCodeBlock fencedCode:
                builder.Append(fencedCode.Lines.ToString());
                break;
            case ContainerBlock container:
                foreach (var child in container)
                {
                    var text = ExtractBlockText(child);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        builder.Append(text).Append(' ');
                    }
                }

                break;
        }

        return builder.ToString().Trim();
    }

    private static string ExtractInlineText(ContainerInline? container)
    {
        var builder = new StringBuilder();
        var inline = container?.FirstChild;
        while (inline != null)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline codeInline:
                    builder.Append(codeInline.Content);
                    break;
                case LinkInline linkInline:
                    builder.Append(linkInline.Title ?? linkInline.Url ?? linkInline.FirstChild?.ToString());
                    break;
                case LineBreakInline:
                    builder.Append(' ');
                    break;
                case ContainerInline nested:
                    builder.Append(ExtractInlineText(nested));
                    break;
            }

            inline = inline.NextSibling;
        }

        return builder.ToString();
    }

    private static string EnsureUniqueSheetName(XLWorkbook workbook, string desiredName)
    {
        var baseName = CleanSheetName(desiredName);
        var finalName = baseName;
        var counter = 1;

        while (workbook.Worksheets.Any(w => string.Equals(w.Name, finalName, StringComparison.OrdinalIgnoreCase)))
        {
            finalName = $"{baseName}_{counter++}";
            if (finalName.Length > 31)
            {
                finalName = finalName[..31];
            }
        }

        return finalName;
    }

    private static string CleanSheetName(string original)
    {
        if (string.IsNullOrWhiteSpace(original))
        {
            return "Table";
        }

        var invalid = new[] { '[', ']', '*', '/', '\\', '?', ':' };
        var builder = new StringBuilder();
        foreach (var ch in original)
        {
            if (invalid.Contains(ch) || char.IsControl(ch))
            {
                continue;
            }

            builder.Append(ch);
        }

        var result = builder.ToString().Trim();
        if (result.Length > 31)
        {
            result = result[..31];
        }

        return string.IsNullOrWhiteSpace(result) ? "Table" : result;
    }

    private sealed record TableRowModel(List<string> Cells, bool IsHeader);

    private sealed record MarkdownTable(string Name, Table Table);
}
