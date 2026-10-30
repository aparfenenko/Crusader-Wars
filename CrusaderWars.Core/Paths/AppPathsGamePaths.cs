namespace CrusaderWars.Core.Paths;

/// <summary>
/// IGamePaths view over the mac config model (AppPaths from paths.json).
/// </summary>
public sealed class AppPathsGamePaths : IGamePaths
{
    private readonly AppPaths _paths;

    public AppPathsGamePaths(AppPaths paths)
    {
        _paths = paths;
    }

    public string Ck3ExecutablePath => _paths.Ck3App;
    public string AttilaExecutablePath => _paths.AttilaApp;
    public string Ck3ConsoleLogPath => _paths.Ck3Log;
    public string AttilaBattleLogPath => _paths.AttilaLog;
    public string Ck3SaveGamesDirectory => _paths.SaveGames;
}
