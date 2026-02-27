using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// Gemma format:
/// &lt;start_of_turn&gt;user\n{content}&lt;end_of_turn&gt;\n&lt;start_of_turn&gt;model\n
/// Note: Gemma has no dedicated system role; system prompt is prepended to the first user message.
/// </summary>
public sealed class GemmaFormatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.Gemma;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var content = string.IsNullOrWhiteSpace(systemPrompt)
            ? userMessage
            : $"{systemPrompt}\n\n{userMessage}";
        return $"<start_of_turn>user\n{content}<end_of_turn>\n<start_of_turn>model\n";
    }

    public string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages)
    {
        var sb = new StringBuilder();
        bool first = true;
        foreach (var (role, content) in messages)
        {
            var gemmaRole = role == "assistant" ? "model" : role;
            var text = first && role == "user" && !string.IsNullOrWhiteSpace(systemPrompt)
                ? $"{systemPrompt}\n\n{content}"
                : content;
            sb.Append($"<start_of_turn>{gemmaRole}\n{text}<end_of_turn>\n");
            if (role == "user") first = false;
        }
        sb.Append("<start_of_turn>model\n");
        return sb.ToString();
    }
}
