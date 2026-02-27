using AutoInferenceBenchmark.Core;

namespace AutoInferenceBenchmark.Forms;

/// <summary>
/// Modal dialog for editing a single <see cref="TestCase"/>.
/// </summary>
public sealed class TestCaseEditor : Form
{
    private readonly TestCase _testCase;
    private TextBox _nameBox = new();
    private ComboBox _difficultyCombo = new();
    private TextBox _promptBox = new();
    private TextBox _expectedBox = new();
    private ComboBox _matchModeCombo = new();
    private NumericUpDown _thresholdSpinner = new();
    private Button _okBtn = new();
    private Button _cancelBtn = new();

    public TestCaseEditor(TestCase testCase)
    {
        _testCase = testCase;
        Text = string.IsNullOrEmpty(testCase.Name) ? "Add Test Case" : $"Edit: {testCase.Name}";
        Size = new Size(520, 480);
        MinimumSize = new Size(400, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        ShowInTaskbar = false;

        BuildUI();
        LoadFromTestCase();
    }

    private void BuildUI()
    {
        var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 38 };
        _okBtn = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(75, 26) };
        _cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(75, 26) };
        _okBtn.Click += (_, _) => { SaveToTestCase(); DialogResult = DialogResult.OK; Close(); };
        _cancelBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 5, 8, 0)
        };
        flow.Controls.Add(_okBtn);
        flow.Controls.Add(_cancelBtn);
        btnPanel.Controls.Add(flow);
        AcceptButton = _okBtn;
        CancelButton = _cancelBtn;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = false,
            Padding = new Padding(8)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Name
        _nameBox = new TextBox { Dock = DockStyle.Fill };
        AddRow(table, "Name:", _nameBox);

        // Difficulty
        _difficultyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        _difficultyCombo.Items.AddRange(Enum.GetNames<Difficulty>().Cast<object>().ToArray());
        AddRow(table, "Difficulty:", _difficultyCombo);

        // Prompt
        _promptBox = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 80, Dock = DockStyle.Fill, Font = new Font("Consolas", 9f) };
        AddRow(table, "Prompt:", _promptBox);

        // Expected response
        _expectedBox = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 80, Dock = DockStyle.Fill, Font = new Font("Consolas", 9f) };
        AddRow(table, "Expected:", _expectedBox);

        // Match mode
        _matchModeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        _matchModeCombo.Items.AddRange(Enum.GetNames<MatchMode>().Cast<object>().ToArray());
        AddRow(table, "Match Mode:", _matchModeCombo);

        // Threshold
        _thresholdSpinner = new NumericUpDown { Minimum = 0, Maximum = 100, DecimalPlaces = 1, Increment = 5, Width = 80 };
        AddRow(table, "Threshold %:", _thresholdSpinner);

        // Set row styles for expanding text boxes
        table.RowCount = 6;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Controls.Add(table);
        Controls.Add(btnPanel);
    }

    private static void AddRow(TableLayoutPanel t, string label, Control input)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(2, 7, 4, 2) };
        input.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
        input.Margin = new Padding(0, 4, 4, 2);
        t.Controls.Add(lbl);
        t.Controls.Add(input);
    }

    private void LoadFromTestCase()
    {
        _nameBox.Text = _testCase.Name;
        _difficultyCombo.SelectedItem = _testCase.Difficulty.ToString();
        _promptBox.Text = _testCase.Prompt;
        _expectedBox.Text = _testCase.ExpectedResponse;
        _matchModeCombo.SelectedItem = _testCase.MatchMode.ToString();
        _thresholdSpinner.Value = (decimal)Math.Clamp(_testCase.SimilarityThreshold, 0f, 100f);
    }

    private void SaveToTestCase()
    {
        _testCase.Name = _nameBox.Text.Trim();
        _testCase.Difficulty = Enum.TryParse<Difficulty>(_difficultyCombo.SelectedItem?.ToString(), out var d) ? d : Difficulty.Easy;
        _testCase.Prompt = _promptBox.Text;
        _testCase.ExpectedResponse = _expectedBox.Text;
        _testCase.MatchMode = Enum.TryParse<MatchMode>(_matchModeCombo.SelectedItem?.ToString(), out var m) ? m : MatchMode.Similarity;
        _testCase.SimilarityThreshold = (float)_thresholdSpinner.Value;
    }
}
