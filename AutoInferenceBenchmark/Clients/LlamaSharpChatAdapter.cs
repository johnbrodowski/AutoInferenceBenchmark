using System.Text;
using AutoInferenceBenchmark.Core;
using AutoInferenceBenchmark.Templates;
using LLama;
using LLama.Common;
using LLama.Sampling;
using LLama.Transformers;

namespace AutoInferenceBenchmark.Clients;

/// <summary>
/// Adapts <see cref="LLama.ChatSession"/> (InteractiveExecutor) into the
/// <see cref="IInferenceClient"/> interface for benchmark use.
/// Creates a fresh sampling pipeline per inference call to apply the exact config.
/// </summary>
public sealed class LlamaSharpChatAdapter : IInferenceClient
{
    private LLamaWeights? _model;
    private LLamaContext? _context;
    private ChatSession? _session;
    private string _loadedModelPath = "";
    private string _systemPrompt = "";
    private IPromptFormatter? _formatter;

    public bool IsLoaded => _model != null && _session != null;

    public async Task LoadModelAsync(string modelPath, string systemPrompt, int threads = 10, int contextSize = 4096)
    {
        if (_model != null && _loadedModelPath == modelPath && _session != null)
        {
            _systemPrompt = systemPrompt;
            return;
        }

        await Task.Run(() =>
        {
            DisposeModel();
            _loadedModelPath = modelPath;
            _systemPrompt = systemPrompt;

            var parameters = new ModelParams(modelPath)
            {
                ContextSize = (uint)contextSize,
                Threads = threads
            };

            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);

            // Detect template format from model metadata
            var format = ChatTemplateParser.DetectFormat(_model.Metadata);
            _formatter = ChatTemplateParser.GetFormatter(format);

            ResetSession();
        });
    }

    public async Task<InferenceResult> RunInferenceAsync(string prompt, InferenceConfig config, CancellationToken ct = default)
    {
        if (_session == null || _context == null)
            throw new InvalidOperationException("Model not loaded. Call LoadModelAsync first.");

        // Reset conversation for benchmark isolation
        ResetSession();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = cts.Token;

        var inferenceParams = new InferenceParams
        {
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = config.Temperature,
                TopP = config.TopP,
                TopK = config.TopK,
                MinP = config.MinP,
                RepeatPenalty = config.RepeatPenalty,
                FrequencyPenalty = config.FrequencyPenalty,
                PresencePenalty = config.PresencePenalty,
                Seed = config.Seed
            },
            MaxTokens = config.MaxTokens,
            AntiPrompts = new[] { "User:" }
        };

        var responseBuilder = new StringBuilder();
        int tokenCount = 0;
        var startTime = DateTime.UtcNow;
        DateTime? firstTokenTime = null;

        try
        {
            await Task.Run(async () =>
            {
                var pending = new StringBuilder();
                bool inFinalChannel = false;

                await foreach (var tok in _session.ChatAsync(
                    new ChatHistory.Message(AuthorRole.User, prompt), inferenceParams)
                    .WithCancellation(token))
                {
                    tokenCount++;
                    firstTokenTime ??= DateTime.UtcNow;

                    if (inFinalChannel)
                    {
                        responseBuilder.Append(tok);
                        continue;
                    }

                    pending.Append(tok);
                    var split = TrySplitAtFinalChannel(pending.ToString());
                    if (split.HasValue)
                    {
                        inFinalChannel = true;
                        if (split.Value.response.Length > 0)
                            responseBuilder.Append(split.Value.response);
                        pending.Clear();
                    }
                }

                if (!inFinalChannel && pending.Length > 0)
                    responseBuilder.Append(pending.ToString());
            }, token);
        }
        catch (OperationCanceledException) { }

        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
        var responseText = responseBuilder.ToString().Trim();
        return new InferenceResult
        {
            ResponseText = responseText,
            TokenCount = tokenCount,
            TokensPerSecond = elapsed > 0 ? tokenCount / (float)elapsed : 0f,
            TimeToFirstTokenSeconds = firstTokenTime.HasValue
                ? (float)(firstTokenTime.Value - startTime).TotalSeconds : 0f,
            TotalLatencySeconds = elapsed,
            Config = config
        };
    }

    public void ResetConversation(string systemPrompt)
    {
        _systemPrompt = systemPrompt;
        if (_model != null && _context != null)
            ResetSession();
    }

    public string? GetChatTemplate()
    {
        if (_model == null) return null;
        _model.Metadata.TryGetValue("tokenizer.chat_template", out var template);
        return template;
    }

    public IReadOnlyDictionary<string, string> GetModelMetadata() =>
        _model?.Metadata ?? new Dictionary<string, string>();

    /// <summary>Returns the auto-detected prompt formatter, or null if model not loaded.</summary>
    public IPromptFormatter? GetFormatter() => _formatter;
    private static (string thinking, string response)? TrySplitAtFinalChannel(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        int search = 0;
        while (search < text.Length)
        {
            int aIdx = text.IndexOf("assistant", search, StringComparison.OrdinalIgnoreCase);
            if (aIdx < 0) break;

            int fIdx = aIdx + "assistant".Length;
            while (fIdx < text.Length && char.IsWhiteSpace(text[fIdx]))
                fIdx++;

            bool finalFits = fIdx + 5 <= text.Length;
            bool finalMatch = finalFits && string.Compare(text, fIdx, "final", 0, 5, StringComparison.OrdinalIgnoreCase) == 0;
            bool finalEnd = !finalFits || fIdx + 5 >= text.Length || !char.IsLetter(text[fIdx + 5]);
            if (finalMatch && finalEnd)
            {
                int responseStart = fIdx + 5;
                while (responseStart < text.Length && char.IsWhiteSpace(text[responseStart]))
                    responseStart++;
                return (text[..aIdx].TrimEnd(), text[responseStart..]);
            }

            search = aIdx + 1;
        }

        search = 0;
        while (search < text.Length)
        {
            int fIdx = text.IndexOf("final", search, StringComparison.OrdinalIgnoreCase);
            if (fIdx < 0) break;

            bool validBefore = fIdx == 0 || !char.IsLetter(text[fIdx - 1]);
            bool validAfter = fIdx + 5 >= text.Length || !char.IsLetter(text[fIdx + 5]);
            if (validBefore && validAfter)
            {
                int responseStart = fIdx + 5;
                while (responseStart < text.Length && char.IsWhiteSpace(text[responseStart]))
                    responseStart++;
                return (text[..fIdx].TrimEnd(), text[responseStart..]);
            }

            search = fIdx + 1;
        }

        return null;
    }

    private void ResetSession()
    {
        var executor = new InteractiveExecutor(_context!);
        var chatHistory = new ChatHistory();
        if (!string.IsNullOrWhiteSpace(_systemPrompt))
            chatHistory.AddMessage(AuthorRole.System, _systemPrompt);

        _session = new ChatSession(executor, chatHistory);
        _session.WithHistoryTransform(new PromptTemplateTransformer(_model!, withAssistant: true));
        _session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            ["User:", "\ufffd"], redundancyLength: 5));
    }

    private void DisposeModel()
    {
        _session = null;
        _context?.Dispose();
        _context = null;
        _model?.Dispose();
        _model = null;
        _formatter = null;
    }

    public void Dispose() => DisposeModel();
}
