using System;
using System.Text;

namespace MarkdownConverter.Converters;

/// <summary>
/// Neutralizes pathological emphasis/delimiter chains that Markdig leaves as deeply
/// nested delimiter nodes (common with long runs of * or _). Code fences are preserved.
/// </summary>
internal static class MarkdownNestingSanitizer
{
    /// <summary>
    /// Escapes long * / _ runs (length &gt;= 3) outside fenced code blocks.
    /// </summary>
    public static string Sanitize(string markdown) => Sanitize(markdown, escapeAllEmphasisDelimiters: false);

    /// <summary>
    /// Escapes every * / _ outside fenced code blocks. Used as a last-resort fallback
    /// so conversion can still succeed when nesting remains pathological.
    /// </summary>
    public static string SanitizeAggressively(string markdown) => Sanitize(markdown, escapeAllEmphasisDelimiters: true);

    private static string Sanitize(string markdown, bool escapeAllEmphasisDelimiters)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return markdown;
        }

        var result = new StringBuilder(markdown.Length + 32);
        var i = 0;
        while (i < markdown.Length)
        {
            if (IsLineStart(markdown, i) && TryReadOpeningFence(markdown, i, out var fenceChar, out var fenceCount))
            {
                result.Append(markdown, i, fenceCount);
                i += fenceCount;

                var closing = FindClosingFence(markdown, i, fenceChar, fenceCount);
                if (closing < 0)
                {
                    result.Append(markdown, i, markdown.Length - i);
                    break;
                }

                result.Append(markdown, i, closing - i);
                i = closing;
                continue;
            }

            var ch = markdown[i];
            if (ch is '*' or '_')
            {
                var runStart = i;
                while (i < markdown.Length && markdown[i] == ch)
                {
                    i++;
                }

                var runLength = i - runStart;
                // Long unresolved delimiter runs (or all delimiters in aggressive mode)
                // are what blow Markdig's render depth.
                if (escapeAllEmphasisDelimiters || runLength >= 3)
                {
                    for (var n = 0; n < runLength; n++)
                    {
                        result.Append('\\').Append(ch);
                    }
                }
                else
                {
                    result.Append(markdown, runStart, runLength);
                }

                continue;
            }

            result.Append(ch);
            i++;
        }

        return result.ToString();
    }

    private static bool IsLineStart(string markdown, int index)
    {
        if (index == 0)
        {
            return true;
        }

        var prev = markdown[index - 1];
        return prev is '\n' or '\r';
    }

    private static bool TryReadOpeningFence(string markdown, int index, out char fenceChar, out int fenceCount)
    {
        fenceChar = '\0';
        fenceCount = 0;

        if (index >= markdown.Length || markdown[index] is not ('`' or '~'))
        {
            return false;
        }

        fenceChar = markdown[index];
        var i = index;
        while (i < markdown.Length && markdown[i] == fenceChar)
        {
            fenceCount++;
            i++;
        }

        return fenceCount >= 3;
    }

    private static int FindClosingFence(string markdown, int start, char fenceChar, int minCount)
    {
        var i = start;
        while (i < markdown.Length)
        {
            var lineStart = i;
            while (i < markdown.Length && markdown[i] is not ('\n' or '\r'))
            {
                i++;
            }

            var lineEnd = i;
            var trimmedStart = lineStart;
            while (trimmedStart < lineEnd && markdown[trimmedStart] is ' ' or '\t')
            {
                trimmedStart++;
            }

            var count = 0;
            var j = trimmedStart;
            while (j < lineEnd && markdown[j] == fenceChar)
            {
                count++;
                j++;
            }

            if (count >= minCount)
            {
                var onlyFence = true;
                while (j < lineEnd)
                {
                    if (markdown[j] is not (' ' or '\t'))
                    {
                        onlyFence = false;
                        break;
                    }

                    j++;
                }

                if (onlyFence)
                {
                    return lineStart;
                }
            }

            if (i < markdown.Length && markdown[i] == '\r')
            {
                i++;
            }

            if (i < markdown.Length && markdown[i] == '\n')
            {
                i++;
            }
        }

        return -1;
    }
}
