namespace ClaudeUsageWidget;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Ensure only one instance runs
        using Mutex mutex = new Mutex(true, "ClaudeUsageWidget_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Claude Usage Widget is already running.", "Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
