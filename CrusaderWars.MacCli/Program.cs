using CrusaderWars.Core.Attila;
using CrusaderWars.Core.Bridge;
using CrusaderWars.Core.Dialogs;
using CrusaderWars.Core.Launch;
using CrusaderWars.Core.Logging;
using CrusaderWars.Core.Logs;
using CrusaderWars.Core.Pack;
using CrusaderWars.Core.Paths;
using CrusaderWars.Core.Process;
using CrusaderWars.Core.Save;
using CrusaderWars.MacCli;
using CrusaderWars;
using CrusaderWars.client;
using CrusaderWars.mod_manager;
using CrusaderWars.armies;
using CrusaderWars.data.battle_results;
using CrusaderWars.data.save_file;
using CrusaderWars.locs;
using CrusaderWars.terrain;
using CrusaderWars.unit_mapper;
using CrusaderWars.twbattle;

var logger = new ConsoleLogger();

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintHelp();
    return 0;
}

string command = args[0];
var flags = ParseFlags(args.Skip(1).ToArray());

try
{
    return await RunCommand(command, flags, logger);
}
catch (Exception ex)
{
    logger.Error(ex.Message);
    logger.Debug(ex.ToString());
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("""
        Crusader Conflicts Mac CLI

        Usage:
          CrusaderConflicts.MacCli <command> [options]

        Commands:
          detect          Find CK3/Attila paths and write ~/.config/crusader-conflicts/paths.json
          watch           Wait until CK3 writes a battle dump to console_history.txt
          parse-console   Parse the current console_history battle dump (DataSearch) and print the facts
          read-save       parse-console + uncompress last_save.ck3 and read armies (ArmiesReader)
          generate-battle Full read pipeline: parse → save → armies → Attila battle files → pack
          launch-attila   Start native Attila with used_mods_cc.txt
          launch-ck3      Start native CK3 (add --continue-last-save to load last save)
          wait-battle     Poll Attila BattleResults_log.txt until the battle ends
          apply-results   Re-pack last_save.ck3 into battle_results.ck3
          run             Full cycle: watch CK3 → pack → Attila → log → save → resume CK3

        Options:
          --timeout-minutes <n>   Wait timeout (default: 180 for battles, 60 for CK3 watch)
          --continue-last-save    Launch CK3 with --continuelastsave
        """);
}

static async Task<int> RunCommand(string command, Dictionary<string, string> flags, ConsoleLogger logger)
{
    var detector = new MacPathDetector(logger);
    int timeoutMinutes = flags.TryGetValue("timeout-minutes", out string? t) && int.TryParse(t, out int n) ? n : 0;
    bool continueLastSave = flags.ContainsKey("continue-last-save");

    switch (command)
    {
        case "detect":
        {
            var detected = detector.Detect();
            PrintDetection(detector.Describe(detected));
            detected.Save();
            logger.Info($"Wrote {PathConfig.DefaultConfigPath}");
            return 0;
        }
        case "parse-console":
        {
            var (paths, _) = LoadServices(logger, detector);
            var watcher = new Ck3LogWatcher(paths, logger);
            if (!watcher.TryRead(out string content) || string.IsNullOrWhiteSpace(content))
            {
                logger.Error($"No battle dump in console_history.txt: {watcher.LogPath}. Run 'watch' first or trigger a battle in the CK3 mod.");
                return 1;
            }

            DataSearch.Search(content);
            PrintParsedBattle();
            return 0;
        }
        case "watch":
        {
            var (paths, _) = LoadServices(logger, detector);
            var watcher = new Ck3LogWatcher(paths, logger);
            if (!File.Exists(watcher.LogPath))
            {
                logger.Warn($"console_history.txt not found yet: {watcher.LogPath}");
            }
            TimeSpan wait = TimeSpan.FromMinutes(timeoutMinutes > 0 ? timeoutMinutes : 60);
            string type = await watcher.WaitForBattleAsync(wait, CancellationToken.None);
            Console.WriteLine($"battle-type={type}");
            return 0;
        }
        case "read-save":
        {
            var (paths, _) = LoadServices(logger, detector);
            var watcher = new Ck3LogWatcher(paths, logger);
            if (!watcher.TryRead(out string content) || string.IsNullOrWhiteSpace(content))
            {
                logger.Error($"No battle dump in console_history.txt: {watcher.LogPath}.");
                return 1;
            }
            DataSearch.Search(content);

            SaveFile.Uncompress();
            var (attacker, defender) = ArmiesReader.ReadBattleArmies();
            PrintArmies("attacker", attacker);
            PrintArmies("defender", defender);
            return 0;
        }
        case "generate-battle":
        {
            var (paths, _) = LoadServices(logger, detector);
            var watcher = new Ck3LogWatcher(paths, logger);
            if (!watcher.TryRead(out string content) || string.IsNullOrWhiteSpace(content))
            {
                logger.Error($"No battle dump in console_history.txt: {watcher.LogPath}.");
                return 1;
            }
            DataSearch.Search(content);

            SaveFile.Uncompress();
            BattleResult.WarID = ArmiesReader.FindWarID();
            var (attacker, defender, left, right) = PrepareArmies();
            GenerateBattleFiles(attacker, defender, left, right);
            Console.WriteLine("generate-battle done: battle files written, pack deployed if rpfm_cli succeeded.");
            return 0;
        }
        case "run":
        {
            var (paths, launcher) = LoadServices(logger, detector);
            var watcher = new Ck3LogWatcher(paths, logger);
            logger.Info($"Waiting for a battle dump in {watcher.LogPath} (trigger the battle in the CK3 mod)...");
            TimeSpan ck3Wait = TimeSpan.FromMinutes(timeoutMinutes > 0 ? timeoutMinutes : 60);
            await watcher.WaitForBattleAsync(ck3Wait, CancellationToken.None);
            if (!watcher.TryRead(out string content) || string.IsNullOrWhiteSpace(content))
            {
                logger.Error($"No battle dump in console_history.txt: {watcher.LogPath}.");
                return 1;
            }
            DataSearch.Search(content);

            SaveFile.Uncompress();
            BattleResult.WarID = ArmiesReader.FindWarID();
            var (attacker, defender, left, right) = PrepareArmies();
            GenerateBattleFiles(attacker, defender, left, right);

            try
            {
                launcher.SuspendCk3();
            }
            catch (Exception ex)
            {
                logger.Warn($"Could not suspend CK3: {ex.Message}");
            }

            new AttilaUserScript(paths, logger).WarnIfPackMissing();
            launcher.LaunchAttila();

            string attilaLogPath = CrusaderWarsEnvironment.Paths.AttilaBattleLogPath;
            TimeSpan attilaWait = TimeSpan.FromMinutes(timeoutMinutes > 0 ? timeoutMinutes : 180);
            logger.Info($"Waiting for the battle to end (log: {attilaLogPath})...");
            var deadline = DateTime.UtcNow + attilaWait;
            bool battleEnded = false;
            while (DateTime.UtcNow < deadline)
            {
                battleEnded = BattleResultReader.HasBattleEnded(attilaLogPath);
                if (battleEnded) break;
                await Task.Delay(1000);
            }
            if (!battleEnded)
            {
                logger.Error("Battle did not end within the timeout. Run again with --timeout-minutes to wait longer.");
                return 1;
            }

            string path_editedSave = "./data/save_file_data/gamestate_file/gamestate";
            string winner = ResultsProcessor.Apply(attacker, defender, left, right, attilaLogPath, path_editedSave);
            Console.WriteLine($"battle applied: winner={winner}. Load battle_results.ck3 in CK3 if it did not continue automatically.");
            return 0;
        }
        default:
            logger.Error($"Unknown command: {command}");
            PrintHelp();
            return 1;
    }
}

static (AppPaths Paths, GameLauncher Launcher) LoadServices(ConsoleLogger logger, MacPathDetector detector)
{
    PathConfig config = File.Exists(PathConfig.DefaultConfigPath) ? PathConfig.Load() : detector.Detect();
    if (!File.Exists(PathConfig.DefaultConfigPath))
    {
        config.Save();
        logger.Info($"Wrote {PathConfig.DefaultConfigPath} (run 'detect' to refresh).");
    }

    var paths = AppPaths.FromConfig(config);

    // Domain code resolves workspace-relative paths (settings\Options.xml, data\...) from
    // the current directory, mirroring the Windows app layout.
    if (Directory.Exists(paths.WorkspaceDir))
        Environment.CurrentDirectory = paths.WorkspaceDir;

    CrusaderWarsEnvironment.Initialize(new AppPathsGamePaths(paths), new ConsoleDialogService(), logger);
    if (!OptionsFileStore.ReadOptionsFile() || !ActiveSubmods.Load())
    {
        logger.Error("Settings load failed. Exiting.");
        Environment.Exit(1);
    }

    var processes = new PosixProcessController(logger);
    var launcher = new GameLauncher(paths, logger, processes);
    return (paths, launcher);
}

static void PrintDetection(IReadOnlyList<DetectionResult> results)
{
    foreach (var item in results)
    {
        string mark = item.Found ? "OK" : "MISSING";
        Console.WriteLine($"{mark,-8} {item.Label,-24} {item.Path}");
    }
}

static void PrintSummary(BattleLogSummary summary)
{
    Console.WriteLine($"ended={summary.Ended} winner={summary.Winner} siege={summary.SiegeOutcome} walls={summary.WallDamage} aliveReports={summary.AliveReports} killReports={summary.KillReports}");
}

static (List<Army> attacker, List<Army> defender, List<Army> left, List<Army> right) PrepareArmies()
{
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

static void GenerateBattleFiles(List<Army> attacker, List<Army> defender, List<Army> left, List<Army> right)
{
    BattleFile.SetArmiesSides(attacker, defender);

    if (!ModOptions.optionsValuesCollection.TryGetValue("CombineKnights", out string? combineKnights) || combineKnights != "Enabled")
        ProminentKnights.Process(attacker, defender);
    if (!BattleState.IsSiegeBattle)
        TerrainGenerator.CheckForSpecialCrossingBattle(attacker, defender);

    CrusaderWarsEnvironment.Logger.Info("Clearing previous battle files...");
    BattleFile.ClearFile();
    DeclarationsFile.Erase();
    BattleScript.EraseScript(BattleState.IsSiegeBattle);
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

    CrusaderWarsEnvironment.Logger.Info("Creating TW:Attila battle files...");
    BattleFile.BETA_CreateBattle(attacker, defender);

    if (BattleLog.HasUnmappedUnits())
    {
        CrusaderWarsEnvironment.Logger.Warn("Some CK3 units could not be mapped to Total War: Attila units and were dropped:");
        foreach (var u in BattleLog.GetUnmappedUnits().Distinct())
            Console.WriteLine($"  - type={u.RegimentType} name={u.UnitName} faction={u.AttilaFaction} culture={u.Culture}");
    }

    CrusaderWarsEnvironment.Logger.Info("Creating .pack file (rpfm)...");
    PackFile.PackFileCreator();
}

static void PrintArmies(string label, List<Army> armies)
{
    Console.WriteLine($"{label}: {armies.Count} armies");
    foreach (var army in armies)
    {
        Console.WriteLine($"  army id={army.ID} realm={army.RealmName} side={army.CombatSide} regiments={army.ArmyRegiments.Count} units={army.Units.Count}");
    }
}

static void PrintParsedBattle()
{
    var leftCommander = CK3LogData.LeftSide.GetCommander();
    var rightCommander = CK3LogData.RightSide.GetCommander();

    Console.WriteLine($"battle-type={(BattleState.IsSiegeBattle ? "SIEGE" : "BATTLE")}");
    Console.WriteLine($"date={Date.Year}.{Date.Month}.{Date.Day} season={Date.GetSeason()}");
    Console.WriteLine($"left-realm={CK3LogData.LeftSide.GetRealmName()} commander={leftCommander.name} prowess={leftCommander.prowess} martial={leftCommander.martial} knights={CK3LogData.LeftSide.GetKnights().Count}");
    Console.WriteLine($"right-realm={CK3LogData.RightSide.GetRealmName()} commander={rightCommander.name} prowess={rightCommander.prowess} martial={rightCommander.martial} knights={CK3LogData.RightSide.GetKnights().Count}");
    var player = DataSearch.Player_Character;
    Console.WriteLine($"player-character={(player is null ? "n/a" : $"{player.GetID()} culture={player.GetCultureID()}")}");
}

static Dictionary<string, string> ParseFlags(string[] args)
{
    var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];
        if (!arg.StartsWith("--", StringComparison.Ordinal))
            continue;
        string key = arg[2..];
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            flags[key] = args[++i];
        }
        else
        {
            flags[key] = "true";
        }
    }
    return flags;
}
