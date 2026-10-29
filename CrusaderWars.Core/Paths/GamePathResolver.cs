namespace CrusaderWars.Core.Paths;

public enum GamePlatform
{
    Windows,
    MacOs
}

public sealed record GamePaths(
    string Ck3ExecutablePath,
    string AttilaExecutablePath,
    string Ck3ConsoleLogPath,
    string AttilaBattleLogPath,
    string Ck3SaveGamesDirectory) : IGamePaths;

/// <summary>
/// Derives the five path facts from raw inputs. The Windows branch is a byte-for-byte
/// replica of the legacy derivations (MainFile.cs startup block, BattleResult.cs /
/// BattleResultReader.cs log derivation); parity is pinned by tests and must not drift.
/// </summary>
public static class GamePathResolver
{
    public static GamePaths Resolve(GamePlatform platform, string documentsPath, string attilaExePath, string ck3ExePath) =>
        platform switch
        {
            GamePlatform.Windows => ResolveWindows(documentsPath, attilaExePath, ck3ExePath),
            GamePlatform.MacOs => ResolveMac(documentsPath, attilaExePath, ck3ExePath),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };

    public static GamePaths ResolveWindows(string documentsPath, string attilaExePath, string ck3ExePath)
    {
        string ck3Root = documentsPath + "\\Paradox Interactive\\Crusader Kings III";
        return new GamePaths(
            Ck3ExecutablePath: ck3ExePath,
            AttilaExecutablePath: attilaExePath,
            Ck3ConsoleLogPath: ck3Root + "\\console_history.txt",
            AttilaBattleLogPath: DeriveWindowsAttilaBattleLog(attilaExePath),
            Ck3SaveGamesDirectory: ck3Root + "\\save games");
    }

    // Byte-for-byte replica of the legacy BattleResult.cs / BattleResultReader.cs formula,
    // including its culture-sensitive IndexOf overload and the throw when "Attila.exe"
    // is absent from the path.
    public static string DeriveWindowsAttilaBattleLog(string attilaExePath) =>
        attilaExePath.Substring(0, attilaExePath.IndexOf("Attila.exe")) + "data\\BattleResults_log.txt";

    public static GamePaths ResolveMac(string documentsPath, string attilaAppPath, string ck3AppPath)
    {
        string ck3Root = documentsPath + "/Paradox Interactive/Crusader Kings III";
        return new GamePaths(
            Ck3ExecutablePath: ck3AppPath,
            AttilaExecutablePath: attilaAppPath,
            Ck3ConsoleLogPath: ck3Root + "/console_history.txt",
            AttilaBattleLogPath: DeriveMacAttilaBattleLog(attilaAppPath),
            Ck3SaveGamesDirectory: ck3Root + "/save games");
    }

    // MAC_PATHS.md: BattleResults_log.txt lives in data/ next to the .app bundle.
    // Accepts either the .app directory itself or a binary inside it.
    public static string DeriveMacAttilaBattleLog(string attilaAppPath)
    {
        int appIndex = attilaAppPath.LastIndexOf(".app", StringComparison.OrdinalIgnoreCase);
        string appRoot = appIndex >= 0 ? attilaAppPath.Substring(0, appIndex + 4) : attilaAppPath;

        int separator = Math.Max(appRoot.LastIndexOf('/'), appRoot.LastIndexOf('\\'));
        string installDir = separator >= 0 ? appRoot.Substring(0, separator) : appRoot;

        return installDir + "/data/BattleResults_log.txt";
    }
}
