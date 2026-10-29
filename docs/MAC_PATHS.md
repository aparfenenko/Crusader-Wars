# Mac paths for Crusader Conflicts CLI

The Mac CLI writes detected paths to `~/.config/crusader-conflicts/paths.json`. Run `detect` on the machine that has CK3 and Attila installed, then edit that file if anything is wrong.

## Expected locations

| Role | Typical macOS path |
|------|--------------------|
| CK3 documents | `~/Documents/Paradox Interactive/Crusader Kings III/` |
| CK3 console log | `~/Documents/Paradox Interactive/Crusader Kings III/console_history.txt` |
| CK3 saves | `~/Documents/Paradox Interactive/Crusader Kings III/save games/` |
| CK3 last save | `~/Documents/Paradox Interactive/Crusader Kings III/last_save.ck3` |
| CK3 app (Steam) | `~/Library/Application Support/Steam/steamapps/common/Crusader Kings III/*.app` |
| Attila app (Steam) | `~/Library/Application Support/Steam/steamapps/common/Total War ATTILA/*.app` |
| Attila `data/` | Next to the `.app`, usually `.../Total War ATTILA/data/` |
| Attila battle log | `.../data/BattleResults_log.txt` |
| Attila user.script | `~/Library/Application Support/The Creative Assembly/Attila/scripts/user.script.txt` |
| rpfm_cli | `tools/rpfm_cli` in this repo, or a Homebrew path |

## Smoke test

```bash
dotnet run --project CrusaderWars.MacCli -- detect
dotnet run --project CrusaderWars.MacCli -- watch
dotnet run --project CrusaderWars.MacCli -- launch-attila
dotnet run --project CrusaderWars.MacCli -- wait-battle
```

Fill in this file with the `OK` / `MISSING` lines from `detect` after the first run on a real Mac install.

## Notes from first detect run

Not yet filled. Run `CrusaderConflicts.MacCli detect` and paste the table here.
