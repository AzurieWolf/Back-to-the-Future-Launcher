using System.Reflection;

namespace BackToTheFutureLauncher;

internal static class EpisodePreferenceInstaller
{
    private sealed record Template(int EpisodeNumber, string ResourceName);

    private static readonly Template[] Templates =
    [
        new(1, "EpisodePreferences.Episode1.prefs.prop"),
        new(2, "EpisodePreferences.Episode2.prefs.prop"),
        new(3, "EpisodePreferences.Episode3.prefs.prop"),
        new(4, "EpisodePreferences.Episode4.prefs.prop"),
        new(5, "EpisodePreferences.Episode5.prefs.prop")
    ];

    public static void InstallMissing()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documents))
            throw new DirectoryNotFoundException("The Windows Documents folder could not be located.");

        InstallMissingToDocuments(documents);
    }

    internal static void InstallMissingToDocuments(string documents)
    {
        string telltaleDirectory = Path.Combine(documents, "Telltale Games");
        Directory.CreateDirectory(telltaleDirectory);

        Assembly assembly = typeof(EpisodePreferenceInstaller).Assembly;
        foreach (Template template in Templates)
        {
            string destinationPath = EpisodePreferenceLocator.GetPreferredPath(
                telltaleDirectory, template.EpisodeNumber);
            if (File.Exists(destinationPath))
                continue;

            string episodeDirectory = Path.GetDirectoryName(destinationPath)
                ?? throw new InvalidDataException("The episode preference destination is invalid.");
            Directory.CreateDirectory(episodeDirectory);
            using Stream source = assembly.GetManifestResourceStream(template.ResourceName)
                ?? throw new InvalidDataException(
                    $"The embedded preference template {template.ResourceName} is missing.");

            try
            {
                using var destination = new FileStream(
                    destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                source.CopyTo(destination);
            }
            catch (IOException) when (File.Exists(destinationPath))
            {
                // Another launcher instance created the file after our existence check.
            }
        }
    }
}
