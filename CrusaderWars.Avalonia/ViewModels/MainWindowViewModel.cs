using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CrusaderWars.Core.Cycle;
using CrusaderWars.Core.Launch;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Paths;
using CrusaderWars.Core.Process;

namespace CrusaderWars.Avalonia.ViewModels;

public class MainWindowViewModel
{
    private AppPaths? _paths;
    private MacPathDetector? _detector;

    public ObservableCollection<string> Log { get; } = new();
    public string Status { get; set; } = "Ready.";
    public bool IsBusy { get; set; }
    public bool StartupFailed { get; set; }

    public void Initialize(AppPaths paths, MacPathDetector detector)
    {
        _paths = paths;
        _detector = detector;
        Status = $"Workspace: {paths.WorkspaceDir}";
        AppendLog($"[INF] Workspace: {paths.WorkspaceDir}");
        AppendLog("[INF] Ready. Trigger the battle in the CK3 mod, then press 'Start Battle'.");
    }

    public void AppendLog(string line)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Log.Add(line);
            Status = line.Length > 120 ? line[..120] : line;
        });
    }

    public async Task StartBattleAsync()
    {
        if (_paths is null || StartupFailed)
        {
            AppendLog("[ERR] Workspace is not initialized. Configure paths first.");
            return;
        }
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await Task.Run(async () =>
            {
                var processes = new PosixProcessController(CrusaderWarsEnvironment.Logger);
                var launcher = new GameLauncher(_paths, CrusaderWarsEnvironment.Logger, processes);
                var cycle = new BattleCycle(_paths, CrusaderWarsEnvironment.Logger, launcher);

                var winner = await cycle.RunFullCycle(
                    TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(180), CancellationToken.None);

                AppendLog($"[INF] Battle applied: winner={winner}. Load battle_results.ck3 in CK3 if it did not continue automatically.");
            });
            Status = "Battle finished.";
        }
        catch (Exception ex)
        {
            AppendLog($"[ERR] Battle cycle failed: {ex.Message}");
            Status = "Battle cycle failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
