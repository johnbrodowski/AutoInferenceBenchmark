using AutoInferenceBenchmark.Core;

namespace AutoInferenceBenchmark.Scoring;

/// <summary>
/// Scores responses using three complementary similarity algorithms:
/// Levenshtein distance, Jaccard token overlap, and Longest Common Subsequence.
/// </summary>
public sealed class SimilarityScorer : IResponseScorer
{
    public ScoringResult Score(string expected, string actual, MatchMode mode, float similarityThreshold)
    {
        var normExpected = ResponseNormalizer.Normalize(expected);
        var normActual = ResponseNormalizer.Normalize(actual);

        if (mode == MatchMode.Exact)
        {
            bool match = string.Equals(normExpected, normActual, StringComparison.OrdinalIgnoreCase);
            return new ScoringResult
            {
                IsPass = match,
                MatchPercentage = match ? 100f : 0f,
                MatchMode = MatchMode.Exact,
                LevenshteinScore = match ? 100f : 0f,
                JaccardScore = match ? 100f : 0f,
                LcsScore = match ? 100f : 0f,
                Details = match ? "Exact match" : "No exact match"
            };
        }

        // Similarity mode: compute all three metrics
        float levenshtein = LevenshteinSimilarity(normExpected, normActual);
        float jaccard = JaccardSimilarity(normExpected, normActual);
        float lcs = LcsSimilarity(normExpected, normActual);

        // Composite: weighted average favoring the best metric
        // (max gets 50% weight, other two split the remaining 50%)
        float max = Math.Max(levenshtein, Math.Max(jaccard, lcs));
        float avg = (levenshtein + jaccard + lcs) / 3f;
        float composite = max * 0.5f + avg * 0.5f;

        bool isPass = composite >= similarityThreshold;

        return new ScoringResult
        {
            IsPass = isPass,
            MatchPercentage = composite,
            MatchMode = MatchMode.Similarity,
            LevenshteinScore = levenshtein,
            JaccardScore = jaccard,
            LcsScore = lcs,
            Details = $"Lev={levenshtein:F1}% Jac={jaccard:F1}% LCS={lcs:F1}% => {composite:F1}%"
        };
    }

    /// <summary>
    /// Levenshtein (edit distance) similarity as a percentage.
    /// 100% = identical, 0% = completely different.
    /// </summary>
    private static float LevenshteinSimilarity(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 100f;
        if (a.Length == 0 || b.Length == 0) return 0f;

        // Use lowercase for case-insensitive comparison
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();

        int distance = LevenshteinDistance(a, b);
        int maxLen = Math.Max(a.Length, b.Length);
        return (1f - (float)distance / maxLen) * 100f;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int m = a.Length, n = b.Length;
        // Use two-row optimization for memory efficiency
        var prev = new int[n + 1];
        var curr = new int[n + 1];

        for (int j = 0; j <= n; j++) prev[j] = j;

        for (int i = 1; i <= m; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= n; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[n];
    }

    /// <summary>
    /// Jaccard similarity: |intersection| / |union| of word tokens, as a percentage.
    /// </summary>
    private static float JaccardSimilarity(string a, string b)
    {
        var tokensA = Tokenize(a);
        var tokensB = Tokenize(b);

        if (tokensA.Count == 0 && tokensB.Count == 0) return 100f;
        if (tokensA.Count == 0 || tokensB.Count == 0) return 0f;

        int intersection = tokensA.Intersect(tokensB).Count();
        int union = tokensA.Union(tokensB).Count();

        return union > 0 ? (float)intersection / union * 100f : 0f;
    }

    /// <summary>
    /// Longest Common Subsequence similarity as a percentage.
    /// LCS length / max(len(a), len(b)) * 100.
    /// </summary>
    private static float LcsSimilarity(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 100f;
        if (a.Length == 0 || b.Length == 0) return 0f;

        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();

        int lcsLen = LcsLength(a, b);
        int maxLen = Math.Max(a.Length, b.Length);
        return (float)lcsLen / maxLen * 100f;
    }

    private static int LcsLength(string a, string b)
    {
        int m = a.Length, n = b.Length;
        var prev = new int[n + 1];
        var curr = new int[n + 1];

        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                curr[j] = a[i - 1] == b[j - 1]
                    ? prev[j - 1] + 1
                    : Math.Max(prev[j], curr[j - 1]);
            }
            (prev, curr) = (curr, prev);
            Array.Clear(curr);
        }
        return prev[n];
    }

    private static HashSet<string> Tokenize(string text) =>
        new(text.ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\n', '\r', ',', '.', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}' },
                StringSplitOptions.RemoveEmptyEntries));
}
