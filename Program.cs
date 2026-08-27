namespace BackToTheFutureLauncher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        string baseDirectory = AppContext.BaseDirectory;
        string configPath = Path.Combine(baseDirectory, "launcher.ini");

        try
        {
            LauncherConfig config = LauncherConfig.Load(configPath);
            EpisodePreferenceInstaller.InstallMissing();
            Application.Run(new LauncherForm(config, baseDirectory));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"The launcher could not start.\n\n{ex.Message}\n\nConfiguration: {configPath}",
                "Launcher error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
