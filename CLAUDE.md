# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Publish standalone exe
dotnet publish -c Release -r win-x64 --self-contained false
```

## Architecture

This is a Windows Forms system tray application that displays Claude Pro/Max subscription usage by polling an undocumented Anthropic API.

### Key Components

- **TrayApplicationContext** - Main application context managing the NotifyIcon, polling timer, and alert system
- **UsagePopupForm** - Popup window displayed on tray icon click, styled to match Claude's UI
- **ClaudeProgressBar** - Custom control with rounded corners using Claude brand colors

### Services

- **CredentialsService** - Reads OAuth token from `~/.claude/.credentials.json` (created by Claude Code authentication)
- **UsageApiService** - Calls `GET https://api.anthropic.com/api/oauth/usage` with OAuth bearer token
- **SettingsService** - Persists user preferences to `%LOCALAPPDATA%/ClaudeUsageWidget/settings.json`

### API Integration

The usage API is undocumented and requires:
- Header: `Authorization: Bearer sk-ant-oat01-...`
- Header: `anthropic-beta: oauth-2025-04-20`

Response includes `five_hour` and `seven_day` utilization percentages with reset timestamps.

### Brand Colors

Claude's signature terracotta: `#D97757` (RGB 217, 119, 87)
