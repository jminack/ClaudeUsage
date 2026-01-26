using System.Reflection;
using ClaudeUsageWidget.Models;
using ClaudeUsageWidget.Services;

namespace ClaudeUsageWidget;

public class UsagePopupForm : Form
{
    private const string LogSource = "UsagePopupForm";

    // Claude brand colors
    private static readonly Color ClaudeTerracotta = Color.FromArgb(217, 119, 87);
    private static readonly Color ClaudeCream = Color.FromArgb(255, 247, 237);      // Warm cream background
    private static readonly Color ClaudeTextDark = Color.FromArgb(68, 51, 45);      // Dark brown text
    private static readonly Color ClaudeTextMuted = Color.FromArgb(140, 120, 110);  // Muted brown text
    private static readonly Color ClaudeProgressBg = Color.FromArgb(237, 227, 217); // Progress bar background

    private UsageResponse? _usageData;
    private DateTime _lastUpdated;
    private readonly SettingsService _settingsService;

    private Label _titleLabel = null!;
    private Label _sessionLabel = null!;
    private Label _sessionResetLabel = null!;
    private ClaudeProgressBar _sessionProgress = null!;
    private Label _sessionPercentLabel = null!;
    private Label _weeklyTitleLabel = null!;
    private LinkLabel _learnMoreLink = null!;
    private Label _weeklyLabel = null!;
    private Label _weeklyResetLabel = null!;
    private ClaudeProgressBar _weeklyProgress = null!;
    private Label _weeklyPercentLabel = null!;
    private Label _lastUpdatedLabel = null!;
    private Button _settingsButton = null!;

    public UsagePopupForm(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoggingService.Debug(LogSource, "Creating UsagePopupForm");
        InitializeComponent();
        LoggingService.Debug(LogSource, "UsagePopupForm created");
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // Form settings
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = ClaudeCream;
        Text = "Claude Usage";
        Padding = new Padding(20);

        int yPos = 20;

        // Title
        _titleLabel = new Label
        {
            Text = "Plan usage limits",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = ClaudeTextDark,
            Location = new Point(20, yPos),
            AutoSize = true
        };
        Controls.Add(_titleLabel);

        // Version label
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        Label versionLabel = new Label
        {
            Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}",
            Font = new Font("Segoe UI", 9),
            ForeColor = ClaudeTextMuted,
            Location = new Point(180, yPos + 5),
            AutoSize = true
        };
        Controls.Add(versionLabel);

        // Settings cog icon in title area
        _settingsButton = new Button
        {
            Text = "⚙",
            Font = new Font("Segoe UI", 12),
            Location = new Point(360, yPos - 2),
            Size = new Size(28, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = ClaudeCream,
            ForeColor = ClaudeTextMuted,
            Cursor = Cursors.Hand
        };
        _settingsButton.FlatAppearance.BorderSize = 0;
        _settingsButton.FlatAppearance.MouseOverBackColor = ClaudeProgressBg;
        _settingsButton.Click += SettingsButton_Click;
        Controls.Add(_settingsButton);

        yPos += 40;

        // Current session section
        _sessionLabel = new Label
        {
            Text = "Current session",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ClaudeTextDark,
            Location = new Point(20, yPos),
            AutoSize = true
        };
        Controls.Add(_sessionLabel);

        _sessionPercentLabel = new Label
        {
            Text = "0% used",
            Font = new Font("Segoe UI", 10),
            ForeColor = ClaudeTextMuted,
            Location = new Point(320, yPos),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(_sessionPercentLabel);
        yPos += 20;

        _sessionResetLabel = new Label
        {
            Text = "Resets in --",
            Font = new Font("Segoe UI", 9),
            ForeColor = ClaudeTextMuted,
            Location = new Point(20, yPos),
            AutoSize = true
        };
        Controls.Add(_sessionResetLabel);
        yPos += 25;

        _sessionProgress = new ClaudeProgressBar
        {
            Location = new Point(20, yPos),
            Size = new Size(280, 10),
            Maximum = 100,
            Value = 0,
            ForeColor = ClaudeTerracotta,
            BackgroundColor = ClaudeProgressBg
        };
        Controls.Add(_sessionProgress);
        yPos += 28;

        // Separator
        Label separator1 = new Label
        {
            BackColor = ClaudeProgressBg,
            Location = new Point(20, yPos),
            Size = new Size(360, 1),
            Text = ""
        };
        Controls.Add(separator1);
        yPos += 15;

        // Weekly limits section
        _weeklyTitleLabel = new Label
        {
            Text = "Weekly limits",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ClaudeTextDark,
            Location = new Point(20, yPos),
            AutoSize = true
        };
        Controls.Add(_weeklyTitleLabel);
        yPos += 20;

        _learnMoreLink = new LinkLabel
        {
            Text = "Learn more about usage limits",
            Font = new Font("Segoe UI", 9),
            LinkColor = ClaudeTerracotta,
            ActiveLinkColor = Color.FromArgb(180, 90, 60),
            Location = new Point(20, yPos),
            AutoSize = true
        };
        _learnMoreLink.LinkClicked += (s, e) =>
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://support.anthropic.com/en/articles/8324991-about-claude-s-pro-plan-usage",
                UseShellExecute = true
            });
        };
        Controls.Add(_learnMoreLink);
        yPos += 30;

        _weeklyLabel = new Label
        {
            Text = "All models",
            Font = new Font("Segoe UI", 10),
            ForeColor = ClaudeTextDark,
            Location = new Point(20, yPos),
            AutoSize = true
        };
        Controls.Add(_weeklyLabel);

        _weeklyPercentLabel = new Label
        {
            Text = "0% used",
            Font = new Font("Segoe UI", 10),
            ForeColor = ClaudeTextMuted,
            Location = new Point(320, yPos),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight
        };
        Controls.Add(_weeklyPercentLabel);
        yPos += 20;

        _weeklyResetLabel = new Label
        {
            Text = "Resets --",
            Font = new Font("Segoe UI", 9),
            ForeColor = ClaudeTextMuted,
            Location = new Point(20, yPos),
            AutoSize = true
        };
        Controls.Add(_weeklyResetLabel);
        yPos += 25;

        _weeklyProgress = new ClaudeProgressBar
        {
            Location = new Point(20, yPos),
            Size = new Size(280, 10),
            Maximum = 100,
            Value = 0,
            ForeColor = ClaudeTerracotta,
            BackgroundColor = ClaudeProgressBg
        };
        Controls.Add(_weeklyProgress);
        yPos += 28;

        // Last updated label
        _lastUpdatedLabel = new Label
        {
            Text = "Last updated: --",
            Font = new Font("Segoe UI", 9),
            ForeColor = ClaudeTextMuted,
            Location = new Point(20, yPos),
            AutoSize = true
        };
        Controls.Add(_lastUpdatedLabel);
        yPos += 30; // Space for label + bottom padding

        // Set client size based on actual content
        ClientSize = new Size(400, yPos);

        ResumeLayout(false);

        // Close on deactivate
        Deactivate += (s, e) => Hide();
    }

    public void UpdateUsage(UsageResponse? usage, DateTime lastUpdated)
    {
        LoggingService.Debug(LogSource, $"UpdateUsage called (usage={usage != null}, lastUpdated={lastUpdated:HH:mm:ss})");

        _usageData = usage;
        _lastUpdated = lastUpdated;

        if (usage == null)
        {
            LoggingService.Debug(LogSource, "UpdateUsage: No usage data, showing error state");
            _sessionPercentLabel.Text = "Error";
            _weeklyPercentLabel.Text = "Error";
            _sessionProgress.Value = 0;
            _weeklyProgress.Value = 0;
            _lastUpdatedLabel.Text = "Last updated: Failed to fetch";
            return;
        }

        // Session (5-hour window)
        int sessionPercent = (int)(usage.FiveHour?.Utilization ?? 0);
        _sessionProgress.Value = Math.Min(sessionPercent, 100);
        _sessionPercentLabel.Text = $"{sessionPercent}% used";

        if (usage.FiveHour?.ResetsAt != null)
        {
            TimeSpan timeUntilReset = usage.FiveHour.ResetsAt.Value - DateTimeOffset.Now;
            if (timeUntilReset.TotalMinutes > 0)
            {
                int hours = (int)timeUntilReset.TotalHours;
                int minutes = timeUntilReset.Minutes;
                _sessionResetLabel.Text = hours > 0
                    ? $"Resets in {hours} hr {minutes} min"
                    : $"Resets in {minutes} min";
            }
            else
            {
                _sessionResetLabel.Text = "Resetting soon...";
            }
        }

        // Weekly (7-day window)
        int weeklyPercent = (int)(usage.SevenDay?.Utilization ?? 0);
        _weeklyProgress.Value = Math.Min(weeklyPercent, 100);
        _weeklyPercentLabel.Text = $"{weeklyPercent}% used";

        if (usage.SevenDay?.ResetsAt != null)
        {
            DateTimeOffset resetTime = usage.SevenDay.ResetsAt.Value.ToLocalTime();
            _weeklyResetLabel.Text = $"Resets {resetTime:ddd h:mm tt}";
        }

        // Last updated
        TimeSpan timeSinceUpdate = DateTime.Now - _lastUpdated;
        if (timeSinceUpdate.TotalMinutes < 1)
        {
            _lastUpdatedLabel.Text = "Last updated: Just now";
        }
        else if (timeSinceUpdate.TotalMinutes < 60)
        {
            int mins = (int)timeSinceUpdate.TotalMinutes;
            _lastUpdatedLabel.Text = $"Last updated: {mins} minute{(mins == 1 ? "" : "s")} ago";
        }
        else
        {
            _lastUpdatedLabel.Text = $"Last updated: {_lastUpdated:h:mm tt}";
        }
    }

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        using SettingsForm settingsForm = new SettingsForm(_settingsService);
        settingsForm.ShowDialog(this);
    }

    public void ShowNearCursor()
    {
        LoggingService.Debug(LogSource, "ShowNearCursor called");

        Point cursorPos = Cursor.Position;
        Screen screen = Screen.FromPoint(cursorPos);
        Rectangle workingArea = screen.WorkingArea;

        // Position near cursor but ensure it stays on screen
        int x = cursorPos.X - Width / 2;
        int y = cursorPos.Y - Height - 10;

        // Adjust if off screen
        if (x < workingArea.Left) x = workingArea.Left;
        if (x + Width > workingArea.Right) x = workingArea.Right - Width;
        if (y < workingArea.Top) y = cursorPos.Y + 20; // Show below cursor instead
        if (y + Height > workingArea.Bottom) y = workingArea.Bottom - Height;

        Location = new Point(x, y);
        LoggingService.Debug(LogSource, $"Showing popup at ({x}, {y})");
        Show();
        Activate();
    }
}

// Custom progress bar with Claude colors
public class ClaudeProgressBar : Control
{
    private int _value;
    private int _maximum = 100;
    private Color _backgroundColor = Color.FromArgb(237, 227, 217);

    public int Value
    {
        get => _value;
        set { _value = Math.Clamp(value, 0, _maximum); Invalidate(); }
    }

    public int Maximum
    {
        get => _maximum;
        set { _maximum = Math.Max(1, value); Invalidate(); }
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; Invalidate(); }
    }

    public ClaudeProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Height = 10;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        Rectangle rect = ClientRectangle;
        int radius = rect.Height / 2;

        // Draw background with rounded corners
        using System.Drawing.Drawing2D.GraphicsPath bgPath = CreateRoundedRectPath(rect, radius);
        using SolidBrush bgBrush = new SolidBrush(_backgroundColor);
        g.FillPath(bgBrush, bgPath);

        // Draw progress with rounded corners
        if (_value > 0)
        {
            int progressWidth = (int)((float)_value / _maximum * rect.Width);
            progressWidth = Math.Max(progressWidth, rect.Height); // Minimum width for rounded appearance
            Rectangle progressRect = new Rectangle(rect.X, rect.Y, progressWidth, rect.Height);

            using System.Drawing.Drawing2D.GraphicsPath progressPath = CreateRoundedRectPath(progressRect, radius);
            using SolidBrush progressBrush = new SolidBrush(ForeColor);
            g.FillPath(progressBrush, progressPath);
        }
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
