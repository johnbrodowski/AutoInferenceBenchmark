using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// Zephyr format:
/// &lt;|system|&gt;\n{content}&lt;/s&gt;\n&lt;|user|&gt;\n{content}&lt;/s&gt;\n&lt;|assistant|&gt;\n
/// </summary>
public sealed class ZephyrFormatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.Zephyr;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<|system|>\n{systemPrompt}</s>\n");
        sb.Append($"<|user|>\n{userMessage}</s>\n");
        sb.Append("<|assistant|>\n");
        return sb.ToString();
    }

    public string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<|system|>\n{systemPrompt}</s>\n");
        foreach (var (role, content) in messages)
            sb.Append($"<|{role}|>\n{content}</s>\n");
        sb.Append("<|assistant|>\n");
        return sb.ToString();
    }
}
