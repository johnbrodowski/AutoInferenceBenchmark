using AutoInferenceBenchmark.Core;

namespace AutoInferenceBenchmark.Benchmark;

/// <summary>
/// Progress report emitted during a benchmark run for UI updates.
/// </summary>
public sealed record BenchmarkProgress
{
    public required InferenceConfig CurrentConfig { get; init; }
    public string CurrentTestCaseName { get; init; } = "";
    public int CompletedCount { get; init; }
    public int TotalCount { get; init; }
    public float BestScoreSoFar { get; init; }
    public float CurrentScore { get; init; }
    public TimeSpan ElapsedTime { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    public float ProgressPercentage => TotalCount > 0 ? (float)CompletedCount / TotalCount * 100f : 0f;
}
