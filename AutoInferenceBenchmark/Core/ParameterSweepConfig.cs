namespace AutoInferenceBenchmark.Core;

public enum SweepMode
{
    /// <summary>Vary only Temperature, hold everything else at defaults.</summary>
    TemperatureOnly,

    /// <summary>Cartesian product of all enabled parameter ranges.</summary>
    AllCombinations
}

/// <summary>
/// Defines ranges and step sizes for each tunable parameter in a benchmark sweep.
/// </summary>
public sealed class ParameterSweepConfig
{
    public SweepMode Mode { get; set; } = SweepMode.TemperatureOnly;

    // Temperature
    public float TemperatureMin { get; set; } = 0.1f;
    public float TemperatureMax { get; set; } = 1.0f;
    public float TemperatureStep { get; set; } = 0.1f;

    // Top-P
    public float TopPMin { get; set; } = 0.9f;
    public float TopPMax { get; set; } = 1.0f;
    public float TopPStep { get; set; } = 0.05f;

    // Top-K
    public int TopKMin { get; set; } = 20;
    public int TopKMax { get; set; } = 60;
    public int TopKStep { get; set; } = 10;

    // Min-P
    public float MinPMin { get; set; } = 0.05f;
    public float MinPMax { get; set; } = 0.2f;
    public float MinPStep { get; set; } = 0.05f;

    // Repeat Penalty
    public float RepeatPenaltyMin { get; set; } = 1.0f;
    public float RepeatPenaltyMax { get; set; } = 1.15f;
    public float RepeatPenaltyStep { get; set; } = 0.05f;

    // Defaults for non-swept parameters
    public float DefaultTemperature { get; set; } = 0.7f;
    public float DefaultTopP { get; set; } = 0.9f;
    public int DefaultTopK { get; set; } = 40;
    public float DefaultMinP { get; set; } = 0.1f;
    public float DefaultRepeatPenalty { get; set; } = 1.0f;
    public float DefaultFrequencyPenalty { get; set; } = 0f;
    public float DefaultPresencePenalty { get; set; } = 0f;
    public int MaxTokens { get; set; } = 2048;

    /// <summary>If true, use a fixed seed for deterministic results.</summary>
    public bool DeterministicSeed { get; set; } = true;

    /// <summary>Fixed seed value when <see cref="DeterministicSeed"/> is true.</summary>
    public uint Seed { get; set; } = 42;
}
