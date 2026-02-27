namespace ApexUIBridge.Models;

public class AiSettings
{
    public string Provider { get; set; } = "LlamaSharp Instruct (Local)";
    public string ModelPath { get; set; } = "";
    public float Temperature { get; set; } = 0.8f;
    public string ReasoningEffort { get; set; } = "medium";
    public int MaxTokens { get; set; } = 2048;
    public int Threads { get; set; } = 10;
    public int ContextSize { get; set; } = 4096;
    public int GpuLayers { get; set; } = 0;
    public List<string> AntiPromptsChat { get; set; } = new() { "<|end|>", "<|start|>" };
    public List<string> AntiPromptsInstruct { get; set; } = new() { "[INST]" };
    public bool ShowThinking { get; set; } = false;
    public bool AutoExec { get; set; } = false;
    public string SystemPrompt { get; set; } = "";

    // Benchmark defaults
    public float BenchmarkTempMin { get; set; } = 0.1f;
    public float BenchmarkTempMax { get; set; } = 1.0f;
    public float BenchmarkTempStep { get; set; } = 0.1f;
}
