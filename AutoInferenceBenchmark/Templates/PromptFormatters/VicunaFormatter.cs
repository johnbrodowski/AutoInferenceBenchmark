using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// Vicuna format:
/// {system}\n\nUSER: {user}\nASSISTANT:
/// </summary>
public sealed class VicunaFormatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.Vicuna;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.AppendLine(systemPrompt).AppendLine();
        sb.Append($"USER: {userMessage}\nASSISTANT:");
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
                sb.Append($"USER: {content}\n");
            else if (role == "assistant")
                sb.Append($"ASSISTANT: {content}</s>\n");
        }
        sb.Append("ASSISTANT:");
        return sb.ToString();
    }
}
