namespace AutoInferenceBenchmark.Templates;

/// <summary>
/// Recognized GGUF model chat template families.
/// Detected from the model's <c>tokenizer.chat_template</c> metadata.
/// </summary>
public enum TemplateFormat
{
    /// <summary>Unknown template — uses GenericFormatter with raw substitution.</summary>
    Unknown,

    /// <summary>ChatML: &lt;|im_start|&gt;role\ncontent&lt;|im_end|&gt;</summary>
    ChatML,

    /// <summary>Llama 2: [INST] &lt;&lt;SYS&gt;&gt;\n...\n&lt;&lt;/SYS&gt;&gt;\n\n{user} [/INST]</summary>
    Llama2,

    /// <summary>Llama 3: &lt;|start_header_id|&gt;role&lt;|end_header_id|&gt;\n\ncontent&lt;|eot_id|&gt;</summary>
    Llama3,

    /// <summary>Mistral/Instruct: [INST] {user} [/INST]</summary>
    Mistral,

    /// <summary>Phi-3/4: &lt;|system|&gt;\ncontent&lt;|end|&gt;\n&lt;|user|&gt;\ncontent&lt;|end|&gt;\n&lt;|assistant|&gt;</summary>
    Phi,

    /// <summary>Gemma: &lt;start_of_turn&gt;user\ncontent&lt;end_of_turn&gt;</summary>
    Gemma,

    /// <summary>Alpaca: ### Instruction:\n{user}\n\n### Response:</summary>
    Alpaca,

    /// <summary>Vicuna: USER: {user}\nASSISTANT:</summary>
    Vicuna,

    /// <summary>Zephyr: &lt;|system|&gt;\ncontent&lt;/s&gt;\n&lt;|user|&gt;\ncontent&lt;/s&gt;\n&lt;|assistant|&gt;</summary>
    Zephyr,

    /// <summary>Command R (Cohere): &lt;|START_OF_TURN_TOKEN|&gt;&lt;|SYSTEM_TOKEN|&gt;content&lt;|END_OF_TURN_TOKEN|&gt;</summary>
    CommandR,

    /// <summary>DeepSeek: pattern similar to ChatML but with deepseek-specific tokens.</summary>
    DeepSeek,

    /// <summary>Qwen: Uses ChatML-like template with qwen-specific tokens.</summary>
    Qwen
}
