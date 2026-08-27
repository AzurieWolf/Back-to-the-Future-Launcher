namespace BackToTheFutureLauncher;

internal sealed record Episode(string Section, string Name, string Executable, string? Preferences);

internal sealed class LauncherConfig
{
    public string Title { get; private init; } = "Game Episode Launcher";
    public string Heading { get; private init; } = "SELECT AN EPISODE";
    public string Background { get; private init; } = "background.jpg";
    public string Icon { get; private init; } = "launcher.ico";
    public int Width { get; private init; } = 960;
    public int Height { get; private init; } = 600;
    public IReadOnlyList<Episode> Episodes { get; private init; } = [];

    public static LauncherConfig Load(string path)
    {
        IniFile ini = IniFile.Load(path);
        var episodes = new List<Episode>();

        foreach (string section in ini.Sections)
        {
            if (section.Equals("launcher", StringComparison.OrdinalIgnoreCase))
                continue;

            string? name = ini.Get(section, "name");
            string? executable = ini.Get(section, "executable");
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(executable))
                episodes.Add(new Episode(section, name, executable, ini.Get(section, "preferences")));
        }

        if (episodes.Count == 0)
            throw new InvalidDataException("launcher.ini does not contain any valid episode sections.");

        return new LauncherConfig
        {
            Title = ValueOrDefault(ini.Get("launcher", "title"), "Game Episode Launcher"),
            Heading = ValueOrDefault(ini.Get("launcher", "heading"), "SELECT AN EPISODE"),
            Background = ValueOrDefault(ini.Get("launcher", "background"), "background.jpg"),
            Icon = ValueOrDefault(ini.Get("launcher", "icon"), "launcher.ico"),
            Width = ParseDimension(ini.Get("launcher", "width"), 960, 640, 3840),
            Height = ParseDimension(ini.Get("launcher", "height"), 600, 420, 2160),
            Episodes = episodes
        };
    }

    private static string ValueOrDefault(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static int ParseDimension(string? value, int fallback, int minimum, int maximum) =>
        int.TryParse(value, out int result) ? Math.Clamp(result, minimum, maximum) : fallback;
}
