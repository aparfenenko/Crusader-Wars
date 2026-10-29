using CrusaderWars.Core.Paths;
using Xunit;

namespace CrusaderWars.Core.Tests;

/// <summary>
/// Parity pins: the resolver's Windows branch must reproduce the legacy derivations
/// byte-for-byte. The reference formulas below are verbatim copies of the original code
/// (MainFile.cs startup block and BattleResult.cs/BattleResultReader.cs log derivation).
/// </summary>
public class WindowsPathParityTests
{
    private static string LegacyAttilaLog(string attilaPath) =>
        attilaPath.Substring(0, attilaPath.IndexOf("Attila.exe")) + "data\\BattleResults_log.txt";

    private static string LegacyConsoleLog(string documentsPath) =>
        documentsPath + "\\Paradox Interactive\\Crusader Kings III\\console_history.txt";

    private static string LegacySaveGames(string documentsPath) =>
        documentsPath + "\\Paradox Interactive\\Crusader Kings III\\save games";

    public static TheoryData<string> AttilaExePaths => new()
    {
        @"C:\Program Files (x86)\Steam\steamapps\common\Total War ATTILA\Attila.exe",
        @"D:\SteamLibrary\steamapps\common\Total War ATTILA\Attila.exe",
        @"E:\Games\Total War ATTILA\Attila.exe",
        @"C:\GOG Games\Total War ATTILA\Attila.exe",
        @"D:\Steam\steamapps\common\Total War ATTILA\Attila.exe",
        @"\\NAS\games\Total War ATTILA\Attila.exe",
        @"C:\Users\Пётр\Steam\steamapps\common\Total War ATTILA\Attila.exe",
        @"D:\Total War ATTILA\Attila.exe",
        @"E:\tw_attila\binaries\Attila.exe",
        @"C:\Attila.exe"
    };

    public static TheoryData<string> DocumentsPaths => new()
    {
        @"C:\Users\John\Documents",
        @"C:\Users\John\OneDrive\Documents",
        @"D:\Docs",
        @"\\NAS\profiles\John\Documents",
        @"C:\Users\Иван\Документы",
        @"C:\Users\test user\Documents"
    };

    [Theory]
    [MemberData(nameof(AttilaExePaths))]
    public void Attila_battle_log_matches_legacy_formula(string attilaExe)
    {
        string resolved = GamePathResolver.DeriveWindowsAttilaBattleLog(attilaExe);

        Assert.Equal(LegacyAttilaLog(attilaExe), resolved);
    }

    [Theory]
    [MemberData(nameof(DocumentsPaths))]
    public void Console_log_and_save_games_match_legacy_formulas(string documentsPath)
    {
        GamePaths paths = GamePathResolver.ResolveWindows(documentsPath, @"C:\Steam\Total War ATTILA\Attila.exe", @"C:\CK3\binaries\ck3.exe");

        Assert.Equal(LegacyConsoleLog(documentsPath), paths.Ck3ConsoleLogPath);
        Assert.Equal(LegacySaveGames(documentsPath), paths.Ck3SaveGamesDirectory);
    }

    [Fact]
    public void Full_windows_resolution_matches_legacy_for_realistic_layout()
    {
        string documents = @"C:\Users\John\Documents";
        string attilaExe = @"D:\SteamLibrary\steamapps\common\Total War ATTILA\Attila.exe";
        string ck3Exe = @"C:\Program Files (x86)\Steam\steamapps\common\Crusader Kings III\binaries\ck3.exe";

        GamePaths paths = GamePathResolver.Resolve(GamePlatform.Windows, documents, attilaExe, ck3Exe);

        Assert.Equal(ck3Exe, paths.Ck3ExecutablePath);
        Assert.Equal(attilaExe, paths.AttilaExecutablePath);
        Assert.Equal(documents + @"\Paradox Interactive\Crusader Kings III\console_history.txt", paths.Ck3ConsoleLogPath);
        Assert.Equal(@"D:\SteamLibrary\steamapps\common\Total War ATTILA\data\BattleResults_log.txt", paths.AttilaBattleLogPath);
        Assert.Equal(documents + @"\Paradox Interactive\Crusader Kings III\save games", paths.Ck3SaveGamesDirectory);
    }

    [Theory]
    [InlineData(@"D:\Games\Total War ATTILA")]
    [InlineData(@"C:\Steam\steamapps\common\Total War ATTILA\attila.exe")]
    public void Missing_attila_exe_suffix_throws_like_legacy(string attilaPath)
    {
        Assert.ThrowsAny<ArgumentException>(() => GamePathResolver.DeriveWindowsAttilaBattleLog(attilaPath));
    }
}
