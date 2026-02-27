using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// Alpaca format:
/// ### Instruction:\n{system}\n\n{user}\n\n### Response:\n
/// </summary>
public sealed class AlpacaFormatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.Alpaca;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.AppendLine(systemPrompt).AppendLine();
        sb.AppendLine("### Instruction:");
        sb.AppendLine(userMessage);
        sb.AppendLine();
        sb.AppendLine("### Response:");
        return sb.ToString();
    }

    public string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.AppendLine(systemPrompt).AppendLine();
        foreach (var (role, content) in messages)
        {
            if (role == "user")
            {
                sb.AppendLine("### Instruction:");
                sb.AppendLine(content);
                sb.AppendLine();
            }
            else if (role == "assistant")
            {
                sb.AppendLine("### Response:");
                sb.AppendLine(content);
                sb.AppendLine();
            }
        }
        sb.AppendLine("### Response:");
        return sb.ToString();
    }
}
