using CrusaderWars.Core.Attila;
using CrusaderWars.Core.Launch;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Logs;
using CrusaderWars.Core.Pack;
using CrusaderWars.Core.Paths;
using CrusaderWars.Core.Save;

namespace CrusaderWars.Core.Bridge;

public sealed class BattleBridge
{
    private readonly AppPaths _paths;
    private readonly IAppLogger _logger;
    private readonly Ck3LogWatcher _ck3Log;
    private readonly AttilaBattleLog _attilaLog;
    private readonly SaveFileService _saves;
    private readonly PackFileBuilder _pack;
    private readonly GameLauncher _launcher;
    private readonly AttilaUserScript _userScript;

    public BattleBridge(
        AppPaths paths,
        IAppLogger logger,
        Ck3LogWatcher ck3Log,
        AttilaBattleLog attilaLog,
        SaveFileService saves,
        PackFileBuilder pack,
        GameLauncher launcher,
        AttilaUserScript userScript)
    {
        _paths = paths;
        _logger = logger;
        _ck3Log = ck3Log;
        _attilaLog = attilaLog;
        _saves = saves;
        _pack = pack;
        _launcher = launcher;
        _userScript = userScript;
    }

    public async Task RunAsync(TimeSpan ck3Wait, TimeSpan attilaWait, CancellationToken cancellationToken)
    {
        _userScript.WarnIfPackMissing();
        _attilaLog.Clear();

        await _ck3Log.WaitForBattleAsync(ck3Wait, cancellationToken);

        try
        {
            _launcher.SuspendCk3();
        }
        catch (Exception ex)
        {
            _logger.Warn($"Could not suspend CK3: {ex.Message}");
        }

        _saves.Uncompress();
        _pack.Deploy();
        _launcher.LaunchAttila();
        await _attilaLog.WaitUntilEndedAsync(attilaWait, cancellationToken);

        var summary = _attilaLog.Summarize();
        _logger.Info($"Battle summary: ended={summary.Ended} winner={summary.Winner} siege={summary.SiegeOutcome}/{summary.WallDamage} reports={summary.AliveReports}");

        _saves.Compress();
        _saves.Finish();
        _launcher.CloseAttila();
        _launcher.ResumeCk3();
        _logger.Info("Battle cycle finished. Load battle_results.ck3 in CK3 if it did not continue automatically.");
    }

    public AppPaths Paths => _paths;
}
