using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// Fallback formatter for unrecognized templates.
/// Uses a simple role-prefixed format.
/// </summary>
public sealed class GenericFormatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.Unknown;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.AppendLine($"System: {systemPrompt}").AppendLine();
        sb.AppendLine($"User: {userMessage}");
        sb.Append("Assistant: ");
        return sb.ToString();
    }

    public string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.AppendLine($"System: {systemPrompt}").AppendLine();
        foreach (var (role, content) in messages)
        {
            var displayRole = char.ToUpper(role[0]) + role[1..];
            sb.AppendLine($"{displayRole}: {content}");
        }
        sb.Append("Assistant: ");
        return sb.ToString();
    }
}
