namespace AutoInferenceBenchmark.Core;

/// <summary>
/// Immutable snapshot of all tunable inference parameters for a single run.
/// Maps 1:1 to <see cref="LLama.Sampling.DefaultSamplingPipeline"/> properties.
/// </summary>
public sealed record InferenceConfig
{
    public float Temperature { get; init; } = 0.75f;
    public float TopP { get; init; } = 0.9f;
    public int TopK { get; init; } = 40;
    public float MinP { get; init; } = 0.1f;
    public float RepeatPenalty { get; init; } = 1.0f;
    public float FrequencyPenalty { get; init; } = 0f;
    public float PresencePenalty { get; init; } = 0f;
    public int MaxTokens { get; init; } = 2048;
    public uint Seed { get; init; } = 0;

    /// <summary>Short human-readable summary for display in grids.</summary>
    public string ToShortString() =>
        $"T={Temperature:F2} P={TopP:F2} K={TopK} Min={MinP:F2} Rep={RepeatPenalty:F2}";
}
