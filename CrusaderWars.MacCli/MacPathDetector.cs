using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;

namespace CrusaderWars.MacCli;

public sealed class DetectionResult
{
    public required string Label { get; init; }
    public required string Path { get; init; }
    public required bool Found { get; init; }
}

public sealed class MacPathDetector
{
    private readonly IAppLogger _logger;

    public MacPathDetector(IAppLogger logger)
    {
        _logger = logger;
    }

    public PathConfig Detect(string? workspaceHint = null)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string steamCommon = Path.Combine(home, "Library", "Application Support", "Steam", "steamapps", "common");
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        string ck3Documents = Path.Combine(documents, "Paradox Interactive", "Crusader Kings III");
        string saveGames = Path.Combine(ck3Documents, "save games");
        string ck3Log = Path.Combine(ck3Documents, "console_history.txt");

        string ck3App = FirstExisting(
            FindApps(Path.Combine(steamCommon, "Crusader Kings III")),
            FindApps("/Applications").Where(p => p.Contains("Crusader Kings", StringComparison.OrdinalIgnoreCase)),
            new[]
            {
                Path.Combine(steamCommon, "Crusader Kings III", "ck3.app"),
                "/Applications/Crusader Kings III.app"
            });

        string attilaApp = FirstExisting(
            FindApps(Path.Combine(steamCommon, "Total War ATTILA")),
            FindApps(Path.Combine(steamCommon, "Total War Attila")),
            FindApps("/Applications").Where(p =>
                p.Contains("Attila", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("ATTILA", StringComparison.OrdinalIgnoreCase)),
            new[]
            {
                Path.Combine(steamCommon, "Total War ATTILA", "Attila.app"),
                Path.Combine(steamCommon, "Total War ATTILA", "Total War Attila.app")
            });

        string attilaData = FindAttilaData(attilaApp, steamCommon);
        string attilaLog = string.IsNullOrEmpty(attilaData)
            ? Path.Combine(home, "Library", "Application Support", "The Creative Assembly", "Attila", "BattleResults_log.txt")
            : Path.Combine(attilaData, "BattleResults_log.txt");

        if (!File.Exists(attilaLog))
        {
            string supportLog = Path.Combine(home, "Library", "Application Support", "The Creative Assembly", "Attila", "data", "BattleResults_log.txt");
            if (File.Exists(supportLog))
                attilaLog = supportLog;
        }

        string workspace = workspaceHint ?? FindWorkspace(Directory.GetCurrentDirectory());
        string rpfm = FirstExisting(new[]
        {
            Path.Combine(workspace, "tools", "rpfm_cli"),
            Path.Combine(workspace, "data", "rpfm", "rpfm_cli"),
            "/usr/local/bin/rpfm_cli",
            "/opt/homebrew/bin/rpfm_cli"
        });

        string userScript = Path.Combine(home, "Library", "Application Support", "The Creative Assembly", "Attila", "scripts", "user.script.txt");

        var config = new PathConfig
        {
            Ck3App = ck3App,
            AttilaApp = attilaApp,
            SaveGames = saveGames,
            AttilaData = attilaData,
            AttilaLog = attilaLog,
            RpfmCli = rpfm,
            Ck3Documents = ck3Documents,
            Ck3Log = ck3Log,
            AttilaUserScript = userScript,
            WorkspaceDir = workspace,
            Ck3ProcessName = "ck3",
            AttilaProcessName = GuessAttilaProcessName(attilaApp),
            UsedModsFileName = "used_mods_cc.txt"
        };

        _logger.Debug($"Detected workspace: {workspace}");
        return config;
    }

    public IReadOnlyList<DetectionResult> Describe(PathConfig config)
    {
        return
        [
            Result("CK3 documents", config.Ck3Documents, Directory.Exists(config.Ck3Documents)),
            Result("CK3 console_history", config.Ck3Log, File.Exists(config.Ck3Log)),
            Result("CK3 save games", config.SaveGames, Directory.Exists(config.SaveGames)),
            Result("CK3 app", config.Ck3App, Path.Exists(config.Ck3App)),
            Result("Attila app", config.AttilaApp, Path.Exists(config.AttilaApp)),
            Result("Attila data", config.AttilaData, Directory.Exists(config.AttilaData)),
            Result("Attila battle log", config.AttilaLog, File.Exists(config.AttilaLog) || Directory.Exists(Path.GetDirectoryName(config.AttilaLog)!)),
            Result("Attila user.script", config.AttilaUserScript, File.Exists(config.AttilaUserScript)),
            Result("rpfm_cli", config.RpfmCli, File.Exists(config.RpfmCli)),
            Result("workspace", config.WorkspaceDir, Directory.Exists(config.WorkspaceDir)),
            Result("workspace: unit mappers", Path.Combine(config.WorkspaceDir, "unit mappers"),
                Directory.Exists(Path.Combine(config.WorkspaceDir, "unit mappers"))),
            Result("workspace: schema_att.ron", Path.Combine(config.WorkspaceDir, "data", "schema_att.ron"),
                File.Exists(Path.Combine(config.WorkspaceDir, "data", "schema_att.ron"))),
            Result("workspace: battle files", Path.Combine(config.WorkspaceDir, "data", "battle files"),
                Directory.Exists(Path.Combine(config.WorkspaceDir, "data", "battle files"))),
            Result("workspace: settings schemas", Path.Combine(config.WorkspaceDir, "settings", "schemas"),
                Directory.Exists(Path.Combine(config.WorkspaceDir, "settings", "schemas")))
        ];
    }

    private static DetectionResult Result(string label, string path, bool found) =>
        new() { Label = label, Path = string.IsNullOrEmpty(path) ? "(not found)" : path, Found = found };

    private static string FindAttilaData(string attilaApp, string steamCommon)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrEmpty(attilaApp))
        {
            string? appDir = Path.GetDirectoryName(attilaApp);
            if (appDir != null)
                candidates.Add(Path.Combine(appDir, "data"));
            candidates.Add(Path.Combine(attilaApp, "Contents", "Resources", "data"));
        }

        candidates.Add(Path.Combine(steamCommon, "Total War ATTILA", "data"));
        candidates.Add(Path.Combine(steamCommon, "Total War Attila", "data"));

        foreach (string dir in candidates)
        {
            if (Directory.Exists(dir))
                return dir;
        }

        return candidates.FirstOrDefault() ?? "";
    }

    private static string GuessAttilaProcessName(string attilaApp)
    {
        if (string.IsNullOrEmpty(attilaApp))
            return "Attila";
        string macos = Path.Combine(attilaApp, "Contents", "MacOS");
        if (Directory.Exists(macos))
        {
            string? binary = Directory.GetFiles(macos).FirstOrDefault();
            if (binary != null)
                return Path.GetFileName(binary);
        }
        return Path.GetFileNameWithoutExtension(attilaApp);
    }

    private static string FindWorkspace(string start)
    {
        DirectoryInfo? dir = new(start);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "data", "battle files"))
                || File.Exists(Path.Combine(dir.FullName, "CrusaderWars.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return start;
    }

    private static IEnumerable<string> FindApps(string directory)
    {
        if (!Directory.Exists(directory))
            yield break;
        foreach (string app in Directory.GetDirectories(directory, "*.app"))
            yield return app;
    }

    private static string FirstExisting(params IEnumerable<string>[] groups)
    {
        foreach (var group in groups)
        {
            foreach (string path in group)
            {
                if (!string.IsNullOrWhiteSpace(path) && Path.Exists(path))
                    return path;
            }
        }
        return "";
    }
}
