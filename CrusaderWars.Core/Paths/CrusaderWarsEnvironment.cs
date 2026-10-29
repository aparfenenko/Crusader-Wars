using CrusaderWars.Core.Dialogs;

namespace CrusaderWars.Core.Paths;

/// <summary>
/// Static access point for application services, replacing direct WinForms dependencies in
/// domain code. Initialized once at application startup (Program.Main on Windows,
/// MacCli Program for macOS) with the platform-appropriate implementations.
/// </summary>
public static class CrusaderWarsEnvironment
{
    private static IGamePaths? _paths;
    private static IDialogService? _dialogs;

    public static bool Initialized => _paths is not null;

    public static void Initialize(IGamePaths paths, IDialogService dialogs)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
    }

    public static IGamePaths Paths =>
        _paths ?? throw new InvalidOperationException(
            "CrusaderWarsEnvironment is not initialized. Call Initialize() at application startup.");

    public static IDialogService Dialogs =>
        _dialogs ?? throw new InvalidOperationException(
            "CrusaderWarsEnvironment is not initialized. Call Initialize() at application startup.");
}
