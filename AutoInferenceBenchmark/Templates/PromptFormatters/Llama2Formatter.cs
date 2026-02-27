using System.Text;

namespace AutoInferenceBenchmark.Templates.PromptFormatters;

/// <summary>
/// Llama 2 format: [INST] &lt;&lt;SYS&gt;&gt;\n{system}\n&lt;&lt;/SYS&gt;&gt;\n\n{user} [/INST]
/// </summary>
public sealed class Llama2Formatter : IPromptFormatter
{
    public TemplateFormat Format => TemplateFormat.Llama2;

    public string FormatPrompt(string? systemPrompt, string userMessage)
    {
        var sb = new StringBuilder();
        sb.Append("[INST] ");
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            sb.Append($"<<SYS>>\n{systemPrompt}\n<</SYS>>\n\n");
        sb.Append($"{userMessage} [/INST]");
        return sb.ToString();
    }

    public string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages)
    {
        var sb = new StringBuilder();
        bool first = true;
        foreach (var (role, content) in messages)
        {
            if (role == "user")
            {
                sb.Append("[INST] ");
                if (first && !string.IsNullOrWhiteSpace(systemPrompt))
                    sb.Append($"<<SYS>>\n{systemPrompt}\n<</SYS>>\n\n");
                sb.Append($"{content} [/INST]");
                first = false;
            }
            else if (role == "assistant")
            {
                sb.Append($" {content} </s>");
            }
        }
        return sb.ToString();
    }
}
