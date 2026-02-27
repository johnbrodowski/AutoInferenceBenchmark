using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// Command R (Cohere) format:
/// &lt;|START_OF_TURN_TOKEN|&gt;&lt;|SYSTEM_TOKEN|&gt;{content}&lt;|END_OF_TURN_TOKEN|&gt;
/// &lt;|START_OF_TURN_TOKEN|&gt;&lt;|USER_TOKEN|&gt;{content}&lt;|END_OF_TURN_TOKEN|&gt;
/// &lt;|START_OF_TURN_TOKEN|&gt;&lt;|CHATBOT_TOKEN|&gt;
/// </summary>
public sealed class CommandRFormatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.CommandR;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<|START_OF_TURN_TOKEN|><|SYSTEM_TOKEN|>{systemPrompt}<|END_OF_TURN_TOKEN|>");
        sb.Append($"<|START_OF_TURN_TOKEN|><|USER_TOKEN|>{userMessage}<|END_OF_TURN_TOKEN|>");
        sb.Append("<|START_OF_TURN_TOKEN|><|CHATBOT_TOKEN|>");
        return sb.ToString();
    }

    public string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<|START_OF_TURN_TOKEN|><|SYSTEM_TOKEN|>{systemPrompt}<|END_OF_TURN_TOKEN|>");
        foreach (var (role, content) in messages)
        {
            var token = role == "assistant" ? "<|CHATBOT_TOKEN|>" : "<|USER_TOKEN|>";
            sb.Append($"<|START_OF_TURN_TOKEN|>{token}{content}<|END_OF_TURN_TOKEN|>");
        }
        sb.Append("<|START_OF_TURN_TOKEN|><|CHATBOT_TOKEN|>");
        return sb.ToString();
    }
}
