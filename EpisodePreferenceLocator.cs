namespace BackToTheFutureLauncher;

internal static class EpisodePreferenceLocator
{
    public static string ResolveConfiguredPath(string configuredPath, string section)
    {
        if (!TryGetEpisodeNumber(section, out int episodeNumber))
            return configuredPath;

        string? configuredDirectory = Path.GetDirectoryName(configuredPath);
        if (configuredDirectory is null)
            return configuredPath;

        string? telltaleDirectory = Directory.GetParent(configuredDirectory)?.FullName;
        if (telltaleDirectory is null)
            return configuredPath;

        string configuredFolder = Path.GetFileName(configuredDirectory) ?? string.Empty;
        if (!configuredFolder.Equals($"Episode {episodeNumber}", StringComparison.OrdinalIgnoreCase) &&
            !configuredFolder.Equals($"Back to the Future {episodeNumber}", StringComparison.OrdinalIgnoreCase))
            return configuredPath;

        return GetPreferredPath(telltaleDirectory, episodeNumber);
    }

    public static string GetPreferredPath(string telltaleDirectory, int episodeNumber)
    {
        string episodePath = Path.Combine(
            telltaleDirectory, $"Episode {episodeNumber}", "prefs.prop");
        if (File.Exists(episodePath))
            return episodePath;

        string gameNamePath = Path.Combine(
            telltaleDirectory, $"Back to the Future {episodeNumber}", "prefs.prop");
        return File.Exists(gameNamePath) ? gameNamePath : episodePath;
    }

    private static bool TryGetEpisodeNumber(string section, out int episodeNumber)
    {
        string digits = new(section.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out episodeNumber) && episodeNumber is >= 1 and <= 5;
    }
}
