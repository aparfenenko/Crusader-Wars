using CrusaderWars.Core.Dialogs;
using CrusaderWars.Core.Logging;

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
    private static IAppLogger? _logger;

    public static bool Initialized => _paths is not null;

    public static void Initialize(IGamePaths paths, IDialogService dialogs, IAppLogger logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public static IGamePaths Paths =>
        _paths ?? throw NotInitialized();

    public static IDialogService Dialogs =>
        _dialogs ?? throw NotInitialized();

    public static IAppLogger Logger =>
        _logger ?? throw NotInitialized();

    private static InvalidOperationException NotInitialized() =>
        new("CrusaderWarsEnvironment is not initialized. Call Initialize() at application startup.");
}
