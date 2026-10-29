namespace CrusaderWars.Core.Paths;

/// <summary>
/// The five path facts the domain depends on. Mirrors the legacy Properties.Settings keys:
/// VAR_ck3_path, VAR_attila_path, VAR_log_ck3, VAR_log_attila, VAR_dir_save.
/// </summary>
public interface IGamePaths
{
    string Ck3ExecutablePath { get; }

    string AttilaExecutablePath { get; }

    string Ck3ConsoleLogPath { get; }

    string AttilaBattleLogPath { get; }

    string Ck3SaveGamesDirectory { get; }
}
