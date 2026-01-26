namespace ClaudeUsageWidget.Services;

/// <summary>
/// Log levels from most verbose to least verbose.
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    None = 4
}

/// <summary>
/// Simple file-based logging service with configurable log levels.
/// Logs to date-stamped files in %LOCALAPPDATA%/ClaudeUsageWidget/logs/.
/// </summary>
public static class LoggingService
{
    private static LogLevel _currentLevel = LogLevel.None;
    private static string? _logDirectory;
    private static readonly object _lock = new object();
    private static bool _initialized;

    /// <summary>
    /// Initialize the logging service with the specified log level.
    /// </summary>
    /// <param name="level">The minimum log level to record.</param>
    public static void Initialize(LogLevel level)
    {
        _currentLevel = level;

        if (level == LogLevel.None)
        {
            _initialized = false;
            return;
        }

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDirectory = Path.Combine(appData, "ClaudeUsageWidget", "logs");
        Directory.CreateDirectory(_logDirectory);

        _initialized = true;

        // Log startup information
        Info("LoggingService", $"Logging initialized at level: {level}");
        Info("LoggingService", $"Log directory: {_logDirectory}");
        Info("LoggingService", $"Application version: {GetApplicationVersion()}");
        Info("LoggingService", $"OS: {Environment.OSVersion}");
        Info("LoggingService", $".NET version: {Environment.Version}");
    }

    /// <summary>
    /// Gets the current log level.
    /// </summary>
    public static LogLevel CurrentLevel => _currentLevel;

    /// <summary>
    /// Returns true if logging is enabled (level is not None).
    /// </summary>
    public static bool IsEnabled => _currentLevel != LogLevel.None && _initialized;

    /// <summary>
    /// Log a debug message.
    /// </summary>
    public static void Debug(string source, string message)
    {
        Log(LogLevel.Debug, source, message);
    }

    /// <summary>
    /// Log an info message.
    /// </summary>
    public static void Info(string source, string message)
    {
        Log(LogLevel.Info, source, message);
    }

    /// <summary>
    /// Log a warning message.
    /// </summary>
    public static void Warning(string source, string message)
    {
        Log(LogLevel.Warning, source, message);
    }

    /// <summary>
    /// Log an error message.
    /// </summary>
    public static void Error(string source, string message)
    {
        Log(LogLevel.Error, source, message);
    }

    /// <summary>
    /// Log an exception with optional context message.
    /// </summary>
    public static void Exception(string source, Exception ex, string? context = null)
    {
        string message = context != null
            ? $"{context}: {ex.GetType().Name}: {ex.Message}"
            : $"{ex.GetType().Name}: {ex.Message}";

        Log(LogLevel.Error, source, message);

        if (_currentLevel == LogLevel.Debug && ex.StackTrace != null)
        {
            Log(LogLevel.Debug, source, $"Stack trace: {ex.StackTrace}");
        }

        if (ex.InnerException != null)
        {
            Exception(source, ex.InnerException, "Inner exception");
        }
    }

    /// <summary>
    /// Parse a log level from a command-line argument string.
    /// </summary>
    /// <param name="value">The string value (e.g., "debug", "info", "warning", "error").</param>
    /// <returns>The parsed LogLevel, or LogLevel.None if invalid.</returns>
    public static LogLevel ParseLogLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return LogLevel.None;
        }

        return value.ToLowerInvariant() switch
        {
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Info,
            "warning" or "warn" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "none" => LogLevel.None,
            _ => LogLevel.None
        };
    }

    /// <summary>
    /// Get the path to today's log file.
    /// </summary>
    public static string? GetCurrentLogFilePath()
    {
        if (!_initialized || _logDirectory == null)
        {
            return null;
        }

        return Path.Combine(_logDirectory, $"log-{DateTime.Now:yyyy-MM-dd}.txt");
    }

    private static void Log(LogLevel level, string source, string message)
    {
        if (!_initialized || level < _currentLevel || _logDirectory == null)
        {
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string levelStr = level.ToString().ToUpperInvariant().PadRight(7);
        string logLine = $"[{timestamp}] [{levelStr}] [{source}] {message}";

        string logFilePath = Path.Combine(_logDirectory, $"log-{DateTime.Now:yyyy-MM-dd}.txt");

        lock (_lock)
        {
            try
            {
                File.AppendAllText(logFilePath, logLine + Environment.NewLine);
            }
            catch
            {
                // Silently fail if we can't write to log - don't crash the app
            }
        }
    }

    private static string GetApplicationVersion()
    {
        try
        {
            System.Reflection.Assembly? assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Version? version = assembly?.GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}
