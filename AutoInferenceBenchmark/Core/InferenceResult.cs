namespace AutoInferenceBenchmark.Core;

/// <summary>
/// Captures the full output and metrics from a single inference call.
/// </summary>
public sealed record InferenceResult
{
    public required string ResponseText { get; init; }
    public int TokenCount { get; init; }
    public float TokensPerSecond { get; init; }
    public float TimeToFirstTokenSeconds { get; init; }
    public double TotalLatencySeconds { get; init; }
    public required InferenceConfig Config { get; init; }
}
