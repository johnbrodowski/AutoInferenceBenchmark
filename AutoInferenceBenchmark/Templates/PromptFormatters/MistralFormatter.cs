using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// Mistral/Instruct format: [INST] {user} [/INST]
/// System prompt is prepended to the first user message.
/// </summary>
public sealed class MistralFormatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.Mistral;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var content = string.IsNullOrWhiteSpace(systemPrompt)
            ? userMessage
            : $"{systemPrompt}\n\n{userMessage}";
        return $"[INST] {content} [/INST]";
    }

    public string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages)
    {
        var sb = new StringBuilder();
        bool first = true;
        foreach (var (role, content) in messages)
        {
            if (role == "user")
            {
                var text = first && !string.IsNullOrWhiteSpace(systemPrompt)
                    ? $"{systemPrompt}\n\n{content}"
                    : content;
                sb.Append($"[INST] {text} [/INST]");
                first = false;
            }
            else if (role == "assistant")
            {
                sb.Append($" {content}</s> ");
            }
        }
        return sb.ToString();
    }
}
