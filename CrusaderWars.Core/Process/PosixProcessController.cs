using System.Diagnostics;
using CrusaderWars.Core.Logging;

namespace CrusaderWars.Core.Process;

/// <summary>
/// POSIX process control via pgrep / kill -STOP / kill -CONT. Works on macOS and Linux.
/// </summary>
public sealed class PosixProcessController : IProcessController
{
    private readonly IAppLogger _logger;
    private readonly bool _isSupported;

    public PosixProcessController(IAppLogger logger)
    {
        _logger = logger;
        _isSupported = CheckCommandsAvailable();
        if (!_isSupported)
            _logger.Warn("pgrep/kill are not available. Process suspend/resume is disabled.");
    }

    public bool IsSupported => _isSupported;

    public bool IsProcessRunning(string processName) => FindPids(processName).Count > 0;

    public IReadOnlyList<int> FindPids(string processName)
    {
        var pids = new SortedSet<int>();

        foreach (string args in new[] { $"-f {Quote(processName)}", Quote(processName) })
        {
            foreach (int pid in RunPgrep(args))
                pids.Add(pid);
        }

        string nameWithoutExt = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName[..^4]
            : processName;
        string appName = nameWithoutExt.EndsWith(".app", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(nameWithoutExt)
            : nameWithoutExt;

        try
        {
            foreach (var proc in System.Diagnostics.Process.GetProcessesByName(appName))
                pids.Add(proc.Id);
        }
        catch (Exception ex)
        {
            _logger.Debug($"GetProcessesByName({appName}) failed: {ex.Message}");
        }

        return pids.ToList();
    }

    public void SuspendProcess(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            throw new ArgumentException("Process name cannot be empty.", nameof(processName));
        EnsureSupported();

        int pid = RequirePid(processName);
        SendSignal(pid, "STOP");
        _logger.Info($"Suspended '{processName}' (PID {pid}).");
    }

    public void ResumeProcess(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            throw new ArgumentException("Process name cannot be empty.", nameof(processName));
        EnsureSupported();

        int pid = RequirePid(processName);
        SendSignal(pid, "CONT");
        _logger.Info($"Resumed '{processName}' (PID {pid}).");
    }

    public void KillProcess(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            throw new ArgumentException("Process name cannot be empty.", nameof(processName));

        var pids = FindPids(processName);
        if (pids.Count == 0)
        {
            _logger.Debug($"No '{processName}' process to kill.");
            return;
        }

        foreach (int pid in pids)
        {
            try
            {
                SendSignal(pid, "TERM");
                _logger.Info($"Sent SIGTERM to '{processName}' (PID {pid}).");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to terminate PID {pid}: {ex.Message}");
            }
        }
    }

    private int RequirePid(string processName)
    {
        var pids = FindPids(processName);
        if (pids.Count == 0)
            throw new InvalidOperationException($"Process '{processName}' not found.");
        return pids[0];
    }

    private void EnsureSupported()
    {
        if (!_isSupported)
        {
            throw new PlatformNotSupportedException(
                "Process suspend/resume requires 'kill' and 'pgrep'.");
        }
    }

    private bool CheckCommandsAvailable()
    {
        try
        {
            int pgrepCode = RunSilent("pgrep", "-x init");
            if (pgrepCode != 0 && pgrepCode != 1)
                return false;

            int killCode = RunSilent("kill", "-0 1");
            return killCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Debug($"Command availability check failed: {ex.Message}");
            return false;
        }
    }

    private static IEnumerable<int> RunPgrep(string arguments)
    {
        using var proc = Start("pgrep", arguments);
        string output = proc.StandardOutput.ReadToEnd().Trim();
        proc.WaitForExit(3000);
        if (proc.ExitCode != 0 || string.IsNullOrEmpty(output))
            yield break;

        foreach (string line in output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(line.Trim(), out int pid))
                yield return pid;
        }
    }

    private static void SendSignal(int pid, string signal)
    {
        using var proc = Start("kill", $"-{signal} {pid}");
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(3000);
        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to send SIG{signal} to PID {pid}: {stderr.Trim()}");
        }
    }

    private static int RunSilent(string fileName, string arguments)
    {
        using var proc = Start(fileName, arguments);
        proc.WaitForExit(3000);
        return proc.ExitCode;
    }

    private static System.Diagnostics.Process Start(string fileName, string arguments)
    {
        var proc = new System.Diagnostics.Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        proc.Start();
        return proc;
    }

    private static string Quote(string value) => value.Contains(' ') ? $"\"{value}\"" : value;
}
