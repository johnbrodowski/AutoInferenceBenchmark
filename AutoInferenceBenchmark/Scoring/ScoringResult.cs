using AutoInferenceBenchmark.Core;

namespace AutoInferenceBenchmark.Scoring;

/// <summary>
/// Result of scoring a model response against an expected response.
/// </summary>
public sealed record ScoringResult
{
    /// <summary>Whether the response meets the test case threshold.</summary>
    public bool IsPass { get; init; }

    /// <summary>Overall match percentage (0–100).</summary>
    public float MatchPercentage { get; init; }

    /// <summary>The match mode used for scoring.</summary>
    public MatchMode MatchMode { get; init; }

    /// <summary>Individual metric scores for transparency.</summary>
    public float LevenshteinScore { get; init; }
    public float JaccardScore { get; init; }
    public float LcsScore { get; init; }

    /// <summary>Human-readable summary.</summary>
    public string Details { get; init; } = "";
}
