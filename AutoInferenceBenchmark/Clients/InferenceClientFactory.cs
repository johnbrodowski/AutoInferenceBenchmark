using AutoInferenceBenchmark.Core;
using AutoInferenceBenchmark.Templates;

namespace AutoInferenceBenchmark.Clients;

/// <summary>
/// Creates the appropriate <see cref="IInferenceClient"/> adapter based on
/// the detected template format or explicit user choice.
/// </summary>
public static class InferenceClientFactory
{
    /// <summary>
    /// Creates a Chat adapter (InteractiveExecutor-backed).
    /// Best for models with multi-turn chat templates (ChatML, Llama3, Phi, Gemma, etc.).
    /// </summary>
    public static IInferenceClient CreateChat() => new LlamaSharpChatAdapter();

    /// <summary>
    /// Creates an Instruct adapter (InstructExecutor-backed).
    /// Best for models with instruct-style templates (Llama2, Mistral, Alpaca, Vicuna).
    /// </summary>
    public static IInferenceClient CreateInstruct() => new LlamaSharpInstructAdapter();

    /// <summary>
    /// Auto-selects the best adapter type based on the detected template format.
    /// Chat-oriented templates use InteractiveExecutor; instruct-oriented use InstructExecutor.
    /// </summary>
    public static IInferenceClient CreateAuto(TemplateFormat format) => format switch
    {
        TemplateFormat.Llama2 => CreateInstruct(),
        TemplateFormat.Mistral => CreateInstruct(),
        TemplateFormat.Alpaca => CreateInstruct(),
        TemplateFormat.Vicuna => CreateInstruct(),
        _ => CreateChat()
    };

    /// <summary>
    /// Loads a model, detects its template, and returns the best adapter — all in one call.
    /// </summary>
    public static async Task<IInferenceClient> CreateAndLoadAsync(
        string modelPath, string systemPrompt, int threads = 10, int contextSize = 4096)
    {
        // First, load with the chat adapter to read metadata
        var probeAdapter = new LlamaSharpChatAdapter();
        await probeAdapter.LoadModelAsync(modelPath, systemPrompt, threads, contextSize);

        var format = ChatTemplateParser.DetectFormat(probeAdapter.GetModelMetadata());

        // If the format is instruct-oriented, dispose the chat adapter and create an instruct one
        bool useInstruct = format is TemplateFormat.Llama2
            or TemplateFormat.Mistral
            or TemplateFormat.Alpaca
            or TemplateFormat.Vicuna;

        if (useInstruct)
        {
            probeAdapter.Dispose();
            var instructAdapter = new LlamaSharpInstructAdapter();
            await instructAdapter.LoadModelAsync(modelPath, systemPrompt, threads, contextSize);
            return instructAdapter;
        }

        return probeAdapter;
    }
}
