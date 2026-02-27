using System.Text;
using AutoInferenceBenchmark.Core;
using AutoInferenceBenchmark.Templates;
using LLama;
using LLama.Common;
using LLama.Sampling;

namespace AutoInferenceBenchmark.Clients;

/// <summary>
/// Adapts <see cref="InstructExecutor"/> into the <see cref="IInferenceClient"/>
/// interface for benchmark use with instruct-format models.
/// </summary>
public sealed class LlamaSharpInstructAdapter : IInferenceClient
{
    private LLamaWeights? _model;
    private LLamaContext? _context;
    private InstructExecutor? _executor;
    private string _loadedModelPath = "";
    private string _systemPrompt = "";
    private IPromptFormatter? _formatter;

    public bool IsLoaded => _model != null && _executor != null;

    public async Task LoadModelAsync(string modelPath, string systemPrompt, int threads = 10, int contextSize = 4096)
    {
        if (_model != null && _loadedModelPath == modelPath && _executor != null)
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

            ResetExecutor();
        });
    }

    public async Task<InferenceResult> RunInferenceAsync(string prompt, InferenceConfig config, CancellationToken ct = default)
    {
        if (_executor == null || _context == null)
            throw new InvalidOperationException("Model not loaded. Call LoadModelAsync first.");

        // Reset for benchmark isolation
        ResetExecutor();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = cts.Token;

        // Format the prompt using the detected template
        var formattedPrompt = _formatter != null
            ? _formatter.FormatPrompt(_systemPrompt, prompt)
            : $"{_systemPrompt}\n\n{prompt}";

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
            AntiPrompts = new[] { "[INST]", "User:", "<|end|>", "<|im_end|>", "<|eot_id|>" }
        };

        var sb = new StringBuilder();
        int tokenCount = 0;
        var startTime = DateTime.UtcNow;
        DateTime? firstTokenTime = null;

        try
        {
            await Task.Run(async () =>
            {
                await foreach (var tok in _executor.InferAsync(formattedPrompt + Environment.NewLine, inferenceParams)
                    .WithCancellation(token))
                {
                    sb.Append(tok);
                    tokenCount++;
                    firstTokenTime ??= DateTime.UtcNow;
                }
            }, token);
        }
        catch (OperationCanceledException) { }

        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
        var responseText = ExtractFinalChannelResponse(sb.ToString().Trim());
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
            ResetExecutor();
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
    private static string ExtractFinalChannelResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

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
                return text[responseStart..].Trim();
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
                return text[responseStart..].Trim();
            }

            search = fIdx + 1;
        }

        return text;
    }

    private void ResetExecutor()
    {
        // Detect instruction tokens from template format
        var prefix = "[INST]";
        var suffix = "[/INST]";

        if (_formatter != null)
        {
            // Use simpler markers for non-Mistral/Llama2 formats since the formatter handles the full template
            switch (_formatter.Format)
            {
                case TemplateFormat.ChatML:
                case TemplateFormat.Qwen:
                case TemplateFormat.DeepSeek:
                    prefix = "<|im_start|>user\n";
                    suffix = "<|im_end|>\n<|im_start|>assistant\n";
                    break;
                case TemplateFormat.Phi:
                    prefix = "<|user|>\n";
                    suffix = "<|end|>\n<|assistant|>\n";
                    break;
                case TemplateFormat.Llama3:
                    prefix = "<|start_header_id|>user<|end_header_id|>\n\n";
                    suffix = "<|eot_id|><|start_header_id|>assistant<|end_header_id|>\n\n";
                    break;
            }
        }

        _executor = new InstructExecutor(_context!, prefix, suffix, null);
    }

    private void DisposeModel()
    {
        _executor = null;
        _context?.Dispose();
        _context = null;
        _model?.Dispose();
        _model = null;
        _formatter = null;
    }

    public void Dispose() => DisposeModel();
}
