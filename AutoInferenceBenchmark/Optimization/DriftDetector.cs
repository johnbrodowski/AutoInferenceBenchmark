namespace AutoInferenceBenchmark.Optimization;

/// <summary>
/// Monitors performance trends across benchmark runs for the same model.
/// Detects when model performance drifts (e.g., after a model update or
/// GGUF re-quantization) and signals that re-exploration is needed.
///
/// <para>Detection strategy:
/// - Maintains a rolling window of recent benchmark scores.
/// - Computes mean and variance of the window.
/// - If a new score deviates beyond a configurable threshold (in standard deviations),
///   drift is flagged.</para>
///
/// <para>This is a future enhancement scaffold.</para>
/// </summary>
public sealed class DriftDetector
{
    private readonly Queue<float> _scoreWindow = new();
    private readonly int _windowSize;
    private readonly float _deviationThreshold;

    /// <summary>
    /// Creates a drift detector.
    /// </summary>
    /// <param name="windowSize">Number of recent scores to track.</param>
    /// <param name="deviationThreshold">Number of standard deviations that triggers a drift alert.</param>
    public DriftDetector(int windowSize = 10, float deviationThreshold = 2.0f)
    {
        _windowSize = windowSize;
        _deviationThreshold = deviationThreshold;
    }

    /// <summary>
    /// Records a new benchmark score and returns whether drift is detected.
    /// </summary>
    public DriftResult RecordScore(float score)
    {
        if (_scoreWindow.Count < 3)
        {
            _scoreWindow.Enqueue(score);
            return new DriftResult { IsDriftDetected = false, Score = score, WindowMean = score, WindowStdDev = 0 };
        }

        float mean = _scoreWindow.Average();
        float variance = _scoreWindow.Select(s => (s - mean) * (s - mean)).Average();
        float stdDev = MathF.Sqrt(variance);

        bool isDrift = stdDev > 0 && MathF.Abs(score - mean) > _deviationThreshold * stdDev;

        _scoreWindow.Enqueue(score);
        while (_scoreWindow.Count > _windowSize)
            _scoreWindow.Dequeue();

        return new DriftResult
        {
            IsDriftDetected = isDrift,
            Score = score,
            WindowMean = mean,
            WindowStdDev = stdDev,
            DeviationFromMean = MathF.Abs(score - mean) / (stdDev > 0 ? stdDev : 1f)
        };
    }

    /// <summary>Resets the score window.</summary>
    public void Reset() => _scoreWindow.Clear();
}

public sealed record DriftResult
{
    public bool IsDriftDetected { get; init; }
    public float Score { get; init; }
    public float WindowMean { get; init; }
    public float WindowStdDev { get; init; }
    public float DeviationFromMean { get; init; }
}
