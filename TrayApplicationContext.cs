namespace ClaudeUsageWidget;

/// <summary>
/// Application lifetime management only.
/// Creates state and controller, handles shutdown.
/// </summary>
public class TrayApplicationContext : ApplicationContext
{
    private readonly AppState _state;
    private readonly UsageController _controller;

    public TrayApplicationContext()
    {
        _state = new AppState();
        _controller = new UsageController(_state);
        _controller.OnExitRequested += ExitApplication;
        _controller.Initialize();
    }

    private void ExitApplication()
    {
        _state.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state.Dispose();
        }
        base.Dispose(disposing);
    }
}
