using ClaudeUsageWidget.Services;

namespace ClaudeUsageWidget;

public class SettingsForm : Form
{
    private readonly SettingsService _settingsService;
    private NumericUpDown _pollIntervalInput = null!;
    private NumericUpDown _alertThresholdInput = null!;
    private CheckBox _alertEnabledCheckbox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public SettingsForm(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Text = "Settings";
        Size = new Size(350, 220);
        BackColor = Color.FromArgb(253, 250, 243);

        int yPos = 20;

        // Poll interval
        Label pollLabel = new Label
        {
            Text = "Poll interval (minutes):",
            Font = new Font("Segoe UI", 10),
            Location = new Point(20, yPos + 3),
            AutoSize = true
        };
        Controls.Add(pollLabel);

        _pollIntervalInput = new NumericUpDown
        {
            Location = new Point(220, yPos),
            Size = new Size(80, 25),
            Minimum = 1,
            Maximum = 60,
            Value = 5
        };
        Controls.Add(_pollIntervalInput);
        yPos += 40;

        // Alert threshold
        Label thresholdLabel = new Label
        {
            Text = "Alert threshold (%):",
            Font = new Font("Segoe UI", 10),
            Location = new Point(20, yPos + 3),
            AutoSize = true
        };
        Controls.Add(thresholdLabel);

        _alertThresholdInput = new NumericUpDown
        {
            Location = new Point(220, yPos),
            Size = new Size(80, 25),
            Minimum = 50,
            Maximum = 100,
            Value = 90
        };
        Controls.Add(_alertThresholdInput);
        yPos += 40;

        // Alert enabled
        _alertEnabledCheckbox = new CheckBox
        {
            Text = "Enable usage alerts",
            Font = new Font("Segoe UI", 10),
            Location = new Point(20, yPos),
            AutoSize = true,
            Checked = true
        };
        Controls.Add(_alertEnabledCheckbox);
        yPos += 50;

        // Buttons
        _saveButton = new Button
        {
            Text = "Save",
            Font = new Font("Segoe UI", 10),
            Location = new Point(130, yPos),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK
        };
        _saveButton.Click += SaveButton_Click;
        Controls.Add(_saveButton);

        _cancelButton = new Button
        {
            Text = "Cancel",
            Font = new Font("Segoe UI", 10),
            Location = new Point(220, yPos),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(_cancelButton);

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        ResumeLayout(false);
    }

    private void LoadSettings()
    {
        Models.AppSettings settings = _settingsService.Settings;
        _pollIntervalInput.Value = settings.PollIntervalMinutes;
        _alertThresholdInput.Value = settings.AlertThresholdPercent;
        _alertEnabledCheckbox.Checked = settings.AlertEnabled;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        _settingsService.UpdatePollInterval((int)_pollIntervalInput.Value);
        _settingsService.UpdateAlertThreshold((int)_alertThresholdInput.Value);
        _settingsService.SetAlertEnabled(_alertEnabledCheckbox.Checked);
        Close();
    }
}
