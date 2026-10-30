using CrusaderWars.Core.Attila;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;
using Xunit;

namespace CrusaderWars.Core.Tests;

public class AttilaUserScriptTests : IDisposable
{
    private readonly string _dir;
    private readonly RecordingLogger _logger;
    private readonly AppPaths _paths;

    public AttilaUserScriptTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "cc-user-script-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _logger = new RecordingLogger();
        _paths = MinimalPaths(_dir);
    }

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
    }

    private static AppPaths MinimalPaths(string dir) => new()
    {
        Ck3App = "/ck3.app",
        AttilaApp = "/attila.app",
        SaveGames = Path.Combine(dir, "save games"),
        AttilaData = Path.Combine(dir, "data"),
        AttilaLog = Path.Combine(dir, "data", "BattleResults_log.txt"),
        RpfmCli = "/usr/local/bin/rpfm_cli",
        Ck3Documents = dir,
        Ck3Log = Path.Combine(dir, "console_history.txt"),
        AttilaUserScript = Path.Combine(dir, "user.script.txt"),
        WorkspaceDir = dir,
        Ck3ProcessName = "ck3",
        AttilaProcessName = "Attila",
        UsedModsFileName = "used_mods_cc.txt"
    };

    [Fact]
    public void Pack_enabled_when_line_present()
    {
        File.WriteAllText(_paths.AttilaUserScript, "mod other.pack;\nmod CrusaderConflicts.pack;\n");

        Assert.True(new AttilaUserScript(_paths, _logger).PackEnabled());
    }

    [Fact]
    public void Pack_not_enabled_when_file_missing_or_without_pack_line()
    {
        Assert.False(new AttilaUserScript(_paths, _logger).PackEnabled());

        File.WriteAllText(_paths.AttilaUserScript, "mod some_other.pack;\n");

        Assert.False(new AttilaUserScript(_paths, _logger).PackEnabled());
    }

    [Fact]
    public void Warn_if_pack_missing_logs_but_never_modifies_or_throws()
    {
        File.WriteAllText(_paths.AttilaUserScript, "mod some_other.pack;\n");
        string before = File.ReadAllText(_paths.AttilaUserScript);

        new AttilaUserScript(_paths, _logger).WarnIfPackMissing();

        Assert.Contains(_logger.Warnings, w => w.Contains("CrusaderConflicts.pack"));
        Assert.Equal(before, File.ReadAllText(_paths.AttilaUserScript));
    }

    [Fact]
    public void Warn_if_pack_missing_is_silent_when_pack_enabled()
    {
        File.WriteAllText(_paths.AttilaUserScript, "mod CrusaderConflicts.pack;\n");

        new AttilaUserScript(_paths, _logger).WarnIfPackMissing();

        Assert.Empty(_logger.Warnings);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<string> Warnings { get; } = new();

        public void Debug(string message) { }

        public void Info(string message) { }

        public void Warn(string message) => Warnings.Add(message);

        public void Error(string message) { }
    }
}
