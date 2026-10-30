using CrusaderWars.Core.Paths;
using Xunit;

namespace CrusaderWars.Core.Tests;

public class PathConfigTests
{
    [Fact]
    public void FillMissingFrom_keeps_manual_values()
    {
        var manual = new PathConfig
        {
            Ck3App = "/Games/Crusader Kings III/ck3.app",
            AttilaApp = "/Games/GOG Games/Attila.app",
            SaveGames = ""
        };
        var detected = new PathConfig
        {
            Ck3App = "/Steam/Crusader Kings III/ck3.app",
            AttilaApp = "/Steam/Total War ATTILA/Attila.app",
            SaveGames = "/Steam/Documents/save games"
        };

        manual.FillMissingFrom(detected);

        Assert.Equal("/Games/Crusader Kings III/ck3.app", manual.Ck3App);
        Assert.Equal("/Games/GOG Games/Attila.app", manual.AttilaApp);
        Assert.Equal("/Steam/Documents/save games", manual.SaveGames);
    }

    [Fact]
    public void SetByName_and_ExistsByName_roundtrip()
    {
        var config = new PathConfig();

        Assert.True(config.SetByName("ck3App", "/definitely/not/here/ck3.app"));
        Assert.Equal("/definitely/not/here/ck3.app", config.Ck3App);
        Assert.False(config.ExistsByName("ck3App"));

        Assert.True(config.SetByName("attilaLog", "/does/not/exist.txt"));
        Assert.False(config.ExistsByName("attilaLog"));

        Assert.False(config.SetByName("unknownKey", "x"));
    }

    [Fact]
    public void ExistsByName_allows_empty_fields()
    {
        var config = new PathConfig();
        Assert.True(config.ExistsByName("ck3App"));
        Assert.True(config.ExistsByName("attilaData"));
    }

    [Fact]
    public void Json_roundtrip_preserves_manual_paths()
    {
        var config = new PathConfig { Ck3App = "/Games/ck3.app" };
        var json = System.Text.Json.JsonSerializer.Serialize(config);
        var restored = System.Text.Json.JsonSerializer.Deserialize<PathConfig>(json);

        Assert.NotNull(restored);
        Assert.Equal("/Games/ck3.app", restored!.Ck3App);
    }
}
