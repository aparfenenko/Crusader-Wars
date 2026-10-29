using CrusaderWars.Core.Paths;
using Xunit;

namespace CrusaderWars.Core.Tests;

/// <summary>
/// Pins the macOS branch derivations against the layouts documented in docs/MAC_PATHS.md.
/// Pure string logic — no filesystem or OS dependence.
/// </summary>
public class MacPathResolverTests
{
    [Fact]
    public void Full_mac_resolution_matches_steam_layout()
    {
        string documents = "/Users/john/Documents";
        string attilaApp = "/Users/john/Library/Application Support/Steam/steamapps/common/Total War ATTILA/Attila.app";
        string ck3App = "/Users/john/Library/Application Support/Steam/steamapps/common/Crusader Kings III/Crusader Kings III.app";

        GamePaths paths = GamePathResolver.Resolve(GamePlatform.MacOs, documents, attilaApp, ck3App);

        Assert.Equal(ck3App, paths.Ck3ExecutablePath);
        Assert.Equal(attilaApp, paths.AttilaExecutablePath);
        Assert.Equal(documents + "/Paradox Interactive/Crusader Kings III/console_history.txt", paths.Ck3ConsoleLogPath);
        Assert.Equal("/Users/john/Library/Application Support/Steam/steamapps/common/Total War ATTILA/data/BattleResults_log.txt", paths.AttilaBattleLogPath);
        Assert.Equal(documents + "/Paradox Interactive/Crusader Kings III/save games", paths.Ck3SaveGamesDirectory);
    }

    [Theory]
    [InlineData("/Games/Attila.app")]
    [InlineData("/Games/Total War Attila.app")]
    [InlineData("/Games/Attila.app/Contents/MacOS/Attila")]
    [InlineData("/Games/attila.app/Contents/MacOS/Attila")]
    public void Battle_log_is_derived_from_the_directory_next_to_the_app_bundle(string attilaAppPath)
    {
        string log = GamePathResolver.DeriveMacAttilaBattleLog(attilaAppPath);

        Assert.EndsWith("/data/BattleResults_log.txt", log);
        Assert.DoesNotContain(".app", log);
    }

    [Fact]
    public void Battle_log_for_app_binary_points_at_sibling_data_folder()
    {
        string log = GamePathResolver.DeriveMacAttilaBattleLog("/Games/Attila.app/Contents/MacOS/Attila");

        Assert.Equal("/Games/data/BattleResults_log.txt", log);
    }
}
