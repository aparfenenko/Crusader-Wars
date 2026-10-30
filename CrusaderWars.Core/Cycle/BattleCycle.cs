using CrusaderWars;
using CrusaderWars.client;
using CrusaderWars.twbattle;
using CrusaderWars.Core.Launch;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Logs;
using CrusaderWars.Core.Paths;
using CrusaderWars.armies;
using CrusaderWars.data.battle_results;
using CrusaderWars.data.save_file;
using CrusaderWars.locs;
using CrusaderWars.terrain;
using CrusaderWars.unit_mapper;

namespace CrusaderWars.Core.Cycle
{
    /// <summary>
    /// The macOS battle cycle orchestrator, shared by the CLI and the Avalonia UI.
    /// Granular methods mirror the Windows single-process flow exactly: the results are
    /// applied to the same scaled in-memory armies the battle was generated from.
    /// </summary>
    public sealed class BattleCycle
    {
        private readonly AppPaths _paths;
        private readonly IAppLogger _logger;
        private readonly GameLauncher _launcher;

        public BattleCycle(AppPaths paths, IAppLogger logger, GameLauncher launcher)
        {
            _paths = paths;
            _logger = logger;
            _launcher = launcher;
        }

        public string Ck3LogPath => _paths.Ck3Log;
        public string AttilaLogPath => _paths.AttilaLog;

        /// <summary>Blocks until CK3 writes a battle dump, then returns its text.</summary>
        public async Task<string> WaitForDumpAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var watcher = new Ck3LogWatcher(_paths, _logger);
            await watcher.WaitForBattleAsync(timeout, cancellationToken);
            if (!watcher.TryRead(out string content) || string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException($"No battle dump in console_history.txt: {watcher.LogPath}");
            return content;
        }

        /// <summary>Parses an already read console dump and prepares the armies.</summary>
        public (List<Army> attacker, List<Army> defender, List<Army> left, List<Army> right) PrepareFromDump(string dump)
        {
            DataSearch.Search(dump);
            return PrepareArmies();
        }

        private (List<Army> attacker, List<Army> defender, List<Army> left, List<Army> right) PrepareArmies()
        {
            SaveFile.Uncompress();
            BattleResult.WarID = ArmiesReader.FindWarID();
            UnitsFile.ResetProcessedArmies();
            // Single parse: unit IDs are assigned globally during parsing, so attacker and
            // defender lists must come from the same ReadBattleArmies call.
            var (attacker, defender) = ArmiesReader.ReadBattleArmies();
            var left = ArmiesReader.GetSideArmies("left", attacker, defender)
                ?? throw new InvalidOperationException("Could not determine the left side armies.");
            var right = ArmiesReader.GetSideArmies("right", attacker, defender)
                ?? throw new InvalidOperationException("Could not determine the right side armies.");
            return (attacker, defender, left, right);
        }

        /// <summary>Clears previous battle files and generates the Attila battle files plus the pack.</summary>
        public void GenerateBattleFiles(List<Army> attacker, List<Army> defender, List<Army> left, List<Army> right)
        {
            BattleFile.SetArmiesSides(attacker, defender);

            if (!ModOptions.optionsValuesCollection.TryGetValue("CombineKnights", out string? combineKnights) || combineKnights != "Enabled")
                ProminentKnights.Process(attacker, defender);
            if (!twbattle.BattleState.IsSiegeBattle)
                TerrainGenerator.CheckForSpecialCrossingBattle(attacker, defender);

            _logger.Info("Clearing previous battle files...");
            BattleFile.ClearFile();
            DeclarationsFile.Erase();
            BattleScript.EraseScript(twbattle.BattleState.IsSiegeBattle);
            Data.units_scripts.Clear();
            BattleResultReader.ClearAttilaLog();
            UnitsCardsNames.RemoveFiles();

            int leftTotal = left.Sum(a => a.GetTotalSoldiers());
            int rightTotal = right.Sum(a => a.GetTotalSoldiers());
            BattleDetails.ChangeBattleDetails(leftTotal, rightTotal, left[0].CombatSide, right[0].CombatSide);
            BattleScript.CreateScript();

            int totalSoldiers = attacker.SelectMany(a => a.Units).Sum(u => u.GetSoldiers()) +
                                defender.SelectMany(a => a.Units).Sum(u => u.GetSoldiers());
            ArmyProportions.AutoSizeUnits(totalSoldiers);
            foreach (var army in attacker) army.ScaleUnits(ArmyProportions.BattleScale);
            foreach (var army in defender) army.ScaleUnits(ArmyProportions.BattleScale);
            BattleLog.Reset();

            _logger.Info("Creating TW:Attila battle files...");
            BattleFile.BETA_CreateBattle(attacker, defender);

            if (BattleLog.HasUnmappedUnits())
            {
                _logger.Warn("Some CK3 units could not be mapped to Total War: Attila units and were dropped:");
                foreach (var u in BattleLog.GetUnmappedUnits().Distinct())
                    _logger.Warn($"  - type={u.RegimentType} name={u.UnitName} faction={u.AttilaFaction} culture={u.Culture}");
            }

            _logger.Info("Creating .pack file (rpfm)...");
            PackFile.PackFileCreator();
        }

        public void SuspendCk3()
        {
            try
            {
                _launcher.SuspendCk3();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not suspend CK3: {ex.Message}");
            }
        }

        public void LaunchAttila()
        {
            _launcher.LaunchAttila();
        }

        /// <summary>Polls the Attila battle log like the Windows app (once per second).</summary>
        public async Task<bool> WaitForBattleEndAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (BattleResultReader.HasBattleEnded(AttilaLogPath))
                    return true;
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken);
            }
            return false;
        }

        /// <summary>
        /// Full single-process cycle: watch → parse → uncompress → armies → generate →
        /// suspend CK3 → launch Attila → wait for the battle end → apply results.
        /// Returns the winner ("attacker"/"defender").
        /// </summary>
        public async Task<string> RunFullCycle(TimeSpan ck3Wait, TimeSpan attilaWait, CancellationToken cancellationToken)
        {
            string dump = await WaitForDumpAsync(ck3Wait, cancellationToken);
            var (attacker, defender, left, right) = PrepareFromDump(dump);
            GenerateBattleFiles(attacker, defender, left, right);

            SuspendCk3();
            LaunchAttila();

            bool ended = await WaitForBattleEndAsync(attilaWait, cancellationToken);
            if (!ended)
                throw new TimeoutException("Battle did not end within the timeout.");

            return twbattle.ResultsProcessor.Apply(attacker, defender, left, right, AttilaLogPath, "./data/save_file_data/gamestate_file/gamestate");
        }
    }
}
