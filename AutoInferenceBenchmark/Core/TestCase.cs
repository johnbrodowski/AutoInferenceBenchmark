namespace AutoInferenceBenchmark.Core;

public enum Difficulty { Easy, Medium, Complex }
public enum MatchMode { Exact, Similarity }

/// <summary>
/// A single benchmark test case: prompt in, expected response out.
/// </summary>
public sealed class TestCase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public Difficulty Difficulty { get; set; } = Difficulty.Easy;
    public string Prompt { get; set; } = "";
    public string ExpectedResponse { get; set; } = "";
    public MatchMode MatchMode { get; set; } = MatchMode.Similarity;

    /// <summary>Minimum similarity percentage required to pass (0–100). Only used when MatchMode is Similarity.</summary>
    public float SimilarityThreshold { get; set; } = 70f;
}
