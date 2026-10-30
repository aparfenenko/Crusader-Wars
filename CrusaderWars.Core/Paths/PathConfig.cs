using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrusaderWars.Core.Paths;

public sealed class PathConfig
{
    [JsonPropertyName("ck3App")]
    public string Ck3App { get; set; } = "";

    [JsonPropertyName("attilaApp")]
    public string AttilaApp { get; set; } = "";

    [JsonPropertyName("saveGames")]
    public string SaveGames { get; set; } = "";

    [JsonPropertyName("attilaData")]
    public string AttilaData { get; set; } = "";

    [JsonPropertyName("attilaLog")]
    public string AttilaLog { get; set; } = "";

    [JsonPropertyName("rpfmCli")]
    public string RpfmCli { get; set; } = "";

    [JsonPropertyName("ck3Documents")]
    public string Ck3Documents { get; set; } = "";

    [JsonPropertyName("ck3Log")]
    public string Ck3Log { get; set; } = "";

    [JsonPropertyName("attilaUserScript")]
    public string AttilaUserScript { get; set; } = "";

    [JsonPropertyName("workspaceDir")]
    public string WorkspaceDir { get; set; } = "";

    [JsonPropertyName("ck3ProcessName")]
    public string Ck3ProcessName { get; set; } = "ck3";

    [JsonPropertyName("attilaProcessName")]
    public string AttilaProcessName { get; set; } = "Attila";

    [JsonPropertyName("usedModsFileName")]
    public string UsedModsFileName { get; set; } = "used_mods_cc.txt";

    /// <summary>
    /// Fills every empty path field from <paramref name="detected"/>. Manually set values
    /// always win: the user's own game locations (GOG, custom installs) are never
    /// overwritten by auto-detection.
    /// </summary>
    public void FillMissingFrom(PathConfig detected)
    {
        if (string.IsNullOrWhiteSpace(Ck3App)) Ck3App = detected.Ck3App;
        if (string.IsNullOrWhiteSpace(AttilaApp)) AttilaApp = detected.AttilaApp;
        if (string.IsNullOrWhiteSpace(SaveGames)) SaveGames = detected.SaveGames;
        if (string.IsNullOrWhiteSpace(AttilaData)) AttilaData = detected.AttilaData;
        if (string.IsNullOrWhiteSpace(AttilaLog)) AttilaLog = detected.AttilaLog;
        if (string.IsNullOrWhiteSpace(RpfmCli)) RpfmCli = detected.RpfmCli;
        if (string.IsNullOrWhiteSpace(Ck3Documents)) Ck3Documents = detected.Ck3Documents;
        if (string.IsNullOrWhiteSpace(Ck3Log)) Ck3Log = detected.Ck3Log;
        if (string.IsNullOrWhiteSpace(AttilaUserScript)) AttilaUserScript = detected.AttilaUserScript;
        if (string.IsNullOrWhiteSpace(WorkspaceDir)) WorkspaceDir = detected.WorkspaceDir;
        if (string.IsNullOrWhiteSpace(Ck3ProcessName)) Ck3ProcessName = detected.Ck3ProcessName;
        if (string.IsNullOrWhiteSpace(AttilaProcessName)) AttilaProcessName = detected.AttilaProcessName;
        if (string.IsNullOrWhiteSpace(UsedModsFileName)) UsedModsFileName = detected.UsedModsFileName;
    }

    /// <summary>
    /// Sets a path field by its JSON name (as used in paths.json, e.g. "ck3App").
    /// Returns false for unknown keys.
    /// </summary>
    public bool SetByName(string name, string value)
    {
        switch (name)
        {
            case "ck3App": Ck3App = value; return true;
            case "attilaApp": AttilaApp = value; return true;
            case "saveGames": SaveGames = value; return true;
            case "attilaData": AttilaData = value; return true;
            case "attilaLog": AttilaLog = value; return true;
            case "rpfmCli": RpfmCli = value; return true;
            case "ck3Documents": Ck3Documents = value; return true;
            case "ck3Log": Ck3Log = value; return true;
            case "attilaUserScript": AttilaUserScript = value; return true;
            case "workspaceDir": WorkspaceDir = value; return true;
            case "ck3ProcessName": Ck3ProcessName = value; return true;
            case "attilaProcessName": AttilaProcessName = value; return true;
            case "usedModsFileName": UsedModsFileName = value; return true;
            default: return false;
        }
    }

    /// <summary>True when the field identified by its JSON name points at an existing file or folder.</summary>
    public bool ExistsByName(string name) => name switch
    {
        "ck3App" => string.IsNullOrEmpty(Ck3App) || Directory.Exists(Ck3App),
        "attilaApp" => string.IsNullOrEmpty(AttilaApp) || Directory.Exists(AttilaApp),
        "saveGames" => string.IsNullOrEmpty(SaveGames) || Directory.Exists(SaveGames),
        "attilaData" => string.IsNullOrEmpty(AttilaData) || Directory.Exists(AttilaData),
        "attilaLog" => string.IsNullOrEmpty(AttilaLog) || File.Exists(AttilaLog),
        "rpfmCli" => string.IsNullOrEmpty(RpfmCli) || File.Exists(RpfmCli),
        "ck3Documents" => string.IsNullOrEmpty(Ck3Documents) || Directory.Exists(Ck3Documents),
        "ck3Log" => string.IsNullOrEmpty(Ck3Log) || File.Exists(Ck3Log),
        "attilaUserScript" => string.IsNullOrEmpty(AttilaUserScript) || File.Exists(AttilaUserScript),
        "workspaceDir" => string.IsNullOrEmpty(WorkspaceDir) || Directory.Exists(WorkspaceDir),
        _ => true
    };

    public static string DefaultConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "crusader-conflicts");

    public static string DefaultConfigPath => Path.Combine(DefaultConfigDirectory, "paths.json");

    public static PathConfig Load(string? path = null)
    {
        path ??= DefaultConfigPath;
        if (!File.Exists(path))
            return new PathConfig();

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PathConfig>(json, JsonOptions) ?? new PathConfig();
    }

    public void Save(string? path = null)
    {
        path ??= DefaultConfigPath;
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
