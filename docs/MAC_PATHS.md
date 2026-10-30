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

## Workspace data supply (decision)

The battle pipeline needs the same distribution pieces next to the workspace as the
Windows application has in its output folder: `unit mappers/` (mappers XML),
`data/schema_att.ron`, `data/battle files/`, `settings/schemas/*.xsd` and
`tools/rpfm_cli`. These are NOT in the git repository — on Windows they are
downloaded/installed by the updater (CWUpdater). On macOS the model is:

- The CLI runs inside a Crusader Conflicts workspace (detected by walking up from the
  current directory until `data/battle files` or `CrusaderWars.sln` is found).
- `detect` reports each required piece as OK / MISSING — build the workspace by copying
  the pieces from a Windows Crusader Conflicts installation (or a future mac updater).
- `generate-battle` fails with a clear error if a required piece is absent.

Fill in this file with the `OK` / `MISSING` lines from `detect` after the first run on a real Mac install.

## Notes from first detect run

Not yet filled. Run `CrusaderConflicts.MacCli detect` and paste the table here.
