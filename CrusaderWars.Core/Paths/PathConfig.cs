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
