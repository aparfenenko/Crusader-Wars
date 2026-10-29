namespace CrusaderWars.Core.Paths;

/// <summary>
/// Static access point for game paths, replacing direct Properties.Settings reads in
/// domain code. Initialized once at application startup (Program.Main on Windows,
/// MacCli Program for macOS) with the platform-appropriate IGamePaths implementation.
/// </summary>
public static class CrusaderWarsEnvironment
{
    private static IGamePaths? _paths;

    public static bool Initialized => _paths is not null;

    public static void Initialize(IGamePaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public static IGamePaths Paths =>
        _paths ?? throw new InvalidOperationException(
            "CrusaderWarsEnvironment is not initialized. Call Initialize() at application startup.");
}
