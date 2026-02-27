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
            AntiPrompts = GetAntiPrompts()
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

                await foreach (var tok in _executor.InferAsync(formattedPrompt + Environment.NewLine, inferenceParams)
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
        var responseText = StripThinkingTags(responseBuilder.ToString().Trim());
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

    /// <summary>
    /// Returns template-specific anti-prompts that won't false-trigger on thinking content.
    /// </summary>
    private string[] GetAntiPrompts()
    {
        if (_formatter == null)
            return new[] { "[INST]" };

        return _formatter.Format switch
        {
            TemplateFormat.Llama2 or TemplateFormat.Mistral
                => new[] { "[INST]" },
            TemplateFormat.Alpaca
                => new[] { "### Instruction:", "### Response:" },
            TemplateFormat.Vicuna
                => new[] { "USER:", "ASSISTANT:" },
            TemplateFormat.ChatML or TemplateFormat.Qwen or TemplateFormat.DeepSeek
                => new[] { "<|im_start|>user", "<|im_end|>" },
            TemplateFormat.Llama3
                => new[] { "<|eot_id|>" },
            TemplateFormat.Phi
                => new[] { "<|end|>", "<|user|>" },
            TemplateFormat.Gemma
                => new[] { "<end_of_turn>" },
            TemplateFormat.Zephyr
                => new[] { "</s>", "<|user|>" },
            TemplateFormat.CommandR
                => new[] { "<|END_OF_TURN_TOKEN|>" },
            _ => new[] { "[INST]" }
        };
    }

    /// <summary>
    /// Splits streaming text at a thinking/response boundary.
    /// Detects: &lt;think&gt;...&lt;/think&gt;, &lt;|thinking|&gt;...&lt;|/thinking|&gt;,
    /// and the "assistant final" / "final" keyword markers.
    /// </summary>
    private static (string thinking, string response)? TrySplitAtFinalChannel(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // ── Check for </think> close tag (DeepSeek R1, QwQ, etc.) ──
        int thinkClose = text.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);
        if (thinkClose >= 0)
        {
            var thinking = text[..thinkClose];
            var response = text[(thinkClose + "</think>".Length)..];
            int thinkOpen = thinking.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
            if (thinkOpen >= 0)
                thinking = thinking[(thinkOpen + "<think>".Length)..];
            return (thinking.Trim(), response.TrimStart());
        }

        // ── Check for <|/thinking|> close tag ──
        int thinkClose2 = text.IndexOf("<|/thinking|>", StringComparison.OrdinalIgnoreCase);
        if (thinkClose2 >= 0)
        {
            var thinking = text[..thinkClose2];
            var response = text[(thinkClose2 + "<|/thinking|>".Length)..];
            int thinkOpen2 = thinking.IndexOf("<|thinking|>", StringComparison.OrdinalIgnoreCase);
            if (thinkOpen2 >= 0)
                thinking = thinking[(thinkOpen2 + "<|thinking|>".Length)..];
            return (thinking.Trim(), response.TrimStart());
        }

        // ── Check for "assistant final" keyword ──
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

        // ── Check for standalone "final" keyword ──
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

    /// <summary>
    /// Post-processing safety net: strips any remaining thinking blocks from the response.
    /// </summary>
    private static string StripThinkingTags(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? "";

        var result = text;

        // Strip <think>...</think> blocks
        while (true)
        {
            int openIdx = result.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
            if (openIdx < 0) break;

            int closeIdx = result.IndexOf("</think>", openIdx, StringComparison.OrdinalIgnoreCase);
            if (closeIdx < 0)
            {
                result = result[..openIdx];
                break;
            }

            result = result[..openIdx] + result[(closeIdx + "</think>".Length)..];
        }

        // Strip <|thinking|>...<|/thinking|> blocks
        while (true)
        {
            int openIdx = result.IndexOf("<|thinking|>", StringComparison.OrdinalIgnoreCase);
            if (openIdx < 0) break;

            int closeIdx = result.IndexOf("<|/thinking|>", openIdx, StringComparison.OrdinalIgnoreCase);
            if (closeIdx < 0)
            {
                result = result[..openIdx];
                break;
            }

            result = result[..openIdx] + result[(closeIdx + "<|/thinking|>".Length)..];
        }

        return result.Trim();
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
