using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// Phi-3/4 format:
/// &lt;|system|&gt;\n{content}&lt;|end|&gt;\n&lt;|user|&gt;\n{content}&lt;|end|&gt;\n&lt;|assistant|&gt;\n
/// </summary>
public sealed class PhiFormatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.Phi;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<|system|>\n{systemPrompt}<|end|>\n");
        sb.Append($"<|user|>\n{userMessage}<|end|>\n");
        sb.Append("<|assistant|>\n");
        return sb.ToString();
    }

    public string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<|system|>\n{systemPrompt}<|end|>\n");
        foreach (var (role, content) in messages)
        {
            sb.Append($"<|{role}|>\n{content}<|end|>\n");
        }
        sb.Append("<|assistant|>\n");
        return sb.ToString();
    }
}
