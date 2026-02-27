using AutoInferenceBenchmark.Core;

namespace AutoInferenceBenchmark.Benchmark;

/// <summary>
/// Final summary produced at the end of a benchmark run.
/// </summary>
public sealed record BenchmarkRunSummary
{
    public long RunId { get; init; }
    public string ModelPath { get; init; } = "";
    public string ModelName { get; init; } = "";
    public int TotalConfigs { get; init; }
    public int TotalTests { get; init; }
    public int TotalRuns { get; init; }
    public required InferenceConfig BestConfig { get; init; }
    public float BestAverageScore { get; init; }
    public TimeSpan Duration { get; init; }
    public bool WasCancelled { get; init; }
    public List<BenchmarkResult> AllResults { get; init; } = new();
}
