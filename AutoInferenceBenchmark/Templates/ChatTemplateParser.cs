namespace AutoInferenceBenchmark.Templates;

/// <summary>
/// Detects the chat template family from a GGUF model's <c>tokenizer.chat_template</c>
/// metadata string and returns the appropriate <see cref="IPromptFormatter"/>.
/// </summary>
public static class ChatTemplateParser
{
    /// <summary>
    /// Detects the template format from the raw Jinja2-style template string
    /// stored in GGUF metadata under the key <c>tokenizer.chat_template</c>.
    /// </summary>
    public static TemplateFormat DetectFormat(string? templateString)
    {
        if (string.IsNullOrWhiteSpace(templateString))
            return TemplateFormat.Unknown;

        var t = templateString;

        // Llama 3 — uses <|start_header_id|> / <|end_header_id|> / <|eot_id|>
        if (t.Contains("<|start_header_id|>", StringComparison.OrdinalIgnoreCase))
            return TemplateFormat.Llama3;

        // Phi — uses <|system|> / <|user|> / <|assistant|> / <|end|>
        if (t.Contains("<|system|>", StringComparison.OrdinalIgnoreCase) &&
            t.Contains("<|end|>", StringComparison.OrdinalIgnoreCase))
            return TemplateFormat.Phi;

        // Zephyr — uses <|system|> / <|user|> / <|assistant|> / </s>
        if (t.Contains("<|system|>", StringComparison.OrdinalIgnoreCase) &&
            t.Contains("</s>", StringComparison.OrdinalIgnoreCase))
            return TemplateFormat.Zephyr;

        // Gemma — uses <start_of_turn> / <end_of_turn>
        if (t.Contains("<start_of_turn>", StringComparison.OrdinalIgnoreCase))
            return TemplateFormat.Gemma;

        // Command R — uses <|START_OF_TURN_TOKEN|> / <|SYSTEM_TOKEN|>
        if (t.Contains("<|START_OF_TURN_TOKEN|>", StringComparison.Ordinal))
            return TemplateFormat.CommandR;

        // DeepSeek — uses <|begin▁of▁sentence|> or deepseek-specific patterns
        if (t.Contains("deepseek", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("<|begin▁of▁sentence|>", StringComparison.Ordinal))
            return TemplateFormat.DeepSeek;

        // Qwen — uses <|im_start|> but often has "qwen" in metadata or uses qwen-specific system handling
        // Note: Qwen uses ChatML format so we check for qwen-specific indicators first
        if (t.Contains("qwen", StringComparison.OrdinalIgnoreCase) &&
            t.Contains("<|im_start|>", StringComparison.OrdinalIgnoreCase))
            return TemplateFormat.Qwen;

        // ChatML — uses <|im_start|> / <|im_end|>
        if (t.Contains("<|im_start|>", StringComparison.OrdinalIgnoreCase))
            return TemplateFormat.ChatML;

        // Llama 2 — uses [INST] with <<SYS>> for system prompt
        if (t.Contains("[INST]", StringComparison.OrdinalIgnoreCase) &&
            t.Contains("<<SYS>>", StringComparison.OrdinalIgnoreCase))
            return TemplateFormat.Llama2;

        // Mistral — uses [INST] without <<SYS>>
        if (t.Contains("[INST]", StringComparison.OrdinalIgnoreCase))
            return TemplateFormat.Mistral;

        // Alpaca — uses ### Instruction / ### Response
        if (t.Contains("### Instruction", StringComparison.OrdinalIgnoreCase))
            return TemplateFormat.Alpaca;

        // Vicuna — uses USER: / ASSISTANT:
        if (t.Contains("USER:", StringComparison.OrdinalIgnoreCase) &&
            t.Contains("ASSISTANT:", StringComparison.OrdinalIgnoreCase))
            return TemplateFormat.Vicuna;

        return TemplateFormat.Unknown;
    }

    /// <summary>
    /// Detects template format from model metadata dictionary.
    /// Looks for the <c>tokenizer.chat_template</c> key.
    /// </summary>
    public static TemplateFormat DetectFormat(IReadOnlyDictionary<string, string> metadata)
    {
        metadata.TryGetValue("tokenizer.chat_template", out var template);
        return DetectFormat(template);
    }

    /// <summary>
    /// Returns the appropriate formatter for the given template format.
    /// </summary>
    public static IPromptFormatter GetFormatter(TemplateFormat format) => format switch
    {
        TemplateFormat.ChatML => new PromptFormatters.ChatMLFormatter(),
        TemplateFormat.Llama2 => new PromptFormatters.Llama2Formatter(),
        TemplateFormat.Llama3 => new PromptFormatters.Llama3Formatter(),
        TemplateFormat.Mistral => new PromptFormatters.MistralFormatter(),
        TemplateFormat.Phi => new PromptFormatters.PhiFormatter(),
        TemplateFormat.Gemma => new PromptFormatters.GemmaFormatter(),
        TemplateFormat.Alpaca => new PromptFormatters.AlpacaFormatter(),
        TemplateFormat.Vicuna => new PromptFormatters.VicunaFormatter(),
        TemplateFormat.Zephyr => new PromptFormatters.ZephyrFormatter(),
        TemplateFormat.CommandR => new PromptFormatters.CommandRFormatter(),
        TemplateFormat.DeepSeek => new PromptFormatters.ChatMLFormatter(), // DeepSeek uses ChatML-compatible format
        TemplateFormat.Qwen => new PromptFormatters.ChatMLFormatter(), // Qwen uses ChatML format
        _ => new PromptFormatters.GenericFormatter()
    };

    /// <summary>
    /// Convenience: detect format and return the formatter in one call.
    /// </summary>
    public static IPromptFormatter GetFormatter(string? templateString) =>
        GetFormatter(DetectFormat(templateString));

    /// <summary>
    /// Convenience: detect format from metadata and return the formatter.
    /// </summary>
    public static IPromptFormatter GetFormatter(IReadOnlyDictionary<string, string> metadata) =>
        GetFormatter(DetectFormat(metadata));
}
