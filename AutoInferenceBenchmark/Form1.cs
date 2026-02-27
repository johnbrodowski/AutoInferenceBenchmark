using ApexUIBridge.Models;

using AutoInferenceBenchmark.Core;
using AutoInferenceBenchmark.Forms;
using AutoInferenceBenchmark.LlamaSharpAI;

using AutoInferenceBenchmark.Settings;

using System.Diagnostics;
using System.Text;

namespace AutoInferenceBenchmark
{
    public partial class Form1 : Form
    {
        // System stats timer
        private readonly System.Windows.Forms.Timer _sysStatsTimer = new() { Interval = 1500 };

        private TimeSpan _lastCpuTime = TimeSpan.Zero;
        private DateTime _lastCpuSample = DateTime.UtcNow;

        private readonly List<Message> _aiConversationHistory = new();

        private LlamaSharpClient? _llamaClient;
        private LlamaSharpInstructClient? _llamaInstructClient;

        private AiSettings _aiSettings = new();

        private BenchmarkPanel? _benchmarkPanel;

        public Form1()
        {
            InitializeComponent();

            // Add BenchmarkPanel to the benchmark tab
            _benchmarkPanel = new BenchmarkPanel
            {
                GetModelPath = () => _aiModelPathBox.Text.Trim(),
                GetSystemPrompt = () => _aiSystemBox.Text,
                GetThreads = () => _aiSettings.Threads,
                GetContextSize = () => _aiSettings.ContextSize
            };
            _benchmarkPanel.ApplyConfigRequested += OnApplyBenchmarkConfig;
            _benchmarkTabPage.Controls.Add(_benchmarkPanel);

            WireEvents();

            var path = Path.GetDirectoryName(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                             "ApexUIBridge", "ai-settings.json"))!;

            // Ensure settings directory exists and load settings
            Directory.CreateDirectory(path);



            _aiSettings = _aiSettingsService.Load();

            // Apply loaded settings to UI
            ApplySettingsToUI();

            SetupProviderCombo();
        }

        private void OnApplyBenchmarkConfig(object? sender, InferenceConfig config)
        {
            _aiSettings.Temperature = config.Temperature;
            SaveSettings();
            ApplySettingsToUI();
            _aiStatusLabel.Text = $"Applied benchmark config: {config.ToShortString()}";
        }

        private void SetupProviderCombo()
        {
            // Wire events — controls themselves are declared in Designer.cs
            _aiProviderCombo.SelectedIndexChanged += OnProviderChanged;

            _btnAiBrowse.Click += (_, _) =>
            {
                using var dlg = new OpenFileDialog
                {
                    Title = "Select GGUF Model File",
                    Filter = "GGUF Models (*.gguf)|*.gguf|All Files (*.*)|*.*",
                    CheckFileExists = true
                };
                if (!string.IsNullOrEmpty(_aiModelPathBox.Text))
                    dlg.InitialDirectory = Path.GetDirectoryName(_aiModelPathBox.Text) ?? "";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _aiModelPathBox.Text = dlg.FileName;
                    _aiSettings.ModelPath = dlg.FileName;
                    SaveSettings();
                }
            };

            _aiModelPathBox.Leave += (_, _) =>
            {
                _aiSettings.ModelPath = _aiModelPathBox.Text.Trim();
                SaveSettings();
            };

            // Select provider from saved settings, then update visibility
            int provIdx = _aiProviderCombo.Items.IndexOf(_aiSettings.Provider);
            _aiProviderCombo.SelectedIndex = provIdx >= 0 ? provIdx : 2; // default: LlamaSharp Instruct
                                                                         // SelectedIndex setter fires OnProviderChanged, so visibility is already set
        }

        private void OnProviderChanged(object? sender, EventArgs e)
        {
            var provider = _aiProviderCombo.SelectedItem?.ToString() ?? _aiSettings.Provider;
            bool isLlama = provider is "LlamaSharp (Local)" or "LlamaSharp Instruct";

            // Persist provider change immediately so it survives unexpected exits
            SyncUIToSettings();
            SaveSettings();
        }

        private void ApplySettingsToUI()
        {
            // Model path (LlamaSharp)
            _aiModelPathBox.Text = _aiSettings.ModelPath;

            // System prompt
            _aiSystemBox.Text = string.IsNullOrEmpty(_aiSettings.SystemPrompt)
                ? "You are a helpful assistant."
                : _aiSettings.SystemPrompt;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AiSetIndicatorState(IndicatorState.Unloaded);
        }

        private async Task AiSendLlamaMessageAsync()
        {
            var modelPath = _aiModelPathBox.Text.Trim();
            if (string.IsNullOrEmpty(modelPath))
            {
                MessageBox.Show(this, "Please enter the path to your GGUF model file.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(modelPath))
            {
                MessageBox.Show(this, $"Model file not found:\n{modelPath}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var userText = _aiInput.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            AiSetUIEnabled(false);
            _aiInput.Clear();
            AiAppendOutput($"\nUser: {userText}\nAI: ");
            _aiConversationHistory.Add(Message.CreateUserMessage(userText));

            try
            {
                _llamaClient ??= new LlamaSharpClient();
                _llamaClient.Temperature = _aiSettings.Temperature;
                _llamaClient.ReasoningEffort = _aiSettings.ReasoningEffort;
                _llamaClient.MaxTokens = _aiSettings.MaxTokens;
                _llamaClient.Threads = _aiSettings.Threads;
                _llamaClient.ContextSize = _aiSettings.ContextSize;
                _llamaClient.GpuLayers = _aiSettings.GpuLayers;
                if (_aiSettings.AntiPromptsChat.Count > 0)
                    _llamaClient.AntiPrompts = _aiSettings.AntiPromptsChat;

                AiSetIndicatorState(_llamaClient.IsLoaded ? IndicatorState.Generating : IndicatorState.Loading);
                _aiStatusLabel.Text = _llamaClient.IsLoaded ? "Preparing..." : "Loading model...";
                await _llamaClient.LoadModelAsync(modelPath, _aiSystemBox.Text);
                AiSetIndicatorState(IndicatorState.Generating);

                const int maxTurns = 20;
                var currentInput = userText;

                for (int turn = 0; turn < maxTurns; turn++)
                {
                    _aiStatusLabel.Text = "Generating...";

                    EventHandler<string> streamHandler = (_, text) => AiAppendOutput(text);
                    EventHandler<string> thinkingHandler = (_, text) => AiAppendThinking(text);
                    EventHandler<int> completeHandler = (_, count) =>
                    {
                        if (InvokeRequired) Invoke(() => UpdateStatsLabel(_llamaClient));
                        else UpdateStatsLabel(_llamaClient);
                    };

                    _llamaClient.StreamingTextReceived += streamHandler;
                    _llamaClient.ThinkingReceived += thinkingHandler;
                    _llamaClient.GenerationCompleted += completeHandler;

                    string responseText;
                    try
                    {
                        responseText = await _llamaClient.SendMessageAsync(currentInput);
                    }
                    finally
                    {
                        _llamaClient.StreamingTextReceived -= streamHandler;
                        _llamaClient.ThinkingReceived -= thinkingHandler;
                        _llamaClient.GenerationCompleted -= completeHandler;
                    }

                    _aiConversationHistory.Add(Message.CreateAssistantMessage(responseText));
                    break;
                }
            }
            catch (Exception ex)
            {
                AiAppendOutput($"\n[ERROR] {ex.Message}\n");
                _aiStatusLabel.Text = "Error";
            }
            finally
            {
                AiSetUIEnabled(true);
                AiSetIndicatorState(_llamaClient?.IsLoaded ?? false
                    ? IndicatorState.Loaded : IndicatorState.Unloaded);
                _aiInput.Focus();
            }
        }

        private void AiSetUIEnabled(bool enabled)
        {
            _btnAiSend.Enabled = enabled;
            _btnAiStream.Enabled = enabled;
            _btnAiLoad.Enabled = enabled;
            _aiInput.Enabled = enabled;
        }

        private void AiAppendOutput(string text)
        {
            if (InvokeRequired) { Invoke(() => AiAppendOutput(text)); return; }

            _aiOutput.SelectionColor = Color.DarkGreen;
            _aiOutput.AppendText(text.Replace("\r\n", "\n").Replace("\n", "\r\n"));
            _aiOutput.ScrollToCaret();
        }

        private void AiAppendThinking(string text)
        {
            if (InvokeRequired) { Invoke(() => AiAppendThinking(text)); return; }
            if (!_aiSettings.ShowThinking) return;

            _aiOutput.SelectionColor = Color.DarkBlue;
            _aiOutput.AppendText(text.Replace("\r\n", "\n").Replace("\n", "\r\n"));
            _aiOutput.ScrollToCaret();
        }

        private async Task AiSendLlamaInstructMessageAsync()
        {
            var modelPath = _aiModelPathBox.Text.Trim();
            if (string.IsNullOrEmpty(modelPath))
            {
                MessageBox.Show(this, "Please enter the path to your GGUF model file.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(modelPath))
            {
                MessageBox.Show(this, $"Model file not found:\n{modelPath}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var userText = _aiInput.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            AiSetUIEnabled(false);
            _aiInput.Clear();
            AiAppendOutput($"\nUser: {userText}\nAI: ");
            _aiConversationHistory.Add(Message.CreateUserMessage(userText));

            try
            {
                _llamaInstructClient ??= new LlamaSharpInstructClient();
                _llamaInstructClient.Temperature = _aiSettings.Temperature;
                _llamaInstructClient.ReasoningEffort = _aiSettings.ReasoningEffort;
                _llamaInstructClient.MaxTokens = _aiSettings.MaxTokens;
                _llamaInstructClient.Threads = _aiSettings.Threads;
                _llamaInstructClient.ContextSize = _aiSettings.ContextSize;
                _llamaInstructClient.GpuLayers = _aiSettings.GpuLayers;
                if (_aiSettings.AntiPromptsInstruct.Count > 0)
                    _llamaInstructClient.AntiPrompts = _aiSettings.AntiPromptsInstruct;

                AiSetIndicatorState(_llamaInstructClient.IsLoaded ? IndicatorState.Generating : IndicatorState.Loading);
                _aiStatusLabel.Text = _llamaInstructClient.IsLoaded ? "Preparing..." : "Loading model...";
                await _llamaInstructClient.LoadModelAsync(modelPath, _aiSystemBox.Text);
                AiSetIndicatorState(IndicatorState.Generating);

                const int maxTurns = 20;
                var currentInput = userText;

                for (int turn = 0; turn < maxTurns; turn++)
                {
                    _aiStatusLabel.Text = "Generating...";

                    EventHandler<string> streamHandler = (_, text) => AiAppendOutput(text);
                    EventHandler<string> thinkingHandler = (_, text) => AiAppendThinking(text);
                    EventHandler<int> completeHandler = (_, count) =>
                    {
                        if (InvokeRequired) Invoke(() => UpdateStatsLabel(_llamaInstructClient));
                        else UpdateStatsLabel(_llamaInstructClient);
                    };

                    _llamaInstructClient.StreamingTextReceived += streamHandler;
                    _llamaInstructClient.ThinkingReceived += thinkingHandler;
                    _llamaInstructClient.GenerationCompleted += completeHandler;

                    string responseText;
                    try
                    {
                        responseText = await _llamaInstructClient.SendMessageAsync(currentInput);
                    }
                    finally
                    {
                        _llamaInstructClient.StreamingTextReceived -= streamHandler;
                        _llamaInstructClient.ThinkingReceived -= thinkingHandler;
                        _llamaInstructClient.GenerationCompleted -= completeHandler;
                    }

                    _aiConversationHistory.Add(Message.CreateAssistantMessage(responseText));
                    AiAppendOutput("\n");
                    break;
                }
            }
            catch (Exception ex)
            {
                AiAppendOutput($"\n[ERROR] {ex.Message}\n");
                _aiStatusLabel.Text = "Error";
            }
            finally
            {
                AiSetUIEnabled(true);
                AiSetIndicatorState(_llamaInstructClient?.IsLoaded ?? false
                    ? IndicatorState.Loaded : IndicatorState.Unloaded);
                _aiInput.Focus();
            }
        }

        private void UpdateStatsLabel(LlamaSharpClient client)
        {
            _aiStatusLabel.Text = $"Done — {client.LastTokensPerSecond:F1} t/s";
            _aiStatsLabel.Text = $"{client.LastTokensPerSecond:F1} t/s  TTFT {client.LastTimeToFirstToken:F2}s";
            FadeStatsLabelColor(client.LastTokensPerSecond);
        }

        private void UpdateStatsLabel(LlamaSharpInstructClient client)
        {
            _aiStatusLabel.Text = $"Done — {client.LastTokensPerSecond:F1} t/s";
            _aiStatsLabel.Text = $"{client.LastTokensPerSecond:F1} t/s  TTFT {client.LastTimeToFirstToken:F2}s";
            FadeStatsLabelColor(client.LastTokensPerSecond);
        }

        private void FadeStatsLabelColor(float tps)
        {
            var target = tps > 15f ? Color.LimeGreen
                       : tps > 5f  ? Color.DarkOrange
                                    : Color.OrangeRed;
            _ = ColorFader.FadeForeAsync(_aiStatsLabel, _aiStatsLabel.ForeColor, target, durationMs: 400);
        }

        private void AiClearChat()
        {
            _aiConversationHistory.Clear();
            _llamaClient?.ResetConversation(_aiSystemBox.Text);
            _llamaInstructClient?.ResetConversation(_aiSystemBox.Text);
            _aiOutput.Clear();
            _aiStatusLabel.Text = "Chat cleared";
            _aiStatsLabel.Text = "";

            var provider = _aiProviderCombo.SelectedItem?.ToString() ?? "";
            bool isLoaded = provider == "LlamaSharp Instruct (Local)"
                ? _llamaInstructClient?.IsLoaded ?? false
                : _llamaClient?.IsLoaded ?? false;
            AiSetIndicatorState(isLoaded ? IndicatorState.Loaded : IndicatorState.Unloaded);
        }

        private async Task AiLoadOrUnloadModelAsync()
        {
            var provider = _aiProviderCombo.SelectedItem?.ToString() ?? "";
            bool useInstruct = provider == "LlamaSharp Instruct (Local)";

            bool isLoaded = useInstruct
                ? _llamaInstructClient?.IsLoaded ?? false
                : _llamaClient?.IsLoaded ?? false;

            if (isLoaded)
            {
                if (useInstruct) { _llamaInstructClient?.Dispose(); _llamaInstructClient = null; }
                else             { _llamaClient?.Dispose(); _llamaClient = null; }

                _aiStatusLabel.Text = "Model unloaded";
                AiSetIndicatorState(IndicatorState.Unloaded);
                return;
            }

            var modelPath = _aiModelPathBox.Text.Trim();
            if (string.IsNullOrEmpty(modelPath))
            {
                MessageBox.Show(this, "Please enter the path to your GGUF model file.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!File.Exists(modelPath))
            {
                MessageBox.Show(this, $"Model file not found:\n{modelPath}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AiSetUIEnabled(false);
            AiSetIndicatorState(IndicatorState.Loading);
            _aiStatusLabel.Text = "Loading model...";

            try
            {
                SyncUIToSettings();
                if (useInstruct)
                {
                    _llamaInstructClient ??= new LlamaSharpInstructClient();
                    _llamaInstructClient.Temperature = _aiSettings.Temperature;
                    _llamaInstructClient.ReasoningEffort = _aiSettings.ReasoningEffort;
                    _llamaInstructClient.MaxTokens = _aiSettings.MaxTokens;
                    _llamaInstructClient.Threads = _aiSettings.Threads;
                    _llamaInstructClient.ContextSize = _aiSettings.ContextSize;
                    _llamaInstructClient.GpuLayers = _aiSettings.GpuLayers;
                    if (_aiSettings.AntiPromptsInstruct.Count > 0)
                        _llamaInstructClient.AntiPrompts = _aiSettings.AntiPromptsInstruct;
                    await _llamaInstructClient.LoadModelAsync(modelPath, _aiSystemBox.Text);
                }
                else
                {
                    _llamaClient ??= new LlamaSharpClient();
                    _llamaClient.Temperature = _aiSettings.Temperature;
                    _llamaClient.ReasoningEffort = _aiSettings.ReasoningEffort;
                    _llamaClient.MaxTokens = _aiSettings.MaxTokens;
                    _llamaClient.Threads = _aiSettings.Threads;
                    _llamaClient.ContextSize = _aiSettings.ContextSize;
                    _llamaClient.GpuLayers = _aiSettings.GpuLayers;
                    if (_aiSettings.AntiPromptsChat.Count > 0)
                        _llamaClient.AntiPrompts = _aiSettings.AntiPromptsChat;
                    await _llamaClient.LoadModelAsync(modelPath, _aiSystemBox.Text);
                }

                _aiStatusLabel.Text = "Model ready";
                AiSetIndicatorState(IndicatorState.Loaded);
            }
            catch (Exception ex)
            {
                AiAppendOutput($"\n[ERROR] {ex.Message}\n");
                _aiStatusLabel.Text = "Load failed";
                AiSetIndicatorState(IndicatorState.Unloaded);
            }
            finally
            {
                AiSetUIEnabled(true);
            }
        }

        private void AiSetIndicatorState(IndicatorState state)
        {
            if (InvokeRequired) { Invoke(() => AiSetIndicatorState(state)); return; }

            _indicatorState = state;

            // Cancel and clean up any running animation
            _indicatorCts?.Cancel();
            _indicatorCts?.Dispose();
            _indicatorCts = null;

            _btnAiStop.Enabled = state == IndicatorState.Generating;
            _btnAiLoad.Enabled = state != IndicatorState.Generating;
            _btnAiLoad.Text = state switch
            {
                IndicatorState.Loading                              => "Loading…",
                IndicatorState.Loaded or IndicatorState.Generating => "Unload",
                _                                                   => "Load"
            };
            _aiIndicatorLabel.Text = state switch
            {
                IndicatorState.Unloaded   => "● unloaded",
                IndicatorState.Loading    => "● loading…",
                IndicatorState.Loaded     => "● ready",
                IndicatorState.Generating => "● active",
                _                         => "●"
            };

            switch (state)
            {
                case IndicatorState.Unloaded:
                    _indicatorCts = ColorFader.PulseFore(_aiIndicatorLabel,
                        Color.FromArgb(160, 0, 0), Color.Red, halfPeriodMs: 1200, steps: 30);
                    break;

                case IndicatorState.Loading:
                    _indicatorCts = ColorFader.PulseFore(_aiIndicatorLabel,
                        Color.DarkOrange, Color.Gold, halfPeriodMs: 500, steps: 20);
                    break;

                case IndicatorState.Loaded:
                    var loadedCts = new CancellationTokenSource();
                    _indicatorCts = loadedCts;
                    _ = ColorFader.FadeForeAsync(_aiIndicatorLabel,
                        _aiIndicatorLabel.ForeColor, Color.LimeGreen,
                        durationMs: 500, ct: loadedCts.Token);
                    break;

                case IndicatorState.Generating:
                    _indicatorCts = ColorFader.PulseFore(_aiIndicatorLabel,
                        Color.Green, Color.LimeGreen, halfPeriodMs: 400, steps: 16);
                    break;
            }
        }

        private void WireEvents()
        {
            _btnAiSettings.Click += (_, _) => OpenAiSettingsDialog();
            _btnAiLoad.Click += async (_, _) => await AiLoadOrUnloadModelAsync();

            // AI Chat events
            _btnAiSend.Click += async (_, _) => await AiSendMessageAsync(stream: false);
            _btnAiStream.Click += async (_, _) => await AiSendMessageAsync(stream: true);
            _btnAiClear.Click += (_, _) => AiClearChat();
            _btnAiStop.Click += (_, _) =>
            {
                _llamaClient?.Stop();
                _llamaInstructClient?.Stop();
                _aiStatusLabel.Text = "Stopped";
            };
            _aiInput.KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _ = AiSendMessageAsync(stream: e.Shift);
                }
            };

            // System stats timer
            _sysStatsTimer.Tick += (_, _) => UpdateSysStats();
            _sysStatsTimer.Start();

            FormClosing += (_, _) =>
            {
                // Save before dispose — if Dispose throws, settings must already be persisted
                SyncUIToSettings();
                SaveSettings();

                _sysStatsTimer.Stop();
                _indicatorCts?.Cancel();
                _indicatorCts?.Dispose();

                _llamaClient?.Dispose();
                _llamaInstructClient?.Dispose();
            };
        }



        private void OpenAiSettingsDialog()
        {
            SyncUIToSettings();
            using var dlg = new AiSettingsDialog(_aiSettings);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                SaveSettings();
                ApplySettingsToUI();
                int idx = _aiProviderCombo.Items.IndexOf(_aiSettings.Provider);
                if (idx >= 0) _aiProviderCombo.SelectedIndex = idx;
                OnProviderChanged(null, EventArgs.Empty);
            }
        }

        private float _lastCpuPct = 0f;
        private Color _sysStatsForeColor = SystemColors.ControlText;
        private CancellationTokenSource? _indicatorCts;
        private IndicatorState _indicatorState = IndicatorState.Unloaded;

        private enum IndicatorState { Unloaded, Loading, Loaded, Generating }

        private void UpdateSysStats()
        {
            try
            {
                var proc = Process.GetCurrentProcess();
                var now = DateTime.UtcNow;
                var cpu = proc.TotalProcessorTime;
                var elapsed = (now - _lastCpuSample).TotalSeconds;

                if (elapsed > 0.1)
                {
                    _lastCpuPct = (float)((cpu - _lastCpuTime).TotalSeconds / elapsed / Environment.ProcessorCount * 100.0);
                    _lastCpuTime = cpu;
                    _lastCpuSample = now;
                }

                long procRamMb = proc.WorkingSet64 / (1024 * 1024);
                long totalRamMb = (long)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024));

                _aiSysStatsLabel.Text = $"CPU {_lastCpuPct:F1}%   Proc RAM: {procRamMb} MB   System RAM: {totalRamMb:N0} MB";

                var cpuColor = _lastCpuPct > 70f ? Color.OrangeRed
                             : _lastCpuPct > 40f ? Color.DarkOrange
                                                  : SystemColors.ControlText;
                if (cpuColor != _sysStatsForeColor)
                {
                    _sysStatsForeColor = cpuColor;
                    _ = ColorFader.FadeForeAsync(_aiSysStatsLabel, _aiSysStatsLabel.ForeColor, cpuColor, durationMs: 600);
                }
            }
            catch { /* non-critical */ }
        }

        private void SaveSettings()
        {
            try { _aiSettingsService.Save(_aiSettings); }
            catch { /* non-critical */ }
        }

        private readonly ISettingsService<AiSettings> _aiSettingsService = new JsonSettingsService<AiSettings>(
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                 "ApexUIBridge", "ai-settings.json"));

        private void SyncUIToSettings()
        {
            _aiSettings.Provider = _aiProviderCombo.SelectedItem?.ToString() ?? _aiSettings.Provider;
            _aiSettings.ModelPath = _aiModelPathBox.Text.Trim();

            _aiSettings.SystemPrompt = _aiSystemBox.Text;
        }

        private async Task AiSendMessageAsync(bool stream)
        {
            if (_aiProviderCombo.SelectedItem?.ToString() == "LlamaSharp (Local)")
            {
                await AiSendLlamaMessageAsync();
                return;
            }

            if (_aiProviderCombo.SelectedItem?.ToString() == "LlamaSharp Instruct (Local)")
            {
                await AiSendLlamaInstructMessageAsync();
                return;
            }
        }
    }
}