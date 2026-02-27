using AutoInferenceBenchmark.Core;

namespace AutoInferenceBenchmark.Benchmark;

/// <summary>
/// Generates all <see cref="InferenceConfig"/> combinations from a <see cref="ParameterSweepConfig"/>.
/// </summary>
public static class ParameterSweepGenerator
{
    /// <summary>
    /// Generates all parameter configurations for the sweep.
    /// Returns them as a list for indexing and progress tracking.
    /// </summary>
    public static List<InferenceConfig> Generate(ParameterSweepConfig sweep)
    {
        return sweep.Mode switch
        {
            SweepMode.TemperatureOnly => GenerateTemperatureOnly(sweep),
            SweepMode.AllCombinations => GenerateAllCombinations(sweep),
            _ => throw new ArgumentException($"Unknown sweep mode: {sweep.Mode}")
        };
    }

    /// <summary>Calculates the total number of configs without generating them.</summary>
    public static int Count(ParameterSweepConfig sweep)
    {
        return sweep.Mode switch
        {
            SweepMode.TemperatureOnly => RangeCount(sweep.TemperatureMin, sweep.TemperatureMax, sweep.TemperatureStep),
            SweepMode.AllCombinations => CountAllCombinations(sweep),
            _ => 0
        };
    }

    private static List<InferenceConfig> GenerateTemperatureOnly(ParameterSweepConfig sweep)
    {
        var configs = new List<InferenceConfig>();
        foreach (var temp in FloatRange(sweep.TemperatureMin, sweep.TemperatureMax, sweep.TemperatureStep))
        {
            configs.Add(new InferenceConfig
            {
                Temperature = temp,
                TopP = sweep.DefaultTopP,
                TopK = sweep.DefaultTopK,
                MinP = sweep.DefaultMinP,
                RepeatPenalty = sweep.DefaultRepeatPenalty,
                FrequencyPenalty = sweep.DefaultFrequencyPenalty,
                PresencePenalty = sweep.DefaultPresencePenalty,
                MaxTokens = sweep.MaxTokens,
                Seed = sweep.DeterministicSeed ? sweep.Seed : 0
            });
        }
        return configs;
    }

    private static List<InferenceConfig> GenerateAllCombinations(ParameterSweepConfig sweep)
    {
        var temps = FloatRange(sweep.TemperatureMin, sweep.TemperatureMax, sweep.TemperatureStep).ToList();
        var topPs = FloatRange(sweep.TopPMin, sweep.TopPMax, sweep.TopPStep).ToList();
        var topKs = IntRange(sweep.TopKMin, sweep.TopKMax, sweep.TopKStep).ToList();
        var minPs = FloatRange(sweep.MinPMin, sweep.MinPMax, sweep.MinPStep).ToList();
        var repPens = FloatRange(sweep.RepeatPenaltyMin, sweep.RepeatPenaltyMax, sweep.RepeatPenaltyStep).ToList();

        var configs = new List<InferenceConfig>();

        foreach (var temp in temps)
        foreach (var topP in topPs)
        foreach (var topK in topKs)
        foreach (var minP in minPs)
        foreach (var repPen in repPens)
        {
            configs.Add(new InferenceConfig
            {
                Temperature = temp,
                TopP = topP,
                TopK = topK,
                MinP = minP,
                RepeatPenalty = repPen,
                FrequencyPenalty = sweep.DefaultFrequencyPenalty,
                PresencePenalty = sweep.DefaultPresencePenalty,
                MaxTokens = sweep.MaxTokens,
                Seed = sweep.DeterministicSeed ? sweep.Seed : 0
            });
        }

        return configs;
    }

    private static int CountAllCombinations(ParameterSweepConfig sweep) =>
        RangeCount(sweep.TemperatureMin, sweep.TemperatureMax, sweep.TemperatureStep)
        * RangeCount(sweep.TopPMin, sweep.TopPMax, sweep.TopPStep)
        * IntRangeCount(sweep.TopKMin, sweep.TopKMax, sweep.TopKStep)
        * RangeCount(sweep.MinPMin, sweep.MinPMax, sweep.MinPStep)
        * RangeCount(sweep.RepeatPenaltyMin, sweep.RepeatPenaltyMax, sweep.RepeatPenaltyStep);

    private static IEnumerable<float> FloatRange(float min, float max, float step)
    {
        if (step <= 0) { yield return min; yield break; }
        for (float v = min; v <= max + step * 0.001f; v += step)
            yield return MathF.Round(v, 4);
    }

    private static IEnumerable<int> IntRange(int min, int max, int step)
    {
        if (step <= 0) { yield return min; yield break; }
        for (int v = min; v <= max; v += step)
            yield return v;
    }

    private static int RangeCount(float min, float max, float step) =>
        step <= 0 ? 1 : (int)MathF.Floor((max - min) / step + 1.001f);

    private static int IntRangeCount(int min, int max, int step) =>
        step <= 0 ? 1 : (max - min) / step + 1;
}
