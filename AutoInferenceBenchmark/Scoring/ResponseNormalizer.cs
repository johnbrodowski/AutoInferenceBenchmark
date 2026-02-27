using System.Text.RegularExpressions;

namespace AutoInferenceBenchmark.Scoring;

/// <summary>
/// Pre-scoring text normalization to reduce noise from formatting differences.
/// </summary>
public static partial class ResponseNormalizer
{
    /// <summary>
    /// Normalizes text for fair comparison: trims whitespace, normalizes line endings,
    /// collapses whitespace runs, and optionally strips markdown formatting.
    /// </summary>
    public static string Normalize(string text, bool stripMarkdown = true)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var result = text;

        // Normalize line endings
        result = result.Replace("\r\n", "\n").Replace("\r", "\n");

        if (stripMarkdown)
        {
            // Strip code fences (```language ... ```)
            result = CodeFenceRegex().Replace(result, "$1");

            // Strip inline code backticks
            result = InlineCodeRegex().Replace(result, "$1");

            // Strip bold/italic markers
            result = result.Replace("**", "").Replace("__", "");
            result = result.Replace("*", "").Replace("_", "");

            // Strip heading markers
            result = HeadingRegex().Replace(result, "");
        }

        // Collapse multiple whitespace (spaces/tabs) into single space
        result = WhitespaceRunRegex().Replace(result, " ");

        // Collapse multiple newlines into single newline
        result = MultiNewlineRegex().Replace(result, "\n");

        // Trim each line
        result = string.Join("\n", result.Split('\n').Select(l => l.Trim()));

        return result.Trim();
    }

    [GeneratedRegex(@"```\w*\n?(.*?)```", RegexOptions.Singleline)]
    private static partial Regex CodeFenceRegex();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"^#{1,6}\s+", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex WhitespaceRunRegex();

    [GeneratedRegex(@"\n{2,}")]
    private static partial Regex MultiNewlineRegex();
}
