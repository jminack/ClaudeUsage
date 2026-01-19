# Claude Usage Widget

A Windows system tray application that displays Claude Pro/Max subscription usage in real-time.

## Features

- **System Tray Integration**: Runs as a lightweight background application with a color-coded tray icon
  - Terracotta at normal levels, orange at 70%+, red at 90%+
  - Tooltip showing session and weekly usage percentages

- **Usage Monitoring**: Tracks two usage windows
  - 5-hour session window for short-term usage
  - 7-day weekly window for longer-term usage
  - Model-specific tracking (Opus and Sonnet)

- **Popup Display**: Click the tray icon to view detailed usage information
  - Progress bars with Claude's brand colors
  - Time until reset for each window
  - Last updated timestamp

- **Smart Alerts**: Configurable notifications when usage exceeds threshold
  - One alert per usage window to avoid spam
  - Automatically resets when the usage window resets

- **Settings**: Configurable poll interval, alert threshold, and notification preferences

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime
- Claude Code installed with valid OAuth credentials (`~/.claude/.credentials.json`)

## Installation

Run `ClaudeUsageWidget-Setup.exe` to install the application.

## How It Works

The widget authenticates using OAuth tokens from Claude Code's credential store and calls Anthropic's usage API to fetch current usage data. It polls at a configurable interval (default: 5 minutes) and updates the tray icon and tooltip accordingly.

## Building from Source

```bash
dotnet build
dotnet publish -c Release
```

## License

MIT License
