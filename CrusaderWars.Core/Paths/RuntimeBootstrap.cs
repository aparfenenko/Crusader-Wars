using CrusaderWars.Core.Dialogs;
using CrusaderWars.Core.Logging;
using CrusaderWars.client;
using CrusaderWars.mod_manager;

namespace CrusaderWars.Core.Paths;

/// <summary>
/// Shared runtime bootstrap for the mac front-ends (CLI and Avalonia UI): loads or
/// detects the path config, points the working directory at the workspace, initializes
/// the environment gateway and loads the settings files.
/// </summary>
public static class RuntimeBootstrap
{
    public static (AppPaths Paths, MacPathDetector Detector) Initialize(IAppLogger logger, IDialogService dialogs)
    {
        var detector = new MacPathDetector(logger);
        PathConfig config = File.Exists(PathConfig.DefaultConfigPath) ? PathConfig.Load() : detector.Detect();
        if (!File.Exists(PathConfig.DefaultConfigPath))
        {
            config.Save();
            logger.Info($"Wrote {PathConfig.DefaultConfigPath} (run 'detect' to refresh).");
        }

        var paths = AppPaths.FromConfig(config);

        // Domain code resolves workspace-relative paths (settings/Options.xml, data/...)
        // from the current directory, mirroring the Windows app layout.
        if (Directory.Exists(paths.WorkspaceDir))
            Environment.CurrentDirectory = paths.WorkspaceDir;

        CrusaderWarsEnvironment.Initialize(new AppPathsGamePaths(paths), dialogs, logger);
        if (!OptionsFileStore.ReadOptionsFile() || !ActiveSubmods.Load())
        {
            throw new InvalidOperationException("Settings load failed.");
        }

        return (paths, detector);
    }
}
