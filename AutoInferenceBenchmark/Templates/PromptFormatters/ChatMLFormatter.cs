using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// ChatML format used by many models (OpenAI-style, Qwen, DeepSeek, etc.).
/// &lt;|im_start|&gt;system\n{content}&lt;|im_end|&gt;
/// &lt;|im_start|&gt;user\n{content}&lt;|im_end|&gt;
/// &lt;|im_start|&gt;assistant\n
/// </summary>
public sealed class ChatMLFormatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.ChatML;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<|im_start|>system\n{systemPrompt}<|im_end|>\n");
        sb.Append($"<|im_start|>user\n{userMessage}<|im_end|>\n");
        sb.Append("<|im_start|>assistant\n");
        return sb.ToString();
    }

    public string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<|im_start|>system\n{systemPrompt}<|im_end|>\n");
        foreach (var (role, content) in messages)
            sb.Append($"<|im_start|>{role}\n{content}<|im_end|>\n");
        sb.Append("<|im_start|>assistant\n");
        return sb.ToString();
    }
}
