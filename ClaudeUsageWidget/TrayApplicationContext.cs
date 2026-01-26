using ClaudeUsageWidget.Services;

namespace ClaudeUsageWidget;

/// <summary>
/// Application lifetime management only.
/// Creates state and controller, handles shutdown.
/// </summary>
public class TrayApplicationContext : ApplicationContext
{
    private const string LogSource = "TrayApplicationContext";
    private readonly AppState _state;
    private readonly UsageController _controller;

    public TrayApplicationContext()
    {
        LoggingService.Info(LogSource, "Creating TrayApplicationContext");

        LoggingService.Debug(LogSource, "Creating AppState");
        _state = new AppState();

        LoggingService.Debug(LogSource, "Creating UsageController");
        _controller = new UsageController(_state);
        _controller.OnExitRequested += ExitApplication;

        LoggingService.Debug(LogSource, "Initializing controller");
        _controller.Initialize();

        LoggingService.Info(LogSource, "TrayApplicationContext initialized");
    }

    private void ExitApplication()
    {
        LoggingService.Info(LogSource, "Exit requested, disposing and exiting application");
        _state.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        LoggingService.Debug(LogSource, $"Dispose called (disposing={disposing})");
        if (disposing)
        {
            _state.Dispose();
        }
        base.Dispose(disposing);
    }
}
