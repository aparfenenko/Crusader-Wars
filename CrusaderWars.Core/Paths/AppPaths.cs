namespace CrusaderWars.Core.Paths;

public sealed class AppPaths
{
    public required string Ck3App { get; init; }
    public required string AttilaApp { get; init; }
    public required string SaveGames { get; init; }
    public required string AttilaData { get; init; }
    public required string AttilaLog { get; init; }
    public required string RpfmCli { get; init; }
    public required string Ck3Documents { get; init; }
    public required string Ck3Log { get; init; }
    public required string AttilaUserScript { get; init; }
    public required string WorkspaceDir { get; init; }
    public required string Ck3ProcessName { get; init; }
    public required string AttilaProcessName { get; init; }
    public required string UsedModsFileName { get; init; }

    public string SaveWorkDir => Path.Combine(WorkspaceDir, "data", "save_file_data");
    public string BattleFilesDir => Path.Combine(WorkspaceDir, "data", "battle files");
    public string SchemaRon => Path.Combine(WorkspaceDir, "data", "schema_att.ron");
    public string Thumbnail => Path.Combine(WorkspaceDir, "settings", "CrusaderWars.png");
    public string ExistingPack => Path.Combine(WorkspaceDir, "CrusaderConflicts.pack");
    public string LastSaveCk3 => Path.Combine(Path.GetDirectoryName(SaveGames.TrimEnd(Path.DirectorySeparatorChar)) ?? SaveGames, "last_save.ck3");
    public string BattleResultsCk3 => Path.Combine(SaveGames, "battle_results.ck3");
    public string ContinueGameJson => Path.Combine(Path.GetDirectoryName(SaveGames.TrimEnd(Path.DirectorySeparatorChar)) ?? SaveGames, "continue_game.json");
    public string UsedModsPath => Path.Combine(AttilaData, UsedModsFileName);

    public static AppPaths FromConfig(PathConfig config)
    {
        string workspace = string.IsNullOrWhiteSpace(config.WorkspaceDir)
            ? Directory.GetCurrentDirectory()
            : config.WorkspaceDir;

        string ck3Documents = string.IsNullOrWhiteSpace(config.Ck3Documents)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Crusader Kings III")
            : config.Ck3Documents;

        string saveGames = string.IsNullOrWhiteSpace(config.SaveGames)
            ? Path.Combine(ck3Documents, "save games")
            : config.SaveGames;

        string ck3Log = string.IsNullOrWhiteSpace(config.Ck3Log)
            ? Path.Combine(ck3Documents, "console_history.txt")
            : config.Ck3Log;

        string userScript = string.IsNullOrWhiteSpace(config.AttilaUserScript)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "The Creative Assembly", "Attila", "scripts", "user.script.txt")
            : config.AttilaUserScript;

        string attilaData = config.AttilaData;
        if (string.IsNullOrWhiteSpace(attilaData) && !string.IsNullOrWhiteSpace(config.AttilaApp))
        {
            // Default: data/ sits next to the .app bundle (mac Steam layout).
            var appDir = Path.GetDirectoryName(config.AttilaApp);
            if (!string.IsNullOrEmpty(appDir))
                attilaData = Path.Combine(appDir, "data");
        }

        string attilaLog = config.AttilaLog;
        if (string.IsNullOrWhiteSpace(attilaLog) && !string.IsNullOrWhiteSpace(attilaData))
            attilaLog = Path.Combine(attilaData, "BattleResults_log.txt");

        return new AppPaths
        {
            Ck3App = config.Ck3App,
            AttilaApp = config.AttilaApp,
            SaveGames = saveGames,
            AttilaData = attilaData,
            AttilaLog = attilaLog,
            RpfmCli = config.RpfmCli,
            Ck3Documents = ck3Documents,
            Ck3Log = ck3Log,
            AttilaUserScript = userScript,
            WorkspaceDir = workspace,
            Ck3ProcessName = string.IsNullOrWhiteSpace(config.Ck3ProcessName) ? "ck3" : config.Ck3ProcessName,
            AttilaProcessName = string.IsNullOrWhiteSpace(config.AttilaProcessName) ? "Attila" : config.AttilaProcessName,
            UsedModsFileName = string.IsNullOrWhiteSpace(config.UsedModsFileName) ? "used_mods_cc.txt" : config.UsedModsFileName
        };
    }
}
