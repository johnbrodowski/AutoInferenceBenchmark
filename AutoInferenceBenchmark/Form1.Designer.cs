namespace AutoInferenceBenchmark
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _aiModelPathBox = new TextBox();
            _aiInput = new TextBox();
            _btnAiStream = new Button();
            _btnAiSend = new Button();
            _aiStatusLabel = new Label();
            _aiOutput = new RichTextBox();
            _aiSystemBox = new TextBox();
            _aiStatsLabel = new Label();
            _aiIndicatorLabel = new Label();
            _btnAiStop = new Button();
            _btnAiClear = new Button();
            _aiProviderCombo = new ComboBox();
            _aiSysStatsLabel = new Label();
            _btnAiBrowse = new Button();
            _btnAiSettings = new Button();
            _btnAiLoad = new Button();
            SuspendLayout();
            // 
            // _aiModelPathBox
            // 
            _aiModelPathBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _aiModelPathBox.Location = new Point(3, 35);
            _aiModelPathBox.Name = "_aiModelPathBox";
            _aiModelPathBox.PlaceholderText = "path";
            _aiModelPathBox.Size = new Size(647, 23);
            _aiModelPathBox.TabIndex = 0;
            // 
            // _aiInput
            // 
            _aiInput.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _aiInput.Location = new Point(3, 231);
            _aiInput.Name = "_aiInput";
            _aiInput.PlaceholderText = "ai input";
            _aiInput.Size = new Size(647, 23);
            _aiInput.TabIndex = 1;
            // 
            // _btnAiStream
            // 
            _btnAiStream.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnAiStream.Location = new Point(656, 151);
            _btnAiStream.Name = "_btnAiStream";
            _btnAiStream.Size = new Size(75, 23);
            _btnAiStream.TabIndex = 2;
            _btnAiStream.Text = "stream";
            _btnAiStream.UseVisualStyleBackColor = true;
            // 
            // _btnAiSend
            // 
            _btnAiSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnAiSend.Location = new Point(656, 180);
            _btnAiSend.Name = "_btnAiSend";
            _btnAiSend.Size = new Size(75, 23);
            _btnAiSend.TabIndex = 3;
            _btnAiSend.Text = "send";
            _btnAiSend.UseVisualStyleBackColor = true;
            // 
            // _aiStatusLabel
            // 
            _aiStatusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _aiStatusLabel.BorderStyle = BorderStyle.FixedSingle;
            _aiStatusLabel.Location = new Point(3, 263);
            _aiStatusLabel.Name = "_aiStatusLabel";
            _aiStatusLabel.Size = new Size(730, 23);
            _aiStatusLabel.TabIndex = 4;
            _aiStatusLabel.Text = "status";
            // 
            // _aiOutput
            // 
            _aiOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _aiOutput.BorderStyle = BorderStyle.FixedSingle;
            _aiOutput.Font = new Font("Segoe UI", 11F);
            _aiOutput.Location = new Point(3, 108);
            _aiOutput.Name = "_aiOutput";
            _aiOutput.Size = new Size(647, 117);
            _aiOutput.TabIndex = 5;
            _aiOutput.Text = "";
            // 
            // _aiSystemBox
            // 
            _aiSystemBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _aiSystemBox.Location = new Point(3, 64);
            _aiSystemBox.Multiline = true;
            _aiSystemBox.Name = "_aiSystemBox";
            _aiSystemBox.PlaceholderText = "system message";
            _aiSystemBox.Size = new Size(647, 38);
            _aiSystemBox.TabIndex = 6;
            // 
            // _aiStatsLabel
            // 
            _aiStatsLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _aiStatsLabel.BorderStyle = BorderStyle.FixedSingle;
            _aiStatsLabel.Location = new Point(3, 295);
            _aiStatsLabel.Name = "_aiStatsLabel";
            _aiStatsLabel.Size = new Size(730, 23);
            _aiStatsLabel.TabIndex = 4;
            _aiStatsLabel.Text = "stats";
            // 
            // _aiIndicatorLabel
            // 
            _aiIndicatorLabel.AutoSize = true;
            _aiIndicatorLabel.Location = new Point(292, 5);
            _aiIndicatorLabel.Name = "_aiIndicatorLabel";
            _aiIndicatorLabel.Size = new Size(54, 15);
            _aiIndicatorLabel.TabIndex = 7;
            _aiIndicatorLabel.Text = "Indicator";
            // 
            // _btnAiStop
            // 
            _btnAiStop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnAiStop.Location = new Point(656, 122);
            _btnAiStop.Name = "_btnAiStop";
            _btnAiStop.Size = new Size(75, 23);
            _btnAiStop.TabIndex = 8;
            _btnAiStop.Text = "Stop";
            _btnAiStop.UseVisualStyleBackColor = true;
            // 
            // _btnAiClear
            // 
            _btnAiClear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnAiClear.Location = new Point(656, 93);
            _btnAiClear.Name = "_btnAiClear";
            _btnAiClear.Size = new Size(75, 23);
            _btnAiClear.TabIndex = 9;
            _btnAiClear.Text = "Clear";
            _btnAiClear.UseVisualStyleBackColor = true;
            // 
            // _aiProviderCombo
            // 
            _aiProviderCombo.FormattingEnabled = true;
            _aiProviderCombo.Items.AddRange(new object[] { "LlamaSharp (Local)", "LlamaSharp Instruct (Local)" });
            _aiProviderCombo.Location = new Point(3, 8);
            _aiProviderCombo.Name = "_aiProviderCombo";
            _aiProviderCombo.Size = new Size(194, 23);
            _aiProviderCombo.TabIndex = 10;
            _aiProviderCombo.Text = "LlamaSharp Instruct (Local)";
            // 
            // _aiSysStatsLabel
            // 
            _aiSysStatsLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _aiSysStatsLabel.BorderStyle = BorderStyle.FixedSingle;
            _aiSysStatsLabel.ForeColor = Color.Black;
            _aiSysStatsLabel.Location = new Point(3, 327);
            _aiSysStatsLabel.Name = "_aiSysStatsLabel";
            _aiSysStatsLabel.Size = new Size(730, 23);
            _aiSysStatsLabel.TabIndex = 4;
            _aiSysStatsLabel.Text = "sys status";
            // 
            // _btnAiBrowse
            // 
            _btnAiBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnAiBrowse.Location = new Point(656, 35);
            _btnAiBrowse.Name = "_btnAiBrowse";
            _btnAiBrowse.Size = new Size(75, 23);
            _btnAiBrowse.TabIndex = 11;
            _btnAiBrowse.Text = "browse";
            _btnAiBrowse.UseVisualStyleBackColor = true;
            // 
            // _btnAiSettings
            // 
            _btnAiSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnAiSettings.Location = new Point(656, 64);
            _btnAiSettings.Name = "_btnAiSettings";
            _btnAiSettings.Size = new Size(75, 23);
            _btnAiSettings.TabIndex = 9;
            _btnAiSettings.Text = "Settings";
            _btnAiSettings.UseVisualStyleBackColor = true;
            // 
            // _btnAiLoad
            // 
            _btnAiLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnAiLoad.Location = new Point(656, 8);
            _btnAiLoad.Name = "_btnAiLoad";
            _btnAiLoad.Size = new Size(75, 23);
            _btnAiLoad.TabIndex = 12;
            _btnAiLoad.Text = "Load";
            _btnAiLoad.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(739, 360);
            Controls.Add(_btnAiLoad);
            Controls.Add(_btnAiBrowse);
            Controls.Add(_aiProviderCombo);
            Controls.Add(_btnAiSettings);
            Controls.Add(_btnAiClear);
            Controls.Add(_btnAiStop);
            Controls.Add(_aiIndicatorLabel);
            Controls.Add(_aiSystemBox);
            Controls.Add(_aiOutput);
            Controls.Add(_aiSysStatsLabel);
            Controls.Add(_aiStatsLabel);
            Controls.Add(_aiStatusLabel);
            Controls.Add(_btnAiSend);
            Controls.Add(_btnAiStream);
            Controls.Add(_aiInput);
            Controls.Add(_aiModelPathBox);
            MinimumSize = new Size(649, 345);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox _aiModelPathBox;
        private TextBox _aiInput;
        private Button _btnAiStream;
        private Button _btnAiSend;
        private Label _aiStatusLabel;
        private RichTextBox _aiOutput;
        private TextBox _aiSystemBox;
        private Label _aiStatsLabel;
        private Label _aiIndicatorLabel;
        private Button _btnAiStop;
        private Button _btnAiClear;
        private ComboBox _aiProviderCombo;
        private Label _aiSysStatsLabel;
        private Button _btnAiBrowse;
        private Button _btnAiSettings;
        private Button _btnAiLoad;
    }
}
