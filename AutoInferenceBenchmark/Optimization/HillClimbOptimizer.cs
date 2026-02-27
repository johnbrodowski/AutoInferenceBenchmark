using AutoInferenceBenchmark.Core;

namespace AutoInferenceBenchmark.Optimization;

/// <summary>
/// Incremental parameter refinement: starts from a known-good configuration
/// and makes small adjustments in the direction of improving scores.
///
/// <para>Strategy:
/// 1. Start from the best config found during a sweep.
/// 2. For each parameter, try ±step adjustments.
/// 3. Keep the adjustment that improves average score.
/// 4. Stop when no single adjustment improves the score (local optimum).</para>
///
/// <para>This is a future enhancement — the scaffold defines the interface
/// and basic algorithm so it can be integrated with the BenchmarkEngine.</para>
/// </summary>
public sealed class HillClimbOptimizer
{
    /// <summary>
    /// Generates candidate configs by adjusting one parameter at a time from the base config.
    /// Returns configs that differ by exactly one parameter step.
    /// </summary>
    public static IEnumerable<InferenceConfig> GenerateNeighbors(InferenceConfig baseConfig, float step = 0.05f)
    {
        // Temperature neighbors
        if (baseConfig.Temperature - step >= 0.05f)
            yield return baseConfig with { Temperature = MathF.Round(baseConfig.Temperature - step, 4) };
        if (baseConfig.Temperature + step <= 2.0f)
            yield return baseConfig with { Temperature = MathF.Round(baseConfig.Temperature + step, 4) };

        // TopP neighbors
        if (baseConfig.TopP - step >= 0.1f)
            yield return baseConfig with { TopP = MathF.Round(baseConfig.TopP - step, 4) };
        if (baseConfig.TopP + step <= 1.0f)
            yield return baseConfig with { TopP = MathF.Round(baseConfig.TopP + step, 4) };

        // MinP neighbors
        if (baseConfig.MinP - step >= 0f)
            yield return baseConfig with { MinP = MathF.Round(baseConfig.MinP - step, 4) };
        if (baseConfig.MinP + step <= 1.0f)
            yield return baseConfig with { MinP = MathF.Round(baseConfig.MinP + step, 4) };

        // RepeatPenalty neighbors
        if (baseConfig.RepeatPenalty - step >= 1.0f)
            yield return baseConfig with { RepeatPenalty = MathF.Round(baseConfig.RepeatPenalty - step, 4) };
        if (baseConfig.RepeatPenalty + step <= 2.0f)
            yield return baseConfig with { RepeatPenalty = MathF.Round(baseConfig.RepeatPenalty + step, 4) };
    }

    /// <summary>
    /// Determines if a new score represents an improvement over the baseline.
    /// Uses a small epsilon to avoid noise-driven changes.
    /// </summary>
    public static bool IsImprovement(float baselineScore, float newScore, float epsilon = 0.5f) =>
        newScore > baselineScore + epsilon;
}
