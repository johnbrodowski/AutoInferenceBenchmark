namespace AutoInferenceBenchmark.Templates;

/// <summary>
/// Formats raw prompts into the model-specific chat template format.
/// </summary>
public interface IPromptFormatter
{
    /// <summary>The template format this formatter handles.</summary>
    TemplateFormat Format { get; }

    /// <summary>Formats a single user turn with an optional system prompt.</summary>
    string FormatPrompt(string? systemPrompt, string userMessage);

    /// <summary>Formats a multi-turn conversation.</summary>
    string FormatConversation(string? systemPrompt, IEnumerable<(string role, string content)> messages);
}
