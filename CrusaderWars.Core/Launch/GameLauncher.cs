using System.Diagnostics;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;
using CrusaderWars.Core.Process;

namespace CrusaderWars.Core.Launch;

public sealed class GameLauncher
{
    private readonly AppPaths _paths;
    private readonly IAppLogger _logger;
    private readonly IProcessController _processes;

    public GameLauncher(AppPaths paths, IAppLogger logger, IProcessController processes)
    {
        _paths = paths;
        _logger = logger;
        _processes = processes;
    }

    public void LaunchCk3(bool continueLastSave)
    {
        if (_processes.IsProcessRunning(_paths.Ck3ProcessName) || LooksRunning(_paths.Ck3App))
        {
            _logger.Info("CK3 is already running.");
            return;
        }

        string args = continueLastSave ? "--continuelastsave" : "";
        StartBundleOrBinary(_paths.Ck3App, args, workingDirectory: null);
        _logger.Info($"Launched CK3 from {_paths.Ck3App} {args}".Trim());
    }

    public void LaunchAttila()
    {
        if (string.IsNullOrWhiteSpace(_paths.AttilaApp))
            throw new InvalidOperationException("Attila app path is not configured.");

        string workingDir = ResolveAttilaWorkingDirectory();
        string modsArg = _paths.UsedModsFileName;
        StartBundleOrBinary(_paths.AttilaApp, modsArg, workingDir);
        _logger.Info($"Launched Attila from {_paths.AttilaApp} with '{modsArg}' (cwd {workingDir})");
    }

    public void CloseAttila() => _processes.KillProcess(_paths.AttilaProcessName);

    public void SuspendCk3() => _processes.SuspendProcess(_paths.Ck3ProcessName);

    public void ResumeCk3()
    {
        try
        {
            _processes.ResumeProcess(_paths.Ck3ProcessName);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Could not resume CK3 ({ex.Message}); launching with --continuelastsave.");
            LaunchCk3(continueLastSave: true);
        }
    }

    private string ResolveAttilaWorkingDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_paths.AttilaData) && Directory.Exists(_paths.AttilaData))
        {
            string? parent = Path.GetDirectoryName(_paths.AttilaData.TrimEnd(Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(parent))
                return parent;
        }

        string app = _paths.AttilaApp;
        if (app.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            return Path.GetDirectoryName(app) ?? Directory.GetCurrentDirectory();

        return Path.GetDirectoryName(app) ?? Directory.GetCurrentDirectory();
    }

    private void StartBundleOrBinary(string path, string arguments, string? workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.Exists(path))
            throw new FileNotFoundException($"Game path not found: {path}");

        if (path.EndsWith(".app", StringComparison.OrdinalIgnoreCase) || Directory.Exists(path))
        {
            string? binary = FindMacOsBinary(path);
            if (binary != null)
            {
                StartProcess(binary, arguments, workingDirectory ?? Path.GetDirectoryName(path));
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "open",
                Arguments = string.IsNullOrWhiteSpace(arguments)
                    ? $"-a \"{path}\""
                    : $"-n -a \"{path}\" --args {arguments}",
                UseShellExecute = false
            };
            System.Diagnostics.Process.Start(startInfo);
            return;
        }

        StartProcess(path, arguments, workingDirectory ?? Path.GetDirectoryName(path));
    }

    private static void StartProcess(string fileName, string arguments, string? workingDirectory)
    {
        var info = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? ""
        };
        System.Diagnostics.Process.Start(info);
    }

    private static string? FindMacOsBinary(string appPath)
    {
        string macos = Path.Combine(appPath, "Contents", "MacOS");
        if (!Directory.Exists(macos))
            return null;
        return Directory.GetFiles(macos).FirstOrDefault();
    }

    private bool LooksRunning(string appPath)
    {
        if (string.IsNullOrWhiteSpace(appPath))
            return false;
        string name = Path.GetFileNameWithoutExtension(appPath);
        return _processes.IsProcessRunning(name);
    }
}
