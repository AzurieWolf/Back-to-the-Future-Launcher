using System.Collections.ObjectModel;

namespace BackToTheFutureLauncher;

internal sealed class IniFile
{
    private readonly List<string> _sectionOrder = [];
    private readonly Dictionary<string, Dictionary<string, string>> _sections =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> Sections => new ReadOnlyCollection<string>(_sectionOrder);

    public static IniFile Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("launcher.ini was not found beside the launcher executable.", path);

        var ini = new IniFile();
        string? currentSection = null;

        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
                continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                if (currentSection.Length == 0)
                    continue;

                if (!ini._sections.ContainsKey(currentSection))
                {
                    ini._sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    ini._sectionOrder.Add(currentSection);
                }
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator < 1 || currentSection is null)
                continue;

            string key = line[..separator].Trim();
            string value = ParseValue(line[(separator + 1)..]);
            ini._sections[currentSection][key] = value;
        }

        return ini;
    }

    public string? Get(string section, string key) =>
        _sections.TryGetValue(section, out Dictionary<string, string>? values) &&
        values.TryGetValue(key, out string? value)
            ? value
            : null;

    private static string ParseValue(string rawValue)
    {
        string value = rawValue.Trim();
        if (value.Length >= 2 && value[0] == (char)34 && value[^1] == (char)34)
            return value[1..^1];
        return value;
    }
}
