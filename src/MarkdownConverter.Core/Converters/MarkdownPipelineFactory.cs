using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using System;

namespace MarkdownConverter.Converters;

internal static class MarkdownPipelineFactory
{
    /// <summary>
    /// Markdig defaults to 128. Real docs with messy emphasis, large tables, or
    /// unresolved delimiter chains often exceed that during HTML rendering.
    /// Keep this below typical stack-overflow risk from deep recursion.
    /// </summary>
    internal const int MaximumNestingDepth = 2048;

    public static MarkdownPipeline CreateHtmlPipeline()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoLinks()
            .UsePipeTables()
            .UseGridTables()
            .UseTaskLists()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .UseEmojiAndSmiley()
            .UseSoftlineBreakAsHardlineBreak();

        builder.MaximumNestingDepth = MaximumNestingDepth;
        return builder.Build();
    }

    public static MarkdownPipeline CreateTablePipeline()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePipeTables()
            .UseGridTables()
            .UseAutoLinks()
            .UseTaskLists()
            .UseEmojiAndSmiley()
            .UseSoftlineBreakAsHardlineBreak();

        builder.MaximumNestingDepth = MaximumNestingDepth;
        return builder.Build();
    }

    public static bool IsNestingDepthExceeded(Exception ex) =>
        ex is ArgumentException
        && ex.Message.Contains("depth limit exceeded", StringComparison.OrdinalIgnoreCase);
}
