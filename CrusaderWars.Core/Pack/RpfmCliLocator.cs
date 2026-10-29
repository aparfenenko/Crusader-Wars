namespace CrusaderWars.Core.Pack;

/// <summary>
/// Resolves the rpfm_cli executable: an explicitly configured path wins, otherwise the
/// legacy relative layout shipped with the application is used. The configured value
/// comes from the shared paths.json config (PathConfig.RpfmCli); when it is absent the
/// caller's fallback preserves the historical behavior byte-for-byte.
/// </summary>
public static class RpfmCliLocator
{
    public static string Resolve(string? configuredPath, string fallbackPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return configuredPath;

        return fallbackPath;
    }
}
