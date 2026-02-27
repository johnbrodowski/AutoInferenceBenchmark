namespace AutoInferenceBenchmark.Core;

/// <summary>
/// Captures score and metrics for one inference run against one test case with one configuration.
/// </summary>
public sealed record BenchmarkResult
{
    public long Id { get; init; }
    public long RunId { get; init; }
    public Guid TestCaseId { get; init; }
    public string TestCaseName { get; init; } = "";
    public required InferenceConfig Config { get; init; }
    public string ResponseText { get; init; } = "";
    public float MatchPercentage { get; init; }
    public bool IsPass { get; init; }
    public float TokensPerSecond { get; init; }
    public float TimeToFirstTokenSeconds { get; init; }
    public double TotalLatencySeconds { get; init; }
    public int TokenCount { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
