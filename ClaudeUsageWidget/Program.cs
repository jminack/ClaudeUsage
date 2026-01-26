using ClaudeUsageWidget.Services;

namespace ClaudeUsageWidget;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Parse command line arguments for logging
        LogLevel logLevel = ParseCommandLineArgs(args);
        LoggingService.Initialize(logLevel);

        LoggingService.Info("Program", "Application starting");
        LoggingService.Debug("Program", $"Command line args: {string.Join(" ", args)}");

        // Ensure only one instance runs
        using Mutex mutex = new Mutex(true, "ClaudeUsageWidget_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            LoggingService.Warning("Program", "Another instance is already running, exiting");
            MessageBox.Show("Claude Usage Widget is already running.", "Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        LoggingService.Debug("Program", "Single instance check passed");

        // Set up global exception handlers
        Application.ThreadException += (sender, e) =>
        {
            LoggingService.Exception("Program", e.Exception, "Unhandled thread exception");
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                LoggingService.Exception("Program", ex, "Unhandled domain exception");
            }
        };

        try
        {
            ApplicationConfiguration.Initialize();
            LoggingService.Debug("Program", "Application configuration initialized");

            Application.Run(new TrayApplicationContext());

            LoggingService.Info("Program", "Application exiting normally");
        }
        catch (Exception ex)
        {
            LoggingService.Exception("Program", ex, "Fatal error during application startup");
            throw;
        }
    }

    /// <summary>
    /// Parse command line arguments for --log-level option.
    /// Usage: ClaudeUsageWidget.exe --log-level debug|info|warning|error
    /// </summary>
    private static LogLevel ParseCommandLineArgs(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--log-level", StringComparison.OrdinalIgnoreCase) ||
                args[i].Equals("-log-level", StringComparison.OrdinalIgnoreCase))
            {
                return LoggingService.ParseLogLevel(args[i + 1]);
            }
        }

        return LogLevel.None;
    }
}
