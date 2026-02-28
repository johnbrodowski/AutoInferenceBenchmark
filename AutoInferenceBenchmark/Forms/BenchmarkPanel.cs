using System.ComponentModel;
using AutoInferenceBenchmark.Benchmark;
using AutoInferenceBenchmark.Clients;
using AutoInferenceBenchmark.Core;
using AutoInferenceBenchmark.Scoring;
using AutoInferenceBenchmark.Storage;
using AutoInferenceBenchmark.Templates;

namespace AutoInferenceBenchmark.Forms;

/// <summary>
/// Code-built panel for benchmark configuration, execution, and results viewing.
/// Hosted inside Form1 as a tab page.
/// </summary>
public sealed class BenchmarkPanel : Panel
{
    // ── Test dataset ────────────────────────────────────────────────────
    private DataGridView _testGrid = new();
    private Button _btnAddTest = new();
    private Button _btnEditTest = new();
    private Button _btnRemoveTest = new();
    private Button _btnImportDataset = new();
    private Button _btnExportDataset = new();

    // ── Sweep config ────────────────────────────────────────────────────
    private RadioButton _rbTempOnly = new();
    private RadioButton _rbAllCombinations = new();
    private NumericUpDown _tempMin = new(), _tempMax = new(), _tempStep = new();
    private NumericUpDown _topPMin = new(), _topPMax = new(), _topPStep = new();
    private NumericUpDown _topKMin = new(), _topKMax = new(), _topKStep = new();
    private NumericUpDown _minPMin = new(), _minPMax = new(), _minPStep = new();
    private NumericUpDown _repPenMin = new(), _repPenMax = new(), _repPenStep = new();
    private NumericUpDown _maxTokensSpinner = new();
    private CheckBox _chkDeterministic = new();
    private Label _lblTotalConfigs = new();

    // ── Execution ───────────────────────────────────────────────────────
    private Button _btnStart = new();
    private Button _btnStop = new();
    private Button _btnLoad = new();
    private ProgressBar _progressBar = new();
    private Label _lblProgress = new();
    private Label _lblEta = new();
    private Label _lblBestSoFar = new();

    // ── Status bar ───────────────────────────────────────────────────────
    private Label _lblIndicator = new();
    private Label _lblModelStats = new();
    private Label _lblSysStats = new();
    private CancellationTokenSource? _indicatorCts;

    // ── Results ─────────────────────────────────────────────────────────
    private DataGridView _resultsGrid = new();
    private Label _lblBestConfig = new();
    private Button _btnApplyBest = new();
    private Button _btnExportCsv = new();

    // ── State ───────────────────────────────────────────────────────────
    private TestDataset _dataset = TestDataset.CreateDefault();
    private CancellationTokenSource? _benchmarkCts;
    private TelemetryDb? _db;
    private BenchmarkRunSummary? _lastSummary;

    /// <summary>Raised when the user clicks "Apply Best Config" so Form1 can update its settings.</summary>
    public event EventHandler<InferenceConfig>? ApplyConfigRequested;

    /// <summary>Raised when the user clicks Load/Unload so Form1 can reuse its model loading logic.</summary>
    public event EventHandler? LoadUnloadRequested;

    /// <summary>
    /// Func that returns the current model path from the main form.
    /// Set by Form1 after construction.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Func<string>? GetModelPath { get; set; }

    /// <summary>
    /// Func that returns the current system prompt from the main form.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Func<string>? GetSystemPrompt { get; set; }

    /// <summary>
    /// Func that returns current threads setting from the main form.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Func<int>? GetThreads { get; set; }

    /// <summary>
    /// Func that returns current context size setting from the main form.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Func<int>? GetContextSize { get; set; }

    /// <summary>
    /// Func that returns current reasoning effort setting from the main form.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Func<string>? GetReasoningEffort { get; set; }

    public BenchmarkPanel()
    {
        Dock = DockStyle.Fill;
        BuildUI();
        RefreshTestGrid();
        UpdateConfigCount();
    }

    // ═══════════════════════════════════════════════════════════════════
    // PUBLIC API — called by Form1 to keep indicator / stats in sync
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the indicator label to match the chat tab's state.
    /// Call this from Form1.AiSetIndicatorState.
    /// </summary>
    public void SetIndicatorState(string text, Color colorA, Color colorB, bool pulse, bool enableLoad = true)
    {
        if (InvokeRequired) { BeginInvoke(() => SetIndicatorState(text, colorA, colorB, pulse, enableLoad)); return; }

        _indicatorCts?.Cancel();
        _indicatorCts?.Dispose();
        _indicatorCts = null;

        _lblIndicator.Text = text;
        _btnLoad.Enabled = enableLoad;
        _btnLoad.Text = text.Contains("ready") || text.Contains("active") ? "Unload" : "Load";

        if (pulse)
            _indicatorCts = ColorFader.PulseFore(_lblIndicator, colorA, colorB);
        else
        {
            var fadeCts = new CancellationTokenSource();
            _indicatorCts = fadeCts;
            _ = ColorFader.FadeForeAsync(_lblIndicator, _lblIndicator.ForeColor, colorA, durationMs: 500, ct: fadeCts.Token);
        }
    }

    /// <summary>Updates the system stats label (CPU / RAM).</summary>
    public void UpdateSysStats(string text)
    {
        if (InvokeRequired) { BeginInvoke(() => UpdateSysStats(text)); return; }
        _lblSysStats.Text = text;
    }

    /// <summary>Updates the model performance stats label (t/s, TTFT).</summary>
    public void UpdateModelStats(string text, float tps = 0f)
    {
        if (InvokeRequired) { BeginInvoke(() => UpdateModelStats(text, tps)); return; }
        _lblModelStats.Text = text;
        if (!string.IsNullOrEmpty(text))
        {
            var target = tps > 15f ? Color.LimeGreen
                       : tps > 5f  ? Color.DarkOrange
                                    : Color.OrangeRed;
            _ = ColorFader.FadeForeAsync(_lblModelStats, _lblModelStats.ForeColor, target, durationMs: 400);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // UI CONSTRUCTION
    // ═══════════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        // ── Status bar at bottom ─────────────────────────────────────
        var statusBar = BuildStatusBar();
        statusBar.Dock = DockStyle.Bottom;

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 340,
            Panel1MinSize = 200,
            Panel2MinSize = 120
        };

        // Top: config panels
        var topSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 380
        };

        topSplit.Layout += (_, _) =>
        {
            var availableWidth = topSplit.ClientSize.Width - topSplit.SplitterWidth;
            if (availableWidth <= 0)
            {
                return;
            }

            var minPanelSize = Math.Min(200, availableWidth / 2);
            if (topSplit.Panel1MinSize != minPanelSize)
            {
                topSplit.Panel1MinSize = minPanelSize;
            }

            if (topSplit.Panel2MinSize != minPanelSize)
            {
                topSplit.Panel2MinSize = minPanelSize;
            }

            var maxDistance = availableWidth - minPanelSize;
            var targetDistance = Math.Min(380, maxDistance);
            targetDistance = Math.Max(minPanelSize, targetDistance);
            if (topSplit.SplitterDistance != targetDistance)
            {
                topSplit.SplitterDistance = targetDistance;
            }
        };

        topSplit.Panel1.Controls.Add(BuildTestDatasetGroup());
        topSplit.Panel2.Controls.Add(BuildSweepAndExecutionPanel());

        mainSplit.Panel1.Controls.Add(topSplit);

        // Bottom: results
        mainSplit.Panel2.Controls.Add(BuildResultsGroup());

        Controls.Add(mainSplit);
        Controls.Add(statusBar);
    }

    private GroupBox BuildTestDatasetGroup()
    {
        var gb = new GroupBox { Text = "Test Dataset", Dock = DockStyle.Fill, Padding = new Padding(6, 16, 6, 6) };

        _testGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false
        };

        // Enabled checkbox (editable)
        var chkCol = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled", HeaderText = "", Width = 30, AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            FillWeight = 1, Resizable = DataGridViewTriState.False
        };
        _testGrid.Columns.Add(chkCol);
        _testGrid.Columns.Add("Name", "Name");
        _testGrid.Columns.Add("Difficulty", "Difficulty");
        _testGrid.Columns.Add("MatchMode", "Match");
        _testGrid.Columns.Add("Threshold", "Threshold");
        _testGrid.Columns["Threshold"]!.DefaultCellStyle.Format = "F0";

        // Only the checkbox column is editable
        _testGrid.CellBeginEdit += (_, e) => { if (e.ColumnIndex != 0) e.Cancel = true; };
        _testGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_testGrid.IsCurrentCellDirty) _testGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _testGrid.CellValueChanged += (_, e) => { if (e.ColumnIndex == 0 && e.RowIndex >= 0) UpdateConfigCount(); };
        _testGrid.CellDoubleClick += (_, e) => { if (e.ColumnIndex != 0) EditSelectedTest(); };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 32,
            FlowDirection = FlowDirection.LeftToRight, AutoSize = false
        };

        _btnAddTest = new Button { Text = "Add", Width = 55 };
        _btnEditTest = new Button { Text = "Edit", Width = 55 };
        _btnRemoveTest = new Button { Text = "Remove", Width = 65 };
        _btnImportDataset = new Button { Text = "Import...", Width = 70 };
        _btnExportDataset = new Button { Text = "Export...", Width = 70 };

        _btnAddTest.Click += (_, _) => AddTest();
        _btnEditTest.Click += (_, _) => EditSelectedTest();
        _btnRemoveTest.Click += (_, _) => RemoveSelectedTest();
        _btnImportDataset.Click += (_, _) => ImportDataset();
        _btnExportDataset.Click += (_, _) => ExportDataset();

        btnPanel.Controls.AddRange([_btnAddTest, _btnEditTest, _btnRemoveTest, _btnImportDataset, _btnExportDataset]);
        gb.Controls.Add(_testGrid);
        gb.Controls.Add(btnPanel);
        return gb;
    }

    private Panel BuildSweepAndExecutionPanel()
    {
        var outer = new Panel { Dock = DockStyle.Fill };

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var sweepGroup = BuildSweepGroup();
        sweepGroup.Dock = DockStyle.Top;
        scroll.Controls.Add(sweepGroup);

        var execGroup = BuildExecutionGroup();
        execGroup.Dock = DockStyle.Bottom;

        outer.Controls.Add(scroll);
        outer.Controls.Add(execGroup);
        return outer;
    }

    private GroupBox BuildSweepGroup()
    {
        var gb = new GroupBox { Text = "Parameter Sweep", AutoSize = true, Padding = new Padding(6, 16, 6, 6) };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

        // Sweep mode
        var modePanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _rbTempOnly = new RadioButton { Text = "Temperature Only", AutoSize = true, Checked = true };
        _rbAllCombinations = new RadioButton { Text = "All Combinations", AutoSize = true };
        _rbTempOnly.CheckedChanged += (_, _) => { UpdateSweepVisibility(); UpdateConfigCount(); };
        _rbAllCombinations.CheckedChanged += (_, _) => UpdateConfigCount();
        modePanel.Controls.AddRange([_rbTempOnly, _rbAllCombinations]);
        table.SetColumnSpan(modePanel, 4);
        table.Controls.Add(modePanel);

        // Headers
        AddHeaderRow(table, "", "Min", "Max", "Step");

        // Temperature
        _tempMin = MakeSpinner(0, 2, 0.1m, 2); _tempMax = MakeSpinner(0, 2, 1.0m, 2); _tempStep = MakeSpinner(0.01m, 1, 0.1m, 2);
        AddParamRow(table, "Temperature:", _tempMin, _tempMax, _tempStep);

        // Top-P
        _topPMin = MakeSpinner(0, 1, 0.9m, 2); _topPMax = MakeSpinner(0, 1, 1.0m, 2); _topPStep = MakeSpinner(0.01m, 1, 0.05m, 2);
        AddParamRow(table, "Top-P:", _topPMin, _topPMax, _topPStep);

        // Top-K
        _topKMin = MakeSpinner(1, 200, 20, 0); _topKMax = MakeSpinner(1, 200, 60, 0); _topKStep = MakeSpinner(1, 100, 10, 0);
        AddParamRow(table, "Top-K:", _topKMin, _topKMax, _topKStep);

        // Min-P
        _minPMin = MakeSpinner(0, 1, 0.05m, 2); _minPMax = MakeSpinner(0, 1, 0.2m, 2); _minPStep = MakeSpinner(0.01m, 1, 0.05m, 2);
        AddParamRow(table, "Min-P:", _minPMin, _minPMax, _minPStep);

        // Repeat Penalty
        _repPenMin = MakeSpinner(1, 2, 1.0m, 2); _repPenMax = MakeSpinner(1, 2, 1.15m, 2); _repPenStep = MakeSpinner(0.01m, 1, 0.05m, 2);
        AddParamRow(table, "Repeat Pen:", _repPenMin, _repPenMax, _repPenStep);

        // Max tokens
        _maxTokensSpinner = new NumericUpDown { Minimum = 64, Maximum = 131072, Increment = 256, Value = 2048, Width = 90 };
        var maxTokLbl = new Label { Text = "Max Tokens:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(2, 7, 4, 2) };
        table.Controls.Add(maxTokLbl);
        table.Controls.Add(_maxTokensSpinner);

        // Deterministic seed
        _chkDeterministic = new CheckBox { Text = "Deterministic (seed=42)", AutoSize = true, Checked = true };
        table.SetColumnSpan(_chkDeterministic, 2);
        table.Controls.Add(_chkDeterministic);

        // Config count
        _lblTotalConfigs = new Label { Text = "Total configs: 0", AutoSize = true, ForeColor = Color.DarkBlue };
        table.SetColumnSpan(_lblTotalConfigs, 4);
        table.Controls.Add(_lblTotalConfigs);

        // Wire value-changed events for config count update
        foreach (var spinner in new[] { _tempMin, _tempMax, _tempStep, _topPMin, _topPMax, _topPStep,
            _topKMin, _topKMax, _topKStep, _minPMin, _minPMax, _minPStep, _repPenMin, _repPenMax, _repPenStep })
        {
            spinner.ValueChanged += (_, _) => UpdateConfigCount();
        }

        gb.Controls.Add(table);
        UpdateSweepVisibility();
        return gb;
    }

    private GroupBox BuildExecutionGroup()
    {
        var gb = new GroupBox { Text = "Execution", Height = 110, Dock = DockStyle.Bottom, Padding = new Padding(6, 16, 6, 6) };

        var btnFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 30, FlowDirection = FlowDirection.LeftToRight
        };
        _btnStart = new Button { Text = "Start Benchmark", Width = 120 };
        _btnStop = new Button { Text = "Stop", Width = 60, Enabled = false };
        _btnStart.Click += async (_, _) => await StartBenchmarkAsync();
        _btnStop.Click += (_, _) => StopBenchmark();
        btnFlow.Controls.AddRange([_btnStart, _btnStop]);

        _progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 18, Margin = new Padding(2) };

        var statusFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true
        };
        _lblProgress = new Label { Text = "Ready", AutoSize = true, Margin = new Padding(4) };
        _lblEta = new Label { Text = "", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(12, 4, 4, 4) };
        _lblBestSoFar = new Label { Text = "", AutoSize = true, ForeColor = Color.DarkGreen, Margin = new Padding(12, 4, 4, 4) };
        statusFlow.Controls.AddRange([_lblProgress, _lblEta, _lblBestSoFar]);

        gb.Controls.Add(statusFlow);
        gb.Controls.Add(_progressBar);
        gb.Controls.Add(btnFlow);
        return gb;
    }

    private GroupBox BuildResultsGroup()
    {
        var gb = new GroupBox { Text = "Results", Dock = DockStyle.Fill, Padding = new Padding(6, 16, 6, 6) };

        _resultsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false
        };
        _resultsGrid.Columns.Add("Config", "Config");
        _resultsGrid.Columns.Add("TestCase", "Test Case");
        _resultsGrid.Columns.Add("Score", "Score %");
        _resultsGrid.Columns.Add("Pass", "Pass");
        _resultsGrid.Columns.Add("TPS", "Tokens/s");
        _resultsGrid.Columns.Add("TTFT", "TTFT (s)");
        _resultsGrid.Columns.Add("Latency", "Latency (s)");
        _resultsGrid.Columns["Score"]!.DefaultCellStyle.Format = "F1";
        _resultsGrid.Columns["TPS"]!.DefaultCellStyle.Format = "F1";
        _resultsGrid.Columns["TTFT"]!.DefaultCellStyle.Format = "F2";
        _resultsGrid.Columns["Latency"]!.DefaultCellStyle.Format = "F1";

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 30, FlowDirection = FlowDirection.LeftToRight
        };
        _lblBestConfig = new Label { Text = "Best: (none)", AutoSize = true, ForeColor = Color.DarkGreen, Margin = new Padding(4) };
        _btnApplyBest = new Button { Text = "Apply Best Config", Width = 120, Enabled = false };
        _btnExportCsv = new Button { Text = "Export CSV...", Width = 90, Enabled = false };
        _btnApplyBest.Click += (_, _) => ApplyBestConfig();
        _btnExportCsv.Click += (_, _) => ExportResultsCsv();
        bottomPanel.Controls.AddRange([_lblBestConfig, _btnApplyBest, _btnExportCsv]);

        gb.Controls.Add(_resultsGrid);
        gb.Controls.Add(bottomPanel);
        return gb;
    }

    private Panel BuildStatusBar()
    {
        var bar = new FlowLayoutPanel
        {
            Height = 28,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Padding = new Padding(4, 4, 4, 0)
        };

        _lblIndicator = new Label { Text = "\u25cf unloaded", AutoSize = true, ForeColor = Color.Red, Margin = new Padding(2, 2, 8, 0) };
        _btnLoad = new Button { Text = "Load", Width = 65, Height = 22 };
        _btnLoad.Click += (_, _) => LoadUnloadRequested?.Invoke(this, EventArgs.Empty);

        _lblModelStats = new Label { Text = "", AutoSize = true, Margin = new Padding(12, 2, 4, 0) };
        _lblSysStats = new Label { Text = "", AutoSize = true, ForeColor = SystemColors.ControlText, Margin = new Padding(12, 2, 4, 0) };

        bar.Controls.AddRange([_lblIndicator, _btnLoad, _lblModelStats, _lblSysStats]);
        return bar;
    }

    // ═══════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private static NumericUpDown MakeSpinner(decimal min, decimal max, decimal value, int decimals) => new()
    {
        Minimum = min, Maximum = max, Value = Math.Clamp(value, min, max),
        DecimalPlaces = decimals, Increment = decimals >= 2 ? 0.01m : decimals == 1 ? 0.1m : 1m,
        Width = 70
    };

    private static void AddHeaderRow(TableLayoutPanel t, string c1, string c2, string c3, string c4)
    {
        foreach (var text in new[] { c1, c2, c3, c4 })
        {
            var lbl = new Label { Text = text, AutoSize = true, Font = new Font(Control.DefaultFont, FontStyle.Bold), Margin = new Padding(2, 4, 2, 2) };
            t.Controls.Add(lbl);
        }
    }

    private static void AddParamRow(TableLayoutPanel t, string label, NumericUpDown min, NumericUpDown max, NumericUpDown step)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(2, 7, 4, 2) };
        t.Controls.Add(lbl);
        t.Controls.Add(min);
        t.Controls.Add(max);
        t.Controls.Add(step);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TEST DATASET OPERATIONS
    // ═══════════════════════════════════════════════════════════════════

    private void RefreshTestGrid()
    {
        _testGrid.Rows.Clear();
        foreach (var tc in _dataset.TestCases)
            _testGrid.Rows.Add(true, tc.Name, tc.Difficulty.ToString(), tc.MatchMode.ToString(), tc.SimilarityThreshold);
    }

    private void AddTest()
    {
        var tc = new TestCase();
        using var dlg = new TestCaseEditor(tc);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            _dataset.TestCases.Add(tc);
            RefreshTestGrid();
            UpdateConfigCount();
        }
    }

    private void EditSelectedTest()
    {
        if (_testGrid.SelectedRows.Count == 0) return;
        int idx = _testGrid.SelectedRows[0].Index;
        if (idx < 0 || idx >= _dataset.TestCases.Count) return;

        using var dlg = new TestCaseEditor(_dataset.TestCases[idx]);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            RefreshTestGrid();
    }

    private void RemoveSelectedTest()
    {
        if (_testGrid.SelectedRows.Count == 0) return;
        int idx = _testGrid.SelectedRows[0].Index;
        if (idx < 0 || idx >= _dataset.TestCases.Count) return;
        _dataset.TestCases.RemoveAt(idx);
        RefreshTestGrid();
        UpdateConfigCount();
    }

    private void ImportDataset()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Import Test Dataset",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
        };
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            try
            {
                _dataset = TestDataset.LoadFromFile(dlg.FileName);
                RefreshTestGrid();
                UpdateConfigCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), $"Failed to import: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ExportDataset()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Export Test Dataset",
            Filter = "JSON Files (*.json)|*.json",
            FileName = $"{_dataset.Name}.json"
        };
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            try { _dataset.SaveToFile(dlg.FileName); }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), $"Failed to export: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SWEEP CONFIG
    // ═══════════════════════════════════════════════════════════════════

    private ParameterSweepConfig BuildSweepConfig() => new()
    {
        Mode = _rbTempOnly.Checked ? SweepMode.TemperatureOnly : SweepMode.AllCombinations,
        TemperatureMin = (float)_tempMin.Value,
        TemperatureMax = (float)_tempMax.Value,
        TemperatureStep = (float)_tempStep.Value,
        TopPMin = (float)_topPMin.Value,
        TopPMax = (float)_topPMax.Value,
        TopPStep = (float)_topPStep.Value,
        TopKMin = (int)_topKMin.Value,
        TopKMax = (int)_topKMax.Value,
        TopKStep = (int)_topKStep.Value,
        MinPMin = (float)_minPMin.Value,
        MinPMax = (float)_minPMax.Value,
        MinPStep = (float)_minPStep.Value,
        RepeatPenaltyMin = (float)_repPenMin.Value,
        RepeatPenaltyMax = (float)_repPenMax.Value,
        RepeatPenaltyStep = (float)_repPenStep.Value,
        MaxTokens = (int)_maxTokensSpinner.Value,
        DeterministicSeed = _chkDeterministic.Checked,
        Seed = 42
    };

    private void UpdateConfigCount()
    {
        try
        {
            var sweep = BuildSweepConfig();
            int count = ParameterSweepGenerator.Count(sweep);
            int enabledTests = GetEnabledTestCount();
            int totalRuns = count * enabledTests;
            _lblTotalConfigs.Text = $"Configs: {count}  |  Tests: {enabledTests}/{_dataset.TestCases.Count}  |  Total runs: {totalRuns}";
        }
        catch
        {
            _lblTotalConfigs.Text = "Configs: (error)";
        }
    }

    private int GetEnabledTestCount()
    {
        int count = 0;
        for (int i = 0; i < _testGrid.Rows.Count; i++)
        {
            if (_testGrid.Rows[i].Cells[0].Value is true)
                count++;
        }
        return count;
    }

    /// <summary>Returns a filtered dataset containing only the checked test cases.</summary>
    private TestDataset GetEnabledDataset()
    {
        var filtered = new TestDataset { Name = _dataset.Name };
        for (int i = 0; i < _testGrid.Rows.Count && i < _dataset.TestCases.Count; i++)
        {
            if (_testGrid.Rows[i].Cells[0].Value is true)
                filtered.TestCases.Add(_dataset.TestCases[i]);
        }
        return filtered;
    }

    private void UpdateSweepVisibility()
    {
        bool allMode = _rbAllCombinations.Checked;
        foreach (var ctrl in new Control[] { _topPMin, _topPMax, _topPStep, _topKMin, _topKMax, _topKStep,
            _minPMin, _minPMax, _minPStep, _repPenMin, _repPenMax, _repPenStep })
        {
            ctrl.Enabled = allMode;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // BENCHMARK EXECUTION
    // ═══════════════════════════════════════════════════════════════════

    private async Task StartBenchmarkAsync()
    {
        var modelPath = GetModelPath?.Invoke() ?? "";
        if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
        {
            MessageBox.Show(FindForm(), "Please load a model first (set model path on the Chat tab).",
                "No Model", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var enabledDataset = GetEnabledDataset();
        if (enabledDataset.TestCases.Count == 0)
        {
            MessageBox.Show(FindForm(), "Enable at least one test case.", "No Tests",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var sweep = BuildSweepConfig();
        var systemPrompt = GetSystemPrompt?.Invoke() ?? "You are a helpful assistant.";
        var reasoningEffort = GetReasoningEffort?.Invoke();
        if (!string.IsNullOrWhiteSpace(reasoningEffort))
        {
            var effort = reasoningEffort.Trim().ToLowerInvariant();
            if (effort is "low" or "medium" or "high")
                systemPrompt += $"\nReasoning: {effort}";
        }

        int threads = GetThreads?.Invoke() ?? 10;
        int contextSize = GetContextSize?.Invoke() ?? 4096;

        // UI state
        _btnStart.Enabled = false;
        _btnStop.Enabled = true;
        _btnApplyBest.Enabled = false;
        _btnExportCsv.Enabled = false;
        _resultsGrid.Rows.Clear();
        _progressBar.Value = 0;
        _progressBar.Maximum = 100;
        _lblProgress.Text = "Loading model...";
        _lblEta.Text = "";
        _lblBestSoFar.Text = "";
        _lblBestConfig.Text = "Best: (running...)";

        _benchmarkCts = new CancellationTokenSource();
        var ct = _benchmarkCts.Token;

        try
        {
            _db ??= new TelemetryDb(TelemetryDb.DefaultPath);

            // Create inference client
            using var client = await InferenceClientFactory.CreateAndLoadAsync(
                modelPath, systemPrompt, threads, contextSize);

            _lblProgress.Text = "Model loaded. Starting benchmark...";

            var engine = new BenchmarkEngine(_db);

            var progress = new Progress<BenchmarkProgress>(p =>
            {
                if (InvokeRequired) { BeginInvoke(() => UpdateProgress(p)); }
                else UpdateProgress(p);
            });

            _lastSummary = await engine.RunBenchmarkAsync(
                client, modelPath, systemPrompt, enabledDataset, sweep, progress, ct);

            // Show final results
            ShowSummary(_lastSummary);
        }
        catch (OperationCanceledException)
        {
            _lblProgress.Text = "Benchmark cancelled.";
            if (_lastSummary != null)
                ShowSummary(_lastSummary);
        }
        catch (Exception ex)
        {
            _lblProgress.Text = $"Error: {ex.Message}";
            MessageBox.Show(FindForm(), $"Benchmark failed:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;
            _benchmarkCts?.Dispose();
            _benchmarkCts = null;
        }
    }

    private void StopBenchmark() => _benchmarkCts?.Cancel();

    private void UpdateProgress(BenchmarkProgress p)
    {
        _progressBar.Value = Math.Min((int)p.ProgressPercentage, 100);
        _lblProgress.Text = $"{p.CompletedCount}/{p.TotalCount} — {p.CurrentTestCaseName} — Score: {p.CurrentScore:F1}%";
        _lblEta.Text = p.EstimatedTimeRemaining.HasValue
            ? $"ETA: {p.EstimatedTimeRemaining.Value:hh\\:mm\\:ss}"
            : "";
        _lblBestSoFar.Text = $"Best avg: {p.BestScoreSoFar:F1}%";

        // Add result row
        _resultsGrid.Rows.Insert(0,
            p.CurrentConfig.ToShortString(),
            p.CurrentTestCaseName,
            p.CurrentScore,
            p.CurrentIsPass ? "Yes" : "No",
            p.CurrentTokensPerSecond,
            p.CurrentTimeToFirstToken,
            p.CurrentLatency);
    }

    private void ShowSummary(BenchmarkRunSummary summary)
    {
        _resultsGrid.Rows.Clear();
        foreach (var r in summary.AllResults.OrderByDescending(r => r.MatchPercentage))
        {
            _resultsGrid.Rows.Add(
                r.Config.ToShortString(),
                r.TestCaseName,
                r.MatchPercentage,
                r.IsPass ? "Yes" : "No",
                r.TokensPerSecond,
                r.TimeToFirstTokenSeconds,
                r.TotalLatencySeconds);
        }

        _lblBestConfig.Text = $"Best: {summary.BestConfig.ToShortString()} — Avg: {summary.BestAverageScore:F1}%";
        _lblProgress.Text = summary.WasCancelled
            ? $"Cancelled after {summary.TotalRuns} runs ({summary.Duration:hh\\:mm\\:ss})"
            : $"Complete: {summary.TotalRuns} runs in {summary.Duration:hh\\:mm\\:ss}";
        _lblEta.Text = "";
        _progressBar.Value = 100;
        _btnApplyBest.Enabled = true;
        _btnExportCsv.Enabled = summary.AllResults.Count > 0;
    }

    private void ApplyBestConfig()
    {
        if (_lastSummary != null)
            ApplyConfigRequested?.Invoke(this, _lastSummary.BestConfig);
    }

    private void ExportResultsCsv()
    {
        if (_lastSummary == null || _lastSummary.AllResults.Count == 0) return;

        using var dlg = new SaveFileDialog
        {
            Title = "Export Results",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"benchmark_{_lastSummary.ModelName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            using var writer = new StreamWriter(dlg.FileName);
            writer.WriteLine("TestCase,Temperature,TopP,TopK,MinP,RepeatPenalty,Score%,Pass,TokensPerSec,TTFT,Latency,Response");
            foreach (var r in _lastSummary.AllResults)
            {
                var resp = r.ResponseText.Replace("\"", "\"\"");
                writer.WriteLine($"\"{r.TestCaseName}\",{r.Config.Temperature},{r.Config.TopP},{r.Config.TopK},{r.Config.MinP},{r.Config.RepeatPenalty},{r.MatchPercentage:F1},{r.IsPass},{r.TokensPerSecond:F1},{r.TimeToFirstTokenSeconds:F2},{r.TotalLatencySeconds:F1},\"{resp}\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(FindForm(), $"Export failed: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
