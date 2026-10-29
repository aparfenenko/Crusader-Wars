using CrusaderWars.Core.Pack;
using Xunit;

namespace CrusaderWars.Core.Tests;

public class RpfmCliLocatorTests
{
    [Fact]
    public void Configured_path_wins_over_fallback()
    {
        string resolved = RpfmCliLocator.Resolve("/opt/homebrew/bin/rpfm_cli", @"data\rpfm\rpfm_cli.exe");

        Assert.Equal("/opt/homebrew/bin/rpfm_cli", resolved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_configured_path_falls_back_to_legacy_layout(string? configured)
    {
        string resolved = RpfmCliLocator.Resolve(configured, @"data\rpfm\rpfm_cli.exe");

        Assert.Equal(@"data\rpfm\rpfm_cli.exe", resolved);
    }
}
