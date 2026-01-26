# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Publish standalone exe (single file, ~146MB)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Build installer (requires NSIS: https://nsis.sourceforge.io)
makensis installer\ClaudeUsageWidget.nsi
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

## Code Style

### Avoid `var` - Use Explicit Types

Use explicit type declarations instead of `var`. This makes code readable without IDE assistance.

```csharp
// Preferred
UsageResponse result = GetUsage();
string name = "hello";

// Avoid
var result = GetUsage();
var name = "hello";
```

**Exceptions** - `var` is acceptable when:
- Complex LINQ results: `var grouped = items.GroupBy(x => x.Category)`
- Anonymous types: `var x = new { Name = "test", Value = 1 }`
- The type is verbose and visible on the same line: `var items = new Dictionary<string, List<int>>()`
