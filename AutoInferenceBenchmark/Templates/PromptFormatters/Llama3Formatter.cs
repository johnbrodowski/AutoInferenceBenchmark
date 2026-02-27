using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// Llama 3 format:
/// &lt;|start_header_id|&gt;system&lt;|end_header_id|&gt;\n\n{content}&lt;|eot_id|&gt;
/// &lt;|start_header_id|&gt;user&lt;|end_header_id|&gt;\n\n{content}&lt;|eot_id|&gt;
/// &lt;|start_header_id|&gt;assistant&lt;|end_header_id|&gt;\n\n
/// </summary>
public sealed class Llama3Formatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.Llama3;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var sb = new StringBuilder();
        sb.Append("<|begin_of_text|>");
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<|start_header_id|>system<|end_header_id|>\n\n{systemPrompt}<|eot_id|>");
        sb.Append($"<|start_header_id|>user<|end_header_id|>\n\n{userMessage}<|eot_id|>");
        sb.Append("<|start_header_id|>assistant<|end_header_id|>\n\n");
        return sb.ToString();
    }

    public string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages)
    {
        var sb = new StringBuilder();
        sb.Append("<|begin_of_text|>");
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<|start_header_id|>system<|end_header_id|>\n\n{systemPrompt}<|eot_id|>");
        foreach (var (role, content) in messages)
            sb.Append($"<|start_header_id|>{role}<|end_header_id|>\n\n{content}<|eot_id|>");
        sb.Append("<|start_header_id|>assistant<|end_header_id|>\n\n");
        return sb.ToString();
    }
}
