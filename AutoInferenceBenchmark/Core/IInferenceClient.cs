namespace AutoInferenceBenchmark.Core;

/// <summary>
/// Unified interface for local inference backends used by the benchmark engine.
/// </summary>
public interface IInferenceClient : IDisposable
{
    bool IsLoaded { get; }

    /// <summary>
    /// Loads the model from a GGUF file. Idempotent for the same path.
    /// </summary>
    Task LoadModelAsync(string modelPath, string systemPrompt, int threads = 10, int contextSize = 4096);

    /// <summary>
    /// Runs a single inference with the given configuration.
    /// The conversation is reset before each call for benchmark isolation.
    /// </summary>
    Task<InferenceResult> RunInferenceAsync(string prompt, InferenceConfig config, CancellationToken ct = default);

    /// <summary>
    /// Resets conversation state so each benchmark run starts clean.
    /// </summary>
    void ResetConversation(string systemPrompt);

    /// <summary>
    /// Returns the raw chat template string from the loaded model's metadata, or null.
    /// </summary>
    string? GetChatTemplate();

    /// <summary>
    /// Returns the full model metadata dictionary, or empty if not loaded.
    /// </summary>
    IReadOnlyDictionary<string, string> GetModelMetadata();
}
