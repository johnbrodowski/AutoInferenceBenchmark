using System.Text.Json;

namespace AutoInferenceBenchmark.Core;

/// <summary>
/// Named collection of test cases. Serializable to/from JSON.
/// </summary>
public sealed class TestDataset
{
    public string Name { get; set; } = "Default";
    public List<TestCase> TestCases { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Creates the built-in dataset with 3 prefilled test cases (easy/medium/complex).</summary>
    public static TestDataset CreateDefault() => new()
    {
        Name = "Default",
        TestCases =
        [
            new TestCase
            {
                Name = "Simple Arithmetic",
                Difficulty = Difficulty.Easy,
                Prompt = "What is 2 + 2? Answer with just the number.",
                ExpectedResponse = "4",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 80f
            },
            new TestCase
            {
                Name = "Python Factorial",
                Difficulty = Difficulty.Medium,
                Prompt = "Write a Python function called 'factorial' that takes an integer n and returns its factorial. Return only the code, no explanation.",
                ExpectedResponse = "def factorial(n):\n    if n <= 1:\n        return 1\n    return n * factorial(n - 1)",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 60f
            },
            new TestCase
            {
                Name = "TCP vs UDP Comparison",
                Difficulty = Difficulty.Complex,
                Prompt = "Explain the key differences between TCP and UDP protocols. Include: connection type, reliability, ordering, speed, and typical use cases. Be concise.",
                ExpectedResponse = "TCP is connection-oriented, reliable, ordered, slower, used for web/email/file transfer. UDP is connectionless, unreliable, unordered, faster, used for streaming/gaming/DNS.",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 40f
            }
        ]
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOpts);
    public static TestDataset FromJson(string json) => JsonSerializer.Deserialize<TestDataset>(json, JsonOpts) ?? new();

    public void SaveToFile(string path) => File.WriteAllText(path, ToJson());
    public static TestDataset LoadFromFile(string path) => FromJson(File.ReadAllText(path));
}
