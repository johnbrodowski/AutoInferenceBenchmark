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

    /// <summary>Creates the built-in dataset with prefilled test cases across difficulty levels.</summary>
    public static TestDataset CreateDefault() => new()
    {
        Name = "Default",
        TestCases =
        [
            // ── General knowledge ──────────────────────────────────────
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
            },

            // ── JSON tool-call style ───────────────────────────────────
            new TestCase
            {
                Name = "JSON Tool Call - Weather",
                Difficulty = Difficulty.Medium,
                Prompt = "You are a function-calling assistant. The user says: \"What's the weather in Tokyo?\"\nRespond with a single JSON tool call object. Use this schema:\n{\"tool\": \"<name>\", \"args\": {\"key\": \"value\"}}\nOutput ONLY the JSON, no explanation.",
                ExpectedResponse = "{\"tool\": \"get_weather\", \"args\": {\"location\": \"Tokyo\"}}",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 55f
            },
            new TestCase
            {
                Name = "JSON Tool Call - Send Email",
                Difficulty = Difficulty.Medium,
                Prompt = "You are a function-calling assistant. The user says: \"Send an email to bob@example.com with subject 'Meeting' and body 'See you at 3pm'\"\nRespond with a single JSON tool call. Use this schema:\n{\"tool\": \"<name>\", \"args\": {\"key\": \"value\"}}\nOutput ONLY the JSON.",
                ExpectedResponse = "{\"tool\": \"send_email\", \"args\": {\"to\": \"bob@example.com\", \"subject\": \"Meeting\", \"body\": \"See you at 3pm\"}}",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 50f
            },
            new TestCase
            {
                Name = "JSON Tool Call - Multi-step",
                Difficulty = Difficulty.Complex,
                Prompt = "You are a function-calling assistant. The user says: \"Search for 'llama cpp' and save the first result\"\nRespond with a JSON array of tool calls in execution order. Schema per call:\n{\"tool\": \"<name>\", \"args\": {\"key\": \"value\"}}\nOutput ONLY the JSON array.",
                ExpectedResponse = "[{\"tool\": \"search\", \"args\": {\"query\": \"llama cpp\"}}, {\"tool\": \"save_result\", \"args\": {\"index\": 0}}]",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 40f
            },

            // ── UI automation CMD format ───────────────────────────────
            new TestCase
            {
                Name = "UI CMD - List Windows",
                Difficulty = Difficulty.Easy,
                Prompt = "You control Windows applications. Execute UI commands by outputting them in this format: [CMD: COMMAND args]\nRules: Only ONE [CMD:] per response. Never batch multiple commands.\n\nUser: Show me what windows are open.",
                ExpectedResponse = "I'll list the available windows.\n[CMD: LIST_WINDOWS]",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 60f
            },
            new TestCase
            {
                Name = "UI CMD - Scan Window",
                Difficulty = Difficulty.Easy,
                Prompt = "You control Windows applications. Execute UI commands by outputting them in this format: [CMD: COMMAND args]\nRules: Only ONE [CMD:] per response. You MUST scan a window before interacting with it. Never guess element IDs.\n\nUser: I want to click the Save button in Notepad.",
                ExpectedResponse = "I'll scan the Notepad window first to find the Save button.\n[CMD: SCAN_WINDOW Notepad]",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 55f
            },
            new TestCase
            {
                Name = "UI CMD - Click Element",
                Difficulty = Difficulty.Medium,
                Prompt = "You control Windows applications. Execute UI commands by outputting them in this format: [CMD: COMMAND args]\nRules: Only ONE [CMD:] per response. Only use IDs from scan results.\n\nYou previously scanned Notepad and the results included:\n  Button id=\"btn_save_42\" name=\"Save\"\n  Button id=\"btn_close_43\" name=\"Close\"\n  TextBox id=\"edit_1\" name=\"Text Editor\"\n\nUser: Click the Save button.",
                ExpectedResponse = "I can see the Save button. Clicking it now.\n[CMD: CLICK btn_save_42]",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 55f
            },
            new TestCase
            {
                Name = "UI CMD - Type Text",
                Difficulty = Difficulty.Medium,
                Prompt = "You control Windows applications. Execute UI commands by outputting them in this format: [CMD: COMMAND args]\nRules: Only ONE [CMD:] per response. Only use IDs from scan results.\n\nYou previously scanned Notepad and the results included:\n  Button id=\"btn_save_42\" name=\"Save\"\n  TextBox id=\"edit_1\" name=\"Text Editor\"\n\nUser: Type 'Hello World' into the text editor.",
                ExpectedResponse = "I'll type the text into the editor.\n[CMD: TYPE edit_1 Hello World]",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 55f
            },
            new TestCase
            {
                Name = "UI CMD - No Batching",
                Difficulty = Difficulty.Complex,
                Prompt = "You control Windows applications. Execute UI commands by outputting them in this format: [CMD: COMMAND args]\nRules: Only ONE [CMD:] per response. Never batch multiple commands. You MUST wait for [RESULT:] before issuing the next command.\n\nYou previously scanned Calculator and the results included:\n  Button id=\"btn_1\" name=\"One\"\n  Button id=\"btn_plus\" name=\"Plus\"\n  Button id=\"btn_2\" name=\"Two\"\n  Button id=\"btn_eq\" name=\"Equals\"\n\nUser: Calculate 1 + 2 for me.",
                ExpectedResponse = "I'll start by clicking the 1 button.\n[CMD: CLICK btn_1]",
                MatchMode = MatchMode.Similarity,
                SimilarityThreshold = 50f
            }
        ]
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOpts);
    public static TestDataset FromJson(string json) => JsonSerializer.Deserialize<TestDataset>(json, JsonOpts) ?? new();

    public void SaveToFile(string path) => File.WriteAllText(path, ToJson());
    public static TestDataset LoadFromFile(string path) => FromJson(File.ReadAllText(path));
}
